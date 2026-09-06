using FileProcessingService.Security;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
var apiKey = app.Configuration[ApiKeyMiddleware.ApiKeyPath];
if (string.IsNullOrEmpty(apiKey))
{
    app.Logger.LogCritical(
        "No API key configured. Set APIKey__Value via user secrets or an environment variable.");
    throw new InvalidOperationException(
        $"{ApiKeyMiddleware.ApiKeyPath} must be configured.");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//insert middleware here making sure it never reacher controller when request unauthenticated
app.UseMiddleware<ApiKeyMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
