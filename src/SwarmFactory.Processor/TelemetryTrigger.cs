using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace SwarmFactory.Processor
{
    public class TelemetryTrigger
    {
        private readonly ILogger _logger;

        public TelemetryTrigger(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<TelemetryTrigger>();
        }

        [Function("ProcessTelemetry")]
        [CosmosDBOutput("SwarmDb", "alerts", Connection = "CosmosConnection")]
        public object? Run(
            [EventHubTrigger("telemetry", Connection = "EventHubConnection", ConsumerGroup = "func-processor")] string[] inputMessages,
            FunctionContext context)
        {
            // We process a batch of messages (default is usually up to 100)
            // For simplicity in this demo, we'll return the FIRST alert generated in the batch.
            // In a real production scenario, you'd use IAsyncCollector to yield multiple alerts.

            foreach (var message in inputMessages)
            {
                try
                {
                    // 1. Deserialize
                    var telemetry = JsonConvert.DeserializeObject<TelemetryPayload>(message);

                    // 2. Business Logic: Check Thresholds
                    if (telemetry != null && telemetry.SensorType == "Temperature" && telemetry.Value > 90.0)
                    {
                        _logger.LogWarning($"CRITICAL: Machine {telemetry.MachineId} is overheating at {telemetry.Value}°C");

                        // 3. Create Alert Object (Matches Phase 1 Spec)
                        var alert = new
                        {
                            id = Guid.NewGuid().ToString(),
                            machineId = telemetry.MachineId,
                            severity = "High",
                            message = $"Overheating detected: {telemetry.Value} {telemetry.Unit}",
                            timestamp = DateTimeOffset.UtcNow,
                            status = "New"
                        };

                        // Return immediately (writing one alert per execution for this simple demo)
                        return alert;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to process message: {ex.Message}");
                }
            }

            // No alerts triggered in this batch
            return null;
        }
    }

    // Helper DTO for Deserialization (Matches iot-events.yaml)
    public class TelemetryPayload
    {
        public string MachineId { get; set; } = string.Empty;
        public string SensorType { get; set; } = string.Empty;
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
    }
}