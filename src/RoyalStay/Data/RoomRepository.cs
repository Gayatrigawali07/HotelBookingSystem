using System.Data;
using Microsoft.Data.SqlClient;
using RoyalStay.Models;

namespace RoyalStay.Data;

public class RoomRepository
{
    private readonly string _cs;

    public RoomRepository(IConfiguration config)
    {
        _cs = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection missing in appsettings.json");
    }

    private const string SelectColumns =
        "RoomId, RoomNumber, RoomType, Price, Capacity, Description, ImageUrl, IsAvailable";

    private static Room Map(SqlDataReader rd) => new()
    {
        RoomId = rd.GetInt32(0),
        RoomNumber = rd.GetString(1),
        RoomType = rd.GetString(2),
        Price = rd.GetDecimal(3),
        Capacity = rd.GetInt32(4),
        Description = rd.IsDBNull(5) ? "" : rd.GetString(5),
        ImageUrl = rd.IsDBNull(6) ? null : rd.GetString(6),
        IsAvailable = rd.GetBoolean(7)
    };

    /// <summary>All rooms (for admin).</summary>
    public List<Room> GetAll()
    {
        var list = new List<Room>();
        using var conn = new SqlConnection(_cs);
        using var cmd = new SqlCommand($"SELECT {SelectColumns} FROM dbo.Rooms ORDER BY RoomNumber", conn);
        conn.Open();
        using var rd = cmd.ExecuteReader();
        while (rd.Read()) list.Add(Map(rd));
        return list;
    }

    public Room? GetById(int id)
    {
        using var conn = new SqlConnection(_cs);
        using var cmd = new SqlCommand($"SELECT {SelectColumns} FROM dbo.Rooms WHERE RoomId = @Id", conn);
        cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        conn.Open();
        using var rd = cmd.ExecuteReader();
        return rd.Read() ? Map(rd) : null;
    }

    /// <summary>
    /// Rooms a guest can book. If dates are given, rooms with an overlapping
    /// non-cancelled booking are excluded. If guests is given, rooms that are too small are excluded.
    /// </summary>
    public List<Room> GetAvailable(DateTime? checkIn, DateTime? checkOut, int? guests)
    {
        var list = new List<Room>();
        var sql = $"SELECT {SelectColumns} FROM dbo.Rooms r WHERE r.IsAvailable = 1";
        if (guests.HasValue) sql += " AND r.Capacity >= @Guests";
        if (checkIn.HasValue && checkOut.HasValue)
        {
            sql += @" AND NOT EXISTS (SELECT 1 FROM dbo.Bookings b
                                      WHERE b.RoomId = r.RoomId
                                        AND b.Status <> 'Cancelled'
                                        AND b.CheckIn < @CheckOut
                                        AND b.CheckOut > @CheckIn)";
        }
        sql += " ORDER BY r.Price";

        using var conn = new SqlConnection(_cs);
        using var cmd = new SqlCommand(sql, conn);
        if (guests.HasValue) cmd.Parameters.Add("@Guests", SqlDbType.Int).Value = guests.Value;
        if (checkIn.HasValue && checkOut.HasValue)
        {
            cmd.Parameters.Add("@CheckIn", SqlDbType.Date).Value = checkIn.Value.Date;
            cmd.Parameters.Add("@CheckOut", SqlDbType.Date).Value = checkOut.Value.Date;
        }
        conn.Open();
        using var rd = cmd.ExecuteReader();
        while (rd.Read()) list.Add(Map(rd));
        return list;
    }

    public bool RoomNumberExists(string roomNumber, int excludeRoomId = 0)
    {
        using var conn = new SqlConnection(_cs);
        using var cmd = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.Rooms WHERE RoomNumber = @No AND RoomId <> @Id", conn);
        cmd.Parameters.Add("@No", SqlDbType.NVarChar, 20).Value = roomNumber;
        cmd.Parameters.Add("@Id", SqlDbType.Int).Value = excludeRoomId;
        conn.Open();
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    private static void AddParams(SqlCommand cmd, Room r)
    {
        cmd.Parameters.Add("@RoomNumber", SqlDbType.NVarChar, 20).Value = r.RoomNumber.Trim();
        cmd.Parameters.Add("@RoomType", SqlDbType.NVarChar, 100).Value = r.RoomType.Trim();
        var price = cmd.Parameters.Add("@Price", SqlDbType.Decimal);
        price.Precision = 10; price.Scale = 2; price.Value = r.Price;
        cmd.Parameters.Add("@Capacity", SqlDbType.Int).Value = r.Capacity;
        cmd.Parameters.Add("@Description", SqlDbType.NVarChar, 1000).Value = r.Description.Trim();
        cmd.Parameters.Add("@ImageUrl", SqlDbType.NVarChar, 500).Value =
            string.IsNullOrWhiteSpace(r.ImageUrl) ? DBNull.Value : r.ImageUrl.Trim();
        cmd.Parameters.Add("@IsAvailable", SqlDbType.Bit).Value = r.IsAvailable;
    }

    public int Create(Room r)
    {
        using var conn = new SqlConnection(_cs);
        using var cmd = new SqlCommand(
            @"INSERT INTO dbo.Rooms (RoomNumber, RoomType, Price, Capacity, Description, ImageUrl, IsAvailable)
              OUTPUT INSERTED.RoomId
              VALUES (@RoomNumber, @RoomType, @Price, @Capacity, @Description, @ImageUrl, @IsAvailable)", conn);
        AddParams(cmd, r);
        conn.Open();
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public bool Update(Room r)
    {
        using var conn = new SqlConnection(_cs);
        using var cmd = new SqlCommand(
            @"UPDATE dbo.Rooms
              SET RoomNumber = @RoomNumber, RoomType = @RoomType, Price = @Price, Capacity = @Capacity,
                  Description = @Description, ImageUrl = @ImageUrl, IsAvailable = @IsAvailable
              WHERE RoomId = @RoomId", conn);
        AddParams(cmd, r);
        cmd.Parameters.Add("@RoomId", SqlDbType.Int).Value = r.RoomId;
        conn.Open();
        return cmd.ExecuteNonQuery() == 1;
    }

    public int CountBookings(int roomId)
    {
        using var conn = new SqlConnection(_cs);
        using var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.Bookings WHERE RoomId = @Id", conn);
        cmd.Parameters.Add("@Id", SqlDbType.Int).Value = roomId;
        conn.Open();
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public enum DeleteResult { Deleted, NotFound, HasBookings }

    /// <summary>Deletes a room only if it has no bookings (checked inside one transaction).</summary>
    public DeleteResult Delete(int roomId)
    {
        using var conn = new SqlConnection(_cs);
        conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        using (var check = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.Bookings WITH (UPDLOCK, HOLDLOCK) WHERE RoomId = @Id", conn, tx))
        {
            check.Parameters.Add("@Id", SqlDbType.Int).Value = roomId;
            if (Convert.ToInt32(check.ExecuteScalar()) > 0)
            {
                tx.Rollback();
                return DeleteResult.HasBookings;
            }
        }

        using var del = new SqlCommand("DELETE FROM dbo.Rooms WHERE RoomId = @Id", conn, tx);
        del.Parameters.Add("@Id", SqlDbType.Int).Value = roomId;
        var rows = del.ExecuteNonQuery();
        tx.Commit();
        return rows == 1 ? DeleteResult.Deleted : DeleteResult.NotFound;
    }
}
