using System.Text;
using System.Text.Json;
using MiniGPTCSharp.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});
builder.Services.AddSingleton<GptMicroscopeService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(new { status = "ok", app = "GPT Microscope" }));
app.MapPost("/api/inspect", (InspectionRequest request, GptMicroscopeService service) =>
    Results.Ok(service.Inspect(request)));
app.MapPost("/api/export-report", (InspectionRequest request, GptMicroscopeService service) =>
{
    var html = service.BuildReportHtml(request);
    return Results.File(Encoding.UTF8.GetBytes(html), "text/html", "gpt-microscope-report.html");
});

app.MapFallbackToFile("index.html");

app.Run();

public partial class Program
{
}
