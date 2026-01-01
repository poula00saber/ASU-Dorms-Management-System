
using ASU_Dorms_Management_System.Extensions;
using ASU_Dorms_Management_System.Middleware;
using ASUDorms.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// Add services using extension methods
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

builder.Services.AddDatabaseContext(builder.Configuration);

// ===== IMPORTANT: Replace AddJwtAuthentication with this detailed version =====
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]))
        };

        // Add event handlers for debugging
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"❌ AUTH FAILED: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("✅ TOKEN VALIDATED");
                var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var dormLocationId = context.Principal?.FindFirst("DormLocationId")?.Value;
                Console.WriteLine($"   User ID: {userId}");
                Console.WriteLine($"   DormLocationId Claim: {dormLocationId}");

                // Log all claims
                Console.WriteLine("   All Claims:");
                foreach (var claim in context.Principal.Claims)
                {
                    Console.WriteLine($"     {claim.Type} = {claim.Value}");
                }

                return Task.CompletedTask;
            },
            OnMessageReceived = context =>
            {
                var path = context.HttpContext.Request.Path.Value;

                // Ignore non-API requests
                if (!path.StartsWith("/api"))
                    return Task.CompletedTask;

                var token = context.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(token))
                {
                    Console.WriteLine($"📩 TOKEN RECEIVED: {token.Substring(0, Math.Min(30, token.Length))}...");
                }
                else
                {
                    Console.WriteLine("⚠️ NO TOKEN IN API REQUEST");
                }

                return Task.CompletedTask;
            }

        };
    });

builder.Services.AddAuthorization();

builder.Services.AddApplicationServices();
builder.Services.AddSwaggerDocumentation();
builder.Services.AddCorsPolicy();

var app = builder.Build();

// Seed database on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();
        Console.WriteLine("✅ Database seeded successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Seeding failed: {ex.Message}");
    }
}

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowReactApp");      // ← CORS BEFORE Authentication
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine("🚀 Application started successfully");
Console.WriteLine($"📍 Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"🔒 JWT Issuer: {builder.Configuration["Jwt:Issuer"]}");
Console.WriteLine($"🔒 JWT Audience: {builder.Configuration["Jwt:Audience"]}");

app.Run();
