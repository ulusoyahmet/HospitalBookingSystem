using HospitalBooking.Domain.Common;

namespace HospitalBooking.Domain.Entities;

public class Patient: AuditableEntity
{
    public Guid? UserId { get; set; } // Link to ApplicationUser
    public string Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? InsuranceNumber { get; set; }

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
