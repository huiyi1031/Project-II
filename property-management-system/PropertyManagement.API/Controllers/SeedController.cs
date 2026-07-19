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
        /// Seeds the database with complete test data.
        /// WARNING: Clears ALL existing data first. Call only once to reset.
        /// </summary>
        [HttpPost("run")]
        public async Task<IActionResult> Run()
        {
            var log = new List<string>();

            try
            {
                // ── 1. Clear all existing data (FK-safe order) ──────────────────────
                _context.AssetMaintenanceHistories.RemoveRange(_context.AssetMaintenanceHistories);
                _context.MaintenancePlans.RemoveRange(_context.MaintenancePlans);
                _context.Assets.RemoveRange(_context.Assets);
                _context.WorkAssignments.RemoveRange(_context.WorkAssignments);
                _context.WorkOrders.RemoveRange(_context.WorkOrders);
                _context.Payments.RemoveRange(_context.Payments);
                _context.Messages.RemoveRange(_context.Messages);
                _context.ChatParticipants.RemoveRange(_context.ChatParticipants);
                _context.Chats.RemoveRange(_context.Chats);
                _context.MaintenanceRequests.RemoveRange(_context.MaintenanceRequests);
                _context.Contracts.RemoveRange(_context.Contracts);
                _context.PropertyServiceTypes.RemoveRange(_context.PropertyServiceTypes);
                _context.ServiceTypes.RemoveRange(_context.ServiceTypes);
                _context.PropertyUnits.RemoveRange(_context.PropertyUnits);
                _context.Properties.RemoveRange(_context.Properties);
                _context.Organisations.RemoveRange(_context.Organisations);
                _context.Occupants.RemoveRange(_context.Occupants);
                _context.Technicians.RemoveRange(_context.Technicians);
                _context.PropertyManagers.RemoveRange(_context.PropertyManagers);
                _context.UserAccounts.RemoveRange(_context.UserAccounts);
                await _context.SaveChangesAsync();
                log.Add("✅ Cleared all existing data");

                // ── 2. Organisation ─────────────────────────────────────────────────
                var org = new Organisation
                {
                    OrganisationName = "Sunway Property Management Sdn Bhd",
                    ContactPerson    = "Dato' Sri Lim Wei Ming",
                    ContactEmail     = "admin@sunwayproperty.com.my",
                    ContactPhone     = "03-8082 7888",
                    Address          = "Level 3, Sunway Geo Tower, Jalan Lagoon Selatan, Subang Jaya, Selangor",
                    RegistrationNo   = "201001012345",
                    IsActive         = true
                };
                _context.Organisations.Add(org);
                await _context.SaveChangesAsync();
                log.Add($"✅ Organisation: {org.OrganisationName}");

                // ── 3. Property Managers (2) ─────────────────────────────────────────
                var managerPw = "Manager@123";

                var managerUser1 = new UserAccount
                {
                    Email          = "nghy-wm24@student.tarc.edu.my",
                    PasswordHash   = BCrypt.Net.BCrypt.HashPassword(managerPw),
                    RoleType       = RoleType.PropertyManager,
                    AccountStatus  = AccountStatus.Active
                };
                var managerUser2 = new UserAccount
                {
                    Email          = "manager2@sunwayproperty.com.my",
                    PasswordHash   = BCrypt.Net.BCrypt.HashPassword(managerPw),
                    RoleType       = RoleType.PropertyManager,
                    AccountStatus  = AccountStatus.Active
                };
                _context.UserAccounts.AddRange(managerUser1, managerUser2);
                await _context.SaveChangesAsync();

                var pm1 = new PropertyManager
                {
                    UserAccountId = managerUser1.Id,
                    FullName      = "Ng Hui Yi",
                    ContactNumber = "012-3456789",
                    Gender        = "F",
                    Position      = "Senior Property Manager"
                };
                var pm2 = new PropertyManager
                {
                    UserAccountId = managerUser2.Id,
                    FullName      = "Jason Lee Kok Wai",
                    ContactNumber = "011-1112222",
                    Gender        = "M",
                    Position      = "Property Manager"
                };
                _context.PropertyManagers.AddRange(pm1, pm2);
                await _context.SaveChangesAsync();
                log.Add($"✅ Managers: {pm1.FullName} (nghy-wm24@student.tarc.edu.my), {pm2.FullName} (manager2@sunwayproperty.com.my) | Password: {managerPw}");

                // ── 4. Technicians (2) ───────────────────────────────────────────────
                var techTempPw  = GenerateTempPassword();
                var techStaticPw = "Tech@123";

                var techUser1 = new UserAccount
                {
                    Email         = "nghy1031@gmail.com",
                    PasswordHash  = BCrypt.Net.BCrypt.HashPassword(techTempPw),
                    RoleType      = RoleType.Technician,
                    AccountStatus = AccountStatus.Pending
                };
                var techUser2 = new UserAccount
                {
                    Email         = "tech2@sunwayproperty.com.my",
                    PasswordHash  = BCrypt.Net.BCrypt.HashPassword(techStaticPw),
                    RoleType      = RoleType.Technician,
                    AccountStatus = AccountStatus.Active
                };
                _context.UserAccounts.AddRange(techUser1, techUser2);
                await _context.SaveChangesAsync();

                _context.Technicians.AddRange(
                    new Technician
                    {
                        UserAccountId      = techUser1.Id,
                        FullName           = "Daniel Tan Ah Kow",
                        ContactNumber      = "013-9876543",
                        Gender             = "M",
                        ExperienceLevel    = "Senior",
                        AvailabilityStatus = "Available",
                        Ranking            = 1m
                    },
                    new Technician
                    {
                        UserAccountId      = techUser2.Id,
                        FullName           = "Muthu Kumar a/l Rajan",
                        ContactNumber      = "019-8887777",
                        Gender             = "M",
                        ExperienceLevel    = "Junior",
                        AvailabilityStatus = "Available",
                        Ranking            = 2m
                    }
                );
                await _context.SaveChangesAsync();

                try
                {
                    await _emailService.SendEmailAsync(
                        techUser1.Email,
                        "Welcome to Property Management System — Activate Your Account",
                        BuildActivationEmail("Daniel Tan Ah Kow", techUser1.Email, techTempPw, "Technician")
                    );
                }
                catch { log.Add("⚠️ Could not send activation email to technician 1"); }

                log.Add($"✅ Technician 1: nghy1031@gmail.com | Temp Password: {techTempPw} (email sent)");
                log.Add($"✅ Technician 2: tech2@sunwayproperty.com.my | Password: {techStaticPw} | Active");

                // ── 5. Properties (2) — each managed by a different PM ───────────────
                var property1 = new Property
                {
                    OrganisationId     = org.Id,
                    ManagedByManagerId = pm1.Id,
                    PropertyName       = "Sunway Nexis Residences",
                    PropertyType       = "Condominium",
                    Address            = "Jalan PJU 5/1, Kota Damansara",
                    City               = "Petaling Jaya",
                    State              = "Selangor",
                    Postcode           = "47810"
                };
                var property2 = new Property
                {
                    OrganisationId     = org.Id,
                    ManagedByManagerId = pm2.Id,
                    PropertyName       = "Sunway Geo Residences",
                    PropertyType       = "Condominium",
                    Address            = "Jalan Lagoon Selatan, Bandar Sunway",
                    City               = "Subang Jaya",
                    State              = "Selangor",
                    Postcode           = "47500"
                };
                _context.Properties.AddRange(property1, property2);
                await _context.SaveChangesAsync();
                log.Add($"✅ Properties: '{property1.PropertyName}' (managed by Ng Hui Yi), '{property2.PropertyName}' (managed by Jason Lee)");

                // ── 6. Property Units — 10 realistic condo units per property ─────────
                //
                // Real condo size breakdown:
                //   Studio    : 1 bed, 1 bath, 450–600 sqft
                //   1-Bedroom : 1 bed, 1 bath, 650–800 sqft
                //   2-Bedroom : 2 bed, 2 bath, 900–1,100 sqft
                //   3-Bedroom : 3 bed, 2 bath, 1,200–1,600 sqft
                //
                var unitDefs = new[]
                {
                    // Block A
                    ("A", "1", "A-01-01", "Studio",      520m,  1, 1, 2),
                    ("A", "1", "A-01-02", "1-Bedroom",   720m,  1, 1, 4),
                    ("A", "5", "A-05-03", "2-Bedroom",   980m,  2, 2, 6),
                    ("A", "5", "A-05-04", "3-Bedroom",  1350m,  3, 2, 8),
                    ("A", "10","A-10-05", "2-Bedroom",  1050m,  2, 2, 6),
                    // Block B
                    ("B", "2", "B-02-01", "Studio",      480m,  1, 1, 2),
                    ("B", "2", "B-02-02", "1-Bedroom",   770m,  1, 1, 4),
                    ("B", "8", "B-08-03", "2-Bedroom",   920m,  2, 2, 6),
                    ("B", "8", "B-08-04", "3-Bedroom",  1450m,  3, 2, 8),
                    ("B", "15","B-15-05", "3-Bedroom",  1580m,  3, 2, 8),
                };

                foreach (var p in new[] { property1, property2 })
                {
                    foreach (var (block, floor, num, type, sqft, beds, baths, maxOcc) in unitDefs)
                    {
                        _context.PropertyUnits.Add(new PropertyUnit
                        {
                            PropertyId       = p.Id,
                            Block            = block,
                            FloorLevel       = floor,
                            UnitNumber       = num,
                            UnitType         = type,
                            AreaSqft         = sqft,
                            Bedrooms         = beds,
                            Bathrooms        = baths,
                            MaxOccupants     = maxOcc,
                            CurrentOccupants = 0,
                            Status           = "Vacant"
                        });
                    }
                }
                await _context.SaveChangesAsync();
                log.Add($"✅ Property Units: 10 units per property × 2 properties = 20 units total");

                // ── 7. Owners (2, using IC bypass flow) ─────────────────────────────
                var ownerUser1 = new UserAccount
                {
                    Email         = "owner1.placeholder@pms.local",
                    PasswordHash  = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                    RoleType      = RoleType.Occupant,
                    AccountStatus = AccountStatus.Pending
                };
                var ownerUser2 = new UserAccount
                {
                    Email         = "owner2.placeholder@pms.local",
                    PasswordHash  = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                    RoleType      = RoleType.Occupant,
                    AccountStatus = AccountStatus.Pending
                };
                _context.UserAccounts.AddRange(ownerUser1, ownerUser2);
                await _context.SaveChangesAsync();

                var occOwner1 = new Occupant
                {
                    UserAccountId   = ownerUser1.Id,
                    FullName        = "Ahmad bin Razak",
                    IdentificationNo = "900101-10-1234",
                    ContactNumber   = "011-2233445",
                    Gender          = "M",
                    OccupantType    = OccupantType.Owner,
                    OccupantStatus  = "Active"
                };
                var occOwner2 = new Occupant
                {
                    UserAccountId   = ownerUser2.Id,
                    FullName        = "Sarah Wong Mei Ling",
                    IdentificationNo = "850505-14-5566",
                    ContactNumber   = "012-9998888",
                    Gender          = "F",
                    OccupantType    = OccupantType.Owner,
                    OccupantStatus  = "Active"
                };
                _context.Occupants.AddRange(occOwner1, occOwner2);
                await _context.SaveChangesAsync();

                // Link owners to units
                var unitA0101 = await _context.PropertyUnits.FirstOrDefaultAsync(u => u.UnitNumber == "A-01-01" && u.PropertyId == property1.Id);
                if (unitA0101 != null)
                {
                    _context.Contracts.Add(new Contract { OccupantId = occOwner1.Id, UnitId = unitA0101.Id, ContractType = "Ownership", StartDate = DateTime.UtcNow.AddMonths(-12), IsPrimaryOccupant = true, Status = "Active" });
                    unitA0101.Status = "Occupied";
                    unitA0101.CurrentOccupants = 1;
                }
                var unitA0503 = await _context.PropertyUnits.FirstOrDefaultAsync(u => u.UnitNumber == "A-05-03" && u.PropertyId == property1.Id);
                if (unitA0503 != null)
                {
                    _context.Contracts.Add(new Contract { OccupantId = occOwner2.Id, UnitId = unitA0503.Id, ContractType = "Ownership", StartDate = DateTime.UtcNow.AddMonths(-6), IsPrimaryOccupant = true, Status = "Active" });
                    unitA0503.Status = "Occupied";
                    unitA0503.CurrentOccupants = 1;
                }
                await _context.SaveChangesAsync();
                log.Add("✅ Owners: IC=900101-10-1234, IC=850505-14-5566 | Linked to A-01-01 and A-05-03");


                // ── 8. Tenants (2, Active) ───────────────────────────────────────────
                var tenantPw = "Tenant@123";
                var tenantUser1 = new UserAccount
                {
                    Email         = "tenant1@demo.com",
                    PasswordHash  = BCrypt.Net.BCrypt.HashPassword(tenantPw),
                    RoleType      = RoleType.Occupant,
                    AccountStatus = AccountStatus.Active
                };
                var tenantUser2 = new UserAccount
                {
                    Email         = "tenant2@demo.com",
                    PasswordHash  = BCrypt.Net.BCrypt.HashPassword(tenantPw),
                    RoleType      = RoleType.Occupant,
                    AccountStatus = AccountStatus.Active
                };
                _context.UserAccounts.AddRange(tenantUser1, tenantUser2);
                await _context.SaveChangesAsync();

                var occTenant1 = new Occupant
                {
                    UserAccountId   = tenantUser1.Id,
                    FullName        = "John Doe",
                    IdentificationNo = "950101-10-1111",
                    ContactNumber   = "016-1231234",
                    Gender          = "M",
                    OccupantType    = OccupantType.Tenant,
                    OccupantStatus  = "Active"
                };
                var occTenant2 = new Occupant
                {
                    UserAccountId   = tenantUser2.Id,
                    FullName        = "Jane Smith",
                    IdentificationNo = "960202-14-2222",
                    ContactNumber   = "017-4564567",
                    Gender          = "F",
                    OccupantType    = OccupantType.Tenant,
                    OccupantStatus  = "Active"
                };
                _context.Occupants.AddRange(occTenant1, occTenant2);
                await _context.SaveChangesAsync();

                // Link tenants to units
                var unitB0201 = await _context.PropertyUnits.FirstOrDefaultAsync(u => u.UnitNumber == "B-02-01" && u.PropertyId == property2.Id);
                if (unitB0201 != null)
                {
                    _context.Contracts.Add(new Contract { OccupantId = occTenant1.Id, UnitId = unitB0201.Id, ContractType = "Tenancy", StartDate = DateTime.UtcNow.AddMonths(-3), EndDate = DateTime.UtcNow.AddMonths(9), IsPrimaryOccupant = true, Status = "Active" });
                    unitB0201.Status = "Occupied";
                    unitB0201.CurrentOccupants = 1;
                }
                var unitB0804 = await _context.PropertyUnits.FirstOrDefaultAsync(u => u.UnitNumber == "B-08-04" && u.PropertyId == property2.Id);
                if (unitB0804 != null)
                {
                    _context.Contracts.Add(new Contract { OccupantId = occTenant2.Id, UnitId = unitB0804.Id, ContractType = "Tenancy", StartDate = DateTime.UtcNow.AddMonths(-1), EndDate = DateTime.UtcNow.AddMonths(11), IsPrimaryOccupant = true, Status = "Active" });
                    unitB0804.Status = "Occupied";
                    unitB0804.CurrentOccupants = 1;
                }
                await _context.SaveChangesAsync();
                log.Add($"✅ Tenants: tenant1@demo.com, tenant2@demo.com | Linked to B-02-01 and B-08-04");


                // ── 9. Assets — 5 per property (realistic building infrastructure) ──
                //
                // Based on real supplier maintenance recommendations:
                //   Elevator       : every 30 days
                //   HVAC System    : every 90 days
                //   Water Pump     : every 60 days
                //   Fire System    : every 180 days (bi-annual)
                //   Backup Generator : every 90 days
                //
                var assetDefs = new[]
                {
                    ("Main Lobby Lift A",          "Elevator",   "Ground Floor Lobby",  "KONE Corporation",        "KONE MonoSpace 500",  30,  "Otis Malaysia Sdn Bhd",          15, new DateTime(2021, 3, 15)),
                    ("Car Park Lift B",             "Elevator",   "B1 Car Park",         "Schindler Group",         "Schindler 3300",       30,  "Schindler Malaysia Sdn Bhd",     15, new DateTime(2021, 3, 15)),
                    ("Central HVAC System",         "HVAC",       "Rooftop Plant Room",  "Daikin Industries Ltd",   "Daikin VRV IV",        90,  "Daikin Malaysia Sdn Bhd",        20, new DateTime(2020, 8, 1)),
                    ("Fire Suppression System",     "Fire System", "Basement & All Floors","Siemens AG",            "Siemens VESDA",       180,  "Siemens Malaysia Sdn Bhd",       25, new DateTime(2019, 12, 10)),
                    ("Backup Diesel Generator",     "Generator",  "Basement B2",         "Caterpillar Inc",         "CAT DE275E0",          90,  "Zeppelin Malaysia Sdn Bhd",      20, new DateTime(2020, 5, 20)),
                };

                foreach (var p in new[] { property1, property2 })
                {
                    foreach (var (name, type, loc, mfr, model, interval, supplier, lifespan, installDate) in assetDefs)
                    {
                        var typePrefix = type.Length >= 3 ? type.Substring(0, 3).ToUpper() : "GEN";
                        var qr = $"PMS-{typePrefix}-{installDate:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";

                        var asset = new Asset
                        {
                            PropertyId              = p.Id,
                            AssetName               = name,
                            AssetType               = type,
                            Location                = loc,
                            InstallationDate        = installDate.ToUniversalTime(),
                            Manufacturer            = mfr,
                            ModelNumber             = model,
                            ExpLifespanYears        = lifespan,
                            MaintenanceIntervalDays = interval,
                            SupplierName            = supplier,
                            WarrantyExpiryDate      = installDate.AddYears(5).ToUniversalTime(),
                            NextMaintenanceDueDate  = DateTime.UtcNow.AddDays(interval / 2.0), // due ~soon
                            Status                  = "Active",
                            QrCode                  = qr
                        };
                        _context.Assets.Add(asset);
                        await _context.SaveChangesAsync();

                        // Add 2 maintenance history records per asset
                        _context.AssetMaintenanceHistories.AddRange(
                            new AssetMaintenanceHistory
                            {
                                AssetId         = asset.Id,
                                MaintenanceType = MaintenanceType.Preventive,
                                Description     = $"Routine preventive maintenance — {interval}-day scheduled service. All components inspected and lubricated.",
                                Cost            = 850m,
                                MaintenanceDate = DateTime.UtcNow.AddDays(-interval * 2).ToUniversalTime(),
                                ResultStatus    = "Completed",
                                PerformedBy     = "Daniel Tan Ah Kow"
                            },
                            new AssetMaintenanceHistory
                            {
                                AssetId         = asset.Id,
                                MaintenanceType = MaintenanceType.Preventive,
                                Description     = $"Routine preventive maintenance. Replaced worn parts, updated firmware where applicable.",
                                Cost            = 1200m,
                                MaintenanceDate = DateTime.UtcNow.AddDays(-interval).ToUniversalTime(),
                                ResultStatus    = "Completed",
                                PerformedBy     = "Muthu Kumar a/l Rajan"
                            }
                        );
                        await _context.SaveChangesAsync();
                    }
                }
                log.Add("✅ Assets: 5 assets × 2 properties = 10 assets, each with 2 maintenance history records");

                // ── Return summary ──────────────────────────────────────────────────
                return Ok(new
                {
                    message = "✅ Database seeded successfully! All data is in Supabase.",
                    accounts = new[]
                    {
                        new { role = "Property Manager", email = "nghy-wm24@student.tarc.edu.my",        password = managerPw,    manages = "Sunway Nexis Residences" },
                        new { role = "Property Manager", email = "manager2@sunwayproperty.com.my",        password = managerPw,    manages = "Sunway Geo Residences" },
                        new { role = "Technician",       email = "nghy1031@gmail.com",                   password = $"See email (temp: {techTempPw})", manages = "N/A" },
                        new { role = "Technician",       email = "tech2@sunwayproperty.com.my",           password = techStaticPw, manages = "N/A" },
                        new { role = "Owner",            email = "Set via IC bypass",                    password = "IC: 900101-10-1234 or 850505-14-5566", manages = "N/A" },
                        new { role = "Tenant",           email = "tenant1@demo.com / tenant2@demo.com",  password = tenantPw,     manages = "N/A" },
                    },
                    summary = new
                    {
                        organisations = 1,
                        properties    = 2,
                        propertyUnits = 20,
                        assets        = 10,
                        assetHistories = 20,
                        managers      = 2,
                        technicians   = 2,
                        owners        = 2,
                        tenants       = 2
                    },
                    log
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = $"Seed failed: {ex.Message}",
                    detail  = ex.InnerException?.Message
                });
            }
        }

        private static string GenerateTempPassword()
        {
            var rng   = System.Security.Cryptography.RandomNumberGenerator.Create();
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
      <div style='background:#eaf3ff; border-radius:12px; padding:20px; margin:20px 0; border-left:4px solid #1f5fae;'>
        <p style='margin:0 0 8px; color:#6b7a90; font-size:12px; text-transform:uppercase; letter-spacing:1px;'>Your Login Credentials</p>
        <p style='margin:4px 0;'><strong>Email:</strong> {email}</p>
        <p style='margin:4px 0;'><strong>Temporary Password:</strong> <code style='background:#fff; padding:2px 8px; border-radius:6px; font-size:16px; border:1px solid #dbe7fb;'>{tempPw}</code></p>
      </div>
      <div style='background:#fff8e1; border-radius:12px; padding:16px; border-left:4px solid #e2a400;'>
        <p style='margin:0; color:#7a5c00; font-size:13px;'>⚠️ Please log in immediately and set your permanent password.</p>
      </div>
      <div style='margin-top:24px; text-align:center;'>
        <a href='http://localhost:4201/auth/login' style='display:inline-block; background:linear-gradient(135deg,#1f5fae,#2f7de0); color:#fff; padding:12px 32px; border-radius:10px; text-decoration:none; font-weight:600; font-size:15px;'>
          Login Now →
        </a>
      </div>
    </div>
    <div style='background:#f5f9ff; padding:16px; text-align:center; font-size:11px; color:#6b7a90;'>
      Property Management System — Do not share your credentials.
    </div>
  </div>
</body>
</html>";
        }
    }
}