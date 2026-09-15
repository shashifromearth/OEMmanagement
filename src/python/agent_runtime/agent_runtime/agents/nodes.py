from langchain_core.messages import HumanMessage, SystemMessage
from langchain_openai import AzureChatOpenAI

from agent_runtime.graph.state import AgentState
from agent_runtime.rag.retriever import retrieve_context
from agent_runtime.settings import settings
from agent_runtime.tools.mcp_tools import MCP_TOOLS


def llm(temperature: float = 0.1) -> AzureChatOpenAI:
    return AzureChatOpenAI(
        azure_endpoint=settings.azure_openai_endpoint or "https://example.openai.azure.com/",
        api_key=settings.azure_openai_api_key or "not-set",
        azure_deployment=settings.azure_openai_deployment,
        api_version=settings.azure_openai_api_version,
        temperature=temperature,
        timeout=30,
        max_retries=2,
    )


INJECTION_MARKERS = ("ignore previous", "system prompt", "jailbreak")


def safety_node(state: AgentState) -> dict:
    text = ""
    if state.get("messages"):
        text = str(state["messages"][-1].content).lower()
    blocked = any(m in text for m in INJECTION_MARKERS)
    return {"safety_blocked": blocked, "requires_human": blocked}


def intent_node(state: AgentState) -> dict:
    last = str(state["messages"][-1].content)
    lowered = last.lower()
    if any(w in lowered for w in ("book", "appointment", "schedule", "slot")):
        intent, nxt, conf = "booking", "booking", 0.86
    elif any(w in lowered for w in ("noise", "brake", "check engine", "diagnostic", "dtc")):
        intent, nxt, conf = "diagnostics", "diagnostics", 0.84
    elif any(w in lowered for w in ("part", "pad", "filter", "stock", "sku")):
        intent, nxt, conf = "parts", "parts", 0.82
    else:
        intent, nxt, conf = "customer_care", "customer_care", 0.7
    return {"intent": intent, "next_agent": nxt, "confidence": conf}


def rag_node(state: AgentState) -> dict:
    query = str(state["messages"][-1].content)
    context, citations = retrieve_context(query)
    return {"rag_context": context, "citations": citations}


def _specialist(system: str, produced_by: str, bind_tools: bool = False):
    def node(state: AgentState) -> dict:
        if not settings.azure_openai_api_key:
            answer = (
                f"[{produced_by}] Based on manuals: {state.get('rag_context', '')[:400]} "
                f"Intent={state.get('intent')}. I can book a bay, check parts, or open a diagnostic RO."
            )
            return {"final_answer": answer, "produced_by": produced_by, "next_agent": "respond"}

        model = llm().bind_tools(MCP_TOOLS) if bind_tools else llm()
        msgs = [
            SystemMessage(content=system + "\n\nRAG:\n" + (state.get("rag_context") or "")),
            HumanMessage(content=str(state["messages"][-1].content)),
        ]
        result = model.invoke(msgs)
        return {
            "final_answer": str(result.content),
            "produced_by": produced_by,
            "next_agent": "respond",
        }

    return node


booking_node = _specialist(
    "You are the booking agent for a vehicle service center. Propose slots, capture VIN and concern.",
    "Booking",
    bind_tools=True,
)
diagnostics_node = _specialist(
    "You are a master technician assistant. Use TSBs and RAG. Never invent DTCs.",
    "Diagnostics",
)
parts_node = _specialist(
    "You are the parts advisor. Check ERP stock via tools. Quote only in-stock or ETA items.",
    "Parts",
    bind_tools=True,
)
customer_care_node = _specialist(
    "You are dealer customer care. Be concise. Escalate warranty disputes to a human.",
    "CustomerCare",
)


def respond_node(state: AgentState) -> dict:
    if state.get("safety_blocked"):
        return {
            "final_answer": "I cannot process that request. Connecting you to a service advisor.",
            "produced_by": "Safety",
            "requires_human": True,
        }
    return {}
