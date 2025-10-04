using Microsoft.AspNetCore.Authorization;

namespace HospitalBooking.API.Policies.Requirements
{
    public class CanEditPatientRecordsRequirement : IAuthorizationRequirement
    {
        // This is just a marker - the logic goes in the handler
    }
}