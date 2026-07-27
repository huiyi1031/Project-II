using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.API.Models.DTOs;
using PropertyManagement.API.Models.Entities;
using PropertyManagement.API.Models.Enums;
using System.Security.Claims;

namespace PropertyManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public UsersController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out long userId))
                return userId;
            return 0;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var user = await _context.UserAccounts
                .Include(u => u.Occupant)
                .Include(u => u.PropertyManager)
                .Include(u => u.Technician)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound("User not found");

            var dto = new ProfileDto
            {
                UserAccountId = user.Id,
                Email = user.Email,
                Role = user.RoleType.ToString(),
                ProfilePictureUrl = user.ProfilePictureUrl
            };

            string? formatG(string? g)
            {
                if (string.IsNullOrWhiteSpace(g)) return null;
                var val = g.Trim().ToUpper();
                return val == "M" ? "Male" : (val == "F" ? "Female" : g);
            }

            if (user.Occupant != null)
            {
                dto.FullName = user.Occupant.FullName;
                dto.ContactNumber = user.Occupant.ContactNumber;
                dto.Gender = formatG(user.Occupant.Gender);
                dto.OccupantType = user.Occupant.OccupantType.ToString();
            }
            else if (user.PropertyManager != null)
            {
                dto.FullName = user.PropertyManager.FullName;
                dto.ContactNumber = user.PropertyManager.ContactNumber;
                dto.Gender = formatG(user.PropertyManager.Gender);
            }
            else if (user.Technician != null)
            {
                dto.FullName = user.Technician.FullName;
                dto.ContactNumber = user.Technician.ContactNumber;
                dto.Gender = formatG(user.Technician.Gender);
            }

            if (string.IsNullOrEmpty(dto.FullName))
            {
                dto.FullName = user.Email;
            }

            return Ok(dto);
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto request)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var user = await _context.UserAccounts
                .Include(u => u.Occupant)
                .Include(u => u.PropertyManager)
                .Include(u => u.Technician)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound("User not found");

            // Update email on UserAccount (all roles)
            if (!string.IsNullOrWhiteSpace(request.Email)
                && !string.Equals(user.Email, request.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                var emailTaken = await _context.UserAccounts
                    .AnyAsync(u => u.Email == request.Email.Trim().ToLower() && u.Id != userId && !u.IsDeleted);
                if (emailTaken)
                    return BadRequest(new { message = "This email is already in use by another account." });
                user.Email = request.Email.Trim().ToLower();
            }

            if (user.Occupant == null && user.RoleType == RoleType.Occupant)
            {
                user.Occupant = new Occupant { UserAccountId = user.Id, FullName = request.FullName ?? user.Email };
                _context.Occupants.Add(user.Occupant);
            }
            else if (user.PropertyManager == null && user.RoleType == RoleType.PropertyManager)
            {
                user.PropertyManager = new PropertyManager { UserAccountId = user.Id, FullName = request.FullName ?? user.Email };
                _context.PropertyManagers.Add(user.PropertyManager);
            }
            else if (user.Technician == null && user.RoleType == RoleType.Technician)
            {
                user.Technician = new Technician { UserAccountId = user.Id, FullName = request.FullName ?? user.Email };
                _context.Technicians.Add(user.Technician);
            }

            string? normGender = null;
            if (request.Gender != null)
            {
                var g = request.Gender.Trim().ToUpper();
                normGender = g.StartsWith("M") ? "M" : (g.StartsWith("F") ? "F" : (g.Length > 0 ? g.Substring(0, 1) : null));
            }

            if (user.Occupant != null)
            {
                if (request.FullName != null) user.Occupant.FullName = request.FullName;
                if (request.ContactNumber != null) user.Occupant.ContactNumber = request.ContactNumber;
                if (request.Gender != null) user.Occupant.Gender = normGender;
            }
            else if (user.PropertyManager != null)
            {
                if (request.FullName != null) user.PropertyManager.FullName = request.FullName;
                if (request.ContactNumber != null) user.PropertyManager.ContactNumber = request.ContactNumber;
                if (request.Gender != null) user.PropertyManager.Gender = normGender;
            }
            else if (user.Technician != null)
            {
                if (request.FullName != null) user.Technician.FullName = request.FullName;
                if (request.ContactNumber != null) user.Technician.ContactNumber = request.ContactNumber;
                if (request.Gender != null) user.Technician.Gender = normGender;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Profile updated successfully", email = user.Email });
        }

        [HttpPost("profile/picture")]
        public async Task<IActionResult> UploadProfilePicture(IFormFile file)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            var user = await _context.UserAccounts.FindAsync(userId);
            if (user == null) return NotFound("User not found");

            var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "profiles");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{userId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";
            var fileUrl = $"{baseUrl}/uploads/profiles/{fileName}";

            user.ProfilePictureUrl = fileUrl;
            await _context.SaveChangesAsync();

            return Ok(new { profilePictureUrl = fileUrl });
        }
    }
}
