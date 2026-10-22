using System.Text.Json;
using EmployeeManagementSystemMvc.Models;
using EmployeeManagementSystemMvc.Repositories;

namespace EmployeeManagementSystemMvc.Services;

public class FaceAttendanceService
{
    private readonly EmployeeRepository _empRepo;
    private readonly AttendanceRepository _attendanceRepo;
    private const double MatchThreshold = 0.60;

    public FaceAttendanceService(EmployeeRepository empRepo, AttendanceRepository attendanceRepo)
    {
        _empRepo = empRepo;
        _attendanceRepo = attendanceRepo;
    }

    public async Task<(bool success, string message)> EnrollFaceAsync(int employeeId, List<double>? descriptor)
    {
        if (descriptor == null || descriptor.Count < 64)
        {
            return (false, "Please keep your face inside the camera frame.");
        }

        try
        {
            string json = JsonSerializer.Serialize(descriptor);
            bool updated = await _empRepo.UpdateFaceDescriptorAsync(employeeId, json);
            if (updated)
            {
                return (true, "Face enrolled successfully.");
            }
            return (false, "Employee record not found for enrollment.");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to store face descriptor: {ex.Message}");
        }
    }

    public async Task<(bool success, string message, string? action, string? punchTime, string? workHours)> 
        PunchWithFaceAsync(int employeeId, List<double>? currentDescriptor)
    {
        if (currentDescriptor == null || currentDescriptor.Count < 64)
        {
            return (false, "Please keep your face inside the camera frame.", null, null, null);
        }

        var emp = await _empRepo.FindByIdAsync(employeeId);
        if (emp == null)
        {
            return (false, "Employee not found.", null, null, null);
        }

        if (string.IsNullOrWhiteSpace(emp.FaceDescriptor))
        {
            return (false, "Please enroll your face first before punching attendance.", null, null, null);
        }

        List<double>? enrolledDescriptor;
        try
        {
            enrolledDescriptor = JsonSerializer.Deserialize<List<double>>(emp.FaceDescriptor);
        }
        catch
        {
            return (false, "Error reading stored face descriptor. Please re-enroll your face.", null, null, null);
        }

        if (enrolledDescriptor == null || enrolledDescriptor.Count != currentDescriptor.Count)
        {
            return (false, "Face descriptor dimension mismatch. Please re-enroll.", null, null, null);
        }

        double distance = CalculateEuclideanDistance(currentDescriptor, enrolledDescriptor);
        if (distance > MatchThreshold)
        {
            return (false, $"Face does not match the enrolled employee record (distance: {Math.Round(distance, 2)}).", null, null, null);
        }

        return await ProcessPunchAsync(employeeId, "FACE");
    }

    public async Task<(bool success, string message, string? action, string? punchTime, string? workHours)> 
        ManualPunchAsync(int employeeId, string action)
    {
        return await ProcessPunchAsync(employeeId, "MANUAL");
    }

    private async Task<(bool success, string message, string? action, string? punchTime, string? workHours)> 
        ProcessPunchAsync(int employeeId, string method)
    {
        var now = DateTime.Now;
        var currentTime = now.TimeOfDay;
        var todayAtt = await _attendanceRepo.FindTodayAttendanceAsync(employeeId);

        if (todayAtt == null)
        {
            // Case 1: Punch In
            await _attendanceRepo.InsertPunchInAsync(employeeId, currentTime, method);
            return (true, "Punch In successful.", "PUNCH_IN", currentTime.ToString(@"hh\:mm\:ss"), null);
        }

        if (todayAtt.PunchOut == null)
        {
            // Case 2: Punch Out
            TimeSpan inTime = todayAtt.PunchIn ?? currentTime;
            int workMinutes = Math.Max(0, (int)(currentTime - inTime).TotalMinutes);

            await _attendanceRepo.UpdatePunchOutAsync(todayAtt.Id, currentTime, workMinutes);
            int h = workMinutes / 60;
            int m = workMinutes % 60;
            string hoursStr = $"{h}h {m}m";

            return (true, "Punch Out successful.", "PUNCH_OUT", currentTime.ToString(@"hh\:mm\:ss"), hoursStr);
        }

        // Case 3: Already punched out today
        return (false, "Today's Punch In and Punch Out are already completed.", null, null, null);
    }

    private static double CalculateEuclideanDistance(List<double> a, List<double> b)
    {
        double sum = 0.0;
        int size = Math.Min(a.Count, b.Count);
        for (int i = 0; i < size; i++)
        {
            double diff = a[i] - b[i];
            sum += diff * diff;
        }
        return Math.Sqrt(sum);
    }
}
