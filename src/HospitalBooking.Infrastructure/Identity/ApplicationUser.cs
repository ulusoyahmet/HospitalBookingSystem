using Microsoft.AspNetCore.Identity;

namespace HospitalBooking.Infrastructure.Identity;

public class ApplicationUser: IdentityUser<Guid>
{
    public string FullName { get; set; }
}
