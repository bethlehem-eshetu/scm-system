using Microsoft.EntityFrameworkCore;
using SCM_System.Data;

var builder = WebApplication.CreateBuilder(args);

// Add logging configuration
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<SCM_System.Services.IProductService, SCM_System.Services.ProductService>();

// Add DbContext with detailed error logging
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    // Enable sensitive data logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Add session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add HttpContextAccessor for session helpers
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    // In development, show detailed errors
    app.UseDeveloperExceptionPage();
}

// ** IMPORTANT: Ensure database is created and seeded with admin user **
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        // Ensure database is created (synchronous version to avoid async issues)
        logger.LogInformation("Ensuring database is created...");
        context.Database.EnsureCreated();
        logger.LogInformation("Database check completed.");

        // Seed the database
        logger.LogInformation("Starting database seeding...");
        SeedData.Initialize(services);
        logger.LogInformation("Database seeded successfully with admin user.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while setting up the database.");
        Console.WriteLine($"❌ DATABASE ERROR: {ex.Message}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"   → Inner: {ex.InnerException.Message}");
        }
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();