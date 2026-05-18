# TradingApp.API

Simulation of an event-driven trading app using the Outbox Pattern, Azure Service Bus queues and topics, and a multi-layered reliability system.

## Architecture

**ASP.NET Core API** — order creation endpoint, full CRUD for orders, outbox messages, dead letters, and quarantined messages.

**SQL Server** — single source of truth with 4 tables:
- `Orders` — trading orders with full lifecycle status
- `OutboxMessages` — transactional outbox with retry classification
- `DeadLetterLogs` — Service Bus DLQ messages for operator triage
- `QuarantinedOutboxMessages` — outbox messages that exhausted retries, classified by failure reason

**Azure Service Bus:**
- Queue: `CREATE_ORDER_QUEUE` — command pattern, competing consumers
- Topic: `order_events_topic` — pub/sub fan-out with 3 subscriptions (`risk-analysis`, `notifications`, `audit-log`)

**Azure Functions** (isolated worker, .NET 8):
- `OrderExecutionProvider` — consumes `CREATE_ORDER_QUEUE`, atomically updates order status, publishes `OrderProcessed` event to topic
- `ScheduledOutboxMessageProcessor` — runs every minute, dispatches outbox messages to Service Bus with 3-phase processing (quarantine → dispatch → auto-recover)
- `ScheduledOrderStatusProcessor` — promotes `ACKNOWLEDGED` orders to `FILLED` on a timer
- `DeadLetterQueueProcessor` — consumes the DLQ, persists failures to `DeadLetterLogs`
- `RiskAnalysisProcessor` — topic subscriber (`risk-analysis`)
- `NotificationsProcessor` — topic subscriber (`notifications`)
- `AuditLogProcessor` — topic subscriber (`audit-log`)

**Shared Library** — `TradingApp.Events` contains shared event/payload contracts used across all functions.

**UI** — `TradingAppUI.html`, single-file testing dashboard with 5 themes, scenarios, and a purge database control.

## Setup

1. **Clone repo**
2. **Run `Database/TradingApp_Setup.sql`** against your local SQL Server. If your instance name differs from `.\SQLEXPRESS`, update the connection string in `TradingApp.API/appsettings.json`
3. **Sign into Visual Studio** with your Microsoft account — `DefaultAzureCredential` uses this to authenticate to Key Vault, no `az login` needed
4. **Send me your email** so I can invite you to the Azure AD tenant and grant Key Vault access policy (`Get`, `List` on secrets) on `tradingapp-demo-kv`
5. **Configure multiple startup projects** — set all 7 Functions + API to `Start`
6. **F5** and open `UI/TradingAppUI.html` in your browser

## Key Vault Secrets

| Secret | Purpose |
|--------|---------|
| `SqlConnectionString` | Local SQL Server connection |
| `ServiceBusConnectionString` | Azure Service Bus namespace |
| `StorageConnectionString` | Azure Storage for Functions runtime |

## Key Patterns

**Outbox pattern** — order creation writes `Order` + `OutboxMessage` in a single DB transaction. No message is ever published without a persisted record first.

**Idempotent consumers** — `OrderExecutionProvider` uses `ExecuteUpdateAsync` with `WHERE IsProcessed = 0`, making concurrent processing safe without distributed locks.

**Classified retry with auto-recovery** — `ScheduledOutboxMessageProcessor` runs in 3 phases each tick:
1. **Quarantine** — messages with `RetryCount >= 5` move to `QuarantinedOutboxMessages` with a typed reason (`ServiceBusUnavailable`, `InvalidPayload`, `Unknown`)
2. **Dispatch** — pending messages are batch-queried and published; failure increments retry count with a classified reason
3. **Auto-recover** — if at least one publish succeeded (proving Service Bus is healthy), all quarantined `ServiceBusUnavailable` messages are resurrected for one more attempt; `InvalidPayload` messages stay quarantined for human triage

**Queue vs topic** — `CREATE_ORDER_QUEUE` uses the command pattern (one consumer processes each message). `order_events_topic` uses pub/sub fan-out (all 3 subscribers independently receive every event).

**DLQ handling** — messages that exhaust Service Bus delivery attempts flow to `DeadLetterQueueProcessor`, which persists them to `DeadLetterLogs` for operator resolution.

## Notes

- Key Vault and Service Bus are shared resources — running two instances simultaneously causes queue contention across separate local databases
- `DefaultAzureCredential` tries Visual Studio credentials first; `az login` is a fallback if VS auth doesn't work
- The `TradingApp.Events` shared library holds `OrderProcessedEvent` and `OrderPayload` — reference it from any new function that needs these contracts

## Project Structure

```
TradingApp/
├── Database/
│   └── TradingApp_Setup.sql            # Full schema for all 4 tables
├── Functions/
│   ├── OrderExecutionProvider/         # Service Bus queue consumer + topic publisher
│   ├── ScheduledOutboxMessageProcessor/# 3-phase outbox dispatcher
│   ├── ScheduledOrderStatusProcessor/  # ACK → FILLED promotion timer
│   ├── DeadLetterQueueProcessor/       # DLQ consumer
│   ├── RiskAnalysisProcessor/          # Topic subscriber
│   ├── NotificationsProcessor/         # Topic subscriber
│   └── AuditLogProcessor/              # Topic subscriber
├── TradingApp.API/                     # ASP.NET Core REST API
├── TradingApp.Business/                # Services, repositories, DTOs, mappers
├── TradingApp.Domain/                  # Entities, enums, DbContext
├── TradingApp.Events/                  # Shared event/payload contracts
└── UI/
    └── TradingAppUI.html               # Single-file testing dashboard
```

## Enum Reference

```
OrderStatus:         0 = PENDING_ACK  1 = ACKNOWLEDGED  2 = REJECTED  3 = FILLED
OutboxRetryReason:   0 = None  1 = ServiceBusUnavailable  2 = InvalidPayload  3 = DatabaseError  4 = Unknown
```
