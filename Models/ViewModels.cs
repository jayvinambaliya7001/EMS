using System.ComponentModel.DataAnnotations;

namespace EmployeeManagementSystemMvc.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Username or Employee Code is required")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class FaceEnrollRequest
{
    public List<double>? Descriptor { get; set; }
}

public class FacePunchRequest
{
    public List<double>? Descriptor { get; set; }
}

public class ManualPunchRequest
{
    public string Action { get; set; } = "PUNCH_IN"; // PUNCH_IN or PUNCH_OUT
}

public class LeaveApplicationViewModel
{
    [Required]
    public string LeaveType { get; set; } = "Casual";

    [Required]
    public DateTime FromDate { get; set; } = DateTime.Today;

    [Required]
    public DateTime ToDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Reason is required")]
    public string Reason { get; set; } = string.Empty;
}

public class ChangePasswordViewModel
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ProfileUpdateViewModel
{
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Photo { get; set; }
}
