using Microsoft.EntityFrameworkCore;
using BAU_Plagiarism_System.Data;
using BAU_Plagiarism_System.Core.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Hangfire;
using Hangfire.SqlServer;
using Hangfire.Dashboard;

try 
{
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c => {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "BAU Plagiarism System API", Version = "v1" });
        
        // Configure JWT Swagger UI
        c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme {
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Please enter token",
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            BearerFormat = "JWT",
            Scheme = "bearer"
        });
        c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new string[] { }
            }
        });
    });

    // Register DbContext with SQL Server
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<BAUDbContext>(options =>
        options.UseSqlServer(connectionString));

    // Register Infrastructure Services
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddHttpClient(); // For WebSearchService

    // Register Business Services
    builder.Services.AddScoped<SimilarityChecker>();
    builder.Services.AddScoped<DocumentReader>();
    builder.Services.AddScoped<TextProcessor>();

    // Register Application Services
    builder.Services.AddScoped<CatalogService>();
    builder.Services.AddScoped<UserService>();
    builder.Services.AddScoped<DocumentService>(provider => 
    {
        var context = provider.GetRequiredService<BAUDbContext>();
        var reader = provider.GetRequiredService<DocumentReader>();
        return new DocumentService(context, reader, "uploads");
    });
    builder.Services.AddScoped<PlagiarismService>();
    builder.Services.AddScoped<ImportService>();
    builder.Services.AddScoped<AiDetectionService>();
    builder.Services.AddScoped<DocumentQualityService>();
    builder.Services.AddScoped<WebSearchService>();

    // Hangfire Configuration
    builder.Services.AddHangfire(configuration => configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        }));

    builder.Services.AddHangfireServer();

    // Register AuthService with configuration
    builder.Services.AddScoped<AuthService>(provider =>
    {
        var context = provider.GetRequiredService<BAUDbContext>();
        var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "BAU_Plagiarism_System_Secret_Key_2024_Very_Long_And_Secure";
        var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "BAU_Plagiarism_System";
        var jwtExpiryMinutesStr = builder.Configuration["Jwt:ExpiryMinutes"] ?? "480";
        if (!int.TryParse(jwtExpiryMinutesStr, out int jwtExpiryMinutes)) jwtExpiryMinutes = 480;
        return new AuthService(context, jwtSecret, jwtIssuer, jwtExpiryMinutes);
    });

    // Configure JWT Authentication
    var jwtSecretVal = builder.Configuration["Jwt:Secret"] ?? "BAU_Plagiarism_System_Secret_Key_2024_Very_Long_And_Secure";
    var jwtIssuerVal = builder.Configuration["Jwt:Issuer"] ?? "BAU_Plagiarism_System";

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecretVal)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuerVal,
            ValidateAudience = true,
            ValidAudience = jwtIssuerVal,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/api/documents"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddAuthorization();

    // CORS for Frontend
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors("AllowAll");
    
    // Global Exception Logging Middleware
    app.Use(async (context, next) => {
        try {
            await next();
        } catch (Exception ex) {
            Console.WriteLine($"[RUNTIME ERROR] {DateTime.Now}: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            await File.AppendAllTextAsync("runtime_errors.log", $"{DateTime.Now}: {ex}\n\n");
            throw;
        }
    });

    app.Use(async (context, next) => {
        Console.WriteLine($"[REQUEST] {context.Request.Method} {context.Request.Path}");
        await next();
    });

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }
    app.UseDefaultFiles();
    app.UseStaticFiles();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new MyHangfireAuthorizationFilter() }
    });

    // Tự động Migration và Seed Data
    using (var scope = app.Services.CreateScope())
    {
        try {
            var context = scope.ServiceProvider.GetRequiredService<BAUDbContext>();
            
            // 1. Aggressive Raw SQL Schema Update (Must run BEFORE any EF queries)
            Console.WriteLine("Checking for missing AI columns...");
            var conn = context.Database.GetDbConnection();
            await conn.OpenAsync();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PlagiarismChecks') AND name = 'AiProbability')
                    BEGIN
                        ALTER TABLE PlagiarismChecks ADD AiProbability DECIMAL(5,2) NULL;
                        ALTER TABLE PlagiarismChecks ADD AiDetectionLevel NVARCHAR(50) NULL;
                        ALTER TABLE PlagiarismChecks ADD AiDetectionJson NVARCHAR(MAX) NULL;
                    END";
                await cmd.ExecuteNonQueryAsync();
            }
            Console.WriteLine("AI Columns check completed.");

            // 2. Standard Migrations
            Console.WriteLine("Applying database migrations...");
            await context.Database.MigrateAsync();
            Console.WriteLine("Database migrations applied successfully.");
            
            // Seed data only if needed
            if (!context.Faculties.Any())
            {
                Console.WriteLine("Seeding initial data...");
                await BAU_Plagiarism_System.API.Data.SeedData.SeedAsync(context);
                
                Console.WriteLine("Seeding 100+ more documents for testing...");
                await BAU_Plagiarism_System.API.Data.MassiveDataSeeder.SeedMassiveAsync(context, 100);
                
                Console.WriteLine("All seeding completed successfully.");
            }
            else {
                Console.WriteLine("Database already seeded, skipping startup tasks.");
            }
        } catch (Exception ex) {
            Console.WriteLine($"Database/Seed Error: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        }
    }

    app.Run();
}
catch (Exception ex)
{
    File.WriteAllText("startup_error.log", ex.ToString());
    Console.WriteLine("STARTUP ERROR: " + ex.Message);
    throw;
}

public class MyHangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // In production, you should check for admin role here
        // For development, we allow all
        return true;
    }
}
