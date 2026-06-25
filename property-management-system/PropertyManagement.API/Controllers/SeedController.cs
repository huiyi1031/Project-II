using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.API.Models.Entities;
using PropertyManagement.API.Models.Enums;

namespace PropertyManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SeedController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SeedController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("users")]
        public async Task<IActionResult> SeedUsers()
        {
            // Check if users already exist
            if (await _context.UserAccounts.AnyAsync())
            {
                return Ok(new { message = "Users already exist!" });
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword("Test123!");

            var users = new List<UserAccount>
            {
                new UserAccount
                {
                    Email = "admin@demo.com",
                    PasswordHash = passwordHash,
                    RoleType = RoleType.PropertyManager,
                    AccountStatus = AccountStatus.Active,
                    CreatedAt = DateTime.UtcNow
                },
                new UserAccount
                {
                    Email = "tech@demo.com",
                    PasswordHash = passwordHash,
                    RoleType = RoleType.Technician,
                    AccountStatus = AccountStatus.Active,
                    CreatedAt = DateTime.UtcNow
                },
                new UserAccount
                {
                    Email = "owner@demo.com",
                    PasswordHash = passwordHash,
                    RoleType = RoleType.Occupant,
                    AccountStatus = AccountStatus.Active,
                    CreatedAt = DateTime.UtcNow
                },
                new UserAccount
                {
                    Email = "tenant@demo.com",
                    PasswordHash = passwordHash,
                    RoleType = RoleType.Occupant,
                    AccountStatus = AccountStatus.Active,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _context.UserAccounts.AddRange(users);
            await _context.SaveChangesAsync();

            // Also create Occupant records for owner and tenant
            var owner = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Email == "owner@demo.com");
            var tenant = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Email == "tenant@demo.com");

            if (owner != null)
            {
                _context.Occupants.Add(new Occupant
                {
                    UserAccountId = owner.Id,
                    FullName = "Demo Owner",
                    OccupantType = OccupantType.Owner,
                    OccupantStatus = "Active"
                });
            }

            if (tenant != null)
            {
                _context.Occupants.Add(new Occupant
                {
                    UserAccountId = tenant.Id,
                    FullName = "Demo Tenant",
                    OccupantType = OccupantType.Tenant,
                    OccupantStatus = "Active"
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Users seeded successfully!",
                users = users.Select(u => new { u.Email, u.RoleType })
            });
        }
    }
}