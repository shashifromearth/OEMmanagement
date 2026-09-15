from fastapi import FastAPI
from langchain_core.messages import HumanMessage
from pydantic import BaseModel, Field

from agent_runtime.graph.supervisor import compiled_graph

app = FastAPI(title="CAR Agent Runtime", version="1.0.0")


class TurnRequest(BaseModel):
    sessionId: str
    userId: str
    channel: str
    dealerId: str
    message: str
    metadata: dict[str, str] = Field(default_factory=dict)


class Citation(BaseModel):
    source: str
    chunkId: str
    score: float


class TurnResponse(BaseModel):
    sessionId: str
    content: str
    producedBy: int
    citations: list[Citation]
    requiresHumanApproval: bool


AGENT_ENUM = {
    "Supervisor": 0,
    "Intent": 1,
    "Booking": 2,
    "Diagnostics": 3,
    "Parts": 4,
    "CustomerCare": 5,
    "Safety": 6,
}


@app.get("/health/ready")
def ready():
    return {"status": "ok"}


@app.post("/v1/turns", response_model=TurnResponse)
def run_turn(body: TurnRequest):
    result = compiled_graph.invoke(
        {
            "session_id": body.sessionId,
            "user_id": body.userId,
            "dealer_id": body.dealerId,
            "channel": body.channel,
            "messages": [HumanMessage(content=body.message)],
            "intent": "",
            "confidence": 0.0,
            "next_agent": "customer_care",
            "rag_context": "",
            "citations": [],
            "tool_results": [],
            "safety_blocked": False,
            "requires_human": False,
            "final_answer": "",
            "produced_by": "Supervisor",
        },
        config={"configurable": {"thread_id": body.sessionId}},
    )
    citations = [Citation(**c) if isinstance(c, dict) else c for c in result.get("citations", [])]
    return TurnResponse(
        sessionId=body.sessionId,
        content=result.get("final_answer") or "I am here to help with service booking and diagnostics.",
        producedBy=AGENT_ENUM.get(result.get("produced_by", "Supervisor"), 0),
        citations=citations,
        requiresHumanApproval=bool(result.get("requires_human")),
    )
