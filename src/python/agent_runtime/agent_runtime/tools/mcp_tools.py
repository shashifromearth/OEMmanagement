"""LangChain Tools wrapping MCP / Tool Executor. Used as graph node leaves, not as the orchestrator."""

import json

import httpx
from langchain_core.tools import tool

from agent_runtime.settings import settings


def _exec(name: str, payload: dict) -> str:
    body = {
        "toolName": name,
        "payloadJson": json.dumps(payload),
        "sessionId": payload.get("sessionId", "unknown"),
        "correlationId": payload.get("correlationId", "corr"),
    }
    with httpx.Client(timeout=15.0) as client:
        r = client.post(f"{settings.tool_executor_url}/mcp/tools/execute", json=body)
        r.raise_for_status()
        return r.text


@tool
def lookup_vehicle(vin: str, session_id: str = "unknown") -> str:
    """Look up a vehicle in the dealer DMS by VIN."""
    return _exec("dms.lookup_vehicle", {"vin": vin, "sessionId": session_id})


@tool
def get_customer(customer_id: str, session_id: str = "unknown") -> str:
    """Load CRM customer profile and loyalty."""
    return _exec("crm.get_customer", {"customerId": customer_id, "sessionId": session_id})


@tool
def check_parts(sku: str, session_id: str = "unknown") -> str:
    """Check ERP parts inventory."""
    return _exec("erp.check_parts", {"sku": sku, "sessionId": session_id})


@tool
def create_booking_slot(when_iso: str, session_id: str = "unknown") -> str:
    """Reserve a service bay slot."""
    return _exec("booking.create_slot", {"when": when_iso, "sessionId": session_id})


MCP_TOOLS = [lookup_vehicle, get_customer, check_parts, create_booking_slot]
