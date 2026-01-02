using System.Text;
using Microsoft.Extensions.Configuration; // Add this
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Newtonsoft.Json;

// 1. BUILD CONFIGURATION
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

string connectionString = config["EventHubConnection"] 
    ?? throw new InvalidOperationException("Missing EventHubConnection in appsettings.json");
string eventHubName = config["EventHubName"] 
    ?? throw new InvalidOperationException("Missing EventHubName");

// --------------------------------------------------------
// SIMULATION DATA
// --------------------------------------------------------
var machineIds = new[] { 
    Guid.Parse("bd973b4f-3322-4374-b65e-2ce232be8433"), // Extruder-Alpha (from your Phase 3 test)
    Guid.NewGuid(), 
    Guid.NewGuid() 
};

var random = new Random();

Console.WriteLine("🏭 SWARM FACTORY SIMULATOR STARTING...");
Console.WriteLine($"Target: {eventHubName}");
Console.WriteLine("Press Ctrl+C to stop.");

// 1. Create Producer Client
await using var producerClient = new EventHubProducerClient(connectionString, eventHubName);

while (true)
{
    // Create a batch of events
    using EventDataBatch eventBatch = await producerClient.CreateBatchAsync();

    for (int i = 0; i < 10; i++) // Send 10 events per loop
    {
        // 2. Generate Random Telemetry (Matches iot-events.yaml)
        var isOverheating = random.Next(0, 20) == 0; // 5% chance of overheating
        var temp = isOverheating ? random.Next(91, 110) : random.Next(60, 85);

        var telemetry = new
        {
            machineId = machineIds[random.Next(machineIds.Length)],
            sensorType = "Temperature",
            value = (double)temp,
            unit = "Celsius",
            timestamp = DateTimeOffset.UtcNow
        };

        var json = JsonConvert.SerializeObject(telemetry);
        
        // 3. Add to Batch
        if (!eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(json))))
        {
            // Batch is full
            break;
        }
        
        // Visual log for overheating
        if (isOverheating) 
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ALERT] Generating HIGH TEMP: {temp}°C");
            Console.ResetColor();
        }
    }

    // 4. Send Batch to Azure
    try 
    {
        await producerClient.SendAsync(eventBatch);
        Console.Write("."); // Heartbeat dot
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error sending batch: {ex.Message}");
    }

    // Throttle (Sleep 1 second)
    await Task.Delay(1000);
}