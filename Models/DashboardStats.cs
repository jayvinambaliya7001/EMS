namespace EmployeeManagementSystemMvc.Models;

public class DashboardStats
{
    public int TotalEmployees { get; set; }
    public int PresentToday { get; set; }
    public int AbsentToday { get; set; }
    public int PendingLeaves { get; set; }
    public int TotalAnnouncements { get; set; }
    public int TotalAwards { get; set; }
    public int BirthdaysThisMonth { get; set; }
    public List<DepartmentCount> DepartmentCounts { get; set; } = new();
    public List<Attendance> RecentAttendance { get; set; } = new();
    public List<LeaveRequest> RecentLeaves { get; set; } = new();
}

public class DepartmentCount
{
    public string Department { get; set; } = "General";
    public int Count { get; set; }
}

public class EmployeeDashboardStats
{
    public int TotalDaysWorked { get; set; }
    public int LeavesApproved { get; set; }
    public int LeavesPending { get; set; }
    public int TotalAwards { get; set; }
    public Attendance? TodayAttendance { get; set; }
    public List<Announcement> RecentAnnouncements { get; set; } = new();
    public List<Notification> UnreadNotifications { get; set; } = new();
}
