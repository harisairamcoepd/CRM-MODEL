from functools import lru_cache
from typing import List

from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    app_name: str = Field(default="COEPD AI Funnel API", alias="APP_NAME")
    app_env: str = Field(default="development", alias="APP_ENV")
    app_host: str = Field(default="0.0.0.0", alias="APP_HOST")
    app_port: int = Field(default=8000, alias="APP_PORT")

    database_url: str = Field(
        default="postgresql+psycopg://postgres:postgres@localhost:5432/coepd_ai_funnel",
        alias="DATABASE_URL",
    )

    jwt_secret_key: str = Field(default="change-me-in-env", alias="JWT_SECRET_KEY")
    jwt_algorithm: str = Field(default="HS256", alias="JWT_ALGORITHM")
    jwt_access_token_expire_minutes: int = Field(default=480, alias="JWT_ACCESS_TOKEN_EXPIRE_MINUTES")

    admin_email: str = Field(default="admin@coepd.ai", alias="ADMIN_EMAIL")
    admin_password: str = Field(default="ChangeMe_Admin_123", alias="ADMIN_PASSWORD")
    admin_full_name: str = Field(default="System Admin", alias="ADMIN_FULL_NAME")

    staff_email: str = Field(default="staff@coepd.ai", alias="STAFF_EMAIL")
    staff_password: str = Field(default="ChangeMe_Staff_123", alias="STAFF_PASSWORD")
    staff_full_name: str = Field(default="Admissions Staff", alias="STAFF_FULL_NAME")

    whatsapp_api_url: str = Field(default="", alias="WHATSAPP_API_URL")
    whatsapp_access_token: str = Field(default="", alias="WHATSAPP_ACCESS_TOKEN")
    whatsapp_sender_id: str = Field(default="", alias="WHATSAPP_SENDER_ID")
    cors_allowed_origins: str = Field(
        default="http://localhost:5099,https://localhost:7099,http://127.0.0.1:5099,https://127.0.0.1:7099,http://localhost:5500,http://127.0.0.1:5500",
        alias="CORS_ALLOWED_ORIGINS",
    )

    log_level: str = Field(default="INFO", alias="LOG_LEVEL")

    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        case_sensitive=False,
        extra="ignore",
    )

    @property
    def is_production(self) -> bool:
        return self.app_env.lower() == "production"

    @property
    def cors_origins(self) -> List[str]:
        return [origin.strip() for origin in self.cors_allowed_origins.split(",") if origin.strip()]


@lru_cache(maxsize=1)
def get_settings() -> Settings:
    return Settings()
