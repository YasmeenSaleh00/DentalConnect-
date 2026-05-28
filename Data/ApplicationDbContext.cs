using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DentBridge.Models;

namespace DentBridge.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<StudentProfile> StudentProfiles { get; set; }
    public DbSet<PatientProfile> PatientProfiles { get; set; }
    public DbSet<DentalCase> DentalCases { get; set; }
    public DbSet<CaseImage> CaseImages { get; set; }
    public DbSet<CaseStatusHistory> CaseStatusHistories { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Testimonial> Testimonials { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // SQL Server requires bounded key columns — cap Identity nvarchar(max) defaults
        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.Id).HasMaxLength(128);
            e.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            e.Property(u => u.LastName).HasMaxLength(100).IsRequired();
            e.Property(u => u.NormalizedEmail).HasMaxLength(256);
            e.Property(u => u.NormalizedUserName).HasMaxLength(256);
        });

        builder.Entity<IdentityRole>(e => e.Property(r => r.Id).HasMaxLength(128));
        builder.Entity<IdentityUserRole<string>>(e =>
        {
            e.Property(r => r.UserId).HasMaxLength(128);
            e.Property(r => r.RoleId).HasMaxLength(128);
        });
        builder.Entity<IdentityUserClaim<string>>(e => e.Property(c => c.UserId).HasMaxLength(128));
        builder.Entity<IdentityUserLogin<string>>(e =>
        {
            e.Property(l => l.UserId).HasMaxLength(128);
            e.Property(l => l.LoginProvider).HasMaxLength(128);
            e.Property(l => l.ProviderKey).HasMaxLength(128);
        });
        builder.Entity<IdentityUserToken<string>>(e =>
        {
            e.Property(t => t.UserId).HasMaxLength(128);
            e.Property(t => t.LoginProvider).HasMaxLength(128);
            e.Property(t => t.Name).HasMaxLength(128);
        });
        builder.Entity<IdentityRoleClaim<string>>(e => e.Property(c => c.RoleId).HasMaxLength(128));

        builder.Entity<StudentProfile>(e =>
        {
            e.HasOne(s => s.User)
             .WithOne(u => u.StudentProfile)
             .HasForeignKey<StudentProfile>(s => s.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(s => s.UserId).IsUnique();
        });

        builder.Entity<PatientProfile>(e =>
        {
            e.HasOne(p => p.User)
             .WithOne(u => u.PatientProfile)
             .HasForeignKey<PatientProfile>(p => p.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(p => p.UserId).IsUnique();
        });

        builder.Entity<DentalCase>(e =>
        {
            e.HasOne(c => c.Patient)
             .WithMany(p => p.Cases)
             .HasForeignKey(c => c.PatientProfileId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(c => c.AssignedStudent)
             .WithMany(s => s.AcceptedCases)
             .HasForeignKey(c => c.AssignedStudentId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Review>(e =>
        {
            e.HasOne(r => r.Case)
             .WithOne(c => c.Review)
             .HasForeignKey<Review>(r => r.CaseId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.Patient)
             .WithMany(p => p.Reviews)
             .HasForeignKey(r => r.PatientProfileId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(r => r.Student)
             .WithMany(s => s.Reviews)
             .HasForeignKey(r => r.StudentProfileId)
             .OnDelete(DeleteBehavior.Restrict);

            e.Property(r => r.Rating).IsRequired();
            e.ToTable(t => t.HasCheckConstraint("CK_Review_Rating", "[Rating] >= 1 AND [Rating] <= 5"));
        });

        builder.Entity<CaseStatusHistory>(e =>
        {
            e.HasOne(h => h.Case)
             .WithMany(c => c.StatusHistory)
             .HasForeignKey(h => h.CaseId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Notification>(e =>
        {
            e.HasOne(n => n.User)
             .WithMany(u => u.Notifications)
             .HasForeignKey(n => n.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Testimonial>(e =>
        {
            e.HasOne(t => t.User)
             .WithMany()
             .HasForeignKey(t => t.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.Property(t => t.UserId).HasMaxLength(128);
            e.Property(t => t.Name).HasMaxLength(150).IsRequired();
            e.Property(t => t.Role).HasMaxLength(50).IsRequired();
            e.Property(t => t.Message).HasMaxLength(1000).IsRequired();
            e.Property(t => t.ReviewedByAdminId).HasMaxLength(128);
            e.ToTable(t => t.HasCheckConstraint("CK_Testimonial_Rating", "[Rating] >= 1 AND [Rating] <= 5"));
        });
    }
}
