using Microsoft.AspNetCore.Mvc;
using EmployeeManagementSystemMvc.Models;
using EmployeeManagementSystemMvc.Repositories;
using EmployeeManagementSystemMvc.Services;

namespace EmployeeManagementSystemMvc.Controllers;

[ApiController]
[Route("api/employee")]
public class EmployeeApiController : ControllerBase
{
    private readonly EmployeeRepository _empRepo;
    private readonly AttendanceRepository _attendanceRepo;
    private readonly LeaveRepository _leaveRepo;
    private readonly AnnouncementRepository _annRepo;
    private readonly AwardRepository _awardRepo;
    private readonly NotificationRepository _notifRepo;
    private readonly FaceAttendanceService _faceService;
    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _env;

    public EmployeeApiController(
        EmployeeRepository empRepo,
        AttendanceRepository attendanceRepo,
        LeaveRepository leaveRepo,
        AnnouncementRepository annRepo,
        AwardRepository awardRepo,
        NotificationRepository notifRepo,
        FaceAttendanceService faceService,
        IAuthService authService,
        IWebHostEnvironment env)
    {
        _empRepo = empRepo;
        _attendanceRepo = attendanceRepo;
        _leaveRepo = leaveRepo;
        _annRepo = annRepo;
        _awardRepo = awardRepo;
        _notifRepo = notifRepo;
        _faceService = faceService;
        _authService = authService;
        _env = env;
    }

    private int? GetCurrentEmployeeId()
    {
        if (HttpContext.Session.GetString("USER_ROLE") != "EMPLOYEE")
            return null;
        return HttpContext.Session.GetInt32("EMPLOYEE_ID");
    }

    // ================= Face Biometric Recognition =================
    [HttpPost("face/enroll")]
    public async Task<IActionResult> EnrollFace([FromBody] FaceEnrollRequest request)
    {
        int? empId = GetCurrentEmployeeId();
        if (!empId.HasValue) return Unauthorized(new { success = false, message = "Employee login required." });

        var (success, message) = await _faceService.EnrollFaceAsync(empId.Value, request?.Descriptor);
        if (success)
            return Ok(new { success = true, message });
        else
            return BadRequest(new { success = false, message });
    }

    [HttpPost("face/punch")]
    public async Task<IActionResult> PunchWithFace([FromBody] FacePunchRequest request)
    {
        int? empId = GetCurrentEmployeeId();
        if (!empId.HasValue) return Unauthorized(new { success = false, message = "Employee login required." });

        var (success, message, action, punchTime, workHours) = 
            await _faceService.PunchWithFaceAsync(empId.Value, request?.Descriptor);

        if (success)
            return Ok(new { success = true, message, action, punchTime, workHours });
        else
            return BadRequest(new { success = false, message });
    }

    // ================= Photo Upload =================
    [HttpPost("profile/photo")]
    public async Task<IActionResult> UploadPhoto(IFormFile photo)
    {
        int? empId = GetCurrentEmployeeId();
        if (!empId.HasValue) return Unauthorized(new { success = false, message = "Employee login required." });

        if (photo == null || photo.Length == 0)
            return BadRequest(new { success = false, message = "No photo file received." });

        if (photo.Length > 2 * 1024 * 1024)
            return BadRequest(new { success = false, message = "Photo must be under 2MB." });

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(photo.ContentType.ToLower()))
            return BadRequest(new { success = false, message = "Only JPG, PNG, or WEBP images are allowed." });

        // Save to wwwroot/uploads/photos/
        string uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "photos");
        Directory.CreateDirectory(uploadsDir);

        string ext = Path.GetExtension(photo.FileName).ToLower();
        if (string.IsNullOrEmpty(ext)) ext = ".jpg";
        string fileName = $"emp_{empId.Value}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{ext}";
        string filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await photo.CopyToAsync(stream);
        }

        string photoUrl = $"/uploads/photos/{fileName}";
        bool updated = await _empRepo.UpdateProfileAsync(empId.Value, null, null, photoUrl);

        if (updated)
            return Ok(new { success = true, message = "Photo uploaded successfully.", photoUrl });
        else
            return StatusCode(500, new { success = false, message = "Failed to update photo in database." });
    }

    // ================= Attendance =================
    [HttpGet("attendance/today")]
    public async Task<IActionResult> GetTodayAttendance()
    {
        int? empId = GetCurrentEmployeeId();
        if (!empId.HasValue) return Unauthorized(new { success = false, message = "Employee login required." });

        var att = await _attendanceRepo.FindTodayAttendanceAsync(empId.Value);
        var emp = await _empRepo.FindByIdAsync(empId.Value);

        return Ok(new
        {
            success = true,
            hasPunchedIn = att != null && att.PunchIn.HasValue,
            hasPunchedOut = att != null && att.PunchOut.HasValue,
            punchIn = att?.FormattedPunchIn,
            punchOut = att?.FormattedPunchOut,
            workHours = att?.FormattedWorkHours,
            faceEnrolled = emp?.FaceEnrolled ?? false
        });
    }

    [HttpPost("attendance/punch")]
    public async Task<IActionResult> ManualPunch([FromBody] ManualPunchRequest request)
    {
        int? empId = GetCurrentEmployeeId();
        if (!empId.HasValue) return Unauthorized(new { success = false, message = "Employee login required." });

        var (success, message, action, punchTime, workHours) = 
            await _faceService.ManualPunchAsync(empId.Value, request?.Action ?? "PUNCH_IN");

        if (success)
            return Ok(new { success = true, message, action, punchTime, workHours });
        else
            return BadRequest(new { success = false, message });
    }

    [HttpGet("attendance/history")]
    public async Task<IActionResult> GetAttendanceHistory([FromQuery] int limit = 30)
    {
        int? empId = GetCurrentEmployeeId();
        if (!empId.HasValue) return Unauthorized(new { success = false, message = "Employee login required." });

        var history = await _attendanceRepo.GetEmployeeHistoryAsync(empId.Value, limit);
        return Ok(new { success = true, history });
    }

    // ================= Leaves =================
    [HttpGet("leaves")]
    public async Task<IActionResult> GetLeaves()
    {
        int? empId = GetCurrentEmployeeId();
        if (!empId.HasValue) return Unauthorized(new { success = false, message = "Employee login required." });

        var leaves = await _leaveRepo.GetByEmployeeIdAsync(empId.Value);
        return Ok(new { success = true, leaves });
    }

    [HttpPost("leaves")]
    public async Task<IActionResult> ApplyLeave([FromBody] LeaveRequest req)
    {
        int? empId = GetCurrentEmployeeId();
        if (!empId.HasValue) return Unauthorized(new { success = false, message = "Employee login required." });

        if (req == null || string.IsNullOrWhiteSpace(req.Reason))
        {
            return BadRequest(new { success = false, message = "Leave reason is required." });
        }
        if (req.ToDate.Date < req.FromDate.Date)
        {
            return BadRequest(new { success = false, message = "To Date cannot be earlier than From Date." });
        }

        req.EmployeeId = empId.Value;
        req.Status = "PENDING";
        int newId = await _leaveRepo.CreateAsync(req);
        req.Id = newId;

        return Ok(new { success = true, message = "Leave application submitted successfully.", leave = req });
    }

    // ================= Profile =================
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        int? empId = GetCurrentEmployeeId();
        if (!empId.HasValue) return Unauthorized(new { success = false, message = "Employee login required." });

        var emp = await _empRepo.FindByIdAsync(empId.Value);
        if (emp == null) return NotFound(new { success = false, message = "Employee not found." });

        return Ok(new { success = true, employee = emp });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] ProfileUpdateViewModel model)
    {
        int? empId = GetCurrentEmployeeId();
        if (!empId.HasValue) return Unauthorized(new { success = false, message = "Employee login required." });

        bool updated = await _empRepo.UpdateProfileAsync(empId.Value, model.Phone, model.Address, model.Photo);
        return Ok(new { success = updated, message = updated ? "Profile updated." : "Failed to update profile." });
    }

    [HttpPut("profile/password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordViewModel model)
    {
        int? empId = GetCurrentEmployeeId();
        if (!empId.HasValue) return Unauthorized(new { success = false, message = "Employee login required." });

        if (model == null || string.IsNullOrWhiteSpace(model.CurrentPassword) || string.IsNullOrWhiteSpace(model.NewPassword))
        {
            return BadRequest(new { success = false, message = "Current and new password are required." });
        }
        if (model.NewPassword.Length < 6)
        {
            return BadRequest(new { success = false, message = "New password must be at least 6 characters." });
        }
        if (model.NewPassword != model.ConfirmPassword)
        {
            return BadRequest(new { success = false, message = "New passwords do not match." });
        }

        var emp = await _empRepo.FindByIdAsync(empId.Value);
        if (emp == null) return NotFound(new { success = false, message = "Employee not found." });

        // Verify current password safely
        bool valid = false;
        try
        {
            valid = BCrypt.Net.BCrypt.Verify(model.CurrentPassword.Trim(), emp.Password.Trim()) || model.CurrentPassword.Trim() == emp.Password.Trim();
        }
        catch
        {
            valid = model.CurrentPassword.Trim() == emp.Password.Trim();
        }

        if (!valid)
            return BadRequest(new { success = false, message = "Current password is incorrect." });

        string hashed = _authService.HashPassword(model.NewPassword.Trim());
        bool updated = await _empRepo.UpdatePasswordAsync(empId.Value, hashed);
        return Ok(new { success = updated, message = updated ? "Password changed successfully." : "Failed to change password." });
    }

    // ================= Notifications =================
    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications()
    {
        int? empId = GetCurrentEmployeeId();
        if (!empId.HasValue) return Unauthorized(new { success = false, message = "Employee login required." });

        var list = await _notifRepo.GetByEmployeeIdAsync(empId.Value);
        return Ok(new { success = true, notifications = list });
    }

    [HttpPut("notifications/{id}/read")]
    public async Task<IActionResult> MarkNotificationRead(int id)
    {
        int? empId = GetCurrentEmployeeId();
        if (!empId.HasValue) return Unauthorized(new { success = false, message = "Employee login required." });

        bool ok = await _notifRepo.MarkAsReadAsync(id, empId.Value);
        return Ok(new { success = ok });
    }

    // ================= Awards & Birthdays =================
    [HttpGet("awards")]
    public async Task<IActionResult> GetAwards()
    {
        int? empId = GetCurrentEmployeeId();
        if (!empId.HasValue) return Unauthorized(new { success = false, message = "Employee login required." });

        var list = await _awardRepo.GetByEmployeeIdAsync(empId.Value);
        return Ok(new { success = true, awards = list });
    }

    [HttpGet("birthdays")]
    public async Task<IActionResult> GetBirthdays()
    {
        var list = await _empRepo.GetUpcomingBirthdaysAsync();
        return Ok(new { success = true, birthdays = list });
    }
}
