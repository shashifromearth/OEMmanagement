from agent_runtime.graph.supervisor import compiled_graph
from langchain_core.messages import HumanMessage


def test_booking_route():
    out = compiled_graph.invoke(
        {
            "session_id": "s1",
            "user_id": "u1",
            "dealer_id": "d1",
            "channel": "web",
            "messages": [HumanMessage(content="Book a brake appointment tomorrow 9am")],
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
        config={"configurable": {"thread_id": "s1"}},
    )
    assert out["intent"] == "booking"
    assert out["produced_by"] in {"Booking", "Safety"}
    assert out["final_answer"]
