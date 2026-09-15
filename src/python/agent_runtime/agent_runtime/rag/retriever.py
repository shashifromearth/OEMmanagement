"""LangChain RAG: retrievers, splitters, and Azure AI Search — not graph control-flow."""

from langchain_community.vectorstores.azuresearch import AzureSearch
from langchain_core.documents import Document
from langchain_openai import AzureOpenAIEmbeddings
from langchain_text_splitters import RecursiveCharacterTextSplitter

from agent_runtime.settings import settings


def build_embeddings() -> AzureOpenAIEmbeddings:
    return AzureOpenAIEmbeddings(
        azure_endpoint=settings.azure_openai_endpoint,
        api_key=settings.azure_openai_api_key,
        azure_deployment="text-embedding-3-large",
        api_version=settings.azure_openai_api_version,
    )


def build_splitter() -> RecursiveCharacterTextSplitter:
    return RecursiveCharacterTextSplitter(chunk_size=800, chunk_overlap=120)


def build_azure_search() -> AzureSearch | None:
    if not settings.azure_search_endpoint:
        return None
    return AzureSearch(
        azure_search_endpoint=settings.azure_search_endpoint,
        azure_search_key=settings.azure_search_key,
        index_name=settings.azure_search_index,
        embedding_function=build_embeddings().embed_query,
    )


def retrieve_context(query: str, k: int = 5) -> tuple[str, list[dict]]:
    store = build_azure_search()
    if store is None:
        fallback = (
            "Use dealer service manuals, OEM TSBs, and warranty policy. "
            "Brake pad replacement typically needs 45-90 minutes and a road test."
        )
        return fallback, [{"source": "fallback-kb", "chunkId": "local-1", "score": 0.1}]

    docs: list[Document] = store.similarity_search(query, k=k)
    context = "\n\n".join(d.page_content for d in docs)
    citations = [
        {
            "source": d.metadata.get("source", "azure-search"),
            "chunkId": d.metadata.get("id", ""),
            "score": float(d.metadata.get("score", 0) or 0),
        }
        for d in docs
    ]
    return context, citations
