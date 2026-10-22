namespace EmployeeManagementSystemMvc.Models;

public class EmployeeAward
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string AwardType { get; set; } = "Employee of the Month";
    public string AwardTitle { get; set; } = string.Empty;
    public string AwardPeriod { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime AwardDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Join helpers
    public string? EmployeeCode { get; set; }
    public string? FullName { get; set; }
    public string? Department { get; set; }
}
