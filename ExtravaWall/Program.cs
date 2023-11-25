using OpenTelemetry.Metrics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using ExtravaWall;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Extrava API", Version = "v1" });
});
builder.Services.AddOpenTelemetry()
    //.ConfigureResource(resource => resource.AddService(serviceName: app.Environment.ApplicationName))
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter()
        .AddMeter("Microsoft.AspNetCore.Hosting",
                         "Microsoft.AspNetCore.Server.Kestrel")
        .AddView("http.server.request.duration",
            new ExplicitBucketHistogramConfiguration
            {
                Boundaries = new double[] { 0, 0.005, 0.01, 0.025, 0.05,
                       0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10 }
            });
    });
builder.Services.AddSingleton<ExtravaMetrics>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPrometheusScrapingEndpoint();

app.Use(async (context, next) =>
{
    var tagsFeature = context.Features.Get<IHttpMetricsTagsFeature>();
    if (tagsFeature != null)
    {
        var source = context.Request.Query["utm_medium"].ToString() switch
        {
            "" => "none",
            "social" => "social",
            "email" => "email",
            "organic" => "organic",
            _ => "other"
        };
        tagsFeature.Tags.Add(new KeyValuePair<string, object?>("mkt_medium", source));
    }

    await next.Invoke();
});

app.MapGet("/", (ExtravaMetrics metrics) =>
{
    metrics.PacketInspected("tcp", 1);
    return "Hello OpenTelemetry! ticks:" + DateTime.Now.Ticks.ToString()[^3..];
});

app.Run();