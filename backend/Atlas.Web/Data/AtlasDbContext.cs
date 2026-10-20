using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Web.Data;

public sealed class AtlasDbContext(DbContextOptions<AtlasDbContext> options)
    : IdentityDbContext<AppUser>(options)
{
    public DbSet<Domain> Domains => Set<Domain>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<ProjectService> Projects => Set<ProjectService>();
    public DbSet<FinanceRecord> FinanceRecords => Set<FinanceRecord>();
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
    public DbSet<TaskCatalog> TaskCatalog => Set<TaskCatalog>();
    public DbSet<WeekSubmission> Submissions => Set<WeekSubmission>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<SubmissionComment> SubmissionComments => Set<SubmissionComment>();
    public DbSet<SubmissionDecision> SubmissionDecisions => Set<SubmissionDecision>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<AppNotification> Notifications => Set<AppNotification>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<Domain>().HasIndex(x => x.Name).IsUnique();
        b.Entity<Team>().HasIndex(x => new { x.Name, x.DomainId }).IsUnique();
        b.Entity<ProjectService>().HasIndex(x => x.Name);
        b.Entity<FinanceRecord>().HasIndex(x => new { x.PeriodStart, x.ProjectServiceId }).IsUnique();
        b.Entity<WorkItem>().HasIndex(x => x.Number).IsUnique();
        b.Entity<TaskCatalog>().HasIndex(x => x.TaskId).IsUnique();
        b.Entity<WeekSubmission>().HasIndex(x => new { x.UserId, x.WeekStart }).IsUnique();
        b.Entity<TimeEntry>().HasIndex(x => new { x.SubmissionId, x.WorkDate });
        b.Entity<TimeEntry>().Property(x => x.ActualHours).HasConversion<double>();
        b.Entity<WeekSubmission>().Property(x => x.RequiredCapacity).HasConversion<double>();
        b.Entity<WeekSubmission>().HasMany(x => x.Entries).WithOne().HasForeignKey(x => x.SubmissionId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<WeekSubmission>().HasMany(x => x.Comments).WithOne().HasForeignKey(x => x.SubmissionId).OnDelete(DeleteBehavior.Cascade);
    }
}
