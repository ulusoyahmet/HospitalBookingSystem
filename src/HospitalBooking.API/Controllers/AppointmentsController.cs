using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalBooking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentsController : ControllerBase
    {
        [HttpGet]
        [Authorize(Roles = "Admin,Doctor,Patient")]
        public IActionResult GetAppointments()
        {
            return Ok(new { message = "This is a protected endpoint." });
        }
    }
}
