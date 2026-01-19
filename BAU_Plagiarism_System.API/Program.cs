using Microsoft.EntityFrameworkCore;
using BAU_Plagiarism_System.Data;
using BAU_Plagiarism_System.Core.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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

    // Register Business Services
    builder.Services.AddScoped<SimilarityChecker>();
    builder.Services.AddScoped<DocumentReader>();
    builder.Services.AddScoped<TextProcessor>();

    // Register Application Services
    builder.Services.AddScoped<CatalogService>();
    builder.Services.AddScoped<UserService>();
    builder.Services.AddScoped<DocumentService>();
    builder.Services.AddScoped<PlagiarismService>();
    builder.Services.AddScoped<ImportService>();

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
    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }
    app.UseDefaultFiles();
    app.UseStaticFiles();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Tự động Migration và Seed Data
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<BAUDbContext>();
        try {
            // Apply migrations (or create database if not exists)
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
