using EmployeeManagementSystemMvc.Data;
using EmployeeManagementSystemMvc.Repositories;
using EmployeeManagementSystemMvc.Services;
using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);

// Add Controllers with Views
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Configure Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".EMS.Session";
});

// Register Database & Repositories
builder.Services.AddSingleton<DbConnectionFactory>();
builder.Services.AddScoped<AdminRepository>();
builder.Services.AddScoped<EmployeeRepository>();
builder.Services.AddScoped<AttendanceRepository>();
builder.Services.AddScoped<LeaveRepository>();
builder.Services.AddScoped<AnnouncementRepository>();
builder.Services.AddScoped<AwardRepository>();
builder.Services.AddScoped<NotificationRepository>();

// Register Application Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<FaceAttendanceService>();

var app = builder.Build();

// Automatically check and initialize XAMPP MySQL database schema
using (var scope = app.Services.CreateScope())
{
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    string sqlPath = Path.Combine(app.Environment.ContentRootPath, "database.sql");
    DbInitializer.Initialize(config, logger, sqlPath);
}

// Configure HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Configure Static File Provider to serve face-api weights and shard files
var contentTypeProvider = new FileExtensionContentTypeProvider();
contentTypeProvider.Mappings[".json"] = "application/json";
contentTypeProvider.Mappings[""] = "application/octet-stream"; // For files without extensions like shard files

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = contentTypeProvider,
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream"
});

app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
