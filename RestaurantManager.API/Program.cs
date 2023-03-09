using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using RestaurantManager.API.Configuration;
using RestaurantManager.API.Filters;
using RestaurantManager.API.Interfaces;
using RestaurantManager.API.Models;
using RestaurantManager.API.Persistence;
using RestaurantManager.API.Services;
using Serilog;


var configuration = new ConfigurationBuilder()
                   .SetBasePath(Directory.GetCurrentDirectory())
                   .AddJsonFile("appsettings.json")
                   .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
                   .Build();

Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

try
{
    Log.Information("Starting web host");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // Add services to the container.
    builder.Services.AddControllers(options => { options.Filters.Add<GlobalExceptionFilter>(); });

    builder.Services.AddApiVersioning(opt =>
    {
        opt.DefaultApiVersion = new ApiVersion(1, 0);
        opt.AssumeDefaultVersionWhenUnspecified = true;
        opt.ReportApiVersions = true;
        opt.ApiVersionReader = ApiVersionReader.Combine(new UrlSegmentApiVersionReader(),
            new HeaderApiVersionReader("x-api-version"),
            new MediaTypeApiVersionReader("x-api-version"));
    });

    // Add ApiExplorer to discover versions
    builder.Services.AddVersionedApiExplorer(setup =>
    {
        setup.GroupNameFormat = "'v'VVV";
        setup.SubstituteApiVersionInUrl = true;
    });

    builder.Services.AddSingleton<IClientsGroupsRepository, ClientsGroupsInMemoryRepository>();

    var tables = builder.Configuration
                        .GetSection(TablesOptions.Tables)
                        .Get<int[]>().Select(size => new Table(size))
                        .ToList();

    builder.Services.AddSingleton<ITablesRepository, TablesInMemoryRepository>(
        serviceProvider => new TablesInMemoryRepository(
            tables: tables
        ));

    builder.Services.AddHostedService<QueuedHostedService>();

    builder.Services.AddSingleton<IRestaurantManager, RestaurantManagerService>();

    builder.Services.AddSingleton<IBackgroundTaskQueue>(context =>
    {
        if (!int.TryParse(builder.Configuration["QueueCapacity"], out var queueCapacity))
            queueCapacity = 10;
        return new BackgroundTaskQueue(queueCapacity);
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();

    builder.Services.AddHealthChecks();

    var app = builder.Build();

    app.MapHealthChecks("/hc");

    var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions.Reverse())
            {
                options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                    description.GroupName.ToUpperInvariant());
            }
        });
    }

    app.UseSerilogRequestLogging();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}