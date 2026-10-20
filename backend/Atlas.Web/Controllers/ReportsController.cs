using Atlas.Web.Data;
using Atlas.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Web.Controllers;

[Route("api/reports")]
public sealed class ReportsController(UserManager<AppUser> users,AtlasDbContext db,SubmissionMapper mapper) : ApiControllerBase(users,db)
{
    [HttpGet("overview")]
    public async Task<IActionResult> Overview([FromQuery]string? start=null,[FromQuery]string? end=null,[FromQuery]string? team=null,
        [FromQuery]string? domain=null,[FromQuery]string? employee=null,[FromQuery]string? project=null,[FromQuery]string? commercialModel=null,[FromQuery]string? status=null)
    {
        var actor=await Actor();var q=await ScopedSubmissions(actor);
        if(!string.IsNullOrWhiteSpace(status))q=q.Where(s=>s.Status==status);
        if(!string.IsNullOrWhiteSpace(employee))q=q.Where(s=>s.UserId==employee);
        if(!string.IsNullOrWhiteSpace(team)){var ids=Db.Users.Where(u=>u.TeamId==team).Select(u=>u.Id);q=q.Where(s=>ids.Contains(s.UserId));}
        if(!string.IsNullOrWhiteSpace(domain)){var ids=Db.Users.Where(u=>u.DomainId==domain).Select(u=>u.Id);q=q.Where(s=>ids.Contains(s.UserId));}
        var submissionRows=await q.OrderByDescending(s=>s.WeekStart).Take(300).ToListAsync();
        var subById=submissionRows.ToDictionary(s=>s.Id);
        var scopedIds=submissionRows.Select(s=>s.Id).ToArray();
        var entriesQuery=Db.TimeEntries.Where(e=>scopedIds.Contains(e.SubmissionId));
        if(DateTime.TryParse(start,out var from))entriesQuery=entriesQuery.Where(e=>e.WorkDate>=from.Date);
        if(DateTime.TryParse(end,out var to))entriesQuery=entriesQuery.Where(e=>e.WorkDate<to.Date.AddDays(1));
        if(!string.IsNullOrWhiteSpace(project))entriesQuery=entriesQuery.Where(e=>e.ProjectServiceId==project);
        if(!string.IsNullOrWhiteSpace(commercialModel))entriesQuery=entriesQuery.Where(e=>e.CommercialModel==commercialModel);
        var entries=await entriesQuery.OrderByDescending(e=>e.WorkDate).Take(8000).ToListAsync();
        var report=new List<ReportEntryDto>();
        var userIds=submissionRows.Select(s=>s.UserId).Distinct().ToArray();
        var reviewerIds=submissionRows.Where(s=>s.ReviewerId!=null).Select(s=>s.ReviewerId!).Distinct().ToArray();
        var userLookup=await Db.Users.Where(x=>userIds.Contains(x.Id)).ToDictionaryAsync(x=>x.Id);
        var reviewerLookup=await Db.Users.Where(x=>reviewerIds.Contains(x.Id)).ToDictionaryAsync(x=>x.Id);
        var teamIds=userLookup.Values.Where(x=>x.TeamId!=null).Select(x=>x.TeamId!).Distinct().ToArray();
        var domainIds=userLookup.Values.Where(x=>x.DomainId!=null).Select(x=>x.DomainId!).Distinct().ToArray();
        var projectIds=entries.Select(x=>x.ProjectServiceId).Distinct().ToArray();
        var teamLookup=await Db.Teams.Where(x=>teamIds.Contains(x.Id)).ToDictionaryAsync(x=>x.Id);
        var domainLookup=await Db.Domains.Where(x=>domainIds.Contains(x.Id)).ToDictionaryAsync(x=>x.Id);
        var projectLookup=await Db.Projects.Where(x=>projectIds.Contains(x.Id)).ToDictionaryAsync(x=>x.Id);
        foreach(var e in entries)
        {
            var s=subById[e.SubmissionId];userLookup.TryGetValue(s.UserId,out var u);
            var teamEntity=u?.TeamId is not null&&teamLookup.TryGetValue(u.TeamId,out var t)?t:null;
            var domainEntity=u?.DomainId is not null&&domainLookup.TryGetValue(u.DomainId,out var d)?d:null;
            projectLookup.TryGetValue(e.ProjectServiceId,out var p);
            var reviewer=s.ReviewerId is not null&&reviewerLookup.TryGetValue(s.ReviewerId,out var rv)?rv:null;
            report.Add(new ReportEntryDto(SubmissionMapper.ToDto(e),u?.DisplayName??"Unknown",u?.Email??"",teamEntity?.Name??"",domainEntity?.Name??"",s.Status,reviewer?.DisplayName??"Unassigned",u?.CostCenter??"",p?.BillingTreatment??""));
        }
        var summaries=new List<SubmissionDto>();foreach(var s in submissionRows.Take(150))summaries.Add(await mapper.MapAsync(s));
        return Ok(new { entries=report,submissions=summaries });
    }

    [HttpGet("compliance")]
    public async Task<IActionResult> Compliance([FromQuery]string? week=null)
    {
        var actor=await Actor();var role=await Role(actor);
        var q=Db.Users.Where(u=>u.IsActive);
        if(role==AtlasRoles.Member)q=q.Where(u=>u.Id==actor.Id);
        else if(role==AtlasRoles.Lead)q=q.Where(u=>u.TeamId==actor.TeamId);
        else if(role==AtlasRoles.DomainOwner)q=q.Where(u=>u.DomainId==actor.DomainId);
        var people=await q.OrderBy(u=>u.DisplayName).ToListAsync();
        var monday=DateTime.TryParse(week,out var d)?DatabaseInitializer.WeekStart(d):DatabaseInitializer.WeekStart(DateTime.Today);
        var rows=new List<object>();
        foreach(var person in people)
        {
            var s=await Db.Submissions.FirstOrDefaultAsync(x=>x.UserId==person.Id&&x.WeekStart==monday);
            var entries=s is null?[]:await Db.TimeEntries.Where(x=>x.SubmissionId==s.Id).ToArrayAsync();
            var total=entries.Sum(x=>x.ActualHours);var team=person.TeamId is null?null:await Db.Teams.FirstOrDefaultAsync(x=>x.Id==person.TeamId);
            rows.Add(new { userId=person.Id,name=person.DisplayName,email=person.Email,team=team?.Name??"",weekStart=monday.ToString("yyyy-MM-dd"),status=s?.Status??"Missing",hours=total,capacity=s?.RequiredCapacity??40,submissionId=s?.Id });
        }
        return Ok(new { weekStart=monday.ToString("yyyy-MM-dd"),people=rows });
    }
}
