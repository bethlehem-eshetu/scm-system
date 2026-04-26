using Microsoft.EntityFrameworkCore;
using SCM_System.Models.Entities;
using System.Security.Cryptography;
using System.Text;

namespace SCM_System.Data
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // Seed Product Categories
                if (!context.ProductCategories.Any())
                {
                    var categories = new List<ProductCategory>
                    {
                        // Clothing
                        new ProductCategory { CategoryName = "Clothing", Description = "Apparel and garments", Level = 1 },
                        // Electronics
                        new ProductCategory { CategoryName = "Electronics", Description = "Electronic devices and gadgets", Level = 1 },
                        // Food & Beverage
                        new ProductCategory { CategoryName = "Food & Beverage", Description = "Consumable goods and drinks", Level = 1 },
                        // Furniture
                        new ProductCategory { CategoryName = "Furniture", Description = "Home and office furniture", Level = 1 },
                        // Raw Materials
                        new ProductCategory { CategoryName = "Raw Materials", Description = "Inputs for manufacturing", Level = 1 },
                        // Construction Materials
                        new ProductCategory { CategoryName = "Construction Materials", Description = "Building supplies", Level = 1 },
                        // Pharmaceuticals
                        new ProductCategory { CategoryName = "Pharmaceuticals", Description = "Medicine and medical gear", Level = 1 },
                        // Office Supplies
                        new ProductCategory { CategoryName = "Office Supplies", Description = "Business operation essentials", Level = 1 },
                        // Automotive Parts
                        new ProductCategory { CategoryName = "Automotive Parts", Description = "Vehicle maintenance parts", Level = 1 },
                        // Agricultural Products
                        new ProductCategory { CategoryName = "Agricultural Products", Description = "Farming inputs and produce", Level = 1 },
                        // Beauty & Personal Care
                        new ProductCategory { CategoryName = "Beauty & Personal Care", Description = "Cosmetics and hygiene", Level = 1 },
                        // Home & Kitchen
                        new ProductCategory { CategoryName = "Home & Kitchen", Description = "Domestic appliances and decor", Level = 1 },
                        // Health & Wellness
                        new ProductCategory { CategoryName = "Health & Wellness", Description = "Vitamins, supplements, and fitness", Level = 1 },
                        // Books & Media
                        new ProductCategory { CategoryName = "Books & Media", Description = "Educational and entertainment materials", Level = 1 },
                        // Industrial Machinery
                        new ProductCategory { CategoryName = "Industrial Machinery", Description = "Heavy equipment and tools", Level = 1 },
                        // Toys & Games
                        new ProductCategory { CategoryName = "Toys & Games", Description = "Play and educational items", Level = 1 }
                    };

                    context.ProductCategories.AddRange(categories);
                    context.SaveChanges();

                    // Subcategories for Clothing
                    var clothing = categories.First(c => c.CategoryName == "Clothing");
                    context.ProductCategories.AddRange(
                        new ProductCategory { CategoryName = "Men's Clothing", ParentCategoryId = clothing.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Women's Clothing", ParentCategoryId = clothing.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Children's Clothing", ParentCategoryId = clothing.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Traditional Ethiopian Clothing", ParentCategoryId = clothing.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Sportswear", ParentCategoryId = clothing.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Uniforms", ParentCategoryId = clothing.Id, Level = 2 }
                    );

                    // Subcategories for Electronics
                    var electronics = categories.First(c => c.CategoryName == "Electronics");
                    context.ProductCategories.AddRange(
                        new ProductCategory { CategoryName = "Mobile Phones", ParentCategoryId = electronics.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Computers & Laptops", ParentCategoryId = electronics.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Tablets", ParentCategoryId = electronics.Id, Level = 2 },
                        new ProductCategory { CategoryName = "TVs & Home Theater", ParentCategoryId = electronics.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Audio Equipment", ParentCategoryId = electronics.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Accessories", ParentCategoryId = electronics.Id, Level = 2 }
                    );

                    // Subcategories for Food & Beverage
                    var food = categories.First(c => c.CategoryName == "Food & Beverage");
                    context.ProductCategories.AddRange(
                        new ProductCategory { CategoryName = "Fresh Produce", ParentCategoryId = food.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Dairy Products", ParentCategoryId = food.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Meat & Poultry", ParentCategoryId = food.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Packaged Foods", ParentCategoryId = food.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Beverages", ParentCategoryId = food.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Coffee & Tea", ParentCategoryId = food.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Spices & Seasonings", ParentCategoryId = food.Id, Level = 2 }
                    );

                    // Subcategories for Furniture
                    var furniture = categories.First(c => c.CategoryName == "Furniture");
                    context.ProductCategories.AddRange(
                        new ProductCategory { CategoryName = "Living Room Furniture", ParentCategoryId = furniture.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Bedroom Furniture", ParentCategoryId = furniture.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Office Furniture", ParentCategoryId = furniture.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Kitchen Furniture", ParentCategoryId = furniture.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Outdoor Furniture", ParentCategoryId = furniture.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Custom Furniture", ParentCategoryId = furniture.Id, Level = 2 }
                    );

                    // Subcategories for Raw Materials
                    var raw = categories.First(c => c.CategoryName == "Raw Materials");
                    context.ProductCategories.AddRange(
                        new ProductCategory { CategoryName = "Textile Materials", ParentCategoryId = raw.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Wood & Lumber", ParentCategoryId = raw.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Metal & Steel", ParentCategoryId = raw.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Plastic Materials", ParentCategoryId = raw.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Chemicals", ParentCategoryId = raw.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Packaging Materials", ParentCategoryId = raw.Id, Level = 2 }
                    );

                    // Subcategories for Construction
                    var construction = categories.First(c => c.CategoryName == "Construction Materials");
                    context.ProductCategories.AddRange(
                        new ProductCategory { CategoryName = "Cement & Concrete", ParentCategoryId = construction.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Bricks & Blocks", ParentCategoryId = construction.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Paints & Coatings", ParentCategoryId = construction.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Plumbing Materials", ParentCategoryId = construction.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Electrical Materials", ParentCategoryId = construction.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Tiles & Flooring", ParentCategoryId = construction.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Roofing Materials", ParentCategoryId = construction.Id, Level = 2 }
                    );

                    // Subcategories for Pharmaceuticals
                    var pharma = categories.First(c => c.CategoryName == "Pharmaceuticals");
                    context.ProductCategories.AddRange(
                        new ProductCategory { CategoryName = "Prescription Drugs", ParentCategoryId = pharma.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Over-the-Counter Medications", ParentCategoryId = pharma.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Medical Supplies", ParentCategoryId = pharma.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Laboratory Equipment", ParentCategoryId = pharma.Id, Level = 2 },
                        new ProductCategory { CategoryName = "PPE & Safety Equipment", ParentCategoryId = pharma.Id, Level = 2 }
                    );

                    // Subcategories for Office Supplies
                    var office = categories.First(c => c.CategoryName == "Office Supplies");
                    context.ProductCategories.AddRange(
                        new ProductCategory { CategoryName = "Stationery", ParentCategoryId = office.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Printer Cartridges", ParentCategoryId = office.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Paper Products", ParentCategoryId = office.Id, Level = 2 },
                        // Office Furniture already in Furniture? I'll add it here too if specifically requested
                        new ProductCategory { CategoryName = "Computer Accessories", ParentCategoryId = office.Id, Level = 2 }
                    );

                    // Subcategories for Automotive
                    var auto = categories.First(c => c.CategoryName == "Automotive Parts");
                    context.ProductCategories.AddRange(
                        new ProductCategory { CategoryName = "Engine Parts", ParentCategoryId = auto.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Brake Systems", ParentCategoryId = auto.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Tires & Wheels", ParentCategoryId = auto.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Batteries", ParentCategoryId = auto.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Lubricants & Oils", ParentCategoryId = auto.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Spare Parts", ParentCategoryId = auto.Id, Level = 2 }
                    );

                    // Subcategories for Agricultural Products
                    var agri = categories.First(c => c.CategoryName == "Agricultural Products");
                    context.ProductCategories.AddRange(
                        new ProductCategory { CategoryName = "Seeds & Fertilizers", ParentCategoryId = agri.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Farming Equipment", ParentCategoryId = agri.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Livestock Feed", ParentCategoryId = agri.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Pesticides", ParentCategoryId = agri.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Irrigation Supplies", ParentCategoryId = agri.Id, Level = 2 }
                    );

                    // Subcategories for Beauty & Personal Care
                    var beauty = categories.First(c => c.CategoryName == "Beauty & Personal Care");
                    context.ProductCategories.AddRange(
                        new ProductCategory { CategoryName = "Cosmetics", ParentCategoryId = beauty.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Skincare", ParentCategoryId = beauty.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Haircare", ParentCategoryId = beauty.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Fragrances", ParentCategoryId = beauty.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Grooming Products", ParentCategoryId = beauty.Id, Level = 2 }
                    );

                    // Subcategories for Home & Kitchen
                    var home = categories.First(c => c.CategoryName == "Home & Kitchen");
                    context.ProductCategories.AddRange(
                        new ProductCategory { CategoryName = "Kitchen Appliances", ParentCategoryId = home.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Cookware", ParentCategoryId = home.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Home Decor", ParentCategoryId = home.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Bedding & Linens", ParentCategoryId = home.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Storage & Organization", ParentCategoryId = home.Id, Level = 2 }
                    );

                    // Subcategories for Health & Wellness
                    var health = categories.First(c => c.CategoryName == "Health & Wellness");
                    context.ProductCategories.AddRange(
                        new ProductCategory { CategoryName = "Vitamins & Supplements", ParentCategoryId = health.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Fitness Equipment", ParentCategoryId = health.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Personal Care Devices", ParentCategoryId = health.Id, Level = 2 }
                    );

                    // Subcategories for Books & Media
                    var books = categories.First(c => c.CategoryName == "Books & Media");
                    context.ProductCategories.AddRange(
                        new ProductCategory { CategoryName = "Educational Materials", ParentCategoryId = books.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Fiction & Non-Fiction", ParentCategoryId = books.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Digital Media", ParentCategoryId = books.Id, Level = 2 }
                    );

                    // Subcategories for Industrial Machinery
                    var industrial = categories.First(c => c.CategoryName == "Industrial Machinery");
                    context.ProductCategories.AddRange(
                        new ProductCategory { CategoryName = "Manufacturing Equipment", ParentCategoryId = industrial.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Heavy Tools", ParentCategoryId = industrial.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Packaging Machinery", ParentCategoryId = industrial.Id, Level = 2 }
                    );

                    // Subcategories for Toys & Games
                    var toys = categories.First(c => c.CategoryName == "Toys & Games");
                    context.ProductCategories.AddRange(
                        new ProductCategory { CategoryName = "Educational Toys", ParentCategoryId = toys.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Board Games", ParentCategoryId = toys.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Outdoor Play", ParentCategoryId = toys.Id, Level = 2 }
                    );

                    context.SaveChanges();
                    Console.WriteLine("=================================");
                    Console.WriteLine("Hierarchical Product Categories seeded!");
                    Console.WriteLine("=================================");
                }
                else if (!context.ProductCategories.Any(c => c.CategoryName == "Health & Wellness"))
                {
                    // Fallback to insert the new categories if the main block didn't run
                    var newCategories = new List<ProductCategory>
                    {
                        new ProductCategory { CategoryName = "Health & Wellness", Description = "Vitamins, supplements, and fitness", Level = 1 },
                        new ProductCategory { CategoryName = "Books & Media", Description = "Educational and entertainment materials", Level = 1 },
                        new ProductCategory { CategoryName = "Industrial Machinery", Description = "Heavy equipment and tools", Level = 1 },
                        new ProductCategory { CategoryName = "Toys & Games", Description = "Play and educational items", Level = 1 }
                    };
                    context.ProductCategories.AddRange(newCategories);
                    context.SaveChanges();

                    var health = newCategories.First(c => c.CategoryName == "Health & Wellness");
                    context.ProductCategories.AddRange(
                        new ProductCategory { CategoryName = "Vitamins & Supplements", ParentCategoryId = health.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Fitness Equipment", ParentCategoryId = health.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Personal Care Devices", ParentCategoryId = health.Id, Level = 2 }
                    );

                    var books = newCategories.First(c => c.CategoryName == "Books & Media");
                    context.ProductCategories.AddRange(
                        new ProductCategory { CategoryName = "Educational Materials", ParentCategoryId = books.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Fiction & Non-Fiction", ParentCategoryId = books.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Digital Media", ParentCategoryId = books.Id, Level = 2 }
                    );

                    var industrial = newCategories.First(c => c.CategoryName == "Industrial Machinery");
                    context.ProductCategories.AddRange(
                        new ProductCategory { CategoryName = "Manufacturing Equipment", ParentCategoryId = industrial.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Heavy Tools", ParentCategoryId = industrial.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Packaging Machinery", ParentCategoryId = industrial.Id, Level = 2 }
                    );

                    var toys = newCategories.First(c => c.CategoryName == "Toys & Games");
                    context.ProductCategories.AddRange(
                        new ProductCategory { CategoryName = "Educational Toys", ParentCategoryId = toys.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Board Games", ParentCategoryId = toys.Id, Level = 2 },
                        new ProductCategory { CategoryName = "Outdoor Play", ParentCategoryId = toys.Id, Level = 2 }
                    );

                    context.SaveChanges();
                    Console.WriteLine("Added new market-ready categories to existing database.");
                }

                // Look for any admin user
                string adminPasswordHash = HashPassword("Admin@123");
                if (!context.Users.Any(u => u.Role == "Admin"))
                {
                    // Create admin user with gmail
                    var adminUser = new User
                    {
                        FullName = "System Administrator",
                        Email = "admin@gmail.com",  // Changed to gmail
                        PasswordHash = adminPasswordHash,
                        PhoneNumber = "0912345678",
                        Role = "Admin",
                        AccountStatus = "Active",
                        IsApproved = true,
                        EmailVerified = true,
                        PhoneVerified = true,
                        CreatedAt = DateTime.Now,
                        LoginAttempts = 0
                    };

                    context.Users.Add(adminUser);
                    context.SaveChanges();

                    Console.WriteLine("=================================");
                    Console.WriteLine("Admin user created successfully!");
                    Console.WriteLine("Email: admin@gmail.com");
                    Console.WriteLine("Password: Admin@123");
                    Console.WriteLine("=================================");
                }

                if (!context.Users.Any(u => u.Email == "retailer@gmail.com"))
                {
                    var retailerUser = new User
                    {
                        FullName = "Test Retailer",
                        Email = "retailer@gmail.com",
                        PasswordHash = adminPasswordHash,
                        PhoneNumber = "0912345679",
                        Role = "Retailer",
                        AccountStatus = "Active",
                        IsApproved = true,
                        EmailVerified = true,
                        PhoneVerified = true,
                        CreatedAt = DateTime.Now,
                        LoginAttempts = 0
                    };
                    context.Users.Add(retailerUser);
                }

                if (!context.Users.Any(u => u.Email == "supplier@gmail.com"))
                {
                    var supplierUser = new User
                    {
                        FullName = "Test Supplier",
                        Email = "supplier@gmail.com",
                        PasswordHash = adminPasswordHash,
                        PhoneNumber = "0912345680",
                        Role = "Supplier",
                        AccountStatus = "Active",
                        IsApproved = true,
                        EmailVerified = true,
                        PhoneVerified = true,
                        CreatedAt = DateTime.Now,
                        LoginAttempts = 0
                    };
                    context.Users.Add(supplierUser);
                }

                if (!context.Users.Any(u => u.Email == "warehouse@gmail.com"))
                {
                    var warehouseUser = new User
                    {
                        FullName = "Test Warehouse",
                        Email = "warehouse@gmail.com",
                        PasswordHash = adminPasswordHash,
                        PhoneNumber = "0912345681",
                        Role = "Warehouse",
                        AccountStatus = "Active",
                        IsApproved = true,
                        EmailVerified = true,
                        PhoneVerified = true,
                        CreatedAt = DateTime.Now,
                        LoginAttempts = 0
                    };
                    context.Users.Add(warehouseUser);
                }
                context.SaveChanges();

                // Add test supplier "re@gmail.com" and products
                if (!context.Users.Any(u => u.Email == "re@gmail.com"))
                {
                    var reUser = new User
                    {
                        FullName = "Test RE Supplier",
                        Email = "re@gmail.com",
                        PasswordHash = adminPasswordHash,
                        PhoneNumber = "0911223344",
                        Role = "Supplier",
                        AccountStatus = "Active",
                        IsApproved = true,
                        EmailVerified = true,
                        PhoneVerified = true,
                        CreatedAt = DateTime.Now,
                        LoginAttempts = 0
                    };
                    context.Users.Add(reUser);
                    context.SaveChanges();

                    var reSupplier = new Supplier
                    {
                        UserId = reUser.Id,
                        CompanyName = "RE Global Supplies",
                        TaxIdentificationNumber = "TIN-RE-19283",
                        LicenseNumber = "LIC-RE-19283",
                        CompanyAddress = "Addis Ababa, Bole",
                        City = "Addis Ababa",
                        VerificationStatus = "Verified",
                        CreatedAt = DateTime.Now
                    };
                    context.Suppliers.Add(reSupplier);
                    context.SaveChanges();

                    var electronicsCatId = context.ProductCategories.FirstOrDefault(c => c.CategoryName.Contains("Electronics"))?.Id ?? 1;
                    var foodCatId = context.ProductCategories.FirstOrDefault(c => c.CategoryName.Contains("Food & Beverage"))?.Id ?? 1;

                    var products = new List<Product>
                    {
                        new Product
                        {
                            SupplierId = reSupplier.Id,
                            CategoryId = electronicsCatId,
                            ProductName = "Sample Smartphone Pro",
                            Description = "A high-end smartphone for testing direct purchase.",
                            BasePrice = 35000.00m,
                            Unit = "Pcs",
                            IsAvailable = true,
                            SKU = "RE-ELEC-001",
                            CreatedAt = DateTime.Now
                        },
                        new Product
                        {
                            SupplierId = reSupplier.Id,
                            CategoryId = foodCatId,
                            ProductName = "Premium Coffee Beans 1Kg",
                            Description = "Locally sourced organic premium coffee beans.",
                            BasePrice = 850.00m,
                            Unit = "Kg",
                            IsAvailable = true,
                            SKU = "RE-FOOD-001",
                            CreatedAt = DateTime.Now
                        }
                    };
                    context.Products.AddRange(products);
                    context.SaveChanges();
                }

                // Seed System Configurations
                if (!context.SystemConfigurations.Any())
                {
                    context.SystemConfigurations.AddRange(new List<SystemConfiguration>
                    {
                        new SystemConfiguration { Key = "CommissionBronze", Value = "2.5", Description = "Commission for Bronze tier suppliers", DataType = "decimal" },
                        new SystemConfiguration { Key = "CommissionSilver", Value = "2.0", Description = "Commission for Silver tier suppliers", DataType = "decimal" },
                        new SystemConfiguration { Key = "CommissionGold", Value = "1.5", Description = "Commission for Gold tier suppliers", DataType = "decimal" },
                        new SystemConfiguration { Key = "CommissionPlatinum", Value = "1.0", Description = "Commission for Platinum tier suppliers", DataType = "decimal" },
                        new SystemConfiguration { Key = "PenaltyWarningThreshold", Value = "3", Description = "Violations before warning", DataType = "int" },
                        new SystemConfiguration { Key = "PenaltySuspensionDays", Value = "7", Description = "Days of suspension after limit", DataType = "int" },
                        new SystemConfiguration { Key = "LowStockDefaultThreshold", Value = "10", Description = "Default low stock alert level", DataType = "int" },
                        new SystemConfiguration { Key = "MaxTenderDays", Value = "30", Description = "Maximum duration for tenders", DataType = "int" },
                        new SystemConfiguration { Key = "OrderCancellationHours", Value = "24", Description = "Hours before unpaid order cancels", DataType = "int" },
                        new SystemConfiguration { Key = "AutoReleaseEscrowDays", Value = "5", Description = "Days before escrow release", DataType = "int" },
                        new SystemConfiguration { Key = "RequireSupplierApproval", Value = "true", Description = "Manual approval for suppliers", DataType = "bool" },
                        new SystemConfiguration { Key = "RequireRetailerApproval", Value = "true", Description = "Manual approval for retailers", DataType = "bool" },
                        new SystemConfiguration { Key = "DefaultAccountStatus", Value = "Pending", Description = "Status for new signups", DataType = "string" },
                        new SystemConfiguration { Key = "EnableFaydaVerification", Value = "true", Description = "Enable Fayda ID checks", DataType = "bool" },
                        new SystemConfiguration { Key = "AppUrl", Value = "https://localhost:5001", Description = "Base application URL", DataType = "string" },
                        new SystemConfiguration { Key = "SupportEmail", Value = "support@ethiochain.com", Description = "Platform support email", DataType = "string" },
                        new SystemConfiguration { Key = "Timezone", Value = "Africa/Addis_Ababa", Description = "System timezone", DataType = "string" },
                        new SystemConfiguration { Key = "Currency", Value = "ETB", Description = "Base platform currency", DataType = "string" },
                        new SystemConfiguration { Key = "DateFormat", Value = "dd MMM yyyy", Description = "Display date format", DataType = "string" },
                        new SystemConfiguration { Key = "ChapaEnvironment", Value = "Test", Description = "Chapa gateway environment", DataType = "string" },
                        new SystemConfiguration { Key = "ChapaTestMode", Value = "true", Description = "Chapa test mode toggle", DataType = "bool" }
                    });
                    context.SaveChanges();
                }

                // Seed Email Templates
                if (!context.EmailTemplates.Any())
                {
                    context.EmailTemplates.AddRange(new List<EmailTemplate>
                    {
                        new EmailTemplate { EventType = "AccountApproval", Subject = "Welcome to EthioChain - Account Approved", Body = "Dear {UserName}, your {Role} account has been approved. You can now log in.", IsActive = true },
                        new EmailTemplate { EventType = "AccountRejection", Subject = "EthioChain Account Update", Body = "Dear {UserName}, unfortunately your registration could not be approved at this time.", IsActive = true },
                        new EmailTemplate { EventType = "OrderPlaced", Subject = "New Order Received", Body = "A new order #{OrderId} has been placed in your store.", IsActive = true },
                        new EmailTemplate { EventType = "PaymentSuccessful", Subject = "Payment Received", Body = "Payment for order #{OrderId} has been successfully processed.", IsActive = true },
                        new EmailTemplate { EventType = "LowStockAlert", Subject = "Low Stock Alert: {ProductName}", Body = "Warning: Product {ProductName} has fallen below the threshold ({Threshold}).", IsActive = true }
                    });
                    context.SaveChanges();
                }


            }
        }

        private static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}