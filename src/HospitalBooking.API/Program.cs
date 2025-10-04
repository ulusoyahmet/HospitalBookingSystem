using HospitalBooking.Infrastructure.Identity;
using HospitalBooking.Infrastructure.Persistence;
using HospitalBooking.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using HospitalBooking.API.Policies;
using Microsoft.AspNetCore.Authorization;
using HospitalBooking.API.Policies.Handlers;
using HospitalBooking.API.Policies.Requirements;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


// Add HttpContextAccessor for handlers that need it
builder.Services.AddHttpContextAccessor();

// Configure authorization policies
builder.Services.ConfigureAuthorizationPolicies(); // Our extension method

// Register custom handlers
builder.Services.AddScoped<IAuthorizationHandler, CanEditPatientRecordsHandler>();
builder.Services.AddScoped<IAuthorizationHandler, WorkingHoursHandler>();
builder.Services.AddScoped<IAuthorizationHandler, MinimumAgeHandler>();

// 1. DbContext
builder.Services.AddDbContext<HospitalDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));

    // Register OpenIddict entities in the same DbContext
    options.UseOpenIddict();
});

// 2. ASP.NET Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<HospitalDbContext>()
.AddDefaultTokenProviders();


// 3. OpenIddict
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<HospitalDbContext>();
    })
    .AddServer(options =>
    {
        options.SetTokenEndpointUris("/connect/token");

        // Allow password + refresh token flows
        options.AllowPasswordFlow()
               .AllowRefreshTokenFlow();

        // Register the signing and encryption credentials.
        if (builder.Environment.IsDevelopment())
        {
            options.AddDevelopmentEncryptionCertificate()
                   .AddDevelopmentSigningCertificate();
        }
        else
        {
            
            // options.AddEncryptionCertificate(certificate)
            //        .AddSigningCertificate(certificate);

            
            options.AddEphemeralEncryptionKey()
                   .AddEphemeralSigningKey();
        }

        options.DisableAccessTokenEncryption();

        // Enable ASP.NET Core integration
        options.UseAspNetCore()
               .EnableTokenEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        options.UseAspNetCore();
        options.UseLocalServer();
    });



// 4. Authentication/Authorization
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
});


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Hospital Booking API", Version = "v1" });

    // Add JWT support
    options.AddSecurityDefinition("oauth2", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.OAuth2,
        Flows = new Microsoft.OpenApi.Models.OpenApiOAuthFlows
        {
            Password = new Microsoft.OpenApi.Models.OpenApiOAuthFlow
            {
                TokenUrl = new Uri("https://localhost:7063/connect/token", UriKind.Absolute),
                Scopes = new Dictionary<string, string>
                {
                    { "api", "Hospital Booking API" }
                }
            }
        }
    });


    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "oauth2"
                }
            },
            new[] { "api" }
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {

        options.OAuthClientId("swagger-client");
        options.OAuthClientSecret("secret-secret-secret");
        options.OAuthScopes("api");
        options.OAuthUsePkce();
    });
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    await RoleSeeder.SeedAsync(roleManager);
    await UserSeeder.SeedAsync(userManager);
    await OpenIddictSeeder.SeedAsync(services);
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
