using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Travyle.Api.Data;
using Travyle.Api.Repositories;
using Travyle.Api.Services;
using Travyle.Api.Services.Auth;

// Allow flexible DateTime kinds in PostgreSQL
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// ─── Infrastructure ──────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Travyle API", Version = "v1" });
});

// Configure Database
builder.Services.AddDbContext<TravyleDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .ConfigureWarnings(warnings =>
               warnings.Ignore(RelationalEventId.PendingModelChangesWarning)));

// ─── AI, Geocoding & External Services ──────────────────────────────────────
builder.Services.AddHttpClient<GeminiService>();
builder.Services.AddHttpClient<IGeocodingService, NominatimGeocodingService>();

// Support & Customer Quality (Component 4) Services
builder.Services.AddHttpClient<NotificationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ISupportAiAgentService, SupportAiAgentService>();
builder.Services.AddScoped<ISupportService, SupportService>();

// ─── Booking Vertical DI ─────────────────────────────────────────────────────
// Repositories
builder.Services.AddScoped<IBookingScheduleRepository, BookingScheduleRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IDiscountRequestRepository, DiscountRequestRepository>();
builder.Services.AddScoped<IPaymentEscrowRepository, PaymentEscrowRepository>();

// ─── Operations DI ───────────────────────────────────────────────────────────
builder.Services.AddScoped<Travyle.Api.Repositories.IOperationsRepository, Travyle.Api.Repositories.OperationsRepository>();
builder.Services.AddScoped<IOperationsService, OperationsService>();

// Services
builder.Services.AddScoped<IBookingScheduleService, BookingScheduleService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IDiscountRequestService, DiscountRequestService>();
builder.Services.AddScoped<IPaymentEscrowService, PaymentEscrowService>();

// Smart Booking Agent DI
builder.Services.AddScoped<Travyle.Api.Services.Agent.IBookingAgentTools, Travyle.Api.Services.Agent.BookingAgentTools>();
builder.Services.AddScoped<Travyle.Api.Services.Agent.ISmartBookingAgentService, Travyle.Api.Services.Agent.SmartBookingAgentProxyService>();
builder.Services.AddScoped<IFirebaseIdentityService, FirebaseIdentityService>();

var aiAgentBaseUrl = builder.Configuration.GetValue<string>("AI_AGENT_BASE_URL") ?? "http://localhost:8000";
builder.Services.AddHttpClient("SmartBookingAgent", client => { client.BaseAddress = new Uri(aiAgentBaseUrl); });

// ─── Firebase Admin SDK ──────────────────────────────────────────────────────
try
{
    FirebaseIdentityService.ConfigureFirebase(builder.Configuration);
    Console.WriteLine(FirebaseAdmin.FirebaseApp.DefaultInstance != null
        ? "✅ Firebase Admin SDK initialized successfully."
        : "⚠️  Firebase Admin SDK was NOT initialized (missing or placeholder credentials).");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Firebase Admin SDK initialization failed: {ex.Message}");
}

// ─── CORS Configuration ──────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ─── Pipeline ────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Travyle API v1");
    c.RoutePrefix = "swagger";
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseStaticFiles();

// 1. CORS MUST come BEFORE Routing / Authorization / Redirections
app.UseCors("AllowAll");

app.UseRouting();
app.UseAuthorization();
app.MapControllers();

// Initialize/Seed database if connection is present
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TravyleDbContext>();
    if (db.Database.CanConnect())
    {
        db.Database.Migrate();
        await DbSeeder.SeedAsync(db);
    }
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Could not initialize database on startup (Check connection string).");
}

app.Run();