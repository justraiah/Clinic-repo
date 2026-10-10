using Microsoft.EntityFrameworkCore;
using Capstone_Clinic.Models;

namespace Capstone_Clinic.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }
        public DbSet<MedicalRequirement> MedicalRequirements { get; set; }
        public DbSet<StudentAccount> StudentAccounts { get; set; }
        public DbSet<ClinicPatient> ClinicPatients { get; set; }
        public DbSet<MedicalRecord> MedicalRecords { get; set; }

        public DbSet<DentalRecord> DentalRecords { get; set; }

        public DbSet<VitalSignLog> VitalSignLogs { get; set; }
        public DbSet<Alert> Alerts { get; set; }
        public DbSet<Staff> Staffs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DentalRecord>()
                .HasOne(d => d.ClinicPatient)
                .WithMany()
                .HasForeignKey(d => d.ClinicPatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VitalSignLog>()
                .HasOne(v => v.MedicalRecord)
                .WithMany(m => m.VitalSignLogs)
                .HasForeignKey(v => v.MedicalRecordId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Alert>()
                .HasOne(a => a.VitalSignLog)
                .WithMany(v => v.Alerts)
                .HasForeignKey(a => a.VitalLogId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}