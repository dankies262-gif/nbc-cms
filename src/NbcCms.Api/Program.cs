using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NbcCms.Api.Data;
using NbcCms.Api.Data.Entities;
using NbcCms.Api.Security;
using NbcCms.Api.Services;
using NbcCms.Api.Services.Interfaces;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (Encoding.UTF8.GetByteCount(jwt.Key) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key must be configured with at least 32 bytes of signing material. " +
        "Set it in appsettings.Local.json, the Jwt__Key environment variable or dotnet user-secrets.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                await Results.Problem(
                    title: "Not authorised",
                    detail: "Sign in through POST /api/auth/login and send the bearer token.",
                    statusCode: StatusCodes.Status401Unauthorized).ExecuteAsync(context.HttpContext);
            },
            OnForbidden = async context =>
            {
                await Results.Problem(
                    title: "Forbidden",
                    detail: "Your account does not have access to this resource.",
                    statusCode: StatusCodes.Status403Forbidden).ExecuteAsync(context.HttpContext);
            }
        };
    });

builder.Services
    .AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
    .AddPolicy("Staff", policy => policy.RequireRole(AppRoles.Staff))
    .AddPolicy("ArtistOnly", policy => policy.RequireRole(AppRoles.Artist));

builder.Services.AddCors(options =>
    options.AddPolicy("Clients", policy => policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod()));

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi();

builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ClaimService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<UserRoleService>();
builder.Services.AddScoped<ArtistBankingDetailsService>();
builder.Services.AddScoped<ISupportingDocumentService, SupportingDocumentService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IOverallAmountService, OverallAmountService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("NbcCms.Api");
    await DataSeeder.SeedAsync(db, logger);
}

app.UseExceptionHandler();
app.UseCors("Clients");
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi().AllowAnonymous();
app.MapControllers();

app.Run();
