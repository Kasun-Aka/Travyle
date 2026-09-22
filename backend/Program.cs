using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Travyle.Api.Data;
using Travyle.Api.Repositories;
using Travyle.Api.Services;

// Allow flexible DateTime kinds in PostgreSQL
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

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
    await db.Database.ExecuteSqlRawAsync(
        "ALTER TABLE \"Bookings\" ADD COLUMN IF NOT EXISTS \"PaymentMethod\" text NOT NULL DEFAULT 'SampleCard';");
    await db.Database.ExecuteSqlRawAsync(
        "ALTER TABLE \"Bookings\" ADD COLUMN IF NOT EXISTS \"ReceiptReference\" text NULL;");
    await db.Database.ExecuteSqlRawAsync(
        "ALTER TABLE \"Bookings\" ADD COLUMN IF NOT EXISTS \"ReceiptImageData\" text NULL;");
    await db.Database.MigrateAsync();
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

