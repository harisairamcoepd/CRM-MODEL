from dataclasses import dataclass


@dataclass
class AppException(Exception):
    message: str
    status_code: int = 400
    code: str = "app_error"
