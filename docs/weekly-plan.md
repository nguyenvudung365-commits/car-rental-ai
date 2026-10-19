# Kế hoạch triển khai 10 tuần — Car Rental AI

> Đề tài: Hệ thống Cho thuê Xe Thông minh — Tối ưu Giá thuê và Tư vấn Đặt xe bằng AI
>
> Trưởng nhóm: Nguyễn Vũ Dũng (TV A)
>
> Repository: https://github.com/nguyenvudung365-commits/car-rental-ai
>
> Kiến trúc: Clean Architecture + Modular Monolith; hệ thống Client–Server; tích hợp .NET–AI bằng HTTP Request–Response.
>
> Tài liệu nền: `docs/architecture.md`, `docs/decisions.md`, `docs/api-contract.md`, `docs/erd.dbml`.

## 1. Mục tiêu sản phẩm

Xây dựng một hệ thống cho thuê xe gồm một frontend React, một backend .NET 10, một AI service FastAPI và SQL Server. Khách hàng có thể đăng ký, đăng nhập, xem xe, nhận giá thuê do AI đề xuất, đặt xe, thanh toán, nhận/trả xe và tải hợp đồng PDF. Nhân viên hoặc Admin có thể quản lý xe, xác nhận booking, ghi nhận thanh toán, lập biên bản trả xe và xem báo cáo.

Phạm vi AI gồm hai chức năng: dự đoán giá thuê theo đặc điểm xe và bối cảnh thuê; chatbot tư vấn xe. Khi AI không phản hồi trong thời gian quy định, hệ thống .NET phải dùng giá niêm yết làm fallback để luồng đặt xe không bị gián đoạn.

Sản phẩm cuối phải chạy được theo mô hình bốn service: frontend `:3000`, .NET API `:5001`, FastAPI AI `:5002` và SQL Server `:1433`. Backend .NET là một khối deploy duy nhất nhưng bên trong chia các module Cars, Bookings và Customers.

## 2. Phân công cố định

| Mã | Thành viên | Vai trò chính | Phạm vi chịu trách nhiệm |
|---|---|---|---|
| A | Nguyễn Vũ Dũng | Team Lead / Architecture | Clean Architecture, Modular Monolith, ERD, API contract, README, Risk Register, DoD, board, review PR và tích hợp cuối |
| B | Đỗ Anh Tuấn | Backend Developer | Auth/JWT, Customer, Car CRUD, Booking, Payment, ReturnRecord, hợp đồng PDF, test backend |
| C | Nguyễn Minh Năng | AI Developer | Dataset mô phỏng, model XGBoost/Random Forest, FastAPI `/predict-price`, `/chat`, test AI |
| D | Lê Việt Anh | Frontend Developer | React + Vite, router, auth UI, danh mục xe, booking UI, giá AI, thanh toán, chatbot, báo cáo |
| E | Vũ Duy Tiến | DevOps / Release | Docker Compose, Dockerfile, GitHub Actions, secrets, logging cơ bản, staging và release |

## 3. Quy ước làm việc

### 3.1. Git và Pull Request

Mỗi task được làm trên một nhánh tính năng tạo từ `develop`, ví dụ `feature/auth-jwt`, `feature/car-crud` hoặc `feature/ai-predict-price`. Không push trực tiếp vào `main` hoặc `develop`.

Quy trình bắt buộc là: cập nhật `develop` → tạo nhánh feature → code và test → push → mở Pull Request vào `develop` → yêu cầu Đỗ Anh Tuấn (GitHub `DoTuan3725`) review → sửa comment → CI xanh → đủ một approval → Squash and merge.

Mỗi PR chỉ nên tập trung vào một mục tiêu. Tiêu đề PR dùng dạng `feat:`, `fix:`, `test:`, `docs:` hoặc `chore:`. Mô tả PR phải có mục tiêu, file chính đã đổi, cách kiểm thử và ảnh chụp màn hình nếu có UI.

### 3.2. Definition of Ready

Một task chỉ được chuyển sang `In Progress` khi đã có người phụ trách, có mô tả đầu vào/đầu ra, biết rõ API hoặc file liên quan, có dependency được ghi nhận và có tiêu chí nghiệm thu cụ thể.

### 3.3. Definition of Done

Task được xem là Done khi code hoặc tài liệu đã hoàn thành, build/test tương ứng chạy thành công, không chứa secret, API contract được cập nhật nếu có thay đổi, PR đã được review và CI xanh. Với frontend phải kiểm tra trên trình duyệt; với service phải kiểm tra health endpoint; với database phải kiểm tra migration hoặc script chạy được.

### 3.4. GitHub Projects

Board dùng ba cột `Todo`, `In Progress`, `Done`. Mỗi issue có các field: `Title`, `Assignee`, `Milestone`, `Priority`, `Area`, `Status` và `Depends on`.

Giá trị đề xuất cho `Priority` là `P0 — bắt buộc demo`, `P1 — chức năng chính`, `P2 — hoàn thiện`. Giá trị cho `Area` là `architecture`, `backend`, `database`, `ai`, `frontend`, `devops`, `testing` hoặc `docs`.

## 4. Lộ trình tổng quan

| Tuần | Mục tiêu | Milestone | Kết quả bắt buộc |
|---|---|---|---|
| W1 | Chốt nền tảng và hợp đồng | Foundation | Architecture, ADR, ERD, API contract, repo rules |
| W2 | Dựng skeleton chạy được | Skeleton | 5 project .NET, 4 service, health check, CI |
| W3 | Xây Auth và người dùng | M3 Auth | Register, login, JWT, role, customer profile |
| W4 | Hoàn thiện quản lý xe | M4 Cars | Car CRUD, ảnh, filter cơ bản, phân quyền |
| W5 | Hoàn thiện booking | M5 Booking | Đặt xe, chống trùng lịch, confirm/cancel |
| W6 | Tích hợp giá AI | M6 AI Pricing | Predict price, timeout/retry/fallback, final price |
| W7 | Hoàn thiện vòng đời thuê | M7 Rental | Payment, nhận/trả xe, late fee, contract PDF |
| W8 | Chatbot và báo cáo | M8 Intelligence | Chat Gemini, search/filter, revenue report |
| W9 | Hardening và staging | M9 Release Candidate | Test tích hợp, security review, deploy staging |
| W10 | Nghiệm thu và bảo vệ | M10 Final | Demo end-to-end, slide, tài liệu, rehearsal |

# 5. Kế hoạch chi tiết theo tuần

## W1 — Chốt kiến trúc, dữ liệu và quy trình

### Mục tiêu

Thống nhất cách tổ chức code, các module nghiệp vụ, sáu bảng dữ liệu, API chính, cách tích hợp AI và quy trình làm việc Git trước khi viết nghiệp vụ.

### Công việc của TV A

- Viết `docs/architecture.md`, mô tả ba lớp: code architecture, system architecture và integration architecture.
- Viết `docs/decisions.md` với ADR về Clean Architecture + Modular Monolith, AI service độc lập, HTTP sync, React/SQL Server và Docker/CI.
- Viết `docs/api-contract.md` cho Auth, Cars, Bookings, Payments, Returns và AI.
- Tạo `docs/erd.dbml` và `docs/erd.sql` cho Customer, Car, CarImage, Booking, Payment và ReturnRecord.
- Thiết lập repository, branch protection cho `main`/`develop`, quy tắc PR và reviewer.
- Xác nhận phân công A/B/C/D/E và tạo các milestone ban đầu.

### Công việc phối hợp

- B kiểm tra API contract có đủ dữ liệu cho nghiệp vụ backend.
- C kiểm tra input/output của `/predict-price` và `/chat` có thể triển khai bằng FastAPI.
- D kiểm tra frontend có đủ thông tin để dựng các màn hình.
- E kiểm tra các port, biến môi trường và yêu cầu Docker.

### Deliverable

`architecture.md`, `decisions.md`, `api-contract.md`, `erd.dbml`, `erd.sql`, `README.md` và quy tắc branch/PR.

### Tiêu chí nghiệm thu

Cả nhóm có thể giải thích backend là Modular Monolith trong khung Clean Architecture, AI là process độc lập, API dùng HTTP sync và không dùng queue. ERD, API contract và phân công không mâu thuẫn nhau.

### Phụ thuộc

Không có. Đây là nền tảng của các tuần sau.

## W2 — Skeleton chạy được

### Mục tiêu

Dựng cấu trúc code và môi trường tối thiểu để mọi thành viên có thể bắt đầu làm độc lập trên đúng kiến trúc.

### Công việc của TV A

- Tạo solution `CarRental.slnx` gồm Domain, Application, Infrastructure, API và Tests.
- Tạo các folder module `Cars`, `Bookings`, `Customers` trong Domain và Application.
- Tạo `BaseEntity`, enum nghiệp vụ, entity khung và interface Application.
- Viết `PricingCalculator` theo công thức:
  `FinalPricePerDay = COALESCE(OverridePrice, PredictedPrice, BasePricePerDay)`.
- Chặn giá trong khoảng `400.000–1.500.000 VNĐ/ngày` và viết unit test.
- Cấu hình `/health`, CORS cho frontend và header `X-Correlation-Id`.
- Review, sửa và merge PR skeleton.

### Công việc của TV B

- Tạo `CarRentalDbContext` trong Infrastructure/Persistence.
- Khai báo DbSet cho sáu entity và các quan hệ chính.
- Cấu hình khóa ngoại, unique index cho Phone, Email, LicensePlate và quan hệ một-nhiều cần thiết.
- Tạo migration đầu tiên hoặc xác nhận migration khớp `docs/erd.sql`.
- Không đưa EF Core hoặc SQL Server reference vào Domain.

### Công việc của TV C

- Dựng FastAPI service trên port `5002`.
- Hoàn thiện `/health`, `/predict-price` và `/chat` ở dạng stub.
- Ghi rõ schema request/response và giá trị fallback trong code.
- Chuẩn bị cấu trúc thư mục cho dataset, model và test.

### Công việc của TV D

- Dựng React + Vite trên port `3000`.
- Tạo router và layout tối thiểu.
- Tạo trang health check gọi `.NET /api/health`.
- Chuẩn bị biến `VITE_API_BASE_URL`, không hard-code secret.

### Công việc của TV E

- Hoàn thiện `docker-compose.yml` cho db, api, ai và frontend.
- Viết hoặc kiểm tra Dockerfile của từng service.
- Tạo `.env.example` với DB, JWT, AI URL, timeout và retry count.
- Duy trì GitHub Actions restore, build và test.
- Kiểm tra `.env`, `appsettings.Development.json`, `bin/` và `obj/` không bị commit.

### Deliverable

Solution 5 project, entity khung, PricingCalculator có test, FastAPI stub, React skeleton, Docker Compose, `.env.example` và CI.

### Tiêu chí nghiệm thu

`docker compose up` khởi động được bốn service; API `/health` trả 200; AI `/health` trả 200; frontend mở được; SQL Server container healthy; `dotnet build` và `dotnet test` trên CI xanh.

### Phụ thuộc

W1 phải hoàn thành. W3–W5 phụ thuộc vào project reference, database context và contract của W2.

## W3 — Auth và quản lý người dùng

### Mục tiêu

Cho phép Customer đăng ký/đăng nhập và bảo vệ endpoint bằng JWT với role rõ ràng.

### Công việc của TV B

- Tạo request/response DTO cho register và login.
- Validate FullName, Phone, Email và password tối thiểu 8 ký tự.
- Hash password bằng BCrypt hoặc thư viện tương đương; không lưu plaintext.
- Kiểm tra trùng Phone, Email và IdentityNumber; trả error code đúng contract.
- Tạo JWT có `sub`, `email`, `role` và thời gian hết hạn.
- Cấu hình authentication/authorization trong API.
- Tạo endpoint profile cơ bản cho Customer và endpoint quản lý role cho Admin nếu cần.
- Viết test cho đăng ký hợp lệ, trùng email, password không hợp lệ, login sai và token hợp lệ.

### Công việc của TV D

- Tạo màn hình đăng ký và đăng nhập.
- Hiển thị lỗi validation và lỗi `PHONE_EXISTS`/`EMAIL_EXISTS`.
- Lưu token trong state hoặc cơ chế phù hợp của ứng dụng; không ghi secret vào source code.
- Tạo route guard cho màn hình yêu cầu đăng nhập.
- Hiển thị tên và role người dùng sau khi đăng nhập.

### Công việc của TV A

- Đối chiếu request/response thực tế với mục Auth trong `api-contract.md`.
- Review việc phân quyền: Public, Customer, Staff và Admin.
- Kiểm tra secret JWT lấy từ environment/configuration.

### Công việc của TV E

- Bổ sung biến JWT vào `.env.example` và compose.
- Kiểm tra API chạy được khi cấu hình secret từ environment.

### Deliverable

Auth API, JWT middleware, role authorization, login/register UI và unit/integration test cơ bản.

### Tiêu chí nghiệm thu

Người dùng đăng ký thành công, đăng nhập nhận JWT, gọi endpoint bảo vệ thành công với token đúng; token thiếu hoặc sai trả 401; role không đủ quyền trả 403; secret không xuất hiện trong git.

### Phụ thuộc

W2 hoàn thành; DbContext và Customer entity đã sẵn sàng.

## W4 — Quản lý xe

### Mục tiêu

Admin/Staff có thể quản lý danh mục xe; Customer có thể xem các xe hợp lệ để đặt.

### Công việc của TV B

- Implement `GET /api/cars` với phân trang và filter carType, minPrice, maxPrice, status.
- Implement `GET /api/cars/{id}` kèm danh sách ảnh.
- Implement POST/PUT/DELETE xe theo quyền.
- Validate LicensePlate, Seats, CarAgeYears và BasePricePerDay.
- Trả 409 `PLATE_EXISTS` khi biển số trùng.
- Chặn xóa xe có booking Active; soft delete theo BaseEntity.
- Implement upload, đặt ảnh primary, sắp xếp ảnh và xóa ảnh.
- Viết test cho CRUD, phân quyền, biển số trùng và xe không tồn tại.

### Công việc của TV D

- Tạo trang danh sách xe có filter và phân trang.
- Tạo trang chi tiết xe, hiển thị ảnh chính, thông số và giá niêm yết.
- Tạo form thêm/sửa xe cho Admin/Staff.
- Hiển thị trạng thái `san_sang`, `dang_thue`, `bao_tri`, `ngung_hoat_dong` theo contract.

### Công việc của TV A

- Kiểm tra enum và cách serialize status/carType.
- Review phân quyền Admin/Staff/Customer.
- Kiểm tra UI không tự quyết định xe còn trống thay cho backend.

### Công việc của TV E

- Kiểm tra volume hoặc cơ chế lưu ảnh trong môi trường local/staging.
- Bổ sung health/logging nếu upload ảnh ảnh hưởng container.

### Deliverable

Car API, CarImage API, database mapping, màn hình danh mục/chi tiết/form quản trị và test.

### Tiêu chí nghiệm thu

Admin tạo được xe và ảnh; Customer xem được xe; filter trả đúng dữ liệu; biển số trùng trả 409; xe không Available không được đưa vào luồng đặt.

### Phụ thuộc

W3 để test authorization; W2 để dùng DbContext và entity.

## W5 — Booking và chống trùng lịch

### Mục tiêu

Xây luồng đặt xe trung tâm và đảm bảo hai booking không chiếm cùng một xe trong khoảng thời gian giao nhau.

### Công việc của TV B

- Tạo `POST /api/bookings` cho Customer.
- Validate ngày bắt đầu không ở quá khứ và `startDate < endDate`.
- Chỉ cho đặt xe đang Available và chưa bị soft delete.
- Kiểm tra overlap theo khoảng nửa mở `[StartDate; EndDate)`.
- Chỉ xem các booking Pending, Confirmed và Active là đang chiếm lịch.
- Trả 409 `BOOKING_OVERLAP` hoặc `CAR_NOT_AVAILABLE` đúng trường hợp.
- Tính RentalDays, TotalPrice và lưu trạng thái Pending.
- Implement danh sách booking của Customer, danh sách cho Staff/Admin, confirm và cancel.
- Ghi lý do cancel; chuẩn bị cơ chế hết hạn Pending thành Expired theo thiết kế đã thống nhất.
- Viết test biên: kết thúc đúng lúc booking kế tiếp, giao nhau một phần, bao phủ toàn bộ, khác xe, booking Cancelled.

### Công việc của TV D

- Tạo form chọn ngày và xe.
- Hiển thị số ngày thuê, giá tạm tính và trạng thái xe.
- Hiển thị lỗi 400/409 từ API bằng thông báo dễ hiểu.
- Tạo trang booking của tôi và trạng thái Pending/Confirmed/Cancelled.

### Công việc của TV A

- Review thuật toán overlap và test case.
- Kiểm tra rule được enforce ở Application/backend, không chỉ ở frontend.
- Đối chiếu status transition với ERD và API contract.

### Công việc của TV E

- Chuẩn bị seed data tối thiểu để nhóm test booking trong Docker.

### Deliverable

Booking API, test chống trùng lịch, booking UI và flow Pending → Confirmed/Cancelled.

### Tiêu chí nghiệm thu

Hai request đồng thời hoặc hai request giao nhau cho cùng xe không cùng thành công; khoảng `[10;13)` và `[13;15)` không bị coi là overlap; Customer chỉ xem/hủy booking của mình; Staff/Admin confirm được booking.

### Phụ thuộc

W3 Auth và W4 Car CRUD.

## W6 — Dự đoán giá AI và fallback

### Mục tiêu

Tích hợp FastAPI vào luồng báo giá/đặt xe mà không làm mất khả năng đặt xe khi AI lỗi.

### Công việc của TV C

- Chuẩn bị dataset mô phỏng có các biến: CarType, Seats, CarAgeYears, RentalDays, BasePricePerDay, mùa/ngày cuối tuần và nhu cầu nếu dùng.
- Chọn XGBoost hoặc Random Forest phù hợp với môi trường triển khai.
- Tách bước train và inference; lưu model hoặc tạo cơ chế load model rõ ràng.
- Hoàn thiện `POST /predict-price` theo schema đã thống nhất.
- Trả `predicted_price_per_day`, confidence và model_used.
- Validate input âm, số chỗ không hợp lệ và base price không hợp lệ.
- Viết test cho response schema và trường hợp model chưa sẵn sàng.

### Công việc của TV B

- Implement `IAiPricingClient` bằng HttpClient trong Infrastructure.
- Cấu hình BaseUrl từ `AiService:BaseUrl`.
- Đặt timeout 2 giây và retry tối đa 2 lần cho lỗi phù hợp.
- Truyền `X-Correlation-Id` qua request tới AI.
- Khi AI lỗi hoặc timeout, trả BasePricePerDay và `isFallback: true`, không trả 5xx cho quote.
- Gọi pricing client từ booking/quote application flow.
- Lưu PredictedPricePerDay, FinalPricePerDay, IsFallback và CorrelationId.
- Dùng PricingCalculator để ưu tiên Override → Predicted → Base rồi clamp giá.
- Viết test success, timeout, AI 5xx, fallback và clamp.

### Công việc của TV D

- Hiển thị giá AI đề xuất và giá chốt.
- Hiển thị badge hoặc thông báo rõ khi `isFallback = true`.
- Không chặn thao tác đặt xe khi fallback trả về hợp lệ.

### Công việc của TV A

- Chốt schema .NET ↔ FastAPI trước khi merge.
- Kiểm tra đơn vị tiền là VNĐ/ngày và không nhầm float với số tiền lưu database.
- Review correlation ID và công thức giá.

### Công việc của TV E

- Kiểm tra biến `AI_TIMEOUT_SECONDS`, `AI_RETRY_COUNT`, `AiService__BaseUrl` trong Compose.
- Tạo kịch bản test tắt container AI.

### Deliverable

AI model/inference, .NET HttpClient, quote endpoint, fallback và UI giá AI.

### Tiêu chí nghiệm thu

AI hoạt động thì nhận được giá dự đoán; AI tắt hoặc quá 2 giây thì API vẫn trả 200 với giá niêm yết và `isFallback: true`; request có correlation ID; final price luôn trong khoảng 400.000–1.500.000 VNĐ/ngày.

### Phụ thuộc

W5 có booking flow; W2 có IAiPricingClient và PricingCalculator.

## W7 — Thanh toán, nhận/trả xe và hợp đồng

### Mục tiêu

Hoàn thiện vòng đời thuê từ booking đã xác nhận đến thanh toán, trả xe và hợp đồng PDF.

### Công việc của TV B

- Cho phép tạo nhiều Payment cho một Booking: cọc, phần còn lại và phí phát sinh.
- Implement `POST /api/bookings/{bookingId}/payments`.
- Implement Staff mark-paid, trạng thái Pending/Paid/Failed/Refunded theo contract.
- Chuyển booking Confirmed → Active khi nhận xe theo quyền Staff.
- Implement `POST /api/bookings/{bookingId}/return` cho Staff.
- Tính LateDays và `LateFee = LateDays * FinalPricePerDay` theo contract.
- Ghi DamageFee, DamageNote, StaffId và `FinalAmount = TotalPrice + LateFee + DamageFee`.
- Chuyển booking Active → Completed và Car → Available sau khi trả xe.
- Chặn trả xe khi booking chưa Active hoặc đã có ReturnRecord.
- Sinh hợp đồng PDF từ booking Confirmed trở đi.
- Viết test payment, transition, late fee, damage fee và PDF response content type.

### Công việc của TV D

- Tạo màn hình chi tiết booking và thanh toán.
- Hiển thị lịch sử các payment và trạng thái thanh toán.
- Tạo màn hình Staff ghi nhận nhận xe/trả xe.
- Hiển thị LateFee, DamageFee, FinalAmount.
- Thêm nút tải hợp đồng PDF.

### Công việc của TV A

- Kiểm tra quan hệ Payment: API contract hiện quy định một booking có nhiều payment; model/database phải thống nhất theo quy định này trước khi merge.
- Kiểm tra trạng thái Booking và Payment không bị trộn lẫn.
- Review công thức FinalAmount và quyền truy cập tài liệu hợp đồng.

### Công việc của TV E

- Kiểm tra thư viện PDF hoạt động trong Linux container.
- Bổ sung dependency native nếu thư viện PDF yêu cầu.

### Deliverable

Payment API, return API, state transitions, late fee, PDF contract và UI tương ứng.

### Tiêu chí nghiệm thu

Booking có thể đi qua Pending → Confirmed → Active → Completed; trả đúng hạn không phạt; trả trễ có LateFee; ReturnRecord chỉ tạo một lần; PDF tải được và chứa thông tin khách, xe, thời gian, giá chốt.

### Phụ thuộc

W5 Booking và W6 FinalPricePerDay.

## W8 — Chatbot, tìm kiếm và báo cáo

### Mục tiêu

Bổ sung các chức năng tạo khác biệt của đề tài và hỗ trợ vận hành.

### Công việc của TV C

- Hoàn thiện `POST /chat` với Gemini API.
- Thiết kế prompt có ngữ cảnh danh mục xe và thông tin booking phù hợp.
- Không gửi password, JWT secret hoặc dữ liệu nhạy cảm không cần thiết cho Gemini.
- Khi Gemini lỗi hoặc chưa cấu hình key, trả thông báo thân thiện theo contract thay vì làm frontend crash.
- Bảo toàn `sessionId`/correlation data theo schema thống nhất.
- Viết test mock cho Gemini success và failure.

### Công việc của TV B

- Implement tìm kiếm/filter xe theo giá, loại, số chỗ, nhiên liệu/hộp số nếu có trong model.
- Implement báo cáo doanh thu theo tháng và theo xe cho Admin.
- Tính tổng booking, doanh thu, tỷ lệ lấp đầy nếu đủ dữ liệu.
- Giới hạn endpoint báo cáo cho Admin/Staff theo thiết kế.
- Viết test query filter và số liệu báo cáo với dữ liệu mẫu.

### Công việc của TV D

- Tạo giao diện chatbot, trạng thái đang gửi và lỗi kết nối.
- Tạo trang tìm kiếm nâng cao.
- Tạo dashboard báo cáo doanh thu cho Admin.
- Format số tiền VNĐ và ngày tháng thống nhất.

### Công việc của TV A

- Kiểm tra chatbot không tự xác nhận booking nếu chưa có bước API booking chính thức.
- Đối chiếu báo cáo với dữ liệu Payment đã Paid, không cộng payment Pending.
- Review privacy boundary của dữ liệu gửi sang AI.

### Công việc của TV E

- Bổ sung `GEMINI_API_KEY` bằng environment/GitHub Secrets.
- Kiểm tra staging không lộ key trong log.

### Deliverable

Chatbot, search/filter, dashboard báo cáo và test dữ liệu.

### Tiêu chí nghiệm thu

Người dùng gửi câu hỏi và nhận được phản hồi; Gemini lỗi vẫn có phản hồi thân thiện; Admin xem được báo cáo với dữ liệu đúng; Customer không gọi được endpoint Admin; frontend không lộ API key.

### Phụ thuộc

W4 Car, W7 Payment/Booking và contract AI.

## W9 — Hardening, kiểm thử và staging

### Mục tiêu

Đưa hệ thống từ trạng thái feature-complete sang release candidate ổn định, an toàn và tái hiện được.

### Công việc của TV A

- Hoàn thiện Risk Register với owner, mức độ ảnh hưởng, xác suất và phương án giảm thiểu.
- Rà soát dependency graph Clean Architecture.
- Kiểm tra JWT, authorization, CORS, input validation và secret scanning.
- Kiểm tra API contract so với implementation thực tế.
- Chốt checklist DoD cho demo.
- Review toàn bộ PR tồn đọng và tạo issue cho lỗi còn lại.

### Công việc của TV B

- Bổ sung unit/integration test cho Auth, Car, Booking overlap, pricing, fallback, payment và return.
- Sửa lỗi concurrency hoặc transaction trong booking/payment.
- Kiểm tra migration mới nhất và seed data staging.
- Chuẩn hóa error response và status code.

### Công việc của TV C

- Đo thời gian inference và kiểm tra model loading khi container restart.
- Thêm validation và test cho `/predict-price`, `/chat`.
- Kiểm tra fallback khi Gemini key thiếu hoặc upstream lỗi.

### Công việc của TV D

- Kiểm tra responsive UI, loading state, error state và route guard.
- Sửa lỗi API base URL giữa local, Docker và staging.
- Kiểm tra không có token hoặc key hard-code trong bundle.

### Công việc của TV E

- Hoàn thiện Dockerfile production cho cả bốn service.
- Mở rộng CI: restore, build, test, kiểm tra format/lint nếu phù hợp.
- Cấu hình GitHub Secrets và biến môi trường staging.
- Deploy staging Railway/Render hoặc môi trường được nhóm thống nhất.
- Thêm health check và hướng dẫn rollback cơ bản.

### Deliverable

Release candidate, CI xanh, Docker production, staging URL, Risk Register, security checklist và test report.

### Tiêu chí nghiệm thu

Có thể dựng lại hệ thống từ README; CI xanh trên PR; staging chạy đủ frontend/API/AI/database hoặc database được cung cấp theo môi trường deploy; không có secret trong git; các luồng chính có test.

### Phụ thuộc

W3–W8 đã hoàn thành chức năng chính.

## W10 — Nghiệm thu, demo và bảo vệ

### Mục tiêu

Ổn định phiên bản cuối, hoàn tất tài liệu và chuẩn bị trả lời phản biện.

### Công việc của TV A

- Đóng băng scope và tạo release/tag cuối.
- Cập nhật README: kiến trúc, cách chạy local, Docker, API, tài khoản demo và xử lý lỗi thường gặp.
- Hoàn thiện slide kiến trúc, ERD, luồng nghiệp vụ, AI, DevOps và phân công.
- Chuẩn bị câu trả lời: vì sao Clean Architecture, vì sao Modular Monolith, vì sao AI tách process, vì sao HTTP sync không dùng queue.
- Kiểm tra toàn bộ issue P0/P1 trước demo.

### Công việc của TV B

- Chuẩn bị phần trình bày backend, JWT, transaction, chống trùng lịch, payment và PDF.
- Chạy rehearsal luồng API end-to-end.
- Sửa lỗi backend cuối cùng có thể ảnh hưởng demo.

### Công việc của TV C

- Chuẩn bị demo model, feature đầu vào, output dự đoán, confidence và fallback.
- Chuẩn bị giải thích giới hạn của dataset mô phỏng và hướng phát triển.
- Kiểm tra chatbot trong môi trường staging.

### Công việc của TV D

- Hoàn thiện giao diện demo, dữ liệu mẫu, thông báo lỗi và responsive layout.
- Chuẩn bị phần trình bày user journey từ đăng nhập đến hoàn tất thuê xe.

### Công việc của TV E

- Đảm bảo staging/container có thể khởi động lại.
- Chuẩn bị log, health check, URL demo và phương án xử lý khi một service tạm thời lỗi.

### Rehearsal bắt buộc

Lần 1 kiểm tra kỹ thuật: mỗi thành viên chạy phần mình và xử lý lỗi. Lần 2 chạy như buổi bảo vệ, giới hạn thời gian, phân vai người nói và người dự phòng.

### Luồng demo end-to-end

Đăng ký/đăng nhập → xem danh mục xe → lọc xe → xem giá AI → tạo booking → kiểm tra overlap → xác nhận booking → tạo payment cọc → nhận xe → trả xe → tính phí trễ/hư hỏng → thanh toán phần còn lại → tải hợp đồng PDF → Admin xem báo cáo.

### Tiêu chí nghiệm thu

Luồng demo chạy liên tục trên môi trường thống nhất; tài liệu và slide khớp code; mỗi thành viên giải thích được phần mình; có phương án demo fallback khi AI hoặc Gemini không hoạt động.

# 6. Backlog chi tiết cho GitHub Projects

Các issue dưới đây có thể nhập thủ công vào GitHub Projects. `P0` là bắt buộc để demo, `P1` là chức năng chính, `P2` là hoàn thiện. `Depends on` ghi mã task phải hoàn thành trước.

| ID | Issue title | Mô tả đầu ra | Assignee | Milestone | Area | Priority | Depends on |
|---|---|---|---|---|---|---|---|
| W1-01 | Chốt Clean Architecture và Modular Monolith | Architecture diagram, dependency rules, module boundaries | A | W1 Foundation | architecture | P0 | — |
| W1-02 | Viết ADR kiến trúc và tích hợp AI | ADR-001 đến ADR-005, nêu rõ phương án loại | A | W1 Foundation | architecture | P0 | W1-01 |
| W1-03 | Hoàn thiện ERD sáu bảng | DBML, SQL, FK, unique index và enum mapping | A | W1 Foundation | database | P0 | W1-01 |
| W1-04 | Chốt API contract | Request/response, status code, error code và quyền | A+B | W1 Foundation | architecture | P0 | W1-03 |
| W1-05 | Thiết lập branch protection và PR rules | Bảo vệ main/develop, bắt buộc review và CI | A+E | W1 Foundation | devops | P0 | — |
| W2-01 | Tạo solution Clean Architecture 5 project | Domain, Application, Infrastructure, API, Tests | A | W2 Skeleton | backend | P0 | W1-01 |
| W2-02 | Tạo entity và module folders | Sáu entity, Common, Cars, Bookings, Customers | A | W2 Skeleton | backend | P0 | W1-03 |
| W2-03 | Tạo PricingCalculator và unit test | Coalesce và clamp đúng rule | A | W2 Skeleton | testing | P0 | W1-04 |
| W2-04 | Tạo DbContext và migration đầu tiên | Mapping sáu bảng, index, relation | B | W2 Skeleton | database | P0 | W2-02 |
| W2-05 | Tạo FastAPI AI stub | `/health`, `/predict-price`, `/chat` | C | W2 Skeleton | ai | P0 | W1-04 |
| W2-06 | Tạo React/Vite skeleton | Router, layout, API health check | D | W2 Skeleton | frontend | P0 | W1-04 |
| W2-07 | Hoàn thiện Docker Compose và env example | Bốn service, port, network, volume và variables | E | W2 Skeleton | devops | P0 | W2-01,W2-05,W2-06 |
| W2-08 | Hoàn thiện CI build và test | Restore, build, test trên PR | E | W2 Skeleton | devops | P0 | W2-01,W2-03 |
| W3-01 | Implement Customer repository và persistence | Truy vấn Customer, unique Phone/Email/IdentityNumber | B | W3 Auth | backend | P0 | W2-04 |
| W3-02 | Implement register API | Validation, hash password, conflict errors | B | W3 Auth | backend | P0 | W3-01 |
| W3-03 | Implement login và JWT | Token, claims, expiry, role | B | W3 Auth | backend | P0 | W3-02 |
| W3-04 | Configure authorization | Public, Customer, Staff, Admin policies | B | W3 Auth | backend | P0 | W3-03 |
| W3-05 | Build login/register frontend | Forms, error state, route guard | D | W3 Auth | frontend | P0 | W3-03 |
| W3-06 | Add auth tests and contract review | 401, 403, invalid credentials, contract check | A+B | W3 Auth | testing | P0 | W3-04,W3-05 |
| W4-01 | Implement Car query APIs | List, detail, filter, pagination | B | W4 Cars | backend | P0 | W2-04 |
| W4-02 | Implement Car CRUD | Create, update, soft delete, validation | B | W4 Cars | backend | P0 | W4-01,W3-04 |
| W4-03 | Implement CarImage APIs | Upload, primary image, sort, delete | B | W4 Cars | backend | P1 | W4-02 |
| W4-04 | Build catalog and detail pages | List, filter, detail, image gallery | D | W4 Cars | frontend | P0 | W4-01 |
| W4-05 | Build Admin car management UI | Create/edit/status/image controls | D | W4 Cars | frontend | P1 | W4-02,W4-03 |
| W4-06 | Test car permissions and conflicts | 409 plate, 403 role, unavailable car | A+B | W4 Cars | testing | P0 | W4-02 |
| W5-01 | Implement booking creation | Date validation, status Pending, total estimate | B | W5 Booking | backend | P0 | W4-01,W3-04 |
| W5-02 | Implement overlap rule | Half-open interval and active statuses | B | W5 Booking | backend | P0 | W5-01 |
| W5-03 | Implement booking list/confirm/cancel | Mine, staff list, confirm, cancel reason | B | W5 Booking | backend | P0 | W5-01,W5-02 |
| W5-04 | Build booking form and history | Date picker, estimate, status and errors | D | W5 Booking | frontend | P0 | W5-01 |
| W5-05 | Add overlap and transition tests | Boundary dates, conflict, cancellation, permissions | A+B | W5 Booking | testing | P0 | W5-02,W5-03 |
| W6-01 | Create AI pricing dataset | Reproducible simulated dataset and README | C | W6 AI Pricing | ai | P0 | W2-05 |
| W6-02 | Train and load pricing model | XGBoost or RF, inference pipeline | C | W6 AI Pricing | ai | P0 | W6-01 |
| W6-03 | Implement `/predict-price` real response | Schema, validation, confidence, model_used | C | W6 AI Pricing | ai | P0 | W6-02 |
| W6-04 | Implement .NET AI HttpClient | Base URL, timeout 2s, retry 2, correlation ID | B | W6 AI Pricing | backend | P0 | W2-05,W5-01 |
| W6-05 | Implement pricing fallback | Base price and `isFallback=true` on AI failure | B | W6 AI Pricing | backend | P0 | W6-04 |
| W6-06 | Connect pricing to booking and quote | Save predicted/final/fallback/correlation fields | B | W6 AI Pricing | backend | P0 | W6-05,W5-01 |
| W6-07 | Build AI price display | Suggested price, final price, fallback badge | D | W6 AI Pricing | frontend | P0 | W6-06 |
| W6-08 | Test pricing integration | Success, timeout, 5xx, retry, fallback, clamp | A+B+C | W6 AI Pricing | testing | P0 | W6-03,W6-06 |
| W7-01 | Implement multi-payment flow | Deposit, balance, mark-paid and history | B | W7 Rental | backend | P0 | W5-03 |
| W7-02 | Implement receive-car transition | Confirmed → Active and permissions | B | W7 Rental | backend | P0 | W7-01 |
| W7-03 | Implement return and late fee | ReturnRecord, LateDays, LateFee, DamageFee | B | W7 Rental | backend | P0 | W7-02 |
| W7-04 | Implement contract PDF | PDF for Confirmed or later booking | B | W7 Rental | backend | P0 | W5-03 |
| W7-05 | Build payment/return UI | Payment, receive, return, fees, PDF download | D | W7 Rental | frontend | P0 | W7-01,W7-03,W7-04 |
| W7-06 | Test rental lifecycle | State transitions, fee formula, duplicate return | A+B | W7 Rental | testing | P0 | W7-03,W7-04 |
| W8-01 | Implement Gemini chat integration | Context, session, friendly upstream fallback | C | W8 Intelligence | ai | P1 | W2-05 |
| W8-02 | Implement search and filters | Query by type, price, seats and status | B | W8 Intelligence | backend | P1 | W4-01 |
| W8-03 | Implement revenue report | Monthly, by car, paid revenue and occupancy | B | W8 Intelligence | backend | P1 | W7-01 |
| W8-04 | Build chatbot UI | Message list, loading and error states | D | W8 Intelligence | frontend | P1 | W8-01 |
| W8-05 | Build report dashboard | Summary cards, table/chart and filters | D | W8 Intelligence | frontend | P1 | W8-03 |
| W8-06 | Review privacy and report correctness | No secret/over-sharing; exclude Pending revenue | A | W8 Intelligence | testing | P0 | W8-01,W8-03 |
| W9-01 | Add backend integration test suite | Auth, cars, booking, pricing, payment and return | B | W9 Release | testing | P0 | W3-06,W5-05,W7-06 |
| W9-02 | Add AI service tests and model checks | Schema, invalid input, model reload, upstream failure | C | W9 Release | testing | P1 | W6-08,W8-01 |
| W9-03 | Frontend release hardening | Responsive, loading, errors, route/API config | D | W9 Release | frontend | P0 | W8-04,W8-05 |
| W9-04 | Production Dockerfiles | Small/reproducible images and health checks | E | W9 Release | devops | P0 | W7-04,W8-01 |
| W9-05 | CI quality and secret checks | Build, test, lint/format where applicable, secret scan | E | W9 Release | devops | P0 | W9-01,W9-04 |
| W9-06 | Deploy and verify staging | Four service startup, smoke test and rollback note | E | W9 Release | devops | P0 | W9-05 |
| W9-07 | Complete Risk Register and security review | Risks, owners, mitigations, JWT/CORS/secret review | A | W9 Release | docs | P0 | W9-01,W9-05 |
| W10-01 | Freeze scope and tag release | Close P0 issues, tag final candidate/release | A+E | W10 Final | docs | P0 | W9-06,W9-07 |
| W10-02 | Update README and demo guide | Setup, env, accounts, URLs, troubleshooting | A | W10 Final | docs | P0 | W10-01 |
| W10-03 | Prepare architecture and AI slides | ERD, Clean Arch, Modular Monolith, AI, DevOps | A+C+E | W10 Final | docs | P0 | W10-01 |
| W10-04 | Prepare frontend/backend demo script | User journey and expected responses | B+D | W10 Final | docs | P0 | W10-01 |
| W10-05 | Run technical rehearsal | Full flow, failure cases, timing and backup plan | Cả nhóm | W10 Final | testing | P0 | W10-02,W10-03,W10-04 |
| W10-06 | Run final end-to-end acceptance | Login → car → AI → booking → payment → return → PDF → report | Cả nhóm | W10 Final | testing | P0 | W10-05 |

# 7. Checklist nghiệm thu cuối

## Kiến trúc

- [ ] Domain không tham chiếu Application, Infrastructure, API hoặc EF Core.
- [ ] Application chỉ phụ thuộc Domain và chứa rule/use case contract.
- [ ] Infrastructure implement các adapter như database, HttpClient và PDF.
- [ ] API là composition root, không chứa business rule lớn.
- [ ] Cars, Bookings và Customers được tổ chức thành module trong backend monolith.
- [ ] AI là service FastAPI độc lập, không biến backend thành microservices toàn phần.

## Backend và dữ liệu

- [ ] Sáu bảng khớp ERD và migration.
- [ ] Phone, Email và LicensePlate có unique constraint phù hợp.
- [ ] Booking overlap được kiểm tra ở backend/Application.
- [ ] JWT và role authorization hoạt động.
- [ ] Payment Pending không được tính vào doanh thu đã thu.
- [ ] ReturnRecord không tạo trùng cho một booking.
- [ ] Công thức FinalAmount đúng với contract.

## AI và tích hợp

- [ ] `/predict-price` khớp schema đã chốt.
- [ ] HttpClient timeout 2 giây và retry tối đa 2 lần.
- [ ] `X-Correlation-Id` được truyền qua các service.
- [ ] AI lỗi/timeout không làm hỏng việc đặt xe.
- [ ] Fallback trả BasePricePerDay và `isFallback: true`.
- [ ] FinalPricePerDay được coalesce và clamp đúng giới hạn.
- [ ] Gemini API key chỉ lấy từ environment/secret.

## DevOps và bảo mật

- [ ] Docker Compose khởi động đúng các service.
- [ ] Health check của API và AI trả 200.
- [ ] CI restore/build/test xanh trên Pull Request.
- [ ] `.env`, JWT secret, Gemini key và password DB không nằm trong git.
- [ ] CORS chỉ cho các origin cần thiết.
- [ ] README cho phép thành viên mới dựng được môi trường.
- [ ] Staging có smoke test và phương án xử lý khi AI không sẵn sàng.

## Demo

- [ ] Có tài khoản demo Customer và Admin/Staff.
- [ ] Có dữ liệu mẫu xe, booking và payment.
- [ ] Có kịch bản AI hoạt động.
- [ ] Có kịch bản AI tắt để trình bày fallback.
- [ ] Có ảnh chụp hoặc log chứng minh CI xanh.
- [ ] Mỗi thành viên biết phần trình bày và câu hỏi phản biện của mình.
