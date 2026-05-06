using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MidStateShuttleService.Models;
using MidStateShuttleService.Service;
using MidStateShuttleService.Services;
using MidStateShuttleService.ViewModels;

namespace MidStateShuttleService.Controllers
{
    public class CheckInController : Controller
    {
        private readonly CheckInServices _checkInService;
        private readonly LocationServices _locationService;
        private readonly ILogger<CheckInController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public CheckInController(
            ApplicationDbContext context,
            CheckInServices checkInService,
            LocationServices locationService,
            ILogger<CheckInController> logger,
            IWebHostEnvironment environment,
            IMemoryCache cache)
        {
            _context = context;
            _checkInService = checkInService;
            _locationService = locationService;
            _logger = logger;
            _environment = environment;
            _cache = cache;
        }

        [AllowAnonymous] // DEV NOTE: Public endpoint used by riders to access the check-in form.
        [HttpGet]
        public IActionResult CheckIn()
        {
            _logger.LogInformation("Check-in page accessed.");

            ViewBag.Locations = GetLocationOptions(); // DEV NOTE: Dropdown population logic centralized below.
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult CheckIn(CheckIn submittedCheckIn)
        {
            // ===============================
            // CHECK-IN RATE LIMITER
            // Prevents spam submissions per IP address using a fixed time window
            // ===============================

            const int LIMIT = 20;               // Max allowed requests per window
            const int WINDOW_MINUTES = 5;       // Time window length

            var now = DateTime.UtcNow;

            // Identify user by IP address (basic anti-spam mechanism)
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var key = $"ratelimit_checkin_{ip}";

            // ===============================
            // Retrieve existing rate limit entry for this IP
            // Stored as (Count, FirstAttemptTime)
            // ===============================
            var entry = _cache.Get<(int Count, DateTime FirstAttempt)>(key);

            // ===============================
            // CASE 1: No entry exists OR window has expired
            // -> Start a new tracking window
            // ===============================
            if (entry == default || (now - entry.FirstAttempt).TotalMinutes > WINDOW_MINUTES)
            {
                entry = (Count: 1, FirstAttempt: now);

                _cache.Set(key, entry, TimeSpan.FromMinutes(WINDOW_MINUTES));

                _logger.LogInformation(
                    "CheckIn RateLimit RESET | IP: {IP} | WindowStart: {Start}",
                    ip,
                    now
                );
            }
            else
            {
                // ===============================
                // CASE 2: Existing window active
                // -> Increment request count
                // ===============================
                entry.Count++;

                // Calculate remaining time in current window
                var remaining = TimeSpan.FromMinutes(WINDOW_MINUTES) - (now - entry.FirstAttempt);

                // Safety fallback (prevents negative expiration values)
                if (remaining <= TimeSpan.Zero)
                    remaining = TimeSpan.FromSeconds(1);

                // Update cache with new count and remaining TTL
                _cache.Set(key, entry, remaining);

                _logger.LogInformation(
                    "CheckIn RateLimit HIT | IP: {IP} | Count: {Count}/{Limit} | ElapsedSec: {Elapsed} | RemainingSec: {Remaining}",
                    ip,
                    entry.Count,
                    LIMIT,
                    (now - entry.FirstAttempt).TotalSeconds,
                    remaining.TotalSeconds
                );
            }

            // ===============================
            // BURST DETECTION (potential spam behavior)
            // Triggers if 4+ requests occur within 30 seconds
            // ===============================
            if (entry.Count >= 4 && (now - entry.FirstAttempt).TotalSeconds <= 30)
            {
                _logger.LogWarning(
                    "CheckIn RateLimit BURST | IP: {IP} | Count: {Count} in {Seconds}s",
                    ip,
                    entry.Count,
                    (now - entry.FirstAttempt).TotalSeconds
                );
            }

            // ===============================
            // ENFORCEMENT STEP
            // Block request if limit exceeded
            // ===============================
            if (entry.Count > LIMIT)
            {
                _logger.LogWarning(
                    "CheckIn RateLimit BLOCKED | IP: {IP} | Count: {Count} exceeded {Limit} | WindowStart: {Start}",
                    ip,
                    entry.Count,
                    LIMIT,
                    entry.FirstAttempt
                );

                TempData["Error"] = "There have been too many submissions under your internet. Please wait before trying again.";
                TempData["Code"] = "E001CI";
                ViewBag.Locations = GetLocationOptions();
                return View(submittedCheckIn);
            }

            // ===============================
            // END RATE LIMITER
            // ===============================

            _logger.LogInformation("Check-in submission received for Name: {Name}, StudentId: {StudentId}", submittedCheckIn?.Name, submittedCheckIn?.StudentId);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Check-in submission failed validation for Name: {Name}, StudentId: {StudentId}", submittedCheckIn?.Name, submittedCheckIn?.StudentId);

                ViewBag.Locations = GetLocationOptions();

                TempData["Error"] = "Please fill out all required fields.";
                TempData["Code"] = "E002CI";

                return View(submittedCheckIn);
            }

            submittedCheckIn.Date = DateTime.UtcNow;
            submittedCheckIn.IsActive = true;

            _checkInService.AddEntity(submittedCheckIn);

            _logger.LogInformation("Check-in created successfully for CheckInId: {CheckInId}, Name: {Name}", submittedCheckIn.CheckInId, submittedCheckIn.Name);

            TempData["Success"] = "Check-in successful!";
            TempData["Code"] = "S001CI";

            return RedirectToAction(nameof(CheckIn));
        }

        /// <summary>
        /// Displays the edit form for an existing check-in. Restricted to Admin role only.
        /// </summary>
        /// <param name="id">The ID of the check-in to edit.</param>
        /// <returns>The edit view populated with the check-in's current data, or a failure result if not found.</returns>
        [Authorize(Roles = "Admin")] // DEV NOTE: Only administrators can edit existing check-ins.
        [HttpGet]
        public IActionResult EditCheckIn(int id)
        {
            _logger.LogInformation("EditCheckIn GET requested for CheckInId: {CheckInId}", id);

            // Attempt to retrieve the existing check-in by its ID.
            CheckIn existingCheckIn = _checkInService.GetEntityById(id);

            // Return a failure response if no matching check-in record was found.
            if (existingCheckIn == null)
            {
                _logger.LogWarning("EditCheckIn GET failed. Check-in not found for CheckInId: {CheckInId}", id);
                return FailedCheckIn("Check-in not found.");
            }

            // Map the retrieved check-in entity to a view model to pass to the view.
            var viewModel = new CheckInViewModel
            {
                CheckInId = existingCheckIn.CheckInId,
                Name = existingCheckIn.Name,
                UtcDate = existingCheckIn.Date,
                Comments = existingCheckIn.Comments,
                FirstTime = existingCheckIn.FirstTime,
                LocationId = existingCheckIn.LocationId,
                IsActive = existingCheckIn.IsActive,
                StudentId = existingCheckIn.StudentId,
                DropOffLocationId = existingCheckIn.DropOffLocationId,
                LocationOptions = GetLocationOptions()    // Populate dropdown options for location selection.
            };

            return View(viewModel);
        }

        /// <summary>
        /// Handles the form submission for editing an existing check-in. Restricted to Admin role only.
        /// </summary>
        /// <param name="submittedModel">The view model containing the updated check-in data from the form.</param>
        /// <returns>
        /// Redirects to the ViewAll action on success, or returns the edit view with error details on failure.
        /// </returns>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditCheckIn(CheckInViewModel submittedModel)
        {
            _logger.LogInformation("EditCheckIn POST received for CheckInId: {CheckInId}", submittedModel.CheckInId);

            // Validate the submitted form data against model annotations before processing.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("EditCheckIn POST failed validation for CheckInId: {CheckInId}", submittedModel.CheckInId);

                // E003CI: Model validation failure — re-populate dropdowns and return the form with an error message.
                TempData["Error"] = "Please fill out all required fields.";
                TempData["Code"] = "E003CI";
                submittedModel.LocationOptions = GetLocationOptions(); // Must be re-populated since options are not persisted across POST.
                return View(submittedModel);
            }

            // Retrieve the original check-in record to apply updates against.
            CheckIn existingCheckIn = _checkInService.GetEntityById(submittedModel.CheckInId);

            // Return a failure response if the check-in no longer exists (e.g. deleted between GET and POST).
            if (existingCheckIn == null)
            {
                _logger.LogWarning("EditCheckIn POST failed. Check-in not found for CheckInId: {CheckInId}", submittedModel.CheckInId);

                // E004CI: Record not found — surface the error to the user and abort the update.
                TempData["Error"] = "Check-in not found.";
                TempData["Code"] = "E004CI";
                return FailedCheckIn("Check-in not found.");
            }

            // Apply the submitted changes to the retrieved entity.
            // Note: StudentId and DropOffLocationId are intentionally excluded from editing.
            existingCheckIn.Name = submittedModel.Name;
            existingCheckIn.Comments = submittedModel.Comments;
            existingCheckIn.FirstTime = submittedModel.FirstTime;
            existingCheckIn.LocationId = submittedModel.LocationId;
            existingCheckIn.IsActive = true;                  // Editing a check-in always marks it as active.
            existingCheckIn.Date = submittedModel.UtcDate;    // Date is stored in UTC.

            // Persist the updated entity to the data store.
            _checkInService.UpdateEntity(existingCheckIn);

            _logger.LogInformation("Check-in updated successfully for CheckInId: {CheckInId}", submittedModel.CheckInId);

            // S002CI: Successful update — notify the user and redirect to the full check-in list.
            TempData["Success"] = "Check-in updated successfully.";
            TempData["Code"] = "S002CI";
            return RedirectToAction("ViewAll", "CheckIn");
        }

        /// <summary>
        /// Displays a list of all check-ins, optionally showing archived (inactive) records.
        /// Accessible by Admin and Driver roles.
        /// </summary>
        /// <param name="viewArchived">
        /// When <c>true</c>, returns only inactive (archived) check-ins.
        /// When <c>false</c> (default), returns only active check-ins.
        /// </param>
        /// <returns>The CheckInTable view populated with the filtered list of check-ins.</returns>
        [HttpGet]
        [Authorize(Roles = "Admin,Driver")]
        public ActionResult ViewAll(bool viewArchived = false)
        {
            // Clear any lingering TempData from previous actions (e.g. success/error messages from EditCheckIn).
            TempData.Clear();

            _logger.LogInformation("ViewAll check-ins requested. viewArchived: {ViewArchived}", viewArchived);

            // Query check-ins with their related location data, filtered by active/archived status.
            // Include() ensures Location and DropOffLocation are eagerly loaded to avoid N+1 queries in the view.
            var checkins = _context.CheckIns
                .Include(c => c.Location)
                .Include(c => c.DropOffLocation)
                .Where(c => c.IsActive == !viewArchived) // Active records when viewArchived=false; inactive when true.
                .ToList();

            // Pass the current archive filter state to the view to toggle UI elements (e.g. active tab, button labels).
            ViewData["Archives"] = viewArchived;

            _logger.LogInformation("ViewAll returned {CheckInCount} check-ins. viewArchived: {ViewArchived}", checkins.Count, viewArchived);

            return View("CheckInTable", checkins);
        }

        /// <summary>
        /// Toggles the active/archived state of a check-in (active → archived, or archived → active).
        /// Restricted to Admin role only.
        /// </summary>
        /// <param name="checkInId">The ID of the check-in whose active state will be toggled.</param>
        /// <returns>
        /// Redirects to ViewAll on success, or to the Dashboard Index on an unexpected error.
        /// Returns a failure result if the check-in record cannot be found.
        /// </returns>
        [Authorize(Roles = "Admin")] // DEV NOTE: Admin-only operation that toggles check-in active state.
        [HttpPost]                   // DEV NOTE: Data modification endpoints should use POST instead of GET.
        [ValidateAntiForgeryToken]
        public IActionResult ToggleCheckInActive(int checkInId)
        {
            _logger.LogInformation("ToggleCheckInActive requested for CheckInId: {CheckInId}", checkInId);

            try
            {
                // Retrieve the check-in record to be toggled.
                CheckIn existingCheckIn = _checkInService.GetEntityById(checkInId);

                // Abort if the record does not exist — cannot toggle a non-existent check-in.
                if (existingCheckIn == null)
                {
                    _logger.LogWarning("ToggleCheckInActive failed. Check-in not found for CheckInId: {CheckInId}", checkInId);
                    return FailedCheckIn("Check-in could not be found.");
                }

                // Flip the active state: active check-ins become archived, archived become active.
                existingCheckIn.IsActive = !existingCheckIn.IsActive;

                // Persist the updated state to the data store.
                _checkInService.UpdateEntity(existingCheckIn);

                _logger.LogInformation(
                    "ToggleCheckInActive succeeded for CheckInId: {CheckInId}. New IsActive: {IsActive}",
                    checkInId, existingCheckIn.IsActive);

                return RedirectToAction("ViewAll");
            }
            catch (Exception exception)
            {
                // DEV NOTE: Logging and SQL exception capture should remain centralized.
                // Delegates structured SQL exception logging to the shared LogEvents utility
                // to keep exception handling consistent across the application.
                LogEvents.LogSqlException(exception, _environment);

                _logger.LogError(exception,
                    "Error toggling check-in active status for CheckInId {CheckInId}",
                    checkInId);

                // Surface a generic error message to the user — avoid exposing internal exception details.
                TempData["ErrorMessage"] = "An unexpected error occurred while updating the check-in.";

                // Redirect to the Dashboard rather than staying on the check-in page, as the list state is now uncertain.
                return RedirectToAction("Index", "Dashboard");
            }
        }

        /// <summary>
        /// Restores an archived check-in by setting its active state to <c>true</c>.
        /// Restricted to Admin role only.
        /// </summary>
        /// <param name="id">The ID of the archived check-in to restore.</param>
        /// <returns>
        /// Redirects to the archived ViewAll list on success, or returns a 404 Not Found if the record does not exist.
        /// </returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Unarchive(int id)
        {
            _logger.LogInformation("Unarchive requested for CheckInId: {CheckInId}", id);

            // Look up the check-in directly via the DbContext rather than the service layer.
            // Note: Unlike ToggleCheckInActive, this method writes directly through _context — consider aligning
            // with _checkInService.UpdateEntity() for consistency if the service layer adds validation logic later.
            var checkin = _context.CheckIns.Find(id);

            // Return 404 if the record does not exist — no check-in to restore.
            if (checkin == null)
            {
                _logger.LogWarning("Unarchive failed. Check-in not found for CheckInId: {CheckInId}", id);
                return NotFound();
            }

            // Explicitly set IsActive to true rather than toggling, since this action is unarchive-only.
            // For a bidirectional toggle, see ToggleCheckInActive.
            checkin.IsActive = true;

            // Persist the restored state directly through the DbContext.
            _context.SaveChanges();

            _logger.LogInformation("Unarchive succeeded for CheckInId: {CheckInId}", id);

            // Redirect back to the archived list so the user can continue managing other archived check-ins.
            return RedirectToAction("ViewAll", new { viewArchived = true });
        }

        [AllowAnonymous] // DEV NOTE: Shared error view for failed check-in operations.
        [HttpGet]
        public IActionResult FailedCheckIn(string errorMessage)
        {
            _logger.LogWarning("FailedCheckIn view returned with error message: {ErrorMessage}", errorMessage);

            ViewBag.ErrorMessage = errorMessage;
            return View("FailedCheckIn");
        }

        /// <summary>
        /// Builds a list of <see cref="SelectListItem"/> entries from all active locations,
        /// for use in check-in form dropdowns.
        /// </summary>
        /// <returns>
        /// A list of <see cref="SelectListItem"/> where each entry represents an active location,
        /// with <c>Text</c> set to the location name and <c>Value</c> set to the location ID.
        /// </returns>
        /// <remarks>
        /// DEV NOTE: This is a controller-level helper scoped to check-in forms. If multiple controllers
        /// require this logic, it should be moved into LocationService as something like GetLocationSelectList().
        /// </remarks>
        private List<SelectListItem> GetLocationOptions()
        {
            _logger.LogInformation("Loading active location options for check-in dropdown.");

            // Filter to active locations only — inactive locations should not appear as selectable options.
            var locations = _context.Locations.Where(l => l.IsActive);

            // Project each location entity into a SelectListItem for use in dropdown rendering.
            // Value is cast to string as SelectListItem.Value requires a string type.
            return locations.Select(location => new SelectListItem
            {
                Text = location.Name,
                Value = location.LocationId.ToString()
            }).ToList();
        }
    }
}