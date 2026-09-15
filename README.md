# CAR Agentic Platform

Production-grade multi-agent system for vehicle service centers and dealers. Control plane is **.NET 8 Clean Architecture**. Agent graph is **LangGraph**. RAG and tools are **LangChain**. Retrieval is **Azure AI Search**. Events are **Kafka** (Azure Event Hubs Kafka protocol in production). Transactions live in **SQL Server**. Conversations live in **Cosmos DB**.

Map this repo to the attached reference architecture:

| Diagram box | Code |
|---|---|
| 1 Gateway | `src/dotnet/Car.Gateway` + Terraform APIM |
| 2 Session & context | `Car.Infrastructure/Session`, `Memory` |
| 3 Orchestrator | `src/dotnet/Car.Orchestrator` |
| 4 LLM router / memory / tools | `Car.LlmRouter`, `Car.Memory`, `Car.ToolExecutor` |
| 4 Agent execution | `src/python/agent_runtime` (LangGraph) |
| 5 Governance | `Car.Infrastructure/Safety` + graph `safety` node |
| Data pipeline | `Car.Ingestion` + LangChain splitters |
| 7 Observability | Terraform App Insights + K8s probes |
| Event bus | Kafka topics in `KafkaTopics` / Event Hubs module |

## Run locally

```bash
docker compose -f iac/docker/docker-compose.yml up --build
curl http://localhost:5080/v1/agent/turns -H "Content-Type: application/json" -d "{\"sessionId\":\"s1\",\"userId\":\"u1\",\"channel\":\"web\",\"dealerId\":\"d1\",\"message\":\"Book a brake job tomorrow 9am\"}"
```

## Deploy

```bash
cd iac/terraform
terraform init
terraform apply -var-file=environments/prod.tfvars
kubectl apply -k ../kubernetes
```

Interview prep: [docs/INTERVIEW-COOKBOOK.md](docs/INTERVIEW-COOKBOOK.md)
