using AntiPlagiarism.FileAnalysisService.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "File Analysis Service API", Version = "v1" });
});

// Configure HttpClientFactory
builder.Services.AddHttpClient("FSSClient", client =>
{
    // Base address will be set from configuration or used directly in calls
});
builder.Services.AddHttpClient("QuickChartClient", client =>
{
    // client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("QuickChart:ApiUrl")); // Not needed if full URL is used in PostAsJsonAsync
});


builder.Services.AddSingleton<ITextAnalyzer, TextAnalyzer>();
builder.Services.AddSingleton<IAnalysisRepository, InMemoryAnalysisRepository>();

builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConsole();
    loggingBuilder.AddDebug();
});


var app = builder.Build();

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "File Analysis Service API v1_dev"));
}

// app.UseHttpsRedirection(); // Not using HTTPS for local demo simplicity
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();
