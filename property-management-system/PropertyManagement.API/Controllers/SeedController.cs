using Microsoft.AspNetCore.Authorization;
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

        [HttpPost("run")]
        public async Task<IActionResult> RunSeed()
        {
            var log = new List<string>();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Clear Existing Data and Reset IDs
                var tables = new[] { 
                    "Messages", "ChatParticipants", "Chats", "Payments", "WorkAssignments", 
                    "WorkOrders", "MaintenanceRequests", "AssetMaintenanceHistories", 
                    "MaintenancePlans", "Assets", "Contracts", "PropertyUnits", 
                    "PropertyServiceTypes", "Occupants", "Technicians", "PropertyManagers", 
                    "Properties", "Organisations", "ServiceTypes", "UserAccounts" 
                };
                var sql = $"TRUNCATE TABLE {string.Join(", ", tables.Select(t => $"\"{t}\""))} RESTART IDENTITY CASCADE;";
                await _context.Database.ExecuteSqlRawAsync(sql);
                log.Add("✅ Cleared existing data.");

                var defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword("password");

                // 2. Service Types
                var serviceTypes = new List<ServiceType>
                {
                    new ServiceType { Name = "Plumbing", Description = "Water leaks, pipes, and sanitary fixtures.", BasePrice = 80 },
                    new ServiceType { Name = "HVAC", Description = "Air-conditioning and ventilation systems.", BasePrice = 120 },
                    new ServiceType { Name = "Electrical", Description = "Wiring, lighting, and power issues.", BasePrice = 90 },
                    new ServiceType { Name = "General Maintenance", Description = "Handyman and general repairs.", BasePrice = 50 },
                    new ServiceType { Name = "Cleaning", Description = "Professional cleaning services.", BasePrice = 100 },
                    new ServiceType { Name = "Security", Description = "Security system and access control.", BasePrice = 150 }
                };
                _context.ServiceTypes.AddRange(serviceTypes);
                await _context.SaveChangesAsync();
                log.Add("✅ Seeded Service Types.");

                // 3. Technicians (2 per service type)
                var techs = new List<Technician>();
                foreach (var st in serviceTypes)
                {
                    for (int i = 1; i <= 2; i++)
                    {
                        var tEmail = $"tech.{st.Name.ToLower().Replace(" ", "")}{i}@demo.com";
                        var tUser = new UserAccount { Email = tEmail, PasswordHash = defaultPasswordHash, RoleType = RoleType.Technician };
                        _context.UserAccounts.Add(tUser);
                        await _context.SaveChangesAsync();

                        var tech = new Technician
                        {
                            UserAccountId = tUser.Id,
                            ServiceTypeId = st.Id,
                            FullName = $"Tech {st.Name} {i}",
                            ContactNumber = $"012-99988{i:D2}",
                            Gender = "M",
                            Age = 30 + i,
                            ExperienceLevel = i == 1 ? "Senior" : "Intermediate",
                            AvailabilityStatus = "Available",
                            Ranking = 5
                        };
                        _context.Technicians.Add(tech);
                        techs.Add(tech);
                    }
                }
                await _context.SaveChangesAsync();
                log.Add("✅ Seeded Technicians.");

                // 4. Organisations
                var orgs = new List<Organisation>
                {
                    new Organisation { OrganisationName = "Sunway Property", ContactPerson = "S. Cheah", ContactEmail = "admin@sunway.com" },
                    new Organisation { OrganisationName = "EcoWorld", ContactPerson = "E. Liew", ContactEmail = "admin@ecoworld.my" },
                    new Organisation { OrganisationName = "SP Setia", ContactPerson = "P. Tan", ContactEmail = "admin@spsetia.com" }
                };
                _context.Organisations.AddRange(orgs);
                await _context.SaveChangesAsync();

                // 5. Properties (2 per Org) & Managers (3 per Property)
                var properties = new List<Property>();
                var allUnits = new List<PropertyUnit>();
                var rand = new Random(42);

                int pmCounter = 1;
                foreach (var org in orgs)
                {
                    for (int p = 1; p <= 2; p++)
                    {
                        var propName = $"{org.OrganisationName} Residences {p}";
                        var prop = new Property
                        {
                            OrganisationId = org.Id,
                            PropertyName = propName,
                            PropertyType = "Residential",
                            Address = $"No {p}, Jalan {org.OrganisationName}",
                            City = "Kuala Lumpur",
                            State = "WP Kuala Lumpur",
                            Postcode = "50000"
                        };
                        _context.Properties.Add(prop);
                        await _context.SaveChangesAsync();
                        properties.Add(prop);

                        // Link all service types to property
                        foreach (var st in serviceTypes)
                        {
                            _context.PropertyServiceTypes.Add(new PropertyServiceType { PropertyId = prop.Id, ServiceTypeId = st.Id });
                        }

                        // Property Managers (3 per property)
                        for (int m = 1; m <= 3; m++)
                        {
                            var mEmail = $"pm{pmCounter}@demo.com";
                            var mUser = new UserAccount { Email = mEmail, PasswordHash = defaultPasswordHash, RoleType = RoleType.PropertyManager };
                            _context.UserAccounts.Add(mUser);
                            await _context.SaveChangesAsync();

                            var pm = new PropertyManager
                            {
                                UserAccountId = mUser.Id,
                                FullName = $"Manager {pmCounter}",
                                ContactNumber = $"012-33344{pmCounter:D2}",
                                Gender = m % 2 == 0 ? "F" : "M",
                                Age = 35 + m,
                                Position = m == 1 ? "Senior Property Manager" : "Property Executive"
                            };
                            _context.PropertyManagers.Add(pm);
                            pmCounter++;

                            if (m == 1) // First manager becomes the primary contact in some logic maybe, but for now they just get the property
                            {
                                // We no longer set ManagedByManagerId on Property.
                            }
                            
                            pm.PropertyId = prop.Id;
                        }

                        // Property Units (20 per property)
                        var blocks = new[] { "A", "B" };
                        var unitTypes = new[] { "Studio", "1-Bedroom", "3-Bedroom" };
                        foreach (var block in blocks)
                        {
                            for (int f = 1; f <= 5; f++)
                            {
                                for (int u = 1; u <= 2; u++)
                                {
                                    var uType = unitTypes[rand.Next(unitTypes.Length)];
                                    int beds = uType == "Studio" ? 1 : (uType == "1-Bedroom" ? 1 : 3);
                                    
                                    var unit = new PropertyUnit
                                    {
                                        PropertyId = prop.Id,
                                        UnitNumber = $"{block}-{f:D2}-{u:D2}",
                                        Block = block,
                                        FloorLevel = f.ToString(),
                                        UnitType = uType,
                                        AreaSqft = uType == "Studio" ? 500 : (uType == "1-Bedroom" ? 750 : 1200),
                                        Bedrooms = beds,
                                        Bathrooms = beds > 1 ? 2 : 1,
                                        Status = "Vacant"
                                    };
                                    _context.PropertyUnits.Add(unit);
                                    allUnits.Add(unit);
                                }
                            }
                        }
                        
                        // Assets (20+ per property)
                        var assets = new List<Asset>();
                        
                        // Property-level global assets
                        assets.AddRange(new[]
                        {
                            new Asset { PropertyId = prop.Id, AssetName = "Main Water Pump", AssetType = "Water Pump", Location = "Pump Room", InstallationDate = DateTime.UtcNow.AddYears(-3), ExpLifespanYears = 10, MaintenanceIntervalDays = 90, SupplierName = "Grundfos", QrCode = $"QR-PMP-{prop.Id}" },
                            new Asset { PropertyId = prop.Id, AssetName = "Main Electrical Switch Room", AssetType = "Electrical Panel", Location = "Basement Level 2", InstallationDate = DateTime.UtcNow.AddYears(-5), ExpLifespanYears = 25, MaintenanceIntervalDays = 180, SupplierName = "Schneider Electric", QrCode = $"QR-ELEC-{prop.Id}" },
                            new Asset { PropertyId = prop.Id, AssetName = "Security Control Center", AssetType = "CCTV & Security", Location = "Guard House", InstallationDate = DateTime.UtcNow.AddYears(-2), ExpLifespanYears = 8, MaintenanceIntervalDays = 60, SupplierName = "Hikvision", QrCode = $"QR-SEC-{prop.Id}" },
                            new Asset { PropertyId = prop.Id, AssetName = "Fire Command Center", AssetType = "Fire System", Location = "Ground Floor", InstallationDate = DateTime.UtcNow.AddYears(-4), ExpLifespanYears = 15, MaintenanceIntervalDays = 90, SupplierName = "Honeywell", QrCode = $"QR-FIRE-{prop.Id}" }
                        });

                        // Block-level assets
                        foreach (var block in blocks)
                        {
                            assets.AddRange(new[]
                            {
                                new Asset { PropertyId = prop.Id, AssetName = $"Passenger Elevator 1 - Block {block}", AssetType = "Elevator", Location = $"Block {block} Core", InstallationDate = DateTime.UtcNow.AddYears(-5), ExpLifespanYears = 20, MaintenanceIntervalDays = 30, SupplierName = "Schindler", QrCode = $"QR-ELV1-{block}-{prop.Id}" },
                                new Asset { PropertyId = prop.Id, AssetName = $"Passenger Elevator 2 - Block {block}", AssetType = "Elevator", Location = $"Block {block} Core", InstallationDate = DateTime.UtcNow.AddYears(-5), ExpLifespanYears = 20, MaintenanceIntervalDays = 30, SupplierName = "Schindler", QrCode = $"QR-ELV2-{block}-{prop.Id}" },
                                new Asset { PropertyId = prop.Id, AssetName = $"Passenger Elevator 3 - Block {block}", AssetType = "Elevator", Location = $"Block {block} Core", InstallationDate = DateTime.UtcNow.AddYears(-5), ExpLifespanYears = 20, MaintenanceIntervalDays = 30, SupplierName = "Schindler", QrCode = $"QR-ELV3-{block}-{prop.Id}" },
                                new Asset { PropertyId = prop.Id, AssetName = $"Service Elevator - Block {block}", AssetType = "Elevator", Location = $"Block {block} Service Core", InstallationDate = DateTime.UtcNow.AddYears(-5), ExpLifespanYears = 20, MaintenanceIntervalDays = 30, SupplierName = "Otis", QrCode = $"QR-SELV-{block}-{prop.Id}" },
                                new Asset { PropertyId = prop.Id, AssetName = $"Main Lobby HVAC - Block {block}", AssetType = "HVAC", Location = $"Block {block} Lobby", InstallationDate = DateTime.UtcNow.AddYears(-3), ExpLifespanYears = 12, MaintenanceIntervalDays = 90, SupplierName = "Daikin", QrCode = $"QR-HVAC-{block}-{prop.Id}" },
                                new Asset { PropertyId = prop.Id, AssetName = $"Backup Genset - Block {block}", AssetType = "Generator", Location = $"Block {block} Basement", InstallationDate = DateTime.UtcNow.AddYears(-2), ExpLifespanYears = 20, MaintenanceIntervalDays = 180, SupplierName = "Cummins", QrCode = $"QR-GEN-{block}-{prop.Id}" },
                                new Asset { PropertyId = prop.Id, AssetName = $"Access Control Gate - Block {block}", AssetType = "Access Control", Location = $"Block {block} Entrance", InstallationDate = DateTime.UtcNow.AddYears(-1), ExpLifespanYears = 10, MaintenanceIntervalDays = 180, SupplierName = "HID Global", QrCode = $"QR-ACC-{block}-{prop.Id}" },
                                new Asset { PropertyId = prop.Id, AssetName = $"Domestic Water Tank - Block {block}", AssetType = "Plumbing", Location = $"Block {block} Roof", InstallationDate = DateTime.UtcNow.AddYears(-6), ExpLifespanYears = 25, MaintenanceIntervalDays = 365, SupplierName = "King Kong", QrCode = $"QR-TNK-{block}-{prop.Id}" }
                            });
                        }
                        
                        _context.Assets.AddRange(assets);
                        await _context.SaveChangesAsync();

                        // Maintenance Plans & History for Assets
                        foreach (var asset in assets)
                        {
                            var plan = new MaintenancePlan
                            {
                                AssetId = asset.Id,
                                IntervalDays = asset.MaintenanceIntervalDays,
                                Status = "Active"
                            };
                            _context.MaintenancePlans.Add(plan);

                            // Generate realistic history from InstallationDate to Now
                            var currentDate = asset.InstallationDate.AddDays(asset.MaintenanceIntervalDays);
                            DateTime lastService = asset.InstallationDate;

                            while (currentDate < DateTime.UtcNow)
                            {
                                _context.AssetMaintenanceHistories.Add(new AssetMaintenanceHistory
                                {
                                    AssetId = asset.Id,
                                    MaintenanceType = MaintenanceType.Preventive,
                                    Description = "Standard scheduled maintenance completed.",
                                    Cost = rand.Next(150, 600),
                                    MaintenanceDate = currentDate,
                                    ResultStatus = "Completed",
                                    PerformedBy = "External Vendor"
                                });
                                lastService = currentDate;
                                currentDate = currentDate.AddDays(asset.MaintenanceIntervalDays);
                            }

                            // Optional: randomly add a slight offset so they aren't all exactly mathematically perfect
                            // But for simplicity, we'll keep the exact interval.
                            
                            plan.LastServiceDate = lastService;
                            plan.NextDueDate = lastService.AddDays(asset.MaintenanceIntervalDays);
                            asset.NextMaintenanceDueDate = plan.NextDueDate;
                        }
                    }
                }
                await _context.SaveChangesAsync();
                log.Add("✅ Seeded Properties, PMs, Units, and Assets.");

                // 6. Occupants (80% of units occupied)
                int occupantCounter = 1;
                var activeContracts = new List<Contract>();
                var occupiedUnits = allUnits.OrderBy(x => rand.Next()).Take((int)(allUnits.Count * 0.8)).ToList();

                foreach (var unit in occupiedUnits)
                {
                    // Create Owner
                    var oEmail = $"owner{occupantCounter}@demo.com";
                    var oUser = new UserAccount { Email = oEmail, PasswordHash = defaultPasswordHash, RoleType = RoleType.Occupant };
                    _context.UserAccounts.Add(oUser);
                    await _context.SaveChangesAsync();

                    string ic = $"{rand.Next(70, 99):D2}{rand.Next(1, 13):D2}{rand.Next(1, 28):D2}-{rand.Next(10, 16):D2}-{rand.Next(1000, 9999)}";

                    var owner = new Occupant
                    {
                        UserAccountId = oUser.Id,
                        FullName = $"Owner Ahmad {occupantCounter}",
                        IdentificationNo = ic,
                        ContactNumber = $"012-555{occupantCounter:D4}",
                        OccupantType = OccupantType.Owner,
                        OccupantStatus = "Active"
                    };
                    _context.Occupants.Add(owner);
                    await _context.SaveChangesAsync();

                    var ownerContract = new Contract
                    {
                        OccupantId = owner.Id,
                        UnitId = unit.Id,
                        ContractType = "Ownership",
                        StartDate = DateTime.UtcNow.AddYears(-2),
                        IsPrimaryOccupant = true,
                        Status = "Active"
                    };
                    _context.Contracts.Add(ownerContract);
                    unit.Status = "Occupied";
                    activeContracts.Add(ownerContract);

                    // 30% chance owner has a Tenant
                    if (rand.NextDouble() < 0.3)
                    {
                        var tUser = new UserAccount { Email = $"tenant{occupantCounter}@demo.com", PasswordHash = defaultPasswordHash, RoleType = RoleType.Occupant };
                        _context.UserAccounts.Add(tUser);
                        await _context.SaveChangesAsync();

                        string tic = $"{rand.Next(80, 99):D2}{rand.Next(1, 13):D2}{rand.Next(1, 28):D2}-{rand.Next(10, 16):D2}-{rand.Next(1000, 9999)}";

                        var tenant = new Occupant
                        {
                            UserAccountId = tUser.Id,
                            FullName = $"Tenant Chong {occupantCounter}",
                            IdentificationNo = tic,
                            ContactNumber = $"017-666{occupantCounter:D4}",
                            OccupantType = OccupantType.Tenant,
                            OccupantStatus = "Active"
                        };
                        _context.Occupants.Add(tenant);
                        await _context.SaveChangesAsync();

                        var tenantContract = new Contract
                        {
                            OccupantId = tenant.Id,
                            UnitId = unit.Id,
                            ContractType = "Tenancy",
                            StartDate = DateTime.UtcNow.AddMonths(-6),
                            EndDate = DateTime.UtcNow.AddMonths(6),
                            IsPrimaryOccupant = true, // Tenant becomes primary for requests
                            Status = "Active"
                        };
                        _context.Contracts.Add(tenantContract);
                        activeContracts.Add(tenantContract);
                        
                        ownerContract.IsPrimaryOccupant = false; // Owner is not primary if rented out
                    }

                    occupantCounter++;
                }
                await _context.SaveChangesAsync();
                log.Add("✅ Seeded Occupants and Contracts.");

                // 7. Maintenance Requests & Work Orders
                var activePrimaryContracts = activeContracts.Where(c => c.IsPrimaryOccupant).ToList();
                int requestCounter = 1;

                foreach (var contract in activePrimaryContracts.Take(20)) // Create 20 requests total
                {
                    var status = requestCounter % 3 == 0 ? RequestStatus.Completed : (requestCounter % 2 == 0 ? RequestStatus.InProgress : RequestStatus.Pending);
                    var category = serviceTypes[rand.Next(serviceTypes.Count)];
                    var tech = techs.FirstOrDefault(t => t.ServiceTypeId == category.Id);

                    var req = new MaintenanceRequest
                    {
                        UnitId = contract.UnitId,
                        OccupantId = contract.OccupantId,
                        Title = $"{category.Name} Issue in {contract.PropertyUnit.UnitNumber}",
                        AssetType = category.Name,
                        Description = $"Experiencing issues with {category.Name.ToLower()}. Please assist.",
                        Status = status,
                        RequestDate = DateTime.UtcNow.AddDays(-requestCounter)
                    };
                    _context.MaintenanceRequests.Add(req);
                    await _context.SaveChangesAsync();

                    if (status == RequestStatus.InProgress || status == RequestStatus.Completed)
                    {
                        var wo = new WorkOrder
                        {
                            RequestId = req.Id,
                            WorkType = "Corrective",
                            Status = status == RequestStatus.Completed ? "Completed" : "InProgress",
                            ScheduleDate = req.RequestDate.AddDays(1),
                            CompletedDate = status == RequestStatus.Completed ? req.RequestDate.AddDays(2) : null,
                            Description = status == RequestStatus.Completed ? "Issue fixed successfully." : null
                        };
                        _context.WorkOrders.Add(wo);
                        await _context.SaveChangesAsync();

                        if (tech != null)
                        {
                            _context.WorkAssignments.Add(new WorkAssignment
                            {
                                WorkOrderId = wo.Id,
                                TechnicianId = tech.Id,
                                AssignedDate = req.RequestDate.AddHours(2),
                                Status = "Accepted"
                            });
                        }

                        if (status == RequestStatus.Completed)
                        {
                            _context.Payments.Add(new Payment
                            {
                                RequestId = req.Id,
                                Amount = (decimal)(category.BasePrice + rand.Next(50, 200)),
                                PaymentDate = req.RequestDate.AddDays(3),
                                PaymentMethod = "Credit Card",
                                PaymentStatus = "Paid"
                            });

                            var chat = new Chat { RequestId = req.Id, CreatedAt = req.RequestDate };
                            _context.Chats.Add(chat);
                            await _context.SaveChangesAsync();

                            _context.Messages.Add(new Message { ChatId = chat.Id, SenderId = contract.Occupant.UserAccountId, MessageContent = "Please come asap.", SentAt = req.RequestDate.AddHours(1) });
                            _context.Messages.Add(new Message { ChatId = chat.Id, SenderId = tech.UserAccountId, MessageContent = "On my way.", SentAt = req.RequestDate.AddHours(2) });
                        }
                    }

                    requestCounter++;
                }
                await _context.SaveChangesAsync();
                log.Add("✅ Seeded Requests, Work Orders, Payments, and Chats.");

                await transaction.CommitAsync();
                return Ok(new { message = "Seeding completed successfully.", details = log });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Seeding failed.", error = ex.Message, inner = ex.InnerException?.Message });
            }
        }

        [HttpGet("test-dates")]
        public async Task<IActionResult> TestDates()
        {
            var data = await _context.Assets.Select(a => new { 
                AssetId = a.Id, 
                AssetName = a.AssetName,
                AssetNext = a.NextMaintenanceDueDate,
                PlanNext = _context.MaintenancePlans.Where(p => p.AssetId == a.Id).Select(p => p.NextDueDate).FirstOrDefault()
            }).Take(10).ToListAsync();
            return Ok(data);
        }

        [HttpPost("fix-dates")]
        public async Task<IActionResult> FixDates()
        {
            // Use SQL TRUNCATE to reset Identity on the history table.
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"AssetMaintenanceHistories\" RESTART IDENTITY CASCADE;");
            
            // Remove existing auto-generated proactive work orders using SQL to avoid EF tracking conflicts
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"WorkOrders\" WHERE \"WorkType\" = 'Preventive Maintenance';");
            
            var assets = await _context.Assets.ToListAsync();
            var plans = await _context.MaintenancePlans.ToListAsync();
            var techs = await _context.Technicians.ToListAsync();
            var now = DateTime.UtcNow;
            var rand = new Random(42);
            int count = 0;

            var descriptionsList = new[] 
            { 
                "Routine check completed with no issues.", 
                "Minor wear observed, parts replaced and calibrated.", 
                "Filter cleaned and system lubricated.", 
                "System running optimally. No attention needed.",
                "Thorough inspection completed. Small adjustments made."
            };

            foreach (var asset in assets)
            {
                var plan = plans.FirstOrDefault(p => p.AssetId == asset.Id);
                if (plan == null) continue;

                // Add a stagger (jitter) so all elevators aren't scheduled on the exact same day
                int jitterDays = (int)(asset.Id % asset.MaintenanceIntervalDays);
                DateTime lastService = asset.InstallationDate.AddDays(jitterDays);
                var currentDate = lastService.AddDays(asset.MaintenanceIntervalDays);

                while (currentDate < now)
                {
                    var wo = new WorkOrder
                    {
                        PlanId = plan.Id,
                        WorkType = "Preventive Maintenance",
                        Description = "Standard scheduled maintenance completed.",
                        Status = "Completed",
                        ScheduleDate = currentDate,
                        CompletedDate = currentDate
                    };
                    _context.WorkOrders.Add(wo);

                    var tech = techs.OrderBy(t => rand.Next()).FirstOrDefault();
                    if (tech != null)
                    {
                        _context.WorkAssignments.Add(new WorkAssignment
                        {
                            WorkOrder = wo,
                            TechnicianId = tech.Id,
                            AssignedDate = currentDate.AddHours(-24),
                            Status = "Accepted"
                        });
                    }

                    var selectedDesc = descriptionsList[rand.Next(descriptionsList.Length)];

                    _context.AssetMaintenanceHistories.Add(new AssetMaintenanceHistory
                    {
                        AssetId = asset.Id,
                        WorkOrder = wo,
                        MaintenanceType = MaintenanceType.Preventive,
                        Description = selectedDesc,
                        Cost = rand.Next(150, 600),
                        MaintenanceDate = currentDate,
                        ResultStatus = "Completed",
                        PerformedBy = tech != null ? tech.FullName : "External Vendor"
                    });
                    
                    lastService = currentDate;
                    currentDate = currentDate.AddDays(asset.MaintenanceIntervalDays);
                }

                plan.LastServiceDate = lastService;
                plan.NextDueDate = lastService.AddDays(asset.MaintenanceIntervalDays);
                asset.NextMaintenanceDueDate = plan.NextDueDate;
                
                count++;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Successfully updated NextMaintenanceDueDate and generated full Work Order histories for {count} assets." });
        }
    }
}