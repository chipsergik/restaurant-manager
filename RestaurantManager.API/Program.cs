using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using RestaurantManager.API.Configuration;
using RestaurantManager.API.Filters;
using RestaurantManager.API.Interfaces;
using RestaurantManager.API.Models;
using RestaurantManager.API.Persistence;
using RestaurantManager.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers(options =>
{
    options.Filters.Add<GlobalExceptionFilter>();
});

builder.Services.AddApiVersioning(opt =>
{
    opt.DefaultApiVersion = new ApiVersion(1,0);
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
builder.Services.AddHostedService<QueuedHostedService>();

var tables = builder.Configuration
                    .GetSection(TablesOptions.Tables)
                    .Get<int[]>().Select(size => new Table(size))
                    .ToList();

builder.Services.AddSingleton<IRestaurantManager, RestaurantManagerService>(
    serviceProvider => new RestaurantManagerService(
        tables: tables,
        clientsGroupsRepository: serviceProvider.GetRequiredService<IClientsGroupsRepository>(),
        logger: serviceProvider.GetRequiredService<ILogger<RestaurantManagerService>>(),
        clientsGroupsQueue: Enumerable.Empty<ClientsGroup>()
    ));

builder.Services.AddSingleton<IBackgroundTaskQueue>(context =>
{
    if (!int.TryParse(builder.Configuration["QueueCapacity"], out var queueCapacity))
        queueCapacity = 10;
    return new BackgroundTaskQueue(queueCapacity);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();



var app = builder.Build();

app.UseHttpLogging();

var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();


// Configure the HTTP request pipeline.
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

app.MapControllers();

app.Run();