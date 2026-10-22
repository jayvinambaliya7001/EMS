using Microsoft.AspNetCore.Mvc;
using EmployeeManagementSystemMvc.Models;
using EmployeeManagementSystemMvc.Repositories;

namespace EmployeeManagementSystemMvc.Controllers;

public class AdminController : Controller
{
    private readonly EmployeeRepository _empRepo;
    private readonly AttendanceRepository _attendanceRepo;
    private readonly LeaveRepository _leaveRepo;
    private readonly AnnouncementRepository _annRepo;
    private readonly AwardRepository _awardRepo;

    public AdminController(
        EmployeeRepository empRepo,
        AttendanceRepository attendanceRepo,
        LeaveRepository leaveRepo,
        AnnouncementRepository annRepo,
        AwardRepository awardRepo)
    {
        _empRepo = empRepo;
        _attendanceRepo = attendanceRepo;
        _leaveRepo = leaveRepo;
        _annRepo = annRepo;
        _awardRepo = awardRepo;
    }

    private bool IsAdminAuthenticated()
    {
        return HttpContext.Session.GetString("USER_ROLE") == "ADMIN";
    }

    public async Task<IActionResult> Dashboard()
    {
        if (!IsAdminAuthenticated())
            return RedirectToAction("AdminLogin", "Account");

        var stats = new DashboardStats
        {
            TotalEmployees = await _empRepo.GetTotalCountAsync(),
            PresentToday = await _attendanceRepo.GetTodayPresentCountAsync(),
            PendingLeaves = await _leaveRepo.GetPendingCountAsync(),
            TotalAnnouncements = await _annRepo.GetTotalCountAsync(),
            TotalAwards = await _awardRepo.GetTotalCountAsync(),
            DepartmentCounts = await _empRepo.GetDepartmentCountsAsync(),
            RecentAttendance = await _attendanceRepo.GetByDateAsync(DateTime.Today),
            RecentLeaves = await _leaveRepo.GetAllAsync("PENDING")
        };

        stats.AbsentToday = Math.Max(0, stats.TotalEmployees - stats.PresentToday);

        ViewBag.AdminName = HttpContext.Session.GetString("ADMIN_NAME") ?? "Administrator";
        ViewBag.Employees = await _empRepo.GetAllAsync();
        ViewBag.Announcements = await _annRepo.GetAllAsync(10);
        ViewBag.Awards = await _awardRepo.GetAllAsync(10);
        ViewBag.Birthdays = await _empRepo.GetUpcomingBirthdaysAsync();

        return View(stats);
    }
}
