using System.Text;
using Microsoft.Extensions.Configuration; 
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Newtonsoft.Json;

// 1. BUILD CONFIGURATION
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory) 
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

string connectionString = config["EventHubConnection"] 
    ?? throw new InvalidOperationException("Missing EventHubConnection");
string eventHubName = config["EventHubName"] 
    ?? throw new InvalidOperationException("Missing EventHubName");
string apiUrl = "http://localhost:5182"; // TwinAPI URL

Console.WriteLine("🏭 SWARM FACTORY SIMULATOR STARTING...");
Console.WriteLine($"Target Event Hub: {eventHubName}");

// 2. FETCH REAL MACHINES (NO GHOSTS)
Console.WriteLine("🔎 Fetching machine registry from TwinAPI...");
var machineIds = new List<Guid>();

try 
{
    using var httpClient = new HttpClient();
    var response = await httpClient.GetStringAsync($"{apiUrl}/machines");
    
    // Deserialize dynamic to just grab IDs
    var machines = JsonConvert.DeserializeObject<List<dynamic>>(response);
    
    if (machines != null)
    {
        foreach (var m in machines)
        {
            // Json.NET dynamic object handling
            string idStr = m.id; 
            string name = m.name;
            Console.WriteLine($"   -> Found: {name} ({idStr})");
            machineIds.Add(Guid.Parse(idStr));
        }
    }
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"❌ CRITICAL: Could not fetch machines from API. Is TwinAPI running?");
    Console.WriteLine($"   Error: {ex.Message}");
    Console.ResetColor();
    return; // Stop app if we can't get real machines
}

if (machineIds.Count == 0)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("⚠️ No machines found in database! Please use Swagger to POST /machines first.");
    Console.ResetColor();
    return;
}

Console.WriteLine($"✅ Loaded {machineIds.Count} active machines.");
Console.WriteLine("🚀 Starting Telemetry Stream. Press Ctrl+C to stop.\n");

// 3. START SIMULATION LOOP
var random = new Random();
await using var producerClient = new EventHubProducerClient(connectionString, eventHubName);

while (true)
{
    using EventDataBatch eventBatch = await producerClient.CreateBatchAsync();

    for (int i = 0; i < 10; i++) 
    {
        // Pick a RANDOM REAL MACHINE from the list
        var targetMachineId = machineIds[random.Next(machineIds.Count)];

        // Generate Random Telemetry
        var isOverheating = random.Next(0, 20) == 0; // 5% chance
        var temp = isOverheating ? random.Next(91, 110) : random.Next(60, 85);

        var telemetry = new
        {
            machineId = targetMachineId,
            sensorType = "Temperature",
            value = (double)temp,
            unit = "Celsius",
            timestamp = DateTimeOffset.UtcNow
        };

        var json = JsonConvert.SerializeObject(telemetry);
        
        if (!eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(json)))) break;
        
        if (isOverheating) 
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ALERT] {targetMachineId} TEMP: {temp}°C");
            Console.ResetColor();
        }
    }

    try 
    {
        await producerClient.SendAsync(eventBatch);
        Console.Write("."); 
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error sending batch: {ex.Message}");
    }

    await Task.Delay(1000);
}