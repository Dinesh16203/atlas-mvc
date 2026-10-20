using Atlas.Web.Data;
using Atlas.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Web.Controllers;

[Route("api/submissions")]
public sealed class SubmissionsController(UserManager<AppUser> users,AtlasDbContext db,SubmissionMapper mapper,AuditWriter audit) : ApiControllerBase(users,db)
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery]string? week=null,[FromQuery]string? status=null)
    {
        var actor=await Actor();var q=await ScopedSubmissions(actor);
        if(DateTime.TryParse(week,out var w)){var monday=DatabaseInitializer.WeekStart(w);q=q.Where(x=>x.WeekStart==monday);}
        if(!string.IsNullOrWhiteSpace(status))q=q.Where(x=>x.Status==status);
        var rows=await q.OrderByDescending(x=>x.WeekStart).ThenBy(x=>x.Status).Take(150).ToListAsync();
        var result=new List<SubmissionDto>();foreach(var row in rows)result.Add(await mapper.MapAsync(row));
        return Ok(new { submissions=result });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOne(string id)
    {
        var actor=await Actor();var submission=await Db.Submissions.FirstOrDefaultAsync(x=>x.Id==id);
        if(submission is null)return NotFound(new { error="Submission not found." });
        if(!await CanSee(actor,submission))return Forbid();
        return Ok(new { submission=await mapper.MapAsync(submission) });
    }

    [HttpPost("{id}/submit")]
    public async Task<IActionResult> Submit(string id)
    {
        var actor=await Actor();var submission=await Db.Submissions.FirstOrDefaultAsync(x=>x.Id==id);
        if(submission is null)return NotFound(new { error="Timesheet not found." });
        var submitterRole=await Role(actor);
        if(actor.Id!=submission.UserId&&submitterRole!=AtlasRoles.Administrator)
        {
            var submitterTarget=await Db.Users.FirstOrDefaultAsync(x=>x.Id==submission.UserId);
            if(submitterRole!=AtlasRoles.Lead||submitterTarget?.TeamId!=actor.TeamId)return Forbid();
        }
        if(submission.Status is not ("Draft" or "Returned" or "Clarification Requested"))return BadRequest(new { error="This timesheet is not in an editable state." });
        var entries=await Db.TimeEntries.Where(x=>x.SubmissionId==id).ToListAsync();
        if(entries.Count==0)return BadRequest(new { error="Add at least one time entry before submitting." });
        var total=entries.Sum(x=>x.ActualHours);
        if(total>submission.RequiredCapacity+0.0001&&entries.All(x=>string.IsNullOrWhiteSpace(x.ExceptionReason)))
            return BadRequest(new { error="The week exceeds required capacity. Add an exception reason to the entry before submitting." });
        submission.Status="Submitted";submission.SubmittedAt=DateTime.UtcNow;submission.DecisionReason="";
        var lead=submission.ReviewerId is null?null:await Db.Users.FirstOrDefaultAsync(x=>x.Id==submission.ReviewerId);
        if(lead is not null)Db.Notifications.Add(new AppNotification { UserId=lead.Id,Title="Timesheet submitted for review",Message=$"{(await Db.Users.FirstAsync(x=>x.Id==submission.UserId)).DisplayName} submitted the week of {submission.WeekStart:MMM d}.",Kind="Approval" });
        Db.SubmissionDecisions.Add(new SubmissionDecision { SubmissionId=id,ActorId=actor.Id,ActorName=actor.DisplayName,Decision="Submitted",Reason="Submitted for review." });
        audit.Add(actor,"Submitted timesheet","Submission",submission.Id,$"Week starting {submission.WeekStart:yyyy-MM-dd}, {total:0.##} hours.");
        await Db.SaveChangesAsync();
        return Ok(new { submission=await mapper.MapAsync(submission) });
    }

    [HttpPost("{id}/withdraw")]
    public async Task<IActionResult> Withdraw(string id)
    {
        var actor=await Actor();var submission=await Db.Submissions.FirstOrDefaultAsync(x=>x.Id==id);
        if(submission is null)return NotFound(new { error="Timesheet not found." });
        if(actor.Id!=submission.UserId)return Forbid();
        if(submission.Status!="Submitted"&&submission.Status!="Clarification Requested")return BadRequest(new { error="Only submitted timesheets can be withdrawn." });
        submission.Status="Draft";submission.SubmittedAt=null;
        Db.SubmissionDecisions.Add(new SubmissionDecision { SubmissionId=id,ActorId=actor.Id,ActorName=actor.DisplayName,Decision="Withdrawn",Reason="Withdrawn for correction." });
        audit.Add(actor,"Withdrew timesheet","Submission",id);
        await Db.SaveChangesAsync();return Ok(new { submission=await mapper.MapAsync(submission) });
    }

    [HttpPost("{id}/decision")]
    public async Task<IActionResult> Decide(string id,DecisionRequest request)
    {
        var actor=await Actor();var role=await Role(actor);
        if(role is not (AtlasRoles.Lead or AtlasRoles.DomainOwner or AtlasRoles.Administrator or AtlasRoles.Management))return Forbid();
        var submission=await Db.Submissions.FirstOrDefaultAsync(x=>x.Id==id);
        if(submission is null)return NotFound(new { error="Timesheet not found." });
        if(!await CanSee(actor,submission))return Forbid();
        if(actor.Id==submission.UserId)return BadRequest(new { error="Self-approval is not allowed. Assign another reviewer." });
        if(submission.Status!="Submitted"&&submission.Status!="Clarification Requested")return BadRequest(new { error="Only submitted timesheets can be reviewed." });
        var decision=request.Decision.Trim();
        var allowed=new[]{"Approve","Reject","Return for Correction","Request Clarification"};
        if(!allowed.Contains(decision))return BadRequest(new { error="Choose a valid review action." });
        if(string.IsNullOrWhiteSpace(request.Reason))return BadRequest(new { error="A decision reason is required for the audit trail." });
        submission.Status=decision switch { "Approve"=>"Approved","Reject"=>"Rejected","Return for Correction"=>"Returned",_=>"Clarification Requested" };
        submission.DecidedAt=DateTime.UtcNow;submission.DecisionReason=request.Reason.Trim()[..Math.Min(request.Reason.Trim().Length,500)];
        Db.SubmissionDecisions.Add(new SubmissionDecision { SubmissionId=id,ActorId=actor.Id,ActorName=actor.DisplayName,Decision=decision,Reason=submission.DecisionReason });
        var employee=await Db.Users.FirstAsync(x=>x.Id==submission.UserId);
        Db.Notifications.Add(new AppNotification { UserId=employee.Id,Title=$"Timesheet {submission.Status.ToLowerInvariant()}",Message=$"Your week starting {submission.WeekStart:MMM d} was {submission.Status.ToLowerInvariant()}: {submission.DecisionReason}",Kind=decision=="Approve"?"Success":"Action" });
        audit.Add(actor,$"{decision} timesheet","Submission",id,submission.DecisionReason);
        await Db.SaveChangesAsync();return Ok(new { submission=await mapper.MapAsync(submission) });
    }

    [HttpPost("{id}/comments")]
    public async Task<IActionResult> Comment(string id,CommentRequest request)
    {
        var actor=await Actor();var submission=await Db.Submissions.FirstOrDefaultAsync(x=>x.Id==id);
        if(submission is null)return NotFound(new { error="Timesheet not found." });
        if(!await CanSee(actor,submission))return Forbid();
        var text=request.Text.Trim();if(text.Length==0||text.Length>500)return BadRequest(new { error="Enter a comment with up to 500 characters." });
        Db.SubmissionComments.Add(new SubmissionComment { SubmissionId=id,UserId=actor.Id,UserName=actor.DisplayName,Text=text });
        audit.Add(actor,"Commented on timesheet","Submission",id,text);
        await Db.SaveChangesAsync();return Ok(new { submission=await mapper.MapAsync(submission) });
    }
}
