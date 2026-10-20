namespace Atlas.Web.Data;

public sealed record LoginRequest(string Email, string Password);
public sealed record UserDto(string Id, string Name, string Email, string Role, string Team, string Domain, string CostCenter);
public sealed record EntryDto(string Id, string RowKey, string SubmissionId, string Date, string CommercialModel, string Track, string Activity,
    string Phase, string TaskCatalogId, string TaskName, string ActivityClassification, string WorkItemId, string WorkItemNumber, string WorkItemType, string WorkItemTitle, string ProjectServiceId,
    string ProjectService, string Release, string Sprint, string Environment, double Hours, bool Realized, string Comments, string ExceptionReason);
public sealed record EntryWrite(string? Id, string? RowKey, string Date, string CommercialModel, string Track, string Activity, string Phase,
    string TaskCatalogId, string WorkItemId, string WorkItemType, string WorkItemTitle, string ProjectServiceId, string ProjectService,
    string? Release, string? Sprint, string? Environment, double Hours, bool Realized, string? Comments, string? ExceptionReason, string? EmployeeId=null);
public sealed record SubmissionDto(string Id, string UserId, string Employee, string Email, string Team, string Domain, string WeekStart,
    string Status, string Reviewer, double RequiredCapacity, double EnteredHours, double Variance, int Exceptions,
    DateTime? SubmittedAt, string DecisionReason, IReadOnlyList<EntryDto> Entries, IReadOnlyList<CommentDto> Comments, IReadOnlyList<DecisionDto> Decisions);
public sealed record CommentDto(string Id, string UserName, string Text, DateTime CreatedAt);
public sealed record DecisionDto(string ActorName, string Decision, string Reason, DateTime CreatedAt);
public sealed record DecisionRequest(string Decision, string Reason);
public sealed record CommentRequest(string Text);
public sealed record UserCreateRequest(string Name,string Email,string Password,string Role,string TeamId,string DomainId,string? ManagerId,string? CostCenter);
public sealed record UserUpdateRequest(string? Name,string? Role,string? TeamId,string? DomainId,string? ManagerId,string? CostCenter,bool? Active);
public sealed record DomainWrite(string Name,bool? Active);
public sealed record TeamWrite(string Name,string DomainId,string? ParentTeamId,bool? Active);
public sealed record ProjectWrite(string Name,string DomainId,string? Client,string? Contract,string? BillId,string? CostCenter,string? BillingTreatment,string? CommercialModel,bool? Active);
public sealed record WorkItemWrite(string Number,string Title,string Type,string ProjectServiceId,string? ParentWorkItem,string? Release,string? Sprint,string? Iteration,string? Environment,string? Status,bool? Active);
public sealed record TaskWrite(string TaskId,string Name,string Track,string Activity,string Phase,string CommercialModel,string ActivityClassification,string? TeamId,string? DomainId,bool? Active);
public sealed record ReportEntryDto(EntryDto Entry,string Employee,string Email,string Team,string Domain,string Status,string Reviewer,string CostCenter,string BillingTreatment);
public sealed record FinanceWrite(string PeriodStart,string ProjectServiceId,string BilledVia,string BillingEntity,string BillingCategory,string InvoiceType,string InvoiceRule,double InvoiceHours,double InvoicedMds,bool Approved);
public sealed record LookupsDto(IReadOnlyList<Domain> Domains, IReadOnlyList<Team> Teams, IReadOnlyList<ProjectService> Projects,
    IReadOnlyList<WorkItem> WorkItems, IReadOnlyList<TaskCatalog> Tasks, IReadOnlyList<UserDto> Users, IReadOnlyList<string> Roles);

public static class TimeEntryRules
{
    public const double MinimumHours = 0.25;
    public const double MaximumHoursPerDay = 8.0;
    public const double WeeklyCapacity = 40.0;

    public static string? Validate(double hours, DateTime date, DateTime today)
    {
        if (!double.IsFinite(hours) || hours < MinimumHours || hours > MaximumHoursPerDay)
            return "Actual hours must be between 0.25 and 8.00.";
        if (date.Date > today.Date) return "Future-dated entries are not allowed.";
        return null;
    }

    public static double Variance(double entered, double capacity) => Math.Round(entered - capacity, 2);
}
