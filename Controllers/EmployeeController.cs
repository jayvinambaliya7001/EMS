using Microsoft.AspNetCore.Mvc;
using EmployeeManagementSystemMvc.Models;
using EmployeeManagementSystemMvc.Repositories;

namespace EmployeeManagementSystemMvc.Controllers;

public class EmployeeController : Controller
{
    private readonly EmployeeRepository _empRepo;
    private readonly AttendanceRepository _attendanceRepo;
    private readonly LeaveRepository _leaveRepo;
    private readonly AnnouncementRepository _annRepo;
    private readonly AwardRepository _awardRepo;
    private readonly NotificationRepository _notifRepo;

    public EmployeeController(
        EmployeeRepository empRepo,
        AttendanceRepository attendanceRepo,
        LeaveRepository leaveRepo,
        AnnouncementRepository annRepo,
        AwardRepository awardRepo,
        NotificationRepository notifRepo)
    {
        _empRepo = empRepo;
        _attendanceRepo = attendanceRepo;
        _leaveRepo = leaveRepo;
        _annRepo = annRepo;
        _awardRepo = awardRepo;
        _notifRepo = notifRepo;
    }

    private int? GetCurrentEmployeeId()
    {
        if (HttpContext.Session.GetString("USER_ROLE") != "EMPLOYEE")
            return null;
        return HttpContext.Session.GetInt32("EMPLOYEE_ID");
    }

    public async Task<IActionResult> Dashboard()
    {
        int? empId = GetCurrentEmployeeId();
        if (!empId.HasValue)
            return RedirectToAction("EmployeeLogin", "Account");

        var emp = await _empRepo.FindByIdAsync(empId.Value);
        if (emp == null)
            return RedirectToAction("EmployeeLogin", "Account");

        var (approved, pending) = await _leaveRepo.GetEmployeeLeaveCountsAsync(empId.Value);

        var stats = new EmployeeDashboardStats
        {
            TotalDaysWorked = await _attendanceRepo.GetEmployeeDaysWorkedAsync(empId.Value),
            LeavesApproved = approved,
            LeavesPending = pending,
            TotalAwards = await _awardRepo.GetEmployeeAwardCountAsync(empId.Value),
            TodayAttendance = await _attendanceRepo.FindTodayAttendanceAsync(empId.Value),
            RecentAnnouncements = await _annRepo.GetAllAsync(5),
            UnreadNotifications = await _notifRepo.GetByEmployeeIdAsync(empId.Value, 10)
        };

        ViewBag.Employee = emp;
        ViewBag.AttendanceHistory = await _attendanceRepo.GetEmployeeHistoryAsync(empId.Value, 30);
        ViewBag.LeaveHistory = await _leaveRepo.GetByEmployeeIdAsync(empId.Value);
        ViewBag.Awards = await _awardRepo.GetByEmployeeIdAsync(empId.Value);
        ViewBag.CompanyBirthdays = await _empRepo.GetUpcomingBirthdaysAsync();

        return View(stats);
    }
}
