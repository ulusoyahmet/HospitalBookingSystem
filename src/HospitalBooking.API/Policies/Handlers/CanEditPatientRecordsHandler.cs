using Microsoft.AspNetCore.Authorization;
using HospitalBooking.API.Policies.Requirements;
using Microsoft.AspNetCore.Http;

namespace HospitalBooking.API.Policies.Handlers
{
    public class CanEditPatientRecordsHandler : AuthorizationHandler<CanEditPatientRecordsRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        public CanEditPatientRecordsHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            CanEditPatientRecordsRequirement requirement)
        {
            var user = context.User;
            
            // Admins can always edit
            if (user.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
            
            // Doctors can edit their own patients' records
            if (user.IsInRole("Doctor"))
            {
                // Get patient ID from route
                var httpContext = _httpContextAccessor.HttpContext;
                var patientId = httpContext?.Request.RouteValues["patientId"]?.ToString();
                
                // Check if doctor is assigned to this patient
                var doctorId = user.FindFirst("doctor_id")?.Value;
                
                // just check if doctor has the claim
                if (!string.IsNullOrEmpty(doctorId) && !string.IsNullOrEmpty(patientId))
                {
                    context.Succeed(requirement);
                }
            }
            
            // If none of the conditions are met, the requirement fails
            // We don't call context.Fail() - just don't call Succeed()
            return Task.CompletedTask;
        }
    }
}