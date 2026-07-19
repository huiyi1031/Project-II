using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Models.Entities;
using PropertyManagement.API.Models.Enums;

namespace PropertyManagement.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // ── DbSets ────────────────────────────────────────────────────────────────
        public DbSet<Organisation> Organisations { get; set; }
        public DbSet<UserAccount> UserAccounts { get; set; }
        public DbSet<Occupant> Occupants { get; set; }
        public DbSet<PropertyManager> PropertyManagers { get; set; }
        public DbSet<Technician> Technicians { get; set; }
        public DbSet<Property> Properties { get; set; }
        public DbSet<PropertyUnit> PropertyUnits { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<AssetMaintenanceHistory> AssetMaintenanceHistories { get; set; }
        public DbSet<MaintenancePlan> MaintenancePlans { get; set; }
        public DbSet<MaintenanceRequest> MaintenanceRequests { get; set; }
        public DbSet<WorkOrder> WorkOrders { get; set; }
        public DbSet<WorkAssignment> WorkAssignments { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<ChatParticipant> ChatParticipants { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<ServiceType> ServiceTypes { get; set; }
        public DbSet<PropertyServiceType> PropertyServiceTypes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Composite Keys ─────────────────────────────────────────────────────
            modelBuilder.Entity<ChatParticipant>()
                .HasKey(cp => new { cp.ChatId, cp.UserAccountId });

            modelBuilder.Entity<PropertyServiceType>()
                .HasKey(pst => new { pst.PropertyId, pst.ServiceTypeId });

            // ── One-to-One Relationships ───────────────────────────────────────────
            modelBuilder.Entity<Occupant>()
                .HasOne(o => o.UserAccount)
                .WithOne(u => u.Occupant)
                .HasForeignKey<Occupant>(o => o.UserAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Technician>()
                .HasOne(t => t.UserAccount)
                .WithOne(u => u.Technician)
                .HasForeignKey<Technician>(t => t.UserAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PropertyManager>()
                .HasOne(pm => pm.UserAccount)
                .WithOne(u => u.PropertyManager)
                .HasForeignKey<PropertyManager>(pm => pm.UserAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── One-to-Many: Organisation → Property ───────────────────────────────
            modelBuilder.Entity<Property>()
                .HasOne(p => p.Organisation)
                .WithMany(o => o.Properties)
                .HasForeignKey(p => p.OrganisationId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── One-to-One: Property → PropertyManager (ManagedBy) ────────────────
            modelBuilder.Entity<Property>()
                .HasOne(p => p.ManagedBy)
                .WithMany()
                .HasForeignKey(p => p.ManagedByManagerId)
                .OnDelete(DeleteBehavior.SetNull);

            // ── MaintenanceRequest ↔ Chat / WorkOrder ──────────────────────────────
            modelBuilder.Entity<MaintenanceRequest>()
                .HasOne(mr => mr.Chat)
                .WithOne(c => c.MaintenanceRequest)
                .HasForeignKey<Chat>(c => c.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MaintenanceRequest>()
                .HasOne(mr => mr.WorkOrder)
                .WithOne(wo => wo.MaintenanceRequest)
                .HasForeignKey<WorkOrder>(wo => wo.RequestId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Default Values ─────────────────────────────────────────────────────
            modelBuilder.Entity<UserAccount>()
                .Property(u => u.AccountStatus)
                .HasDefaultValue(AccountStatus.Active)
                .HasConversion<int>();

            modelBuilder.Entity<PropertyUnit>()
                .Property(pu => pu.CurrentOccupants)
                .HasDefaultValue(0);

            modelBuilder.Entity<MaintenanceRequest>()
                .Property(mr => mr.Status)
                .HasDefaultValue(RequestStatus.Pending)
                .HasConversion<int>();

            // ── Indexes ────────────────────────────────────────────────────────────
            // Unit number must be unique WITHIN a property (not globally)
            modelBuilder.Entity<PropertyUnit>()
                .HasIndex(pu => new { pu.PropertyId, pu.UnitNumber })
                .IsUnique()
                .HasDatabaseName("IX_PropertyUnit_PropertyId_UnitNumber");

            modelBuilder.Entity<Asset>()
                .HasIndex(a => a.QrCode)
                .IsUnique()
                .HasDatabaseName("IX_Asset_QrCode");
        }
    }
}