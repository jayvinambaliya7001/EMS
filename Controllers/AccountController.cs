using Microsoft.AspNetCore.Mvc;
using EmployeeManagementSystemMvc.Models;
using EmployeeManagementSystemMvc.Services;

namespace EmployeeManagementSystemMvc.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;

    public AccountController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    public IActionResult AdminLogin(string? returnUrl)
    {
        if (HttpContext.Session.GetString("USER_ROLE") == "ADMIN")
            return RedirectToAction("Dashboard", "Admin");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminLogin(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var (success, message, admin) = await _authService.AuthenticateAdminAsync(model.Username, model.Password);
        if (!success || admin == null)
        {
            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }

        // Set session
        HttpContext.Session.SetString("USER_ROLE", "ADMIN");
        HttpContext.Session.SetInt32("ADMIN_ID", admin.Id);
        HttpContext.Session.SetString("ADMIN_NAME", admin.FullName);
        HttpContext.Session.SetString("ADMIN_USERNAME", admin.Username);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl) && model.ReturnUrl != "/" && !model.ReturnUrl.Contains("/Account/", StringComparison.OrdinalIgnoreCase))
            return Redirect(model.ReturnUrl);

        return RedirectToAction("Dashboard", "Admin");
    }

    [HttpGet]
    public IActionResult EmployeeLogin(string? returnUrl)
    {
        if (HttpContext.Session.GetString("USER_ROLE") == "EMPLOYEE")
            return RedirectToAction("Dashboard", "Employee");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EmployeeLogin(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var (success, message, emp) = await _authService.AuthenticateEmployeeAsync(model.Username, model.Password);
        if (!success || emp == null)
        {
            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }

        // Set session
        HttpContext.Session.SetString("USER_ROLE", "EMPLOYEE");
        HttpContext.Session.SetInt32("EMPLOYEE_ID", emp.Id);
        HttpContext.Session.SetString("EMPLOYEE_CODE", emp.EmployeeCode);
        HttpContext.Session.SetString("EMPLOYEE_NAME", emp.FullName);
        HttpContext.Session.SetString("EMPLOYEE_DEPT", emp.Department ?? "Engineering");

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl) && model.ReturnUrl != "/" && !model.ReturnUrl.Contains("/Account/", StringComparison.OrdinalIgnoreCase))
            return Redirect(model.ReturnUrl);

        return RedirectToAction("Dashboard", "Employee");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}
