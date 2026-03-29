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
                    context.ProductCategories.AddRange(
                        new ProductCategory { CategoryName = "Electronics & Gadgets", Description = "Electronic devices and accessories" },
                        new ProductCategory { CategoryName = "Clothing & Apparel", Description = "Apparel and garments" },
                        new ProductCategory { CategoryName = "Food & Beverage", Description = "Consumable goods and drinks" },
                        new ProductCategory { CategoryName = "Home & Garden", Description = "Furniture and home accessories" },
                        new ProductCategory { CategoryName = "Health & Beauty", Description = "Personal care products" },
                        new ProductCategory { CategoryName = "Industrial Tools", Description = "Industrial and manufacturing equipment" },
                        new ProductCategory { CategoryName = "Agriculture", Description = "Farming and agricultural supplies" },
                        new ProductCategory { CategoryName = "Raw Materials", Description = "Basic materials for manufacturing" }
                    );
                    context.SaveChanges();
                    Console.WriteLine("=================================");
                    Console.WriteLine("Product Categories seeded!");
                    Console.WriteLine("=================================");
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