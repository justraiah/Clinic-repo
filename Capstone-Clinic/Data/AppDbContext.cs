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
        public DbSet<StudentAccount> StudentAccounts { get; set; }

        public DbSet<MedicalRecord> MedicalRecords { get; set; }

        public DbSet<VitalSignLog> VitalSignLogs { get; set; }

        public DbSet<Alert> Alerts { get; set; }
        public DbSet<Staff> Staffs { get; set; }
    }
}