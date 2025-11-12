from fastapi import APIRouter, HTTPException, Query
from typing import Optional
from services import AzureCostService

router = APIRouter()
cost_service = AzureCostService()


@router.get("/")
async def root():
    """Root endpoint."""
    return {
        "message": "Azure SmartCost Monitoring API",
        "version": "1.0.0",
        "status": "active"
    }


@router.get("/health")
async def health_check():
    """Health check endpoint."""
    return {"status": "healthy"}


@router.get("/subscription")
async def get_subscription():
    """Get Azure subscription information."""
    try:
        return cost_service.get_subscription_info()
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@router.get("/costs/summary")
async def get_cost_summary(days: int = Query(default=30, ge=1, le=365)):
    """
    Get cost summary for the specified period.
    
    Args:
        days: Number of days to query (1-365, default: 30)
    """
    try:
        return cost_service.get_cost_summary(days=days)
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@router.get("/costs/by-resource-group")
async def get_costs_by_resource_group():
    """Get costs grouped by resource group."""
    try:
        return cost_service.get_cost_by_resource_group()
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@router.get("/costs/forecast")
async def get_cost_forecast(days: int = Query(default=30, ge=1, le=365)):
    """
    Get cost forecast for the specified period.
    
    Args:
        days: Number of days to forecast (1-365, default: 30)
    """
    try:
        return cost_service.get_cost_forecast(days=days)
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))
