\# Car Rental AI — Hệ thống Cho thuê Xe Thông minh



> Tối ưu giá thuê \& tư vấn đặt xe bằng AI · Đồ án Chuyên đề tổng hợp .NET + DevOps + AI (10 tuần)



\[!\[.NET 10](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com)

\[!\[React](https://img.shields.io/badge/React-Vite-61DAFB)](https://vitejs.dev)

\[!\[FastAPI](https://img.shields.io/badge/FastAPI-Python-009688)](https://fastapi.tiangolo.com)

\[!\[SQL Server](https://img.shields.io/badge/SQL%20Server-2019-CC2927)](https://www.microsoft.com/sql-server)

\[!\[Docker](https://img.shields.io/badge/Docker-Compose-2496ED)](https://docs.docker.com/compose/)



\## Tổng quan



Khách chọn xe, chọn ngày thuê → hệ thống gọi AI dự đoán giá tối ưu theo mùa, loại xe, thời hạn thuê; đồng thời chatbot tư vấn xe phù hợp. Nếu AI lỗi hoặc quá 2 giây không phản hồi, hệ thống tự fallback về giá niêm yết — người dùng không bao giờ bị chặn.



\- \*\*Backend:\*\* .NET 10, Clean Architecture + Modular Monolith, port 5001

\- \*\*AI Service:\*\* FastAPI (Python) — XGBoost / Random Forest + Gemini, port 5002, chạy độc lập ngoài monolith

\- \*\*Frontend:\*\* React + Vite, port 3000

\- \*\*Database:\*\* SQL Server, port 1433



\## Kiến trúc



\*\*Nhãn ngắn:\*\* Clean Architecture, Modular Monolith · \*\*Khung 3 cột:\*\* 1.2 Clean/Onion + 2.4 Client-Server (backend là 2.2 Modular Monolith) + 3.1 Request-Response



```mermaid

flowchart LR

&#x20;   FE\[React + Vite :3000] -->|HTTP JSON + JWT| API\[.NET 10 API :5001]

&#x20;   API -->|EF Core| DB\[(SQL Server :1433)]

&#x20;   API -->|HTTP timeout 2s retry 2| AI\[FastAPI :5002]

&#x20;   AI -->|/predict-price| ML\[XGBoost / RF]

&#x20;   AI -->|/chat| Gemini\[Gemini API]

```



\- Code: Clean Architecture 5 project, bên trong Domain/Application chia module Cars / Bookings / Customers

\- Hệ thống: Client-Server — React là client, .NET API và FastAPI là server

\- Tích hợp .NET ↔ AI: Request-Response đồng bộ (HTTP), không dùng queue/broker — xem \[docs/decisions.md](docs/decisions.md) (ADR-003)



Chi tiết: \[docs/architecture.md](docs/architecture.md)



\## Công nghệ



| Lớp | Tech | Ghi chú |

|---|---|---|

| Backend | .NET 10, EF Core, JWT | Clean Arch 5 project, Modular Monolith |

| AI | FastAPI, XGBoost/RF, Gemini | 2 endpoint: /predict-price, /chat |

| Frontend | React + Vite | Gọi API qua JWT |

| DB | SQL Server | 6 bảng: Customer, Car, CarImage, Booking, Payment, ReturnRecord |

| DevOps | Docker Compose, GitHub Actions | CI build xanh, CD Railway/Render |



\## Cấu trúc repo



```

CarRental.sln

├── src/CarRental.Domain/           # Module: Cars / Bookings / Customers / Common

├── src/CarRental.Application/      # Module: Cars / Bookings / Customers

├── src/CarRental.Infrastructure/   # EF Core, PDF, HttpClient gọi AI

├── src/CarRental.API/              # Composition root, Controllers

├── tests/CarRental.Tests/

├── ai-service/                     # FastAPI — service độc lập (Năng)

├── frontend/                       # React + Vite (Việt Anh)

├── docs/

│   ├── api-contract.md

│   ├── architecture.md

│   ├── decisions.md

│   └── erd.dbml

└── docker-compose.yml

```



Quy tắc: Domain không tham chiếu gì. Application chỉ tham chiếu Domain. Mỗi module nghiệp vụ nằm trong folder `Modules/<TênModule>` — chỉ là quy ước đặt folder.



\## Phân công



| TV | Họ tên | Vai trò |

|---|---|---|

| A | Nguyễn Vũ Dũng (nhóm trưởng) | Kiến trúc, ERD, API contract, board, review PR |

| B | Đỗ Anh Tuấn | Backend nghiệp vụ (Auth, Car, Booking, Payment, PDF) |

| C | Nguyễn Minh Năng | AI Service FastAPI |

| D | Lê Việt Anh | Frontend React |

| E | Vũ Duy Tiến | Docker Compose, CI/CD, deploy |



\## Bắt đầu nhanh



```bash

git clone https://github.com/nguyenvudung365-commits/car-rental-ai.git

cd car-rental-ai



\# Backend (.NET 10)

dotnet build

dotnet run --project src/CarRental.API



\# Hoặc chạy full stack bằng Docker

docker compose up --build

```



\- API Swagger: http://localhost:5001/swagger

\- Frontend: http://localhost:3000

\- AI Service docs: http://localhost:5002/docs



Biến môi trường để trong `.env` (không commit): `JWT\_KEY`, `GEMINI\_API\_KEY`, `ConnectionStrings\_\_Default`.



\## Tài liệu



\- \[API Contract](docs/api-contract.md) — endpoint, mã lỗi, ví dụ request/response

\- \[Architecture](docs/architecture.md) — kiến trúc đã chốt

\- \[Decisions \& Phân rã chức năng](docs/decisions.md) — ADR + sơ đồ mindmap nghiệp vụ

\- \[ERD](docs/erd.dbml) — 6 bảng



\## Quy tắc giá



```

FinalPricePerDay = COALESCE(OverridePrice, PredictedPrice, Car.BasePricePerDay)

Giới hạn \[400.000đ, 1.500.000đ] / ngày

```



\## Quy trình Git



`feature/\*` từ `develop` → Pull Request → 1 approval → Squash merge. Không push thẳng vào `main` / `develop`.



