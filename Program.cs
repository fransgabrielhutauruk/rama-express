using Microsoft.EntityFrameworkCore;
using RamaExpress.Data;
// RamaExpress.Data.Service; 

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSession();

//builder.Services.AddDbContext<RamaExpressAppContext>(options =>
    //options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnectionString")));

// Existing services
//builder.Services.AddScoped<IKaryawanService, KaryawanService>();

// builder.Services.AddScoped<IAboutService, AboutService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();


app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ✅ TAMBAH ROUTING UNTUK ABOUT (OPSIONAL)
app.MapControllerRoute(
    name: "about",
    pattern: "tentang",
    defaults: new { controller = "About", action = "Index" });

app.Run(); 