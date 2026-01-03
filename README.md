# Swarm-Factory: AI-Architected Digital Twin Platform

![Status](https://img.shields.io/badge/status-active-success)
![Methodology](https://img.shields.io/badge/methodology-Spec--Driven_%7C_Agentic--Swarm-purple)
![Tech Stack](https://img.shields.io/badge/azure-.NET%208-blue)

**Swarm-Factory** is a cloud-native, event-driven Digital Twin platform for the manufacturing industry. It simulates high-frequency IoT telemetry, processes data streams in real-time using Serverless functions, and visualizes fleet status in a "Mission Control" React dashboard.

---

## AI-Driven Development Methodology
> **Note for Reviewers:** This project demonstrates an **AI-First Architecture** approach, moving beyond simple code generation to **Spec-Driven Development (SDD)** and **Agentic Workflows**.

The "AI" in this project is the **Architectural Process itself**. Instead of manually writing boilerplate, the development followed a strict "Human-in-the-Loop" Swarm Architecture:

### 1. Spec-Driven Core (The "Source of Truth")
Before a single line of C# or TypeScript was written, the entire system behavior was locked down using industry-standard contracts:
* **OpenAPI 3.0 (`specs/factory-api.yaml`):** Defines the rigid schema for Machine State and Alerts.
* **AsyncAPI 2.6 (`specs/iot-events.yaml`):** Defines the binary payload structure for the high-frequency Telemetry Stream.

### 2. Agentic Swarm Workflow
We treated the implementation as a task for specialized AI Agents, orchestrated by the Architect:

| Agent Role | Input Context | Output Artifact | Result |
| :--- | :--- | :--- | :--- |
| **Backend Agent** | OpenAPI Spec + .NET 8 Patterns | `SwarmFactory.TwinAPI` | **Type-Safe Models:** C# Records generated strictly from YAML schemas. |
| **Infra Agent** | Azure Constraints + AsyncAPI | `infra/main.bicep` | **Infrastructure as Code:** Zero-click deployment of Event Hubs & Cosmos DB. |
| **Frontend Agent** | OpenAPI Schema + Material UI | `frontend/src/types.ts` | **Contract Compliance:** TS Interfaces perfectly matching the Backend. |
| **QA Agent** | Telemetry Spec + Load Patterns | `SwarmFactory.Simulator` | **Smart Simulation:** Load generator that respects strict data contracts. |

---

## Technical Architecture

The solution uses a **Cloud-Native, Event-Driven Architecture (EDA)** optimized for Azure PaaS.

```mermaid
graph TD
    Sim[IoT Simulator] -->|Telemetry Stream (AMQP)| EH[Azure Event Hubs]
    EH -->|Trigger| Func[Azure Function (Processor)]
    Func -->|JSON Analysis| Cosmos[Azure Cosmos DB]
    
    API[TwinAPI (.NET 8)] -->|Read State| Cosmos
    
    User[React Dashboard] -->|Polls| API
```

### Tech Stack
* **Core:** .NET 8 (C#)
* **Compute:** Azure Functions (Isolated Worker), ASP.NET Core Web API
* **Data:** Azure Cosmos DB (NoSQL), Azure Event Hubs (Kafka/AMQP)
* **Frontend:** React (Vite) + TypeScript + Material UI (MUI v6)
* **Infrastructure:** Azure Bicep (IaC)

---

## Getting Started (The Manual)

Follow these steps to run the full Swarm-Factory platform on your local machine.

### 1. Prerequisites
* **Azure Account** (Free tier works)
* **Azure CLI** (`az login`)
* **.NET 8 SDK**
* **Node.js 18+**
* **Azure Functions Core Tools v4** (`npm install -g azure-functions-core-tools@4`)
* **Make** (Optional, but recommended for orchestration)

### 2. Infrastructure Setup
First, provision the Azure resources. The Bicep templates will create the Event Hub and Database.

```bash
# Login to Azure
az login

# Deploy Infrastructure (Auto-creates Resource Group 'rg-swarm-factory')
make resource-group
make infra
```

### 3. Configuration
The project follows strict security practices. **Secrets are not stored in Git.**
You must create local configuration files based on the Azure outputs.

**A. Backend Secrets (`src/SwarmFactory.TwinAPI/appsettings.Development.json`)**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "CosmosConnection": "<YOUR_COSMOS_CONNECTION_STRING>"
}
```

**B. Simulator Secrets (`src/SwarmFactory.Simulator/appsettings.json`)**
```json
{
  "EventHubConnection": "<YOUR_EVENTHUB_CONNECTION_STRING>",
  "EventHubName": "telemetry"
}
```

**C. Processor Secrets (`src/SwarmFactory.Processor/local.settings.json`)**
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "CosmosConnection": "<YOUR_COSMOS_CONNECTION_STRING>",
    "EventHubConnection": "<YOUR_EVENTHUB_CONNECTION_STRING>"
  }
}
```

*Tip: You can retrieve connection strings using `make get-cosmos-conn` and `make get-evh-conn`.*

---

## Running the Swarm
The platform requires **5 separate terminal windows** to run all components concurrently.

### Terminal 1: Infrastructure Emulator
Starts the local Azure Storage emulator (required for Azure Functions).
```bash
make run-azurite
```

### Terminal 2: The "Brain" (Web API)
Serves the REST API for Machine State and Alerts.
```bash
make run-api
# Runs on: http://localhost:5182
```

### Terminal 3: The "Nervous System" (Azure Function)
Processes the high-speed telemetry stream.
```bash
make run-processor
```

### Terminal 4: The "Heartbeat" (IoT Simulator)
Generates realistic load (Temperature/Vibration data).
*Note: The simulator is "Smart"—it fetches real machine IDs from the API before generating data.*
```bash
make run-simulator
```

### Terminal 5: The "Face" (Frontend)
Launches the Mission Control Dashboard.
```bash
make run-frontend
# Opens: http://localhost:5173
```

---

## Project Structure

```text
swarm-factory/
├── specs/                  # The Contracts (OpenAPI / AsyncAPI)
├── infra/                  # Infrastructure as Code (Bicep)
├── src/
│   ├── SwarmFactory.TwinAPI/    # .NET 8 Web API (State Management)
│   ├── SwarmFactory.Processor/  # Azure Function (Event Processing)
│   └── SwarmFactory.Simulator/  # Console App (Load Generator)
├── frontend/               # React + Vite + MUI Dashboard
└── Makefile                # Orchestration Scripts
```

---

## Troubleshooting

**1. Simulator crash: "No machines found"**
The Simulator requires *real* machines to exist in the database.
* **Fix:** Open Swagger (`http://localhost:5182/swagger`) and use `POST /machines` to register at least one machine (e.g., "Extruder-Alpha").

**2. Frontend: "Network Error"**
The React app cannot reach the .NET API.
* **Fix:** Ensure `run-api` is running and the port in `frontend/src/App.tsx` (`API_URL`) matches the output of Terminal 1 (usually `http://localhost:5182`).

**3. Azure Function: "Listener validation failed"**
The function cannot connect to the local storage emulator.
* **Fix:** Ensure you ran `make run-azurite` or have the Azurite extension running in VS Code.

---

**Architect:** Nahasat Nibir
**License:** MIT