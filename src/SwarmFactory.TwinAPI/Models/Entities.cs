using System;
using SystemTextJson = System.Text.Json.Serialization;
using NewtonsoftJson = Newtonsoft.Json; 
using NewtonsoftConverters = Newtonsoft.Json.Converters; // Added alias for clarity

namespace SwarmFactory.TwinAPI.Models
{
    // ----------------------------------------------------------------
    // ENUMS
    // ----------------------------------------------------------------
    // System.Text.Json attributes handle the Web API JSON (HTTP)
    [SystemTextJson.JsonConverter(typeof(SystemTextJson.JsonStringEnumConverter))]
    public enum MachineType
    {
        RobotArm,
        ConveyorBelt,
        Extruder,
        Press
    }

    [SystemTextJson.JsonConverter(typeof(SystemTextJson.JsonStringEnumConverter))]
    public enum MachineStatus
    {
        Online,
        Offline,
        Maintenance,
        Error
    }

    [SystemTextJson.JsonConverter(typeof(SystemTextJson.JsonStringEnumConverter))]
    public enum AlertSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    [SystemTextJson.JsonConverter(typeof(SystemTextJson.JsonStringEnumConverter))]
    public enum AlertStatus
    {
        New,
        Acknowledged,
        Resolved
    }

    // ----------------------------------------------------------------
    // ENTITIES
    // ----------------------------------------------------------------

    // Maps to Cosmos Container: 'machines'
    public record Machine
    {
        [NewtonsoftJson.JsonProperty("id")]
        public string Id { get; init; } = Guid.NewGuid().ToString();

        [NewtonsoftJson.JsonProperty("name")]
        public string Name { get; init; } = string.Empty;

        // FIX: Force Newtonsoft to serialize this as a String ("RobotArm") instead of Int (0)
        [NewtonsoftJson.JsonProperty("type")]
        [NewtonsoftJson.JsonConverter(typeof(NewtonsoftConverters.StringEnumConverter))]
        public MachineType Type { get; init; }

        [NewtonsoftJson.JsonProperty("status")]
        [NewtonsoftJson.JsonConverter(typeof(NewtonsoftConverters.StringEnumConverter))]
        public MachineStatus Status { get; init; } = MachineStatus.Online;

        [NewtonsoftJson.JsonProperty("location")]
        public string Location { get; init; } = string.Empty;

        [NewtonsoftJson.JsonProperty("lastMaintenanceDate")]
        public DateTimeOffset LastMaintenanceDate { get; init; } = DateTimeOffset.UtcNow;
    }

    // Input DTO (Purely for API Input, handled by System.Text.Json)
    public record MachineInput(string Name, MachineType Type, string Location);

    // Maps to Cosmos Container: 'alerts'
    public record Alert
    {
        [NewtonsoftJson.JsonProperty("id")]
        public string Id { get; init; } = Guid.NewGuid().ToString();

        [NewtonsoftJson.JsonProperty("machineId")]
        public string MachineId { get; init; } = string.Empty;

        [NewtonsoftJson.JsonProperty("severity")]
        [NewtonsoftJson.JsonConverter(typeof(NewtonsoftConverters.StringEnumConverter))]
        public AlertSeverity Severity { get; init; }

        [NewtonsoftJson.JsonProperty("message")]
        public string Message { get; init; } = string.Empty;

        [NewtonsoftJson.JsonProperty("timestamp")]
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

        [NewtonsoftJson.JsonProperty("status")]
        [NewtonsoftJson.JsonConverter(typeof(NewtonsoftConverters.StringEnumConverter))]
        public AlertStatus Status { get; init; } = AlertStatus.New;
    }
}