using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MidStateShuttleService.Controllers;
using MidStateShuttleService.Models;
using MidStateShuttleService.Service;
using MidStateShuttleService.Services;
using Moq;
using Xunit;

public class CheckInCreateTests
{
    private ApplicationDbContext GetInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private CheckInController CreateController(ApplicationDbContext db = null)
    {
        db ??= GetInMemoryDb();

        var mockLogger = new Mock<ILogger<CheckInController>>();
        var mockEnv = new Mock<IWebHostEnvironment>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var checkInService = new CheckInServices(db);
        var locationService = new LocationServices(db);

        var controller = new CheckInController(
            db,
            checkInService,
            locationService,
            mockLogger.Object,
            mockEnv.Object,
            cache
        );

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            controller.ControllerContext.HttpContext,
            Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>()
        );

        return controller;
    }

    // ===============================
    // RATE LIMITER TESTS
    // ===============================

    [Fact]
    public void CheckIn_RateLimitExceeded_ReturnsE001CI()
    {
        var controller = CreateController();
        var model = new CheckIn { Name = "John", StudentId = "12345" };

        ViewResult lastResult = null;
        for (int i = 0; i <= 16; i++)
        {
            lastResult = controller.CheckIn(model) as ViewResult;
        }

        Assert.Equal("E001CI", controller.TempData["Code"]);
    }

    [Fact]
    public void CheckIn_WithinRateLimit_DoesNotReturnE001CI()
    {
        var controller = CreateController();
        var model = new CheckIn
        {
            Name = "John",
            StudentId = "12345678",
            LocationId = 1,
            DropOffLocationId = 2
        };

        for (int i = 0; i < 5; i++)
        {
            controller.CheckIn(model);
        }

        Assert.NotEqual("E001CI", controller.TempData["Code"]);
    }

    // ===============================
    // MODEL VALIDATION TESTS
    // ===============================

    [Fact]
    public void CheckIn_InvalidModel_ReturnsE002CI()
    {
        var controller = CreateController();
        controller.ModelState.AddModelError("Name", "Required");

        var model = new CheckIn();

        var result = controller.CheckIn(model) as ViewResult;

        Assert.NotNull(result);
        Assert.Equal("E002CI", controller.TempData["Code"]);
    }

    [Fact]
    public void CheckIn_ValidModel_RedirectsToCheckIn()
    {
        var controller = CreateController();

        var model = new CheckIn
        {
            Name = "John",
            StudentId = "12345678",
            LocationId = 1,
            DropOffLocationId = 2,
            FirstTime = false,
            Comments = "No comments"
        };

        var result = controller.CheckIn(model) as RedirectToActionResult;

        Assert.NotNull(result);
        Assert.Equal("CheckIn", result.ActionName);
    }

    // ===============================
    // DATA INTEGRITY TESTS
    // ===============================

    [Fact]
    public void CheckIn_ValidSubmission_SetsIsActiveAndDate()
    {
        var db = GetInMemoryDb();
        var controller = CreateController(db);

        var model = new CheckIn
        {
            Name = "John",
            StudentId = "12345678",
            LocationId = 1,
            DropOffLocationId = 2
        };

        controller.CheckIn(model);

        var saved = db.CheckIns.FirstOrDefault();

        Assert.NotNull(saved);
        Assert.True(saved.IsActive);
        Assert.True((DateTime.UtcNow - saved.Date).TotalSeconds < 5);
    }

    [Fact]
    public void CheckIn_ValidSubmission_ReturnsS001CI()
    {
        var controller = CreateController();

        var model = new CheckIn
        {
            Name = "John",
            StudentId = "12345678",
            LocationId = 1,
            DropOffLocationId = 2
        };

        controller.CheckIn(model);

        Assert.Equal("S001CI", controller.TempData["Code"]);
    }
}