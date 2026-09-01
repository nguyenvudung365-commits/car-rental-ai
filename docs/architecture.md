\# Kiến trúc hệ thống — Car Rental AI



> Tài liệu chốt (freeze) kiến trúc tuần 1. Mọi thay đổi phải qua Pull Request + ADR mới.

> Phiên bản: 1.0 — cập nhật 09/2026



\## 1. Ba tầng kiến trúc (đừng nhầm với nhau)



| Tầng | Trả lời câu hỏi | Công cụ |

|---|---|---|

| Code architecture | Code tổ chức bên trong .NET thế nào? | Clean Architecture, 5 project |

| System architecture | Hệ thống gồm những service nào, nói chuyện ra sao? | Sơ đồ mục 2 |

| Integration architecture | .NET gọi AI bằng cơ chế gì? | HTTP sync, mục 3 |



\## 2. System architecture



```mermaid

flowchart LR

&#x20;   FE\[React + Vite :3000] -->|HTTP JSON + JWT| API\[.NET 10 API :5001]

&#x20;   API -->|EF Core| DB\[(SQL Server :1433)]

&#x20;   API -->|HTTP, timeout 2s, retry 2| AI\[FastAPI AI Service :5002]

&#x20;   AI -->|/predict-price| ML\[XGBoost / Random Forest]

&#x20;   AI -->|/chat| Gemini\[Gemini API]

```



| Thành phần | Port | Tech | Phụ trách |

|---|---|---|---|

| Frontend | 3000 | React + Vite | Lê Việt Anh |

| .NET API | 5001 | .NET 10, Clean Architecture | Nguyễn Vũ Dũng + Đỗ Anh Tuấn |

| AI Service | 5002 | FastAPI (Python), XGBoost/RF, Gemini | Nguyễn Minh Năng |

| Database | 1433 | SQL Server | Nguyễn Vũ Dũng |

| CI/CD + Docker | — | GitHub Actions, Docker Compose | Vũ Duy Tiến |



\## 3. Integration architecture (.NET ↔ AI) — ĐÃ CHỐT



\- \*\*Cơ chế:\*\* HTTP request-response đồng bộ. .NET là client, FastAPI là server.

\- \*\*Timeout:\*\* 2 giây. \*\*Retry:\*\* 2 lần.

\- \*\*Fallback:\*\* AI lỗi/hết timeout → API trả giá niêm yết `Car.BasePricePerDay`, đánh dấu `isFallback: true` trong response. Trải nghiệm người dùng không bao giờ chết vì AI.

\- \*\*Correlation ID:\*\* mỗi request sinh 1 GUID, gửi kèm header `X-Correlation-Id`, log ở cả 2 phía để trace.

\- \*\*Không dùng message queue / pub-sub (RabbitMQ, Kafka...):\*\* quyết định giá là đồng bộ, người dùng chờ kết quả ngay. Thêm broker = thêm hạ tầng phải vận hành, không giải quyết vấn đề nào của bài toán này.

\- \*\*MassTransit / Wolverine:\*\* không dùng (chỉ có nghĩa với message bus). \*\*MediatR:\*\* không bắt buộc, chỉ là quy ước code trong Application layer nếu nhóm thấy hữu ích.



\## 4. Code architecture — Clean Architecture .NET 10



```

CarRental.sln

├── src/CarRental.Domain          → KHÔNG tham chiếu gì (không EF, không ASP.NET)

├── src/CarRental.Application     → chỉ tham chiếu Domain

├── src/CarRental.Infrastructure  → tham chiếu Application (EF Core, PDF, HTTP client)

├── src/CarRental.API             → tham chiếu Application + Infrastructure (composition root)

└── tests/CarRental.Tests         → tham chiếu API

```



Quy tắc phụ thuộc: mũi tên chỉ từ ngoài vào trong. Domain không bao giờ biết đến database hay web.



\## 5. Quy tắc giá (business rule)



```

FinalPricePerDay = COALESCE(OverridePrice, PredictedPrice, Car.BasePricePerDay)

Chặn trong khoảng \[400 000đ, 1 500 000đ] / ngày

```



\## 6. Bảo mật



\- JWT Bearer cho mọi endpoint trừ `/api/auth/\*`.

\- Secret (JWT key, Gemini key, chain string) đi qua `.env` local và GitHub Secrets trên CI — \*\*không bao giờ commit vào repo\*\*.



\## 7. Liên kết



\- Chi tiết endpoint: \[`api-contract.md`](api-contract.md)

\- Bảng thực thể: \[`../docs/erd.dbml`](erd.dbml)

\- Lý do chọn HTTP sync: xem ADR-001 trong `decisions.md`

