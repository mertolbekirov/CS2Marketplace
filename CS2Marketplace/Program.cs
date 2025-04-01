using CS2Marketplace.Data;
using CS2Marketplace.Services;
using Microsoft.EntityFrameworkCore;
using Stripe;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Retrieve connection string from configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Register the DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register our custom services
builder.Services.AddTransient<SteamAuthService>();
builder.Services.AddTransient<SteamApiService>();
builder.Services.AddTransient<PaymentService>();
builder.Services.AddTransient<SteamTradeService>();

// Register the GC-based float fetcher (consider it as a singleton if you want to keep its connection alive)
builder.Services.AddSingleton<SteamFloatFetcher>(sp =>
{
    // Supply valid Steam credentials (or read from configuration)
    string username = builder.Configuration["Steam:Username"];
    string password = builder.Configuration["Steam:Password"];
    var fetcher = new SteamFloatFetcher(username, password);
    // Optionally, you might start the connection here asynchronously (or call it later when needed)
    Task.Run(async () => await fetcher.ConnectAndLogOnAsync()).Wait();
    return fetcher;
});
builder.Services.AddHttpClient();

// Add MemoryCache, MVC controllers, and session support.
builder.Services.AddMemoryCache();
builder.Services.AddControllersWithViews();
builder.Services.AddSession();
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

// Add background service for checking trade status
builder.Services.AddHostedService<TradeStatusCheckService>();

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

app.Run();
