using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrganizationRequests.Application;
using OrganizationRequests.Application.Abstractions;
using OrganizationRequests.Domain.Entities;
using OrganizationRequests.Domain.Enums;
using OrganizationRequests.Infrastructure.Security;

namespace OrganizationRequests.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IAppDbContext, IDataProtectionKeyContext
{
    public DbSet<Account> Users => Set<Account>();
    public DbSet<OrganizationalUnit> OrganizationalUnits => Set<OrganizationalUnit>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<ServiceRequest> Requests => Set<ServiceRequest>();
    public DbSet<RequestStatusHistory> RequestStatusHistories => Set<RequestStatusHistory>();
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (ChangeTracker.Entries<RequestStatusHistory>().Any(x =>
                x.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Request history cannot be edited or deleted.");
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfiguration(new AccountConfiguration());
        builder.ApplyConfiguration(new UnitConfiguration());
        builder.ApplyConfiguration(new EmployeeConfiguration());
        builder.ApplyConfiguration(new RequestConfiguration());
        builder.ApplyConfiguration(new HistoryConfiguration());
        builder.ApplyConfiguration(new KeyConfiguration());
    }

    private static void ConfigureEntity<T>(EntityTypeBuilder<T> builder) where T : Entity
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CreatedDateUtc).HasColumnName("created_date_utc")
            .HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
            .HasColumnType("datetime2").IsRequired();
    }

    private sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> b)
        {
            b.ToTable("Users", table => table.HasCheckConstraint("CK_Users_Role",
                "[role] IN ('Employee','UnitReviewer')"));
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.NormalizedUserName).HasDatabaseName("UserNameIndex").IsUnique();
            b.HasIndex(x => x.NormalizedEmail).HasDatabaseName("EmailIndex");
            b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            b.Property(x => x.CreatedDateUtc).HasColumnName("created_date_utc").HasColumnType("datetime2")
                .HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
            b.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
            b.Property(x => x.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(32);
            b.Property(x => x.IsActive).HasColumnName("is_active");
            b.Property(x => x.UserName).HasColumnName("user_name").HasMaxLength(256);
            b.Property(x => x.NormalizedUserName).HasColumnName("normalized_user_name").HasMaxLength(256);
            b.Property(x => x.Email).HasColumnName("email").HasMaxLength(256);
            b.Property(x => x.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(256);
            b.Property(x => x.EmailConfirmed).HasColumnName("email_confirmed");
            b.Property(x => x.PasswordHash).HasColumnName("password_hash");
            b.Property(x => x.SecurityStamp).HasColumnName("security_stamp");
            b.Property(x => x.ConcurrencyStamp).HasColumnName("concurrency_stamp").IsConcurrencyToken();
            b.Property(x => x.PhoneNumber).HasColumnName("phone_number");
            b.Property(x => x.PhoneNumberConfirmed).HasColumnName("phone_number_confirmed");
            b.Property(x => x.TwoFactorEnabled).HasColumnName("two_factor_enabled");
            b.Property(x => x.LockoutEnd).HasColumnName("lockout_end").HasColumnType("datetimeoffset");
            b.Property(x => x.LockoutEnabled).HasColumnName("lockout_enabled");
            b.Property(x => x.AccessFailedCount).HasColumnName("access_failed_count");
        }
    }

    private sealed class UnitConfiguration : IEntityTypeConfiguration<OrganizationalUnit>
    {
        public void Configure(EntityTypeBuilder<OrganizationalUnit> b)
        {
            b.ToTable("OrganizationalUnits");
            ConfigureEntity(b);
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            b.Property(x => x.IsActive).HasColumnName("is_active");
        }
    }

    private sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
    {
        public void Configure(EntityTypeBuilder<Employee> b)
        {
            b.ToTable("Employees");
            ConfigureEntity(b);
            b.Property(x => x.UserId).HasColumnName("user_id");
            b.Property(x => x.OrganizationalUnitId).HasColumnName("organizational_unit_id");
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            b.HasIndex(x => x.UserId).IsUnique();
            b.HasAlternateKey(x => new { x.UserId, x.OrganizationalUnitId });
            b.HasAlternateKey(x => new { x.Id, x.OrganizationalUnitId });
            b.HasOne<Account>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<OrganizationalUnit>().WithMany().HasForeignKey(x => x.OrganizationalUnitId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.OrganizationalUnitId, x.Name, x.Id });
        }
    }

    private sealed class RequestConfiguration : IEntityTypeConfiguration<ServiceRequest>
    {
        public void Configure(EntityTypeBuilder<ServiceRequest> b)
        {
            b.ToTable("Requests", t =>
            {
                t.HasCheckConstraint("CK_Requests_Status", "[status] IN ('Pending','Approved','Rejected')");
                t.HasCheckConstraint("CK_Requests_Type", "[request_type] IN ('Equipment','InternalService','TechnicalIssue')");
            });
            ConfigureEntity(b);
            b.Property(x => x.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasColumnName("description").HasMaxLength(4000).IsRequired();
            b.Property(x => x.Type).HasColumnName("request_type").HasConversion<string>().HasMaxLength(32);
            b.Property(x => x.CreatedById).HasColumnName("created_by_id");
            b.Property(x => x.OrganizationalUnitId).HasColumnName("organizational_unit_id");
            b.Property(x => x.ReceiverEmployeeId).HasColumnName("receiver_employee_id");
            b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.RowVersion).HasColumnName("row_version").IsRowVersion();
            b.HasOne<Account>().WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<OrganizationalUnit>().WithMany().HasForeignKey(x => x.OrganizationalUnitId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Employee>().WithMany().HasForeignKey(x => new { x.ReceiverEmployeeId, x.OrganizationalUnitId })
                .HasPrincipalKey(x => new { x.Id, x.OrganizationalUnitId }).OnDelete(DeleteBehavior.Restrict);
            b.HasMany(x => x.History).WithOne().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
            b.Navigation(x => x.History).UsePropertyAccessMode(PropertyAccessMode.Field);
            b.HasIndex(x => new { x.CreatedById, x.CreatedDateUtc, x.Id });
            b.HasIndex(x => new { x.OrganizationalUnitId, x.Status, x.CreatedDateUtc, x.Id });
        }
    }

    private sealed class HistoryConfiguration : IEntityTypeConfiguration<RequestStatusHistory>
    {
        public void Configure(EntityTypeBuilder<RequestStatusHistory> b)
        {
            b.ToTable("RequestStatusHistories", t =>
            {
                t.HasCheckConstraint("CK_History_Transition", "([previous_status] IS NULL AND [new_status] = 'Pending') OR ([previous_status] IS NOT NULL AND [previous_status] = 'Pending' AND [new_status] IN ('Approved','Rejected'))");
                t.HasCheckConstraint("CK_History_RejectionReason", "[new_status] <> 'Rejected' OR ([comment] IS NOT NULL AND LEN(LTRIM(RTRIM([comment]))) > 0)");
            });
            ConfigureEntity(b);
            b.Property(x => x.RequestId).HasColumnName("request_id");
            b.Property(x => x.PreviousStatus).HasColumnName("previous_status").HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.NewStatus).HasColumnName("new_status").HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.ActorId).HasColumnName("actor_id");
            b.Property(x => x.Comment).HasColumnName("comment").HasMaxLength(1000);
            b.HasOne<Account>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.RequestId, x.CreatedDateUtc, x.Id });
            // There is one creation entry and, at most, one final decision per request.
            b.HasIndex(x => x.RequestId).HasFilter("[new_status] IN ('Approved','Rejected')").IsUnique();
        }
    }

    private sealed class KeyConfiguration : IEntityTypeConfiguration<DataProtectionKey>
    {
        public void Configure(EntityTypeBuilder<DataProtectionKey> b)
        {
            b.ToTable("DataProtectionKeys");
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.FriendlyName).HasColumnName("friendly_name");
            b.Property(x => x.Xml).HasColumnName("xml");
        }
    }
}
