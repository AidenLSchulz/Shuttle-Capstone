using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MidStateShuttleService.Models;
using MidStateShuttleService.Service;

namespace MidStateShuttleService.Controllers
{
    public class LocationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LocationController> _logger;

        // DEV NOTE:
        // Keep constructor injection simple and consistent with the rest of the project.
        public LocationController(ApplicationDbContext context, ILogger<LocationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: LocationController
        public ActionResult Index()
        {
            _logger.LogInformation("Location Index accessed.");
            return View();
        }

        // GET: LocationController/Create
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public ActionResult Create()
        {
            _logger.LogInformation("Location Create page accessed.");
            return View();
        }

        /// <summary>
        /// Handles submission of the location creation form and persists the new location record.
        /// Restricted to Admin role only.
        /// </summary>
        /// <param name="location">The location model populated from the submitted form.</param>
        /// <returns>
        /// Redirects back to the Create page on success, returns the form with validation errors
        /// on invalid input, or returns the form with a model error on an unhandled exception.
        /// </returns>
        // POST: LocationController/Create
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Location location)
        {
            _logger.LogInformation("Location Create POST received for Name: {LocationName}", location?.Name);

            // Validate the submitted form data before attempting to persist.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Location Create failed validation for Name: {LocationName}", location?.Name);
                return View(location);
            }

            try
            {
                LocationServices locationServices = new LocationServices(_context);

                // All newly created locations are marked active by default.
                location.IsActive = true;

                locationServices.AddEntity(location);

                _logger.LogInformation("Location created successfully for LocationId: {LocationId}", location.LocationId);

                // Set success state in both TempData and Session to cover redirect-based and session-based UI feedback.
                TempData["SuccessMessage"] = "The location has been successfully created!";
                HttpContext.Session.SetString("LocationSuccess", "true");
                TempData["LocationSuccess"] = true;

                return RedirectToAction(nameof(Create));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "An error occurred while creating location.");

                // DEV NOTE: Surfaces the underlying exception detail directly in the model error.
                // This is acceptable during development but should be replaced with a generic message
                // before production to avoid exposing internal error details to admin users.
                var actualError = exception.InnerException?.Message ?? exception.Message;
                ModelState.AddModelError("", actualError);

                return View(location);
            }
        }

        /// <summary>
        /// Displays the edit form for an existing location. Restricted to Admin role only.
        /// </summary>
        /// <param name="id">The ID of the location to edit.</param>
        /// <returns>
        /// The Edit view populated with the location's current data, a failure result if the
        /// location does not exist, or a failure result if an exception occurs during retrieval.
        /// </returns>
        // GET: LocationController/Edit/5
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public ActionResult Edit(int id)
        {
            _logger.LogInformation("Location Edit GET requested for LocationId: {LocationId}", id);

            try
            {
                LocationServices locationServices = new LocationServices(_context);
                Location location = locationServices.GetEntityById(id);

                // Return a failure result if no matching location record was found.
                if (location == null)
                {
                    _logger.LogWarning("Location Edit GET failed. Location not found for LocationId: {LocationId}", id);
                    return FailedLocation("Location Not Found");
                }

                return View(location);
            }
            catch (Exception exception)
            {
                // Any failure during retrieval lands here — return a failure result rather than an unhandled error page.
                _logger.LogError(exception, "An error occurred while loading location {LocationId} for edit.", id);
                return FailedLocation("Location could not be loaded");
            }
        }

        /// <summary>
        /// Handles submission of the location edit form and persists the updated location record.
        /// Restricted to Admin role only.
        /// </summary>
        /// <param name="model">The location model populated from the submitted form.</param>
        /// <returns>
        /// Redirects back to the Edit page on success, returns the form with validation errors on
        /// invalid input, or returns a failure result if the location cannot be found or an exception occurs.
        /// </returns>
        // POST: LocationController/Edit/5
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Location model)
        {
            _logger.LogInformation("Location Edit POST received for LocationId: {LocationId}", model?.LocationId);

            // Guard against a null model — should not occur under normal form submission but handled defensively.
            if (model == null)
            {
                _logger.LogWarning("Location Edit POST failed. Model was null.");
                return FailedLocation("Updates to location could not be applied");
            }

            // Validate the submitted form data before attempting to persist.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Location Edit POST failed validation for LocationId: {LocationId}", model.LocationId);
                return View(model);
            }

            try
            {
                LocationServices locationServices = new LocationServices(_context);

                // Re-fetch the existing record to apply updates against rather than replacing the entity directly.
                Location existingLocation = locationServices.GetEntityById(model.LocationId);

                // Return a failure result if the location no longer exists (e.g. deleted between GET and POST).
                if (existingLocation == null)
                {
                    _logger.LogWarning("Location Edit POST failed. Location not found for LocationId: {LocationId}", model.LocationId);
                    return FailedLocation("Location Not Found");
                }

                // Apply only the editable fields from the submitted model.
                // IsActive is intentionally excluded — editing a location must not reactivate an archived one.
                existingLocation.Name = model.Name;
                existingLocation.Address = model.Address;
                existingLocation.City = model.City;
                existingLocation.State = model.State;
                existingLocation.ZipCode = model.ZipCode;
                existingLocation.Abbreviation = model.Abbreviation;

                locationServices.UpdateEntity(existingLocation);

                _logger.LogInformation("Location updated successfully for LocationId: {LocationId}", existingLocation.LocationId);

                // Set success state in both TempData and Session to cover redirect-based and session-based UI feedback.
                HttpContext.Session.SetString("LocationSuccess", "true");
                TempData["LocationSuccess"] = true;

                return RedirectToAction(nameof(Edit), new { id = existingLocation.LocationId });
            }
            catch (Exception exception)
            {
                // Any failure during update lands here — return a failure result rather than an unhandled error page.
                _logger.LogError(exception, "An error occurred while updating location {LocationId}.", model.LocationId);
                return FailedLocation("Updates to location could not be applied");
            }
        }

        /// <summary>
        /// Displays a list of all locations, optionally showing archived (inactive) records.
        /// Accessible by Admin and Driver roles.
        /// </summary>
        /// <param name="viewArchived">
        /// When <c>true</c>, returns only inactive (archived) locations.
        /// When <c>false</c> (default), returns only active locations.
        /// </param>
        /// <returns>The LocationTable view populated with the filtered list of locations.</returns>
        [HttpGet]
        [Authorize(Roles = "Admin,Driver")]
        public ActionResult ViewAll(bool viewArchived = false)
        {
            _logger.LogInformation("Location ViewAll requested. ViewArchived: {ViewArchived}", viewArchived);

            // Filter locations by active/archived status.
            // Active records are returned by default; archived records when viewArchived=true.
            var locations = _context.Locations
                .Where(l => l.IsActive == !viewArchived)
                .ToList();

            _logger.LogInformation("Location ViewAll returned {LocationCount} records.", locations.Count);

            // Pass the current archive filter state to the view to drive UI elements such as tab state or button labels.
            ViewData["Archives"] = viewArchived;

            return View("LocationTable", locations);
        }

        /// <summary>
        /// Toggles the active/archived state of a location (active → archived, or archived → active).
        /// Functionally acts as a soft delete for active locations. Restricted to Admin role only.
        /// </summary>
        /// <param name="id">The ID of the location whose active state will be toggled.</param>
        /// <returns>
        /// Redirects to ViewAll on success, or returns a failure result if the location cannot
        /// be found or an exception occurs.
        /// </returns>
        /// <remarks>
        /// DEV NOTE: There are two consecutive return statements at the end of the success path —
        /// the second RedirectToAction("Index", "Dashboard") is unreachable and should be removed.
        /// </remarks>
        // POST: LocationController/DeleteLocation/5
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteLocation(int id)
        {
            _logger.LogInformation("Location Delete toggle requested for LocationId: {LocationId}", id);

            try
            {
                LocationServices locationServices = new LocationServices(_context);
                Location location = locationServices.GetEntityById(id);

                // Return a failure result if no matching location record was found.
                if (location == null)
                {
                    _logger.LogWarning("Location Delete failed. Location not found for LocationId: {LocationId}", id);
                    return FailedLocation("Location Not Found");
                }

                // Flip the active state — active locations become archived, archived locations become active.
                // Despite the action being named "Delete", no data is permanently removed.
                location.IsActive = !location.IsActive;
                locationServices.UpdateEntity(location);

                _logger.LogInformation(
                    "Location IsActive toggled successfully for LocationId: {LocationId}. New IsActive: {IsActive}",
                    id, location.IsActive);

                return RedirectToAction("ViewAll");
            }
            catch (Exception exception)
            {
                // Any failure during the toggle lands here — return a failure result rather than an unhandled error page.
                _logger.LogError(exception, "An error occurred while toggling location {LocationId}.", id);
                return FailedLocation("Updates to location could not be applied");
            }
        }

        /// <summary>
        /// Restores an archived location by explicitly setting its active state to <c>true</c>.
        /// Restricted to Admin role only.
        /// </summary>
        /// <param name="id">The ID of the archived location to restore.</param>
        /// <returns>
        /// Redirects to the archived ViewAll list on success, or returns a 404 Not Found if
        /// the location does not exist.
        /// </returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Unarchive(int id)
        {
            _logger.LogInformation("Location Unarchive requested for LocationId: {LocationId}", id);

            var location = _context.Locations.Find(id);

            // Return 404 if the record does not exist — no location to restore.
            if (location == null)
            {
                _logger.LogWarning("Location Unarchive failed. Location not found for LocationId: {LocationId}", id);
                return NotFound();
            }

            // Explicitly set IsActive to true rather than toggling — this action is unarchive-only.
            // For a bidirectional toggle, see DeleteLocation above.
            location.IsActive = true;
            _context.SaveChanges();

            _logger.LogInformation("Location unarchived successfully for LocationId: {LocationId}", id);

            // Redirect back to the archived list so the admin can continue managing other archived locations.
            return RedirectToAction("ViewAll", new { viewArchived = true });
        }

        /// <summary>
        /// Shared failure handler for location actions. Passes an error message to the
        /// FailedLocation view via <c>ViewBag</c> and returns the error view.
        /// </summary>
        /// <param name="errorMessage">A human-readable description of the failure to display to the user.</param>
        /// <returns>The FailedLocation view populated with the provided error message.</returns>
        /// <remarks>
        /// DEV NOTE: This method has no access modifier specified, which defaults to private in C#.
        /// If it is intended to be callable from other controllers or as an action, it should be
        /// explicitly marked public. If it is internal to this controller only, add private explicitly
        /// for clarity.
        /// </remarks>
        public ActionResult FailedLocation(string errorMessage)
        {
            _logger.LogWarning("FailedLocation triggered with message: {ErrorMessage}", errorMessage);

            // Surface the error message to the view via ViewBag for display to the admin.
            ViewBag.ErrorMessage = errorMessage;

            return View("FailedLocation");
        }
    }
}