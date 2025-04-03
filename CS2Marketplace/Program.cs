using CS2Marketplace.Data;
using CS2Marketplace.Services;
using CS2Marketplace.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Stripe;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Retrieve connection string from configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(); // Enables Azure overrides

// Register the DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register our custom services
builder.Services.AddTransient<SteamAuthService>();
builder.Services.AddScoped<SteamApiService>();
builder.Services.AddTransient<IPaymentService,PaymentService>();
builder.Services.AddTransient<IUserService,UserService>();
builder.Services.AddTransient<IMarketplaceService,MarketplaceService>();
builder.Services.AddTransient<IStripeConnectService, StripeConnectService>();

builder.Services.AddHttpClient();

// Add MemoryCache, MVC controllers, and session support.
builder.Services.AddMemoryCache();
builder.Services.AddControllersWithViews();
builder.Services.AddSession();
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

var cultureInfo = new CultureInfo("en-US");
cultureInfo.NumberFormat.CurrencySymbol = "€";
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.Run();
