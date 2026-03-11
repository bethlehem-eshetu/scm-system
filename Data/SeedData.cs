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
                // Look for any admin user
                if (context.Users.Any(u => u.Role == "Admin"))
                {
                    return; // DB has been seeded
                }

                // Hash password (Admin@123)
                string adminPasswordHash = HashPassword("Admin@123");

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