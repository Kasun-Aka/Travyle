using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Travyle.Api.Data;
using Travyle.Api.Models;

namespace Travyle.Api.Services.Auth;

public sealed record VerifiedStaff(User User);
public sealed record VerifiedFirebaseIdentity(string Uid, string? Email);

public interface IFirebaseIdentityService
{
    Task<VerifiedFirebaseIdentity?> VerifyFirebaseTokenAsync(HttpRequest request, CancellationToken ct = default);
    Task<User?> VerifyUserAsync(HttpRequest request, CancellationToken ct = default);
    Task<VerifiedStaff?> VerifyStaffAsync(HttpRequest request, CancellationToken ct = default);
}

public sealed class FirebaseIdentityService : IFirebaseIdentityService
{
    private readonly TravyleDbContext _db;

    public FirebaseIdentityService(TravyleDbContext db)
    {
        _db = db;
    }

    public async Task<VerifiedFirebaseIdentity?> VerifyFirebaseTokenAsync(HttpRequest request, CancellationToken ct = default)
    {
        if (!request.Headers.TryGetValue("Authorization", out var authorization) ||
            !authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = authorization.ToString()["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(token) || FirebaseApp.DefaultInstance == null)
        {
            return null;
        }

        FirebaseToken decoded;
        try
        {
            decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
        }
        catch (FirebaseAuthException)
        {
            return null;
        }

        var email = decoded.Claims.TryGetValue("email", out var emailClaim)
            ? emailClaim as string
            : null;
        return new VerifiedFirebaseIdentity(decoded.Uid, email);
    }

    public async Task<User?> VerifyUserAsync(HttpRequest request, CancellationToken ct = default)
    {
        var identity = await VerifyFirebaseTokenAsync(request, ct);
        if (identity == null) return null;

        return await _db.Users.FirstOrDefaultAsync(u => u.FirebaseUid == identity.Uid, ct);
    }

    public async Task<VerifiedStaff?> VerifyStaffAsync(HttpRequest request, CancellationToken ct = default)
    {
        var user = await VerifyUserAsync(request, ct);
        if (user == null ||
            (!string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(user.Role, "Operator", StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        return new VerifiedStaff(user);
    }

    public static void ConfigureFirebase(IConfiguration configuration)
    {
        if (FirebaseApp.DefaultInstance != null)
        {
            return;
        }

        var projectId = Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID")
                ?? configuration["Firebase:ProjectId"];
        var clientEmail = Environment.GetEnvironmentVariable("FIREBASE_CLIENT_EMAIL")
                  ?? configuration["Firebase:ClientEmail"];
        var privateKey = (Environment.GetEnvironmentVariable("FIREBASE_PRIVATE_KEY")
                  ?? configuration["Firebase:PrivateKey"])?.Replace("\\n", "\n");

        if (string.IsNullOrWhiteSpace(projectId) || projectId == "PROJECT_ID" ||
            string.IsNullOrWhiteSpace(clientEmail) ||
            clientEmail == "CLIENT_EMAIL" ||
            string.IsNullOrWhiteSpace(privateKey) || privateKey == "PRIVATE_KEY")
        {
            return;
        }

        var credentialJson = $$"""
        {
          "type": "service_account",
          "project_id": "{{projectId}}",
          "client_email": "{{clientEmail}}",
          "private_key": {{System.Text.Json.JsonSerializer.Serialize(privateKey)}}
        }
        """;

        FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.FromJson(credentialJson),
            ProjectId = projectId
        });
    }
}
