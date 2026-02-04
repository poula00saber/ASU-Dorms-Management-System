using ASU_Dorms_Management_System.Extensions;
using ASU_Dorms_Management_System.Middleware;
using ASUDorms.Infrastructure.Data;
using ASUDorms.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Railway deployment: Get port from environment variable
var port = Environment.GetEnvironmentVariable("PORT") ?? "5065";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Add services using extension methods
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

builder.Services.AddDatabaseContext(builder.Configuration);

// Configure ForwardedHeaders for Cloudflare Tunnel
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
    options.AllowedHosts.Clear();
});

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
                Console.WriteLine($"? AUTH FAILED: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("? TOKEN VALIDATED");
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
                    Console.WriteLine($"?? TOKEN RECEIVED: {token.Substring(0, Math.Min(30, token.Length))}...");
                }
                else
                {
                    Console.WriteLine("?? NO TOKEN IN API REQUEST");
                }

                return Task.CompletedTask;
            }

        };
    });

builder.Services.AddAuthorization();

builder.Services.AddApplicationServices();
builder.Services.AddSwaggerDocumentation();
builder.Services.AddCorsPolicy(builder.Configuration);

var app = builder.Build();

// Seed database on startup (and auto-migrate for Railway)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Auto-migrate database (important for Railway deployment)
        if (app.Environment.IsProduction())
        {
            Console.WriteLine("?? Running database migrations...");
            await context.Database.MigrateAsync();
            Console.WriteLine("? Database migrations completed");
        }
        
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();
        Console.WriteLine("? Database seeded successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"? Database setup failed: {ex.Message}");
        Console.WriteLine($"   Stack: {ex.StackTrace}");
    }
}
// Configure middleware pipeline

// IMPORTANT: ForwardedHeaders must be FIRST for Cloudflare tunnel
app.UseForwardedHeaders();

// Always enable Swagger for tunnel access
app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<ExceptionHandlingMiddleware>();

// CORS MUST BE FIRST - before anything else that handles requests
app.UseCors("AllowReactApp");

// REMOVE or COMMENT OUT HttpsRedirection - Cloudflare handles HTTPS
// This causes "Failed to fetch" when React calls HTTP and gets redirected
// app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Startup logging with Cloudflare info
var cloudflareEnabled = builder.Configuration.GetValue<bool>("Cloudflare:Enabled");
var tunnelUrl = builder.Configuration.GetValue<string>("Cloudflare:TunnelUrl");

Console.WriteLine("");
Console.WriteLine("==========================================");
Console.WriteLine("  ASU Dorms Management System API");
Console.WriteLine("  PostgreSQL + Railway Ready");
Console.WriteLine("==========================================");
Console.WriteLine($"  Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"  Port: {port}");
Console.WriteLine($"  JWT Issuer:  {builder.Configuration["Jwt:Issuer"]}");
if (cloudflareEnabled && !string.IsNullOrEmpty(tunnelUrl))
{
    Console.WriteLine($"  Cloudflare:  {tunnelUrl}");
}
Console.WriteLine("==========================================");
Console.WriteLine("");

app.Run();
