using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RoyalStay.Data;
using RoyalStay.Models;

namespace RoyalStay.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin/Rooms/{action=Index}/{id?}")]
public class AdminRoomsController : Controller
{
    private readonly RoomRepository _rooms;

    public AdminRoomsController(RoomRepository rooms) => _rooms = rooms;

    [HttpGet]
    public IActionResult Index() => View(_rooms.GetAll());

    [HttpGet]
    public IActionResult Create() => View(new Room());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Room model)
    {
        model.RoomId = 0;
        if (_rooms.RoomNumberExists(model.RoomNumber.Trim()))
            ModelState.AddModelError(nameof(model.RoomNumber), "This room number already exists.");

        if (!ModelState.IsValid) return View(model);

        try
        {
            _rooms.Create(model);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            ModelState.AddModelError(nameof(model.RoomNumber), "This room number already exists.");
            return View(model);
        }

        TempData["Success"] = $"Room {model.RoomNumber} was added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        var room = _rooms.GetById(id);
        return room == null ? NotFound() : View(room);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, Room model)
    {
        if (id != model.RoomId) return BadRequest();
        if (_rooms.GetById(id) == null) return NotFound();

        if (_rooms.RoomNumberExists(model.RoomNumber.Trim(), model.RoomId))
            ModelState.AddModelError(nameof(model.RoomNumber), "This room number already exists.");

        if (!ModelState.IsValid) return View(model);

        try
        {
            _rooms.Update(model);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            ModelState.AddModelError(nameof(model.RoomNumber), "This room number already exists.");
            return View(model);
        }

        TempData["Success"] = $"Room {model.RoomNumber} was updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Delete(int id)
    {
        var room = _rooms.GetById(id);
        if (room == null) return NotFound();
        ViewBag.BookingCount = _rooms.CountBookings(id);
        return View(room);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        try
        {
            switch (_rooms.Delete(id))
            {
                case RoomRepository.DeleteResult.Deleted:
                    TempData["Success"] = "Room deleted.";
                    break;
                case RoomRepository.DeleteResult.HasBookings:
                    TempData["Error"] = "This room has bookings and cannot be deleted. Edit the room and untick 'Available for booking' instead.";
                    break;
                default:
                    TempData["Error"] = "Room not found.";
                    break;
            }
        }
        catch (SqlException ex) when (ex.Number == 547) // FK violation safety net
        {
            TempData["Error"] = "This room is referenced by other records and cannot be deleted.";
        }
        return RedirectToAction(nameof(Index));
    }
}
