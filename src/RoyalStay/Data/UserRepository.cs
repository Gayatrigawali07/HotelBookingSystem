using System.Data;
using Microsoft.Data.SqlClient;
using RoyalStay.Security;

namespace RoyalStay.Data;

public record AdminUser(int UserId, string FullName, string Email, string PasswordHash);

public class UserRepository
{
    private readonly string _cs;

    public UserRepository(IConfiguration config)
    {
        _cs = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection missing in appsettings.json");
    }

    public AdminUser? FindAdminByEmail(string email)
    {
        using var conn = new SqlConnection(_cs);
        using var cmd = new SqlCommand(
            @"SELECT UserId, FullName, Email, PasswordHash
              FROM dbo.Users
              WHERE Email = @Email AND Role = 'Admin'", conn);
        cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 150).Value = email;
        conn.Open();
        using var rd = cmd.ExecuteReader();
        if (!rd.Read()) return null;
        return new AdminUser(rd.GetInt32(0), rd.GetString(1), rd.GetString(2), rd.GetString(3));
    }

    public bool AnyAdminExists()
    {
        using var conn = new SqlConnection(_cs);
        using var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.Users WHERE Role = 'Admin'", conn);
        conn.Open();
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public void CreateAdmin(string fullName, string email, string plainPassword)
    {
        using var conn = new SqlConnection(_cs);
        using var cmd = new SqlCommand(
            @"INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role)
              VALUES (@Name, @Email, @Hash, 'Admin')", conn);
        cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 200).Value = fullName;
        cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 150).Value = email;
        cmd.Parameters.Add("@Hash", SqlDbType.NVarChar, 256).Value = PasswordHasher.Hash(plainPassword);
        conn.Open();
        cmd.ExecuteNonQuery();
    }
}
