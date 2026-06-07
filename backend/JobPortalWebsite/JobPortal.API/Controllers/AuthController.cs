using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JobPortal.API.Data;
using JobPortal.API.DTOs;
using JobPortal.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace JobPortal.API.Controllers;


[ApiController]
[Route("api/[controller]")]

public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(User user)
    {
        try
        {
            if (string.IsNullOrEmpty(user.Name) || string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.Password) || user.RoleId <= 0)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Name, Email, Password and Role are required",
                    Data = null
                });
            }
            var existingUser = _context.Users.Any(u => u.Email.ToLower() == user.Email.ToLower());
            if (existingUser)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = "User already exists with this email",
                    Data = null
                });
            }
            var roleExists = _context.Roles.Any(x => x.Id == user.RoleId);
            if (!roleExists)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Invalid role",
                    Data = null
                });
            }
            //HashPassword is a method in BCrypt.Net that hashes the password
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            const int recruiterRoleId = 2;
            const int candidateRoleId = 3;

            if (user.RoleId == candidateRoleId)
            {
                _context.CandidateProfiles.Add(new CandidateProfile
                {
                    UserId = user.Id,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else if (user.RoleId == recruiterRoleId)
            {
                var company = new Company
                {
                    Name = null,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Companies.Add(company);
                await _context.SaveChangesAsync();

                _context.EmployerProfiles.Add(new EmployerProfile
                {
                    UserId = user.Id,
                    CompanyId = company.Id,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = "User registered successfully",
                Data = user.Id.ToString()
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>
            {
                Success = false,
                Message = "Something went wrong while registering user",
                Data = null
            });
        }
    }

    [HttpPost("login")]
    public IActionResult Login(User loginUser)
    {
        if (string.IsNullOrEmpty(loginUser.Email) || string.IsNullOrEmpty(loginUser.Password))
        {
            return BadRequest(new ApiResponse<string>
            {
                Success = false,
                Message = "Email and Password are required",
                Data = null
            });
        }

        var user = _context.Users.FirstOrDefault(u => u.Email.ToLower() == loginUser.Email.ToLower());
        if (user == null || !BCrypt.Net.BCrypt.Verify(loginUser.Password, user.Password))
        {
            return Unauthorized(new ApiResponse<string>
            {
                Success = false,
                Message = "Invalid email or password",
                Data = null
            });
        }

        var token = GenerateToken(user);
        return Ok(new ApiResponse<string>
        {
            Success = true,
            Message = "Login successful",
            Data = token
        });
    }
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        try
        {
            if (request == null
                || string.IsNullOrWhiteSpace(request.CurrentPassword)
                || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Current password and new password are required",
                    Data = null
                });
            }

            if (request.NewPassword.Length < 6)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = "New password must be at least 6 characters",
                    Data = null
                });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Unauthorized access",
                    Data = null
                });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.Password))
            {
                return Unauthorized(new ApiResponse<string>
                {
                    Success = false,
                    Message = "User not found",
                    Data = null
                });
            }

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password))
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Current password is incorrect",
                    Data = null
                });
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = "Password updated successfully",
                Data = null
            });
        }
        catch
        {
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Something went wrong while changing password",
                Data = null
            });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Token is valid",
            Data = new { userId, email, role }
        });
    }
    [Authorize(Roles = "2")]
    [HttpGet("employer-only")]
    public IActionResult EmployerOnly()
    {
        return Ok(new ApiResponse<string>
        {
            Success = true,
            Message = "Welcome Employer",
            Data = "You can access employer features."
        });
    }
    private string GenerateToken(User user)
    {
        //gets jwt settings from appsettings.json
        var jwtSettings = _configuration.GetSection("Jwt");
        //convert secret key to bytes
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
        //tells how to sign token - hmacSha256 algorithm
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        //created claim - mini user info
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name ?? string.Empty),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Role, user.RoleId.ToString())
        };

        //actual jwt object
        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],  //JobPortalAPI
            audience: jwtSettings["Audience"], //JobPortalClient
            claims: claims, //user info
            expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpiryMinutes"])), //60 minutes
            signingCredentials: credentials //signature key/algorithm
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

