using Microsoft.AspNetCore.Authorization;

namespace HospitalBooking.API.Policies.Requirements
{
    public class WorkingHoursRequirement : IAuthorizationRequirement
    {
        public int StartHour { get; }
        public int EndHour { get; }
        
        public WorkingHoursRequirement(int startHour, int endHour)
        {
            StartHour = startHour;
            EndHour = endHour;
        }
    }
}

