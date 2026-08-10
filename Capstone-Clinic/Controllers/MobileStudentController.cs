using Capstone_Clinic.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Capstone_Clinic.Controllers;

[ApiController]
[Route("api/mobile/student")]
[Authorize(AuthenticationSchemes = "MobileJwt")]
public class MobileStudentController : ControllerBase
{
    private readonly AppDbContext _context;

    public MobileStudentController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var studentIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(studentIdClaim, out int studentId))
        {
            return Unauthorized(new
            {
                message = "Invalid student authentication."
            });
        }

        var student = await _context.Students
            .FirstOrDefaultAsync(s =>
                s.StudentId == studentId);

        if (student == null)
        {
            return NotFound(new
            {
                message = "Student record not found."
            });
        }

        return Ok(new
        {
            studentId = student.StudentId,
            studentNumber = student.StudentNumber,
            fullName = student.FullName,
            dateOfBirth = student.DateOfBirth,
            course = student.Course,
            yearLevel = student.YearLevel,
            medicalRequirementStatus =
                student.MedicalRequirementStatus
        });
    }
}