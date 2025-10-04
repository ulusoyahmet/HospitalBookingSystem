using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HospitalBooking.API.Policies.Handlers;
using HospitalBooking.API.Policies.Requirements;


namespace HospitalBooking.API.Policies{
public static class AuthorizationPolicies
{
    public static void ConfigureAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // --------------------------------------------------------
            // EXAMPLE 1: Simple Role Policy
            // --------------------------------------------------------
            // User must have the "Admin" role
            options.AddPolicy("RequireAdminRole", policy =>
                policy.RequireRole("Admin"));

            // --------------------------------------------------------
            // EXAMPLE 2: Multiple Roles (OR logic)
            // --------------------------------------------------------
            // User must have EITHER "Admin" OR "Doctor" role
            options.AddPolicy("RequireAdminOrDoctor", policy =>
                policy.RequireRole("Admin", "Doctor"));

            // --------------------------------------------------------
            // EXAMPLE 3: Claims-Based Policy
            // --------------------------------------------------------
            // User must have email_verified claim with value "true"
            options.AddPolicy("RequireEmailConfirmed", policy =>
                policy.RequireClaim("email_verified", "true"));
            
            // User must have a department claim (any value)
            options.AddPolicy("RequireDepartment", policy =>
                policy.RequireClaim("department"));
            
            // User must have department claim with specific values
            options.AddPolicy("CardiologyDepartment", policy =>
                policy.RequireClaim("department", "Cardiology", "Cardiac Surgery"));

            // --------------------------------------------------------
            // EXAMPLE 4: Complex Composite Policy (AND logic)
            // --------------------------------------------------------
            // ALL of these requirements must be met
            options.AddPolicy("SeniorMedicalStaff", policy =>
            {
                policy.RequireAuthenticatedUser(); // Must be logged in
                policy.RequireRole("Doctor", "Nurse"); // Must be Doctor OR Nurse
                policy.RequireClaim("employment_type", "full-time"); // Must be full-time
                policy.RequireClaim("years_experience"); // Must have experience claim
            });

            // --------------------------------------------------------
            // EXAMPLE 5: Custom Requirements
            // --------------------------------------------------------
            // For complex logic that can't be expressed with simple rules
            options.AddPolicy("CanEditPatientRecords", policy =>
                policy.Requirements.Add(new CanEditPatientRecordsRequirement()));

            options.AddPolicy("WorkingHours", policy =>
                policy.Requirements.Add(new WorkingHoursRequirement(8, 18))); // 8 AM to 6 PM

            options.AddPolicy("MinimumAge", policy =>
                policy.Requirements.Add(new MinimumAgeRequirement(18)));

            // --------------------------------------------------------
            // EXAMPLE 6: Building Dynamic Policies
            // --------------------------------------------------------
            options.AddPolicy("DepartmentManager", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                {
                    // Custom logic in a lambda
                    var user = context.User;
                    var hasManagerRole = user.IsInRole("Manager");
                    var hasDepartment = user.HasClaim(c => c.Type == "department");
                    return hasManagerRole && hasDepartment;
                });
            });
        });

        // Register custom authorization handlers
        services.AddScoped<IAuthorizationHandler, CanEditPatientRecordsHandler>();
        services.AddScoped<IAuthorizationHandler, WorkingHoursHandler>();
        services.AddScoped<IAuthorizationHandler, MinimumAgeHandler>();
    }
}

// Custom requirement example
public class MinimumAgeRequirement : IAuthorizationRequirement
{
    public int MinimumAge { get; }
    
    public MinimumAgeRequirement(int minimumAge)
    {
        MinimumAge = minimumAge;
    }
}

// Custom authorization handler
public class MinimumAgeHandler : AuthorizationHandler<MinimumAgeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MinimumAgeRequirement requirement)
    {
        var dateOfBirthClaim = context.User.FindFirst("date_of_birth");
        
        if (dateOfBirthClaim != null)
        {
            var dateOfBirth = DateTime.Parse(dateOfBirthClaim.Value);
            var age = DateTime.Today.Year - dateOfBirth.Year;
            
            if (age >= requirement.MinimumAge)
            {
                context.Succeed(requirement);
            }
        }
        
        return Task.CompletedTask;
    }
}
}
