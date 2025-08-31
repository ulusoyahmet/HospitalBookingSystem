using System.Reflection.Emit;
using HospitalBooking.Domain.Entities;
using HospitalBooking.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HospitalBooking.Infrastructure.Persistence;

public class HospitalDbContext: IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public HospitalDbContext(DbContextOptions<HospitalDbContext> options)
        : base(options) { }

    public DbSet<Doctor> Doctors { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<DoctorAvailability> DoctorAvailabilities { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(HospitalDbContext).Assembly);
        // Configuration Appointment unique index
        builder.Entity<Appointment>(b =>
        {
            b.HasOne(a => a.Doctor).WithMany(d => d.Appointments).HasForeignKey(a => a.DoctorId);
            b.HasOne(a => a.Patient).WithMany(p => p.Appointments).HasForeignKey(a => a.PatientId);
            b.Property(a => a.RowVersion).IsRowVersion();
            b.HasIndex(a => new { a.DoctorId, a.Start }).IsUnique();
        });

        builder.Entity<DoctorAvailability>(b =>
        {
            b.HasOne(d => d.Doctor).WithMany(a => a.Availabilities).HasForeignKey(d => d.DoctorId);
        });
    }
}
