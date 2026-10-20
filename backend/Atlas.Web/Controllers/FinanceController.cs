using Atlas.Web.Data;
using Atlas.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Web.Controllers;

[Route("api/finance")]
public sealed class FinanceController(UserManager<AppUser> users,AtlasDbContext db,AuditWriter audit) : ApiControllerBase(users,db)
{
    [Authorize(Roles=AtlasRoles.Administrator+","+AtlasRoles.Management)]
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery]string? start=null,[FromQuery]string? end=null)
    {
        var actor=await Actor();var role=await Role(actor);var q=Db.FinanceRecords.AsQueryable();
        if(role==AtlasRoles.Member||role==AtlasRoles.Lead)q=q.Where(x=>x.DomainId==actor.DomainId);
        else if(role==AtlasRoles.DomainOwner)q=q.Where(x=>x.DomainId==actor.DomainId);
        if(DateTime.TryParse(start,out var from))q=q.Where(x=>x.PeriodStart>=new DateTime(from.Year,from.Month,1));
        if(DateTime.TryParse(end,out var to))q=q.Where(x=>x.PeriodStart<new DateTime(to.Year,to.Month,1).AddMonths(1));
        return Ok(new { records=await q.OrderByDescending(x=>x.PeriodStart).ToListAsync() });
    }

    [Authorize(Roles=AtlasRoles.Administrator)]
    [HttpPost]
    public async Task<IActionResult> Create(FinanceWrite request)
    {
        if(!DateTime.TryParse(request.PeriodStart,out var period)||request.InvoiceHours<0||request.InvoicedMds<0)return BadRequest(new { error="Choose a period and enter non-negative invoice hours and MDs." });
        var project=await Db.Projects.FirstOrDefaultAsync(x=>x.Id==request.ProjectServiceId);if(project is null)return BadRequest(new { error="Choose a project or service." });
        var month=new DateTime(period.Year,period.Month,1);
        if(await Db.FinanceRecords.AnyAsync(x=>x.PeriodStart==month&&x.ProjectServiceId==project.Id))return Conflict(new { error="This project already has an invoice record for that month." });
        var row=FromRequest(request,project,month);Db.FinanceRecords.Add(row);audit.Add(await Actor(),"Created invoice record","FinanceRecord",row.Id,row.BillId);await Db.SaveChangesAsync();return Created($"/api/finance/{row.Id}",row);
    }

    [Authorize(Roles=AtlasRoles.Administrator)]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id,FinanceWrite request)
    {
        var row=await Db.FinanceRecords.FirstOrDefaultAsync(x=>x.Id==id);if(row is null)return NotFound();
        if(!DateTime.TryParse(request.PeriodStart,out var period)||request.InvoiceHours<0||request.InvoicedMds<0)return BadRequest(new { error="Choose a period and enter non-negative invoice hours and MDs." });
        var project=await Db.Projects.FirstOrDefaultAsync(x=>x.Id==request.ProjectServiceId);if(project is null)return BadRequest(new { error="Choose a project or service." });
        row=FromRequest(request,project,new DateTime(period.Year,period.Month,1),row);audit.Add(await Actor(),"Updated invoice record","FinanceRecord",row.Id,row.BillId);await Db.SaveChangesAsync();return Ok(row);
    }

    private static FinanceRecord FromRequest(FinanceWrite r,ProjectService p,DateTime month,FinanceRecord? target=null)
    {
        target??=new FinanceRecord();target.PeriodStart=month;target.ProjectServiceId=p.Id;target.ProjectService=p.Name;target.DomainId=p.DomainId;target.CostCenter=p.CostCenter;target.Contract=p.Contract;target.BillId=p.BillId;
        target.BilledVia=r.BilledVia;target.BillingEntity=r.BillingEntity;target.BillingCategory=r.BillingCategory;target.InvoiceType=r.InvoiceType;target.InvoiceRule=r.InvoiceRule;target.InvoiceHours=r.InvoiceHours;target.InvoicedMds=r.InvoicedMds;target.Approved=r.Approved;return target;
    }
}
