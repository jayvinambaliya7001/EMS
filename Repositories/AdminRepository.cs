using Dapper;
using EmployeeManagementSystemMvc.Data;
using EmployeeManagementSystemMvc.Models;

namespace EmployeeManagementSystemMvc.Repositories;

public class AdminRepository
{
    private readonly DbConnectionFactory _db;

    public AdminRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<Admin?> FindByUsernameAsync(string username)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Admin>(
            "SELECT id AS Id, username AS Username, password AS Password, full_name AS FullName, created_at AS CreatedAt FROM admin WHERE LOWER(username) = LOWER(@Username) LIMIT 1;",
            new { Username = username.Trim() });
    }

    public async Task<Admin?> FindByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Admin>(
            "SELECT id AS Id, username AS Username, password AS Password, full_name AS FullName, created_at AS CreatedAt FROM admin WHERE id = @Id LIMIT 1;",
            new { Id = id });
    }
}
