using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Holonix.Server.Domain.Entities;

namespace Holonix.Server.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<BusinessService> BusinessServices => Set<BusinessService>();
    public DbSet<BusinessToService> BusinessToServices => Set<BusinessToService>();
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<BusinessRole> BusinessRoles => Set<BusinessRole>();
    public DbSet<BusinessUser> BusinessUsers => Set<BusinessUser>();
    public DbSet<BusinessUserRole> BusinessUserRoles => Set<BusinessUserRole>();
    public DbSet<BusinessUserAvailability> BusinessUserAvailabilities => Set<BusinessUserAvailability>();
    public DbSet<BusinessUserTimeOff> BusinessUserTimeOffs => Set<BusinessUserTimeOff>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobWorkload> JobWorkloads => Set<JobWorkload>();
    public DbSet<JobRecurrence> JobRecurrences => Set<JobRecurrence>();
    public DbSet<JobRecurrenceException> JobRecurrenceExceptions => Set<JobRecurrenceException>();
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
            entity.Property(user => user.ProfileImageBase64)
                .HasColumnType("nvarchar(max)");
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


        builder.Entity<BusinessService>(entity =>
        {
            entity.ToTable("BusineessService", "business");
            entity.HasKey(x => x.ServiceId);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
        });

        builder.Entity<Business>(entity =>
        {
            entity.ToTable("Business", "business");
            entity.HasKey(x => x.BusinessId);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Description).HasColumnType("nvarchar(max)");
            entity.Property(x => x.BusinessIconBase64).HasColumnType("nvarchar(max)");
            entity.Property(x => x.OwnerJobPercentage).HasColumnType("decimal(5,2)");
        });

        builder.Entity<BusinessToService>(entity =>
        {
            entity.ToTable("BusinessToService", "business");
            entity.HasKey(x => x.BusinessToServiceId);
            entity.HasOne(x => x.Business)
                .WithMany()
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Service)
                .WithMany()
                .HasForeignKey(x => x.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BusinessRole>(entity =>
        {
            entity.ToTable("BusinessRole", "business");
            entity.HasKey(x => x.BusinessRoleId);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
        });

        builder.Entity<BusinessUser>(entity =>
        {
            entity.ToTable("BusinessUser", "business");
            entity.HasKey(x => x.BusinessUserId);
            entity.Property(x => x.UserId).IsRequired().HasMaxLength(450);
            entity.HasOne(x => x.Business)
                .WithMany()
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
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

        builder.Entity<WorkloadType>(entity =>
        {
            entity.ToTable("WorkloadType", "work");
            entity.HasKey(x => x.WorkloadTypeId);
            entity.Property(x => x.Type).IsRequired().HasMaxLength(120);
        });

        builder.Entity<WorkloadStatus>(entity =>
        {
            entity.ToTable("WorkloadStatus", "work");
            entity.HasKey(x => x.WorkloadStatusId);
            entity.Property(x => x.Status).IsRequired().HasMaxLength(120);
            entity.HasOne(x => x.WorkloadType)
                .WithMany()
                .HasForeignKey(x => x.WorkloadTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Workload>(entity =>
        {
            entity.ToTable("Workload", "work");
            entity.HasKey(x => x.WorkloadId);
            entity.HasOne(x => x.WorkloadType)
                .WithMany()
                .HasForeignKey(x => x.WorkloadTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.WorkloadStatus)
                .WithMany()
                .HasForeignKey(x => x.WorkloadStatusId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}



