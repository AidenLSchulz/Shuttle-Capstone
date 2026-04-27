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
using MidStateShuttleService.ViewModels;
using Moq;
using Xunit;

public class CheckInUpdateTests
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

    private CheckIn SeedCheckIn(ApplicationDbContext db)
    {
        var checkIn = new CheckIn
        {
            Name = "John",
            StudentId = "12345",
            LocationId = 1,
            DropOffLocationId = 2,
            IsActive = true,
            Date = DateTime.UtcNow
        };

        db.CheckIns.Add(checkIn);
        db.SaveChanges();
        return checkIn;
    }

    // =============================================
    // EditCheckIn — E003CI: ModelState invalid
    // =============================================

    [Fact]
    public void EditCheckIn_InvalidModel_ReturnsE003CI()
    {
        var controller = CreateController();
        controller.ModelState.AddModelError("Name", "Required");

        var model = new CheckInViewModel();

        var result = controller.EditCheckIn(model) as ViewResult;

        Assert.NotNull(result);
        Assert.Equal("E003CI", controller.TempData["Code"]);
    }

    // =============================================
    // EditCheckIn — E004CI: Check-in not found
    // =============================================

    [Fact]
    public void EditCheckIn_CheckInNotFound_ReturnsE004CI()
    {
        var controller = CreateController();

        var model = new CheckInViewModel
        {
            CheckInId = 9999, // does not exist
            Name = "John",
            LocationId = 1,
            UtcDate = DateTime.UtcNow
        };

        controller.EditCheckIn(model);

        Assert.Equal("E004CI", controller.TempData["Code"]);
    }

    // =============================================
    // EditCheckIn — S002CI: Successful update
    // =============================================

    [Fact]
    public void EditCheckIn_ValidModel_ReturnsS002CI()
    {
        var db = GetInMemoryDb();
        var seeded = SeedCheckIn(db);
        var controller = CreateController(db);

        var model = new CheckInViewModel
        {
            CheckInId = seeded.CheckInId,
            Name = "John Updated",
            LocationId = 1,
            UtcDate = DateTime.UtcNow
        };

        var result = controller.EditCheckIn(model) as RedirectToActionResult;

        Assert.NotNull(result);
        Assert.Equal("ViewAll", result.ActionName);
        Assert.Equal("S002CI", controller.TempData["Code"]);
    }

    // =============================================
    // EditCheckIn — Data integrity
    // =============================================

    [Fact]
    public void EditCheckIn_ValidModel_UpdatesFieldsCorrectly()
    {
        var db = GetInMemoryDb();
        var seeded = SeedCheckIn(db);
        var controller = CreateController(db);

        var model = new CheckInViewModel
        {
            CheckInId = seeded.CheckInId,
            Name = "John Updated",
            Comments = "Updated comment",
            FirstTime = true,
            LocationId = 2,
            UtcDate = DateTime.UtcNow
        };

        controller.EditCheckIn(model);

        var updated = db.CheckIns.Find(seeded.CheckInId);
        Assert.NotNull(updated);
        Assert.Equal("John Updated", updated.Name);
        Assert.Equal("Updated comment", updated.Comments);
        Assert.True(updated.FirstTime);
        Assert.Equal(2, updated.LocationId);
        Assert.True(updated.IsActive);
    }
}