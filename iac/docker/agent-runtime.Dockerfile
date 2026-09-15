FROM python:3.12-slim
WORKDIR /app
RUN adduser --disabled-password --gecos "" appuser
COPY src/python/agent_runtime/pyproject.toml /app/pyproject.toml
COPY src/python/agent_runtime/agent_runtime /app/agent_runtime
RUN pip install --no-cache-dir .
USER appuser
EXPOSE 8088
HEALTHCHECK CMD python -c "import urllib.request; urllib.request.urlopen('http://127.0.0.1:8088/health/ready')"
CMD ["uvicorn", "agent_runtime.main:app", "--host", "0.0.0.0", "--port", "8088", "--workers", "2"]
