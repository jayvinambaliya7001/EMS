using Microsoft.AspNetCore.Mvc;
using EmployeeManagementSystemMvc.Models;
using EmployeeManagementSystemMvc.Repositories;
using EmployeeManagementSystemMvc.Services;

namespace EmployeeManagementSystemMvc.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminApiController : ControllerBase
{
    private readonly EmployeeRepository _empRepo;
    private readonly AttendanceRepository _attendanceRepo;
    private readonly LeaveRepository _leaveRepo;
    private readonly AnnouncementRepository _annRepo;
    private readonly AwardRepository _awardRepo;
    private readonly IAuthService _authService;

    public AdminApiController(
        EmployeeRepository empRepo,
        AttendanceRepository attendanceRepo,
        LeaveRepository leaveRepo,
        AnnouncementRepository annRepo,
        AwardRepository awardRepo,
        IAuthService authService)
    {
        _empRepo = empRepo;
        _attendanceRepo = attendanceRepo;
        _leaveRepo = leaveRepo;
        _annRepo = annRepo;
        _awardRepo = awardRepo;
        _authService = authService;
    }

    private bool CheckAdminAuth()
    {
        return HttpContext.Session.GetString("USER_ROLE") == "ADMIN";
    }

    // ================= Dashboard =================
    [HttpGet("dashboard/stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        if (!CheckAdminAuth()) return Unauthorized(new { success = false, message = "Admin authorization required." });

        int totalEmp = await _empRepo.GetTotalCountAsync();
        int present = await _attendanceRepo.GetTodayPresentCountAsync();
        int pendingLeaves = await _leaveRepo.GetPendingCountAsync();
        int totalAnn = await _annRepo.GetTotalCountAsync();
        int totalAwards = await _awardRepo.GetTotalCountAsync();

        var stats = new
        {
            totalEmployees = totalEmp,
            presentToday = present,
            absentToday = Math.Max(0, totalEmp - present),
            pendingLeaves = pendingLeaves,
            totalAnnouncements = totalAnn,
            totalAwards = totalAwards,
            departments = await _empRepo.GetDepartmentCountsAsync()
        };

        return Ok(new { success = true, stats });
    }

    // ================= Employees =================
    [HttpGet("employees")]
    public async Task<IActionResult> GetAllEmployees()
    {
        if (!CheckAdminAuth()) return Unauthorized(new { success = false, message = "Admin authorization required." });
        var employees = await _empRepo.GetAllAsync();
        return Ok(new { success = true, employees });
    }

    [HttpGet("employees/{id}")]
    public async Task<IActionResult> GetEmployeeById(int id)
    {
        if (!CheckAdminAuth()) return Unauthorized(new { success = false, message = "Admin authorization required." });
        var emp = await _empRepo.FindByIdAsync(id);
        if (emp == null) return NotFound(new { success = false, message = "Employee not found." });
        return Ok(new { success = true, employee = emp });
    }

    [HttpPost("employees")]
    public async Task<IActionResult> CreateEmployee([FromBody] Employee emp)
    {
        if (!CheckAdminAuth()) return Unauthorized(new { success = false, message = "Admin authorization required." });
        if (emp == null || string.IsNullOrWhiteSpace(emp.EmployeeCode) || string.IsNullOrWhiteSpace(emp.FullName) || string.IsNullOrWhiteSpace(emp.Email))
        {
            return BadRequest(new { success = false, message = "Employee Code, Full Name, and Email are required." });
        }

        try
        {
            if (string.IsNullOrWhiteSpace(emp.Password)) emp.Password = "employee123";
            emp.Password = _authService.HashPassword(emp.Password);
            if (string.IsNullOrWhiteSpace(emp.Status)) emp.Status = "ACTIVE";
            if (emp.JoinDate == null) emp.JoinDate = DateTime.Today;

            int newId = await _empRepo.CreateAsync(emp);
            emp.Id = newId;
            return Ok(new { success = true, message = "Employee created successfully.", employee = emp });
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("Duplicate", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { success = false, message = "An employee with this Employee Code or Email already exists." });
            }
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPut("employees/{id}")]
    public async Task<IActionResult> UpdateEmployee(int id, [FromBody] Employee emp)
    {
        if (!CheckAdminAuth()) return Unauthorized(new { success = false, message = "Admin authorization required." });
        emp.Id = id;
        bool updated = await _empRepo.UpdateAsync(emp);
        if (!updated) return NotFound(new { success = false, message = "Employee not found or no changes made." });
        return Ok(new { success = true, message = "Employee updated successfully.", employee = emp });
    }

    public class StatusRequest { public string Status { get; set; } = "ACTIVE"; }

    [HttpPatch("employees/{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] StatusRequest req)
    {
        if (!CheckAdminAuth()) return Unauthorized(new { success = false, message = "Admin authorization required." });
        bool ok = await _empRepo.UpdateStatusAsync(id, req.Status);
        return Ok(new { success = ok, message = ok ? "Status updated." : "Failed to update status." });
    }

    [HttpDelete("employees/{id}")]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        if (!CheckAdminAuth()) return Unauthorized(new { success = false, message = "Admin authorization required." });
        bool deleted = await _empRepo.DeleteAsync(id);
        return Ok(new { success = deleted, message = deleted ? "Employee deleted." : "Employee not found." });
    }

    // ================= Attendance =================
    [HttpGet("attendance")]
    public async Task<IActionResult> GetAttendance([FromQuery] string? date, [FromQuery] string? department)
    {
        if (!CheckAdminAuth()) return Unauthorized(new { success = false, message = "Admin authorization required." });
        DateTime queryDate = DateTime.Today;
        if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var parsedDate))
        {
            queryDate = parsedDate;
        }

        var list = await _attendanceRepo.GetByDateAsync(queryDate, department);
        return Ok(new { success = true, attendance = list });
    }

    public class ManualAttendanceDto
    {
        public int EmployeeId { get; set; }
        public DateTime Date { get; set; }
        public string PunchIn { get; set; } = "09:00:00";
        public string? PunchOut { get; set; }
        public string Status { get; set; } = "PRESENT";
    }

    [HttpPost("attendance/manual")]
    public async Task<IActionResult> MarkManualAttendance([FromBody] ManualAttendanceDto dto)
    {
        if (!CheckAdminAuth()) return Unauthorized(new { success = false, message = "Admin authorization required." });
        if (dto == null || dto.EmployeeId <= 0)
        {
            return BadRequest(new { success = false, message = "Please select a valid employee." });
        }

        if (!TimeSpan.TryParse(dto.PunchIn, out var inTime))
        {
            inTime = new TimeSpan(9, 0, 0);
        }

        TimeSpan? outTime = null;
        if (!string.IsNullOrEmpty(dto.PunchOut) && TimeSpan.TryParse(dto.PunchOut, out var parsedOut))
        {
            outTime = parsedOut;
        }

        bool saved = await _attendanceRepo.SaveManualAttendanceAsync(dto.EmployeeId, dto.Date, inTime, outTime, dto.Status);
        return Ok(new { success = saved, message = saved ? "Attendance marked." : "Failed to save attendance." });
    }

    // ================= Leaves =================
    [HttpGet("leaves")]
    public async Task<IActionResult> GetLeaves([FromQuery] string? status)
    {
        if (!CheckAdminAuth()) return Unauthorized(new { success = false, message = "Admin authorization required." });
        var leaves = await _leaveRepo.GetAllAsync(status);
        return Ok(new { success = true, leaves });
    }

    public class ReviewLeaveDto { public string? Note { get; set; } }

    [HttpPut("leaves/{id}/approve")]
    public async Task<IActionResult> ApproveLeave(int id, [FromBody] ReviewLeaveDto? dto)
    {
        if (!CheckAdminAuth()) return Unauthorized(new { success = false, message = "Admin authorization required." });
        bool ok = await _leaveRepo.UpdateStatusAsync(id, "APPROVED", dto?.Note ?? "Approved by admin.");
        return Ok(new { success = ok, message = ok ? "Leave approved." : "Leave request not found." });
    }

    [HttpPut("leaves/{id}/reject")]
    public async Task<IActionResult> RejectLeave(int id, [FromBody] ReviewLeaveDto? dto)
    {
        if (!CheckAdminAuth()) return Unauthorized(new { success = false, message = "Admin authorization required." });
        bool ok = await _leaveRepo.UpdateStatusAsync(id, "REJECTED", dto?.Note ?? "Rejected by admin.");
        return Ok(new { success = ok, message = ok ? "Leave rejected." : "Leave request not found." });
    }

    // ================= Announcements =================
    [HttpGet("announcements")]
    public async Task<IActionResult> GetAnnouncements()
    {
        if (!CheckAdminAuth()) return Unauthorized(new { success = false, message = "Admin authorization required." });
        var list = await _annRepo.GetAllAsync();
        return Ok(new { success = true, announcements = list });
    }

    [HttpPost("announcements")]
    public async Task<IActionResult> CreateAnnouncement([FromBody] Announcement ann)
    {
        if (!CheckAdminAuth()) return Unauthorized(new { success = false, message = "Admin authorization required." });
        ann.CreatedBy = HttpContext.Session.GetString("ADMIN_NAME") ?? "System Admin";
        int id = await _annRepo.CreateAsync(ann);
        ann.Id = id;
        return Ok(new { success = true, message = "Announcement published.", announcement = ann });
    }

    [HttpDelete("announcements/{id}")]
    public async Task<IActionResult> DeleteAnnouncement(int id)
    {
        if (!CheckAdminAuth()) return Unauthorized(new { success = false, message = "Admin authorization required." });
        bool ok = await _annRepo.DeleteAsync(id);
        return Ok(new { success = ok, message = ok ? "Announcement deleted." : "Failed to delete announcement." });
    }

    // ================= Awards =================
    [HttpGet("awards")]
    public async Task<IActionResult> GetAwards()
    {
        if (!CheckAdminAuth()) return Unauthorized(new { success = false, message = "Admin authorization required." });
        var list = await _awardRepo.GetAllAsync();
        return Ok(new { success = true, awards = list });
    }

    [HttpPost("awards")]
    public async Task<IActionResult> CreateAward([FromBody] EmployeeAward award)
    {
        if (!CheckAdminAuth()) return Unauthorized(new { success = false, message = "Admin authorization required." });
        if (award.AwardDate == default) award.AwardDate = DateTime.Today;
        int id = await _awardRepo.CreateAsync(award);
        award.Id = id;
        return Ok(new { success = true, message = "Award issued.", award });
    }

    [HttpDelete("awards/{id}")]
    public async Task<IActionResult> DeleteAward(int id)
    {
        if (!CheckAdminAuth()) return Unauthorized(new { success = false, message = "Admin authorization required." });
        bool ok = await _awardRepo.DeleteAsync(id);
        return Ok(new { success = ok, message = ok ? "Award deleted." : "Failed to delete award." });
    }

    // ================= Birthdays =================
    [HttpGet("birthdays")]
    public async Task<IActionResult> GetBirthdays()
    {
        if (!CheckAdminAuth()) return Unauthorized(new { success = false, message = "Admin authorization required." });
        var list = await _empRepo.GetUpcomingBirthdaysAsync();
        return Ok(new { success = true, birthdays = list });
    }
}
