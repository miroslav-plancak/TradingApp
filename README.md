# TradingApp.API

Simulation of an event-driven trading app using the Outbox Pattern with Azure Service Bus.

## Architecture

- **ASP.NET Core API** - Order endpoints
- **SQL Server** - Orders, OutboxMessages, DeadLetterLogs
- **Azure Service Bus** - `CREATE_ORDER_QUEUE`
- **Azure Functions** (isolated, .NET 8):
  - `OrderExecutionProvider` - Service Bus consumer
  - `ScheduledOutboxMessageProcessor` - Outbox dispatcher (1 min)
  - `ScheduledOrderStatusProcessor` - Status timer
  - `DeadLetterQueueProcessor` - DLQ handler
- **UI** - Single-file HTML testing interface

## Setup

1. **Clone repo**
2. **Run `Database/TradingApp_Setup.sql`** to create local DB (update connection string in `TradingApp.API/appsettings.json` if SQL instance differs from `.\SQLEXPRESS`)
3. **Sign into Visual Studio** with your Microsoft account (needed for `DefaultAzureCredential` → Key Vault)
4. **Send me your email** so I can:
   - Invite you as guest to Azure AD tenant
   - Grant Key Vault access policy (Get, List on secrets) on `tradingapp-demo-kv`
5. **Configure multiple startup projects** (all 4 Functions + API → Start)
6. **F5** and open `UI/TradingAppUI.html`

## Notes

- Key Vault holds `SqlConnectionString`, `ServiceBusConnectionString`, `StorageConnectionString`
- All Functions resolve secrets via `DefaultAzureCredential` (VS sign-in is sufficient)
- Service Bus and Key Vault are shared - running both our instances simultaneously will cause queue contention and race conditions across our local DBs

## Key Patterns

Outbox pattern, event-driven via Service Bus, idempotent consumers (atomic UPDATE with `WHERE IsProcessed = 0`), DLQ handling.

## Project Structure

```
TradingApp/
├── Database/               # SQL setup
├── Functions/              # 4 isolated worker Functions
├── TradingApp.API/         # ASP.NET Core API
├── TradingApp.Business/    # Services, repos, DTOs
├── TradingApp.Domain/      # Entities, DbContext
└── UI/                     # Testing interface
```