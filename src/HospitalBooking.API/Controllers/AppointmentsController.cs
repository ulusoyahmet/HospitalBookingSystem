using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HospitalBooking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentsController : ControllerBase
    {
        // Patients can view their own appointments
        [HttpGet("my-appointments")]
        [Authorize(Roles = "Patient,Doctor,Admin")]
        public IActionResult GetMyAppointments()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Logic would differ based on role
            return Ok(new 
            { 
                message = $"Appointments for user {userId} with role {userRole}",
                appointments = new[] 
                {
                    new { id = 1, date = "2024-01-15", doctor = "Dr. Smith" },
                    new { id = 2, date = "2024-01-20", doctor = "Dr. Johnson" }
                }
            });
        }

        // Only doctors can see all appointments
        [HttpGet("all-appointments")]
        [Authorize(Roles = "Doctor,Admin")]
        public IActionResult GetAllAppointments()
        {
            return Ok(new 
            { 
                message = "All appointments in the system",
                appointments = new[]
                {
                    new { id = 1, patient = "John Doe", date = "2024-01-15" },
                    new { id = 2, patient = "Jane Smith", date = "2024-01-20" }
                }
            });
        }

        // Only admin can delete appointments
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteAppointment(int id)
        {
            return Ok(new { message = $"Appointment {id} deleted by admin" });
        }
    }
}

