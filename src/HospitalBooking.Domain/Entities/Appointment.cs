using HospitalBooking.Domain.Common;
using HospitalBooking.Domain.Enums;

namespace HospitalBooking.Domain.Entities;

public class Appointment: AuditableEntity
{
    public Guid DoctorId { get; set; }
    public Doctor Doctor { get; set; }

    public Guid PatientId { get; set; }
    public Patient Patient { get; set; }

    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Booked;

    public byte[] RowVersion { get; set; } // for concurrency
}
