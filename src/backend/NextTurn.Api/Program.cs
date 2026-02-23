var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<NextTurn.Api.Repositories.OrganizationRepository>();

var app = builder.Build();

// Swagger enabled in ALL environments, but no automatic redirect
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapControllers();
app.MapGet("/health", () => Results.Ok("OK"));
app.MapGet("/", () => Results.Ok("NextTurn API is running"));

app.Run();