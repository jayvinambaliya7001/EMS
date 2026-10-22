namespace EmployeeManagementSystemMvc.Models;

public class LeaveRequest
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string LeaveType { get; set; } = "Casual";
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING"; // PENDING, APPROVED, REJECTED
    public string? AdminNote { get; set; }
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }

    // Join helpers
    public string? EmployeeCode { get; set; }
    public string? FullName { get; set; }
    public string? Department { get; set; }

    public int TotalDays => Math.Max(1, (int)(ToDate.Date - FromDate.Date).TotalDays + 1);
}
