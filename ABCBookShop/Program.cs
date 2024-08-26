using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
string? a = builder.Configuration.GetConnectionString("ABCBookDb");
// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContextPool<ABCBook.Model.ABCBookShopContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("ABCBookDb"));
    options.EnableSensitiveDataLogging();
    options.EnableDetailedErrors();
    options.LogTo((sql) => Debug.WriteLine(sql), LogLevel.Debug);
});

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
