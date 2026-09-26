using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Travyle.Api.Data;
using Travyle.Api.Repositories;
using Travyle.Api.Services;
using Travyle.Api.Services.Auth;

// Allow flexible DateTime kinds in PostgreSQL
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

FirebaseIdentityService.ConfigureFirebase(builder.Configuration);

// ─── Infrastructure ──────────────────────────────────────────────────────────

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Travyle API", Version = "v1" });
});

builder.Services.AddDbContext<TravyleDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .ConfigureWarnings(warnings =>
               warnings.Ignore(RelationalEventId.PendingModelChangesWarning)));

// ─── Booking Vertical DI ─────────────────────────────────────────────────────

// Repositories
builder.Services.AddScoped<IBookingScheduleRepository, BookingScheduleRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IDiscountRequestRepository, DiscountRequestRepository>();
builder.Services.AddScoped<IPaymentEscrowRepository, PaymentEscrowRepository>();

// Services
builder.Services.AddScoped<IBookingScheduleService, BookingScheduleService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IDiscountRequestService, DiscountRequestService>();
builder.Services.AddScoped<IPaymentEscrowService, PaymentEscrowService>();

// Smart Booking Agent DI
builder.Services.AddScoped<Travyle.Api.Services.Agent.IBookingAgentTools, Travyle.Api.Services.Agent.BookingAgentTools>();
builder.Services.AddScoped<Travyle.Api.Services.Agent.ISmartBookingAgentService, Travyle.Api.Services.Agent.SmartBookingAgentProxyService>();
builder.Services.AddScoped<IFirebaseIdentityService, FirebaseIdentityService>();
builder.Services.AddHttpClient("SmartBookingAgent", client => { client.BaseAddress = new Uri("http://localhost:8000"); });

// ─── CORS (allow Flutter dev) ────────────────────────────────────────────────

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// ─── Pipeline ────────────────────────────────────────────────────────────────

var app = builder.Build();

// Seed initial database tables if empty
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TravyleDbContext>();
    await db.Database.MigrateAsync();

    await db.Database.ExecuteSqlRawAsync(
        "ALTER TABLE \"Bookings\" ADD COLUMN IF NOT EXISTS \"PaymentMethod\" text NOT NULL DEFAULT 'SampleCard';");
    await db.Database.ExecuteSqlRawAsync(
        "ALTER TABLE \"Bookings\" ADD COLUMN IF NOT EXISTS \"ReceiptReference\" text NULL;");
    await db.Database.ExecuteSqlRawAsync(
        "ALTER TABLE \"Bookings\" ADD COLUMN IF NOT EXISTS \"ReceiptImageData\" text NULL;");
    await db.Database.ExecuteSqlRawAsync(
        "ALTER TABLE \"BookingSchedules\" ADD COLUMN IF NOT EXISTS \"SlotOverrides\" text NOT NULL DEFAULT '[]';");

    // Use raw Npgsql connection to avoid EF treating { } in DEFAULT values as format placeholders
    var conn = db.Database.GetDbConnection();
    await conn.OpenAsync();
    await using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS ""BookingAgentWorkflows"" (
                ""Id"" uuid NOT NULL PRIMARY KEY,
                ""TravelerId"" uuid NOT NULL,
                ""TravelerName"" text NOT NULL DEFAULT '',
                ""TravelerEmail"" text NOT NULL DEFAULT '',
                ""Objective"" text NOT NULL DEFAULT '',
                ""Status"" text NOT NULL DEFAULT 'Running',
                ""PlanJson"" text NOT NULL DEFAULT '[]',
                ""CompletedStepsJson"" text NOT NULL DEFAULT '[]',
                ""ToolResultsJson"" text NOT NULL DEFAULT '{}',
                ""ValidationResultsJson"" text NOT NULL DEFAULT '{}',
                ""ProposedBookingJson"" text NULL,
                ""CreatedBookingId"" uuid NULL,
                ""BookingReference"" text NULL,
                ""ApprovalStatus"" text NOT NULL DEFAULT 'PENDING',
                ""ApprovedBy"" text NULL,
                ""ApproverRole"" text NULL,
                ""ApproverNotes"" text NULL,
                ""ApprovedAt"" timestamp with time zone NULL,
                ""ErrorMessage"" text NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT NOW(),
                ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT NOW()
            );";
        await cmd.ExecuteNonQueryAsync();
    }
    await conn.CloseAsync();

    await DbSeeder.SeedAsync(db);
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Travyle API v1"));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

