using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MidStateShuttleService.Enums;
using MidStateShuttleService.Helpers;
using MidStateShuttleService.Models;
using MidStateShuttleService.Service;

namespace MidStateShuttleService.Controllers
{
    public class RoutesController : Controller
    {
        private readonly ILogger<RoutesController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        // Inject ApplicationDbContext into the controller constructor
        public RoutesController(
            ApplicationDbContext context,
            ILogger<RoutesController> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;

            _logger.LogInformation("RoutesController initialized.");
        }

        // GET: RoutesController
        public ActionResult Index()
        {
            _logger.LogInformation("Routes Index action called.");
            return View();
        }

        // GET: RoutesController/Details/5
        public ActionResult Details(int id)
        {
            _logger.LogInformation("Routes Details action called for RouteId: {RouteId}", id);
            return View();
        }

        // GET: RoutesController/Create
        [Authorize(Roles = "Admin")]
        public ActionResult Create()
        {
            _logger.LogInformation("Routes Create GET action called.");
            LoadRouteDropdowns();
            return View();
        }

        // POST: RoutesController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Create(Routes route)
        {
            _logger.LogInformation("Routes Create POST action called.");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Routes Create POST failed ModelState validation.");
                LoadRouteDropdowns();
                return View(route);
            }

            try
            {
                RouteServices rs = new RouteServices(_context);
                route.IsActive = true;
                route.Enabled = false; 
                rs.AddEntity(route);

                _logger.LogInformation("Route created successfully.");

                HttpContext.Session.SetString("RouteSuccess", "true");
                TempData["RouteSuccess"] = true;

                return RedirectToAction(nameof(Create));
            }
            catch (Exception ex)
            {
                LogEvents.LogSqlException(ex, _environment);
                _logger.LogError(ex, "An error occurred while creating the route.");
                LoadRouteDropdowns();
                ModelState.AddModelError("", "An unexpected error occurred while creating the route.");
                return View(route);
            }
        }

        /// <summary>
        /// Creates a new route pre-populated from an existing ride's location and timing data.
        /// Intended as a shortcut to convert a custom ride submission into a reusable scheduled route.
        /// Restricted to Admin role only.
        /// </summary>
        [Authorize(Roles = "Admin")]
        public ActionResult CreateFromRide(int rideId)
        {
            _logger.LogInformation("CreateFromRide action called for RideId: {RideId}", rideId);

            Ride ride = _context.Rides.Where(r => r.RideId == rideId).FirstOrDefault();
            RequestDay rDay = _context.RequestDays.Where(d => d.RequestDayId == ride.RequestDayId).FirstOrDefault();
            WeekDay dayOfWeek = rDay.WeekDay;

            // DEV NOTE: This null check fires after ride is already dereferenced above (ride.RequestDayId).
            // If ride is null, a NullReferenceException will be thrown before this point is reached.
            // Move this check to immediately after the ride lookup to make the fallback effective.
            if (ride == null)
            {
                _logger.LogWarning("CreateFromRide could not find RideId: {RideId}", rideId);
                RedirectToAction(nameof(Create));
            }

            // Build a new route from the ride's pick-up and drop-off locations.
            // Drop-off time defaults to 30 minutes after the ride's drop-off time as a starting estimate.
            Routes route = new Routes();
            route.IsActive = true;
            route.PickUpTime = ride.DropOffTime.Value.ToTimeSpan();
            route.DropOffTime = route.PickUpTime.Value.Add(TimeSpan.FromMinutes(30));
            route.DropOffLocationID = ride.DropOffLocationID;
            route.PickUpLocationID = ride.PickUpLocationID;
            route.DayOfWeek = dayOfWeek;

            try
            {
                RouteServices rs = new RouteServices(_context);

                // DEV NOTE: IsActive is set twice — once above and once here. The second assignment is redundant.
                route.IsActive = true;

                rs.AddEntity(route);

                _logger.LogInformation("Route created successfully from RideId: {RideId}", rideId);

                // Set success state in both Session and TempData to cover session-based and redirect-based UI feedback.
                HttpContext.Session.SetString("RouteSuccess", "true");
                TempData["RouteSuccess"] = true;

                return RedirectToAction(nameof(Create));
            }
            catch (Exception ex)
            {
                LogEvents.LogSqlException(ex, _environment);
                _logger.LogError(ex, "An error occurred while creating the route.");

                // Re-populate dropdowns and surface a generic error — the route view requires dropdown data to render.
                LoadRouteDropdowns();
                ModelState.AddModelError("", "An unexpected error occurred while creating the route.");
                return View(route);
            }
        }

        /// <summary>
        /// Displays the edit form for an existing route. Restricted to Admin role only.
        /// </summary>
        // GET: RoutesController/Edit/5
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(int id)
        {
            _logger.LogInformation("Routes Edit GET action called for RouteId: {RouteId}", id);

            var route = _context.Routes.Find(id);

            if (route == null)
            {
                _logger.LogWarning("Routes Edit GET could not find RouteId: {RouteId}", id);
                return NotFound();
            }

            LoadRouteDropdowns();
            return View(route);
        }

        /// <summary>
        /// Handles submission of the route edit form and persists the updated route record.
        /// Restricted to Admin role only.
        /// </summary>
        // POST: RoutesController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(int id, Routes updatedRoute)
        {
            _logger.LogInformation("Routes Edit POST action called for RouteId: {RouteId}", id);

            // Guard against form tampering by verifying the route ID matches the submitted model ID.
            if (id != updatedRoute.RouteID)
            {
                _logger.LogWarning("Routes Edit POST received mismatched RouteId. UrlId: {UrlId}, ModelId: {ModelId}", id, updatedRoute.RouteID);
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Routes Edit POST failed ModelState validation for RouteId: {RouteId}", id);

                // Re-populate dropdowns since ViewBag data is not persisted across POST requests.
                LoadRouteDropdowns();
                return View(updatedRoute);
            }

            try
            {
                // Editing a route always marks it as active — editing an archived route implicitly restores it.
                updatedRoute.IsActive = true;

                _context.Update(updatedRoute);
                _context.SaveChanges();

                _logger.LogInformation("Route updated successfully for RouteId: {RouteId}", updatedRoute.RouteID);

                // Set success state in both Session and TempData to cover session-based and redirect-based UI feedback.
                HttpContext.Session.SetString("RouteSuccess", "true");
                TempData["RouteSuccess"] = true;
                TempData["SuccessMessage"] = "The route has been successfully updated!";

                return RedirectToAction(nameof(Edit), new { id = updatedRoute.RouteID });
            }
            catch (Exception ex)
            {
                // DEV NOTE: Unlike most other edit actions in this codebase that return the form view on failure,
                // this catch block redirects to the Dashboard. The unsaved changes and any model errors are lost.
                // Consider returning View(updatedRoute) with a model error for consistency.
                LogEvents.LogSqlException(ex, _environment);
                _logger.LogError(ex, "An error occurred while updating the route.");
                return RedirectToAction("Index", "Dashboard");
            }
        }

        /// <summary>
        /// Displays a list of all routes, optionally showing archived (inactive) records.
        /// Accessible by Admin and Driver roles.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Driver")]
        public ActionResult ViewAll(bool viewArchived = false)
        {
            _logger.LogInformation("Routes ViewAll action called. ViewArchived: {ViewArchived}", viewArchived);

            // Eagerly load pick-up and drop-off locations to avoid N+1 queries when rendering the route table.
            var routes = _context.Routes
                .Include(r => r.PickUpLocation)
                .Include(r => r.DropOffLocation)
                .Where(r => r.IsActive == !viewArchived)
                .ToList();

            // Pass the current archive filter state to the view to drive UI elements such as tab state or button labels.
            ViewData["Archives"] = viewArchived;

            _logger.LogInformation("Routes ViewAll returning {RouteCount} routes.", routes.Count);

            return View("RouteTable", routes);
        }

        /// <summary>
        /// Displays the public-facing schedule table showing all routes that are both active and enabled.
        /// </summary>
        [HttpGet]
        public ActionResult ViewScheduleTable()
        {
            _logger.LogInformation("Routes ViewScheduleTable action called.");

            // Filter to routes that are both active (not archived) and enabled (visible on the schedule).
            // A route can be active but disabled to temporarily hide it from the public schedule without archiving it.
            var routes = _context.Routes
                .Include(r => r.PickUpLocation)
                .Include(r => r.DropOffLocation)
                .Where(r => r.IsActive && r.Enabled)
                .ToList();

            _logger.LogInformation("Routes ViewScheduleTable returning {RouteCount} routes.", routes.Count);

            return View("ScheduleTable", routes);
        }

        /// <summary>
        /// Toggles the "Enabled" bool, this is different than the one used to track if its archived
        /// </summary>
        [Authorize(Roles = "Admin")]
        public ActionResult ToggleVisibility(int id)
        {
            _logger.LogInformation("Routes ToggleVisibility GET action called for RouteId: {RouteId}", id);

            try
            {
                var route = _context.Routes.Find(id);

                if (route != null)
                {
                    route.Enabled = !route.Enabled;
                    _context.SaveChanges();

                    _logger.LogInformation("Route Visibility toggled successfully for RouteId: {RouteId}", id);
                }
                else
                {
                    // Handle the case where the route with the specified id is not found
                    _logger.LogWarning("Routes ToggleVisibility could not find RouteId: {RouteId}", id);
                    ModelState.AddModelError("", "Route not found.");
                    return View();
                }

                return RedirectToAction("ViewAll");
            }
            catch (Exception ex)
            {
                // Log the exception
                LogEvents.LogSqlException(ex, (IWebHostEnvironment)_context);
                _logger.LogError(ex, "An error occurred while toggling Visilbity of the route.");

                // Optionally add a model error for displaying an error message to the user
                ModelState.AddModelError("", "An unexpected error occurred while toggling Visibility of the route, please try again.");

                // Return the view with an error message
                return View();
            }

            return RedirectToAction("Index", "Dashboard");
        }

        // GET: RoutesController/Delete/5
        [Authorize(Roles = "Admin")]
        public ActionResult Delete(int id)
        {
            _logger.LogInformation("Routes Delete GET action called for RouteId: {RouteId}", id);

            try
            {
                var route = _context.Routes.Find(id);

                if (route != null)
                {
                    route.IsActive = !route.IsActive; // Toggle IsActive from true to false or false to true
                    _context.SaveChanges();

                    _logger.LogInformation("Route IsActive toggled successfully for RouteId: {RouteId}", id);
                }
                else
                {
                    // Handle the case where the route with the specified id is not found
                    _logger.LogWarning("Routes Delete GET could not find RouteId: {RouteId}", id);
                    ModelState.AddModelError("", "Route not found.");
                    return View();
                }

                return RedirectToAction("ViewAll");
            }
            catch (Exception ex)
            {
                // Log the exception
                LogEvents.LogSqlException(ex, (IWebHostEnvironment)_context);
                _logger.LogError(ex, "An error occurred while toggling IsActive of the route.");

                // Optionally add a model error for displaying an error message to the user
                ModelState.AddModelError("", "An unexpected error occurred while toggling IsActive of the route, please try again.");

                // Return the view with an error message
                return View();
            }

            return RedirectToAction("Index", "Dashboard");
        }


        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadRoutesExcel(IFormFile excelFile)
        {
            _logger.LogInformation("Excel route upload started.");

            // Basic file validation
            if (excelFile == null || excelFile.Length == 0)
            {
                _logger.LogWarning("No file provided for route upload.");
                TempData["ErrorMessage"] = "Please select a valid Excel file.";
                return RedirectToAction("ViewAll");
            }

            // Only allow .xlsx
            var extension = Path.GetExtension(excelFile.FileName);
            if (!extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Invalid file type uploaded: {FileName}", excelFile.FileName);
                TempData["ErrorMessage"] = "Only .xlsx Excel files are supported.";
                return RedirectToAction("ViewAll");
            }

            try
            {
                using var stream = new MemoryStream();
                await excelFile.CopyToAsync(stream);
                stream.Position = 0;

                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);

                // Skip header row
                var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

                // Cache active locations
                var locations = _context.Locations
                    .Where(l => l.IsActive)
                    .ToList();

                // Only check duplicates against active routes
                var activeRoutes = _context.Routes
                    .Where(r => r.IsActive)
                    .ToList();

                int createdCount = 0;
                int skippedCount = 0;
                int duplicateCount = 0;

                foreach (var row in rows)
                {
                    // B = DEP, C = START, D = END, E = ARRIVAL
                    var depValue = row.Cell(2).GetString().Trim();
                    var startValue = row.Cell(3).GetString().Trim();
                    var endValue = row.Cell(4).GetString().Trim();
                    var arrivalValue = row.Cell(5).GetString().Trim();

                    // Skip incomplete rows
                    if (string.IsNullOrWhiteSpace(depValue) ||
                        string.IsNullOrWhiteSpace(startValue) ||
                        string.IsNullOrWhiteSpace(endValue) ||
                        string.IsNullOrWhiteSpace(arrivalValue))
                    {
                        _logger.LogWarning("Skipping Excel row {RowNumber}: missing required fields.", row.RowNumber());
                        skippedCount++;
                        continue;
                    }

                    // Match locations by abbreviation
                    var pickupLocation = locations.FirstOrDefault(l =>
                        l.Abbreviation.Equals(startValue, StringComparison.OrdinalIgnoreCase));

                    var dropoffLocation = locations.FirstOrDefault(l =>
                        l.Abbreviation.Equals(endValue, StringComparison.OrdinalIgnoreCase));

                    if (pickupLocation == null || dropoffLocation == null)
                    {
                        _logger.LogWarning(
                            "Skipping Excel row {RowNumber}: invalid location. START: {Start}, END: {End}",
                            row.RowNumber(),
                            startValue,
                            endValue);

                        skippedCount++;
                        continue;
                    }

                    // Parse times (stored as Central — DO NOT convert)
                    if (!TimeSpan.TryParse(depValue, out var pickupTime) ||
                        !TimeSpan.TryParse(arrivalValue, out var dropoffTime))
                    {
                        _logger.LogWarning(
                            "Skipping Excel row {RowNumber}: invalid time format. DEP: {DEP}, ARRIVAL: {ARRIVAL}",
                            row.RowNumber(),
                            depValue,
                            arrivalValue);

                        skippedCount++;
                        continue;
                    }

                    // DEV NOTE:
                    // Create the same route for every weekday.
                    // The Excel sheet only gives the route details, not the day,
                    // so each valid row becomes Monday through Friday routes.
                    var weekdays = new[]
                    {
    WeekDay.Monday,
    WeekDay.Tuesday,
    WeekDay.Wednesday,
    WeekDay.Thursday,
    WeekDay.Friday
};

                    foreach (var dayOfWeek in weekdays)
                    {
                        // Duplicate check (active only)
                        var duplicateExists = activeRoutes.Any(r =>
                            r.PickUpLocationID == pickupLocation.LocationId &&
                            r.DropOffLocationID == dropoffLocation.LocationId &&
                            r.PickUpTime == pickupTime &&
                            r.DropOffTime == dropoffTime &&
                            r.DayOfWeek == dayOfWeek);

                        if (duplicateExists)
                        {
                            _logger.LogInformation(
                                "Skipping duplicate route row {RowNumber} for {DayOfWeek}. START: {Start}, END: {End}",
                                row.RowNumber(),
                                dayOfWeek,
                                startValue,
                                endValue);

                            duplicateCount++;
                            continue;
                        }

                        // Create ONE route per weekday for this Excel row.
                        var route = new Routes
                        {
                            PickUpLocationID = pickupLocation.LocationId,
                            DropOffLocationID = dropoffLocation.LocationId,
                            PickUpTime = pickupTime,
                            DropOffTime = dropoffTime,
                            DayOfWeek = dayOfWeek,
                            IsActive = true
                        };

                        _context.Routes.Add(route);
                        activeRoutes.Add(route);
                        createdCount++;
                    }
                }

                    await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Excel route upload complete. Created: {Created}, Skipped: {Skipped}, Duplicates: {Duplicates}",
                    createdCount,
                    skippedCount,
                    duplicateCount);

                TempData["SuccessMessage"] =
                    $"{createdCount} routes created. {skippedCount} rows skipped. {duplicateCount} duplicates ignored.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Excel route upload.");
                TempData["ErrorMessage"] = "An error occurred while processing the file.";
            }

            return RedirectToAction("ViewAll");
        }


        /// <summary>
        /// Toggles the active/archived state of a route (active → archived, or archived → active).
        /// Functionally acts as a soft delete for active routes. Restricted to Admin role only.
        /// </summary>
        // POST: RoutesController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            _logger.LogInformation("Routes Delete POST action called for RouteId: {RouteId}", id);

            try
            {
                var route = _context.Routes.Find(id);

                if (route == null)
                {
                    _logger.LogWarning("Routes Delete POST could not find RouteId: {RouteId}", id);
                    TempData["ErrorMessage"] = "Route not found.";
                    return RedirectToAction("ViewAll");
                }

                // Flip the active state — active routes become archived, archived routes become active.
                // Despite the action being named "Delete", no data is permanently removed.
                route.IsActive = !route.IsActive;
                _context.SaveChanges();

                _logger.LogInformation("Route IsActive toggled successfully for RouteId: {RouteId}", id);

                return RedirectToAction("ViewAll");
            }
            catch (Exception ex)
            {
                LogEvents.LogSqlException(ex, _environment);
                _logger.LogError(ex, "An error occurred while toggling IsActive of the route.");
                TempData["ErrorMessage"] = "An unexpected error occurred while updating the route.";
                return RedirectToAction("ViewAll");
            }
            catch
            {
                // DEV NOTE: This bare catch block is unreachable — the catch (Exception ex) block above
                // already catches all exceptions including non-CLS-compliant ones in the .NET runtime.
                // This block should be removed, or the two catches should be merged.
                _logger.LogError("An unknown error occurred in Routes Delete POST for RouteId: {RouteId}", id);
                return View();
            }
        }

        /// <summary>
        /// Restores an archived route by setting it as active, while explicitly disabling it
        /// so it does not immediately appear on the public schedule until reviewed.
        /// Restricted to Admin role only.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Unarchive(int id)
        {
            _logger.LogInformation("Routes Unarchive action called for RouteId: {RouteId}", id);

            var route = _context.Routes.Find(id);

            if (route == null)
            {
                _logger.LogWarning("Routes Unarchive could not find RouteId: {RouteId}", id);
                return NotFound();
            }

            // Restore the route to active but leave it disabled so an admin can review it
            // before it reappears on the public-facing schedule. See ViewScheduleTable for the IsActive + Enabled filter.
            route.IsActive = true;
            route.Enabled = false;
            _context.SaveChanges();

            _logger.LogInformation("Route unarchived successfully for RouteId: {RouteId}", id);

            return RedirectToAction("ViewAll", new { viewArchived = true });
        }

        /// <summary>
        /// Returns a JSON array of active, enabled routes matching the given pick-up location,
        /// drop-off location, and day of week. Intended to be called from client-side JavaScript
        /// to dynamically populate route options in the registration form.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetRoutes(int pickupId, int dropoffId, int dayOfWeek)
        {
            _logger.LogInformation("Routes GetRoutes action called. PickupId: {PickupId}, DropoffId: {DropoffId}, DayOfWeek: {DayOfWeek}", pickupId, dropoffId, dayOfWeek);

            // Cast the integer day of week to the WeekDay enum for the EF query filter.
            WeekDay weekDay = (WeekDay)dayOfWeek;

            // Filter to routes matching all three criteria and visible on the public schedule.
            // Both IsActive and Enabled must be true — see ViewScheduleTable for the same filter pattern.
            var routes = await _context.Routes
                .Where(r =>
                    r.PickUpLocationID == pickupId &&
                    r.DropOffLocationID == dropoffId &&
                    r.DayOfWeek == weekDay &&
                    r.IsActive == true &&
                    r.Enabled == true)
                .ToListAsync();

            // Project to an anonymous type with only the fields needed by the client.
            // Times are formatted via FormatTime() for consistent display in the dropdown.
            var result = routes.Select(r => new
            {
                id = r.RouteID,
                pickupTime = FormatTime(r.PickUpTime),
                dropoffTime = FormatTime(r.DropOffTime)
            });

            _logger.LogInformation("Routes GetRoutes returning {RouteCount} matching routes.", routes.Count);

            return Json(result);
        }

        private static string FormatTime(TimeSpan? time)
        {
            if (time == null)
                return "";

            return DateTime.Today.Add(time.Value).ToString("h:mm tt");
        }

        // Helper method used by Create/Edit views to populate dropdown lists
        private void LoadRouteDropdowns()
        {
            _logger.LogInformation("LoadRouteDropdowns called.");

            // Load all ACTIVE locations for the pickup/drop-off dropdowns
            LocationServices ls = new LocationServices(_context);
            ViewBag.Locations = ls.GetAllEntities()
                .Where(location => location.IsActive)
                .Select(location => new SelectListItem
                {
                    Text = location.Name,                    // Location name shown to user
                    Value = location.LocationId.ToString()   // Location ID submitted with form
                });

            // Load drivers so a route can be assigned to one
            DriverServices ds = new DriverServices(_context);
            ViewBag.Drivers = ds.GetAllEntities()
                .Select(driver => new SelectListItem
                {
                    Text = driver.Name,                 // Driver name shown in dropdown
                    Value = driver.DriverId.ToString()  // Driver ID submitted
                });

            // Load buses/shuttles for route assignment
            BusServices bs = new BusServices(_context);
            ViewBag.Buses = bs.GetAllEntities()
                .Select(bus => new SelectListItem
                {
                    Text = "Shuttle: " + bus.BusNo,     // Label shown in dropdown
                    Value = bus.BusId.ToString()        // Bus ID submitted
                });

            _logger.LogInformation("LoadRouteDropdowns completed.");
        }
    }
}