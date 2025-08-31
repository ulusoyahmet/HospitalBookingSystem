using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Core;


namespace HospitalBooking.Infrastructure.Persistence.Seed
{
    public static class OpenIddictSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

            if (await manager.FindByClientIdAsync("swagger-client") == null)
            {
                await manager.CreateAsync(new OpenIddictApplicationDescriptor
                {
                    ClientId = "swagger-client",
                    ClientSecret = "secret-secret-secret",
                    DisplayName = "Swagger UI Client",
                    Permissions =
                {
                    // Grant types
                    OpenIddictConstants.Permissions.GrantTypes.Password,
                    OpenIddictConstants.Permissions.GrantTypes.RefreshToken,

                    // Endpoints
                    OpenIddictConstants.Permissions.Endpoints.Token,

                    // Scopes
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Permissions.Prefixes.Scope + "api"
                }
                });
            }
        }
    }

}
