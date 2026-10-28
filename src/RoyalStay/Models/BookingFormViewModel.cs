using System.ComponentModel.DataAnnotations;

namespace RoyalStay.Models;

public class BookingFormViewModel
{
    public int RoomId { get; set; }
    public string RoomName { get; set; } = "";
    public decimal PricePerNight { get; set; }
    public int RoomCapacity { get; set; } = 4;

    [Required, StringLength(100)]
    public string FullName { get; set; } = "";

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = "";

    [Required, RegularExpression(@"^[6-9]\d{9}$",
        ErrorMessage = "Enter a valid 10-digit Indian mobile number.")]
    public string Phone { get; set; } = "";

    [Range(1, 10)]
    public int Guests { get; set; } = 2;

    [Required, DataType(DataType.Date)]
    public DateTime? CheckIn { get; set; }

    [Required, DataType(DataType.Date)]
    public DateTime? CheckOut { get; set; }
}
