# SpendWisely — Distributed, Real‑Time, AI‑Powered Expense Management System

SpendWisely is a cloud‑ready, scalable, event‑driven, AI‑enabled real‑time expense management platform built with a modern distributed architecture designed to help users track spending, enforce budgets, and gain AI‑powered financial insights.

The system follows production‑grade engineering practices, including Domain‑Driven Design (DDD), Test‑Driven Development (TDD), asynchronous workflows, caching, and resilience patterns such as retry and circuit breaker policies.

---

## ⚙️ Tech Stack

<img width="1536" height="1024" alt="Tech_Stack_Table" src="https://github.com/user-attachments/assets/2810ff7e-361c-49a6-be59-f7c087bafe17" />

---

## 🏗 Architecture

The diagram below shows SpendWisely’s architecture, highlighting async workflows, AI worker integration, caching, and event‑driven design.

<img width="1536" height="1024" alt="SpendWisely_Architecture_Final" src="https://github.com/user-attachments/assets/92bb72eb-5016-4bde-b747-b49f68d5065a" />

---

## 🔄 SpendWisely — End‑to‑End Working Flow

### **1️⃣ Expense CRUD / Budget CRUD Operations**
**Example: Expense Creation**
1. User adds an expense via React UI.  
2. API request hits .NET 8 backend.  
3. Backend validates input, applies domain rules, and stores data in SQL Server (authoritative store).  
4. In the same transaction, an Outbox record is created for the event (`ExpenseCreated`).  

---

### **2️⃣ Outbox Processor**
1. Background worker scans unprocessed Outbox records.  
2. Publishes events to RabbitMQ exchange (fanout/topic).  
3. Marks Outbox entries as processed after successful publish.  
4. Polly retry + circuit breaker ensures resilience against transient failures.  

---

### **3️⃣ Event Bus (RabbitMQ)**
1. Exchange acts as central event bus.  
2. Multiple queues are bound to the exchange based on routing keys:  
   - MongoDB Event Store consumer  
   - Go Real‑Time Analytics consumer  
   - AI Expense Categorization consumer  
   - AI Monthly Insights consumer  
3. Each queue receives the appropriate event copy.  
4. Retry and DLQ handle transient or failed deliveries.  

---

### **4️⃣ Event Store (MongoDB)**
1. Consumer writes each event to MongoDB for audit, replay, and debugging.  
2. Enables event‑sourcing style history and future replay capability.  

---

### **5️⃣ Real‑Time Analytics (Go Service)**
1. Go service consumes events from RabbitMQ.  
2. Updates Redis counters (category totals, daily totals, user totals, budgets).  
3. Pushes real‑time aggregates and budget consumption to the React UI via WebSockets.  
4. Sends budget‑exceeded notifications when thresholds are crossed.  
5. Dashboard updates instantly without page refresh.  

---

### **6️⃣ Caching Layer (Redis)**
SpendWisely optimizes AI categorization using a semantic caching layer in Redis.  
After the AI (OpenAI LLM) classifies an expense, the system stores the mapping so future similar descriptions can be categorized instantly without another LLM call.

**Example:**  
User adds an expense (e.g., “Starbucks Latte”).  
AI Categorization Consumer checks Redis for a matching description or keyword.  
- **Cache hit →** category returned instantly (no AI call).  
- **Cache miss →** OpenAI LLM invoked → category generated → result stored in Redis → category updated in SQL.

**Impact:**
- Reduces OpenAI calls by 70–90 %  
- Reduces latency  
- Saves cost  
- Improves user experience  

Redis also holds real‑time aggregates for instant UI updates without page refresh.

---

### **7️⃣ Daily Reconciliation Job (Quartz.NET)**
1. Runs once per day.  
2. Reads all expenses from SQL for the current month.  
3. Aggregates totals by category and user.  
4. Updates Dashboard monthly summary table.  
5. Ensures consistency between Redis real‑time counters and authoritative SQL.  
6. At month‑end, emits a `MonthlySummaryGenerated` event → triggers AI Insights consumer → resets Redis counters.  

---

### **8️⃣ AI Insights (Summary, Recommendations, Spending Spikes, Anomalies, Forecast)**
1. Month‑end reconciliation triggers the AI Insights Engine.  
2. AI service invokes OpenAI LLM, analyzes spending patterns, and generates insights.  
3. Insights stored in SQL and displayed in UI.  
**Example:** “You spent 20 % more on Food this month.”  

---

### **9️⃣ Logging & Observability**
1. Serilog logs structured events to Console, File, and Seq.  
2. Prometheus scrapes metrics from .NET and Go services.  
3. Grafana visualizes performance, throughput, and latency.  
4. OpenTelemetry traces distributed flows end‑to‑end.  

---

### **🔟 Deployment & CI/CD**
1. All services containerized with Docker.  
2. GitHub Actions builds, tests, and deploys to Fly.io.  
3. Environment variables manage configuration.  
4. Health checks ensure uptime and reliability.  

---

## 🧩 Idempotency — ProcessedEvents Table
SpendWisely ensures idempotent event processing across all consumers using a dedicated SQL table called `ProcessedEvents`.

**Purpose**
1. Prevents duplicate event handling when multiple consumers process the same event.  
2. Guarantees exactly‑once semantics in a distributed event‑driven system.  
3. Enables safe retries and resilience without side effects.  

**Workflow Integration**
- Each consumer checks `ProcessedEvents` before handling an event.  
- If `(EventId, EventType)` exists → skip processing (already handled).  
- If not → process event → insert record into `ProcessedEvents`.  
- Ensures no duplicate side effects even under retries or concurrent deliveries.  

---

## 🔐 Authentication Using JWT
SpendWisely uses JWT (JSON Web Tokens) for secure, stateless authentication across all services.  
This ensures that every API request is authenticated without maintaining server‑side sessions, making the system scalable and microservice‑friendly.

---

## 🧪 Testing Strategy
SpendWisely follows a layered testing approach aligned with Clean Architecture.

1. **Domain Tests** — Validate business rules; pure unit tests (no mocks, no DB).  
2. **Application Tests** — Validate workflows and use cases; external dependencies mocked (RabbitMQ, Redis, AI, SQL).  
3. **Infrastructure Tests** — Validate real infrastructure using Testcontainers (SQL, MongoDB, Redis, RabbitMQ, Outbox, ProcessedEvents).  
4. **Integration Tests** — Validate full event‑driven pipeline end‑to‑end (API → SQL → Outbox → RabbitMQ → Go → Redis → WebSockets).  

---

## 🚧 Ongoing Work (In Progress)
1. **Go Real‑Time Service** — finalizing WebSocket updates, budget alerts, and Redis counters.  
2. **React UI** — completing dashboard, charts, budget screens, and real‑time updates.  
3. **Testing Coverage** — expanding domain, application, infrastructure, and end‑to‑end tests.  
4. **CI/CD & Cloud Deployment** — final Fly.io deployment pipeline and environment setup.  
