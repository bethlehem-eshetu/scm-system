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
        public DbSet<BankAccount> BankAccounts { get; set; }
        public DbSet<RetailerAddress> RetailerAddresses { get; set; }
        public DbSet<RetailerPaymentMethod> RetailerPaymentMethods { get; set; }
        public DbSet<RetailerPreference> RetailerPreferences { get; set; }

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

        // ========== NEW INVENTORY MANAGEMENT TABLES ==========
        public DbSet<InventoryReservation> InventoryReservations { get; set; }
        public DbSet<InventoryMovement> InventoryMovements { get; set; }
        public DbSet<InventorySnapshot> InventorySnapshots { get; set; }
        public DbSet<StockTransfer> StockTransfers { get; set; }
        public DbSet<InventoryAdjustment> InventoryAdjustments { get; set; }
        public DbSet<InboundShipment> InboundShipments { get; set; }
        public DbSet<InboundShipmentItem> InboundShipmentItems { get; set; }

        // Identity Verification Mock
        public DbSet<FaydaRegistry> FaydaRegistries { get; set; }
        public DbSet<FaydaVerification> FaydaVerifications { get; set; }
        public DbSet<EmailLog> EmailLogs { get; set; }
        public DbSet<SupportTicket> SupportTickets { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<AdminNotification> AdminNotifications { get; set; }


        // Product Catalog Tables
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<SupplierCategory> SupplierCategories { get; set; }
        public DbSet<RetailerCategory> RetailerCategories { get; set; }

        public DbSet<ProductAttributeDefinition> ProductAttributeDefinitions { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<ProductAttributeValue> ProductAttributeValues { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<InventoryHistory> InventoryHistories { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }

        // Procurement & Order Management Tables
        public DbSet<Tender> Tenders { get; set; }
        public DbSet<TenderItem> TenderItems { get; set; }
        public DbSet<TenderBid> TenderBids { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }
        public DbSet<DispatchOverrideLog> DispatchOverrideLogs { get; set; }
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
        public DbSet<Payment> Payments { get; set; }
        public DbSet<SupplierTransaction> SupplierTransactions { get; set; }
        public DbSet<Refund> Refunds { get; set; }
        public DbSet<DeadLetterWebhook> DeadLetterWebhooks { get; set; }

        // Communication Tables
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageViolation> MessageViolations { get; set; }

        // System Tables
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<DeliveryRating> DeliveryRatings { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<SystemConfiguration> SystemConfigurations { get; set; }
        public DbSet<EmailTemplate> EmailTemplates { get; set; }

        // Add this DbSet inside your ApplicationDbContext class
        public DbSet<DepositRecord> DepositRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ========== INVENTORY RESERVATION CONFIGURATIONS ==========

            // InventoryReservation - Product (many-to-one)
            modelBuilder.Entity<InventoryReservation>()
                .HasOne(r => r.Product)
                .WithMany()
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // InventoryReservation - PurchaseOrder (many-to-one)
            modelBuilder.Entity<InventoryReservation>()
                .HasOne(r => r.PurchaseOrder)
                .WithMany(po => po.InventoryReservations)
                .HasForeignKey(r => r.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            // InventoryReservation - Order (many-to-one)
            modelBuilder.Entity<InventoryReservation>()
                .HasOne(r => r.Order)
                .WithMany()
                .HasForeignKey(r => r.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            // InventoryReservation - Supplier (many-to-one)
            modelBuilder.Entity<InventoryReservation>()
                .HasOne(r => r.Supplier)
                .WithMany()
                .HasForeignKey(r => r.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            // InventoryReservation - Warehouse (many-to-one)
            modelBuilder.Entity<InventoryReservation>()
                .HasOne(r => r.Warehouse)
                .WithMany()
                .HasForeignKey(r => r.WarehouseId)
                .OnDelete(DeleteBehavior.SetNull);

            // InventoryReservation Indexes
            modelBuilder.Entity<InventoryReservation>()
                .HasIndex(r => new { r.ProductId, r.Status })
                .HasDatabaseName("IX_InventoryReservation_Product_Status");

            modelBuilder.Entity<InventoryReservation>()
                .HasIndex(r => r.ExpiresAt)
                .HasDatabaseName("IX_InventoryReservation_ExpiresAt");

            modelBuilder.Entity<InventoryReservation>()
                .HasIndex(r => r.OrderId)
                .HasDatabaseName("IX_InventoryReservation_OrderId");

            modelBuilder.Entity<InventoryReservation>()
                .HasIndex(r => r.PurchaseOrderId)
                .HasDatabaseName("IX_InventoryReservation_PurchaseOrderId");

            // InventoryReservation Default Values
            modelBuilder.Entity<InventoryReservation>()
                .Property(r => r.Status)
                .HasDefaultValue("Pending");

            modelBuilder.Entity<InventoryReservation>()
                .Property(r => r.Priority)
                .HasDefaultValue(1);

            modelBuilder.Entity<InventoryReservation>()
                .Property(r => r.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            // ========== INVENTORY MOVEMENT CONFIGURATIONS ==========

            // InventoryMovement - Product (many-to-one)
            modelBuilder.Entity<InventoryMovement>()
                .HasOne(m => m.Product)
                .WithMany()
                .HasForeignKey(m => m.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // InventoryMovement - Warehouse (many-to-one)
            modelBuilder.Entity<InventoryMovement>()
                .HasOne(m => m.Warehouse)
                .WithMany()
                .HasForeignKey(m => m.WarehouseId)
                .OnDelete(DeleteBehavior.SetNull);

            // InventoryMovement - User (many-to-one)
            modelBuilder.Entity<InventoryMovement>()
                .HasOne(m => m.PerformedByUser)
                .WithMany()
                .HasForeignKey(m => m.PerformedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // InventoryMovement Indexes
            modelBuilder.Entity<InventoryMovement>()
                .HasIndex(m => new { m.ProductId, m.CreatedAt })
                .HasDatabaseName("IX_InventoryMovement_Product_Date");

            modelBuilder.Entity<InventoryMovement>()
                .HasIndex(m => m.ReferenceNumber)
                .HasDatabaseName("IX_InventoryMovement_ReferenceNumber");

            modelBuilder.Entity<InventoryMovement>()
                .HasIndex(m => m.MovementType)
                .HasDatabaseName("IX_InventoryMovement_Type");

            // InventoryMovement Default Values
            modelBuilder.Entity<InventoryMovement>()
                .Property(m => m.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            // ========== STOCK TRANSFER CONFIGURATIONS ==========
            modelBuilder.Entity<StockTransfer>()
                .HasOne(st => st.SourceWarehouse)
                .WithMany()
                .HasForeignKey(st => st.SourceWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockTransfer>()
                .HasOne(st => st.DestinationWarehouse)
                .WithMany()
                .HasForeignKey(st => st.DestinationWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockTransfer>()
                .HasOne(st => st.Product)
                .WithMany()
                .HasForeignKey(st => st.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockTransfer>()
                .HasOne(st => st.RequestedBy)
                .WithMany()
                .HasForeignKey(st => st.RequestedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockTransfer>()
                .HasOne(st => st.ApprovedBy)
                .WithMany()
                .HasForeignKey(st => st.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InventoryAdjustment>()
                .HasOne(ia => ia.PerformedBy)
                .WithMany()
                .HasForeignKey(ia => ia.PerformedById)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== INBOUND SHIPMENT CONFIGURATIONS ==========
            modelBuilder.Entity<InboundShipment>()
                .HasOne(s => s.Supplier)
                .WithMany(sup => sup.InboundShipments)
                .HasForeignKey(s => s.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InboundShipment>()
                .HasOne(s => s.Warehouse)
                .WithMany(w => w.InboundShipments)
                .HasForeignKey(s => s.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InboundShipmentItem>()
                .HasOne(si => si.InboundShipment)
                .WithMany(s => s.Items)
                .HasForeignKey(si => si.InboundShipmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InboundShipmentItem>()
                .HasOne(si => si.Product)
                .WithMany()
                .HasForeignKey(si => si.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== INVENTORY SNAPSHOT CONFIGURATIONS ==========
            modelBuilder.Entity<InventorySnapshot>()
                .HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InventorySnapshot>()
                .HasOne(i => i.Warehouse)
                .WithMany()
                .HasForeignKey(i => i.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

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

            // Retailer - RetailerPreference (one-to-one)
            modelBuilder.Entity<Retailer>()
                .HasOne(r => r.Preference)
                .WithOne(rp => rp.Retailer)
                .HasForeignKey<RetailerPreference>(rp => rp.RetailerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Supplier - SupplierEmployee (one-to-many)
            modelBuilder.Entity<SupplierEmployee>()
                .HasOne(se => se.Supplier)
                .WithMany(s => s.Employees)
                .HasForeignKey(se => se.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            // Supplier - BankAccount (one-to-many)
            modelBuilder.Entity<BankAccount>()
                .HasOne(ba => ba.Supplier)
                .WithMany(s => s.BankAccounts)
                .HasForeignKey(ba => ba.SupplierId)
                .OnDelete(DeleteBehavior.Cascade);

            // SupplierEmployee - Warehouse (many-to-one)
            modelBuilder.Entity<SupplierEmployee>()
                .HasOne(se => se.Warehouse)
                .WithMany(w => w.Employees)
                .HasForeignKey(se => se.WarehouseId)
                .OnDelete(DeleteBehavior.SetNull);

            // SupplierEmployee - Vehicle (many-to-one)
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

            // User - UserSession (one-to-many)
            modelBuilder.Entity<UserSession>()
                .HasOne(us => us.User)
                .WithMany(u => u.UserSessions)
                .HasForeignKey(us => us.UserId)
                .OnDelete(DeleteBehavior.Cascade);

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

            // Payment Configuration
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Order)
                .WithMany()
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Retailer)
                .WithMany()
                .HasForeignKey(p => p.RetailerId)
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

            // Module 3 Relationships
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

            // Vehicle unique constraints
            modelBuilder.Entity<Vehicle>()
                .HasIndex(v => v.LicensePlate)
                .IsUnique();

            // Warehouse unique constraints and properties
            modelBuilder.Entity<Warehouse>()
                .HasIndex(w => w.WarehouseCode)
                .IsUnique();
            
            modelBuilder.Entity<Warehouse>()
                .Property(w => w.ReceivingAreaSizeM2)
                .HasPrecision(18, 2);

            // SupplierEmployee unique constraints
            modelBuilder.Entity<SupplierEmployee>()
                .HasIndex(se => se.Email)
                .IsUnique();
            modelBuilder.Entity<SupplierEmployee>()
                .HasIndex(se => se.Phone)
                .IsUnique();

            // Conversation unique constraint
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

            // ========== SUPPLIER TRANSACTION CONFIGURATIONS ==========
            modelBuilder.Entity<SupplierTransaction>()
                .HasOne(st => st.Supplier)
                .WithMany(s => s.SupplierTransactions)
                .HasForeignKey(st => st.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SupplierTransaction>()
                .HasOne(st => st.Order)
                .WithMany()
                .HasForeignKey(st => st.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SupplierTransaction>()
                .HasIndex(st => st.OrderId)
                .HasDatabaseName("IX_SupplierTransaction_OrderId");

            // ========== PAYMENT INDEXES ==========
            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.TxRef)
                .IsUnique()
                .HasDatabaseName("UQ_Payment_TxRef");

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.OrderId)
                .HasDatabaseName("IX_Payment_OrderId");

            modelBuilder.Entity<Commission>()
                .HasIndex(c => c.OrderId)
                .HasDatabaseName("IX_Commission_OrderId");

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

            // InventoryHistory configurations
            modelBuilder.Entity<InventoryHistory>()
                .HasOne(ih => ih.Product)
                .WithMany()
                .HasForeignKey(ih => ih.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InventoryHistory>()
                .HasOne(ih => ih.Warehouse)
                .WithMany()
                .HasForeignKey(ih => ih.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InventoryHistory>()
                .HasOne(ih => ih.PerformedBy)
                .WithMany()
                .HasForeignKey(ih => ih.SupplierEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Financial & Audit Indices
            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.TxRef)
                .IsUnique();

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.OrderId);

            modelBuilder.Entity<Commission>()
                .HasIndex(c => c.OrderId);

            modelBuilder.Entity<SupplierTransaction>()
                .HasIndex(st => st.OrderId);

            modelBuilder.Entity<SupplierTransaction>()
                .HasIndex(st => st.SupplierId);

            // ========== DECIMAL PRECISION CONFIGURATIONS ==========

            // Vehicle
            modelBuilder.Entity<Vehicle>().Property(v => v.MaxLoadCapacity).HasPrecision(18, 2);
            modelBuilder.Entity<Vehicle>().Property(v => v.InternalVolumeM3).HasPrecision(18, 2);
            modelBuilder.Entity<Vehicle>().Property(v => v.FuelTankCapacity).HasPrecision(18, 2);
            modelBuilder.Entity<Vehicle>().Property(v => v.Mileage).HasPrecision(18, 2);
            modelBuilder.Entity<Vehicle>().Property(v => v.CurrentMileage).HasPrecision(18, 2);
            modelBuilder.Entity<Vehicle>().Property(v => v.FuelEfficiency).HasPrecision(18, 2);
            modelBuilder.Entity<Vehicle>().Property(v => v.TireChangeDueMileage).HasPrecision(18, 2);

            // Warehouse
            modelBuilder.Entity<Warehouse>().Property(w => w.ReceivingAreaSizeM2).HasPrecision(18, 2);

            // MaintenanceRecord
            modelBuilder.Entity<MaintenanceRecord>().Property(m => m.OdometerAtService).HasPrecision(18, 2);
            modelBuilder.Entity<MaintenanceRecord>().Property(m => m.NextServiceMileage).HasPrecision(18, 2);

            // IncidentReport
            modelBuilder.Entity<IncidentReport>().Property(i => i.Lat).HasPrecision(18, 8);
            modelBuilder.Entity<IncidentReport>().Property(i => i.Long).HasPrecision(18, 8);

            // DispatchTask
            modelBuilder.Entity<DispatchTask>().Property(d => d.DeliveryLat).HasPrecision(18, 8);
            modelBuilder.Entity<DispatchTask>().Property(d => d.DeliveryLong).HasPrecision(18, 8);

            // Supplier
            modelBuilder.Entity<Supplier>().Property(s => s.CommissionRate).HasPrecision(5, 2);
            modelBuilder.Entity<Supplier>().Property(s => s.Balance).HasPrecision(18, 2);

            // SupplierEmployee
            modelBuilder.Entity<SupplierEmployee>().Property(s => s.MonthlySalary).HasPrecision(18, 2);

            // Order
            modelBuilder.Entity<Order>().Property(o => o.Subtotal).HasPrecision(18, 2);
            modelBuilder.Entity<Order>().Property(o => o.VAT).HasPrecision(18, 2);
            modelBuilder.Entity<Order>().Property(o => o.TotalAmount).HasPrecision(18, 2);

            // PurchaseOrder
            modelBuilder.Entity<PurchaseOrder>().Property(p => p.Subtotal).HasPrecision(18, 2);
            modelBuilder.Entity<PurchaseOrder>().Property(p => p.VAT).HasPrecision(18, 2);
            modelBuilder.Entity<PurchaseOrder>().Property(p => p.Discount).HasPrecision(18, 2);
            modelBuilder.Entity<PurchaseOrder>().Property(p => p.TotalAmount).HasPrecision(18, 2);

            // PurchaseOrderItem & OrderItem
            modelBuilder.Entity<PurchaseOrderItem>().Property(p => p.UnitPrice).HasPrecision(18, 2);
            modelBuilder.Entity<OrderItem>().Property(o => o.UnitPrice).HasPrecision(18, 2);

            // Product
            modelBuilder.Entity<Product>().Property(p => p.BasePrice).HasPrecision(18, 2);
            modelBuilder.Entity<Product>().Property(p => p.CostPrice).HasPrecision(18, 2);
            modelBuilder.Entity<Product>().Property(p => p.WholesalePrice).HasPrecision(18, 2);
            modelBuilder.Entity<Product>().Property(p => p.DiscountPercentage).HasPrecision(5, 2);
            modelBuilder.Entity<Product>().Property(p => p.TaxRate).HasPrecision(5, 2);
            modelBuilder.Entity<Product>().Property(p => p.ShippingWeight).HasPrecision(18, 2);
            modelBuilder.Entity<Product>().Property(p => p.ShippingLength).HasPrecision(18, 2);
            modelBuilder.Entity<Product>().Property(p => p.ShippingWidth).HasPrecision(18, 2);
            modelBuilder.Entity<Product>().Property(p => p.ShippingHeight).HasPrecision(18, 2);

            // Commission
            modelBuilder.Entity<Commission>().Property(c => c.OrderAmount).HasPrecision(18, 2);
            modelBuilder.Entity<Commission>().Property(c => c.CommissionRate).HasPrecision(5, 2);
            modelBuilder.Entity<Commission>().Property(c => c.CommissionAmount).HasPrecision(18, 2);
            modelBuilder.Entity<Commission>().Property(c => c.CommissionRateAtTransaction).HasPrecision(5, 2);

            // Payment
            modelBuilder.Entity<Payment>().Property(p => p.Amount).HasPrecision(18, 2);

            // Refund
            modelBuilder.Entity<Refund>().Property(r => r.Amount).HasPrecision(18, 2);

            // Tender & TenderBid
            modelBuilder.Entity<Tender>().Property(t => t.BudgetMin).HasPrecision(18, 2);
            modelBuilder.Entity<Tender>().Property(t => t.BudgetMax).HasPrecision(18, 2);
            modelBuilder.Entity<TenderBid>().Property(t => t.UnitPrice).HasPrecision(18, 2);
            modelBuilder.Entity<TenderBid>().Property(t => t.ProposedTotalAmount).HasPrecision(18, 2);

            // GPSLog
            modelBuilder.Entity<GPSLog>().Property(g => g.SpeedKph).HasPrecision(18, 2);
        }
    }
}