using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Holonix.Server.Domain.Entities;

namespace Holonix.Server.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<Service> Services => Set<Service>();
    public DbSet<BusinessService> BusinessServices => Set<BusinessService>();
    public DbSet<BusinessSubService> BusinessSubServices => Set<BusinessSubService>();
    public DbSet<BusinessSubServiceAssignment> BusinessSubServiceAssignments => Set<BusinessSubServiceAssignment>();
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<BusinessDetails> BusinessDetails => Set<BusinessDetails>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<BusinessRole> BusinessRoles => Set<BusinessRole>();
    public DbSet<BusinessUser> BusinessUsers => Set<BusinessUser>();
    public DbSet<BusinessUserRole> BusinessUserRoles => Set<BusinessUserRole>();
    public DbSet<BusinessUserAvailability> BusinessUserAvailabilities => Set<BusinessUserAvailability>();
    public DbSet<BusinessUserTimeOff> BusinessUserTimeOffs => Set<BusinessUserTimeOff>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobWorkload> JobWorkloads => Set<JobWorkload>();
    public DbSet<JobRecurrence> JobRecurrences => Set<JobRecurrence>();
    public DbSet<JobRecurrenceException> JobRecurrenceExceptions => Set<JobRecurrenceException>();
    public DbSet<EmployeeInvite> EmployeeInvites => Set<EmployeeInvite>();
    public DbSet<EmployeeInviteWorkload> EmployeeInviteWorkloads => Set<EmployeeInviteWorkload>();
    public DbSet<Workload> Workloads => Set<Workload>();
    public DbSet<WorkloadType> WorkloadTypes => Set<WorkloadType>();
    public DbSet<WorkloadStatus> WorkloadStatuses => Set<WorkloadStatus>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("authentication");

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id)
                .HasColumnName("UsersId");
            entity.Property(user => user.InactiveDate)
                .HasColumnType("datetime2");
            entity.Property(user => user.ProfileImageBase64)
                .HasColumnType("nvarchar(max)");
            entity.HasIndex(user => user.NormalizedEmail)
                .HasDatabaseName("EmailIndex")
                .IsUnique()
                .HasFilter("[NormalizedEmail] IS NOT NULL AND [InactiveDate] IS NULL");
        });

        builder.Entity<IdentityRole>(entity =>
        {
            entity.ToTable("Roles");
        });

        builder.Entity<IdentityUserRole<string>>(entity =>
        {
            entity.ToTable("UserRoles");
        });

        builder.Entity<IdentityUserClaim<string>>(entity =>
        {
            entity.ToTable("UserClaims");
        });

        builder.Entity<IdentityUserLogin<string>>(entity =>
        {
            entity.ToTable("UserLogins");
        });

        builder.Entity<IdentityRoleClaim<string>>(entity =>
        {
            entity.ToTable("RoleClaims");
        });

        builder.Entity<IdentityUserToken<string>>(entity =>
        {
            entity.ToTable("UserTokens");
        });


        builder.Entity<Service>(entity =>
        {
            entity.ToTable("Category", "service");
            entity.HasKey(x => x.ServiceId);
            entity.Property(x => x.ServiceId).HasColumnName("CategoryId");
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
        });

        builder.Entity<Business>(entity =>
        {
            entity.ToTable("Business", "business");
            entity.HasKey(x => x.BusinessId);
            entity.Property(x => x.BusinessCode).IsRequired().HasMaxLength(10);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
            entity.Property(x => x.InactiveDate).HasColumnType("datetime2");
            entity.HasIndex(x => x.BusinessCode).IsUnique();
            entity.HasOne(x => x.Details)
                .WithOne(x => x.Business)
                .HasForeignKey<BusinessDetails>(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<BusinessDetails>(entity =>
        {
            entity.ToTable("BusinessDetails", "business");
            entity.HasKey(x => x.BusinessId);
            entity.Property(x => x.Description).HasColumnType("nvarchar(max)");
            entity.Property(x => x.BusinessEmail).HasMaxLength(256);
            entity.Property(x => x.BusinessPhoneNumber).HasMaxLength(32);
            entity.Property(x => x.Address1).HasMaxLength(200);
            entity.Property(x => x.Address2).HasMaxLength(200);
            entity.Property(x => x.City).HasMaxLength(120);
            entity.Property(x => x.State).HasMaxLength(120);
            entity.Property(x => x.ZipCode).HasMaxLength(32);
            entity.Property(x => x.Latitude).HasColumnType("decimal(9,6)");
            entity.Property(x => x.Longitude).HasColumnType("decimal(9,6)");
            entity.Property(x => x.BusinessIconBase64).HasColumnType("nvarchar(max)");
            entity.Property(x => x.BusinessJobPercentage).HasColumnType("decimal(5,2)");
            entity.HasOne(x => x.Country)
                .WithMany()
                .HasForeignKey(x => x.CountryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Country>(entity =>
        {
            entity.ToTable("Country", "reference");
            entity.HasKey(x => x.CountryId);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(120);
            entity.Property(x => x.TwoLetterIsoCode).IsRequired().HasMaxLength(2);
            entity.Property(x => x.ThreeLetterIsoCode).HasMaxLength(3);
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasIndex(x => x.TwoLetterIsoCode).IsUnique();
            entity.HasIndex(x => x.ThreeLetterIsoCode).IsUnique().HasFilter("[ThreeLetterIsoCode] IS NOT NULL");
        });

        builder.Entity<BusinessService>(entity =>
        {
            entity.ToTable("BusinessCategory", "business");
            entity.HasKey(x => x.BusinessServiceId);
            entity.Property(x => x.BusinessServiceId).HasColumnName("BusinessCategoryId");
            entity.Property(x => x.ServiceId).HasColumnName("CategoryId");
            entity.HasOne(x => x.Business)
                .WithMany()
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Service)
                .WithMany()
                .HasForeignKey(x => x.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BusinessSubService>(entity =>
        {
            entity.ToTable("BusinessService", "business");
            entity.HasKey(x => x.BusinessSubServiceId);
            entity.Property(x => x.BusinessSubServiceId).HasColumnName("BusinessServiceId");
            entity.Property(x => x.ServiceId).HasColumnName("CategoryId");
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Description).HasColumnType("nvarchar(max)");
            entity.Property(x => x.ConsultationNeeded).HasColumnType("bit");
            entity.Property(x => x.DurationMinutes).HasColumnType("int");
            entity.Property(x => x.Price).HasColumnType("decimal(18,2)");
            entity.Property(x => x.EmployeeCount).HasColumnType("int");
            entity.Property(x => x.EffectiveDate).HasColumnType("date");
            entity.Property(x => x.InactiveDate).HasColumnType("datetime2");
            entity.HasIndex(x => new { x.BusinessId, x.ServiceId, x.Name })
                .IsUnique()
                .HasFilter("[InactiveDate] IS NULL");
            entity.HasOne(x => x.Business)
                .WithMany()
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Service)
                .WithMany()
                .HasForeignKey(x => x.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BusinessSubServiceAssignment>(entity =>
        {
            entity.ToTable("BusinessServiceAssignment", "business");
            entity.HasKey(x => x.BusinessSubServiceAssignmentId);
            entity.Property(x => x.BusinessSubServiceAssignmentId).HasColumnName("BusinessServiceAssignmentId");
            entity.Property(x => x.BusinessSubServiceId).HasColumnName("BusinessServiceId");
            entity.HasIndex(x => new { x.BusinessSubServiceId, x.BusinessUserId }).IsUnique();
            entity.HasOne(x => x.BusinessSubService)
                .WithMany()
                .HasForeignKey(x => x.BusinessSubServiceId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.BusinessUser)
                .WithMany()
                .HasForeignKey(x => x.BusinessUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BusinessRole>(entity =>
        {
            entity.ToTable("BusinessRole", "business");
            entity.HasKey(x => x.BusinessRoleId);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<BusinessUser>(entity =>
        {
            entity.ToTable("BusinessUser", "business");
            entity.HasKey(x => x.BusinessUserId);
            entity.Property(x => x.UserId).IsRequired().HasMaxLength(450);
            entity.Property(x => x.ReportsToUserId).HasMaxLength(450);
            entity.HasOne(x => x.Business)
                .WithMany()
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ReportsToUser)
                .WithMany()
                .HasForeignKey(x => x.ReportsToUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BusinessUserRole>(entity =>
        {
            entity.ToTable("BusinessUserRole", "business");
            entity.HasKey(x => x.BusinessUserRoleId);
            entity.HasOne(x => x.BusinessUser)
                .WithMany()
                .HasForeignKey(x => x.BusinessUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.BusinessRole)
                .WithMany()
                .HasForeignKey(x => x.BusinessRoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BusinessUserAvailability>(entity =>
        {
            entity.ToTable("BusinessUserAvailability", "business");
            entity.HasKey(x => x.BusinessUserAvailabilityId);
            entity.HasOne(x => x.BusinessUser)
                .WithMany()
                .HasForeignKey(x => x.BusinessUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BusinessUserTimeOff>(entity =>
        {
            entity.ToTable("BusinessUserTimeOff", "business");
            entity.HasKey(x => x.BusinessUserTimeOffId);
            entity.HasOne(x => x.BusinessUser)
                .WithMany()
                .HasForeignKey(x => x.BusinessUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Job>(entity =>
        {
            entity.ToTable("Job", "job");
            entity.HasKey(x => x.JobId);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Cost).HasColumnType("decimal(18,2)");
            entity.Property(x => x.NetCost).HasColumnType("decimal(18,2)");
            entity.Property(x => x.UserId).HasMaxLength(450);
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Business)
                .WithMany()
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.BusinessUser)
                .WithMany()
                .HasForeignKey(x => x.BusinessUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.JobRecurrence)
                .WithMany()
                .HasForeignKey(x => x.JobRecurrenceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<JobWorkload>(entity =>
        {
            entity.ToTable("JobWorkload", "job");
            entity.HasKey(x => x.JobWorkloadId);
            entity.HasOne(x => x.Job)
                .WithMany()
                .HasForeignKey(x => x.JobId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Workload)
                .WithMany()
                .HasForeignKey(x => x.WorkloadId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<JobRecurrence>(entity =>
        {
            entity.ToTable("JobRecurrence", "job");
            entity.HasKey(x => x.JobRecurrenceId);
        });

        builder.Entity<JobRecurrenceException>(entity =>
        {
            entity.ToTable("JobRecurrenceException", "job");
            entity.HasKey(x => x.JobRecurrenceExceptionId);
            entity.HasOne(x => x.JobRecurrence)
                .WithMany()
                .HasForeignKey(x => x.JobRecurrenceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<EmployeeInvite>(entity =>
        {
            entity.ToTable("EmployeeInvite", "business");
            entity.HasKey(x => x.EmployeeInviteId);
            entity.Property(x => x.TargetEmail).IsRequired().HasMaxLength(256);
            entity.Property(x => x.NormalizedTargetEmail).IsRequired().HasMaxLength(256);
            entity.Property(x => x.TargetUserId).HasMaxLength(450);
            entity.HasIndex(x => x.BusinessId);
            entity.HasIndex(x => x.NormalizedTargetEmail);
            entity.HasIndex(x => x.TargetUserId);
            entity.HasIndex(x => x.TargetBusinessRoleId);
            entity.HasOne(x => x.Business)
                .WithMany()
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.TargetUser)
                .WithMany()
                .HasForeignKey(x => x.TargetUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.TargetBusinessRole)
                .WithMany()
                .HasForeignKey(x => x.TargetBusinessRoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<EmployeeInviteWorkload>(entity =>
        {
            entity.ToTable("EmployeeInviteWorkload", "work");
            entity.HasKey(x => x.EmployeeInviteWorkloadId);
            entity.HasIndex(x => x.WorkloadId).IsUnique();
            entity.HasIndex(x => x.EmployeeInviteId).IsUnique();
            entity.HasOne(x => x.Workload)
                .WithMany()
                .HasForeignKey(x => x.WorkloadId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.EmployeeInvite)
                .WithMany()
                .HasForeignKey(x => x.EmployeeInviteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<WorkloadType>(entity =>
        {
            entity.ToTable("WorkloadType", "work");
            entity.HasKey(x => x.WorkloadTypeId);
            entity.Property(x => x.Type).IsRequired().HasMaxLength(120);
            entity.HasIndex(x => x.Type).IsUnique();
        });

        builder.Entity<WorkloadStatus>(entity =>
        {
            entity.ToTable("WorkloadStatus", "work");
            entity.HasKey(x => x.WorkloadStatusId);
            entity.Property(x => x.Status).IsRequired().HasMaxLength(120);
            entity.HasIndex(x => new { x.WorkloadStatusId, x.WorkloadTypeId }).IsUnique();
            entity.HasIndex(x => new { x.WorkloadTypeId, x.Status }).IsUnique();
            entity.HasOne(x => x.WorkloadType)
                .WithMany()
                .HasForeignKey(x => x.WorkloadTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Workload>(entity =>
        {
            entity.ToTable("Workload", "work");
            entity.HasKey(x => x.WorkloadId);
            entity.Property(x => x.CreatedByUserId).IsRequired().HasMaxLength(450);
            entity.HasIndex(x => x.CreatedByUserId);
            entity.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.WorkloadType)
                .WithMany()
                .HasForeignKey(x => x.WorkloadTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.WorkloadStatus)
                .WithMany()
                .HasForeignKey(x => new { x.WorkloadStatusId, x.WorkloadTypeId })
                .HasPrincipalKey(x => new { x.WorkloadStatusId, x.WorkloadTypeId })
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}



