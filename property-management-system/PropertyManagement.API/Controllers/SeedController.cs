using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.API.Models.Entities;
using PropertyManagement.API.Models.Enums;
using PropertyManagement.API.Services;

namespace PropertyManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SeedController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public SeedController(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        /// <summary>
        /// Seeds the database with test data. Call this ONCE after running migrations.
        /// WARNING: This clears all existing user/occupant/technician/manager data first.
        /// </summary>
        [HttpPost("run")]
        public async Task<IActionResult> Run()
        {
            var log = new List<string>();

            try
            {
                // ── 1. Clear dependent data first (FK order) ─────────────────────
                _context.Contracts.RemoveRange(_context.Contracts);
                _context.MaintenanceRequests.RemoveRange(_context.MaintenanceRequests);
                _context.Occupants.RemoveRange(_context.Occupants);
                _context.Technicians.RemoveRange(_context.Technicians);
                _context.PropertyManagers.RemoveRange(_context.PropertyManagers);
                _context.UserAccounts.RemoveRange(_context.UserAccounts);
                _context.PropertyUnits.RemoveRange(_context.PropertyUnits);
                _context.Properties.RemoveRange(_context.Properties);
                await _context.SaveChangesAsync();
                log.Add("✅ Cleared existing data");

                // ── 2. Property Manager (Active, no email needed — use known password) ──
                var managerTempPw = "Manager@123";
                var managerUser = new UserAccount
                {
                    Email = "nghy-wm24@student.tarc.edu.my",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(managerTempPw),
                    RoleType = RoleType.PropertyManager,
                    AccountStatus = AccountStatus.Active
                };
                _context.UserAccounts.Add(managerUser);
                await _context.SaveChangesAsync();

                _context.PropertyManagers.Add(new PropertyManager
                {
                    UserAccountId = managerUser.Id,
                    FullName = "Ng Hui Yi",
                    ContactNumber = "012-3456789",
                    Gender = "F",
                    Position = "Lead Manager"
                });
                await _context.SaveChangesAsync();
                log.Add($"✅ Manager: nghy-wm24@student.tarc.edu.my | Password: {managerTempPw}");

                // ── 3. Technician (Pending — real email sent) ─────────────────────
                var techTempPw = GenerateTempPassword();
                var techUser = new UserAccount
                {
                    Email = "nghy1031@gmail.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(techTempPw),
                    RoleType = RoleType.Technician,
                    AccountStatus = AccountStatus.Pending
                };
                _context.UserAccounts.Add(techUser);
                await _context.SaveChangesAsync();

                _context.Technicians.Add(new Technician
                {
                    UserAccountId = techUser.Id,
                    FullName = "Daniel Tan",
                    ContactNumber = "013-9876543",
                    Gender = "M",
                    ExperienceLevel = "Senior",
                    AvailabilityStatus = "Available",
                    Ranking = 1m
                });
                await _context.SaveChangesAsync();

                await _emailService.SendEmailAsync(
                    techUser.Email,
                    "Welcome to Property Management System — Activate Your Account",
                    BuildActivationEmail("Daniel Tan", techUser.Email, techTempPw, "Technician")
                );
                log.Add($"✅ Technician: nghy1031@gmail.com | Temp Password: {techTempPw} | Activation email sent!");

                // ── 4. Property ───────────────────────────────────────────────────
                var property = new Property
                {
                    PropertyName = "Sunway Nexis Residences",
                    PropertyType = "Condominium",
                    Address = "Jalan PJU 5/1, Kota Damansara",
                    City = "Petaling Jaya",
                    State = "Selangor",
                    Postcode = "47810"
                };
                _context.Properties.Add(property);
                await _context.SaveChangesAsync();
                log.Add($"✅ Property: {property.PropertyName} (ID: {property.Id})");

                // ── 5. Units ──────────────────────────────────────────────────────
                var unit1 = new PropertyUnit
                {
                    PropertyId = property.Id,
                    UnitNumber = "A-12-03",
                    FloorLevel = "12",
                    Block = "A",
                    UnitType = "Studio",
                    AreaSqft = 650,
                    MaxOccupants = 4,
                    CurrentOccupants = 0,
                    Status = "Vacant"
                };
                var unit2 = new PropertyUnit
                {
                    PropertyId = property.Id,
                    UnitNumber = "B-05-11",
                    FloorLevel = "5",
                    Block = "B",
                    UnitType = "2-Bedroom",
                    AreaSqft = 900,
                    MaxOccupants = 6,
                    CurrentOccupants = 0,
                    Status = "Vacant"
                };
                _context.PropertyUnits.AddRange(unit1, unit2);
                await _context.SaveChangesAsync();
                log.Add($"✅ Units: A-12-03, B-05-11");

                // ── 6. Owner (Pending — using IC, no direct email account) ────────
                // The owner will use the IC bypass flow to set up their account.
                // We seed the Occupant record with IC but with a placeholder email.
                var ownerUser = new UserAccount
                {
                    Email = "owner.placeholder@pms.local",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                    RoleType = RoleType.Occupant,
                    AccountStatus = AccountStatus.Pending
                };
                _context.UserAccounts.Add(ownerUser);
                await _context.SaveChangesAsync();

                var ownerOccupant = new Occupant
                {
                    UserAccountId = ownerUser.Id,
                    FullName = "Ahmad bin Razak",
                    IdentificationNo = "900101-10-1234",
                    ContactNumber = "011-2233445",
                    Gender = "M",
                    OccupantType = OccupantType.Owner,
                    OccupantStatus = "Active"
                };
                _context.Occupants.Add(ownerOccupant);
                await _context.SaveChangesAsync();
                log.Add("✅ Owner: IC=900101-10-1234 | Use IC bypass flow to set email+password");

                // ── 7. Tenant (Pending — real student email) ──────────────────────
                var tenantTempPw = GenerateTempPassword();
                var tenantUser = new UserAccount
                {
                    Email = "nghy-wm24@student.tarc.edu.my",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(tenantTempPw),
                    RoleType = RoleType.Occupant,
                    AccountStatus = AccountStatus.Pending
                };
                // Note: If nghy-wm24 is also the Manager, we skip this to avoid duplicate email.
                // In production the Owner would register a different tenant email.
                // For testing, we note the temp password in the log.
                log.Add($"⚠️  Tenant temp password (not inserted, same email as manager): {tenantTempPw}");

                return Ok(new
                {
                    message = "✅ Seed complete! Database is ready for testing.",
                    accounts = new[]
                    {
                        new { role = "Property Manager", email = "nghy-wm24@student.tarc.edu.my", password = managerTempPw, status = "Active — login directly" },
                        new { role = "Technician",        email = "nghy1031@gmail.com",            password = techTempPw,    status = "Pending — check nghy1031@gmail.com for temp password then set new password" },
                        new { role = "Owner",             email = "(none yet)",                    password = "Use IC bypass", status = "IC: 900101-10-1234 → set your email → set password" },
                    },
                    log
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Seed failed: {ex.Message}", detail = ex.InnerException?.Message });
            }
        }

        private static string GenerateTempPassword()
        {
            var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            return $"TEMP-{BitConverter.ToString(bytes).Replace("-", "")}";
        }

        private static string BuildActivationEmail(string name, string email, string tempPw, string role)
        {
            return $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family: Segoe UI, Arial, sans-serif; background:#f5f9ff; padding:20px;'>
  <div style='max-width:560px; margin:0 auto; background:#fff; border-radius:16px; border:1px solid #dbe7fb; overflow:hidden;'>
    <div style='background:linear-gradient(135deg,#0b2d5c,#1f5fae); padding:32px; color:#fff; text-align:center;'>
      <h1 style='margin:0; font-size:22px;'>🏢 Property Management System</h1>
      <p style='margin:8px 0 0; color:#cde0ff; font-size:14px;'>Account Activation</p>
    </div>
    <div style='padding:32px;'>
      <h2 style='color:#0b2d5c; margin-top:0;'>Welcome, {name}!</h2>
      <p style='color:#3d546e;'>Your <strong>{role}</strong> account has been created by the Property Management Office.</p>
      <p style='color:#3d546e;'>Use the credentials below to log in for the first time. You will be required to set a new permanent password immediately.</p>
      
      <div style='background:#eaf3ff; border-radius:12px; padding:20px; margin:20px 0; border-left:4px solid #1f5fae;'>
        <p style='margin:0 0 8px; color:#6b7a90; font-size:12px; text-transform:uppercase; letter-spacing:1px;'>Your Login Credentials</p>
        <p style='margin:4px 0;'><strong>Email:</strong> {email}</p>
        <p style='margin:4px 0;'><strong>Temporary Password:</strong> <code style='background:#fff; padding:2px 8px; border-radius:6px; font-size:16px; border:1px solid #dbe7fb;'>{tempPw}</code></p>
      </div>
      
      <div style='background:#fff8e1; border-radius:12px; padding:16px; border-left:4px solid #e2a400;'>
        <p style='margin:0; color:#7a5c00; font-size:13px;'>⚠️ This temporary password expires. Please log in as soon as possible and set your permanent password.</p>
      </div>
      
      <div style='margin-top:24px; text-align:center;'>
        <a href='http://localhost:4201/auth/login' 
           style='display:inline-block; background:linear-gradient(135deg,#1f5fae,#2f7de0); color:#fff; padding:12px 32px; border-radius:10px; text-decoration:none; font-weight:600; font-size:15px;'>
          Login Now →
        </a>
      </div>
    </div>
    <div style='background:#f5f9ff; padding:16px; text-align:center; font-size:11px; color:#6b7a90;'>
      Property Management System &mdash; Do not share your credentials with anyone.
    </div>
  </div>
</body>
</html>";
        }
    }
}