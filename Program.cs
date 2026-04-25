using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Services;


var builder = WebApplication.CreateBuilder(args);

// Add logging configuration
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<SCM_System.Services.IProductService, SCM_System.Services.ProductService>();

// Add Inventory Service
builder.Services.AddScoped<IInventoryService, InventoryService>();

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

// Add Cookie Authentication
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;
    });
// Add session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpClient<IChapaService, ChapaService>();

// Add HttpContextAccessor for session helpers
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<SCM_System.Services.INotificationService, SCM_System.Services.NotificationService>();
builder.Services.AddScoped<SCM_System.Services.ITenderService, SCM_System.Services.TenderService>();
builder.Services.AddScoped<SCM_System.Services.IBidService, SCM_System.Services.BidService>();
builder.Services.AddScoped<SCM_System.Services.IPurchaseOrderService, SCM_System.Services.PurchaseOrderService>();
builder.Services.AddScoped<SCM_System.Services.IOrderService, SCM_System.Services.OrderService>();
builder.Services.AddScoped<SCM_System.Services.ICartService, SCM_System.Services.CartService>();
builder.Services.AddScoped<SCM_System.Services.ISupplierService, SCM_System.Services.SupplierService>();
builder.Services.AddScoped<SCM_System.Services.IFaydaService, SCM_System.Services.FaydaService>();
builder.Services.AddScoped<SCM_System.Services.IAuditLogService, SCM_System.Services.AuditLogService>();

// Add Email Service
builder.Services.Configure<SCM_System.Models.EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<SCM_System.Services.IEmailService, SCM_System.Services.EmailService>();
builder.Services.AddHostedService<SCM_System.Services.EmailLogCleanupService>();
builder.Services.AddHostedService<SCM_System.Services.ReservationExpiryBackgroundService>();
builder.Services.AddHostedService<SCM_System.Services.PaymentExpiryBackgroundService>();
builder.Services.AddHostedService<SCM_System.Services.InventorySnapshotBackgroundService>();
builder.Services.AddHostedService<SCM_System.Services.ReorderSuggestionBackgroundService>();
builder.Services.AddHostedService<SCM_System.Services.DeliverySLABackgroundService>();

builder.Services.AddScoped<ICommissionService, CommissionService>();
builder.Services.AddScoped<IChapaService, ChapaService>();
builder.Services.AddScoped<IReturnService, ReturnService>();
// Add this after builder.Services.AddScoped statements
builder.Services.AddScoped<IContactDetectionService, ContactDetectionService>();
builder.Services.AddScoped<IPenaltyService, PenaltyService>();
builder.Services.AddScoped<IRatingService, RatingService>();
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

        // Apply all entity framework migrations
        logger.LogInformation("Applying migrations...");
        context.Database.Migrate();
        logger.LogInformation("Database migration completed.");

        // Seed the database
        logger.LogInformation("Starting database seeding...");
        SeedData.Initialize(services);
        FaydaSeeder.Seed(context);
        logger.LogInformation("Database seeded successfully with admin user and Fayda mock registry.");
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

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
