import os
from dotenv import load_dotenv

load_dotenv()

class Settings:
    # Base URL of the ASP.NET Core backend (used by tools)
    backend_base_url: str = os.getenv("BACKEND_BASE_URL", "http://localhost:5085")
    # Optional API key if backend expects one (not used currently)
    backend_api_key: str | None = os.getenv("BACKEND_API_KEY")
    agent_execution_key: str | None = os.getenv("AGENT_EXECUTION_KEY")

settings = Settings()
