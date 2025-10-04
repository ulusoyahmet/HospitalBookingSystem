using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientRecordsController : ControllerBase
    {

        private readonly IAuthorizationService _authorizationService;

        public PatientRecordsController(IAuthorizationService authorizationService)
        {
            _authorizationService = authorizationService;
        }

        // Using policy attribute
        [HttpGet("admin-dashboard")]
        [Authorize(Policy = "RequireAdminRole")]
        public IActionResult GetAdminDashboard()
        {
            return Ok("Admin dashboard data");
        }

        // Using multiple policies (must satisfy ALL)
        [HttpPut("patient/{patientId}/record")]
        [Authorize(Policy = "CanEditPatientRecords")]
        [Authorize(Policy = "WorkingHours")]
        public IActionResult UpdatePatientRecord(int patientId)
        {
            return Ok($"Updated patient {patientId} record");
        }

        // Simple role-based (without policy)
        [HttpGet("doctor-schedule")]
        [Authorize(Roles = "Doctor,Admin")]
        public IActionResult GetDoctorSchedule()
        {
            return Ok("Doctor schedule");
        }

        // Programmatic authorization
        [HttpDelete("patient/{patientId}")]
        public async Task<IActionResult> DeletePatientRecord(int patientId)
        {
            var authResult = await _authorizationService.AuthorizeAsync(
                User, 
                patientId, // Resource being accessed
                "CanEditPatientRecords");

            if (!authResult.Succeeded)
            {
                return Forbid();
            }

            return Ok($"Deleted patient {patientId}");
        }
    }
}



