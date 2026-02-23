var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<NextTurn.Api.Repositories.OrganizationRepository>();

var app = builder.Build();

// Enable Swagger in Development OR QA
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("QA"))
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Redirect root to Swagger only in Dev/QA
    app.MapGet("/", () => Results.Redirect("/swagger"));
}
else
{
    // In Production, show a simple status message at root
    app.MapGet("/", () => Results.Ok("NextTurn API is running"));
}

app.UseHttpsRedirection();

app.MapControllers();
app.MapGet("/health", () => Results.Ok("OK"));

app.Run();