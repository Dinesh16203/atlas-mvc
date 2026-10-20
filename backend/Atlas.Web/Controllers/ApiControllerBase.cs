using Atlas.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Web.Controllers;

[ApiController]
[Authorize]
public abstract class ApiControllerBase(UserManager<AppUser> userManager, AtlasDbContext db) : ControllerBase
{
    protected AtlasDbContext Db=>db;
    protected UserManager<AppUser> UserManager=>userManager;
    protected async Task<AppUser> Actor()=>await userManager.GetUserAsync(User) ?? throw new UnauthorizedAccessException();
    protected async Task<string> Role(AppUser user)=> (await userManager.GetRolesAsync(user)).FirstOrDefault()??AtlasRoles.Member;
    protected async Task<UserDto> UserDto(AppUser user)
    {
        var team=user.TeamId is null?null:await db.Teams.FirstOrDefaultAsync(x=>x.Id==user.TeamId);
        var domain=user.DomainId is null?null:await db.Domains.FirstOrDefaultAsync(x=>x.Id==user.DomainId);
        return new UserDto(user.Id,user.DisplayName,user.Email??"",await Role(user),team?.Name??"",domain?.Name??"",user.CostCenter);
    }
    protected async Task<bool> CanSee(AppUser actor, WeekSubmission submission)
    {
        if(actor.Id==submission.UserId)return true;
        var role=await Role(actor);
        if(role is AtlasRoles.Administrator or AtlasRoles.Management)return true;
        var target=await db.Users.FirstOrDefaultAsync(x=>x.Id==submission.UserId);
        return target is not null && (role==AtlasRoles.Lead&&actor.TeamId==target.TeamId || role==AtlasRoles.DomainOwner&&actor.DomainId==target.DomainId);
    }
    protected async Task<IQueryable<WeekSubmission>> ScopedSubmissions(AppUser actor)
    {
        var role=await Role(actor);
        IQueryable<WeekSubmission> q=db.Submissions;
        if(role is AtlasRoles.Administrator or AtlasRoles.Management)return q;
        if(role==AtlasRoles.Lead){var teamUserIds=db.Users.Where(u=>u.TeamId==actor.TeamId).Select(u=>u.Id);return q.Where(s=>teamUserIds.Contains(s.UserId));}
        if(role==AtlasRoles.DomainOwner){var domainUserIds=db.Users.Where(u=>u.DomainId==actor.DomainId).Select(u=>u.Id);return q.Where(s=>domainUserIds.Contains(s.UserId));}
        return q.Where(s=>s.UserId==actor.Id);
    }
}
