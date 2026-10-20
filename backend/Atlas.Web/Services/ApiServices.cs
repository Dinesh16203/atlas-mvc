using Atlas.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Web.Services;

public sealed class AuditWriter(AtlasDbContext db)
{
    public void Add(AppUser actor, string action, string targetType, string targetId, string details="") =>
        db.AuditEvents.Add(new AuditEvent { ActorId=actor.Id, ActorName=actor.DisplayName, Action=action,
            TargetType=targetType, TargetId=targetId, Details=details, CreatedAt=DateTime.UtcNow });
}

public sealed class SubmissionMapper(AtlasDbContext db)
{
    public async Task<SubmissionDto> MapAsync(WeekSubmission s)
    {
        var user=await db.Users.FirstOrDefaultAsync(x=>x.Id==s.UserId);
        var team=user?.TeamId is null?null:await db.Teams.FirstOrDefaultAsync(x=>x.Id==user.TeamId);
        var domain=user?.DomainId is null?null:await db.Domains.FirstOrDefaultAsync(x=>x.Id==user.DomainId);
        var reviewer=s.ReviewerId is null?null:await db.Users.FirstOrDefaultAsync(x=>x.Id==s.ReviewerId);
        var entries=await db.TimeEntries.Where(x=>x.SubmissionId==s.Id).OrderBy(x=>x.WorkDate).ThenBy(x=>x.RowKey).ToListAsync();
        var comments=await db.SubmissionComments.Where(x=>x.SubmissionId==s.Id).OrderBy(x=>x.CreatedAt).ToListAsync();
        var decisions=await db.SubmissionDecisions.Where(x=>x.SubmissionId==s.Id).OrderBy(x=>x.CreatedAt).ToListAsync();
        var total=entries.Sum(x=>x.ActualHours);
        var exceptions=entries.Count(x=>x.ExceptionReason.Length>0)+ (total>s.RequiredCapacity?1:0);
        return new SubmissionDto(s.Id,s.UserId,user?.DisplayName??"Unknown",user?.Email??"",team?.Name??"",domain?.Name??"",
            s.WeekStart.ToString("yyyy-MM-dd"),s.Status,reviewer?.DisplayName??"Unassigned",s.RequiredCapacity,total,
            TimeEntryRules.Variance(total,s.RequiredCapacity),exceptions,s.SubmittedAt,s.DecisionReason??"",
            entries.Select(ToDto).ToList(),comments.Select(x=>new CommentDto(x.Id,x.UserName,x.Text,x.CreatedAt)).ToList(),
            decisions.Select(x=>new DecisionDto(x.ActorName,x.Decision,x.Reason,x.CreatedAt)).ToList());
    }

    public static EntryDto ToDto(TimeEntry e)=>new(e.Id,e.RowKey,e.SubmissionId,e.WorkDate.ToString("yyyy-MM-dd"),e.CommercialModel,
        e.Track,e.Activity,e.Phase,e.TaskCatalogId,e.TaskName,e.ActivityClassification,e.WorkItemId,e.WorkItemNumber,e.WorkItemType,e.WorkItemTitle,e.ProjectServiceId,e.ProjectService,
        e.Release,e.Sprint,e.Environment,e.ActualHours,e.Realized,e.Comments,e.ExceptionReason);
}
