# Tổng quan dự án và việc giao cho Năng

Dự án: Car Rental AI (Nhóm 7, Chuyên đề tổng hợp). Tài liệu này tóm tắt nhóm đã làm gì, còn thiếu gì, và bốn việc giao cho Năng: AI định giá, chatbot, cổng thanh toán thật và đăng ký xác thực OTP.

Cập nhật ngày 16/11/2026, đối chiếu với nhánh `develop` trên GitHub và API contract. Khi nhận việc, hãy đọc lại code thật trước khi sửa. [api-contract.md](api-contract.md) là căn cứ chính cho API hiện có. [weekly-plan.md](weekly-plan.md) chỉ là kế hoạch, không phải trạng thái hoàn thành. Hợp đồng AI chi tiết hơn nằm ở [prompt-nang-ai.md](prompt-nang-ai.md).

## 1. Hệ thống gồm những gì

| Thành phần | Công nghệ | Cổng | Người phụ trách |
|---|---|---|---|
| Frontend | React + Vite, chạy qua nginx | 3000 | Việt Anh |
| Backend | .NET 10, Clean Architecture, Modular Monolith | 5001 | Tuấn |
| AI service | FastAPI (Python), độc lập | 5002 | Năng |
| Cơ sở dữ liệu | SQL Server 2022 | 1433 | Tuấn |
| Lưu ảnh xe | MinIO, chỉ nhận WebP | 9000 / 9001 | Tiến |

Luồng chung: React gọi .NET bằng JWT; .NET gọi FastAPI qua HTTP đồng bộ (timeout 2 giây mỗi lần, thử lại 2 lần, hết lượt thì dùng giá niêm yết và đặt `IsFallback = true`). Không có queue. Key bí mật (Gemini, cổng thanh toán) chỉ nằm ở server, không bao giờ xuống trình duyệt.

Ba vai trò: **Customer** (khách thuê), **Staff** (nhân viên), **Admin** (quản trị viên). Công ty tự cho thuê xe, chưa có vai trò chủ xe cá nhân.

Chạy cả hệ thống: `docker compose up --build` trong thư mục `car-rental-ai`, file `.env` copy từ `.env.example`. Dữ liệu mẫu: xem [scripts/README-seed-demo.md](../scripts/README-seed-demo.md).

## 2. Nhóm đã làm được

Đã merge vào `develop`:

- **Tài khoản:** đăng ký, đăng nhập JWT, phân quyền 3 vai trò.
- **Quản lý xe:** thêm/sửa/xóa xe, lọc theo loại và giá, phân trang; upload, chọn ảnh đại diện và xóa ảnh WebP lên MinIO.
- **Đặt xe:** báo giá trước khi đặt, tạo đơn có chặn trùng lịch bằng transaction và khóa dòng, xác nhận, hủy, xem lịch sử.
- **Gọi AI định giá:** client .NET đã có timeout, retry, fallback và test. Công thức giá chốt nằm ở .NET: `FinalPricePerDay = clamp(OverridePrice ?? PredictedPrice ?? BasePricePerDay, 400000, 1500000)`.
- **Hạ tầng:** Docker Compose, CI GitHub Actions (build .NET, build frontend, kiểm tra cú pháp AI).
- **Frontend:** danh sách và chi tiết xe, đặt xe, đăng nhập/đăng ký, đơn của tôi, các trang admin (xe, ảnh xe, đơn, trả xe, báo cáo), khung chat.

Đang làm dở: **PR #21** gồm API thanh toán, nhận xe, trả xe (tính phí trễ, phí hư hỏng) và hợp đồng PDF. CI đang đỏ; cần rebase lên `develop` mới rồi review trước khi merge. Việc 4.3 phụ thuộc PR này.

## 3. Nhóm còn thiếu

| Hạng mục | Tình trạng |
|---|---|
| AI định giá thật | FastAPI chỉ trả lại giá gốc (`model_used = stub_fallback`), chưa có dataset hay model |
| Chatbot | FastAPI chỉ echo; .NET chưa có `/api/ai/chat` dù frontend đã gọi route này |
| Thanh toán thật | Nhân viên nhập tay số tiền, hình thức, mã giao dịch rồi bấm "Cọc" hoặc "Đã thu". Khách không trả tiền được trên web |
| Xác thực OTP | Đăng ký xong dùng được ngay, không xác minh email |
| Báo cáo doanh thu | Có trang giao diện, chưa có API tính số liệu |
| Khôi phục mật khẩu | Chưa có |
| Giao diện | Chưa có trang giới thiệu trước đăng nhập, trang chủ sau đăng nhập còn trống (Dũng làm cùng Việt Anh) |
| Kiểm thử, bảo mật | Còn thiếu test tích hợp cho thanh toán/trả xe, rà soát bảo mật |
| Triển khai staging | Chưa làm (Tiến) |

## 4. Việc giao cho Năng

Việc 4.3 và 4.4 nằm ở backend .NET, không phải Python. Hãy trao đổi với Tuấn trước khi sửa `src/CarRental.*` để tránh đụng code đang làm. Quy trình: nhánh feature từ `develop`, mở PR vào `develop`, Tuấn review, CI xanh rồi mới merge.

### 4.1. AI định giá thật

Giữ nguyên route `POST /predict-price` và tên trường snake_case để .NET không phải sửa.

Request .NET đang gửi:

```json
{
  "car_type": 1,
  "car_age_years": 3,
  "seats": 5,
  "rental_days": 3,
  "base_price_per_day": 500000,
  "start_date": "2026-11-07"
}
```

- `car_type`: 1 Sedan, 2 SUV, 3 Hatchback, 4 MPV, 5 Pickup, 6 Luxury.
- FastAPI hiện bỏ qua `rental_days` và `start_date`. Hãy khai báo và dùng làm đặc trưng (số ngày thuê, cuối tuần, mùa cao điểm).

Response giữ nguyên tên trường; .NET chỉ đọc `predicted_price_per_day`:

```json
{ "predicted_price_per_day": 575000, "confidence": 0.8, "model_used": "random_forest_v1" }
```

Việc cần làm:
1. Tạo dataset mô phỏng, ghi rõ cách sinh và giả định trong báo cáo.
2. Huấn luyện Random Forest hoặc XGBoost, lưu model, nạp khi container khởi động.
3. Giá trả về là giá mỗi ngày, không phải tổng tiền. Override và tổng tiền thuộc về .NET.
4. Bỏ comment các thư viện ML trong `ai-service/requirements.txt`, cập nhật Dockerfile.
5. Viết test Python cho dự đoán, dữ liệu sai, model chưa nạp. Suy luận phải đủ nhanh vì .NET chờ tối đa 2 giây.

Nghiệm thu: booking thật nhận `IsFallback = false` với giá khác giá gốc; tắt AI thì vẫn đặt xe được với giá niêm yết.

### 4.2. Chatbot tư vấn bằng Gemini

Phần Python (`POST /chat`): gọi Gemini bằng key trong biến môi trường `GEMINI_API_KEY`, trả lời tiếng Việt, đưa danh mục xe hiện có vào prompt. Không gửi mật khẩu, JWT hay dữ liệu cá nhân không cần thiết cho Gemini. Thiếu key hoặc Gemini lỗi thì trả câu thông báo thân thiện. Chatbot không tự đặt hay xác nhận booking.

Phần .NET còn thiếu: thêm `POST /api/ai/chat`, chuyển tiếp sang FastAPI `/chat` kèm `X-Correlation-Id`. Chốt định dạng request/response với Tuấn và Việt Anh (đề xuất: `message` vào, `reply` ra).

### 4.3. Cổng thanh toán thật (đề xuất VNPay sandbox)

Chốt nhà cung cấp với Dũng trước khi bắt đầu; VNPay sandbox là đề xuất vì có môi trường thử miễn phí.

Hiện trạng:
- Payment có `Amount`, `Method`, `Status` (1 ChuaThanhToan, 2 DaCoc, 3 DaThanhToan, 4 DaHoanTien), mã giao dịch, thời điểm thanh toán. Một booking có nhiều payment.
- PR #21 có API tạo payment và `POST /api/payments/{id}/mark-paid` cho nhân viên; frontend là `PaymentsModal.jsx`.

Luồng cần xây:
1. Khách bấm "Thanh toán" trên đơn của mình. .NET tạo payment `ChuaThanhToan`, dựng URL thanh toán có chữ ký, trả về frontend.
2. Frontend chuyển khách sang trang cổng thanh toán (môi trường thử).
3. Cổng gọi lại server (IPN) và chuyển khách về trang kết quả. **Chỉ tin IPN đã kiểm tra chữ ký**, không tin tham số trên trang kết quả.
4. IPN phải idempotent: cổng có thể gọi nhiều lần cho cùng giao dịch.
5. Số tiền do server tính từ booking, không nhận từ trình duyệt.
6. Giữ nút ghi nhận tiền mặt cho nhân viên.

Bảo mật: hash secret và mã merchant chỉ để trong biến môi trường, `.env.example` chỉ chứa `CHANGE_ME`. IPN cần URL công khai; khi chạy local dùng ngrok hoặc cloudflared để thử.

Nghiệm thu: thanh toán thử thành công làm payment chuyển `DaThanhToan`; hủy hoặc thất bại giữ `ChuaThanhToan`; gửi lại cùng IPN không tạo thêm bản ghi; chữ ký sai bị từ chối; có test.

### 4.4. Đăng ký xác thực OTP qua email

Hiện trạng: `POST /api/auth/register` tạo tài khoản và trả token ngay.

Luồng đề xuất:
1. Đăng ký tạo tài khoản **chưa xác thực**, sinh OTP 6 chữ số, gửi qua email.
2. `POST /api/auth/verify-otp` nhận email và mã; đúng thì đánh dấu đã xác thực và trả token.
3. `POST /api/auth/resend-otp` gửi lại mã, có giới hạn tần suất.
4. Đăng nhập khi chưa xác thực thì trả lỗi rõ ràng, kèm cách gửi lại mã.

Yêu cầu kỹ thuật:
- Thêm trường `IsEmailVerified` vào `Customer` kèm migration EF. Tài khoản đã có (gồm tài khoản seed) đặt là đã xác thực để không bị khóa.
- Không lưu OTP dạng chữ rõ; lưu bản băm và thời điểm hết hạn (đề xuất 5 phút).
- Giới hạn số lần nhập sai (ví dụ 5) và tần suất gửi lại.
- Gửi email qua SMTP hoặc dịch vụ như Resend/SendGrid, cấu hình bằng biến môi trường, `.env.example` chỉ chứa giá trị mẫu.
- Frontend: Việt Anh thêm bước nhập mã vào trang đăng ký.

Nghiệm thu: đăng ký ra email có mã; mã đúng kích hoạt tài khoản; mã sai, hết hạn hoặc dùng lại đều bị từ chối; nhập sai quá số lần thì khóa tạm; có test.

## 5. Quy tắc chung

- Không commit secret. Dùng `.env` (đã nằm trong `.gitignore`) và `.env.example` chứa giá trị mẫu.
- Cập nhật [api-contract.md](api-contract.md) cùng PR khi thêm hoặc đổi API.
- Mỗi PR một mục tiêu, có hướng dẫn kiểm thử trong phần mô tả.
- Không viết lại lịch sử các nhánh dùng chung (`main`, `develop`).
- Muốn đổi hợp đồng giữa các phần (đường dẫn, tên trường) thì báo Dũng trước.

## 6. Các quyết định đã chốt

| Câu hỏi | Quyết định |
|---|---|
| Vai trò | Giữ 3 vai trò, đổi nhãn tiếng Việt trên giao diện: Khách thuê / Nhân viên / Quản trị viên |
| Cổng thanh toán | Cổng thật, Năng làm; nhà cung cấp chốt với Dũng (đề xuất VNPay sandbox) |
| Kênh OTP | Email, Năng làm |
| Giao diện | Thêm trang giới thiệu, trang chủ sau đăng nhập, làm lại trang xe; phần admin giữ nguyên |
