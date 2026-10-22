namespace EmployeeManagementSystemMvc.Models;

public class Attendance
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public DateTime WorkDate { get; set; }
    public TimeSpan? PunchIn { get; set; }
    public TimeSpan? PunchOut { get; set; }
    public int WorkMinutes { get; set; }
    public string PunchMethod { get; set; } = "FACE";
    public string? PunchInPhoto { get; set; }
    public string? PunchOutPhoto { get; set; }
    public string Status { get; set; } = "PRESENT";

    // Navigation / Join helpers
    public string? EmployeeCode { get; set; }
    public string? FullName { get; set; }
    public string? Department { get; set; }

    public string FormattedPunchIn => PunchIn.HasValue ? PunchIn.Value.ToString(@"hh\:mm\:ss") : "-";
    public string FormattedPunchOut => PunchOut.HasValue ? PunchOut.Value.ToString(@"hh\:mm\:ss") : "-";
    public string FormattedWorkHours
    {
        get
        {
            if (WorkMinutes <= 0) return "-";
            int h = WorkMinutes / 60;
            int m = WorkMinutes % 60;
            return $"{h}h {m}m";
        }
    }
}
