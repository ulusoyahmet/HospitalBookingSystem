using HospitalBooking.Domain.Common;

namespace HospitalBooking.Domain.Entities;

public class Patient: AuditableEntity
{
    public string Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
