using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Travyle.Api.Controllers;
using Travyle.Api.Data;
using Travyle.Api.Models;
using Travyle.Api.Services.Auth;
using Xunit;

namespace Travyle.Tests;

public class MobileDataAccessTests
{
    [Fact]
    public async Task BookingHistory_RejectsAnotherTravelersId()
    {
        var identity = new StubFirebaseIdentityService(CreateUser("Traveler"));
        var controller = new BookingsController(null!, null!, identity);
        SetRequestContext(controller);

        var result = await controller.GetTravelerBookings(Guid.NewGuid());

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DiscountHistory_RejectsAnotherTravelersId()
    {
        var identity = new StubFirebaseIdentityService(CreateUser("Traveler"));
        var controller = new DiscountRequestsController(null!, identity);
        SetRequestContext(controller);

        var result = await controller.GetForTraveler(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GlobalDiscountList_RequiresStaffRole()
    {
        var identity = new StubFirebaseIdentityService(CreateUser("Traveler"));
        var controller = new DiscountRequestsController(null!, identity);
        SetRequestContext(controller);

        var result = await controller.GetAll(CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task AccountSync_DoesNotAcceptAdminRoleFromRequestBody()
    {
        var firebaseUid = Guid.NewGuid().ToString("N");
        var email = $"{firebaseUid}@test.example";
        var options = new DbContextOptionsBuilder<TravyleDbContext>()
            .UseInMemoryDatabase(nameof(AccountSync_DoesNotAcceptAdminRoleFromRequestBody))
            .Options;
        await using var db = new TravyleDbContext(options);
        var identity = new StubFirebaseIdentityService(
            user: null,
            verifiedIdentity: new VerifiedFirebaseIdentity(firebaseUid, email));
        var controller = new AuthController(db, identity);
        SetRequestContext(controller);

        var result = await controller.SyncUser(new AuthController.SyncRequest
        {
            FirebaseUid = firebaseUid,
            Email = email,
            FullName = "New account",
            Role = "Admin"
        }, CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result);
        var createdUser = Assert.IsType<User>(response.Value);
        Assert.Equal("Traveler", createdUser.Role);
    }

    private static User CreateUser(string role) => new()
    {
        Id = Guid.NewGuid(),
        FirebaseUid = Guid.NewGuid().ToString("N"),
        Email = "traveler@test.example",
        FullName = "Test traveler",
        Role = role
    };

    private static void SetRequestContext(ControllerBase controller)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    private sealed class StubFirebaseIdentityService : IFirebaseIdentityService
    {
        private readonly User? _user;
        private readonly VerifiedFirebaseIdentity? _verifiedIdentity;

        public StubFirebaseIdentityService(
            User? user,
            VerifiedFirebaseIdentity? verifiedIdentity = null)
        {
            _user = user;
            _verifiedIdentity = verifiedIdentity ?? (user == null
                ? null
                : new VerifiedFirebaseIdentity(user.FirebaseUid, user.Email));
        }

        public Task<VerifiedFirebaseIdentity?> VerifyFirebaseTokenAsync(
            HttpRequest request,
            CancellationToken ct = default) => Task.FromResult(_verifiedIdentity);

        public Task<User?> VerifyUserAsync(
            HttpRequest request,
            CancellationToken ct = default) => Task.FromResult(_user);

        public Task<VerifiedStaff?> VerifyStaffAsync(
            HttpRequest request,
            CancellationToken ct = default)
        {
            var isStaff = _user != null &&
                (string.Equals(_user.Role, "Admin", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(_user.Role, "Operator", StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(isStaff ? new VerifiedStaff(_user!) : null);
        }
    }
}