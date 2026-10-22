using Dapper;
using EmployeeManagementSystemMvc.Data;
using EmployeeManagementSystemMvc.Models;

namespace EmployeeManagementSystemMvc.Repositories;

public class NotificationRepository
{
    private readonly DbConnectionFactory _db;

    public NotificationRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<List<Notification>> GetByEmployeeIdAsync(int employeeId, int limit = 30)
    {
        using var conn = _db.CreateConnection();
        string sql = @"
            SELECT 
                id AS Id, employee_id AS EmployeeId, announcement_id AS AnnouncementId,
                title AS Title, message AS Message, category AS Category,
                channel AS Channel, is_read AS IsRead, created_at AS CreatedAt
            FROM notifications
            WHERE employee_id = @EmpId
            ORDER BY id DESC
            LIMIT @Limit;";

        var list = await conn.QueryAsync<Notification>(sql, new { EmpId = employeeId, Limit = limit });
        return list.AsList();
    }

    public async Task<int> CreateAsync(Notification n)
    {
        using var conn = _db.CreateConnection();
        string sql = @"
            INSERT INTO notifications 
            (employee_id, announcement_id, title, message, category, channel, is_read, created_at)
            VALUES 
            (@EmployeeId, @AnnouncementId, @Title, @Message, @Category, @Channel, @IsRead, CURRENT_TIMESTAMP);
            SELECT LAST_INSERT_ID();";

        return await conn.ExecuteScalarAsync<int>(sql, n);
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, int employeeId)
    {
        using var conn = _db.CreateConnection();
        int affected = await conn.ExecuteAsync(
            "UPDATE notifications SET is_read = TRUE WHERE id = @Id AND employee_id = @EmpId;",
            new { Id = notificationId, EmpId = employeeId });
        return affected > 0;
    }

    public async Task<bool> MarkAllAsReadAsync(int employeeId)
    {
        using var conn = _db.CreateConnection();
        int affected = await conn.ExecuteAsync(
            "UPDATE notifications SET is_read = TRUE WHERE employee_id = @EmpId;",
            new { EmpId = employeeId });
        return affected > 0;
    }
}
