using HospitalBooking.Infrastructure.Identity;
using HospitalBooking.Infrastructure.Persistence;
using HospitalBooking.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using Microsoft.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Microsoft.AspNetCore.Authentication;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;



namespace HospitalBooking.API.Controllers
{
    [ApiController]
    [Route("connect")]
    public class AuthController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;
        private readonly HospitalDbContext _context;

        public AuthController(
            SignInManager<ApplicationUser> signInManager, 
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            ILogger<AuthController> logger,
            HospitalDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// OAuth2/OpenID Connect token endpoint
        /// Handles password and refresh_token grant types
        /// </summary>
        [HttpPost("token")]
        [Consumes("application/x-www-form-urlencoded")]
        [Produces("application/json")]
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            // Handle Password Grant Type (username/password login)
            if (request.IsPasswordGrantType())
            {
                return await HandlePasswordGrantType(request);
            }

            // Handle Refresh Token Grant Type
            if (request.IsRefreshTokenGrantType())
            {
                return await HandleRefreshTokenGrantType(request);
            }

            // Handle Client Credentials Grant Type (machine-to-machine)
            if (request.IsClientCredentialsGrantType())
            {
                return await HandleClientCredentialsGrantType(request);
            }

            // Unsupported grant type
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.UnsupportedGrantType,
                ErrorDescription = "The specified grant type is not supported."
            });
        }

        /// <summary>
        /// Handles password grant type authentication
        /// </summary>
        private async Task<IActionResult> HandlePasswordGrantType(OpenIddictRequest request)
        {
            // Find user by username/email
            var user = await _userManager.FindByNameAsync(request.Username) 
                ?? await _userManager.FindByEmailAsync(request.Username);

            if (user == null)
            {
                _logger.LogWarning("Login failed: User {Username} not found", request.Username);
                return CreateInvalidGrantError("The username/password couple is invalid.");
            }

            // Check if email is confirmed (optional requirement)
            if (_configuration.GetValue<bool>("Authentication:RequireConfirmedEmail") && !user.EmailConfirmed)
            {
                _logger.LogWarning("Login failed: Email not confirmed for user {Username}", request.Username);
                return CreateInvalidGrantError("Email confirmation is required.");
            }

            // Check if account is locked
            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning("Login failed: Account locked for user {Username}", request.Username);
                return CreateInvalidGrantError("The account is locked out.");
            }

            // Validate the password
            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            
            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("Account locked out for user {Username} after failed login", request.Username);
                    return CreateInvalidGrantError("The account has been locked out due to multiple failed login attempts.");
                }
                
                if (result.RequiresTwoFactor)
                {
                    _logger.LogInformation("2FA required for user {Username}", request.Username);
                    return CreateInvalidGrantError("Two-factor authentication is required.");
                }

                _logger.LogWarning("Invalid password for user {Username}", request.Username);
                return CreateInvalidGrantError("The username/password couple is invalid.");
            }

            // Create the claims principal
            var principal = await CreateClaimsPrincipalAsync(user, request.GetScopes());

            _logger.LogInformation("User {Username} logged in successfully", request.Username);

            // Return the sign-in result with tokens
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Handles refresh token grant type
        /// </summary>
        private async Task<IActionResult> HandleRefreshTokenGrantType(OpenIddictRequest request)
        {
            // Retrieve the claims principal stored in the refresh token
            var info = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            if (!info.Succeeded)
            {
                return CreateInvalidGrantError("The refresh token is no longer valid.");
            }

            // Retrieve the user profile corresponding to the refresh token
            var user = await _userManager.GetUserAsync(info.Principal);
            if (user == null)
            {
                return CreateInvalidGrantError("The refresh token is no longer valid.");
            }

            // Ensure the user is still allowed to sign in
            if (!await _signInManager.CanSignInAsync(user))
            {
                return CreateInvalidGrantError("The user is no longer allowed to sign in.");
            }

            // Check if account is locked
            if (await _userManager.IsLockedOutAsync(user))
            {
                return CreateInvalidGrantError("The account is locked out.");
            }

            // Create a new claims principal with updated claims
            var principal = await CreateClaimsPrincipalAsync(user, info.Principal.GetScopes());

            _logger.LogInformation("Refresh token used successfully for user {Username}", user.UserName);

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Handles client credentials grant type (for machine-to-machine auth)
        /// </summary>
        private async Task<IActionResult> HandleClientCredentialsGrantType(OpenIddictRequest request)
        {
            // Note: Client authentication is handled by OpenIddict middleware
            // Here we just create a principal for the client application

            var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            
            // Add client_id as subject
            identity.AddClaim(Claims.Subject, request.ClientId);
            
            // Add client-specific claims
            identity.AddClaim("client_type", "service");
            identity.AddClaim(Claims.Scope, "api");

            var principal = new ClaimsPrincipal(identity);
            
            principal.SetScopes(request.GetScopes());
            principal.SetResources(await GetResourcesAsync(request.GetScopes()));

            foreach (var claim in principal.Claims)
            {
                claim.SetDestinations(GetDestinations(claim, principal));
            }

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Creates a claims principal for a user with all necessary claims
        /// </summary>
        private async Task<ClaimsPrincipal> CreateClaimsPrincipalAsync(ApplicationUser user, ImmutableArray<string> scopes)
        {
            // Create the base principal from Identity
            var principal = await _signInManager.CreateUserPrincipalAsync(user);

            // Set standard OpenID Connect claims
            principal.SetClaim(Claims.Subject, user.Id.ToString());
            principal.SetClaim(Claims.Email, user.Email);
            principal.SetClaim(Claims.Name, user.UserName);
            principal.SetClaim(Claims.PreferredUsername, user.UserName);
            
            // Add additional profile claims
            if (!string.IsNullOrEmpty(user.FullName))
            {
                principal.SetClaim(Claims.GivenName, user.FullName.Split(' ').FirstOrDefault());
                principal.SetClaim(Claims.FamilyName, user.FullName.Split(' ').LastOrDefault());
                principal.SetClaim("full_name", user.FullName);
            }

            // Add custom claims
            principal.SetClaim("email_verified", user.EmailConfirmed.ToString().ToLower());
            principal.SetClaim("phone_number_verified", user.PhoneNumberConfirmed.ToString().ToLower());
            
            if (!string.IsNullOrEmpty(user.PhoneNumber))
            {
                principal.SetClaim(Claims.PhoneNumber, user.PhoneNumber);
            }

            // Add role claims
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Any())
            {
                principal.SetClaims(Claims.Role, roles.ToImmutableArray());
                
                // Add role-specific claims based on user role
                await AddRoleSpecificClaimsAsync(principal, user, roles);
            }

            // Add user claims from database
            var userClaims = await _userManager.GetClaimsAsync(user);
            foreach (var claim in userClaims)
            {
                principal.SetClaim(claim.Type, claim.Value);
            }

            // Set the scopes granted to the user
            principal.SetScopes(scopes);

            // Set the resources associated with the scopes
            principal.SetResources(await GetResourcesAsync(scopes));

            // Set destinations for each claim
            foreach (var claim in principal.Claims)
            {
                claim.SetDestinations(GetDestinations(claim, principal));
            }

            return principal;
        }

        /// <summary>
        /// Adds role-specific claims based on user's roles
        /// </summary>
        private async Task AddRoleSpecificClaimsAsync(ClaimsPrincipal principal, ApplicationUser user, IList<string> roles)
        {
            // Add doctor-specific claims
            if (roles.Contains("Doctor"))
            {
                var doctor = await _context.Doctors
                    .FirstOrDefaultAsync(d => d.UserId == user.Id);
                
                if (doctor != null)
                {
                    principal.SetClaim("doctor_id", doctor.Id.ToString());
                    principal.SetClaim("license_number", doctor.LicenseNumber ?? "");
                    principal.SetClaim("specialization", doctor.Specialization ?? "");
                    principal.SetClaim("department", doctor.Department ?? "");
                    principal.SetClaim("employee_id", doctor.EmployeeId ?? "");
                    principal.SetClaim("can_prescribe", "true");
                }
            }

            // Add patient-specific claims
            if (roles.Contains("Patient"))
            {
                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.UserId == user.Id);
                
                if (patient != null)
                {
                    principal.SetClaim("patient_id", patient.Id.ToString());
                    principal.SetClaim("insurance_number", patient.InsuranceNumber ?? "");
                    
                    if (patient.DateOfBirth.HasValue)
                    {
                        principal.SetClaim("date_of_birth", patient.DateOfBirth.Value.ToString("yyyy-MM-dd"));
                        var age = DateTime.Today.Year - patient.DateOfBirth.Value.Year;
                        if (patient.DateOfBirth.Value.Date > DateTime.Today.AddYears(-age)) age--;
                        principal.SetClaim("age", age.ToString());
                    }
                }
            }

            // Add admin-specific claims
            if (roles.Contains("Admin"))
            {
                principal.SetClaim("admin_level", "full");
                principal.SetClaim("can_manage_users", "true");
                principal.SetClaim("can_view_reports", "true");
            }
        }

        /// <summary>
        /// Gets the resources associated with the specified scopes
        /// </summary>
        private async Task<ImmutableArray<string>> GetResourcesAsync(ImmutableArray<string> scopes)
        {
            // In a real application, you might look these up from a database
            var resources = new List<string>();

            if (scopes.Contains("api"))
            {
                resources.Add("hospital_api");
            }

            if (scopes.Contains("appointments"))
            {
                resources.Add("appointment_service");
            }

            if (scopes.Contains("medical_records"))
            {
                resources.Add("medical_records_service");
            }

            return resources.ToImmutableArray();
        }

        /// <summary>
        /// Determines the destinations for each claim (access_token, id_token, or both)
        /// </summary>
        private IEnumerable<string> GetDestinations(Claim claim, ClaimsPrincipal principal)
        {
            switch (claim.Type)
            {
                // Claims that go in both access token and ID token
                case Claims.Name:
                case Claims.PreferredUsername:
                    yield return Destinations.AccessToken;
                    
                    if (principal.HasScope(Scopes.Profile))
                        yield return Destinations.IdentityToken;
                    
                    yield break;

                case Claims.Email:
                case "email_verified":
                    yield return Destinations.AccessToken;
                    
                    if (principal.HasScope(Scopes.Email))
                        yield return Destinations.IdentityToken;
                    
                    yield break;

                case Claims.Role:
                case "department":
                case "employee_id":
                case "license_number":
                case "patient_id":
                    // Roles and work-related claims only go in access token
                    yield return Destinations.AccessToken;
                    
                    if (principal.HasScope(Scopes.Roles))
                        yield return Destinations.IdentityToken;
                    
                    yield break;

                // Claims that only go in the ID token
                case Claims.GivenName:
                case Claims.FamilyName:
                case Claims.Birthdate:
                case Claims.PhoneNumber:
                case "full_name":
                    if (principal.HasScope(Scopes.Profile))
                        yield return Destinations.IdentityToken;
                    
                    yield break;

                // Never include sensitive claims
                case "AspNet.Identity.SecurityStamp":
                case "insurance_number":
                case "social_security_number":
                    // Never include these in tokens
                    yield break;

                // Default: only include in access token
                default:
                    yield return Destinations.AccessToken;
                    yield break;
            }
        }

        /// <summary>
        /// Helper method to create an invalid grant error response
        /// </summary>
        private IActionResult CreateInvalidGrantError(string description)
        {
            var properties = new AuthenticationProperties(new Dictionary<string, string>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
            });

            return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Logout endpoint - revokes refresh tokens
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            // Ask OpenIddict to revoke the refresh token
            await HttpContext.SignOutAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            // Sign out of the Identity cookie authentication
            await _signInManager.SignOutAsync();

            _logger.LogInformation("User {Username} logged out", User.Identity.Name);

            return Ok(new { message = "Logged out successfully" });
        }

        /// <summary>
        /// User info endpoint - returns claims about the authenticated user
        /// </summary>
        [HttpGet("userinfo")]
        [Authorize]
        [Produces("application/json")]
        public async Task<IActionResult> UserInfo()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var claims = new Dictionary<string, object>();

            // Add standard claims
            claims[Claims.Subject] = user.Id.ToString();
            claims[Claims.Name] = user.UserName;
            claims[Claims.Email] = user.Email;
            claims["email_verified"] = user.EmailConfirmed;

            // Add profile claims if profile scope was requested
            if (User.HasScope(Scopes.Profile))
            {
                claims["full_name"] = user.FullName;
                claims[Claims.PreferredUsername] = user.UserName;
                
                // Load patient data if user is a patient
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Patient"))
                {
                    var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
                    if (patient?.DateOfBirth.HasValue == true)
                    {
                        claims[Claims.Birthdate] = patient.DateOfBirth.Value.ToString("yyyy-MM-dd");
                    }
                }
            }

            // Add role claims
            if (User.HasScope(Scopes.Roles))
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Any())
                {
                    claims[Claims.Role] = roles;
                }
            }

            return Ok(claims);
        }

        /// <summary>
        /// Introspection endpoint - validates tokens
        /// </summary>
        [HttpPost("introspect")]
        [Produces("application/json")]
        public async Task<IActionResult> Introspect()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            // Check if token is valid
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            
            if (!result.Succeeded)
            {
                return Ok(new
                {
                    active = false
                });
            }

            // Return token information
            var claims = result.Principal.Claims.ToDictionary(
                claim => claim.Type,
                claim => claim.Value);

            return Ok(new
            {
                active = true,
                claims = claims,
                scope = result.Principal.GetScopes().ToArray(),
                client_id = result.Principal.GetClaim(Claims.ClientId),
                token_type = "Bearer",
                exp = result.Principal.GetClaim(Claims.ExpiresAt),
                iat = result.Principal.GetClaim(Claims.IssuedAt)
            });
        }
    }
}