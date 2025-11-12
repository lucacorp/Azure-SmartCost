from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from api import router
from config import settings

# Create FastAPI application
app = FastAPI(
    title="Azure SmartCost Monitoring API",
    description="API for monitoring and analyzing Azure costs",
    version="1.0.0",
    debug=settings.debug
)

# Configure CORS
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Include routes
app.include_router(router, prefix="/api/v1", tags=["Azure Cost Management"])


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(
        "main:app",
        host=settings.api_host,
        port=settings.api_port,
        reload=settings.debug
    )
