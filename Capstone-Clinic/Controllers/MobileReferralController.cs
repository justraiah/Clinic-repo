
using Capstone_Clinic.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Capstone_Clinic.Controllers;

[ApiController]
[Route("api/mobile/student/referrals")]
[Authorize(AuthenticationSchemes = "MobileJwt")]
public class MobileReferralController : ControllerBase
{
    private readonly AppDbContext _context;

    public MobileReferralController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyReferrals()
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

        var referrals = await _context.Referrals
            .AsNoTracking()
            .Where(r =>
                r.ClinicPatient != null &&
                r.ClinicPatient.StudentId == studentId)
            .OrderByDescending(r => r.ReferralDate)
            .Select(r => new
            {
                referralId = r.ReferralId,
                clinicPatientId = r.ClinicPatientId,
                referralDate = r.ReferralDate,
                facilityName = r.FacilityName,
                reasonForReferral = r.ReasonForReferral,
                clinicalNotes = r.ClinicalNotes,
                status = r.Status,
                createdAt = r.CreatedAt
            })
            .ToListAsync();

        return Ok(referrals);
    }
}
