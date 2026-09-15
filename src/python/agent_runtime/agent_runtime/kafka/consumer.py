"""Kafka consumer: same LangGraph as HTTP so request/event paths stay DRY."""

import json
import logging

from confluent_kafka import Consumer, Producer
from langchain_core.messages import HumanMessage

from agent_runtime.graph.supervisor import compiled_graph
from agent_runtime.settings import settings

log = logging.getLogger(__name__)


def run_consumer() -> None:
    consumer = Consumer(
        {
            "bootstrap.servers": settings.kafka_bootstrap,
            "group.id": "car-agent-runtime",
            "enable.auto.commit": False,
            "auto.offset.reset": "earliest",
            "isolation.level": "read_committed",
        }
    )
    producer = Producer({"bootstrap.servers": settings.kafka_bootstrap, "acks": "all", "enable.idempotence": True})
    consumer.subscribe(["agent.requests"])
    while True:
        msg = consumer.poll(1.0)
        if msg is None:
            continue
        if msg.error():
            log.error(msg.error())
            continue
        payload = json.loads(msg.value())
        out = compiled_graph.invoke(
            {
                "session_id": payload["sessionId"],
                "user_id": payload.get("userId", ""),
                "dealer_id": payload.get("dealerId", ""),
                "channel": payload.get("channel", "kafka"),
                "messages": [HumanMessage(content=payload["message"])],
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
            config={"configurable": {"thread_id": payload["sessionId"]}},
        )
        producer.produce("agent.turns", key=payload["sessionId"], value=json.dumps({"content": out.get("final_answer")}))
        producer.flush()
        consumer.commit(msg)
