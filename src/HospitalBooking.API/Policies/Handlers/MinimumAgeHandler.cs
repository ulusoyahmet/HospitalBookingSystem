using Microsoft.AspNetCore.Authorization;
using HospitalBooking.API.Policies.Requirements;


namespace HospitalBooking.API.Policies.Handlers
{

    public class MinimumAgeHandler : AuthorizationHandler<MinimumAgeRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            MinimumAgeRequirement requirement)
        {
            var dateOfBirthClaim = context.User.FindFirst("date_of_birth");
            
            if (dateOfBirthClaim != null && DateTime.TryParse(dateOfBirthClaim.Value, out var dateOfBirth))
            {
                var age = DateTime.Today.Year - dateOfBirth.Year;
                if (dateOfBirth.Date > DateTime.Today.AddYears(-age)) age--;
                
                if (age >= requirement.MinimumAge)
                {
                    context.Succeed(requirement);
                }
            }
            
            return Task.CompletedTask;
        }
    } 
}


