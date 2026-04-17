using System;
using System.Collections.Generic;
using System.Linq;
using SCM_System.Models.Entities;

namespace SCM_System.Data
{
    public static class FaydaSeeder
    {
        public static void Seed(ApplicationDbContext context)
        {
            if (context.FaydaRegistries.Any())
                return; // already seeded

            var random = new Random();
            var list = new List<FaydaRegistry>();

            string[] firstNames = { "Abebe", "Hana", "Dawit", "Selam", "Bekele", "Marta", "Kaleb", "Sara", "Yonas", "Liya" };
            string[] lastNames = { "Kebede", "Tesfaye", "Alemayehu", "Girma", "Tadesse", "Worku", "Bekele", "Mohammed", "Ali", "Daniel" };
            string[] regions = { "Addis Ababa", "Oromia", "Amhara", "Tigray", "SNNPR", "Sidama", "Somali" };

            for (int i = 1; i <= 100; i++)
            {
                var firstName = firstNames[random.Next(firstNames.Length)];
                var lastName = lastNames[random.Next(lastNames.Length)];

                var fan = GenerateFAN(random);
                while (list.Any(x => x.FAN == fan))
                {
                    fan = GenerateFAN(random);
                }

                list.Add(new FaydaRegistry
                {
                    FAN = fan,
                    FullName = $"{firstName} {lastName}",
                    DateOfBirth = RandomDate(random),
                    Gender = random.Next(2) == 0 ? "Male" : "Female",
                    Region = regions[random.Next(regions.Length)],
                    IsActive = true
                });
            }

            context.FaydaRegistries.AddRange(list);
            context.SaveChanges();
        }

        private static string GenerateFAN(Random random)
        {
            return string.Concat(Enumerable.Range(0, 16)
                .Select(_ => random.Next(0, 10).ToString()));
        }

        private static DateTime RandomDate(Random random)
        {
            int year = random.Next(1970, 2005);
            int month = random.Next(1, 13);
            int day = random.Next(1, 28);

            return new DateTime(year, month, day);
        }
    }
}
