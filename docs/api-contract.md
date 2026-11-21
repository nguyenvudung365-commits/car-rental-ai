# API Contract Theo Code Hiện Tại

Đối chiếu ngày 26/10/2026 với code local, bao gồm thay đổi chưa commit. Đây là mô tả implementation, không phải danh sách API dự kiến. Nguồn: `src/CarRental.API/Controllers/`, DTO trong Application và `ai-service/main.py`.

## 1. Quy Ước

- .NET: `http://localhost:5001`; API nghiệp vụ có prefix `/api`.
- Controller .NET giữ tên thuộc tính DTO dạng PascalCase; object vô danh như `{ items, total }`, `{ code, message }` giữ tên chữ thường. Request JSON không phân biệt hoa/thường theo cấu hình web mặc định.
- Enum trong DTO xe được trả dạng số. `BookingDto.Status` là chuỗi vì service gọi `ToString()`.
- Tiền là VNĐ; giá định giá là VNĐ/ngày. Backend dùng `decimal`, FastAPI stub dùng `float`.
- JWT: `Authorization: Bearer <token>`. Role: `Customer`, `Staff`, `Admin`.
- Middleware nghiệp vụ trả `{ "code": "...", "message": "..." }`; `code` có thể null. Model validation trả 400 với `code = "validation_error"`. Không phải mọi lỗi framework/exception đều có định dạng này.
- .NET trả header `X-Correlation-Id`. Booking tạo ID riêng để lưu cùng đơn và truyền sang AI; ID này có thể khác header của request phía ngoài.

### Enum Đang Dùng

| Enum | Giá trị |
|---|---|
| UserRole | 1 Customer, 2 Staff, 3 Admin |
| CarType | 1 Sedan, 2 SUV, 3 Hatchback, 4 MPV, 5 Pickup, 6 Luxury |
| CarStatus | 1 SanSang, 2 DangThue, 3 BaoTri, 4 NgungHoatDong |
| BookingStatus | 1 ChoXacNhan, 2 DaXacNhan, 3 DangThue, 4 DaTraXe, 5 DaHuy |
| PaymentStatus | 1 ChuaThanhToan, 2 DaCoc, 3 DaThanhToan, 4 DaHoanTien |
| PaymentMethod | 1 TienMat, 2 ChuyenKhoan, 3 TheNoiDia, 4 ViDienTu |

## 2. Health Và Auth

| Method | Route | Quyền | Kết quả thành công |
|---|---|---|---|
| GET | `/health`, `/api/health` | Public | 200 `{ "status": "ok", "service": "CarRental.API" }` |
| POST | `/api/auth/register` | Public | 200 `AuthResponse` |
| POST | `/api/auth/login` | Public | 200 `AuthResponse` |
| PUT | `/api/auth/me` | Đăng nhập | 204 |

Register request: `FullName`, `Email`, `PhoneNumber`, `Password`. Tên/email/điện thoại/mật khẩu bắt buộc; mật khẩu tối thiểu 8 ký tự. Email được trim/lowercase. Email hoặc điện thoại trùng trả 409 `EMAIL_EXISTS` / `PHONE_EXISTS`. Tài khoản mới luôn là Customer.

Login request: `Email`, `Password`; sai thông tin trả 401. `AuthResponse` có `Token`, `FullName`, `Role`, không có `CustomerId`.

Update profile request: `FullName`, `IdentityNumber`, `Address`. Không có `GET /api/auth/me` trong controller hiện tại.

## 3. Xe Và Ảnh

| Method | Route | Quyền | Kết quả thành công |
|---|---|---|---|
| GET | `/api/cars` | Public | 200 `{ "items": [CarDto], "total": 0 }` |
| GET | `/api/cars/{id}` | Public | 200 `CarDto`; không có xe: 404 |
| POST | `/api/cars` | Admin/Staff | 200 `CarDto` |
| PUT | `/api/cars/{id}` | Admin/Staff | 204 |
| DELETE | `/api/cars/{id}` | Admin/Staff | 204 |
| POST | `/api/cars/{carId}/images` | Admin/Staff | 200 `CarImageDto` |
| PUT | `/api/cars/{carId}/images/{imageId}/primary` | Admin/Staff | 204 |
| DELETE | `/api/cars/{carId}/images/{imageId}` | Admin/Staff | 204 |

Filter xe: `CarType`, `MinPrice`, `MaxPrice`, `Status`, `Page=1`, `PageSize=10`, `AvailableFrom`, `AvailableTo`. Giá lọc là `BasePricePerDay`, không phải giá AI. Chưa có filter số chỗ, nhiên liệu hoặc hộp số.

Lọc xe còn trống: truyền cả `AvailableFrom` và `AvailableTo` (ISO date). Loại xe có booking `ChoXacNhan`/`DaXacNhan`/`DangThue` chồng khoảng `[Start, End)` theo cùng quy tắc chống trùng lịch khi đặt. Thiếu một đầu hoặc `AvailableTo <= AvailableFrom`: 400. Filter này không xét `Car.Status`; kết hợp thêm `Status=SanSang` nếu cần.

`CarDto`: `Id`, `CarType`, `Brand`, `Model`, `LicensePlate`, `Seats`, `CarAgeYears`, `BasePricePerDay`, `Status`, `OverridePrice`, `Description`, `Images`, `Transmission?`, `FuelType?`, `ManufactureYear?`.

Enum số: `Transmission` 1=Số sàn, 2=Số tự động; `FuelType` 1=Xăng, 2=Dầu, 3=Điện, 4=Hybrid. `ManufactureYear` 1990–2100 (chỉ hiển thị, giá AI dùng `CarAgeYears`). Ba trường này tùy chọn ở create/update request.

Create request: `CarType`, `Brand`, `Model`, `LicensePlate`, `Seats`, `CarAgeYears`, `BasePricePerDay`, `Description`. DTO create kiểm tra số chỗ 1–16, tuổi xe 0–50, giá > 0; không nên suy ra DTO update có cùng validation.

Update request thêm `Status`, `OverridePrice`. Biển số trùng trả 409 `PLATE_EXISTS`. Xóa xe đang có `Status = DangThue` trả 409 `CAR_IN_USE`; code hiện xóa cứng, chưa kiểm tra mọi booking tương lai của xe.

Upload dùng multipart field `file`, chỉ nhận WebP tối đa 5 MB, kiểm tra extension/MIME/header. `CarImageDto` có `Id`, `FilePath`, `IsPrimary`. `FilePath` hiện là object key dạng `cars/<guid>.webp`, không phải URL công khai đầy đủ. Ảnh đầu là ảnh chính; khi xóa ảnh chính, service chọn ảnh có ID nhỏ nhất còn lại. URL truy cập ảnh và casing phía UI còn cần đối chiếu khi tích hợp.

## 4. Đặt Xe Và Báo Giá

Toàn bộ `BookingsController` yêu cầu đăng nhập.

| Method | Route | Quyền | Kết quả thành công |
|---|---|---|---|
| POST | `/api/bookings` | Đăng nhập | 201 `BookingDto`, có Location |
| POST | `/api/bookings/estimate` | Đăng nhập | 200 `BookingEstimateDto` |
| GET | `/api/bookings/car/{carId}/booked-ranges` | Đăng nhập | 200 danh sách `{ StartDate, EndDate }` |
| GET | `/api/bookings` | Đăng nhập | 200 `{ "items": [BookingDto], "total": 0 }` |
| GET | `/api/bookings/{id}` | Chủ đơn hoặc Admin/Staff | 200 `BookingDto` |
| POST | `/api/bookings/{id}/confirm` | Admin/Staff | 200 `BookingDto` |
| POST | `/api/bookings/{id}/cancel` | Chủ đơn hoặc Admin/Staff | 200 `BookingDto` |

Create request: `CarId`, `StartDate`, `EndDate`, `Note` (tùy chọn). Estimate request: `CarId`, `StartDate`, `EndDate`. Không có `HasDriver`.

```json
{
  "CarId": 1,
  "StartDate": "2026-11-07",
  "EndDate": "2026-11-10",
  "Note": "Nhận xe buổi sáng"
}
```

- Create kiểm tra start < end, ngày bắt đầu không ở quá khứ, xe tồn tại và `SanSang`.
- Số ngày là `(EndDate - StartDate).Days`; chưa có quy tắc làm tròn thuê theo giờ. Nên dùng ngày trọn vẹn khi tích hợp.
- Gọi AI trước transaction; sau đó khóa dòng xe bằng `UPDLOCK, ROWLOCK`, kiểm tra overlap và lưu đơn trong cùng transaction.
- Overlap dùng khoảng nửa mở `[StartDate, EndDate)`, chỉ xét booking chưa xóa thuộc `ChoXacNhan`, `DaXacNhan`, `DangThue`. Hai khoảng sát nhau được chấp nhận.
- Trùng lịch: 409 `BOOKING_OVERLAP`; xe không sẵn sàng: 409 `CAR_NOT_AVAILABLE`.
- Confirm chỉ chuyển `ChoXacNhan` → `DaXacNhan`. Cancel chặn `DangThue`, `DaTraXe`, `DaHuy`; lỗi trạng thái trả 409 `INVALID_STATUS`.
- Estimate hiện chỉ kiểm tra start < end và xe tồn tại; không kiểm tra đầy đủ ngày quá khứ, trạng thái xe hay overlap như create.
- Danh sách booking tự giới hạn Customer theo tài khoản; Admin/Staff xem được toàn bộ. Filter: `CustomerId`, `CarId`, `Status`, `StartDateFrom`, `StartDateTo`, `Page=1`, `PageSize=10`; DTO giới hạn Page >= 1 và PageSize 1–200.

`BookingDto`: `Id`, `CustomerId`, `CustomerName`, `CarId`, `CarName`, `LicensePlate`, `StartDate`, `EndDate`, `RentalDays`, `PredictedPricePerDay`, `FinalPricePerDay`, `TotalPrice`, `IsFallback`, `Status`, `CorrelationId`, `Note`, `CreatedAt`.

`BookingEstimateDto`: `RentalDays`, `BasePricePerDay`, `PredictedPricePerDay`, `FinalPricePerDay`, `TotalPrice`, `IsFallback`.

```text
FinalPricePerDay = clamp(OverridePrice ?? PredictedPrice ?? BasePricePerDay, 400000, 1500000)
TotalPrice = FinalPricePerDay * RentalDays
```

Fallback client trả giá gốc trong `PredictedPrice` cùng `IsFallback = true`; trường giá dự đoán lưu trên booking vì vậy có thể chứa giá gốc, không nhất thiết null.

## 4b. Thanh Toán, Nhận Xe Và Trả Xe

Nguồn: `RentalController.cs`, `RentalService.cs`. Tất cả yêu cầu đăng nhập; Customer chỉ thao tác booking của mình (sai chủ đơn: 403).

| Method | Route | Quyền | Kết quả thành công |
|---|---|---|---|
| POST | `/api/bookings/{bookingId}/payments` | Chủ đơn hoặc Admin/Staff | 201 `PaymentDto` (trạng thái `ChuaThanhToan`) |
| GET | `/api/bookings/{bookingId}/payments` | Chủ đơn hoặc Admin/Staff | 200 `BookingPaymentsDto` |
| POST | `/api/payments/{paymentId}/mark-paid` | Admin/Staff | 200 `PaymentDto` |
| POST | `/api/bookings/{bookingId}/pickup` | Admin/Staff | 200 `BookingDto` (`DaXacNhan` → `DangThue`) |
| POST | `/api/bookings/{bookingId}/return` | Admin/Staff | 200 `ReturnRecordDto` (`DangThue` → `DaTraXe`) |
| GET | `/api/bookings/{bookingId}/return` | Chủ đơn hoặc Admin/Staff | 200 `ReturnRecordDto`, 404 nếu chưa trả |

- Create payment: `Amount` (> 0), `Method` (1–4), `TransactionCode` (tùy chọn). Chỉ cho booking đã xác nhận trở đi (không phải `ChoXacNhan`/`DaHuy`), lỗi: 409 `INVALID_STATUS`. Tổng các khoản chưa hoàn tiền không được vượt số phải trả: 409 `PAYMENT_EXCEEDS_DUE`. Một booking có nhiều payment.
- Số phải trả = `TotalPrice`, hoặc `ReturnRecord.FinalAmount` sau khi đã trả xe.
- Mark-paid: `Status` chỉ nhận 2 (`DaCoc`) hoặc 3 (`DaThanhToan`), chỉ áp dụng cho payment đang `ChuaThanhToan`, ghi `PaidAt`.
- `BookingPaymentsDto`: `Items`, `TotalPrice`, `AmountToPay`, `PaidAmount` (chỉ tính `DaCoc`/`DaThanhToan`), `AmountDue`.
- Return request: `ReturnedAt` (tùy chọn, mặc định bây giờ), `DamageFee` (>= 0), `DamageNote`. Không cho trước `StartDate`. Chỉ nhận khi booking `DangThue`; đã có biên bản: 409 `RETURN_EXISTS`.
- Công thức: `LateDays = max(0, ngày trả − ngày EndDate)` (theo ngày lịch); `LateFee = LateDays × FinalPricePerDay`; `FinalAmount = TotalPrice + LateFee + DamageFee`. `StaffId` lấy từ JWT.
- `ReturnRecordDto`: `BookingId`, `ReturnedAt`, `EndDate`, `LateDays`, `LateFee`, `DamageFee`, `DamageNote`, `TotalPrice`, `FinalAmount`, `StaffId`.
- Chưa tự đổi `Car.Status` theo vòng đời thuê, chưa có refund/`DaHoanTien` qua API, chưa có hợp đồng PDF.

## 4c. Báo Cáo (chỉ Admin)

| Method | Route | Trả về |
|---|---|---|
| GET | `/api/reports/revenue?from=&to=` | 200 `{ totalRevenue, totalBookings, byMonth: [{ month: "2026-11", revenue, bookings }] }` |
| GET | `/api/reports/occupancy?from=&to=` | 200 `{ totalCars, avgOccupancyRate, byCar: [{ carId, licensePlate, occupancyRate }] }` |

- JSON trả về dạng PascalCase như các API khác (`TotalRevenue`, `ByMonth`, `Month`...); bảng trên viết camelCase cho dễ đọc.
- `from`/`to` là ngày, gồm cả hai đầu; mặc định 30 ngày gần nhất; `from > to` hoặc quá 366 ngày: 400.
- Doanh thu = tổng `Payment` có `Status` là `DaCoc`/`DaThanhToan` và `PaidAt` trong khoảng, nhóm theo tháng của `PaidAt`. `bookings` đếm booking khác nhau có thu tiền. Không trừ hoàn tiền; payment `ChuaThanhToan` không tính.
- Lấp đầy = số ngày thuê (booking `DaXacNhan`/`DangThue`/`DaTraXe`, cắt theo khoảng) / số ngày trong khoảng, giá trị 0..1, tính cho xe chưa xóa và khác `NgungHoatDong`.

## 5. .NET → FastAPI

Nguồn: `IAiPricingClient.cs`, `AiPricingClient.cs`, `ai-service/main.py`. Trong Compose, .NET gọi `http://ai:5002/predict-price`.

### POST /predict-price

Payload thực tế do .NET gửi:

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

FastAPI hiện chỉ khai báo `car_type`, `seats`, `car_age_years`, `base_price_per_day`. Hai trường `rental_days` và `start_date` đang bị bỏ qua theo cấu hình Pydantic mặc định, chưa tham gia dự đoán.

Response stub hiện tại:

```json
{
  "predicted_price_per_day": 500000,
  "confidence": 0.0,
  "model_used": "stub_fallback"
}
```

- Chưa có model ML; giá trả về bằng `base_price_per_day`.
- .NET chỉ đọc `predicted_price_per_day`; chưa dùng `confidence` hoặc `model_used`. Stub trả 200 nên .NET hiện coi là thành công, `IsFallback = false`.
- Timeout mặc định 2 giây **mỗi lần gọi**, retry 2 lần: tối đa 3 lần gọi. Backoff 200 ms rồi 400 ms; không đợi sau lần cuối. Đây không phải deadline tổng 2 giây.
- Client retry khi HTTP không thành công, lỗi mạng hoặc timeout. JSON sai cú pháp thì dừng retry; thiếu trường giá thì tiếp tục retry. Hết lượt thì trả giá gốc và `IsFallback = true`.
- Header `X-Correlation-Id` ưu tiên ID trong request application, sau đó mới lấy header HTTP hiện tại. FastAPI stub chưa đọc/log header này.

### GET /health

200 `{ "status": "ok", "service": "ai-service" }`. Health hiện không kiểm tra model hay Gemini.

### POST /chat

FastAPI request: `message`, `correlation_id` (tùy chọn). Response: `reply`, `correlation_id`. Thiếu key hoặc key bằng placeholder hiện tại: 503 với `detail`. Có key: echo `[Stub] Bạn hỏi: ...`; chưa gọi Gemini, chưa có lịch sử hội thoại hoặc ngữ cảnh xe.

## 6. Chức Năng Chưa Có Backend

- Chưa có `/api/ai/quote`: báo giá đang đi qua `/api/bookings/estimate` có JWT.
- Chưa có `/api/ai/chat`: `ChatWidget.jsx` đã gọi route này nhưng chưa có controller/client tương ứng ở .NET.
- Chưa có khôi phục mật khẩu.
- Payment, nhận/trả xe đã có API (mục 4b); báo cáo đã có API (mục 4c).

Khi triển khai AI thật cần thống nhất bổ sung feature thuê, validation, hành vi model chưa sẵn sàng, ý nghĩa confidence, nguồn ngữ cảnh chatbot và schema session trước khi nối xuyên suốt.
