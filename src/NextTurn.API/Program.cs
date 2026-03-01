using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using NextTurn.API.Middleware;
using NextTurn.Application;
using NextTurn.Infrastructure;
using System.Threading.RateLimiting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ── Register services ─────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// MediatR handlers, FluentValidation validators, ValidationBehavior pipeline.
builder.Services.AddApplication();

// DbContext, repositories, password hasher, HttpTenantContext, JwtTokenService, etc.
builder.Services.AddInfrastructure(builder.Configuration);

// ── JWT bearer authentication ─────────────────────────────────────────────────
// Tells ASP.NET Core how to validate incoming "Authorization: Bearer {token}" headers.
// The signing key, issuer, and audience mirror what JwtTokenService uses to generate tokens.
//
// IMPORTANT: builder.Configuration is read inside the lambda, not captured as a local variable.
// IOptions<JwtBearerOptions> resolves lazily (on first request), so the lambda runs AFTER
// WebApplicationFactory.ConfigureWebHost has injected its test-environment overrides.
// Capturing 'builder.Configuration["JwtSettings:Secret"]' eagerly (outside the lambda) would
// snapshot the empty string from appsettings.json before the test factory adds its providers.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience            = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                                          Encoding.UTF8.GetBytes(
                                              builder.Configuration["JwtSettings:Secret"] ?? string.Empty)),
            ClockSkew                = TimeSpan.Zero, // no grace period — tokens expire exactly at 'exp'
        };
    });

// ── Rate limiting ─────────────────────────────────────────────────────────────
// Sliding window: max 10 requests per 60-second window per client IP.
// Applied selectively via [EnableRateLimiting("login")] on the login endpoint only.
// Returns HTTP 429 when the limit is exceeded.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddSlidingWindowLimiter("login", limiterOptions =>
    {
        limiterOptions.Window            = TimeSpan.FromSeconds(60);
        limiterOptions.SegmentsPerWindow = 6;  // 10-second segments for smoother distribution
        limiterOptions.PermitLimit       = 10;
        limiterOptions.QueueLimit        = 0;  // reject immediately, no queuing
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

// ── Build ─────────────────────────────────────────────────────────────────────
WebApplication app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Exception handlers — must be early so they wrap all downstream middleware.
// DomainException (400) wraps ValidationException (422) because domain errors
// are a subset — ordering doesn't matter here since they catch different types.
app.UseMiddleware<DomainExceptionMiddleware>();
app.UseMiddleware<ValidationExceptionMiddleware>();

// Resolve TenantId from JWT claim 'tid' or X-Tenant-Id header.
// Placed after exception middlewares so tenant errors are also caught properly.
app.UseMiddleware<TenantMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

app.Run();
