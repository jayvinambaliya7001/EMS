using Dapper;
using EmployeeManagementSystemMvc.Data;
using EmployeeManagementSystemMvc.Models;

namespace EmployeeManagementSystemMvc.Repositories;

public class AnnouncementRepository
{
    private readonly DbConnectionFactory _db;

    public AnnouncementRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<List<Announcement>> GetAllAsync(int limit = 20)
    {
        using var conn = _db.CreateConnection();
        string sql = @"
            SELECT 
                id AS Id, title AS Title, message AS Message, category AS Category,
                created_by AS CreatedBy, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM announcements
            ORDER BY created_at DESC
            LIMIT @Limit;";

        var list = await conn.QueryAsync<Announcement>(sql, new { Limit = limit });
        return list.AsList();
    }

    public async Task<int> CreateAsync(Announcement ann)
    {
        using var conn = _db.CreateConnection();
        string sql = @"
            INSERT INTO announcements 
            (title, message, category, created_by, created_at)
            VALUES 
            (@Title, @Message, @Category, @CreatedBy, CURRENT_TIMESTAMP);
            SELECT LAST_INSERT_ID();";

        return await conn.ExecuteScalarAsync<int>(sql, ann);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        int affected = await conn.ExecuteAsync("DELETE FROM announcements WHERE id = @Id;", new { Id = id });
        return affected > 0;
    }

    public async Task<int> GetTotalCountAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM announcements;");
    }
}
