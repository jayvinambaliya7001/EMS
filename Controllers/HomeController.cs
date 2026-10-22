using Microsoft.AspNetCore.Mvc;
using EmployeeManagementSystemMvc.Data;
using MySqlConnector;

namespace EmployeeManagementSystemMvc.Controllers;

public class HomeController : Controller
{
    private readonly DbConnectionFactory _db;

    public HomeController(DbConnectionFactory db)
    {
        _db = db;
    }

    public IActionResult Index()
    {
        // Check if user is already logged in
        var role = HttpContext.Session.GetString("USER_ROLE");
        if (role == "ADMIN")
        {
            return RedirectToAction("Dashboard", "Admin");
        }
        if (role == "EMPLOYEE")
        {
            return RedirectToAction("Dashboard", "Employee");
        }

        // Test database connectivity
        bool dbConnected = false;
        string dbMessage = "Checking XAMPP MySQL connection...";
        try
        {
            using var conn = _db.CreateConnection();
            conn.Open();
            dbConnected = true;
            dbMessage = "Connected to MySQL on localhost:3306 (employee_management)";
        }
        catch (Exception ex)
        {
            dbConnected = false;
            dbMessage = $"Database offline: {ex.Message}. Please ensure MySQL is started in XAMPP.";
        }

        ViewBag.DbConnected = dbConnected;
        ViewBag.DbMessage = dbMessage;

        return View();
    }

    [HttpPost]
    public IActionResult ReinitializeDatabase([FromServices] IConfiguration config, [FromServices] ILogger<HomeController> logger)
    {
        string sqlPath = Path.Combine(Directory.GetCurrentDirectory(), "database.sql");
        DbInitializer.Initialize(config, logger, sqlPath);
        TempData["SuccessMessage"] = "Database schema checked / initialized successfully.";
        return RedirectToAction(nameof(Index));
    }
}
