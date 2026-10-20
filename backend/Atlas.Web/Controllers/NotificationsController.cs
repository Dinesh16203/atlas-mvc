using Atlas.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Web.Controllers;

[Route("api/notifications")]
public sealed class NotificationsController(UserManager<AppUser> users,AtlasDbContext db) : ApiControllerBase(users,db)
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var actor=await Actor();var list=await Db.Notifications.Where(n=>n.UserId==actor.Id).OrderByDescending(n=>n.CreatedAt).Take(50).ToListAsync();
        return Ok(new { notifications=list });
    }
    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkRead(string id)
    {
        var actor=await Actor();var n=await Db.Notifications.FirstOrDefaultAsync(x=>x.Id==id&&x.UserId==actor.Id);
        if(n is null)return NotFound();n.Read=true;await Db.SaveChangesAsync();return Ok(new { ok=true });
    }
    [HttpPost("remind/{submissionId}")]
    public async Task<IActionResult> Remind(string submissionId)
    {
        var actor=await Actor();var role=await Role(actor);var s=await Db.Submissions.FirstOrDefaultAsync(x=>x.Id==submissionId);
        if(s is null)return NotFound(new { error="Timesheet not found." });
        if(role is not (AtlasRoles.Lead or AtlasRoles.Administrator or AtlasRoles.DomainOwner))return Forbid();
        if(!await CanSee(actor,s))return Forbid();
        if(s.Status is "Submitted" or "Approved")return BadRequest(new { error="This timesheet does not need a submission reminder." });
        Db.Notifications.Add(new AppNotification { UserId=s.UserId,Title="Reminder: complete your weekly time",Message=$"{actor.DisplayName} reminded you to complete the week starting {s.WeekStart:MMM d}.",Kind="Reminder" });
        Db.AuditEvents.Add(new AuditEvent { ActorId=actor.Id,ActorName=actor.DisplayName,Action="Sent in-app timesheet reminder",TargetType="Submission",TargetId=s.Id });
        await Db.SaveChangesAsync();return Ok(new { ok=true });
    }
}
