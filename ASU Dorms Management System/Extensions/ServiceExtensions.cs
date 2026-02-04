using ASUDorms.Application.Interfaces;
using ASUDorms.Domain.Interfaces;
using ASUDorms.Infrastructure.Data;
using ASUDorms.Infrastructure.Repositories;
using ASUDorms.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace ASU_Dorms_Management_System.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddDatabaseContext(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Get connection string - support Railway's DATABASE_URL format
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            // Railway provides DATABASE_URL as environment variable
            var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
            if (!string.IsNullOrEmpty(databaseUrl))
            {
                connectionString = ConvertRailwayUrlToNpgsql(databaseUrl);
                Console.WriteLine("[DB] Using Railway DATABASE_URL");
            }
            else
            {
                Console.WriteLine("[DB] Using appsettings connection string");
            }

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(
                    connectionString,
                    b => b.MigrationsAssembly("ASUDorms.Infrastructure")));

            return services;
        }

        /// <summary>
        /// Converts Railway's postgres:// URL to Npgsql connection string format
        /// Railway format: postgres://user:password@host:port/database
        /// Npgsql format: Host=host;Port=port;Database=database;Username=user;Password=password
        /// </summary>
        private static string ConvertRailwayUrlToNpgsql(string databaseUrl)
        {
            try
            {
                var uri = new Uri(databaseUrl);
                var userInfo = uri.UserInfo.Split(':');
                var username = userInfo[0];
                var password = userInfo.Length > 1 ? userInfo[1] : "";
                var host = uri.Host;
                var port = uri.Port > 0 ? uri.Port : 5432;
                var database = uri.AbsolutePath.TrimStart('/');

                return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB] Error parsing DATABASE_URL: {ex.Message}");
                return databaseUrl; // Return as-is if parsing fails
            }
        }

        public static IServiceCollection AddJwtAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = configuration["Jwt:Issuer"],
                        ValidAudience = configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]))
                    };
                });

            services.AddAuthorization();
            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Repositories
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IStudentService, StudentService>();
            services.AddScoped<IMealService, MealService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<IHolidayService, HolidayService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<DatabaseSeeder>();

            return services;
        }

        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "ASU Dorms Management API",
                    Version = "v1",
                    Description = "Multi-tenant dorm management system with meal tracking",
                    Contact = new OpenApiContact
                    {
                        Name = "ASU IT Department",
                        Email = "it@asu.edu.eg"
                    }
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            return services;
        }

        public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
        {
            var cloudflareEnabled = configuration.GetValue<bool>("Cloudflare:Enabled");
            var tunnelUrl = configuration.GetValue<string>("Cloudflare:TunnelUrl");

            services.AddCors(options =>
            {
                options.AddPolicy("AllowReactApp", policy =>
                {
                    // Use SetIsOriginAllowed for maximum flexibility with tunnels, Railway, and Vercel
                    policy.SetIsOriginAllowed(origin =>
                    {
                        // Allow all localhost variations
                        if (origin.Contains("localhost") || origin.Contains("127.0.0.1"))
                            return true;

                        // Allow all Cloudflare tunnel domains
                        if (origin.Contains(".trycloudflare.com") || origin.Contains(".cfargotunnel.com"))
                            return true;

                        // Allow Vercel deployments (production frontend)
                        if (origin.Contains(".vercel.app") || origin.Contains("vercel.app"))
                            return true;

                        // Allow Railway deployments
                        if (origin.Contains(".railway.app") || origin.Contains("railway.app"))
                            return true;

                        // Allow local network IPs (192.168.x.x, 10.x.x.x, 172.16-31.x.x)
                        if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                        {
                            var host = uri.Host;
                            if (host.StartsWith("192.168.") || host.StartsWith("10.") || 
                                (host.StartsWith("172.") && int.TryParse(host.Split('.')[1], out var second) && second >= 16 && second <= 31))
                                return true;
                        }

                        // Allow configured tunnel URL
                        if (cloudflareEnabled && !string.IsNullOrWhiteSpace(tunnelUrl) && 
                            origin.TrimEnd('/').Equals(tunnelUrl.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
                            return true;

                        Console.WriteLine($"[CORS] Blocked origin: {origin}");
                        return false;
                    })
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .WithExposedHeaders("X-Selected-Dorm-Id", "selected-dorm-id", "DormId", "Content-Type", "Authorization")
                    .SetPreflightMaxAge(TimeSpan.FromHours(1));

                    Console.WriteLine("[CORS] Policy configured - allowing Vercel, Railway, localhost, and tunnels");
                });
            });

            return services;
        }
    }
}
