using HospitalBooking.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace HospitalBooking.Infrastructure.Persistence.Seed
{
    public static class UserSeeder
    {
        public static async Task SeedAsync(UserManager<ApplicationUser> userManager)
        {
            if (await userManager.FindByEmailAsync("admin@hospital.com") == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = "admin@hospital.com",
                    Email = "admin@hospital.com",
                    FullName = "System Administrator",
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(admin, "Admin123$"); // strong test password
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}
