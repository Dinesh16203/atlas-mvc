using Atlas.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.Web.Controllers;

[Route("api/auth")]
public sealed class AuthController(UserManager<AppUser> users, SignInManager<AppUser> signIn, AtlasDbContext db) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        if(User.Identity?.IsAuthenticated!=true)return Unauthorized(new { error="Please sign in." });
        var user=await users.GetUserAsync(User);
        if(user is null||!user.IsActive)return Unauthorized(new { error="This account is inactive." });
        return Ok(new { user=await ToDto(user) });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest? request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Email and password are required." });

        var user=await users.FindByEmailAsync(request.Email.Trim());
        if(user is null||!user.IsActive)return Unauthorized(new { error="Email or password is incorrect." });
        var result=await signIn.CheckPasswordSignInAsync(user,request.Password,false);
        if(!result.Succeeded)return Unauthorized(new { error="Email or password is incorrect." });
        await signIn.SignInAsync(user,isPersistent:false);
        db.AuditEvents.Add(new AuditEvent { ActorId=user.Id,ActorName=user.DisplayName,Action="Signed in",TargetType="Session",TargetId=user.Id });
        await db.SaveChangesAsync();
        return Ok(new { user=await ToDto(user) });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var user=await users.GetUserAsync(User);
        if(user is not null){db.AuditEvents.Add(new AuditEvent { ActorId=user.Id,ActorName=user.DisplayName,Action="Signed out",TargetType="Session",TargetId=user.Id });await db.SaveChangesAsync();}
        await signIn.SignOutAsync();
        return Ok(new { ok=true });
    }

    private async Task<UserDto> ToDto(AppUser user)
    {
        var roles=await users.GetRolesAsync(user);
        var team=user.TeamId is null?null:db.Teams.FirstOrDefault(x=>x.Id==user.TeamId);
        var domain=user.DomainId is null?null:db.Domains.FirstOrDefault(x=>x.Id==user.DomainId);
        return new UserDto(user.Id,user.DisplayName,user.Email??"",roles.FirstOrDefault()??AtlasRoles.Member,team?.Name??"",domain?.Name??"",user.CostCenter);
    }
}
