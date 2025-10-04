using Microsoft.AspNetCore.Authorization;
using HospitalBooking.API.Policies.Requirements;

namespace HospitalBooking.API.Policies.Handlers
{
    public class WorkingHoursHandler : AuthorizationHandler<WorkingHoursRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            WorkingHoursRequirement requirement)
        {
            var currentHour = DateTime.Now.Hour;

            // Check if current time is within working hours
            if (currentHour >= requirement.StartHour && currentHour < requirement.EndHour)
            {
                context.Succeed(requirement);
            }
            // Admins can work anytime
            else if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
            }
            
            return Task.CompletedTask;
        }
    }    
}
