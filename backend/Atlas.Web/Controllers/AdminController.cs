using Atlas.Web.Data;
using Atlas.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Web.Controllers;

[Route("api/admin")]
[Authorize(Roles=AtlasRoles.Administrator)]
public sealed class AdminController(UserManager<AppUser> users,AtlasDbContext db,AuditWriter audit) : ApiControllerBase(users,db)
{
    [HttpGet("users")]
    public async Task<IActionResult> Users()
    {
        var all=await Db.Users.OrderBy(x=>x.DisplayName).ToListAsync();var result=new List<object>();
        foreach(var u in all)result.Add(new { user=await UserDto(u), active=u.IsActive, managerId=u.ManagerId, teamId=u.TeamId, domainId=u.DomainId });
        return Ok(new { users=result });
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser(UserCreateRequest request)
    {
        if(string.IsNullOrWhiteSpace(request.Name)||!roles.Contains(request.Role))return BadRequest(new { error="Enter a name and choose a valid role." });
        if(!await Db.Teams.AnyAsync(x=>x.Id==request.TeamId&&x.Active)||!await Db.Domains.AnyAsync(x=>x.Id==request.DomainId&&x.Active))return BadRequest(new { error="Choose an active team and domain." });
        var user=new AppUser { UserName=request.Email.Trim().ToLowerInvariant(),Email=request.Email.Trim().ToLowerInvariant(),EmailConfirmed=true,
            DisplayName=request.Name.Trim(),TeamId=request.TeamId,DomainId=request.DomainId,ManagerId=request.ManagerId,CostCenter=request.CostCenter?.Trim()??"" };
        var result=await UserManager.CreateAsync(user,request.Password);
        if(!result.Succeeded)return BadRequest(new { error=string.Join(" ",result.Errors.Select(x=>x.Description)) });
        await UserManager.AddToRoleAsync(user,request.Role);
        var actor=await Actor();audit.Add(actor,"Created user","User",user.Id,$"{user.Email}, role {request.Role}");
        await Db.SaveChangesAsync();return Created($"/api/admin/users/{user.Id}",new { user=await UserDto(user),active=user.IsActive });
    }

    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser(string id,UserUpdateRequest request)
    {
        var user=await UserManager.FindByIdAsync(id);if(user is null)return NotFound(new { error="User not found." });
        var actor=await Actor();var changes=new List<string>();
        if(request.Name is not null){user.DisplayName=request.Name.Trim();changes.Add("name");}
        if(request.TeamId is not null){if(!await Db.Teams.AnyAsync(x=>x.Id==request.TeamId&&x.Active))return BadRequest(new { error="Choose an active team." });user.TeamId=request.TeamId;changes.Add("team");}
        if(request.DomainId is not null){if(!await Db.Domains.AnyAsync(x=>x.Id==request.DomainId&&x.Active))return BadRequest(new { error="Choose an active domain." });user.DomainId=request.DomainId;changes.Add("domain");}
        if(request.ManagerId is not null){user.ManagerId=request.ManagerId;changes.Add("manager");}
        if(request.CostCenter is not null){user.CostCenter=request.CostCenter.Trim();changes.Add("cost center");}
        if(request.Active is not null){user.IsActive=request.Active.Value;changes.Add(user.IsActive?"activated":"deactivated");}
        if(request.Role is not null)
        {
            if(!roles.Contains(request.Role))return BadRequest(new { error="Unknown role." });
            var prior=await UserManager.GetRolesAsync(user);if(prior.Count>0)await UserManager.RemoveFromRolesAsync(user,prior);
            await UserManager.AddToRoleAsync(user,request.Role);changes.Add("role");
        }
        audit.Add(actor,"Updated user","User",id,string.Join(", ",changes));await Db.SaveChangesAsync();
        return Ok(new { user=await UserDto(user),active=user.IsActive });
    }

    [HttpPost("domains")]
    public async Task<IActionResult> CreateDomain(DomainWrite request)
    {
        if(string.IsNullOrWhiteSpace(request.Name))return BadRequest(new { error="Domain name is required." });
        if(await Db.Domains.AnyAsync(x=>x.Name.ToLower()==request.Name.Trim().ToLower()))return Conflict(new { error="That domain already exists." });
        var d=new Domain { Name=request.Name.Trim(),Active=request.Active??true };Db.Domains.Add(d);audit.Add(await Actor(),"Created domain","Domain",d.Id,d.Name);await Db.SaveChangesAsync();return Created($"/api/admin/domains/{d.Id}",d);
    }

    [HttpPut("domains/{id}")]
    public async Task<IActionResult> UpdateDomain(string id,DomainWrite request)
    {
        var d=await Db.Domains.FirstOrDefaultAsync(x=>x.Id==id);if(d is null)return NotFound();
        if(!string.IsNullOrWhiteSpace(request.Name))d.Name=request.Name.Trim();if(request.Active is not null)d.Active=request.Active.Value;
        audit.Add(await Actor(),"Updated domain","Domain",id,d.Name);await Db.SaveChangesAsync();return Ok(d);
    }

    [HttpPost("teams")]
    public async Task<IActionResult> CreateTeam(TeamWrite request)
    {
        if(string.IsNullOrWhiteSpace(request.Name)||!await Db.Domains.AnyAsync(x=>x.Id==request.DomainId&&x.Active))return BadRequest(new { error="Enter a team name and select a domain." });
        var t=new Team { Name=request.Name.Trim(),DomainId=request.DomainId,ParentTeamId=request.ParentTeamId,Active=request.Active??true };
        Db.Teams.Add(t);audit.Add(await Actor(),"Created team","Team",t.Id,t.Name);await Db.SaveChangesAsync();return Created($"/api/admin/teams/{t.Id}",t);
    }

    [HttpPut("teams/{id}")]
    public async Task<IActionResult> UpdateTeam(string id,TeamWrite request)
    {
        var t=await Db.Teams.FirstOrDefaultAsync(x=>x.Id==id);if(t is null)return NotFound();
        if(!string.IsNullOrWhiteSpace(request.Name))t.Name=request.Name.Trim();if(!string.IsNullOrWhiteSpace(request.DomainId))t.DomainId=request.DomainId;
        t.ParentTeamId=request.ParentTeamId;if(request.Active is not null)t.Active=request.Active.Value;
        audit.Add(await Actor(),"Updated team","Team",id,t.Name);await Db.SaveChangesAsync();return Ok(t);
    }

    [HttpPost("projects")]
    public async Task<IActionResult> CreateProject(ProjectWrite r)
    {
        if(string.IsNullOrWhiteSpace(r.Name)||!await Db.Domains.AnyAsync(x=>x.Id==r.DomainId))return BadRequest(new { error="Project name and domain are required." });
        var p=new ProjectService { Name=r.Name.Trim(),DomainId=r.DomainId,Client=r.Client??"ATRIAS",Contract=r.Contract??"",BillId=r.BillId??"",CostCenter=r.CostCenter??"",BillingTreatment=r.BillingTreatment??"Billable",CommercialModel=r.CommercialModel??"Time & Materials",Active=r.Active??true };
        Db.Projects.Add(p);audit.Add(await Actor(),"Created project","ProjectService",p.Id,p.Name);await Db.SaveChangesAsync();return Created($"/api/admin/projects/{p.Id}",p);
    }

    [HttpPut("projects/{id}")]
    public async Task<IActionResult> UpdateProject(string id,ProjectWrite r)
    {
        var p=await Db.Projects.FirstOrDefaultAsync(x=>x.Id==id);if(p is null)return NotFound();
        p.Name=r.Name.Trim();p.DomainId=r.DomainId;p.Client=r.Client??"";p.Contract=r.Contract??"";p.BillId=r.BillId??"";p.CostCenter=r.CostCenter??"";p.BillingTreatment=r.BillingTreatment??"Billable";p.CommercialModel=r.CommercialModel??p.CommercialModel;if(r.Active is not null)p.Active=r.Active.Value;
        audit.Add(await Actor(),"Updated project","ProjectService",id,p.Name);await Db.SaveChangesAsync();return Ok(p);
    }

    [HttpPost("work-items")]
    public async Task<IActionResult> CreateWorkItem(WorkItemWrite r)
    {
        if(string.IsNullOrWhiteSpace(r.Number)||string.IsNullOrWhiteSpace(r.Title)||!await Db.Projects.AnyAsync(x=>x.Id==r.ProjectServiceId))return BadRequest(new { error="Work item number, title, and project are required." });
        var w=new WorkItem { Number=r.Number.Trim(),Title=r.Title.Trim(),Type=r.Type,ProjectServiceId=r.ProjectServiceId,ParentWorkItem=r.ParentWorkItem,Release=r.Release??"",Sprint=r.Sprint??"",Iteration=r.Iteration??"",Environment=r.Environment??"DEV",Status=r.Status??"Open",Active=r.Active??true };
        Db.WorkItems.Add(w);audit.Add(await Actor(),"Created work item","WorkItem",w.Id,w.Number);await Db.SaveChangesAsync();return Created($"/api/admin/work-items/{w.Id}",w);
    }

    [HttpPut("work-items/{id}")]
    public async Task<IActionResult> UpdateWorkItem(string id,WorkItemWrite r)
    {
        var w=await Db.WorkItems.FirstOrDefaultAsync(x=>x.Id==id);if(w is null)return NotFound();
        w.Number=r.Number.Trim();w.Title=r.Title.Trim();w.Type=r.Type;w.ProjectServiceId=r.ProjectServiceId;w.ParentWorkItem=r.ParentWorkItem;w.Release=r.Release??"";w.Sprint=r.Sprint??"";w.Iteration=r.Iteration??"";w.Environment=r.Environment??"DEV";w.Status=r.Status??"Open";if(r.Active is not null)w.Active=r.Active.Value;
        audit.Add(await Actor(),"Updated work item","WorkItem",id,w.Number);await Db.SaveChangesAsync();return Ok(w);
    }

    [HttpPost("tasks")]
    public async Task<IActionResult> CreateTask(TaskWrite r)
    {
        if(string.IsNullOrWhiteSpace(r.Name)||string.IsNullOrWhiteSpace(r.TaskId))return BadRequest(new { error="Task ID and name are required." });
        if(!models.Contains(r.CommercialModel)||!classifications.Contains(r.ActivityClassification))return BadRequest(new { error="Select a valid commercial model and classification." });
        var t=new TaskCatalog { TaskId=r.TaskId.Trim(),Name=r.Name.Trim(),Track=r.Track,Activity=r.Activity,Phase=r.Phase,CommercialModel=r.CommercialModel,ActivityClassification=r.ActivityClassification,TeamId=r.TeamId,DomainId=r.DomainId,Active=r.Active??true };
        Db.TaskCatalog.Add(t);audit.Add(await Actor(),"Created catalogue task","TaskCatalog",t.Id,t.TaskId+" "+t.Name);await Db.SaveChangesAsync();return Created($"/api/admin/tasks/{t.Id}",t);
    }

    [HttpPut("tasks/{id}")]
    public async Task<IActionResult> UpdateTask(string id,TaskWrite r)
    {
        var t=await Db.TaskCatalog.FirstOrDefaultAsync(x=>x.Id==id);if(t is null)return NotFound();
        t.TaskId=r.TaskId.Trim();t.Name=r.Name.Trim();t.Track=r.Track;t.Activity=r.Activity;t.Phase=r.Phase;t.CommercialModel=r.CommercialModel;t.ActivityClassification=r.ActivityClassification;t.TeamId=r.TeamId;t.DomainId=r.DomainId;if(r.Active is not null)t.Active=r.Active.Value;
        audit.Add(await Actor(),"Updated catalogue task","TaskCatalog",id,t.TaskId);await Db.SaveChangesAsync();return Ok(t);
    }

    [HttpGet("audit")]
    public async Task<IActionResult> Audit([FromQuery]int limit=100)
    {
        limit=Math.Clamp(limit,1,500);return Ok(new { events=await Db.AuditEvents.OrderByDescending(x=>x.CreatedAt).Take(limit).ToListAsync() });
    }

    [HttpGet("roles")]
    public IActionResult Roles()=>Ok(new { roles=AtlasRoles.All });

    private static readonly string[] roles=AtlasRoles.All;
    private static readonly string[] models=["Time & Materials","Fixed Price","Managed Services","Internal / Non-Billable","Retainer","Other"];
    private static readonly string[] classifications=["T&M","Non-T&M","Out"];
}
