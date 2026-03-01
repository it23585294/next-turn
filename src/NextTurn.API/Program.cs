using NextTurn.API.Middleware;
using NextTurn.Application;
using NextTurn.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ── Register services ─────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// MediatR handlers, FluentValidation validators, ValidationBehavior pipeline.
builder.Services.AddApplication();

// DbContext, repositories, password hasher, HttpTenantContext, etc.
builder.Services.AddInfrastructure(builder.Configuration);

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

app.UseAuthorization();
app.MapControllers();

app.Run();
