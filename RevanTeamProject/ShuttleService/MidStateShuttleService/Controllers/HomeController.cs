using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MidStateShuttleService.Models;
using MidStateShuttleService.Service;
using System.Diagnostics;

namespace MidStateShuttleService.Controllers
{
    /// <summary>
    /// Handles public-facing pages including the home page, testimonial submission,
    /// privacy policy, and error display. All actions are publicly accessible via
    /// [AllowAnonymous] overrides despite the controller-level [Authorize] attribute.
    /// </summary>
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Displays the public home page with approved testimonials and the route schedule.
        /// </summary>
        /// <returns>
        /// The Index view populated with active, approved testimonials ordered by most recent,
        /// and a route schedule loaded into <c>ViewBag.RouteSchedule</c>.
        /// </returns>
        [AllowAnonymous]
        public IActionResult Index()
        {
            _logger.LogInformation("Home Index accessed.");

            // Fetch only active testimonials that have been approved for public display.
            // IsActive = admin approved; DisplayTestimonial = flagged for homepage display.
            // DEV NOTE: The commented-out block above fetched all feedback regardless of approval state — do not restore it.
            var activeFeedbackList = _context.Feedbacks
                .Where(f => f.IsActive && f.DisplayTestimonial)
                .OrderByDescending(f => f.DateSubmitted)
                .ToList();

            _logger.LogInformation("Home Index returning {Count} active testimonials.", activeFeedbackList.Count);

            // Load the route schedule for display on the home page.
            RouteServices rs = new RouteServices(_context);
            ViewBag.RouteSchedule = rs.GetScheduleRoutes();

            _logger.LogInformation("Route schedule loaded for Home Index.");

            return View(activeFeedbackList);
        }

        /// <summary>
        /// Displays the privacy policy page.
        /// </summary>
        [AllowAnonymous]
        public IActionResult Privacy()
        {
            _logger.LogInformation("Privacy page accessed.");
            return View();
        }

        /// <summary>
        /// Displays the error page. Response caching is disabled to ensure error details are always fresh.
        /// </summary>
        /// <returns>
        /// The Error view populated with the current request's trace ID for diagnostics.
        /// </returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [AllowAnonymous]
        public IActionResult Error()
        {
            _logger.LogWarning("Error page triggered.");
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /// <summary>
        /// Handles public submission of a testimonial/feedback form. Persists the entry in a
        /// pending (unapproved) state and increments the admin dashboard notification counter.
        /// Accessible anonymously — no authentication required.
        /// </summary>
        /// <param name="feedback">
        /// The feedback model bound from the form. Only <c>Comment</c>, <c>CustomerName</c>,
        /// and <c>Rating</c> are accepted from the form — all other fields are set server-side.
        /// </param>
        /// <returns>
        /// Redirects to the Index action on success, or re-renders the Index view with the
        /// active testimonial list and route schedule on validation failure or exception.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Create([Bind("Comment,CustomerName,Rating")] Feedback feedback)
        {
            _logger.LogInformation("Home Create POST received. CustomerName: {CustomerName}, Rating: {Rating}", feedback?.CustomerName, feedback?.Rating);

            if (ModelState.IsValid)
            {
                try
                {
                    // Default to "Anonymous" if the submitter leaves the name field blank.
                    feedback.CustomerName = string.IsNullOrWhiteSpace(feedback.CustomerName)
                        ? "Anonymous"
                        : feedback.CustomerName;

                    // Public submissions are held for admin approval before appearing on the site.
                    feedback.IsActive = false;

                    // Display flag is controlled by admin approval — never set to true on public submission.
                    feedback.DisplayTestimonial = false;

                    // Store submission timestamp in UTC for consistent timezone handling across display contexts.
                    feedback.DateSubmitted = DateTime.UtcNow;

                    // Persist the new feedback entry to the database.
                    _context.Add(feedback);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Testimonial successfully saved.");

                    // Signal the Index view to display the submission success modal on redirect.
                    TempData["FeedbackSuccess"] = "True";

                    // Increment the session-tracked feedback count to surface an unread badge on the admin dashboard.
                    int feedbackCount = HttpContext.Session.GetInt32("FeedbackCount") ?? 0;
                    feedbackCount++;
                    HttpContext.Session.SetInt32("FeedbackCount", feedbackCount);
                    HttpContext.Session.SetString("LastFeedback", "You have a new feedback!");

                    _logger.LogInformation("Feedback session updated. New FeedbackCount: {FeedbackCount}", feedbackCount);

                    // Redirect to Index to prevent duplicate submission on page refresh (Post/Redirect/Get pattern).
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception exception)
                {
                    // Log persistence or processing failures — fall through to re-render the Index view below.
                    _logger.LogError(exception, "Error saving testimonial.");
                }
            }
            else
            {
                _logger.LogWarning("Home Create POST failed validation.");

                // Log each individual validation error to aid in diagnosing form submission issues.
                foreach (var modelStateKey in ViewData.ModelState.Keys)
                {
                    var modelStateValue = ViewData.ModelState[modelStateKey];

                    foreach (var error in modelStateValue.Errors)
                    {
                        _logger.LogError(error.ErrorMessage);
                    }
                }
            }

            // Re-fetch the active testimonial list so the Index view can render correctly after a failed submission.
            // Note: This re-fetch mirrors the Index GET logic and should be kept in sync if that query changes.
            var activeFeedbackList = _context.Feedbacks
                .Where(feedbackItem => feedbackItem.IsActive)
                .OrderByDescending(feedbackItem => feedbackItem.DateSubmitted)
                .ToList();

            _logger.LogInformation("Reloading Home Index with {Count} active testimonials after failed submission.", activeFeedbackList.Count);

            // Re-load the route schedule so the home page renders fully after a failed submission.
            RouteServices routeService = new RouteServices(_context);
            ViewBag.RouteSchedule = routeService.GetScheduleRoutes();

            _logger.LogInformation("Route schedule reloaded after failed submission.");

            return View("Index", activeFeedbackList);
        }

        /// <summary>
        /// Intended to retrieve and return an HTML-formatted route schedule string.
        /// </summary>
        /// <returns>
        /// An HTML string representation of the route schedule, or an error message string on failure.
        /// </returns>
        /// <remarks>
        /// DEV NOTE: The try block is empty — no route retrieval logic has been implemented.
        /// This method always returns <c>null</c> on the success path, which would render nothing in the view.
        /// Either implement the retrieval logic or remove this method if it has been superseded by
        /// <c>RouteServices.GetScheduleRoutes()</c> used in the Index action.
        /// </remarks>
        private string getSchedule()
        {
            _logger.LogInformation("getSchedule called.");

            RouteServices rs = new RouteServices(_context);

            try
            {
                // DEV NOTE: No implementation — route retrieval logic is missing from this block.
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Routes could not be retrieved");
                return "<h5>An error has occurred displaying route schedule at this time. Please try again later.";
            }

            _logger.LogInformation("getSchedule completed successfully.");

            // DEV NOTE: Always returns null — the success path has no return value defined.
            return null;
        }
    }
}