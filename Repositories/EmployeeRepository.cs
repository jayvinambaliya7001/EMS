using Dapper;
using EmployeeManagementSystemMvc.Data;
using EmployeeManagementSystemMvc.Models;

namespace EmployeeManagementSystemMvc.Repositories;

public class EmployeeRepository
{
    private readonly DbConnectionFactory _db;

    public EmployeeRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    private const string SelectBase = @"
        SELECT 
            id AS Id,
            employee_code AS EmployeeCode,
            username AS Username,
            full_name AS FullName,
            email AS Email,
            phone AS Phone,
            department AS Department,
            designation AS Designation,
            address AS Address,
            join_date AS JoinDate,
            password AS Password,
            face_descriptor AS FaceDescriptor,
            status AS Status,
            gender AS Gender,
            dob AS Dob,
            salary AS Salary,
            photo AS Photo,
            created_at AS CreatedAt
        FROM employees";

    public async Task<List<Employee>> GetAllAsync()
    {
        using var conn = _db.CreateConnection();
        var list = await conn.QueryAsync<Employee>($"{SelectBase} ORDER BY id DESC;");
        return list.AsList();
    }

    public async Task<Employee?> FindByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Employee>(
            $"{SelectBase} WHERE id = @Id LIMIT 1;", new { Id = id });
    }

    public async Task<Employee?> FindByEmployeeCodeOrUsernameAsync(string identifier)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Employee>(
            $"{SelectBase} WHERE LOWER(employee_code) = LOWER(@Iden) OR LOWER(username) = LOWER(@Iden) LIMIT 1;", 
            new { Iden = identifier.Trim() });
    }

    public async Task<int> CreateAsync(Employee emp)
    {
        using var conn = _db.CreateConnection();
        string sql = @"
            INSERT INTO employees 
            (employee_code, username, full_name, email, phone, department, designation, address, join_date, password, face_descriptor, status, gender, dob, salary, photo)
            VALUES 
            (@EmployeeCode, @Username, @FullName, @Email, @Phone, @Department, @Designation, @Address, @JoinDate, @Password, @FaceDescriptor, @Status, @Gender, @Dob, @Salary, @Photo);
            SELECT LAST_INSERT_ID();";

        return await conn.ExecuteScalarAsync<int>(sql, emp);
    }

    public async Task<bool> UpdateAsync(Employee emp)
    {
        using var conn = _db.CreateConnection();
        string sql = @"
            UPDATE employees SET
                employee_code = @EmployeeCode,
                username = @Username,
                full_name = @FullName,
                email = @Email,
                phone = @Phone,
                department = @Department,
                designation = @Designation,
                address = @Address,
                join_date = @JoinDate,
                status = @Status,
                gender = @Gender,
                dob = @Dob,
                salary = @Salary
            WHERE id = @Id;";

        int affected = await conn.ExecuteAsync(sql, emp);
        return affected > 0;
    }

    public async Task<bool> UpdateStatusAsync(int id, string status)
    {
        using var conn = _db.CreateConnection();
        int affected = await conn.ExecuteAsync(
            "UPDATE employees SET status = @Status WHERE id = @Id;",
            new { Id = id, Status = status });
        return affected > 0;
    }

    public async Task<bool> UpdateFaceDescriptorAsync(int id, string descriptorJson)
    {
        using var conn = _db.CreateConnection();
        int affected = await conn.ExecuteAsync(
            "UPDATE employees SET face_descriptor = @Descriptor WHERE id = @Id;",
            new { Id = id, Descriptor = descriptorJson });
        return affected > 0;
    }

    public async Task<bool> UpdateProfileAsync(int id, string? phone, string? address, string? photo)
    {
        using var conn = _db.CreateConnection();
        string sql = @"
            UPDATE employees SET
                phone = COALESCE(@Phone, phone),
                address = COALESCE(@Address, address),
                photo = COALESCE(@Photo, photo)
            WHERE id = @Id;";

        int affected = await conn.ExecuteAsync(sql, new { Id = id, Phone = phone, Address = address, Photo = photo });
        return affected > 0;
    }

    public async Task<bool> UpdatePasswordAsync(int id, string hashedPassword)
    {
        using var conn = _db.CreateConnection();
        int affected = await conn.ExecuteAsync(
            "UPDATE employees SET password = @Pwd WHERE id = @Id;",
            new { Id = id, Pwd = hashedPassword });
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        int affected = await conn.ExecuteAsync("DELETE FROM employees WHERE id = @Id;", new { Id = id });
        return affected > 0;
    }

    public async Task<int> GetTotalCountAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM employees WHERE status = 'ACTIVE';");
    }

    public async Task<List<DepartmentCount>> GetDepartmentCountsAsync()
    {
        using var conn = _db.CreateConnection();
        var results = await conn.QueryAsync<DepartmentCount>(@"
            SELECT COALESCE(department, 'Unassigned') AS Department, COUNT(*) AS Count 
            FROM employees 
            WHERE status = 'ACTIVE'
            GROUP BY department;");
        return results.AsList();
    }

    public async Task<List<Employee>> GetUpcomingBirthdaysAsync()
    {
        using var conn = _db.CreateConnection();
        // Compare month/day with today
        string sql = @"
            SELECT 
                id AS Id, employee_code AS EmployeeCode, full_name AS FullName, 
                department AS Department, dob AS Dob, photo AS Photo
            FROM employees
            WHERE dob IS NOT NULL AND status = 'ACTIVE'
            ORDER BY MONTH(dob), DAY(dob);";

        var list = await conn.QueryAsync<Employee>(sql);
        return list.AsList();
    }
}
