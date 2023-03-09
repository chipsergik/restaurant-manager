using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Options;
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
                   .AddJsonFile(
                        $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json",
                        true)
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

    builder.Services.AddOptions<TablesOptions>()
           .Bind(builder.Configuration.GetSection(TablesOptions.Name))
           .Validate(config =>
                {
                    if (config == null)
                    {
                        return false;
                    }

                    var orderedDistinctTableSizes = config.Sizes.Distinct().OrderBy(size => size);
                    for (var expectedSize = 2; expectedSize <= 6; expectedSize++)
                    {
                        if (!orderedDistinctTableSizes.Any(size => size == expectedSize))
                        {
                            return false;
                        }
                    }

                    return true;
                },
                "Tables should contain every size in range from 2 to 6")
           .ValidateOnStart();

    builder.Services.AddSingleton<ITablesRepository, TablesInMemoryRepository>(
        serviceProvider => new TablesInMemoryRepository(
            tables: serviceProvider.GetRequiredService<IOptions<TablesOptions>>().Value.Tables.ToList()
        ));

    builder.Services.AddHostedService<QueuedHostedService>();

    builder.Services.AddSingleton<IRestaurantManager, RestaurantManagerService>();

    builder.Services.AddOptions<BackgroundTaskQueueOptions>()
           .Bind(builder.Configuration.GetSection(BackgroundTaskQueueOptions.Name))
           .ValidateDataAnnotations()
           .ValidateOnStart();
    
    builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

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