var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

app.Use(async (context, next) =>
{
    if (!context.Request.Headers.ContainsKey("X-Correlation-Id"))
    {
        context.Request.Headers["X-Correlation-Id"] = Guid.NewGuid().ToString("N");
    }

    context.Response.Headers["X-Correlation-Id"] = context.Request.Headers["X-Correlation-Id"].ToString();
    await next();
});

var health = () => Results.Ok(new { status = "ok", service = "CarRental.API" });
app.MapGet("/health", health);
app.MapGet("/api/health", health);

app.Run();
