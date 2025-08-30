using HospitalBooking.Domain.Common;

namespace HospitalBooking.Domain.Entities;

public class DoctorAvailability: AuditableEntity
{
    public Guid DoctorId { get; set; }
    public Doctor Doctor { get; set; }

    public int Weekday { get; set; } // 0=Sunday, 6=Saturday
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int SlotDurationMinutes { get; set; } = 30;
}
