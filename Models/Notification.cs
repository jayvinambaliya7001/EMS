namespace EmployeeManagementSystemMvc.Models;

public class Notification
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int? AnnouncementId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string Channel { get; set; } = "IN_APP";
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
