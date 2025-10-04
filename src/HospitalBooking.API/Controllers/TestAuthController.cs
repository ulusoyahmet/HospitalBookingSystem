using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HospitalBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requires authentication for all endpoints
    public class TestAuthController : ControllerBase
    {
        // Public endpoint - any authenticated user can access
        [HttpGet("profile")]
        public IActionResult GetProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var name = User.FindFirst(ClaimTypes.Name)?.Value;
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            return Ok(new
            {
                UserId = userId,
                Email = email,
                Name = name,
                Roles = roles,
                Claims = User.Claims.Select(c => new { c.Type, c.Value })
            });
        }

        // Admin only endpoint
        [HttpGet("admin-only")]
        [Authorize(Roles = "Admin")]
        public IActionResult AdminOnly()
        {
            return Ok(new { message = "This is admin-only content", user = User.Identity.Name });
        }

        // Doctor only endpoint
        [HttpGet("doctor-only")]
        [Authorize(Roles = "Doctor")]
        public IActionResult DoctorOnly()
        {
            return Ok(new { message = "This is doctor-only content", user = User.Identity.Name });
        }

        // Patient only endpoint
        [HttpGet("patient-only")]
        [Authorize(Roles = "Patient")]
        public IActionResult PatientOnly()
        {
            return Ok(new { message = "This is patient-only content", user = User.Identity.Name });
        }

        // Multiple roles - Admin OR Doctor can access
        [HttpGet("staff-only")]
        [Authorize(Roles = "Admin,Doctor")]
        public IActionResult StaffOnly()
        {
            return Ok(new { message = "This is staff-only content (Admin or Doctor)", user = User.Identity.Name });
        }

        // Custom policy-based authorization example
        [HttpGet("custom-policy")]
        [Authorize(Policy = "RequireEmailConfirmed")]
        public IActionResult CustomPolicy()
        {
            return Ok(new { message = "This requires email confirmed policy", user = User.Identity.Name });
        }
    }
}



// Add this configuration to Program.cs for custom policies
public static class AuthorizationPolicies
{
    public static void ConfigureAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Custom policy examples
            options.AddPolicy("RequireEmailConfirmed", policy =>
                policy.RequireClaim("email_verified", "true"));

            options.AddPolicy("RequireAdminOrDoctor", policy =>
                policy.RequireRole("Admin", "Doctor"));

            options.AddPolicy("RequireMinimumAge", policy =>
                policy.Requirements.Add(new MinimumAgeRequirement(18)));

            // Department-based policy
            options.AddPolicy("CardiologyDepartment", policy =>
                policy.RequireClaim("department", "Cardiology"));
        });
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