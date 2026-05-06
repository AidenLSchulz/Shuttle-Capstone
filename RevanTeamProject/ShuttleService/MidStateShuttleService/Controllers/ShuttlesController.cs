using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MidStateShuttleService.Models;
using MidStateShuttleService.Service;

namespace MidStateShuttleService.Controllers
{
    public class ShuttlesController : Controller
    {
        private readonly ILogger<ShuttlesController> _logger;
        private readonly ApplicationDbContext _context;

        public ShuttlesController(ApplicationDbContext context, ILogger<ShuttlesController> logger)
        {
            _context = context;
            _logger = logger;

            _logger.LogInformation("ShuttlesController initialized.");
        }

        // GET: ShuttlesController
        public ActionResult Index()
        {
            _logger.LogInformation("Shuttles Index action called.");
            return View();
        }

        // GET: ShuttlesController/Details/5
        public ActionResult Details(int id)
        {
            _logger.LogInformation("Shuttles Details action called for BusId: {BusId}", id);
            return View();
        }

        // GET: ShuttlesController/Create
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public ActionResult Create()
        {
            _logger.LogInformation("Shuttles Create GET action called.");

            return View();
        }

        /// <summary>
        /// Handles submission of the shuttle creation form and persists the new bus record.
        /// Restricted to Admin role only.
        /// </summary>
        // POST: ShuttlesController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Create(Bus bus)
        {
            _logger.LogInformation("Shuttles Create POST action called.");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Shuttles Create POST failed ModelState validation.");

                return View(bus);
            }

            try
            {
                BusServices bs = new BusServices(_context);

                // All newly created buses are marked active by default.
                bus.IsActive = true;
                bs.AddEntity(bus);

                _logger.LogInformation("Bus created successfully.");

                // Set success state in both Session and TempData to cover session-based and redirect-based UI feedback.
                TempData["SuccessMessage"] = "The bus has been successfully created!";
                HttpContext.Session.SetString("ShuttleSuccess", "true");
                TempData["ShuttleSuccess"] = true;

                return RedirectToAction(nameof(Create));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating the bus.");

                ModelState.AddModelError("", "An unexpected error occurred while creating the shuttle.");
                return View(bus);
            }
        }

        /// <summary>
        /// Displays the edit form for an existing shuttle. Restricted to Admin role only.
        /// </summary>
        // GET: ShuttlesController/Edit/5
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public ActionResult Edit(int id)
        {
            _logger.LogInformation("Shuttles Edit GET action called for BusId: {BusId}", id);

            try
            {
                var bus = _context.Buses.Find(id);

                if (bus == null)
                {
                    _logger.LogWarning("Shuttles Edit GET could not find BusId: {BusId}", id);
                    return NotFound();
                }

                return View(bus);
            }
            catch (Exception ex)
            {
                // Any retrieval failure redirects to the Dashboard rather than surfacing an error page.
                _logger.LogError(ex, "An error occurred while loading shuttle {BusId} for edit.", id);
                return RedirectToAction("Index", "Dashboard");
            }
        }

        /// <summary>
        /// Handles submission of the shuttle edit form and persists updated fields to the existing record.
        /// Restricted to Admin role only.
        /// </summary>
        // POST: ShuttlesController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(int id, Bus bus)
        {
            _logger.LogInformation("Shuttles Edit POST action called for BusId: {BusId}", id);

            // Guard against a null model or form tampering by verifying the route ID matches the submitted model ID.
            if (bus == null || id != bus.BusId)
            {
                _logger.LogWarning("Shuttles Edit POST received invalid bus data for BusId: {BusId}", id);
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Shuttles Edit POST failed ModelState validation for BusId: {BusId}", id);

                return View(bus);
            }

            try
            {
                // Re-fetch the existing record to apply updates against rather than replacing the entity directly.
                var existingBus = _context.Buses.Find(id);

                if (existingBus == null)
                {
                    _logger.LogWarning("Shuttles Edit POST could not find BusId: {BusId}", id);
                    return NotFound();
                }

                // Apply only the editable fields from the submitted model.
                // IsActive is intentionally excluded — editing a shuttle must not reactivate an archived one.
                existingBus.BusNo = bus.BusNo;
                existingBus.PassengerCapacity = bus.PassengerCapacity;

                _context.SaveChanges();

                _logger.LogInformation("Shuttle updated successfully for BusId: {BusId}", id);

                // Set success state in both Session and TempData to cover session-based and redirect-based UI feedback.
                TempData["SuccessMessage"] = "The bus has been successfully updated!";
                HttpContext.Session.SetString("ShuttleSuccess", "true");
                TempData["ShuttleSuccess"] = true;

                return RedirectToAction(nameof(Edit), new { id = existingBus.BusId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating shuttle {BusId}.", id);

                ModelState.AddModelError("", "An unexpected error occurred while updating the shuttle.");
                return View(bus);
            }
        }

        /// <summary>
        /// Displays a list of all shuttles, optionally showing archived (inactive) records.
        /// Accessible by Admin and Driver roles.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Driver")]
        public ActionResult ViewAll(bool viewArchived = false)
        {
            _logger.LogInformation("Shuttles ViewAll action called. ViewArchived: {ViewArchived}", viewArchived);

            // Filter shuttles by active/archived status.
            // Active records are returned by default; archived records when viewArchived=true.
            var shuttles = _context.Buses
                .Where(b => b.IsActive == !viewArchived)
                .ToList();

            // Pass the current archive filter state to the view to drive UI elements such as tab state or button labels.
            ViewData["Archives"] = viewArchived;

            _logger.LogInformation("Shuttles ViewAll returning {ShuttleCount} shuttles.", shuttles.Count);

            return View("ShuttleTable", shuttles);
        }

        /// <summary>
        /// Toggles the active/archived state of a shuttle (active → archived, or archived → active)
        /// and surfaces a context-aware success message based on the resulting state.
        /// Restricted to Admin role only.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public ActionResult Delete(int id)
        {
            _logger.LogInformation("Shuttles Delete GET action called for BusId: {BusId}", id);

            try
            {
                var shuttle = _context.Buses.Find(id);

                if (shuttle == null)
                {
                    _logger.LogWarning("Shuttles Delete GET could not find BusId: {BusId}", id);
                    TempData["ErrorMessage"] = "Shuttle not found.";
                    return RedirectToAction("Index", "Dashboard");
                }

                // Flip the active state — active shuttles become archived, archived shuttles become active.
                // IsActive is nullable so a null value is treated as false before toggling.
                bool isCurrentlyActive = shuttle.IsActive ?? false;
                shuttle.IsActive = !isCurrentlyActive;

                _context.Buses.Update(shuttle);
                _context.SaveChanges();

                _logger.LogInformation("Shuttle IsActive toggled successfully for BusId: {BusId}", id);

                // Surface a context-aware message based on the resulting state after the toggle.
                // Unlike other Delete actions in the codebase, this one distinguishes restore from archive in the UI.
                TempData["SuccessMessage"] = shuttle.IsActive == true
                    ? "The shuttle has been restored successfully!"
                    : "The shuttle has been removed successfully!";

                return RedirectToAction("ViewAll");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while toggling IsActive of the shuttle {BusId}.", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while updating the shuttle.";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        /// <summary>
        /// Restores an archived shuttle by explicitly setting its active state to <c>true</c>.
        /// Restricted to Admin role only.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Unarchive(int id)
        {
            _logger.LogInformation("Shuttles Unarchive action called for BusId: {BusId}", id);

            var shuttle = _context.Buses.Find(id);

            if (shuttle == null)
            {
                _logger.LogWarning("Shuttles Unarchive could not find BusId: {BusId}", id);
                return NotFound();
            }

            // Explicitly set IsActive to true rather than toggling — this action is unarchive-only.
            // For a bidirectional toggle, see the Delete GET action above.
            shuttle.IsActive = true;
            _context.SaveChanges();

            _logger.LogInformation("Shuttle unarchived successfully for BusId: {BusId}", id);

            return RedirectToAction("ViewAll", new { viewArchived = true });
        }
    }
}