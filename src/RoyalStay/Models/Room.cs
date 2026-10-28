using System.ComponentModel.DataAnnotations;

namespace RoyalStay.Models;

// Maps to dbo.Rooms. Column names are unchanged from the original schema usage:
// RoomId, RoomNumber, Price, IsAvailable (+ RoomType, Capacity, Description, ImageUrl).
public class Room : IValidatableObject
{
    public int RoomId { get; set; }

    [Required(ErrorMessage = "Room number is required.")]
    [StringLength(20)]
    [RegularExpression(@"^[A-Za-z0-9\-]+$", ErrorMessage = "Room number may contain only letters, digits and '-'.")]
    [Display(Name = "Room number")]
    public string RoomNumber { get; set; } = "";

    [Required(ErrorMessage = "Room type is required.")]
    [StringLength(100)]
    [Display(Name = "Room type")]
    public string RoomType { get; set; } = "";

    [Range(typeof(decimal), "1", "1000000", ErrorMessage = "Price must be between 1 and 1,000,000.")]
    [Display(Name = "Price per night (₹)")]
    public decimal Price { get; set; }

    [Range(1, 10, ErrorMessage = "Capacity must be between 1 and 10 guests.")]
    [Display(Name = "Max guests")]
    public int Capacity { get; set; } = 2;

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(1000)]
    public string Description { get; set; } = "";

    [StringLength(500)]
    [Display(Name = "Image URL")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Available for booking")]
    public bool IsAvailable { get; set; } = true;

    public const string DefaultImage =
        "https://images.unsplash.com/photo-1611892440504-42a792e24d32?auto=format&fit=crop&w=1100&q=85";

    public string DisplayImage => string.IsNullOrWhiteSpace(ImageUrl) ? DefaultImage : ImageUrl!;

    // Only http/https image URLs are allowed (blocks javascript:, data:, file: etc.)
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(ImageUrl))
        {
            if (!Uri.TryCreate(ImageUrl.Trim(), UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                yield return new ValidationResult(
                    "Image URL must be a valid http:// or https:// address.",
                    new[] { nameof(ImageUrl) });
            }
        }
    }
}
