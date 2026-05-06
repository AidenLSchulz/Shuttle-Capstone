using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MidStateShuttleService.Models;
using MidStateShuttleService.Service;
using MidStateShuttleService.Services;

namespace MidStateShuttleService.Controllers
{
    public class DriverController : Controller
    {
        private readonly DriverServices _driverService;
        private readonly ILogger<DriverController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly ApplicationDbContext _context;

        public DriverController(
            ApplicationDbContext context,
            DriverServices driverService,
            ILogger<DriverController> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _driverService = driverService;
            _logger = logger;
            _environment = environment;
        }

        [Authorize(Roles = "Admin")] // DEV NOTE: Admin-only list page for driver management.
        [HttpGet]
        public IActionResult Index()
        {
            _logger.LogInformation("Driver index page accessed.");
            return View();
        }

        /// <summary>
        /// Displays the details page for a specific driver. Restricted to Admin role only.
        /// </summary>
        /// <param name="id">The ID of the driver to view.</param>
        /// <returns>
        /// The Details view populated with the driver's data, or a 404 Not Found if the driver does not exist.
        /// </returns>
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Details(int id)
        {
            _logger.LogInformation("Driver details requested for DriverId: {DriverId}", id);

            Driver existingDriver = _driverService.GetEntityById(id);

            // Return 404 if no matching driver record was found.
            if (existingDriver == null)
            {
                _logger.LogWarning("Driver details request failed. Driver not found for DriverId: {DriverId}", id);
                return NotFound();
            }

            return View(existingDriver);
        }

        /// <summary>
        /// Displays the driver creation form. Restricted to Admin role only.
        /// </summary>
        /// <returns>The Create view with an empty driver form.</returns>
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            _logger.LogInformation("Driver create page accessed.");
            return View();
        }

        /// <summary>
        /// Handles submission of the driver creation form and persists the new driver record.
        /// Restricted to Admin role only.
        /// </summary>
        /// <param name="submittedDriver">The driver model populated from the submitted form.</param>
        /// <returns>
        /// Redirects back to the Create page on success, returns the form with validation errors
        /// on invalid input, or returns the form with a model error on an unhandled exception.
        /// </returns>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Driver submittedDriver)
        {
            _logger.LogInformation("Driver create submitted for Name: {DriverName}", submittedDriver?.Name);

            // Validate the submitted form data before attempting to persist.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Driver create failed validation for Name: {DriverName}", submittedDriver?.Name);
                return View(submittedDriver);
            }

            try
            {
                // All newly created drivers are marked active by default.
                submittedDriver.IsActive = true;
                _driverService.AddEntity(submittedDriver);

                _logger.LogInformation("Driver created successfully for DriverId: {DriverId}", submittedDriver.DriverId);

                // Set success state in both TempData and Session to cover redirect-based and session-based UI feedback.
                TempData["SuccessMessage"] = "The driver has been successfully created!";
                HttpContext.Session.SetString("DriverSuccess", "true");
                TempData["DriverSuccess"] = true;

                return RedirectToAction(nameof(Create));
            }
            catch (Exception exception)
            {
                LogEvents.LogSqlException(exception, _environment);
                _logger.LogError(exception, "An error occurred while creating driver.");

                // Surface a generic error to the admin without exposing internal exception details.
                ModelState.AddModelError("", "An unexpected error occurred, please try again.");
                return View(submittedDriver);
            }
        }

        /// <summary>
        /// Displays the edit form for an existing driver. Restricted to Admin role only.
        /// </summary>
        /// <param name="id">The ID of the driver to edit.</param>
        /// <returns>
        /// The Edit view populated with the driver's current data, or a 404 Not Found if the driver does not exist.
        /// </returns>
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            _logger.LogInformation("Driver edit page requested for DriverId: {DriverId}", id);

            Driver existingDriver = _driverService.GetEntityById(id);

            // Return 404 if no matching driver record was found.
            if (existingDriver == null)
            {
                _logger.LogWarning("Driver edit page failed. Driver not found for DriverId: {DriverId}", id);
                return NotFound();
            }

            return View(existingDriver);
        }

        /// <summary>
        /// Handles submission of the driver edit form and persists the updated driver record.
        /// Restricted to Admin role only.
        /// </summary>
        /// <param name="id">The route ID of the driver being edited, used to guard against form tampering.</param>
        /// <param name="submittedDriver">The driver model populated from the submitted form.</param>
        /// <returns>
        /// Redirects back to the Edit page on success, returns BadRequest on an ID mismatch,
        /// returns the form with validation errors on invalid input, or returns the form with
        /// a model error on an unhandled exception.
        /// </returns>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Driver submittedDriver)
        {
            _logger.LogInformation("Driver edit submitted for DriverId: {DriverId}", id);

            // Guard against form tampering by verifying the route ID matches the submitted model ID.
            if (id != submittedDriver.DriverId)
            {
                _logger.LogWarning(
                    "Driver edit failed due to id mismatch. Route id: {RouteId}, Model id: {ModelId}",
                    id, submittedDriver.DriverId);
                return BadRequest();
            }

            // Validate the submitted form data before attempting to persist.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Driver edit failed validation for DriverId: {DriverId}", submittedDriver.DriverId);
                return View(submittedDriver);
            }

            try
            {
                // Editing a driver always marks them as active — editing an archived driver implicitly restores them.
                submittedDriver.IsActive = true;
                _driverService.UpdateEntity(submittedDriver);

                _logger.LogInformation("Driver updated successfully for DriverId: {DriverId}", submittedDriver.DriverId);

                // Set success state in both TempData and Session to cover redirect-based and session-based UI feedback.
                TempData["SuccessMessage"] = "The driver has been successfully updated!";
                HttpContext.Session.SetString("DriverSuccess", "true");
                TempData["DriverSuccess"] = true;

                return RedirectToAction(nameof(Edit), new { id = submittedDriver.DriverId });
            }
            catch (Exception exception)
            {
                LogEvents.LogSqlException(exception, _environment);
                _logger.LogError(exception, "An error occurred while updating driver.");

                // Surface a generic error to the admin without exposing internal exception details.
                ModelState.AddModelError("", "An unexpected error occurred, please try again.");
                return View(submittedDriver);
            }
        }

        /// <summary>
        /// Displays a list of all drivers, optionally showing archived (inactive) records.
        /// Restricted to Admin role only.
        /// </summary>
        /// <param name="viewArchived">
        /// When <c>true</c>, returns only inactive (archived) drivers.
        /// When <c>false</c> (default), returns only active drivers.
        /// </param>
        /// <returns>The DriverTable view populated with the filtered list of drivers.</returns>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public ActionResult ViewAll(bool viewArchived = false)
        {
            _logger.LogInformation("Driver ViewAll requested. ViewArchived: {ViewArchived}", viewArchived);

            // Filter drivers by active/archived status.
            // Active records are returned by default; archived records when viewArchived=true.
            var drivers = _context.Drivers
                .Where(d => d.IsActive == !viewArchived)
                .ToList();

            // Pass the current archive filter state to the view to drive UI elements such as tab state or button labels.
            ViewData["Archives"] = viewArchived;

            _logger.LogInformation("Driver ViewAll returned {DriverCount} records.", drivers.Count);

            return View("DriverTable", drivers);
        }

        /// <summary>
        /// Toggles the active/archived state of a driver (active → archived, or archived → active).
        /// Functionally acts as a soft delete for active drivers. Restricted to Admin role only.
        /// </summary>
        /// <param name="id">The ID of the driver whose active state will be toggled.</param>
        /// <returns>
        /// Redirects to ViewAll on success, or redirects to the Dashboard Index on failure or exception.
        /// </returns>
        /// <remarks>
        /// DEV NOTE: There are two consecutive return statements at the end of the success path —
        /// the second RedirectToAction("Index", "Dashboard") is unreachable and should be removed.
        /// </remarks>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            _logger.LogInformation("Driver delete toggle requested for DriverId: {DriverId}", id);

            try
            {
                Driver existingDriver = _driverService.GetEntityById(id);

                // Redirect to the Dashboard with an error if the driver record cannot be found.
                if (existingDriver == null)
                {
                    _logger.LogWarning("Driver delete toggle failed. Driver not found for DriverId: {DriverId}", id);
                    TempData["ErrorMessage"] = "Driver not found.";
                    return RedirectToAction("Index", "Dashboard");
                }

                // Flip the active state — active drivers become archived, archived drivers become active.
                // Despite the action being named "Delete", no data is permanently removed.
                existingDriver.IsActive = !existingDriver.IsActive;
                _driverService.UpdateEntity(existingDriver);

                _logger.LogInformation(
                    "Driver IsActive toggled successfully for DriverId: {DriverId}. New IsActive: {IsActive}",
                    id, existingDriver.IsActive);

                // DEV NOTE: The line below this redirect is unreachable and should be removed.
                return RedirectToAction("ViewAll");
                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception exception)
            {
                LogEvents.LogSqlException(exception, _environment);
                _logger.LogError(exception, "An error occurred while toggling IsActive of the driver.");

                // Surface a generic error to the admin — avoid exposing internal exception details.
                TempData["ErrorMessage"] = "An unexpected error occurred while updating the driver.";

                return RedirectToAction("Index", "Dashboard");
            }
        }

        /// <summary>
        /// Restores an archived driver by explicitly setting their active state to <c>true</c>.
        /// Restricted to Admin role only.
        /// </summary>
        /// <param name="id">The ID of the archived driver to restore.</param>
        /// <returns>
        /// Redirects to the archived ViewAll list on success, or returns a 404 Not Found if the driver does not exist.
        /// </returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Unarchive(int id)
        {
            _logger.LogInformation("Driver unarchive requested for DriverId: {DriverId}", id);

            var driver = _context.Drivers.Find(id);

            // Return 404 if the record does not exist — no driver to restore.
            if (driver == null)
            {
                _logger.LogWarning("Driver unarchive failed. Driver not found for DriverId: {DriverId}", id);
                return NotFound();
            }

            // Explicitly set IsActive to true rather than toggling — this action is unarchive-only.
            // For a bidirectional toggle, see the Delete action above.
            driver.IsActive = true;
            _context.SaveChanges();

            _logger.LogInformation("Driver unarchived successfully for DriverId: {DriverId}", id);

            // Redirect back to the archived list so the admin can continue managing other archived drivers.
            return RedirectToAction("ViewAll", new { viewArchived = true });
        }
    }
}