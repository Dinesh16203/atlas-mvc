using Atlas.Web.Data;
using Atlas.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Web.Controllers;

[Route("api/time-entries")]
public sealed class TimeEntriesController(UserManager<AppUser> users,AtlasDbContext db,AuditWriter audit) : ApiControllerBase(users,db)
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery]string? start=null,[FromQuery]string? end=null)
    {
        var actor=await Actor();
        var scoped=await ScopedSubmissions(actor);
        var ids=await scoped.Select(s=>s.Id).ToListAsync();
        var query=Db.TimeEntries.Where(e=>ids.Contains(e.SubmissionId));
        if(DateTime.TryParse(start,out var from))query=query.Where(e=>e.WorkDate>=from.Date);
        if(DateTime.TryParse(end,out var to))query=query.Where(e=>e.WorkDate<to.Date.AddDays(1));
        var result=await query.OrderByDescending(e=>e.WorkDate).Take(3000).ToListAsync();
        return Ok(new { entries=result.Select(SubmissionMapper.ToDto) });
    }

    [HttpPost]
    public async Task<IActionResult> Create(EntryWrite request)
    {
        var actor=await Actor();
        if(!DateTime.TryParse(request.Date,out var date))return BadRequest(new { error="Choose a valid work date." });
        var hourError=TimeEntryRules.Validate(request.Hours,date,DateTime.Today);
        if(hourError is not null)return BadRequest(new { error=hourError });
        var role=await Role(actor);
        var target=actor;
        if(!string.IsNullOrWhiteSpace(request.EmployeeId)&&request.EmployeeId!=actor.Id)
        {
            if(role is not (AtlasRoles.Lead or AtlasRoles.Administrator))return Forbid();
            target=await Db.Users.FirstOrDefaultAsync(x=>x.Id==request.EmployeeId&&x.IsActive)??throw new InvalidOperationException("Team member not found.");
            if(role==AtlasRoles.Lead&&target.TeamId!=actor.TeamId)return Forbid();
        }
        var project=await Db.Projects.FirstOrDefaultAsync(x=>x.Id==request.ProjectServiceId&&x.Active);
        var work=await Db.WorkItems.FirstOrDefaultAsync(x=>x.Id==request.WorkItemId&&x.Active);
        var task=await Db.TaskCatalog.FirstOrDefaultAsync(x=>x.Id==request.TaskCatalogId&&x.Active);
        if(project is null||work is null||task is null)return BadRequest(new { error="Select an active task, project, and work item." });
        if(!Models.Contains(request.CommercialModel))return BadRequest(new { error="Select a valid commercial model." });
        if(work.ProjectServiceId!=project.Id)return BadRequest(new { error="The selected work item does not belong to this project / service." });
        if(role is not (AtlasRoles.Administrator or AtlasRoles.Management) && project.DomainId!=target.DomainId)return BadRequest(new { error="Select a project / service in the employee's domain." });
        if(task.TeamId is not null&&task.TeamId!=target.TeamId||task.DomainId is not null&&task.DomainId!=target.DomainId)return BadRequest(new { error="The selected catalogue task is not available to this team and domain." });
        if(!string.IsNullOrWhiteSpace(request.ExceptionReason)&&request.Hours>TimeEntryRules.MaximumHoursPerDay)
            return BadRequest(new { error="An exception request cannot exceed the 8-hour daily entry limit. Split the work across dates or contact an administrator." });
        var week=DatabaseInitializer.WeekStart(date);
        var submission=await Db.Submissions.FirstOrDefaultAsync(s=>s.UserId==target.Id&&s.WeekStart==week);
        if(submission is null){submission=new WeekSubmission { UserId=target.Id,WeekStart=week,ReviewerId=target.ManagerId,RequiredCapacity=40,Status="Draft" };Db.Submissions.Add(submission);}
        if(submission.Status is not ("Draft" or "Returned" or "Clarification Requested"))return BadRequest(new { error="Withdraw this submitted week before editing its entries." });
        if(role!=AtlasRoles.Administrator&&date.Date<new DateTime(DateTime.Today.Year,DateTime.Today.Month,1))return BadRequest(new { error="Previous months are locked. Contact an administrator for a correction." });
        var used=await Db.TimeEntries.Where(x=>x.SubmissionId==submission.Id&&x.WorkDate==date.Date).SumAsync(x=>(double?)x.ActualHours)??0;
        if(used+request.Hours>TimeEntryRules.MaximumHoursPerDay+0.0001)return BadRequest(new { error=$"This would exceed 8.00 hours for {date:ddd, MMM d}. Current total: {used:0.##} hours." });
        var entry=FromRequest(request,submission.Id,project,work,task,date);
        Db.TimeEntries.Add(entry);
        audit.Add(actor,"Created time entry","TimeEntry",entry.Id,$"Employee: {target.DisplayName}; Work item: {entry.WorkItemNumber}; Hours: {entry.ActualHours:0.##}");
        await Db.SaveChangesAsync();
        return Created($"/api/time-entries/{entry.Id}",new { entry=SubmissionMapper.ToDto(entry) });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id,EntryWrite request)
    {
        var actor=await Actor();
        var entry=await Db.TimeEntries.FirstOrDefaultAsync(x=>x.Id==id);
        if(entry is null)return NotFound(new { error="Entry not found." });
        var submission=await Db.Submissions.FirstAsync(x=>x.Id==entry.SubmissionId);
        if(!await CanEdit(actor,submission))return Forbid();
        if(!DateTime.TryParse(request.Date,out var date))return BadRequest(new { error="Choose a valid work date." });
        var hourError=TimeEntryRules.Validate(request.Hours,date,DateTime.Today);
        if(hourError is not null)return BadRequest(new { error=hourError });
        if(submission.Status is not ("Draft" or "Returned" or "Clarification Requested"))return BadRequest(new { error="Withdraw the submitted week before editing." });
        if((await Role(actor))!=AtlasRoles.Administrator&&date.Date<new DateTime(DateTime.Today.Year,DateTime.Today.Month,1))return BadRequest(new { error="Previous months are locked. Contact an administrator for a correction." });
        var project=await Db.Projects.FirstOrDefaultAsync(x=>x.Id==request.ProjectServiceId&&x.Active);
        var work=await Db.WorkItems.FirstOrDefaultAsync(x=>x.Id==request.WorkItemId&&x.Active);
        var task=await Db.TaskCatalog.FirstOrDefaultAsync(x=>x.Id==request.TaskCatalogId&&x.Active);
        if(project is null||work is null||task is null)return BadRequest(new { error="Select an active task, project, and work item." });
        if(!Models.Contains(request.CommercialModel))return BadRequest(new { error="Select a valid commercial model." });
        if(work.ProjectServiceId!=project.Id)return BadRequest(new { error="The selected work item does not belong to this project / service." });
        var entryOwner=await Db.Users.FirstAsync(x=>x.Id==submission.UserId);
        if((await Role(actor)) is not (AtlasRoles.Administrator or AtlasRoles.Management) && project.DomainId!=entryOwner.DomainId)return BadRequest(new { error="Select a project / service in the employee's domain." });
        if(task.TeamId is not null&&task.TeamId!=entryOwner.TeamId||task.DomainId is not null&&task.DomainId!=entryOwner.DomainId)return BadRequest(new { error="The selected catalogue task is not available to this team and domain." });
        var used=(await Db.TimeEntries.Where(x=>x.SubmissionId==submission.Id&&x.WorkDate==date.Date&&x.Id!=id).SumAsync(x=>(double?)x.ActualHours)??0)+request.Hours;
        if(used>TimeEntryRules.MaximumHoursPerDay+0.0001)return BadRequest(new { error=$"Daily total would exceed 8.00 hours. Requested day total: {used:0.##}." });
        var oldDate=entry.WorkDate;
        CopyRequest(entry,request,project,work,task,date);
        audit.Add(actor,"Updated time entry","TimeEntry",entry.Id,$"Date {oldDate:yyyy-MM-dd} to {date:yyyy-MM-dd}; hours {entry.ActualHours:0.##}");
        await Db.SaveChangesAsync();
        return Ok(new { entry=SubmissionMapper.ToDto(entry) });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var actor=await Actor();
        var entry=await Db.TimeEntries.FirstOrDefaultAsync(x=>x.Id==id);
        if(entry is null)return NotFound(new { error="Entry not found." });
        var submission=await Db.Submissions.FirstAsync(x=>x.Id==entry.SubmissionId);
        if(!await CanEdit(actor,submission))return Forbid();
        if(submission.Status is not ("Draft" or "Returned" or "Clarification Requested"))return BadRequest(new { error="Withdraw the submitted week before deleting entries." });
        if((await Role(actor))!=AtlasRoles.Administrator&&entry.WorkDate<new DateTime(DateTime.Today.Year,DateTime.Today.Month,1))return BadRequest(new { error="Previous months are locked. Contact an administrator for a correction." });
        Db.TimeEntries.Remove(entry);
        audit.Add(actor,"Deleted time entry","TimeEntry",entry.Id,$"Deleted {entry.WorkItemNumber}, {entry.WorkDate:yyyy-MM-dd}, {entry.ActualHours:0.##}h.");
        await Db.SaveChangesAsync();
        return Ok(new { ok=true });
    }

    private async Task<bool> CanEdit(AppUser actor,WeekSubmission submission)
    {
        if(actor.Id==submission.UserId||(await Role(actor))==AtlasRoles.Administrator)return true;
        if((await Role(actor))==AtlasRoles.Lead)
        {
            var target=await Db.Users.FirstOrDefaultAsync(x=>x.Id==submission.UserId);
            return target?.TeamId==actor.TeamId;
        }
        return false;
    }

    private static TimeEntry FromRequest(EntryWrite r,string submissionId,ProjectService project,WorkItem work,TaskCatalog task,DateTime date)
    {
        var entry=new TimeEntry { SubmissionId=submissionId,RowKey=string.IsNullOrWhiteSpace(r.RowKey)?Guid.NewGuid().ToString("N"):r.RowKey!,WorkDate=date.Date };
        CopyRequest(entry,r,project,work,task,date);return entry;
    }

    private static void CopyRequest(TimeEntry entry,EntryWrite r,ProjectService project,WorkItem work,TaskCatalog task,DateTime date)
    {
        entry.WorkDate=date.Date;entry.CommercialModel=r.CommercialModel;entry.ActivityClassification=task.ActivityClassification;entry.TaskCatalogId=task.Id;entry.TaskName=task.Name;entry.Track=task.Track;entry.Activity=task.Activity;entry.Phase=task.Phase;
        entry.WorkItemId=work.Id;entry.WorkItemNumber=work.Number;entry.WorkItemType=work.Type;entry.WorkItemTitle=work.Title;
        entry.ProjectServiceId=project.Id;entry.ProjectService=project.Name;entry.Release=Text(r.Release,40);entry.Sprint=Text(r.Sprint,80);
        entry.Environment=Text(r.Environment,40);entry.ActualHours=r.Hours;entry.Realized=r.Realized;entry.Comments=Text(r.Comments,500);entry.ExceptionReason=Text(r.ExceptionReason,200);
    }
    private static string Text(string? value,int max)=>string.IsNullOrWhiteSpace(value)?"":value.Trim()[..Math.Min(value.Trim().Length,max)];
    private static readonly string[] Models=["Time & Materials","Fixed Price","Managed Services","Internal / Non-Billable","Retainer","Other"];
}
