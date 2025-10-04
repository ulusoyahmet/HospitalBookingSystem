using Microsoft.AspNetCore.Authorization;

namespace HospitalBooking.API.Policies.Requirements
{
    public class MinimumAgeRequirement : IAuthorizationRequirement
    {
        public int MinimumAge { get; }
        
        public MinimumAgeRequirement(int minimumAge)
        {
            MinimumAge = minimumAge;
        }
    }
}
