using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Build.Framework;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using MidStateShuttleService.Models;
using MidStateShuttleService.Service;
using MidStateShuttleService.Services;
using System.Net;
using System.Net.Mail;

namespace MidStateShuttleService.Controllers
{
    public class CommunicateController : Controller
    {
        private readonly EmailServices _emailServices;

        private readonly ILogger<CommunicateController> _logger;

        private readonly ApplicationDbContext _context;

        // Inject ApplicationDbContext into the controller constructor
        public CommunicateController(ApplicationDbContext context, ILogger<CommunicateController> logger, EmailServices emailServices)
        {
            _context = context; // Assign the injected ApplicationDbContext to the _context field
            _logger = logger; // Assign the injected ILogger to the _logger field
            _emailServices = emailServices;
        }

        /// <summary>
        /// Displays the communication form, optionally pre-populated with route context
        /// if a specific route is being targeted for messaging.
        /// Restricted to Admin role only.
        /// </summary>
        /// <param name="routeId">
        /// Optional route ID. When provided, the form is pre-contextualized with the route's
        /// pick-up and drop-off location info for display purposes.
        /// </param>
        /// <returns>The communication form view with an initialized <see cref="CommuncateModel"/>.</returns>
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Index(int? routeId)
        {
            _logger.LogInformation("CommunicateController Index GET accessed. RouteId: {RouteId}", routeId);

            var model = new CommuncateModel();

            // Pre-populate the location name options for any location-based fields in the form.
            model.LocationNames = GetLocationNames();

            // If a route was specified, load its details to contextualize the communication form.
            if (routeId.HasValue)
            {
                // Eagerly load pick-up and drop-off locations to build the route display string below.
                var route = _context.Routes
                    .Include(r => r.PickUpLocation)
                    .Include(r => r.DropOffLocation)
                    .FirstOrDefault(r => r.RouteID == routeId.Value);

                if (route != null)
                {
                    _logger.LogInformation("Route found for communication. RouteId: {RouteId}", route.RouteID);

                    // Pass route details to the view for display (e.g. "Stop A → Stop B" header or label).
                    ViewData["RouteId"] = route.RouteID;
                    ViewData["RouteInfo"] = $"{route.PickUpLocation.Name} → {route.DropOffLocation.Name}";
                }
                else
                {
                    // Route ID was provided but no matching record exists — log and continue without route context.
                    _logger.LogWarning("Route not found for communication. RouteId: {RouteId}", routeId);
                }
            }

            return View(model);
        }

        /// <summary>
        /// Handles submission of the communication form, persists the message, and sends
        /// notification emails to all students registered on the specified route.
        /// Restricted to Admin role only.
        /// </summary>
        /// <param name="c">The submitted communication model containing the message and targeting details.</param>
        /// <param name="routeId">
        /// Optional route ID used to look up registered students who should receive the email notification.
        /// Defaults to 0 (no route) if not provided, which will return no registered students.
        /// </param>
        /// <returns>
        /// Redirects to the Index GET on success, returns the Error view on an unhandled exception,
        /// or returns the form view with validation errors if the model state is invalid.
        /// </returns>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult Index(CommuncateModel c, int? routeId)
        {
            _logger.LogInformation("CommunicateController Index POST received. RouteId: {RouteId}", routeId);

            // Re-populate location options since they are not persisted across POST requests.
            c.LocationNames = GetLocationNames();

            if (ModelState.IsValid)
            {
                try
                {
                    // Persist the communication message to the data store before sending emails.
                    CommunicationServices cs = new CommunicationServices(_context);
                    c.IsActive = true; // All newly submitted communications are marked active by default.
                    cs.AddEntity(c);

                    _logger.LogInformation("Communication message saved successfully. RouteId: {RouteId}", routeId);

                    // Retrieve the list of student emails registered on the target route.
                    // routeId ?? 0 means no emails will be sent if no route was specified.
                    RegisterServices rs = new RegisterServices(_context);
                    var registeredStudents = rs.GetEmailsByRoute(routeId ?? 0);

                    _logger.LogInformation(
                        "Found {StudentCount} registered students for RouteId: {RouteId}",
                        registeredStudents.Count(), routeId);

                    // Send the communication message to each registered student on the route.
                    // Note: Emails are sent synchronously — consider async sending for large student lists.
                    foreach (var student in registeredStudents)
                    {
                        _logger.LogInformation(
                            "Sending communication email to {StudentEmail} for RouteId: {RouteId}",
                            student.Email, routeId);

                        _emailServices.SendEmail(student.Email, "Mid State Shuttle Service Update", c.message);
                    }

                    // Signal the redirected GET action to display a success confirmation to the user.
                    TempData["CommunicationSuccess"] = true;

                    _logger.LogInformation("Communication processing completed successfully for RouteId: {RouteId}", routeId);

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Any failure during save or email sending lands here — the operation is not partially rolled back.
                    // DEV NOTE: Consider wrapping persistence and email sending separately if partial failure handling is needed.
                    _logger.LogError(ex, "Error Sending Message");
                    return View("Error");
                }
            }

            // Model validation failed — return the form with current input so the user can correct errors.
            _logger.LogWarning("CommunicateController Index POST failed validation. RouteId: {RouteId}", routeId);
            return View(c);
        }

        [AllowAnonymous]
        public IActionResult MessageSent()
        {
            _logger.LogInformation("MessageSent view accessed.");

            return View();
        }

        /// <summary>
        /// Displays the view for the student's communication form
        /// </summary>
        /// <returns> The Student Communicate View </returns>
        [AllowAnonymous]
        public IActionResult StudentCommunicate()
        {
            _logger.LogInformation("StudentCommunicate GET accessed.");

            return View();
        }

        /// <summary>
        /// Handles student message form submission. Persists the message, updates the session
        /// message count, and sends an internal admin notification.
        /// Accessible anonymously — no authentication required.
        /// </summary>
        /// <param name="c">The submitted message model containing the student's name, email, and message content.</param>
        /// <returns>
        /// Redirects to the StudentCommunicate GET on success, returns the Error view on an unhandled exception,
        /// or returns the form view with validation errors if the model state is invalid.
        /// </returns>
        [AllowAnonymous]
        [HttpPost]
        public IActionResult StudentCommunicate(Message c)
        {
            _logger.LogInformation("StudentCommunicate POST received from Name: {Name}, Email: {Email}", c?.name, c?.Email);

            if (ModelState.IsValid)
            {
                try
                {
                    // Persist the student's message to the data store before processing notifications.
                    MessageServices ms = new MessageServices(_context);
                    c.IsActive = true;
                    ms.AddEntity(c);

                    _logger.LogInformation("Student message saved successfully. MessageId: {MessageId}", c.id);

                    // Increment the session-tracked message count for the current user.
                    // This is used to surface unread message indicators in the UI.
                    int messageCount = HttpContext.Session.GetInt32("MessageCount") ?? 0;
                    messageCount++;
                    HttpContext.Session.SetInt32("MessageCount", messageCount);

                    // Store a summary string in session for display in notification banners or badges.
                    HttpContext.Session.SetString("LastMessage", "You have a new message!");

                    // Flag the session and TempData so both server-side and redirect-based success states are covered.
                    // Session persists across the redirect; TempData is consumed on the next request.
                    HttpContext.Session.SetString("CommunicationSuccess", "true");
                    TempData["CommunicationSuccess"] = true;

                    // Build and dispatch an internal admin notification alerting staff to the new student message.
                    Notification notif = new Notification();
                    notif.Subject = "New Message!";
                    notif.Body = c.name + " Sent a message!";
                    notif.TimeSent = DateTime.Now;
                    notif.MessageId = c.id;
                    new NotificationService(_context).SendNotification(notif);

                    _logger.LogInformation("Notification sent successfully for MessageId: {MessageId}", c.id);

                    return RedirectToAction("StudentCommunicate");
                }
                catch (Exception ex)
                {
                    // Any failure during message persistence or notification dispatching lands here.
                    // DEV NOTE: Consider separating persistence and notification into distinct try/catch blocks
                    // so a notification failure does not prevent the student's message from being confirmed.
                    _logger.LogError(ex, "Error Sending Message");
                    return View("Error");
                }
            }

            // Model validation failed — return the form with the submitted input so the student can correct errors.
            _logger.LogWarning("StudentCommunicate POST failed validation for Name: {Name}, Email: {Email}", c?.name, c?.Email);
            return View(c);
        }

        /// <summary>
        /// Retrieves location names from the service layer for use in communication form dropdowns.
        /// </summary>
        /// <returns>
        /// An enumerable of <see cref="SelectListItem"/> entries representing available locations.
        /// </returns>
        /// <remarks>
        /// DEV NOTE: If multiple controllers require this logic, consider moving it into LocationServices
        /// rather than duplicating it across controllers.
        /// </remarks>
        private IEnumerable<SelectListItem> GetLocationNames()
        {
            _logger.LogInformation("Fetching location names for communication dropdown.");

            // Delegate to the service layer for location retrieval rather than querying the DbContext directly.
            LocationServices ls = new LocationServices(_context);
            var locations = ls.GetLocationNames();

            return locations;
        }

        /// <summary>
        /// Displays a list of all student messages, optionally showing archived (inactive) records.
        /// Restricted to Admin role only.
        /// </summary>
        /// <param name="viewArchived">
        /// When <c>true</c>, returns only inactive (archived) messages.
        /// When <c>false</c> (default), returns only active messages.
        /// </param>
        /// <returns>The MessagesTable view populated with the filtered list of messages.</returns>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public ActionResult ViewAll(bool viewArchived = false)
        {
            _logger.LogInformation("ViewAll messages requested. ViewArchived: {ViewArchived}", viewArchived);

            // Filter messages by active/archived status.
            // Active records are returned by default; archived records when viewArchived=true.
            var messages = _context.Messages.Where(m => m.IsActive == !viewArchived);

            // Pass the current archive filter state to the view to drive UI elements such as tab state or button labels.
            ViewData["Archives"] = viewArchived;

            return View("MessagesTable", messages);
        }

        /// <summary>
        /// Displays the response form for a specific student message.
        /// </summary>
        /// <param name="id">The ID of the message to respond to.</param>
        /// <returns>
        /// The MessageRespond view populated with the message, or a 404 Not Found if the message does not exist.
        /// </returns>
        /// <remarks>
        /// DEV NOTE: This action is missing an explicit [HttpGet] attribute and authorization constraint.
        /// Consider adding both for consistency with the rest of the controller.
        /// </remarks>
        public IActionResult MessageRespond(int id)
        {
            _logger.LogInformation("MessageRespond GET requested for MessageId: {MessageId}", id);

            // Retrieve the target message to pre-populate the response form.
            var message = _context.Messages.FirstOrDefault(m => m.id == id);

            // Return 404 if the message no longer exists — cannot respond to a non-existent record.
            if (message == null)
            {
                _logger.LogWarning("MessageRespond GET failed. Message not found for MessageId: {MessageId}", id);
                return NotFound();
            }

            return View(message);
        }

        /// <summary>
        /// Handles submission of the message response form and sends a reply email to the student.
        /// </summary>
        /// <param name="id">The ID of the message being responded to.</param>
        /// <param name="responseMessage">The response text to send to the student's email address.</param>
        /// <returns>
        /// Redirects to the Index action on success, or returns the response view with an error message on failure.
        /// Returns 404 Not Found if the message record does not exist.
        /// </returns>
        /// <remarks>
        /// DEV NOTE: This action is marked <c>async</c> but contains no awaited calls — either the email sending
        /// should be made truly async via an awaitable overload, or the async modifier should be removed.
        /// </remarks>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MessageRespond(int id, string responseMessage)
        {
            _logger.LogInformation("MessageRespond POST received for MessageId: {MessageId}", id);

            // Re-fetch the message on POST rather than relying on hidden form fields to avoid tampering.
            var message = _context.Messages.FirstOrDefault(m => m.id == id);

            // Return 404 if the message no longer exists between the GET and POST requests.
            if (message == null)
            {
                _logger.LogWarning("MessageRespond POST failed. Message not found for MessageId: {MessageId}", id);
                return NotFound();
            }

            try
            {
                string subject = "Message reply from Mid-State Shuttle Services";

                // Send the admin's response to the email address captured on the original student message.
                _emailServices.SendEmail(message.Email, subject, responseMessage);

                _logger.LogInformation("Response email sent successfully for MessageId: {MessageId}", id);

                TempData["Success"] = "Response sent successfully.";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send response for MessageId: {MessageId}", id);

                // Surface the exception message directly to the admin — this is an internal-facing action
                // so exposing error detail is acceptable here unlike student-facing endpoints.
                TempData["Error"] = $"Failed to send response: {ex.Message}";

                return View(message);
            }
        }

        /// <summary>
        /// Toggles the active/archived state of a message (active → archived, or archived → active).
        /// Functionally acts as a soft delete for active messages. Restricted to Admin role only.
        /// </summary>
        /// <param name="id">The ID of the message whose active state will be toggled.</param>
        /// <returns>
        /// Redirects to ViewAll on success, or returns an empty view with a model error on failure.
        /// </returns>
        /// <remarks>
        /// DEV NOTE: This action is missing an explicit [HttpGet] attribute and should be converted to
        /// [HttpPost] for consistency — data modification actions should not be triggered via GET requests.
        /// The legacy comment referencing "DriverController" in the route annotation is also outdated and
        /// should be updated to reflect the correct controller.
        /// </remarks>
        // GET: DriverController/Delete/5
        [Authorize(Roles = "Admin")]
        public ActionResult Delete(int id)
        {
            _logger.LogInformation("Delete requested for MessageId: {MessageId}", id);

            try
            {
                var message = _context.Messages.Find(id);

                if (message != null)
                {
                    // Toggle the active state — active messages become archived, archived messages become active.
                    // Despite the action being named "Delete", this is a soft delete via IsActive flag, not a hard delete.
                    message.IsActive = !message.IsActive;
                    _context.SaveChanges();

                    _logger.LogInformation(
                        "Message IsActive toggled successfully for MessageId: {MessageId}. New IsActive: {IsActive}",
                        id, message.IsActive);
                }
                else
                {
                    _logger.LogWarning("Delete failed. Message not found for MessageId: {MessageId}", id);

                    // Surface a model error and return an empty view since there is no record to act on.
                    // DEV NOTE: Returning an empty View() here may cause a rendering error if the view
                    // requires a model — consider returning NotFound() or redirecting with a TempData error instead.
                    ModelState.AddModelError("", "Message not found.");
                    return View();
                }

                return RedirectToAction("ViewAll");
            }
            catch (Exception ex)
            {
                // DEV NOTE: The cast of _context to IWebHostEnvironment is almost certainly incorrect
                // and will throw at runtime. LogSqlException likely expects the injected _environment field instead.
                LogEvents.LogSqlException(ex, (IWebHostEnvironment)_context);

                _logger.LogError(ex, "An error occurred while toggling IsActive of the message.");

                // Surface a generic error to the admin — avoid exposing internal exception details in the model error.
                ModelState.AddModelError("", "An unexpected error occurred while toggling IsActive of the driver, please try again.");

                return View();
            }
        }

        /// <summary>
        /// Restores an archived message by explicitly setting its active state to <c>true</c>.
        /// Restricted to Admin role only.
        /// </summary>
        /// <param name="id">The ID of the archived message to restore.</param>
        /// <returns>
        /// Redirects to the archived ViewAll list on success, or returns a 404 Not Found if the message does not exist.
        /// </returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Unarchive(int id)
        {
            _logger.LogInformation("Unarchive requested for MessageId: {MessageId}", id);

            var message = _context.Messages.Find(id);

            // Return 404 if the record does not exist — no message to restore.
            if (message == null)
            {
                _logger.LogWarning("Unarchive failed. Message not found for MessageId: {MessageId}", id);
                return NotFound();
            }

            // Explicitly set IsActive to true rather than toggling — this action is unarchive-only.
            // For a bidirectional toggle, see the Delete action above.
            message.IsActive = true;
            _context.SaveChanges();

            _logger.LogInformation("Message unarchived successfully for MessageId: {MessageId}", id);

            // Redirect back to the archived list so the user can continue managing other archived messages.
            return RedirectToAction("ViewAll", new { viewArchived = true });
        }
    }
}