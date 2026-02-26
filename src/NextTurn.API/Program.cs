WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// --- Register services ---
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// --- Build ---
WebApplication app = builder.Build();

// --- Configure middleware pipeline ---
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
