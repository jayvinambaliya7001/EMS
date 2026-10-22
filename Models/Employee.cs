namespace EmployeeManagementSystemMvc.Models;

public class Employee
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Department { get; set; }
    public string? Designation { get; set; }
    public string? Address { get; set; }
    public DateTime? JoinDate { get; set; }
    public string Password { get; set; } = string.Empty;
    public string? FaceDescriptor { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public string Gender { get; set; } = "Not Specified";
    public DateTime? Dob { get; set; }
    public decimal Salary { get; set; } = 0.00m;
    public string? Photo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Helper flag for frontend
    public bool FaceEnrolled => !string.IsNullOrWhiteSpace(FaceDescriptor);
}
