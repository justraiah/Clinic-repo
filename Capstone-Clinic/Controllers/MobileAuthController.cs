using Capstone_Clinic.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Capstone_Clinic.Controllers;

[ApiController]
[Route("api/mobile/auth")]
public class MobileAuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<Models.StudentAccount> _passwordHasher;
    private readonly IConfiguration _configuration;

    public MobileAuthController(
        AppDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;

        _passwordHasher =
            new PasswordHasher<Models.StudentAccount>();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] MobileLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.StudentNumber) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new
            {
                message = "Student Number and password are required."
            });
        }

        var studentNumber = request.StudentNumber.Trim();

        var student = await _context.Students
            .FirstOrDefaultAsync(
                s => s.StudentNumber == studentNumber);

        if (student == null)
        {
            return Unauthorized(new
            {
                message = "Invalid Student Number or password."
            });
        }

        var account = await _context.StudentAccounts
            .FirstOrDefaultAsync(
                a => a.StudentId == student.StudentId &&
                     a.IsActive);

        if (account == null)
        {
            return Unauthorized(new
            {
                message = "This student account is inactive."
            });
        }

        var passwordResult =
            _passwordHasher.VerifyHashedPassword(
                account,
                account.PasswordHash,
                request.Password);

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new
            {
                message = "Invalid Student Number or password."
            });
        }

        var claims = new List<Claim>
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                student.StudentId.ToString()),

            new Claim(
                ClaimTypes.Name,
                student.FullName),

            new Claim(
                "StudentNumber",
                student.StudentNumber)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"]!));

        var credentials =
            new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        var tokenString =
            new JwtSecurityTokenHandler()
                .WriteToken(token);

        return Ok(new
        {
            message = "Login successful.",
            token = tokenString,
            studentId = student.StudentId,
            studentNumber = student.StudentNumber,
            fullName = student.FullName
        });
    }
}

public class MobileLoginRequest
{
    public string StudentNumber { get; set; } = "";
    public string Password { get; set; } = "";
}