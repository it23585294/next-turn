using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using NextTurn.API.Middleware;
using NextTurn.API.OpenApi;
using NextTurn.Application;
using NextTurn.Infrastructure;
using Scalar.AspNetCore;
using System.Threading.RateLimiting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ── Register services ─────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serialize enum values as their string names (e.g. "Active" not 0).
        // Required for the frontend to receive intelligible values like QueueStatus.
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

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
        // Disable ASP.NET Core's default claim-type renaming so the raw JWT claim
        // names (e.g. "role", "sub") are preserved. Without this, "role" gets
        // remapped to the long WS-Federation claim URI, breaking RequireClaim("role").
        options.MapInboundClaims = false;

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
            // Map the short "role" claim to the identity's RoleClaimType so that
            // [Authorize(Roles = "OrgAdmin,...")] and ClaimsPrincipal.IsInRole() work
            // correctly even though MapInboundClaims = false preserves the short name.
            RoleClaimType            = "role",
        };
    });

// ── Authorization policies ──────────────────────────────────────────────────────
// Named policies backed by the "role" JWT claim (preserved as-is via MapInboundClaims = false).
// Hierarchy: SystemAdmin ⊃ OrgAdmin ⊃ Staff ⊃ User.
// A global FallbackPolicy requires every endpoint to be authenticated unless
// explicitly decorated with [AllowAnonymous] (register, login).
builder.Services.AddAuthorization(options =>
{
    // Any authenticated user
    options.AddPolicy("IsUser", policy =>
        policy.RequireAuthenticatedUser()
              .RequireClaim("role", "User", "Staff", "OrgAdmin", "SystemAdmin"));

    // Staff and above
    options.AddPolicy("IsStaff", policy =>
        policy.RequireAuthenticatedUser()
              .RequireClaim("role", "Staff", "OrgAdmin", "SystemAdmin"));

    // Org admins and above
    options.AddPolicy("IsOrgAdmin", policy =>
        policy.RequireAuthenticatedUser()
              .RequireClaim("role", "OrgAdmin", "SystemAdmin"));

    // Platform-wide admin only
    options.AddPolicy("IsSystemAdmin", policy =>
        policy.RequireAuthenticatedUser()
              .RequireClaim("role", "SystemAdmin"));

    // Global fallback: every endpoint requires a valid JWT unless [AllowAnonymous] is present.
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
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
    // Raw OpenAPI JSON spec: /openapi/v1.json
    // AllowAnonymous: the global FallbackPolicy would otherwise block these dev-only routes.
    app.MapOpenApi().AllowAnonymous();

    // Scalar interactive UI: /scalar/v1
    // Reads the spec from /openapi/v1.json automatically.
    app.MapScalarApiReference(options =>
    {
        options.Title            = "NextTurn API";
        options.DefaultHttpClient = new(ScalarTarget.Http, ScalarClient.HttpClient);
    }).AllowAnonymous();
}

app.UseHttpsRedirection();

// Exception handlers — must be early so they wrap all downstream middleware.
// DomainException (400) wraps ValidationException (422) because domain errors
// are a subset — ordering doesn't matter here since they catch different types.
app.UseMiddleware<DomainExceptionMiddleware>();
app.UseMiddleware<ValidationExceptionMiddleware>();

app.UseAuthentication();

// Resolve TenantId from JWT claim 'tid' or X-Tenant-Id header.
// Must run AFTER UseAuthentication() so context.User is populated from the JWT
// before TenantMiddleware attempts to read the 'tid' claim.
app.UseMiddleware<TenantMiddleware>();

app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

app.Run();
