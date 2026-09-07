using FileProcessingService.Controllers;
using FileProcessingService.Security;
using FileProcessingService.Services;
using Microsoft.AspNetCore.Http.Features;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

//for the purpose of serializeing enums as names so error codes won't just display as a number but rather
//displaying errorCode as strings (eg. "ColumnNotFound")
builder.Services.AddControllers().
    AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
//Keeping the multipart limit alighed with the controller's own check so oversized uploaded files will be caught by a JSOn error rather than a framework exception
builder.Services.Configure<FormOptions>(options =>
    options.MultipartBodyLengthLimit = FilesController.MaxUploadBytes);
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<ICsvProcessor, CsvProcessor>();
builder.Services.AddSingleton<IProcessingTracker, InMemoryProcessingTracker>();

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
    //redirecting would break every request.
    app.UseHttpsRedirection();
}



//insert middleware here making sure it never reacher controller when request unauthenticated
app.UseMiddleware<ApiKeyMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
