from pydantic_settings import BaseSettings
from pydantic import Field


class Settings(BaseSettings):
    """Application settings loaded from environment variables."""
    
    # Azure Configuration
    azure_tenant_id: str = Field(..., description="Azure Tenant ID")
    azure_client_id: str = Field(..., description="Azure Client ID")
    azure_client_secret: str = Field(..., description="Azure Client Secret")
    azure_subscription_id: str = Field(..., description="Azure Subscription ID")
    
    # Application Configuration
    api_host: str = Field(default="0.0.0.0", description="API Host")
    api_port: int = Field(default=8000, description="API Port")
    debug: bool = Field(default=False, description="Debug Mode")
    
    class Config:
        env_file = ".env"
        env_file_encoding = "utf-8"
        case_sensitive = False


settings = Settings()
