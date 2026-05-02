using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlite("Data Source=SCM_System.db"); // Assuming SQLite

            using (var context = new ApplicationDbContext(optionsBuilder.Options))
            {
                var categories = context.ProductCategories.ToList();
                Console.WriteLine($"Total Categories: {categories.Count}");
                Console.WriteLine($"Parent Categories: {categories.Count(c => c.ParentCategoryId == null)}");
                Console.WriteLine($"Sub Categories: {categories.Count(c => c.ParentCategoryId != null)}");

                var suppliers = context.Suppliers.ToList();
                foreach(var supplier in suppliers) {
                    var supplierCats = context.SupplierCategories.Where(sc => sc.SupplierId == supplier.Id).Select(sc => sc.Category).ToList();
                    Console.WriteLine($"Supplier {supplier.Id} ({supplier.CompanyName}) Categories:");
                    foreach(var c in supplierCats) {
                        Console.WriteLine($" - {c.CategoryName} (Parent: {c.ParentCategoryId})");
                    }
                }
            }
        }
    }
}
