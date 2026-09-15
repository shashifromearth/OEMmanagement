"""LangGraph supervisor: durable multi-agent control flow, checkpointed for HA failover."""

from langgraph.checkpoint.memory import MemorySaver
from langgraph.graph import END, START, StateGraph

from agent_runtime.agents.nodes import (
    booking_node,
    customer_care_node,
    diagnostics_node,
    intent_node,
    parts_node,
    rag_node,
    respond_node,
    safety_node,
)
from agent_runtime.graph.state import AgentState


def route_after_intent(state: AgentState) -> str:
    if state.get("safety_blocked"):
        return "respond"
    mapping = {
        "booking": "booking",
        "diagnostics": "diagnostics",
        "parts": "parts",
        "customer_care": "customer_care",
    }
    return mapping.get(state.get("next_agent", "customer_care"), "customer_care")


def build_graph():
    graph = StateGraph(AgentState)
    graph.add_node("safety", safety_node)
    graph.add_node("intent", intent_node)
    graph.add_node("rag", rag_node)
    graph.add_node("booking", booking_node)
    graph.add_node("diagnostics", diagnostics_node)
    graph.add_node("parts", parts_node)
    graph.add_node("customer_care", customer_care_node)
    graph.add_node("respond", respond_node)

    graph.add_edge(START, "safety")
    graph.add_edge("safety", "intent")
    graph.add_edge("intent", "rag")
    graph.add_conditional_edges(
        "rag",
        route_after_intent,
        {
            "booking": "booking",
            "diagnostics": "diagnostics",
            "parts": "parts",
            "customer_care": "customer_care",
            "respond": "respond",
        },
    )
    graph.add_edge("booking", "respond")
    graph.add_edge("diagnostics", "respond")
    graph.add_edge("parts", "respond")
    graph.add_edge("customer_care", "respond")
    graph.add_edge("respond", END)

    checkpointer = MemorySaver()
    return graph.compile(checkpointer=checkpointer)


compiled_graph = build_graph()
