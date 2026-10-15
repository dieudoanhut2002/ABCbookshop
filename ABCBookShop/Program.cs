using ABCBook.Model;
using ABCBook.Model.Data;
using Hangfire;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình Logging tập trung với Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/abcbookshop-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
    .CreateLogger();

builder.Host.UseSerilog();

// 2. Cấu hình Database EF Core
var connectionString = builder.Configuration.GetConnectionString("ABCBookDb")
    ?? throw new InvalidOperationException("Connection string 'ABCBookDb' not found.");

builder.Services.AddDbContext<ABCBookShopContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
    });

    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors();
    }
});

// 3. Cấu hình Hangfire Background Jobs
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString));

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = Environment.ProcessorCount * 2;
    options.Queues = new[] { "critical", "default", "reports" };
});

// 4. Cấu hình Xác thực Cookie & Phân quyền RBAC
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "ABCBookShop.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(10); // Phù hợp cho ca làm việc thu ngân
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireChainLevel", policy =>
        policy.RequireRole("SysChainOwner", "ChainManager"));

    options.AddPolicy("RequireStoreManager", policy =>
        policy.RequireRole("SysChainOwner", "ChainManager", "StoreManager"));

    options.AddPolicy("RequirePosAccess", policy =>
        policy.RequireRole("SysChainOwner", "ChainManager", "StoreManager", "Cashier", "SalesStaff"));
});

// 5. Cấu hình MVC, Session và Razor Views
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Cấu hình HttpClient cho tích hợp bên ngoài (Telegram, VietQR...)
builder.Services.AddHttpClient();

// Đăng ký Business Services (Catalog & Excel)
builder.Services.AddScoped<ABCBookAPIService.Interfaces.ICategoryService, ABCBookAPIService.Services.CategoryService>();
builder.Services.AddScoped<ABCBookAPIService.Interfaces.IProductService, ABCBookAPIService.Services.ProductService>();
builder.Services.AddScoped<ABCBookAPIService.Interfaces.IProductExcelService, ABCBookAPIService.Services.ProductExcelService>();

// Đăng ký Business Services (Inventory, Transfer & Purchasing)
builder.Services.AddScoped<ABCBookAPIService.Interfaces.IInventoryService, ABCBookAPIService.Services.InventoryService>();
builder.Services.AddScoped<ABCBookAPIService.Interfaces.ITransferService, ABCBookAPIService.Services.TransferService>();
builder.Services.AddScoped<ABCBookAPIService.Interfaces.IPurchasingService, ABCBookAPIService.Services.PurchasingService>();

// Đăng ký Business Services (POS Terminal, Shift Session & Automation)
builder.Services.AddScoped<ABCBookAPIService.Interfaces.IShiftService, ABCBookAPIService.Services.ShiftService>();
builder.Services.AddScoped<ABCBookAPIService.Interfaces.IPosService, ABCBookAPIService.Services.PosService>();
builder.Services.AddScoped<ABCBookAPIService.Interfaces.IShiftReportService, ABCBookAPIService.Services.ShiftReportService>();

// Đăng ký Business Services (Finance, OPEX, Store P&L & Payroll)
builder.Services.AddScoped<ABCBookAPIService.Interfaces.IFinanceService, ABCBookAPIService.Services.FinanceService>();

// Đăng ký Business Services (Storefront, Cart & Executive Dashboard)
builder.Services.AddScoped<ABCBookAPIService.Interfaces.IStorefrontService, ABCBookAPIService.Services.StorefrontService>();

var app = builder.Build();


// 6. Tự động Migrate và Seed Data ban đầu trong môi trường Development
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ABCBookShopContext>();
        if (app.Environment.IsDevelopment())
        {
            Log.Information("Bắt đầu khởi tạo và kiểm tra database schema...");
            await DbInitializer.SeedAsync(context);
            Log.Information("Khởi tạo database và seed dữ liệu mẫu thành công!");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Đã xảy ra lỗi trong quá trình khởi tạo dữ liệu ban đầu.");
    }
}

// 7. Cấu hình HTTP Request Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// 8. Hangfire Dashboard & Recurring Automation Jobs
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    // Có thể cấu hình custom authorization filter sau khi phân quyền hoàn tất
    DashboardTitle = "ABCBookShop - Tác vụ nền & Tự động hóa"
});

// Đăng ký Recurring Jobs giám sát ca làm việc định kỳ
RecurringJob.AddOrUpdate<ABCBookAPIService.Interfaces.IShiftReportService>(
    "pos-check-unclosed-shifts",
    service => service.CheckAndAlertUnclosedShiftsAsync(),
    "*/30 * * * *"); // Chạy mỗi 30 phút kiểm tra ca quá 14 tiếng


// 9. Định tuyến Areas và Default MVC
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

Log.Information("Hệ thống ABCBookShop khởi động thành công.");
app.Run();
