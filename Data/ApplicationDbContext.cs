using Microsoft.EntityFrameworkCore;
using SCM_System.Models.Entities;

namespace SCM_System.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // User Management Tables
        public DbSet<User> Users { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Retailer> Retailers { get; set; }
        public DbSet<SupplierEmployee> SupplierEmployees { get; set; }
        public DbSet<DriverProfile> DriverProfiles { get; set; }
        public DbSet<WarehouseProfile> WarehouseProfiles { get; set; }
        public DbSet<WarehouseAssignment> WarehouseAssignments { get; set; }
        public DbSet<VehicleAssignment> VehicleAssignments { get; set; }
        public DbSet<Penalty> Penalties { get; set; }

        // Logistics 2.0 Compliance & History
        public DbSet<VehicleDocument> VehicleDocuments { get; set; }
        public DbSet<EmployeeDocument> EmployeeDocuments { get; set; }
        public DbSet<WarehouseManagerHistory> WarehouseManagerHistories { get; set; }
        public DbSet<VehicleDriverHistory> VehicleDriverHistories { get; set; }
        public DbSet<EmployeeWarehouseAccess> EmployeeWarehouseAccesses { get; set; }

        // Logistics 2.0 Operational Intelligence
        public DbSet<DispatchTask> DispatchTasks { get; set; }
        public DbSet<GPSLog> GPSLogs { get; set; }
        public DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }
        public DbSet<IncidentReport> IncidentReports { get; set; }
        public DbSet<InventoryTransfer> InventoryTransfers { get; set; }
        
        // Identity Verification Mock
        public DbSet<FaydaRegistry> FaydaRegistries { get; set; }
        public DbSet<FaydaVerification> FaydaVerifications { get; set; }
        public DbSet<EmailLog> EmailLogs { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }


        // Product Catalog Tables
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<SupplierCategory> SupplierCategories { get; set; }
        public DbSet<RetailerCategory> RetailerCategories { get; set; }

        public DbSet<ProductAttributeDefinition> ProductAttributeDefinitions { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductAttributeValue> ProductAttributeValues { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }

        // Procurement & Order Management Tables
        public DbSet<Tender> Tenders { get; set; }
        public DbSet<TenderItem> TenderItems { get; set; }
        public DbSet<TenderBid> TenderBids { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

        // Delivery Tables
        public DbSet<Delivery> Deliveries { get; set; }
        public DbSet<DeliveryTracking> DeliveryTrackings { get; set; }
        public DbSet<ReturnRequest> ReturnRequests { get; set; }

        // Payment Tables
        public DbSet<Commission> Commissions { get; set; }

        // Communication Tables
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageViolation> MessageViolations { get; set; }

        // System Tables
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ========== USER MANAGEMENT CONFIGURATIONS ==========

            // User - Supplier (one-to-one)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Supplier)
                .WithOne(s => s.User)
                .HasForeignKey<Supplier>(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // User - Retailer (one-to-one)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Retailer)
                .WithOne(r => r.User)
                .HasForeignKey<Retailer>(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // User - SupplierEmployee (one-to-one)
            modelBuilder.Entity<User>()
                .HasOne(u => u.SupplierEmployee)
                .WithOne(se => se.User)
                .HasForeignKey<SupplierEmployee>(se => se.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Supplier - SupplierEmployee (one-to-many)
            modelBuilder.Entity<SupplierEmployee>()
                .HasOne(se => se.Supplier)
                .WithMany(s => s.Employees)
                .HasForeignKey(se => se.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            // SupplierEmployee - Warehouse (many-to-one)
            modelBuilder.Entity<SupplierEmployee>()
                .HasOne(se => se.Warehouse)
                .WithMany(w => w.Employees)
                .HasForeignKey(se => se.WarehouseId)
                .OnDelete(DeleteBehavior.SetNull);

            // SupplierEmployee - Vehicle (many-to-one) (Maintaining for backward compatibility, assignments handle actual logic)
            modelBuilder.Entity<SupplierEmployee>()
                .HasOne(se => se.Vehicle)
                .WithMany(v => v.DeliveryAgents)
                .HasForeignKey(se => se.VehicleId)
                .OnDelete(DeleteBehavior.SetNull);

            // Profiles Config
            modelBuilder.Entity<SupplierEmployee>()
                .HasOne(se => se.DriverProfile)
                .WithOne(dp => dp.SupplierEmployee)
                .HasForeignKey<DriverProfile>(dp => dp.SupplierEmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SupplierEmployee>()
                .HasOne(se => se.WarehouseProfile)
                .WithOne(wp => wp.SupplierEmployee)
                .HasForeignKey<WarehouseProfile>(wp => wp.SupplierEmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Assignments Config
            modelBuilder.Entity<WarehouseAssignment>()
                .HasOne(wa => wa.Warehouse)
                .WithMany(w => w.Assignments)
                .HasForeignKey(wa => wa.WarehouseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WarehouseAssignment>()
                .HasOne(wa => wa.SupplierEmployee)
                .WithMany(se => se.WarehouseAssignments)
                .HasForeignKey(wa => wa.SupplierEmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VehicleAssignment>()
                .HasOne(va => va.Vehicle)
                .WithMany(v => v.Assignments)
                .HasForeignKey(va => va.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VehicleAssignment>()
                .HasOne(va => va.SupplierEmployee)
                .WithMany(se => se.VehicleAssignments)
                .HasForeignKey(va => va.SupplierEmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // User - Penalty (one-to-many)
            modelBuilder.Entity<Penalty>()
                .HasOne(p => p.User)
                .WithMany(u => u.Penalties)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== PRODUCT CATALOG CONFIGURATIONS ==========

            // ProductCategory self-reference (parent-child)
            modelBuilder.Entity<ProductCategory>()
                .HasOne(pc => pc.ParentCategory)
                .WithMany(pc => pc.SubCategories)
                .HasForeignKey(pc => pc.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // ProductCategory - ProductAttributeDefinition (one-to-many)
            modelBuilder.Entity<ProductAttributeDefinition>()
                .HasOne(pad => pad.Category)
                .WithMany(pc => pc.AttributeDefinitions)
                .HasForeignKey(pad => pad.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product - Supplier (many-to-one)
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Supplier)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product - Category (many-to-one)
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(pc => pc.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Supplier - Category Many-to-Many
            modelBuilder.Entity<SupplierCategory>()
                .HasOne(sc => sc.Supplier)
                .WithMany(s => s.SupplierCategories)
                .HasForeignKey(sc => sc.SupplierId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SupplierCategory>()
                .HasOne(sc => sc.Category)
                .WithMany(c => c.SupplierCategories)
                .HasForeignKey(sc => sc.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            // Retailer - Category Many-to-Many
            modelBuilder.Entity<RetailerCategory>()
                .HasOne(rc => rc.Retailer)
                .WithMany(r => r.RetailerCategories)
                .HasForeignKey(rc => rc.RetailerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RetailerCategory>()
                .HasOne(rc => rc.Category)
                .WithMany(c => c.RetailerCategories)
                .HasForeignKey(rc => rc.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);


            // Product - Inventory (one-to-many)
            modelBuilder.Entity<Inventory>()
                .HasOne(i => i.Product)
                .WithMany(p => p.Inventories)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Warehouse - Inventory (one-to-many)
            modelBuilder.Entity<Inventory>()
                .HasOne(i => i.Warehouse)
                .WithMany(w => w.Inventories)
                .HasForeignKey(i => i.WarehouseId)
                .OnDelete(DeleteBehavior.SetNull);

            // ProductAttributeValue - Product (many-to-one)
            modelBuilder.Entity<ProductAttributeValue>()
                .HasOne(pav => pav.Product)
                .WithMany(p => p.AttributeValues)
                .HasForeignKey(pav => pav.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // ProductAttributeValue - AttributeDefinition (many-to-one)
            modelBuilder.Entity<ProductAttributeValue>()
                .HasOne(pav => pav.AttributeDefinition)
                .WithMany()
                .HasForeignKey(pav => pav.AttributeId)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== PROCUREMENT CONFIGURATIONS ==========
            // Removed for reimplementation
            
            // ========== ORDER MANAGEMENT CONFIGURATIONS ==========
            // Removed for reimplementation
            
            // Cart - Retailer (one-to-one)
            modelBuilder.Entity<Cart>()
                .HasOne(c => c.Retailer)
                .WithOne(r => r.Cart)
                .HasForeignKey<Cart>(c => c.RetailerId)
                .OnDelete(DeleteBehavior.Cascade);

            // CartItem - Cart (many-to-one with cascade delete)
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Cart)
                .WithMany(c => c.CartItems)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            // CartItem - Product (many-to-one)
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany()
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            // ========== DELIVERY CONFIGURATIONS ==========

            // Delivery - SupplierEmployee (many-to-one)
            modelBuilder.Entity<Delivery>()
                .HasOne(d => d.DeliveryEmployee)
                .WithMany(se => se.Deliveries)
                .HasForeignKey(d => d.DeliveryEmployeeId)
                .OnDelete(DeleteBehavior.SetNull);

            // DeliveryTracking - Delivery (many-to-one)
            modelBuilder.Entity<DeliveryTracking>()
                .HasOne(dt => dt.Delivery)
                .WithMany(d => d.TrackingHistory)
                .HasForeignKey(dt => dt.DeliveryId)
                .OnDelete(DeleteBehavior.Cascade);

            // Warehouse - Supplier (many-to-one)
            modelBuilder.Entity<Warehouse>()
                .HasOne(w => w.Supplier)
                .WithMany(s => s.Warehouses)
                .HasForeignKey(w => w.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            // Vehicle - Supplier (many-to-one)
            modelBuilder.Entity<Vehicle>()
                .HasOne(v => v.Supplier)
                .WithMany(s => s.Vehicles)
                .HasForeignKey(v => v.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== PAYMENT CONFIGURATIONS ==========

            // Commission - Supplier (many-to-one)
            modelBuilder.Entity<Commission>()
                .HasOne(c => c.Supplier)
                .WithMany(s => s.Commissions)
                .HasForeignKey(c => c.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== COMMUNICATION CONFIGURATIONS ==========

            // Conversation - Supplier (many-to-one)
            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.Supplier)
                .WithMany(s => s.Conversations)
                .HasForeignKey(c => c.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            // Conversation - Retailer (many-to-one)
            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.Retailer)
                .WithMany(r => r.Conversations)
                .HasForeignKey(c => c.RetailerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Message - Conversation (many-to-one)
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Message - Sender (many-to-one)
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            // MessageViolation - Message (one-to-one)
            modelBuilder.Entity<MessageViolation>()
                .HasOne(mv => mv.Message)
                .WithOne()
                .HasForeignKey<MessageViolation>(mv => mv.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            // ========== SYSTEM CONFIGURATIONS ==========

            // AuditLog Configuration
            modelBuilder.Entity<AuditLog>()
                .HasOne(al => al.PerformedByUser)
                .WithMany()
                .HasForeignKey(al => al.PerformedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Notification - User (many-to-one)
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Rating removed for now

            // Rating - Retailer (many-to-one)
            modelBuilder.Entity<Rating>()
                .HasOne(r => r.Retailer)
                .WithMany(r => r.GivenRatings)
                .HasForeignKey(r => r.RetailerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Rating - Supplier (many-to-one)
            modelBuilder.Entity<Rating>()
                .HasOne(r => r.Supplier)
                .WithMany(s => s.ReceivedRatings)
                .HasForeignKey(r => r.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            // Module 3 Relationships (No Cascade)
            modelBuilder.Entity<PurchaseOrder>()
                .HasOne(po => po.Retailer)
                .WithMany(r => r.PurchaseOrders)
                .HasForeignKey(po => po.RetailerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PurchaseOrder>()
                .HasOne(po => po.Supplier)
                .WithMany(s => s.PurchaseOrders)
                .HasForeignKey(po => po.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Retailer)
                .WithMany(r => r.Orders)
                .HasForeignKey(o => o.RetailerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Supplier)
                .WithMany(s => s.Orders)
                .HasForeignKey(o => o.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PurchaseOrder>()
                .HasOne(po => po.TenderBid)
                .WithMany()
                .HasForeignKey(po => po.TenderBidId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PurchaseOrder>()
                .HasOne(po => po.Order)
                .WithMany(o => o.PurchaseOrders)
                .HasForeignKey(po => po.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderStatusHistory>()
                .HasOne(h => h.ChangedByUser)
                .WithMany()
                .HasForeignKey(h => h.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PurchaseOrderItem>()
                .HasOne(poi => poi.Product)
                .WithMany(p => p.PurchaseOrderItems)
                .HasForeignKey(poi => poi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TenderBid>()
                .HasOne(tb => tb.Supplier)
                .WithMany(s => s.TenderBids)
                .HasForeignKey(tb => tb.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== INDEXES AND UNIQUE CONSTRAINTS ==========

            // User unique constraints
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.FAN)
                .IsUnique();

            // FaydaRegistry unique constraint
            modelBuilder.Entity<FaydaRegistry>()
                .HasIndex(f => f.FAN)
                .IsUnique();

            // Supplier unique constraints
            modelBuilder.Entity<Supplier>()
                .HasIndex(s => s.LicenseNumber)
                .IsUnique();

            modelBuilder.Entity<Supplier>()
                .HasIndex(s => s.TaxIdentificationNumber)
                .IsUnique();

            // Product unique constraints
            modelBuilder.Entity<Product>()
                .HasIndex(p => new { p.SupplierId, p.ProductName })
                .IsUnique();

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.SKU)
                .IsUnique();

            // PurchaseOrder unique constraint removed
            // Conversation unique constraint (prevent duplicate conversations between same supplier-retailer)
            modelBuilder.Entity<Conversation>()
                .HasIndex(c => new { c.SupplierId, c.RetailerId })
                .IsUnique();

            // ========== DEFAULT VALUES ==========

            // User defaults
            modelBuilder.Entity<User>()
                .Property(u => u.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<User>()
                .Property(u => u.AccountStatus)
                .HasDefaultValue("Pending");

            modelBuilder.Entity<User>()
                .Property(u => u.LoginAttempts)
                .HasDefaultValue(0);

            // Supplier defaults
            modelBuilder.Entity<Supplier>()
                .Property(s => s.VerificationStatus)
                .HasDefaultValue("Pending");

            modelBuilder.Entity<Supplier>()
                .Property(s => s.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            // Product defaults
            modelBuilder.Entity<Product>()
                .Property(p => p.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            // Order defaults removed
            // Commission defaults
            modelBuilder.Entity<Commission>()
                .Property(c => c.Status)
                .HasDefaultValue("Pending");

            modelBuilder.Entity<Commission>()
                .Property(c => c.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            // Notification defaults
            modelBuilder.Entity<Notification>()
                .Property(n => n.IsRead)
                .HasDefaultValue(false);

            modelBuilder.Entity<Notification>()
                .Property(n => n.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            // Delivery defaults
            modelBuilder.Entity<Delivery>()
                .Property(d => d.DeliveryStatus)
                .HasDefaultValue("Preparing");

            // ========== LOGISTICS 2.0 GLOBAL FILTERS ==========
            modelBuilder.Entity<Warehouse>().HasQueryFilter(w => !w.IsDeleted);
            modelBuilder.Entity<Vehicle>().HasQueryFilter(v => !v.IsDeleted);
            modelBuilder.Entity<SupplierEmployee>().HasQueryFilter(se => !se.IsDeleted);
            modelBuilder.Entity<VehicleDocument>().HasQueryFilter(vd => vd.IsActive);
            modelBuilder.Entity<EmployeeDocument>().HasQueryFilter(ed => ed.IsActive);

            // ========== LOGISTICS 2.0 RELATIONSHIPS ==========

            // Warehouse - Manager History
            modelBuilder.Entity<WarehouseManagerHistory>()
                .HasOne(wmh => wmh.Warehouse)
                .WithMany(w => w.ManagerHistories)
                .HasForeignKey(wmh => wmh.WarehouseId);

            // Vehicle - Driver History
            modelBuilder.Entity<VehicleDriverHistory>()
                .HasOne(vdh => vdh.Vehicle)
                .WithMany(v => v.DriverHistories)
                .HasForeignKey(vdh => vdh.VehicleId);

            // Employee Warehouse Access
            modelBuilder.Entity<EmployeeWarehouseAccess>()
                .HasOne(ewa => ewa.SupplierEmployee)
                .WithMany(se => se.HubAccesses)
                .HasForeignKey(ewa => ewa.SupplierEmployeeId);

            modelBuilder.Entity<EmployeeWarehouseAccess>()
                .HasOne(ewa => ewa.Warehouse)
                .WithMany(w => w.StaffAccesses)
                .HasForeignKey(ewa => ewa.WarehouseId);

            // Dispatch Tasks
            modelBuilder.Entity<DispatchTask>()
                .HasOne(dt => dt.Order)
                .WithMany()
                .HasForeignKey(dt => dt.OrderId);

            // Inventory Transfers
            modelBuilder.Entity<InventoryTransfer>()
                .HasOne(it => it.SourceWarehouse)
                .WithMany(w => w.OutgoingTransfers)
                .HasForeignKey(it => it.SourceWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InventoryTransfer>()
                .HasOne(it => it.DestinationWarehouse)
                .WithMany(w => w.IncomingTransfers)
                .HasForeignKey(it => it.DestinationWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InventoryTransfer>()
                .HasOne(it => it.RequestedBy)
                .WithMany(se => se.RequestedTransfers)
                .HasForeignKey(it => it.RequestedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InventoryTransfer>()
                .HasOne(it => it.ApprovedBy)
                .WithMany(se => se.ApprovedTransfers)
                .HasForeignKey(it => it.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Incident Reports
            modelBuilder.Entity<IncidentReport>()
                .HasOne(ir => ir.ReportedBy)
                .WithMany(se => se.ReportedIncidents)
                .HasForeignKey(ir => ir.ReportedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Dispatch Tasks
            modelBuilder.Entity<DispatchTask>()
                .HasOne(dt => dt.DeliveryAgent)
                .WithMany(se => se.AssignedTasks)
                .HasForeignKey(dt => dt.DeliveryAgentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}