using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MidStateShuttleService.Controllers;
using MidStateShuttleService.Models;
using MidStateShuttleService.Services;
using Moq;
using Xunit;
using MidStateShuttleService.Enums;

public class RegisterUpdateTests
{
    private ApplicationDbContext GetInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private RegisterController CreateController(ApplicationDbContext db)
    {
        var mockLogger = new Mock<ILogger<RegisterController>>();
        var cache = new MemoryCache(new MemoryCacheOptions());

        var controller = new RegisterController(db, null, mockLogger.Object, cache);

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

    private RegisterModel SeedRegistration(ApplicationDbContext db)
    {
        var model = new RegisterModel
        {
            Name = "John",
            Email = "john@test.com",
            Phone = "5555555555",
            Term = SchoolTerm.Spring,
            isCustom = false,
            DaySchedules = new List<RequestDay>
        {
            new RequestDay
            {
                WeekDay = WeekDay.Monday,
                Rides = new List<Ride>
                {
                    new Ride { RouteId = 1, PickUpLocationID = 1, DropOffLocationID = 2 }
                }
            }
        }
        };

        db.RegisterModels.Add(model);
        db.SaveChanges();
        return model;
    }

    private RegisterModel SeedSpecialRegistration(ApplicationDbContext db)
    {
        var model = new RegisterModel
        {
            Name = "Jane",
            Email = "jane@test.com",
            Phone = "5555555555",
            Term = SchoolTerm.Fall,
            isCustom = true,
            customMessage = "Original message",
            customDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5))
        };

        db.RegisterModels.Add(model);
        db.SaveChanges();
        return model;
    }

    // =============================================
    // EditSave — E006RG: No day schedules
    // =============================================

    [Fact]
    public void EditSave_NoDaySchedules_ReturnsE006RG()
    {
        var db = GetInMemoryDb();
        var seeded = SeedRegistration(db);
        var controller = CreateController(db);

        var model = new RegisterModel
        {
            RegistrationId = seeded.RegistrationId,
            Name = "John",
            Email = "john@test.com",
            DaySchedules = new List<RequestDay>() // empty
        };

        var result = controller.EditSave(model) as ViewResult;

        Assert.NotNull(result);
        Assert.Equal("E006RG", controller.TempData["Code"]);
    }

    [Fact]
    public void EditSave_NullDaySchedules_ReturnsE006RG()
    {
        var db = GetInMemoryDb();
        var seeded = SeedRegistration(db);
        var controller = CreateController(db);

        var model = new RegisterModel
        {
            RegistrationId = seeded.RegistrationId,
            Name = "John",
            Email = "john@test.com",
            DaySchedules = null
        };

        var result = controller.EditSave(model) as ViewResult;

        Assert.NotNull(result);
        Assert.Equal("E006RG", controller.TempData["Code"]);
    }

    // =============================================
    // EditSave — E007RG: Duplicate weekdays
    // =============================================

    [Fact]
    public void EditSave_DuplicateDays_ReturnsE007RG()
    {
        var db = GetInMemoryDb();
        var seeded = SeedRegistration(db);
        var controller = CreateController(db);

        var model = new RegisterModel
        {
            RegistrationId = seeded.RegistrationId,
            Name = "John",
            Email = "john@test.com",
            DaySchedules = new List<RequestDay>
            {
                new RequestDay
                {
                    WeekDay = WeekDay.Monday,
                    Rides = new List<Ride>
                    {
                        new Ride { RouteId = 1, PickUpLocationID = 1, DropOffLocationID = 2 }
                    }
                },
                new RequestDay
                {
                    WeekDay = WeekDay.Monday, // duplicate
                    Rides = new List<Ride>
                    {
                        new Ride { RouteId = 2, PickUpLocationID = 1, DropOffLocationID = 2 }
                    }
                }
            }
        };

        var result = controller.EditSave(model) as ViewResult;

        Assert.NotNull(result);
        Assert.Equal("E007RG", controller.TempData["Code"]);
    }

    // =============================================
    // EditSave — E008RG: A day with no rides
    // =============================================

    [Fact]
    public void EditSave_DayWithNoRides_ReturnsE008RG()
    {
        var db = GetInMemoryDb();
        var seeded = SeedRegistration(db);
        var controller = CreateController(db);

        var model = new RegisterModel
        {
            RegistrationId = seeded.RegistrationId,
            Name = "John",
            Email = "john@test.com",
            DaySchedules = new List<RequestDay>
            {
                new RequestDay
                {
                    WeekDay = WeekDay.Monday,
                    Rides = new List<Ride>() // day exists but no rides
                }
            }
        };

        var result = controller.EditSave(model) as ViewResult;

        Assert.NotNull(result);
        Assert.Equal("E008RG", controller.TempData["Code"]);
    }

    // =============================================
    // EditSave — E009RG: Ride missing route and time
    // =============================================

    [Fact]
    public void EditSave_RideMissingRouteAndTime_ReturnsE009RG()
    {
        var db = GetInMemoryDb();
        var seeded = SeedRegistration(db);
        var controller = CreateController(db);

        var model = new RegisterModel
        {
            RegistrationId = seeded.RegistrationId,
            Name = "John",
            Email = "john@test.com",
            DaySchedules = new List<RequestDay>
            {
                new RequestDay
                {
                    WeekDay = WeekDay.Monday,
                    Rides = new List<Ride>
                    {
                        new Ride
                        {
                            RouteId = null,
                            DropOffTime = null,
                            PickUpLocationID = 1,
                            DropOffLocationID = 2
                        }
                    }
                }
            }
        };

        var result = controller.EditSave(model) as ViewResult;

        Assert.NotNull(result);
        Assert.Equal("E009RG", controller.TempData["Code"]);
    }

    // =============================================
    // EditSave — S001RG: Successful save
    // =============================================

    [Fact]
    public void EditSave_ValidModel_ReturnsS001RG()
    {
        var db = GetInMemoryDb();
        var seeded = SeedRegistration(db);
        var controller = CreateController(db);

        var model = new RegisterModel
        {
            RegistrationId = seeded.RegistrationId,
            Name = "John Updated",
            Email = "john@test.com",
            DaySchedules = new List<RequestDay>
            {
                new RequestDay
                {
                    WeekDay = WeekDay.Tuesday,
                    Rides = new List<Ride>
                    {
                        new Ride { RouteId = 1, PickUpLocationID = 1, DropOffLocationID = 2 }
                    }
                }
            }
        };

        var result = controller.EditSave(model) as RedirectToActionResult;

        Assert.NotNull(result);
        Assert.Equal("Details", result.ActionName);
        Assert.Equal("S001RG", controller.TempData["Code"]);
    }

    // =============================================
    // EditSave — NotFound: Registration does not exist
    // =============================================

    [Fact]
    public void EditSave_RegistrationNotFound_ReturnsNotFound()
    {
        var db = GetInMemoryDb();
        var controller = CreateController(db);

        var model = new RegisterModel
        {
            RegistrationId = 9999,
            Name = "John",
            Email = "john@test.com",
            DaySchedules = new List<RequestDay>
            {
                new RequestDay
                {
                    WeekDay = WeekDay.Monday,
                    Rides = new List<Ride>
                    {
                        new Ride { RouteId = 1, PickUpLocationID = 1, DropOffLocationID = 2 }
                    }
                }
            }
        };

        var result = controller.EditSave(model);

        Assert.IsType<NotFoundResult>(result);
    }

    // =============================================
    // SpecialEditSave — E010RG: Missing custom message
    // =============================================

    [Fact]
    public void SpecialEditSave_MissingCustomMessage_ReturnsE010RG()
    {
        var db = GetInMemoryDb();
        var seeded = SeedSpecialRegistration(db);
        var controller = CreateController(db);

        var model = new RegisterModel
        {
            RegistrationId = seeded.RegistrationId,
            customMessage = "",
            customDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3))
        };

        var result = controller.SpecialEditSave(model) as ViewResult;

        Assert.NotNull(result);
        Assert.Equal("E010RG", controller.TempData["Code"]);
    }

    [Fact]
    public void SpecialEditSave_WhitespaceCustomMessage_ReturnsE010RG()
    {
        var db = GetInMemoryDb();
        var seeded = SeedSpecialRegistration(db);
        var controller = CreateController(db);

        var model = new RegisterModel
        {
            RegistrationId = seeded.RegistrationId,
            customMessage = "   ", // whitespace only
            customDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3))
        };

        var result = controller.SpecialEditSave(model) as ViewResult;

        Assert.NotNull(result);
        Assert.Equal("E010RG", controller.TempData["Code"]);
    }

    // =============================================
    // SpecialEditSave — E011RG: Custom date in the past
    // =============================================

    [Fact]
    public void SpecialEditSave_CustomDateInPast_ReturnsE011RG()
    {
        var db = GetInMemoryDb();
        var seeded = SeedSpecialRegistration(db);
        var controller = CreateController(db);

        var model = new RegisterModel
        {
            RegistrationId = seeded.RegistrationId,
            customMessage = "Valid message",
            customDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)) // yesterday
        };

        var result = controller.SpecialEditSave(model) as ViewResult;

        Assert.NotNull(result);
        Assert.Equal("E011RG", controller.TempData["Code"]);
    }

    [Fact]
    public void SpecialEditSave_NullCustomDate_DoesNotReturnE011RG()
    {
        var db = GetInMemoryDb();
        var seeded = SeedSpecialRegistration(db);
        var controller = CreateController(db);

        var model = new RegisterModel
        {
            RegistrationId = seeded.RegistrationId,
            customMessage = "Valid message",
            customDate = null
        };

        controller.SpecialEditSave(model);

        Assert.NotEqual("E011RG", controller.TempData["Code"]);
    }

    // =============================================
    // SpecialEditSave — S002RG: Successful save
    // =============================================

    [Fact]
    public void SpecialEditSave_ValidModel_ReturnsS002RG()
    {
        var db = GetInMemoryDb();
        var seeded = SeedSpecialRegistration(db);
        var controller = CreateController(db);

        var model = new RegisterModel
        {
            RegistrationId = seeded.RegistrationId,
            customMessage = "Updated message",
            customDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))
        };

        var result = controller.SpecialEditSave(model) as RedirectToActionResult;

        Assert.NotNull(result);
        Assert.Equal("SpecialDetails", result.ActionName);
        Assert.Equal("S002RG", controller.TempData["Code"]);
    }

    // =============================================
    // SpecialEditSave — NotFound: Registration does not exist
    // =============================================

    [Fact]
    public void SpecialEditSave_RegistrationNotFound_ReturnsNotFound()
    {
        var db = GetInMemoryDb();
        var controller = CreateController(db);

        var model = new RegisterModel
        {
            RegistrationId = 9999,
            customMessage = "Valid message",
            customDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3))
        };

        var result = controller.SpecialEditSave(model);

        Assert.IsType<NotFoundResult>(result);
    }
}