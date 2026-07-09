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
        private readonly string _appUrl;

        public SeedController(AppDbContext context, IEmailService emailService, IConfiguration configuration)
        {
            _context = context;
            _emailService = emailService;
            _appUrl = configuration["AppUrl"] ?? "http://localhost:4201";
        }

        [HttpPost("migrate-profile-pic")]
        public async Task<IActionResult> MigrateProfilePic()
        {
            try {
                await _context.Database.ExecuteSqlRawAsync("ALTER TABLE \"UserAccounts\" ADD COLUMN IF NOT EXISTS \"ProfilePictureUrl\" character varying(255) NULL;");
                await _context.Database.ExecuteSqlRawAsync("ALTER TABLE \"Occupants\" ADD COLUMN IF NOT EXISTS \"ParentOccupantId\" bigint NULL;");
                return Ok("Columns added successfully");
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("debug-users")]
        public async Task<IActionResult> DebugUsers()
        {
            var users = await _context.UserAccounts
                .Include(u => u.Occupant)
                .Include(u => u.PropertyManager)
                .Include(u => u.Technician)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.RoleType,
                    OccupantName = u.Occupant != null ? u.Occupant.FullName : null,
                    ManagerName = u.PropertyManager != null ? u.PropertyManager.FullName : null,
                    TechName = u.Technician != null ? u.Technician.FullName : null
                })
                .ToListAsync();

            return Ok(users);
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
                // ── 1. Clear dependent data first and reset IDs (PostgreSQL) ────────
                await _context.Database.ExecuteSqlRawAsync(@"
                    TRUNCATE TABLE 
                        ""Contracts"", 
                        ""MaintenanceRequests"", 
                        ""Occupants"", 
                        ""Technicians"", 
                        ""PropertyManagers"", 
                        ""UserAccounts"", 
                        ""PropertyUnits"", 
                        ""Properties"" 
                    RESTART IDENTITY CASCADE;
                ");
                log.Add("✅ Cleared existing data and reset all ID sequences to 1");

                // ── Helper ────────────────────────────────────────────────────────
                string Hash(string pw) => BCrypt.Net.BCrypt.HashPassword(pw);

                // ── 2. Add Properties & Units ─────────────────────────────────────
                var property = new Property { PropertyName = "Sunway Nexis Residences", PropertyType = "Condominium", Address = "Jalan PJU 5/1", City = "Petaling Jaya", State = "Selangor", Postcode = "47810" };
                _context.Properties.Add(property);
                await _context.SaveChangesAsync();

                var unit1 = new PropertyUnit { PropertyId = property.Id, UnitNumber = "A-12-03", FloorLevel = "12", Block = "A", UnitType = "Studio", AreaSqft = 650, MaxOccupants = 4, CurrentOccupants = 0, Status = "Vacant" };
                var unit2 = new PropertyUnit { PropertyId = property.Id, UnitNumber = "B-05-11", FloorLevel = "5", Block = "B", UnitType = "2-Bedroom", AreaSqft = 900, MaxOccupants = 6, CurrentOccupants = 0, Status = "Vacant" };
                _context.PropertyUnits.AddRange(unit1, unit2);
                await _context.SaveChangesAsync();

                // ── 3. Add Dummy Users ───────────────────────────────────────────
                var users = new List<object>();

                // Manager
                var mgr = new UserAccount { Email = "manager@test.com", PasswordHash = Hash("Manager@123"), RoleType = RoleType.PropertyManager, AccountStatus = AccountStatus.Active };
                _context.UserAccounts.Add(mgr);
                await _context.SaveChangesAsync();
                _context.PropertyManagers.Add(new PropertyManager { UserAccountId = mgr.Id, FullName = "Ng Hui Yi", ContactNumber = "012-3456789", Gender = "F", Position = "Lead Manager" });
                users.Add(new { role = "Manager", email = "manager@test.com", password = "Manager@123" });

                // Technicians
                var tech1 = new UserAccount { Email = "tech1@test.com", PasswordHash = Hash("Tech@123"), RoleType = RoleType.Technician, AccountStatus = AccountStatus.Active };
                var tech2 = new UserAccount { Email = "tech2@test.com", PasswordHash = Hash("Tech@123"), RoleType = RoleType.Technician, AccountStatus = AccountStatus.Active };
                _context.UserAccounts.AddRange(tech1, tech2);
                await _context.SaveChangesAsync();
                _context.Technicians.Add(new Technician { UserAccountId = tech1.Id, FullName = "Daniel Tan", ContactNumber = "013-9876543", Gender = "M", ExperienceLevel = "Senior", AvailabilityStatus = "Available", Ranking = 1m });
                _context.Technicians.Add(new Technician { UserAccountId = tech2.Id, FullName = "Ali Bin Abu", ContactNumber = "011-1234567", Gender = "M", ExperienceLevel = "Junior", AvailabilityStatus = "Available", Ranking = 0m });
                users.Add(new { role = "Technician", email = "tech1@test.com", password = "Tech@123" });
                users.Add(new { role = "Technician", email = "tech2@test.com", password = "Tech@123" });

                // Owners
                var owner1 = new UserAccount { Email = "owner1@test.com", PasswordHash = Hash("Owner@123"), RoleType = RoleType.Occupant, AccountStatus = AccountStatus.Active };
                var owner2 = new UserAccount { Email = "owner2@test.com", PasswordHash = Hash("Owner@123"), RoleType = RoleType.Occupant, AccountStatus = AccountStatus.Active };
                _context.UserAccounts.AddRange(owner1, owner2);
                await _context.SaveChangesAsync();
                _context.Occupants.Add(new Occupant { UserAccountId = owner1.Id, FullName = "Ahmad bin Razak", IdentificationNo = "900101-10-1234", ContactNumber = "011-2233445", Gender = "M", OccupantType = OccupantType.Owner, OccupantStatus = "Active" });
                _context.Occupants.Add(new Occupant { UserAccountId = owner2.Id, FullName = "John Doe", IdentificationNo = "850202-14-5678", ContactNumber = "019-8765432", Gender = "M", OccupantType = OccupantType.Owner, OccupantStatus = "Active" });
                users.Add(new { role = "Owner", email = "owner1@test.com", password = "Owner@123" });
                users.Add(new { role = "Owner", email = "owner2@test.com", password = "Owner@123" });

                // Tenants
                var tenant1 = new UserAccount { Email = "tenant1@test.com", PasswordHash = Hash("Tenant@123"), RoleType = RoleType.Occupant, AccountStatus = AccountStatus.Active };
                var tenant2 = new UserAccount { Email = "tenant2@test.com", PasswordHash = Hash("Tenant@123"), RoleType = RoleType.Occupant, AccountStatus = AccountStatus.Active };
                _context.UserAccounts.AddRange(tenant1, tenant2);
                await _context.SaveChangesAsync();
                _context.Occupants.Add(new Occupant { UserAccountId = tenant1.Id, FullName = "Sarah Lim", IdentificationNo = "950303-01-9012", ContactNumber = "016-1122334", Gender = "F", OccupantType = OccupantType.Tenant, OccupantStatus = "Active" });
                _context.Occupants.Add(new Occupant { UserAccountId = tenant2.Id, FullName = "Michael Chong", IdentificationNo = "980404-07-3456", ContactNumber = "017-5566778", Gender = "M", OccupantType = OccupantType.Tenant, OccupantStatus = "Active" });
                users.Add(new { role = "Tenant", email = "tenant1@test.com", password = "Tenant@123" });
                users.Add(new { role = "Tenant", email = "tenant2@test.com", password = "Tenant@123" });

                return Ok(new
                {
                    message = "✅ Seed complete! Database is ready for testing.",
                    accounts = users,
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

        private static string BuildActivationEmail(string name, string email, string tempPw, string role, string appUrl = "http://localhost:4201")
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
        <a href='{appUrl}/auth/login' 
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