from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", extra="ignore")

    azure_openai_endpoint: str = ""
    azure_openai_api_key: str = ""
    azure_openai_deployment: str = "gpt-4o"
    azure_openai_api_version: str = "2024-10-21"
    azure_search_endpoint: str = ""
    azure_search_key: str = ""
    azure_search_index: str = "vehicle-knowledge"
    tool_executor_url: str = "http://tool-executor:5082"
    kafka_bootstrap: str = "localhost:9092"
    llm_router_url: str = "http://llm-router:5083"


settings = Settings()
