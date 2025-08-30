using HospitalBooking.Domain.Common;

namespace HospitalBooking.Domain.Entities
{
    public class Doctor: AuditableEntity
    {
        public string Name { get; set; }
        public string Specialization { get; set; }
        public string? Bio { get; set; }
        public string? Phone { get; set; }

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public ICollection<DoctorAvailability> Availabilities { get; set; } = new List<DoctorAvailability>();
    }
}
