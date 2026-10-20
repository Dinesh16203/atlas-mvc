using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Web.Data;

public static class DatabaseInitializer
{
    private static readonly string[] DomainNames = ["Structure", "Measure", "Bill", "Settle", "CrossProcess", "SDAS", "AX", "Infeed", "DB Developers", "SL", "Hubops", "Infra", "DBA", "BI", "SRE", "Release", "Deployment", "TA & PT", "Data"];

    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();
        await db.Database.EnsureCreatedAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in AtlasRoles.All)
            if (!await roleManager.RoleExistsAsync(role)) await roleManager.CreateAsync(new IdentityRole(role));

        if (await db.Domains.AnyAsync()) return;
        var domainByName = DomainNames.Select(n => new Domain { Name = n }).ToList();
        db.Domains.AddRange(domainByName);
        var structure = domainByName[0];
        var measure = domainByName[1];
        var bill = domainByName[2];
        var teams = new[] {
            new Team { Name = "Digital Delivery", DomainId = structure.Id },
            new Team { Name = "Data Engineering", DomainId = measure.Id },
            new Team { Name = "Service Operations", DomainId = bill.Id },
            new Team { Name = "Platform Reliability", DomainId = structure.Id }
        };
        db.Teams.AddRange(teams);
        await db.SaveChangesAsync();

        var projects = new[] {
            new ProjectService { Name="ATLAS Platform", DomainId=structure.Id, Client="ATRIAS", Contract="CN-2024-0178", BillId="BILL-88421", CostCenter="CC-3100", BillingTreatment="Billable", CommercialModel="Time & Materials" },
            new ProjectService { Name="Aurora Platform", DomainId=measure.Id, Client="ATRIAS", Contract="CN-2025-0112", BillId="BILL-89104", CostCenter="CC-3200", BillingTreatment="Billable", CommercialModel="Fixed Price" },
            new ProjectService { Name="Operations Support", DomainId=bill.Id, Client="ATRIAS", Contract="MS-2025-0008", BillId="BILL-87033", CostCenter="CC-3300", BillingTreatment="Billable", CommercialModel="Managed Services" },
            new ProjectService { Name="Internal Enablement", DomainId=structure.Id, Client="ATRIAS", Contract="INT-2025", BillId="", CostCenter="CC-1000", BillingTreatment="Non-Billable", CommercialModel="Internal / Non-Billable" },
            new ProjectService { Name="Data Modernization", DomainId=measure.Id, Client="ATRIAS", Contract="CN-2025-0194", BillId="BILL-89611", CostCenter="CC-3200", BillingTreatment="Billable", CommercialModel="Time & Materials" }
        };
        db.Projects.AddRange(projects);
        var tasks = new[] {
            new TaskCatalog { TaskId="TASK-001", Name="Development", Track="Delivery", Activity="Development", Phase="Build", CommercialModel="Time & Materials", ActivityClassification="T&M", TeamId=teams[0].Id, DomainId=structure.Id },
            new TaskCatalog { TaskId="TASK-002", Name="Analysis", Track="Delivery", Activity="Analysis", Phase="Solution Design", CommercialModel="Fixed Price", ActivityClassification="Non-T&M", TeamId=teams[0].Id, DomainId=structure.Id },
            new TaskCatalog { TaskId="TASK-003", Name="Production Support", Track="Support", Activity="Support", Phase="Production Support", CommercialModel="Managed Services", ActivityClassification="Non-T&M", TeamId=teams[2].Id, DomainId=bill.Id },
            new TaskCatalog { TaskId="TASK-004", Name="Training", Track="Enablement", Activity="Training", Phase="KT", CommercialModel="Internal / Non-Billable", ActivityClassification="Out", TeamId=teams[0].Id, DomainId=structure.Id },
            new TaskCatalog { TaskId="TASK-005", Name="Project Management", Track="Governance", Activity="Project Management", Phase="Governance", CommercialModel="Time & Materials", ActivityClassification="T&M", TeamId=teams[0].Id, DomainId=structure.Id },
            new TaskCatalog { TaskId="TASK-006", Name="Annual Leave", Track="Leave", Activity="Leave", Phase="Leave", CommercialModel="Internal / Non-Billable", ActivityClassification="Out", TeamId=null, DomainId=null },
            new TaskCatalog { TaskId="TASK-007", Name="Meetings", Track="Internal", Activity="Meetings", Phase="Governance", CommercialModel="Internal / Non-Billable", ActivityClassification="Out", TeamId=null, DomainId=null },
            new TaskCatalog { TaskId="TASK-008", Name="Testing", Track="Delivery", Activity="Testing", Phase="Unit Test", CommercialModel="Fixed Price", ActivityClassification="Non-T&M", TeamId=teams[1].Id, DomainId=measure.Id },
            new TaskCatalog { TaskId="TASK-009", Name="Incident Triage", Track="Support", Activity="Incident", Phase="Production Support", CommercialModel="Managed Services", ActivityClassification="Non-T&M", TeamId=teams[2].Id, DomainId=bill.Id }
        };
        db.TaskCatalog.AddRange(tasks);
        var workItems = new[] {
            new WorkItem { Number="ATLAS-10427", Title="Implement time entry validation rules", Type="User Story", ProjectServiceId=projects[0].Id, Release="R2.5", Sprint="Sprint 12", Environment="DEV" },
            new WorkItem { Number="ATLAS-10398", Title="Update user profile API", Type="User Story", ProjectServiceId=projects[0].Id, Release="R2.5", Sprint="Sprint 12", Environment="DEV" },
            new WorkItem { Number="ATLAS-10376", Title="Design review preparation", Type="Task", ProjectServiceId=projects[0].Id, Release="R2.5", Sprint="Sprint 11", Environment="DEV" },
            new WorkItem { Number="OPS-2210", Title="Service health monitoring", Type="Incident", ProjectServiceId=projects[2].Id, Release="2025.3", Sprint="Operations", Environment="PROD" },
            new WorkItem { Number="GEN-1001", Title="Internal meetings", Type="Non-Ticketing task", ProjectServiceId=projects[3].Id, Release="", Sprint="", Environment="N/A" },
            new WorkItem { Number="WI-10435", Title="Build effort tracking dashboard", Type="User Story", ProjectServiceId=projects[1].Id, Release="R2.5", Sprint="Sprint 12", Environment="DEV" },
            new WorkItem { Number="INC0042816", Title="Resolve overnight batch failure", Type="Incident", ProjectServiceId=projects[2].Id, Release="2025.3", Sprint="Operations", Environment="PROD" }
        };
        db.WorkItems.AddRange(workItems);
        var financeMonth=new DateTime(DateTime.Today.Year,DateTime.Today.Month,1).AddMonths(-1);
        db.FinanceRecords.AddRange(projects.Where(p=>p.BillId.Length>0).Select((p,i)=>new FinanceRecord { PeriodStart=financeMonth,ProjectServiceId=p.Id,ProjectService=p.Name,DomainId=p.DomainId,CostCenter=p.CostCenter,Contract=p.Contract,BillId=p.BillId,BilledVia=i%2==0?"Accenture":"Direct",BillingEntity="ATRIAS",BillingCategory=p.BillingTreatment,InvoiceType="Monthly",InvoiceRule="Approved actual hours",InvoiceHours=1120-i*135,InvoicedMds=(1120-i*135)/8.0,Approved=true }));
        await db.SaveChangesAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        async Task<AppUser> AddUser(string email, string name, string role, Team team, string password = "AtlasDemo1!")
        {
            var u = new AppUser { UserName=email, Email=email, EmailConfirmed=true, DisplayName=name, TeamId=team.Id,
                DomainId=team.DomainId, CostCenter=team.Name=="Data Engineering"?"CC-3200":"CC-3100", IsActive=true };
            var created = await userManager.CreateAsync(u, password);
            if (!created.Succeeded) throw new InvalidOperationException(string.Join("; ", created.Errors.Select(x=>x.Description)));
            await userManager.AddToRoleAsync(u, role);
            return u;
        }
        var member = await AddUser("member@atlas.local", "Jordan Davis", AtlasRoles.Member, teams[0]);
        var lead = await AddUser("lead@atlas.local", "Taylor Kim", AtlasRoles.Lead, teams[0]);
        var owner = await AddUser("owner@atlas.local", "Mira Shah", AtlasRoles.DomainOwner, teams[0]);
        var admin = await AddUser("admin@atlas.local", "Dev Admin", AtlasRoles.Administrator, teams[3]);
        var management = await AddUser("management@atlas.local", "Alex Carter", AtlasRoles.Management, teams[0]);
        var maria = await AddUser("maria@atlas.local", "Maria Lopez", AtlasRoles.Member, teams[0]);
        member.ManagerId = lead.Id;
        maria.ManagerId = lead.Id;
        owner.ManagerId = lead.Id;
        await db.SaveChangesAsync();

        var monday = WeekStart(DateTime.Today);
        var priorMonday = monday.AddDays(-7);
        async Task<WeekSubmission> AddSubmission(AppUser person, DateTime week, string status, AppUser reviewer, bool filled)
        {
            var submission = new WeekSubmission { UserId=person.Id, WeekStart=week, Status=status, ReviewerId=reviewer.Id, RequiredCapacity=40,
                SubmittedAt=status=="Draft"?null:week.AddDays(4).AddHours(16), DecidedAt=status=="Approved"?week.AddDays(5).AddHours(10):null,
                DecisionReason=status=="Approved"?"Reviewed and approved.":"" };
            db.Submissions.Add(submission);
            var end = status=="Approved"?4:Math.Clamp((DateTime.Today.Date-week.Date).Days,0,4);
            for(var day=0;day<=end;day++)
            {
                var date=week.AddDays(day);
                var project=projects[day%3];
                var item=workItems[day%workItems.Length];
                var hours=filled? (day==4?4.0:8.0) : (day<2?8.0:6.0);
                db.TimeEntries.Add(new TimeEntry { SubmissionId=submission.Id, RowKey=$"{person.Id}-{week:yyyyMMdd}-{day}", WorkDate=date,
                    CommercialModel=tasks[day%tasks.Length].CommercialModel, Track=day%2==0?"Delivery":"Support", Activity=day%2==0?"Development":"Analysis",
                    Phase=day%2==0?"Build":"Solution Design",TaskCatalogId=tasks[day%tasks.Length].Id,TaskName=tasks[day%tasks.Length].Name,ActivityClassification=tasks[day%tasks.Length].ActivityClassification, WorkItemId=item.Id, WorkItemNumber=item.Number, WorkItemType=item.Type,
                    WorkItemTitle=item.Title, ProjectServiceId=project.Id, ProjectService=project.Name, Release=item.Release, Sprint=item.Sprint,
                    Environment=item.Environment, ActualHours=hours, Realized=true, Comments="Seeded example timesheet entry." });
            }
            if(status=="Approved") db.SubmissionDecisions.Add(new SubmissionDecision { SubmissionId=submission.Id, ActorId=reviewer.Id, ActorName=reviewer.DisplayName, Decision="Approved", Reason="Reviewed and approved.", CreatedAt=submission.DecidedAt!.Value });
            if(status=="Submitted") db.SubmissionDecisions.Add(new SubmissionDecision { SubmissionId=submission.Id, ActorId=person.Id, ActorName=person.DisplayName, Decision="Submitted", Reason="Submitted for weekly review.", CreatedAt=submission.SubmittedAt!.Value });
            return submission;
        }
        await AddSubmission(member, monday, "Draft", lead, false);
        await AddSubmission(lead, monday, "Draft", owner, false);
        await AddSubmission(maria, monday, "Submitted", lead, true);
        await AddSubmission(member, priorMonday, "Approved", lead, true);
        db.Notifications.Add(new AppNotification { UserId=member.Id, Title="Complete your weekly time", Message="Your current timesheet is saved as a draft. Review and submit it by Friday.", Kind="Reminder" });
        db.Notifications.Add(new AppNotification { UserId=lead.Id, Title="1 submission needs review", Message="Maria Lopez submitted the week for your review.", Kind="Approval" });
        db.AuditEvents.Add(new AuditEvent { ActorId=admin.Id, ActorName=admin.DisplayName, Action="Demo data initialized", TargetType="Database", TargetId="seed", Details="Local ATLAS demo seed." });
        await db.SaveChangesAsync();
    }

    public static DateTime WeekStart(DateTime date)
    {
        var day=(int)date.DayOfWeek;
        var back=(day+6)%7;
        return date.Date.AddDays(-back);
    }
}
