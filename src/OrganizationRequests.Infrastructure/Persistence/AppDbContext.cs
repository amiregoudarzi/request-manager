using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrganizationRequests.Application;
using OrganizationRequests.Domain.Entities;
using OrganizationRequests.Infrastructure.Security;

namespace OrganizationRequests.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityUserContext<Account, Guid>(options), IAppDbContext, IDataProtectionKeyContext
{
    public DbSet<OrganizationalUnit> OrganizationalUnits => Set<OrganizationalUnit>();
    public DbSet<UnitReviewer> UnitReviewers => Set<UnitReviewer>();
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
        builder.ApplyConfiguration(new ReviewerConfiguration());
        builder.ApplyConfiguration(new RequestConfiguration());
        builder.ApplyConfiguration(new HistoryConfiguration());
        builder.ApplyConfiguration(new KeyConfiguration());
        ConfigureIdentityTables(builder);
    }

    private static void ConfigureEntity<T>(EntityTypeBuilder<T> builder) where T : Entity
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CreatedDateUtc).HasColumnName("created_date_utc")
            .HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
            .HasColumnType("datetime2").IsRequired();
    }

    // Only Identity's user store is enabled; no role-management tables are needed.
    private static void ConfigureIdentityTables(ModelBuilder builder)
    {
        builder.Entity<IdentityUserClaim<Guid>>(b =>
        {
            b.ToTable("UserClaims");
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.UserId).HasColumnName("user_id");
            b.Property(x => x.ClaimType).HasColumnName("claim_type");
            b.Property(x => x.ClaimValue).HasColumnName("claim_value");
        });
        builder.Entity<IdentityUserLogin<Guid>>(b =>
        {
            b.ToTable("UserLogins");
            b.Property(x => x.LoginProvider).HasColumnName("login_provider").HasMaxLength(128);
            b.Property(x => x.ProviderKey).HasColumnName("provider_key").HasMaxLength(128);
            b.Property(x => x.ProviderDisplayName).HasColumnName("provider_display_name");
            b.Property(x => x.UserId).HasColumnName("user_id");
        });
        builder.Entity<IdentityUserToken<Guid>>(b =>
        {
            b.ToTable("UserTokens");
            b.Property(x => x.LoginProvider).HasColumnName("login_provider").HasMaxLength(128);
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(128);
            b.Property(x => x.Value).HasColumnName("value");
            b.Property(x => x.UserId).HasColumnName("user_id");
        });
    }

    private sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> b)
        {
            b.ToTable("Users");
            b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            b.Property(x => x.CreatedDateUtc).HasColumnName("created_date_utc").HasColumnType("datetime2")
                .HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
            b.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
            b.Property(x => x.IsActive).HasColumnName("is_active");
            b.Property(x => x.UserName).HasColumnName("user_name");
            b.Property(x => x.NormalizedUserName).HasColumnName("normalized_user_name");
            b.Property(x => x.Email).HasColumnName("email");
            b.Property(x => x.NormalizedEmail).HasColumnName("normalized_email");
            b.Property(x => x.EmailConfirmed).HasColumnName("email_confirmed");
            b.Property(x => x.PasswordHash).HasColumnName("password_hash");
            b.Property(x => x.SecurityStamp).HasColumnName("security_stamp");
            b.Property(x => x.ConcurrencyStamp).HasColumnName("concurrency_stamp");
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

    private sealed class ReviewerConfiguration : IEntityTypeConfiguration<UnitReviewer>
    {
        public void Configure(EntityTypeBuilder<UnitReviewer> b)
        {
            b.ToTable("UnitReviewers");
            ConfigureEntity(b);
            b.Property(x => x.UserId).HasColumnName("user_id");
            b.Property(x => x.OrganizationalUnitId).HasColumnName("organizational_unit_id");
            b.HasIndex(x => new { x.UserId, x.OrganizationalUnitId }).IsUnique();
            b.HasOne<Account>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<OrganizationalUnit>().WithMany().HasForeignKey(x => x.OrganizationalUnitId)
                .OnDelete(DeleteBehavior.Restrict);
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
            b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.RowVersion).HasColumnName("row_version").IsRowVersion();
            b.HasOne<Account>().WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<OrganizationalUnit>().WithMany().HasForeignKey(x => x.OrganizationalUnitId)
                .OnDelete(DeleteBehavior.Restrict);
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
