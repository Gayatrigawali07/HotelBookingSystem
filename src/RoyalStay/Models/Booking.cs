namespace RoyalStay.Models;

// Read model for listing bookings (joined with dbo.Rooms).
public class Booking
{
    public int BookingId { get; set; }
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = "";
    public string RoomType { get; set; } = "";
    public string GuestName { get; set; } = "";
    public string GuestEmail { get; set; } = "";
    public string? GuestPhone { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Guests { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }

    public int Nights => (CheckOut.Date - CheckIn.Date).Days;
}

public static class BookingStatus
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";
    public const string Completed = "Completed";

    public static readonly string[] All = { Pending, Confirmed, Cancelled, Completed };

    public static bool IsValid(string? s) => s != null && All.Contains(s);
}
