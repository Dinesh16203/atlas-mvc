using Atlas.Web.Data;
using Atlas.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Web.Controllers;

[Route("api/lookups")]
public sealed class LookupsController(UserManager<AppUser> users,AtlasDbContext db) : ApiControllerBase(users,db)
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var actorUser=await Actor();
        var role=await Role(actorUser);
        var userQuery=Db.Users.Where(u=>u.IsActive);
        if(role==AtlasRoles.Member)userQuery=userQuery.Where(u=>u.Id==actorUser.Id);
        else if(role==AtlasRoles.Lead)userQuery=userQuery.Where(u=>u.TeamId==actorUser.TeamId);
        else if(role==AtlasRoles.DomainOwner)userQuery=userQuery.Where(u=>u.DomainId==actorUser.DomainId);
        var usersList=await userQuery.OrderBy(u=>u.DisplayName).ToListAsync();
        var dtos=new List<UserDto>();
        foreach(var u in usersList)dtos.Add(await UserDto(u));
        var teamQuery=Db.Teams.Where(x=>x.Active);
        var projectQuery=Db.Projects.Where(x=>x.Active);
        if(role is not (AtlasRoles.Administrator or AtlasRoles.Management))
        {
            teamQuery=teamQuery.Where(x=>x.DomainId==actorUser.DomainId);
            projectQuery=projectQuery.Where(x=>x.DomainId==actorUser.DomainId);
        }
        if(role is not (AtlasRoles.Administrator or AtlasRoles.Management))
        {
            var projectIds=projectQuery.Select(p=>p.Id);
            var workItems=await Db.WorkItems.Where(x=>x.Active&&projectIds.Contains(x.ProjectServiceId)).OrderBy(x=>x.Number).ToListAsync();
            var taskQuery=Db.TaskCatalog.Where(x=>x.Active&&(x.DomainId==null||x.DomainId==actorUser.DomainId));
            if(role is AtlasRoles.Member or AtlasRoles.Lead)taskQuery=taskQuery.Where(x=>x.TeamId==null||x.TeamId==actorUser.TeamId);
            var tasks=await taskQuery.OrderBy(x=>x.TaskId).ToListAsync();
            return Ok(new LookupsDto(await Db.Domains.Where(x=>x.Active).OrderBy(x=>x.Name).ToListAsync(),
                await teamQuery.OrderBy(x=>x.Name).ToListAsync(),await projectQuery.OrderBy(x=>x.Name).ToListAsync(),workItems,tasks,dtos,
                role==AtlasRoles.Administrator?AtlasRoles.All:Array.Empty<string>()));
        }
        return Ok(new LookupsDto(await Db.Domains.Where(x=>x.Active).OrderBy(x=>x.Name).ToListAsync(),
            await teamQuery.OrderBy(x=>x.Name).ToListAsync(),
            await projectQuery.OrderBy(x=>x.Name).ToListAsync(),
            await Db.WorkItems.Where(x=>x.Active).OrderBy(x=>x.Number).ToListAsync(),
            await Db.TaskCatalog.Where(x=>x.Active).OrderBy(x=>x.TaskId).ToListAsync(),dtos,role==AtlasRoles.Administrator?AtlasRoles.All:Array.Empty<string>()));
    }
}
