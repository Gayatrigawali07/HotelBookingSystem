using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RoyalStay.Data;
using RoyalStay.Models;

namespace RoyalStay.Controllers;

public class BookingsController : Controller
{
    private readonly BookingRepository _bookings;
    private readonly RoomRepository _rooms;

    public BookingsController(BookingRepository bookings, RoomRepository rooms)
    {
        _bookings = bookings;
        _rooms = rooms;
    }

    [HttpGet]
    public IActionResult Create(int roomId, DateTime? checkIn = null, DateTime? checkOut = null)
    {
        var room = _rooms.GetById(roomId);
        if (room == null || !room.IsAvailable) return NotFound();

        return View(new BookingFormViewModel
        {
            RoomId = room.RoomId,
            RoomName = room.RoomType,
            PricePerNight = room.Price,
            RoomCapacity = room.Capacity,
            Guests = Math.Min(2, room.Capacity),
            CheckIn = checkIn,
            CheckOut = checkOut
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(BookingFormViewModel model)
    {
        var room = _rooms.GetById(model.RoomId);
        if (room == null || !room.IsAvailable) return NotFound();

        // Never trust hidden fields: always take name / price / capacity from the database.
        model.RoomName = room.RoomType;
        model.PricePerNight = room.Price;
        model.RoomCapacity = room.Capacity;

        if (model.CheckIn.HasValue && model.CheckOut.HasValue)
        {
            if (model.CheckIn.Value.Date < DateTime.Today)
                ModelState.AddModelError(nameof(model.CheckIn), "Check-in date cannot be in the past.");
            if (model.CheckOut.Value.Date <= model.CheckIn.Value.Date)
                ModelState.AddModelError(nameof(model.CheckOut), "Check-out must be after check-in.");
        }

        if (model.Guests > room.Capacity)
            ModelState.AddModelError(nameof(model.Guests), $"This room allows a maximum of {room.Capacity} guest(s).");

        if (!ModelState.IsValid)
            return View(model);

        BookingResult result;
        try
        {
            result = _bookings.Create(
                model.RoomId, model.FullName, model.Email, model.Phone,
                model.CheckIn!.Value, model.CheckOut!.Value, model.Guests);
        }
        catch (SqlException ex) when (ex.Number == 1205) // deadlock victim: two guests booked at the same instant
        {
            ModelState.AddModelError("", "Another guest was booking at the same moment. Please press Confirm again.");
            return View(model);
        }

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        return RedirectToAction(nameof(Confirmation),
            new { id = result.BookingId, email = model.Email.Trim().ToLowerInvariant() });
    }

    // Shown after a successful save. Data is read back from SQL Server (not from the form).
    public IActionResult Confirmation(int id, string email)
    {
        var booking = string.IsNullOrWhiteSpace(email) ? null : _bookings.GetByIdAndEmail(id, email);
        return booking == null ? NotFound() : View(booking);
    }

    // Guests have no accounts, so they look up their bookings by the email used at booking time.
    // To protect guest privacy, the lookup needs BOTH the email and the phone number used at booking time.
    public IActionResult MyBookings(string? email, string? phone)
    {
        ViewBag.Email = email;
        ViewBag.Phone = phone;
        ViewBag.Searched = false;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(phone))
            return View(new List<Booking>());

        ViewBag.Searched = true;
        return View(_bookings.GetByEmailAndPhone(email, phone));
    }
}
