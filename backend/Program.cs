using Microsoft.EntityFrameworkCore;
using Travyle.Api.Data;
using Travyle.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Travyle Smart Travel API", Version = "v1", Description = "API for Travyle Travel Management & Support Platform" });
});

// Configure Database
builder.Services.AddDbContext<TravyleDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Component 4 Services
builder.Services.AddHttpClient<NotificationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ISupportAiAgentService, SupportAiAgentService>();
builder.Services.AddScoped<ISupportService, SupportService>();

// CORS configuration for React Web Admin and Flutter
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Travyle API v1");
    c.RoutePrefix = "swagger";
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Initialize/Seed database if connection is present
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TravyleDbContext>();
    if (db.Database.CanConnect())
    {
        db.Database.EnsureCreated();
        await DbSeeder.SeedAsync(db);
    }
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Could not initialize database on startup (Check connection string).");
}

app.Run();

