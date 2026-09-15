from typing import Annotated, Any, Literal, TypedDict

from langgraph.graph.message import add_messages


class AgentState(TypedDict):
    session_id: str
    user_id: str
    dealer_id: str
    channel: str
    messages: Annotated[list, add_messages]
    intent: str
    confidence: float
    next_agent: Literal[
        "booking", "diagnostics", "parts", "customer_care", "respond", "human"
    ]
    rag_context: str
    citations: list[dict[str, Any]]
    tool_results: list[dict[str, Any]]
    safety_blocked: bool
    requires_human: bool
    final_answer: str
    produced_by: str
