using Microsoft.AspNetCore.Identity;

namespace Atlas.Web.Data;

public static class AtlasRoles
{
    public const string Member = "Team Member";
    public const string Lead = "Team Lead";
    public const string DomainOwner = "Domain Owner";
    public const string Administrator = "Administrator";
    public const string Management = "Management";
    public static readonly string[] All = [Member, Lead, DomainOwner, Administrator, Management];
}

public sealed class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
    public string? TeamId { get; set; }
    public string? DomainId { get; set; }
    public string? ManagerId { get; set; }
    public string CostCenter { get; set; } = "CC-3100";
    public bool IsActive { get; set; } = true;
}

public sealed class Domain
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public bool Active { get; set; } = true;
}

public sealed class Team
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string DomainId { get; set; } = "";
    public string? ParentTeamId { get; set; }
    public bool Active { get; set; } = true;
}

public sealed class ProjectService
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string DomainId { get; set; } = "";
    public string Client { get; set; } = "ATRIAS";
    public string Contract { get; set; } = "";
    public string BillId { get; set; } = "";
    public string CostCenter { get; set; } = "";
    public string BillingTreatment { get; set; } = "Billable";
    public string CommercialModel { get; set; } = "Time & Materials";
    public bool Active { get; set; } = true;
}

public sealed class FinanceRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime PeriodStart { get; set; }
    public string ProjectServiceId { get; set; } = "";
    public string ProjectService { get; set; } = "";
    public string DomainId { get; set; } = "";
    public string CostCenter { get; set; } = "";
    public string Contract { get; set; } = "";
    public string BillId { get; set; } = "";
    public string BilledVia { get; set; } = "";
    public string BillingEntity { get; set; } = "ATRIAS";
    public string BillingCategory { get; set; } = "Delivery";
    public string InvoiceType { get; set; } = "Monthly";
    public string InvoiceRule { get; set; } = "Actual hours";
    public double InvoiceHours { get; set; }
    public double InvoicedMds { get; set; }
    public bool Approved { get; set; } = true;
}

public sealed class WorkItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Number { get; set; } = "";
    public string Title { get; set; } = "";
    public string Type { get; set; } = "Incident";
    public string? ParentWorkItem { get; set; }
    public string ProjectServiceId { get; set; } = "";
    public string Release { get; set; } = "";
    public string Sprint { get; set; } = "";
    public string Iteration { get; set; } = "";
    public string Environment { get; set; } = "DEV";
    public string Status { get; set; } = "In Progress";
    public string Source { get; set; } = "Local";
    public bool Active { get; set; } = true;
}

public sealed class TaskCatalog
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string TaskId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Track { get; set; } = "Delivery";
    public string Activity { get; set; } = "Development";
    public string Phase { get; set; } = "Build";
    public string TaskCatalogId { get; set; } = "";
    public string TaskName { get; set; } = "";
    public string ActivityClassification { get; set; } = "T&M";
    public string CommercialModel { get; set; } = "Time & Materials";
    public string? TeamId { get; set; }
    public string? DomainId { get; set; }
    public bool Active { get; set; } = true;
}

public sealed class WeekSubmission
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = "";
    public DateTime WeekStart { get; set; }
    public string Status { get; set; } = "Draft";
    public string? ReviewerId { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecisionReason { get; set; }
    public double RequiredCapacity { get; set; } = 40;
    public ICollection<TimeEntry> Entries { get; set; } = new List<TimeEntry>();
    public ICollection<SubmissionComment> Comments { get; set; } = new List<SubmissionComment>();
}

public sealed class TimeEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SubmissionId { get; set; } = "";
    public string RowKey { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime WorkDate { get; set; }
    public string CommercialModel { get; set; } = "Time & Materials";
    public string Track { get; set; } = "Delivery";
    public string Activity { get; set; } = "Development";
    public string Phase { get; set; } = "Build";
    public string TaskCatalogId { get; set; } = "";
    public string TaskName { get; set; } = "";
    public string ActivityClassification { get; set; } = "T&M";
    public string WorkItemId { get; set; } = "";
    public string WorkItemNumber { get; set; } = "";
    public string WorkItemType { get; set; } = "Incident";
    public string WorkItemTitle { get; set; } = "";
    public string ProjectServiceId { get; set; } = "";
    public string ProjectService { get; set; } = "";
    public string Release { get; set; } = "";
    public string Sprint { get; set; } = "";
    public string Environment { get; set; } = "DEV";
    public double ActualHours { get; set; }
    public bool Realized { get; set; } = true;
    public string Comments { get; set; } = "";
    public string ExceptionReason { get; set; } = "";
}

public sealed class SubmissionComment
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SubmissionId { get; set; } = "";
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string Text { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class SubmissionDecision
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SubmissionId { get; set; } = "";
    public string ActorId { get; set; } = "";
    public string ActorName { get; set; } = "";
    public string Decision { get; set; } = "";
    public string Reason { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class AuditEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ActorId { get; set; } = "";
    public string ActorName { get; set; } = "";
    public string Action { get; set; } = "";
    public string TargetType { get; set; } = "";
    public string TargetId { get; set; } = "";
    public string Details { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class AppNotification
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Kind { get; set; } = "Reminder";
    public bool Read { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
