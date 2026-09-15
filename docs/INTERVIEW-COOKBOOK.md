# CAR Agentic Platform — Interview Cookbook

Use this file as a talk track. Every claim maps to code in this repo. Interviewers want **where**, **why**, and **what fails if you skip it**.

---

## 1. One-minute pitch

We built a **control-plane / data-plane split**:

- **.NET 8** owns identity, sessions, transactional work orders, routing policy, MCP tool adapters, content safety, and Kafka publishing. That matches the attached Azure reference architecture (gateway, orchestrator, LLM router, memory, tool executor).
- **Python LangGraph** owns the **multi-agent state machine** (safety → intent → RAG → specialist → respond) with a checkpointer keyed by `sessionId` so a crashed replica can resume the same thread.
- **LangChain** owns **RAG primitives and tools** (Azure AI Search retriever, text splitters, embeddings, MCP tools). It does **not** own routing between agents.
- **SQL Server** is the system of record for vehicles and work orders (ACID, reporting, dealer ops).
- **Cosmos DB** stores conversation turns (session partition, globally distributed, 99.99 region failover).
- **Kafka / Event Hubs Kafka** carries agent turns, tool executions, ingestion jobs, and audit so producers and consumers scale independently.
- **Azure AI Search** is the vector + semantic index for manuals, TSBs, and FAQs.

SLO: **99.9% monthly availability** ≈ 43.8 minutes downtime/month. We get there with multi-AZ AKS, PDBs, HPA, Kafka RF=3/minISR=2, Cosmos multi-region, Redis Premium, Search 3 replicas, circuit breakers, and fail-open vs fail-closed safety.

---

## 2. LangChain vs LangGraph — where, why, benefit

This is the most common interview question on this project. Memorize this table.

| Concern | Library | Exact location | Why this library | Benefit to the system |
|---|---|---|---|---|
| Multi-agent control flow, branching, durable thread | **LangGraph** | `src/python/agent_runtime/agent_runtime/graph/supervisor.py` | Graphs have **nodes, conditional edges, shared state, checkpointing**. A chain is a line; a service advisor flow is a **state machine** (booking vs diagnostics vs human). | Specialist agents stay isolated; supervisor routes once; a pod kill does not lose `thread_id` session progress. |
| Shared graph state | **LangGraph** `TypedDict` + `add_messages` | `agent_runtime/graph/state.py` | Reducer semantics for messages; typed fields for intent, citations, safety. | Prevents “lost last message” bugs; interviewers hear **reducer / checkpoint**. |
| Resume after crash | **LangGraph** `MemorySaver` (prod: swap to Postgres/Redis saver) | `supervisor.py` `compile(checkpointer=...)` | HA: same `thread_id` = `sessionId`. | Supports 99.9% without “sorry, start over”. |
| HTTP and Kafka use the **same** compiled graph | **LangGraph** | `main.py` and `kafka/consumer.py` | DRY: one brain, two transports. | Sync UX for web; async for WhatsApp/SMS/batch. |
| Azure AI Search retrieval | **LangChain** | `agent_runtime/rag/retriever.py` | Retrievers, embeddings, `Document` metadata are LangChain’s job. | Swap FAISS locally vs Azure Search in prod without rewriting the graph. |
| Chunking manuals | **LangChain** `RecursiveCharacterTextSplitter` | `retriever.py` `build_splitter()` | Standard RAG preprocessing. | Better recall than naive page dumps. |
| MCP/DMS/CRM/ERP tools | **LangChain** `@tool` | `agent_runtime/tools/mcp_tools.py` | Tool schema for LLM function calling. | Graph nodes **bind_tools**; Tool Executor (.NET) remains the adapter (DIP). |
| Prompt + model invoke inside a specialist | **LangChain** `AzureChatOpenAI`, `SystemMessage` | `agent_runtime/agents/nodes.py` | One LLM call per node is a **chain**, not a graph. | YAGNI: do not wrap a single prompt in LangGraph. |

### Say this out loud

> “LangChain is the **component library** (retriever, splitter, tools, chat model). LangGraph is the **orchestrator of agents**. If we used only LangChain LCEL chains we would encode routing as nested `RunnableBranch` spaghetti and lose checkpointed cycles. If we used only LangGraph without LangChain we would reimplement Azure Search and tool schemas. The .NET orchestrator does **not** run the graph; it enforces auth, safety, SQL side effects, and publishes Kafka. That is Clean Architecture: the agent runtime is an **adapter** behind `IAgentRuntime`.”

### What we deliberately did **not** do (YAGNI)

- No LangSmith-only control plane in-repo (can add later).
- No 20-agent swarm. Five specialists + safety + intent + RAG is enough for dealer service.
- No Autogen group chat: we need **deterministic edges** for audit and HITL.

---

## 3. SOLID — exact usages

### Single Responsibility (SRP)

| Type | File | One reason to change |
|---|---|---|
| `WorkOrder` aggregate | `src/dotnet/Car.Domain/WorkOrders/WorkOrder.cs` | Scheduling/status rules, not HTTP |
| `HandleAgentTurnHandler` | `src/dotnet/Car.Application/AgentTurns/HandleAgentTurn.cs` | Application workflow of a turn |
| `CostLatencyRoutingPolicy` | `src/dotnet/Car.Infrastructure/Llm/PolicyLlmRouter.cs` | Which model to pick |
| `AzureContentSafetyGuard` | `src/dotnet/Car.Infrastructure/Safety/AzureContentSafetyGuard.cs` | Safety only |
| `booking_node` vs `diagnostics_node` | `nodes.py` | Different prompts/tools |
| Kafka consumer vs HTTP | `Ingestion/Program.cs` vs `Orchestrator/Program.cs` | Different hosts |

### Open/Closed (OCP)

- `IRoutingPolicy` / `CostLatencyRoutingPolicy`: add `PremiumReasoningPolicy` **without** editing `PolicyLlmRouter` or the API (`Car.LlmRouter/Program.cs`).
- LangGraph: add a `warranty` node and one map entry in `route_after_intent` — graph stays closed for existing specialists.
- `IVectorSearch`: `AzureAiSearchVectorStore` vs `NoOpVectorSearch` in `Car.Infrastructure/DependencyInjection.cs`.

### Liskov Substitution (LSP)

- Any `IAgentRuntime` must return `AgentResponse` or throw; `HttpAgentRuntime` is substitutable with a fake in tests.
- Any `IEventBus` (Kafka today) can be replaced by Event Hubs producer with the same contract (`IEventBus.PublishAsync`).
- LangChain tools all share the `@tool` callable contract; `HttpToolExecutor` always returns `ToolExecutionResult`.

### Interface Segregation (ISP)

Ports in `src/dotnet/Car.Domain/Ports/Ports.cs` are **narrow**: `ISessionStore` is not mixed with `IConversationStore`; `ILlmRouter` is not mixed with `IToolExecutor`. Callers do not depend on Cosmos APIs.

### Dependency Inversion (DIP)

- Domain **defines** ports; Infrastructure **implements** SQL, Redis, Cosmos, Kafka, Azure Search, Polly HTTP.
- Application depends on `IContentSafety`, `IAgentRuntime`, `IEventBus` — never on Confluent.Kafka types.
- Python graph calls LangChain tools; tools HTTP to .NET Tool Executor (MCP adapter). Domain of “execute dealer system” stays behind `IToolExecutor`.

---

## 4. Clean Architecture, DRY, YAGNI in this repo

**Layers (inward dependencies):**

```
Car.Gateway / Orchestrator / LlmRouter / Memory / ToolExecutor / Ingestion   (hosts)
        ↓
Car.Infrastructure   (Kafka, EF SQL, Cosmos, Redis, Search, Polly)
        ↓
Car.Application      (MediatR use cases, FluentValidation)
        ↓
Car.Domain + Car.BuildingBlocks   (entities, ports, Result)
```

**DRY**

- One `AddInfrastructure` registration for all hosts.
- One `KafkaTopics` class.
- One compiled LangGraph for HTTP and Kafka.
- One `Result<T>` instead of ad-hoc HTTP error shapes in the application layer.

**YAGNI**

- Tool Executor uses **stub MCP payloads** until real DMS credentials exist — the **port and Kafka audit** are real.
- No Azure Data Factory graph in code; ingestion is a Kafka worker + LangChain splitter. ADF can wrap the same topic later.
- In-memory LangGraph checkpointer locally; production swap is one constructor.

---

## 5. Data plane: why SQL + Cosmos + Kafka + Search (not one database)

| Store | Data | Why | Interview trap |
|---|---|---|---|
| SQL Server `CarOps` | Vehicles, work orders | Strong consistency, joins, dealer reports, constraints | Do not put chat transcripts in SQL at this scale |
| Cosmos `conversations` | Turns, partition `/sessionId` | Hot key per session, TTL possible, multi-region | Session consistency is enough for chat |
| Redis | Gateway session bag, 4h TTL | Sub-ms hop for orchestrator | Not source of truth |
| Azure AI Search | Chunks + embeddings | Semantic + vector RAG | Search is not a system of record |
| Kafka | Facts about turns/tools/ingest | Fan-out, replay, audit, backpressure | Not a DB; compact or archive |

**Polyglot persistence** is an architecture choice, not fashion: different **consistency and access patterns**.

---

## 6. Request path (sync) and event path (async)

```
Client → APIM/YARP Gateway (JWT, rate limit)
      → Orchestrator HandleAgentTurn
         → IContentSafety
         → Cosmos append user turn
         → IAgentRuntime HTTP LangGraph
              safety → intent → rag (LangChain/Azure Search)
                    → booking|diagnostics|parts|customer_care
                    → tools → ToolExecutor MCP → Kafka tool.executions
              → respond
         → Cosmos assistant turn
         → Redis session
         → Kafka agent.turns
```

Async channels (WhatsApp, SMS): produce `agent.requests`; Python `kafka/consumer.py` runs the **same graph**; results on `agent.turns`.

---

## 7. High availability and 99.9% — how to defend the SLO

**Math:** 99.9% = 8.77 hours/year ≈ 43.8 min/month. Design for **degraded mode**, not zero failure.

| Layer | Mechanism in repo | Failure mode covered |
|---|---|---|
| AKS | 3 replicas, HPA, PDB `minAvailable: 2`, zone spread | Node/zone loss |
| Kafka | RF=3, minISR=2, `acks=all`, idempotent producer | Broker loss, exactly-once-ish produce |
| Consumers | `enable.auto.commit=false`, commit after work, `read_committed` | Crash mid-process → reprocess |
| SQL | Azure SQL backup LTR + private network | Disk/region (pair with failover group in next iteration) |
| Cosmos | Multi-region, zone redundant primary, continuous backup | Region loss |
| Redis | Premium replica | Cache node loss (sessions rebuild from Cosmos) |
| Search | `replica_count = 3` | Query HA |
| LLM | `max_retries`, timeout, cheaper model policy | Azure OpenAI 429/5xx |
| Tools | Polly circuit breaker | DMS down → `circuit_open` not cascading timeout |
| Safety | Heuristic if Azure Content Safety times out | Availability vs risk (documented fail-open for timeout, fail-closed for injection) |
| Graph | checkpointer `thread_id` | Agent pod recycle |

**Nines interview answer:** 99.9% is **three nines**. Do not claim 99.99 unless you have active-active SQL, global traffic manager, and multi-region AKS. This design is **regional AKS + globally distributed Cosmos + Kafka durability**.

**Fault tolerance patterns named:** circuit breaker, retry with timeout, bulkhead (separate deployments), graceful degradation (`NoOpVectorSearch`), idempotent Kafka, PDB, topology spread.

---

## 8. Mapping to the attached diagram (numbered boxes)

1. **Gateway** — `Car.Gateway` YARP + JWT + rate limiter; Terraform `modules/apim` Premium.
2. **Session & context** — Redis `RedisSessionStore`; RAG context built in LangGraph `rag` node.
3. **Orchestrator** — MediatR `HandleAgentTurnHandler` (intent is actually in LangGraph; .NET owns the use case envelope).
4. **LLM Router** — `IRoutingPolicy` (OCP). **Memory** — Cosmos + Memory API. **Tool Executor** — MCP HTTP + Kafka.
5. **Governance** — `AzureContentSafetyGuard` + graph `safety_node` + audit topic.
6. **Ingestion** — `Car.Ingestion` Kafka worker; embeddings in LangChain (Python) when Search is configured.
7. **Observability** — Terraform Log Analytics + App Insights; K8s probes; Kafka publish logs.

**MCP:** Tool names `dms.lookup_vehicle`, `crm.get_customer`, `erp.check_parts`, `booking.create_slot`, `graph.send_email`.

---

## 9. Interview Q&A (practice out loud)

### Architecture

**Q: Why hybrid .NET + Python?**  
A: Dealer enterprises already run .NET for DMS-adjacent APIs, Entra ID, EF, and APIM. LangGraph’s ecosystem is Python-first. The boundary is `IAgentRuntime` so either side can be replaced.

**Q: Why not Semantic Kernel only?**  
A: SK is valid on .NET. We chose LangGraph for **checkpointed multi-agent graphs** and LangChain Azure Search loaders. SK could implement `IAgentRuntime` later (OCP/DIP).

**Q: Where is the workflow engine (Durable Functions in the diagram)?**  
A: Long-running human approvals can be Durable Functions **or** Kafka + `requiresHumanApproval`. We used the latter (YAGNI) with the same HITL flag on `AgentResponse`.

**Q: How do you prevent prompt injection?**  
A: Dual layer: .NET `LooksLikePromptInjection` + Azure Content Safety; graph `safety_node`. Never put secrets in prompts; tools are allow-listed.

**Q: How do you ground answers?**  
A: RAG node always runs before specialists; citations returned to the client; diagnostics prompt says “never invent DTCs”.

### LangChain / LangGraph

**Q: Difference between chain and graph?**  
A: Chain = DAG of runnables. Graph = cyclic/conditional state machine with checkpointing and multi-actor nodes.

**Q: Why `add_messages`?**  
A: Reducer appends messages instead of overwriting state on each node.

**Q: What is `thread_id`?**  
A: Checkpointer key. We set it to `sessionId` so HTTP retries and Kafka retries share memory.

**Q: How would you add a warranty agent?**  
A: New node function, add to `StateGraph`, extend `route_after_intent` mapping. No change to .NET ports if it only uses RAG/tools.

**Q: LangGraph vs CrewAI vs Autogen?**  
A: We need **auditable edges** and HITL, not free-form debate. CrewAI is role-play; Autogen is conversational; LangGraph is an explicit state machine.

### .NET / SOLID

**Q: Why MediatR?**  
A: Use-case as transaction script with validation; hosts stay thin (`Orchestrator/Program.cs`).

**Q: Why `Result<T>` not exceptions for business errors?**  
A: Content safety and validation are expected; exceptions for infrastructure. Clear HTTP mapping.

**Q: How is OCP shown in LLM routing?**  
A: New `IRoutingPolicy` implementation, register in DI, no change to `ILlmRouter` consumers.

**Q: EF ignoring `DomainEvents`?**  
A: Persistence ignorance; events are raised on the aggregate and can be dispatched after `SaveChanges` (next slice).

### Data / Kafka

**Q: Why Kafka instead of Azure Service Bus?**  
A: Replay, partitions by `sessionId` key, high fan-out, Event Hubs Kafka protocol in Azure so we keep one client (`Confluent.Kafka`).

**Q: Ordering?**  
A: Same `key` = `sessionId` → same partition → per-session order.

**Q: Exactly once?**  
A: Idempotent producer + manual commit after side effects. Full EOS needs transactional consume-produce; we document at-least-once + idempotent handlers.

**Q: Cosmos partition key?**  
A: `/sessionId` — all turns for a chat in one logical partition.

**Q: SQL vs Cosmos for work orders?**  
A: Work orders have relations, money, status machines, reporting — SQL. Chat is write-heavy, schema-flexible — Cosmos.

### RAG

**Q: Chunk size 800 overlap 120?**  
A: Typical for manuals: enough context for a procedure step, overlap preserves headings.

**Q: Semantic ranker?**  
A: Azure AI Search `QueryType.Semantic` in `AzureAiSearchVectorStore`. Hybrid search is the production upgrade (vector + BM25).

**Q: How do you evaluate RAG?**  
A: Golden questions from TSBs; faithfulness vs citations; online: thumbs-down + Kafka `agent.failures`.

### HA / SRE

**Q: Walk through Azure OpenAI outage.**  
A: Router could fail over deployment; orchestrator catch returns user-safe message and Kafka `agent.failures`; circuit on tools unaffected.

**Q: Walk through Kafka outage.**  
A: Sync path still works if orchestrator Kafka publish fails — **current code publish is after success**; mention making publish outbox for stronger guarantees (interview gold: **identify the gap**).

**Q: PDB vs HPA?**  
A: PDB = voluntary disruption (drain). HPA = load. Both required.

**Q: Why min 3 replicas?**  
A: Survive one zone + one rolling update.

### Security

**Q: Zero trust?**  
A: NetworkPolicy default deny, Entra JWT at gateway, Key Vault, non-root containers, SQL public access disabled.

**Q: PII?**  
A: Diagram box 5; mask before logs; Cosmos encryption at rest; do not put VIN+PII in prompts without minimization.

### Behavioral / design

**Q: Biggest trade-off?**  
A: Extra hop .NET → Python vs single stack. We bought ecosystem speed and a clean port.

**Q: What would you add in month two?**  
A: Outbox, Redis/Postgres checkpointer, hybrid search, real MCP servers, Durable Functions for multi-day approvals, eval harness.

---

## 10. Whiteboard script (5 minutes)

1. Draw clients (dealer, tech mobile, customer).
2. Draw APIM + Gateway.
3. Draw Orchestrator with safety + MediatR.
4. Draw LangGraph boxes: safety, intent, rag, four specialists, respond.
5. Draw SQL, Cosmos, Redis, Search, Kafka.
6. Draw Tool Executor to DMS/CRM/ERP.
7. Mark SLO: replicas, RF=3, circuit breaker.
8. Say SOLID one-liners while pointing at ports vs adapters.

---

## 11. Code flashcards (memorize paths)

- Ports: `src/dotnet/Car.Domain/Ports/Ports.cs`
- Use case: `src/dotnet/Car.Application/AgentTurns/HandleAgentTurn.cs`
- SQL aggregate: `src/dotnet/Car.Domain/WorkOrders/WorkOrder.cs`
- Kafka: `src/dotnet/Car.Infrastructure/Messaging/KafkaEventBus.cs`
- Router OCP: `src/dotnet/Car.Infrastructure/Llm/PolicyLlmRouter.cs`
- Graph: `src/python/agent_runtime/agent_runtime/graph/supervisor.py`
- RAG LangChain: `src/python/agent_runtime/agent_runtime/rag/retriever.py`
- Tools LangChain: `src/python/agent_runtime/agent_runtime/tools/mcp_tools.py`
- K8s HA: `iac/kubernetes/*.yaml`
- Terraform: `iac/terraform/main.tf` + `modules/*`

---

## 12. Honest gaps (senior signal)

Call these out before the interviewer does:

1. Kafka publish in the turn handler is not an **outbox** — dual write risk.
2. `MemorySaver` is process-local — production needs Redis/Postgres checkpointer.
3. Tool Executor stubs dealer systems — production MCP servers needed.
4. Azure SQL zone redundant / failover group not fully wired (SKU S2 is cost-YAGNI; bump to BC/Premium for true AZ).
5. Semantic Search requires a semantic configuration on the index, not only `QueryType.Semantic`.

Seniors who name gaps get hired. Juniors who claim perfection do not.

---

## 13. Principles cheat-sheet (one card)

- **SRP** — one handler, one aggregate, one agent node.
- **OCP** — `IRoutingPolicy`, new LangGraph node.
- **LSP** — ports honored by all adapters.
- **ISP** — split stores and routers.
- **DIP** — domain ports, infra adapters.
- **DRY** — one graph, one DI, one topic list.
- **YAGNI** — stubs at the edges, real contracts in the middle.
- **Clean Architecture** — domain has zero Azure packages.

That is the system, the libraries, and the interview.
