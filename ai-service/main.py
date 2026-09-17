"""
Car Rental AI Service - FastAPI
Ports: /health, /predict-price, /chat
Owner: TV C - Nguyễn Minh Năng
"""
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from typing import Optional
import os

app = FastAPI(title="Car Rental AI Service", version="1.0.0")

# CORS cho .NET API
app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:5001", "http://api:5001"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


class PredictPriceRequest(BaseModel):
    car_type: int
    seats: int
    car_age_years: int
    base_price_per_day: float


class PredictPriceResponse(BaseModel):
    predicted_price_per_day: float
    confidence: float
    model_used: str


class ChatRequest(BaseModel):
    message: str
    correlation_id: Optional[str] = None


class ChatResponse(BaseModel):
    reply: str
    correlation_id: Optional[str] = None


@app.get("/health")
def health():
    """Health check endpoint"""
    return {"status": "ok", "service": "ai-service"}


@app.post("/predict-price", response_model=PredictPriceResponse)
def predict_price(req: PredictPriceRequest):
    """
    Dự đoán giá thuê xe theo ngày (XGBoost/Random Forest)
    TODO TV C: train model + load model + predict
    """
    # Stub: trả giá base (logic thật sẽ dùng model ML)
    return PredictPriceResponse(
        predicted_price_per_day=req.base_price_per_day,
        confidence=0.0,
        model_used="stub_fallback"
    )


@app.post("/chat", response_model=ChatResponse)
def chat(req: ChatRequest):
    """
    Chatbot tư vấn thuê xe (Gemini API)
    TODO TV C: gọi Gemini API với prompt context
    """
    gemini_key = os.getenv("GEMINI_API_KEY", "")
    if not gemini_key or gemini_key == "CHANGE_ME_your_gemini_api_key_here":
        raise HTTPException(status_code=503, detail="Gemini API key chưa cấu hình")

    # Stub: echo tin nhắn
    return ChatResponse(
        reply=f"[Stub] Bạn hỏi: {req.message}",
        correlation_id=req.correlation_id
    )


if __name__ == "__main__":
    import uvicorn
    port = int(os.getenv("PORT", 5002))
    uvicorn.run(app, host="0.0.0.0", port=port)
