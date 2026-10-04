using Microsoft.AspNetCore.Mvc;
using Travyle.Api.Models;
using Travyle.Api.Services.Auth;

namespace Travyle.Api.Controllers;

/// <summary>
/// Component 4 (Support &amp; Quality) shared authorization helpers.
/// Reuses the existing Firebase identity verification; no shared auth code is modified.
/// Staff = Admin or Operator (same definition used elsewhere in the API).
/// </summary>
public abstract class SupportControllerBase : ControllerBase
{
    protected readonly IFirebaseIdentityService IdentityService;

    protected SupportControllerBase(IFirebaseIdentityService identityService)
    {
        IdentityService = identityService;
    }

    protected static bool IsStaff(User user) =>
        string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(user.Role, "Operator", StringComparison.OrdinalIgnoreCase);

    /// <summary>Returns the verified signed-in user, or null (caller returns 401).</summary>
    protected Task<User?> GetSignedInUserAsync(CancellationToken ct) =>
        IdentityService.VerifyUserAsync(Request, ct);

    protected UnauthorizedObjectResult SignInRequired() =>
        Unauthorized(new { message = "A valid sign-in is required." });

    protected ObjectResult StaffOnly() =>
        StatusCode(StatusCodes.Status403Forbidden, new { message = "This action requires an Admin or Operator account." });
}
