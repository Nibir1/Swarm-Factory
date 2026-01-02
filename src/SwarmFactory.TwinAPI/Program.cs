using Microsoft.Azure.Cosmos;
using SwarmFactory.TwinAPI.Models;
using SwarmFactory.TwinAPI.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------------------
// 1. Add Services
// ----------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// FIX: Add CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

// ----------------------------------------------------------------
// 2. Azure Cosmos DB Configuration
// ----------------------------------------------------------------
var cosmosConnectionString = builder.Configuration["CosmosConnection"];
if (string.IsNullOrEmpty(cosmosConnectionString))
{
    Console.WriteLine("WARNING: CosmosConnection is missing.");
}
else
{
    builder.Services.AddSingleton<ICosmosDbService>(sp =>
    {
        var client = new CosmosClient(cosmosConnectionString);
        return new CosmosDbService(client, "SwarmDb");
    });
}

// ----------------------------------------------------------------
// 3. Build App
// ----------------------------------------------------------------
var app = builder.Build();

// Enable Swagger in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// FIX: Enable CORS (Must be before MapGet/MapPost)
app.UseCors("AllowAll");

app.UseHttpsRedirection();

// ----------------------------------------------------------------
// 4. API ENDPOINTS
// ----------------------------------------------------------------

// GET /machines
app.MapGet("/machines", async (ICosmosDbService db) =>
{
    var machines = await db.GetMachinesAsync();
    return Results.Ok(machines);
})
.WithName("GetMachines");

// POST /machines
app.MapPost("/machines", async (MachineInput input, ICosmosDbService db) =>
{
    var newMachine = new Machine
    {
        Name = input.Name,
        Type = input.Type,
        Location = input.Location,
        Status = MachineStatus.Online,
        LastMaintenanceDate = DateTimeOffset.UtcNow
    };

    await db.AddMachineAsync(newMachine);
    return Results.Created($"/machines/{newMachine.Id}", newMachine);
})
.WithName("CreateMachine");

// GET /machines/{id}
app.MapGet("/machines/{id}", async (string id, ICosmosDbService db) =>
{
    var machine = await db.GetMachineAsync(id);
    return machine is not null ? Results.Ok(machine) : Results.NotFound();
})
.WithName("GetMachineById");

// GET /alerts
app.MapGet("/alerts", async (string? status, ICosmosDbService db) =>
{
    var alerts = await db.GetAlertsAsync(status);
    return Results.Ok(alerts);
})
.WithName("GetAlerts");

// ----------------------------------------------------------------
// 5. Run App
// ----------------------------------------------------------------
app.Run();