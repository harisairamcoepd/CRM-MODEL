from __future__ import annotations

import logging
import time
from pathlib import Path
from uuid import uuid4

from fastapi import FastAPI, HTTPException, Request, status
from fastapi.exceptions import RequestValidationError
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import FileResponse, JSONResponse

from app.api.routes import admin, auth, chatbot, demo, events, health, leads, staff, stats
from app.core.config import get_settings
from app.core.database import SessionLocal, check_db_connection, init_db
from app.core.exceptions import AppException
from app.core.logging import setup_logging
from app.services.auth_service import AuthService

settings = get_settings()
setup_logging()
logger = logging.getLogger(__name__)
dashboard_file = Path(r"C:\Users\PC\Downloads\sales_funnel_crm_dashboard.html")

app = FastAPI(
    title=settings.app_name,
    version="1.0.0",
    docs_url="/docs",
    redoc_url="/redoc",
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=settings.cors_origins or ["http://localhost:5099"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


@app.get("/", include_in_schema=False)
def dashboard() -> FileResponse:
    if not dashboard_file.exists():
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Dashboard file not found")

    return FileResponse(dashboard_file)


@app.get("/admin", include_in_schema=False)
def admin_dashboard() -> FileResponse:
    if not dashboard_file.exists():
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Dashboard file not found")

    return FileResponse(dashboard_file)


@app.middleware("http")
async def request_logger(request: Request, call_next):
    request_id = str(uuid4())
    start = time.perf_counter()

    try:
        response = await call_next(request)
    except Exception:
        elapsed_ms = (time.perf_counter() - start) * 1000
        logger.exception(
            "Unhandled request error | request_id=%s method=%s path=%s duration_ms=%.2f",
            request_id,
            request.method,
            request.url.path,
            elapsed_ms,
        )
        raise

    elapsed_ms = (time.perf_counter() - start) * 1000
    response.headers["X-Request-ID"] = request_id

    logger.info(
        "HTTP request | request_id=%s method=%s path=%s status=%s duration_ms=%.2f",
        request_id,
        request.method,
        request.url.path,
        response.status_code,
        elapsed_ms,
    )
    return response


@app.exception_handler(AppException)
async def app_exception_handler(_: Request, exc: AppException) -> JSONResponse:
    return JSONResponse(
        status_code=exc.status_code,
        content={
            "code": exc.code,
            "message": exc.message,
        },
    )


@app.exception_handler(RequestValidationError)
async def validation_exception_handler(_: Request, exc: RequestValidationError) -> JSONResponse:
    return JSONResponse(
        status_code=status.HTTP_422_UNPROCESSABLE_ENTITY,
        content={
            "code": "validation_error",
            "message": "Validation failed",
            "details": exc.errors(),
        },
    )


@app.exception_handler(HTTPException)
async def http_exception_handler(_: Request, exc: HTTPException) -> JSONResponse:
    return JSONResponse(
        status_code=exc.status_code,
        content={
            "code": "http_error",
            "message": str(exc.detail),
        },
    )


@app.exception_handler(Exception)
async def unhandled_exception_handler(_: Request, exc: Exception) -> JSONResponse:
    logger.exception("Unhandled application exception: %s", exc)
    return JSONResponse(
        status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
        content={
            "code": "internal_server_error",
            "message": "An unexpected server error occurred.",
        },
    )


@app.on_event("startup")
def on_startup() -> None:
    check_db_connection()
    init_db()

    db = SessionLocal()
    try:
        AuthService(db).seed_default_users()
    finally:
        db.close()

    logger.info("Application startup completed successfully")


@app.get("/")
def root() -> dict:
    return {
        "service": settings.app_name,
        "status": "running",
        "docs": "/docs",
    }


app.include_router(health.router)
app.include_router(auth.router)
app.include_router(admin.router)
app.include_router(leads.router)
app.include_router(demo.router)
app.include_router(stats.router)
app.include_router(staff.router)
app.include_router(chatbot.router)
app.include_router(events.router)
