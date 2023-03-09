using RestaurantManager.API.Interfaces;
using RestaurantManager.API.Models;
using RestaurantManager.API.Persistence;
using RestaurantManager.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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

var app = builder.Build();

app.UseHttpLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();