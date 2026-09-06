using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NekretnineApp.Models;
using NekretnineApp.Models.ViewModels;
using NekretnineApp.Repositories;

namespace NekretnineApp.Controllers
{
    [Authorize]
    public class ReservationsController : Controller
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly IPropertyRepository _propertyRepository;

        public ReservationsController(
            IReservationRepository reservationRepository,
            IPropertyRepository propertyRepository)
        {
            _reservationRepository = reservationRepository;
            _propertyRepository = propertyRepository;
        }

        [Authorize(Roles = RoleNames.User)]
        public async Task<IActionResult> Create(int propertyId)
        {
            var property = await _propertyRepository.GetByIdAsync(propertyId);
            if (property == null || !property.IsActive)
                return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (property.UserId == currentUserId)
            {
                TempData["Error"] = "Ne možete zakazati obilazak sopstvene nekretnine.";
                return RedirectToAction("Details", "Properties", new { id = propertyId });
            }

            ViewBag.Property = property;
            return View(new Reservation
            {
                PropertyId = propertyId,
                AppointmentDateTime = DateTime.Today.AddDays(1).AddHours(10)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.User)]
        public async Task<IActionResult> Create(Reservation model)
        {
            var property = await _propertyRepository.GetByIdAsync(model.PropertyId);
            if (property == null || !property.IsActive)
                return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
                return Challenge();

            if (property.UserId == currentUserId)
                ModelState.AddModelError(string.Empty, "Ne možete zakazati obilazak sopstvene nekretnine.");

            if (model.AppointmentDateTime <= DateTime.Now)
                ModelState.AddModelError(nameof(model.AppointmentDateTime), "Termin obilaska mora biti u budućnosti.");

            if (model.AppointmentDateTime.Minute % 15 != 0)
                ModelState.AddModelError(nameof(model.AppointmentDateTime), "Vrijeme obilaska izaberite u intervalima od 15 minuta.");

            if (await _reservationRepository.HasConflictAsync(model.PropertyId, model.AppointmentDateTime))
                ModelState.AddModelError(nameof(model.AppointmentDateTime), "Za izabrani termin već postoji zahtjev ili prihvaćen obilazak.");

            if (!ModelState.IsValid)
            {
                ViewBag.Property = property;
                return View(model);
            }

            var reservation = new Reservation
            {
                PropertyId = model.PropertyId,
                AppointmentDateTime = model.AppointmentDateTime,
                Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim(),
                UserId = currentUserId,
                Status = ReservationStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _reservationRepository.AddAsync(reservation);
            await _reservationRepository.SaveAsync();

            TempData["Success"] = "Zahtjev za obilazak je uspješno poslat.";
            return RedirectToAction(nameof(MyReservations));
        }

        [Authorize(Roles = RoleNames.User)]
        public async Task<IActionResult> MyReservations()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            return View(await _reservationRepository.GetByUserIdAsync(userId));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.User)]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var reservation = await _reservationRepository.GetByIdAsync(id);

            if (reservation == null)
                return NotFound();

            if (reservation.UserId != userId)
                return Forbid();

            if (reservation.Status is ReservationStatus.Cancelled or ReservationStatus.Rejected)
            {
                TempData["Error"] = "Ovaj zahtjev nije moguće otkazati.";
                return RedirectToAction(nameof(MyReservations));
            }

            if (reservation.AppointmentDateTime <= DateTime.Now)
            {
                TempData["Error"] = "Termin koji je već počeo nije moguće otkazati.";
                return RedirectToAction(nameof(MyReservations));
            }

            reservation.Status = ReservationStatus.Cancelled;
            _reservationRepository.Update(reservation);
            await _reservationRepository.SaveAsync();

            TempData["Success"] = "Zahtjev za obilazak je otkazan.";
            return RedirectToAction(nameof(MyReservations));
        }

        [Authorize(Roles = RoleNames.Advertiser + "," + RoleNames.Admin)]
        public async Task<IActionResult> Requests(ReservationStatus? status)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            var reservations = User.IsInRole(RoleNames.Admin)
                ? await _reservationRepository.GetAllAsync(status)
                : await _reservationRepository.GetForAdvertiserAsync(userId, status);

            var model = reservations.Select(r => new ReservationItemViewModel
            {
                Reservation = r,
                UserEmail = r.User?.Email ?? "Nepoznat korisnik",
                UserName = r.User?.FullName ?? "Nepoznat korisnik",
                UserPhone = r.User?.PhoneNumber ?? "Nije unesen"
            }).ToList();

            ViewBag.SelectedStatus = status;
            return View(model);
        }

        [Authorize(Roles = RoleNames.Advertiser + "," + RoleNames.Admin)]
        public IActionResult Index(ReservationStatus? status)
        {
            return RedirectToAction(nameof(Requests), new { status });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Advertiser + "," + RoleNames.Admin)]
        public async Task<IActionResult> Approve(int id)
        {
            var reservation = await _reservationRepository.GetByIdAsync(id);
            if (reservation == null)
                return NotFound();

            if (!CanManageRequest(reservation))
                return Forbid();

            if (reservation.Status != ReservationStatus.Pending)
            {
                TempData["Error"] = "Samo zahtjev na čekanju može biti prihvaćen.";
                return RedirectToAction(nameof(Requests));
            }

            if (reservation.AppointmentDateTime <= DateTime.Now)
            {
                TempData["Error"] = "Termin je prošao i zahtjev nije moguće prihvatiti.";
                return RedirectToAction(nameof(Requests));
            }

            if (await _reservationRepository.HasConflictAsync(
                    reservation.PropertyId,
                    reservation.AppointmentDateTime,
                    reservation.Id,
                    approvedOnly: true))
            {
                TempData["Error"] = "Za izabrani termin već postoji prihvaćen obilazak.";
                return RedirectToAction(nameof(Requests));
            }

            reservation.Status = ReservationStatus.Approved;
            _reservationRepository.Update(reservation);
            await _reservationRepository.SaveAsync();

            TempData["Success"] = "Zahtjev za obilazak je prihvaćen.";
            return RedirectToAction(nameof(Requests));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Advertiser + "," + RoleNames.Admin)]
        public async Task<IActionResult> Reject(int id)
        {
            var reservation = await _reservationRepository.GetByIdAsync(id);
            if (reservation == null)
                return NotFound();

            if (!CanManageRequest(reservation))
                return Forbid();

            if (reservation.Status != ReservationStatus.Pending)
            {
                TempData["Error"] = "Samo zahtjev na čekanju može biti odbijen.";
                return RedirectToAction(nameof(Requests));
            }

            reservation.Status = ReservationStatus.Rejected;
            _reservationRepository.Update(reservation);
            await _reservationRepository.SaveAsync();

            TempData["Success"] = "Zahtjev za obilazak je odbijen.";
            return RedirectToAction(nameof(Requests));
        }

        private bool CanManageRequest(Reservation reservation)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return User.IsInRole(RoleNames.Admin) || reservation.Property?.UserId == userId;
        }
    }
}
