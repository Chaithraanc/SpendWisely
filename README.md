# SpendWisely
SpendWisely is a scalable, event-driven, AI-enabled expense management system designed to help users track spending, enforce budgets, and gain AI-powered financial insights.

The system follows production-grade engineering practices, including Domain-Driven Design (DDD), Test-Driven Development (TDD), asynchronous workflows, caching, and resilience patterns.

🛠 Current Progress

✅ Designed system architecture using DDD and modular monolith approach

✅ Implemented database schema (SQL Server + MongoDB Event Store)

✅ Applied TDD for core business logic

✅ Developing core REST APIs (Expenses, Categories, Budgets)

🔄 Currently working on: RabbitMQ integration, async AI-based expense classification, Redis caching, dashboard & insights


💻 Tech Stack

1.Backend: .NET 8, ASP.NET Core Web API

2.Frontend: React/Next.js

3.Databases: SQL Server, MongoDB (Event Sourcing)

4.Messaging: RabbitMQ

5.Caching: Redis

6.Resilience & Reliability: Polly (Retry, Circuit Breaker)

🏗 Architecture

The diagram below shows SpendWisely’s architecture, highlighting async workflows, AI worker integration, caching, and event-driven design.

<img width="5112" height="4845" alt="SpendWisely_Architecture2" src="https://github.com/user-attachments/assets/d476f976-45ec-482f-9ce6-8d075a7d78ef" />


Flow Highlights:

1.Users create expenses → API → SQL + Event Store → RabbitMQ → AI Worker → SQL + Redis

2.Monthly aggregation jobs generate dashboard summaries & AI insights

3.Admin panel monitors system metrics (queues, budgets, logs) via Prometheus + Grafana

4.Redis cache stores AI category predictions and dashboard aggregates for fast UI rendering

🗺 Roadmap / Upcoming Work

 1.Complete RabbitMQ event publishing and retry / DLQ mechanism

 2.Implement AI-based expense categorization and insights

 3.Add Redis caching for dashboard and AI results

 4.Build user dashboard with analytics and budget alerts

 5.Integrate Prometheus + Grafana for monitoring and alerting

 6.Logging & Monitoring: Serilog, Prometheus, Grafana
