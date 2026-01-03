# 🛒 GIC's Take-home assignment: E-Commerce Microservices Demo (.NET 8, Kafka, Docker)
This repository contains a microservices-based e-commerce system that demonstrates clean architecture, event-driven communication using Kafka, structured logging, and a test-driven approach, while intentionally documenting tradeoffs and future extensions.

---

## High-Level Architecture

<img width="1347" height="747" alt="image" src="https://github.com/user-attachments/assets/62ab8e9f-0d95-4e43-a008-87de50679004" />

- User Service (.NET 8 Web API)
- Order Service (.NET 8 Web API)
- Shared Library (.NET 8 Class Library, contains Kafka objects, Middleware, Exceptions)
- Kafka Broker (event-driven communication)
- Automated Tests (xUnit, Moq, Testcontainers)
- Frontend UI (React + Vite + TypeScript)

---

## Services Deep Dive
### 1) User Service
**a) Responsibilities**
- Create and retrieve users
- Publish UserCreated events to Kafka
- Consume OrderCreated events (only logging consumption for now, no business logic or processing)

**b) Endpoints**
- POST /api/v1/users
- GET  /api/v1/users
- GET  /api/v1/users/{id}

**c) Domain model**
```csharp
{
    id: Guid,           // Unique identifier
    name: string,       // User's full name
    email: string       // User's email address, case insensitive
    createdAtUtc: DateTime
}
```

### 2) Order Service

**a) Responsibilities**
- Create orders, retrieve orders per users
- Publish OrderCreated events to Kafka
- Consume UserCreated events (only logging consumption for now, no business logic or processing)

**b) Endpoints**
- POST /api/v1/orders
- GET  /api/v1/orders/{id}
- GET  /api/v1/orders/user/{userId}

**c) Domain model**
```csharp
{
    id: Guid,           // Unique identifier
    userId: Guid,       // Reference to the user who placed the order
    product: string,    // Product name
    quantity: int,      // Number of items
    price: decimal      // Total price
    createdAtUtc: DateTime
}
```

### 3) Clean Architecture
Both services follow a simple clean architecture structure:

**/API** --> Controllers, DTOs

**/Application** --> Service logic, interfaces

**/Domain** --> Entities 

**/Infrastructure** --> EF Core DbContext, repositories

**Shared .NET 8 class library** --> Centralise shared concerns (middleware, kafka objects, exceptions)

### 4) Event-Driven Communication (Kafka)

All Kafka messages use a versioned, explicit contract: **UserCreatedEventV1** or **OrderCreatedEventV1**, wrapped in a **KafkaEvent** with details like versioning, who produced the messages, EventId etc. to enable forward compatibility, traceability, and clear ownership of events. Kafka objects are written in the Shared .NET 8 class library

| Topic              | Producer        | Consumer      | Contract              |
| ------------------ | --------------- | ------------- | --------------------- |
| `user.created.v1`  | User Service    | Order Service | `UserCreatedEventV1`  |
| `order.created.v1` | Order Service   | User Service  | `OrderCreatedEventV1` |
| `*.dlq.v1`         | Shared Consumer | —             | Generic DLQ payload   |

**a) KafkaProducer**
- Singleton

**b) KafkaConsumer**
- Runs as a BackgroundService
- At-least-once delivery semantics
- Offsets committed only after successful processing
- Retries are attempted before failing (3 times with logging)
- Failed messages are routed to a Dead Letter Queue (DLQ)

### 5) Testing strategy

Testing was approached TDD-first. A CI pipeline is written into Github Actions to include testing, so that the most updated test cases are run when merging branches

<img width="700" alt="image" src="https://github.com/user-attachments/assets/9d614f90-15a4-4292-a354-3afb61a10c46" />


**Unit Tests**
- Domain validation (Users, Orders)
- Service validation (unique email etc.)
- Application service logic
- Kafka producer publishing
- Kafka consumer handlers (success + failure cases)

**Integration Tests**
- API behavior (status codes, responses)
- Kafka consume flows using Testcontainers
- DLQ behavior when handlers fail

### 6) Error handling
A shared exception-mapping middleware (written in the Shared .NET 8 class library) with custom exceptions to ensure consistent responses across all services

| Exception                     | Status                   |
| ----------------------------- | ------------------------ |
| `ArgumentException`           | 400                      |
| `ConflictException`           | 400                      |
| `NotFoundException`           | 404                      |
| `ServiceUnavailableException` | 503                      |
| Other                         | 500                      |

### 7) Structured Logging
Serilog is used across all services

---

# Running the System
## Prerequisites
- .NET 8 SDK (for localized testing)
- Docker & Docker Compose

## Start Everything
You may run the following command from the project root (/ECommerce)
```
docker-compose up --build
```

## Access
- User Service: http://localhost:5001/swagger
- Order Service: http://localhost:5002/swagger
- Kafka UI (Provectus): http://localhost:8085
- Frontend UI: http://localhost:5173

## Demonstration
### 1) Endpoints
You may use the swagger services to test the endpoints, or use the frontend UI created with react vite typescript

### 2) Kafka events
You may refer to either the Kafka UI to view the published/consumed messages, or refer directly to the respective containers to see the messages being consumed and logged down:

<img width="821" height="19" alt="image" src="https://github.com/user-attachments/assets/04b1f419-a392-4140-ad50-6bfffb50bf0a" />

### 3) Testing
You may run the following command from the project root (/ECommerce)
```
cd ECommerce.Tests
dotnet test
```

---

# Tradeoffs & Extensions
The following were intentionally not implemented, but explicitly considered:

## 1) Idempotency Persistence
Events may be re-delivered (at-least-once), however handlers are currently logging i.e. no demonstration of idempotency 

Future extension: Persist processed EventIds and skip duplicates safely 

## 2) Poison Message
Current DLQ logic only handles handler failures. Invalid json or event messages are silently committed 

Future extension: Explicit event message validation. Erroneous event message or invalid JSON to be routed to a DLQ for reprocessing or observation


## 3) Outbox Pattern
Current design publishes events after DB write. It's possible for a DB write without an event publishing to occur. 
This is fine for now as there are no downstream logic relying on successful publishing. If there are downstream logics that rely on event publishes after DB writes (creating a payment after an order is submitted etc.), this current design will not work

Future extension: Outbox table + background publisher

## 4) Health Checks
Future extension: Endpoints for service liveness checkes, and external service readiness (Kafka connectivity, database connectivity if using a real DB)


## 5) OpenTelemetry / Tracing
Future extension: Can be added for distributed tracing, kafka spans, HTTP request correlation


## 6) Specific service logic: User Existence Validation in Order Service 
Currently not enforced to avoid synchronous coupling
Future extension: Event-driven read model (store User created data in OrderService after receiving event)

---

# AI Tool Usage
Primarily used ChatGPT for:
- High level design validation
- Edge-case test cases review
- Refining documentation

All architectural decisions, tradeoffs, and final implementations were reviewed and owned manually.
