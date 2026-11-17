# Hướng dẫn Jira — Car Rental AI (bản làm lại, Epic theo chức năng)

> Backlog chính, Epic theo chức năng. Cột hiện trạng được đối chiếu thêm với code local ngày 26/10/2026 (gồm thay đổi chưa commit). Commit/PR cũ là căn cứ lịch sử, không xác nhận Jira/GitHub hiện tại. Hai hướng dẫn Jira cũ đã bỏ để tránh trùng key/lịch; workflow xem [sprint-backlog-workflow.md](sprint-backlog-workflow.md), contract hiện tại xem [api-contract.md](api-contract.md).
> Trạng thái ghi ở đây là **đề xuất khi nhập Jira**, không phải xác nhận trực tiếp từ Jira. Chỉ để Done khi đạt tiêu chí nghiệm thu và có bằng chứng (commit/PR/log/ảnh).

## 1. Thiết lập

| Mục | Giá trị |
|---|---|
| Project | Car Rental AI, key `CRAI`, template Scrum (team-managed) |
| Loại thẻ được dùng | **Epic** (nhóm chức năng), **Story**, **Task**, **Bug**. Không dùng Subtask, không tạo loại thẻ khác |
| Board | To Do → In Progress → In Review → Done |
| Epic | Mỗi Epic là **một nhóm chức năng** của hệ thống, không gắn với tuần/Sprint. Một thẻ chỉ thuộc một Epic |
| Sprint | Chỉ dùng để xếp lịch làm việc, độc lập với Epic (một Epic có thể trải qua nhiều Sprint) |

**Khi nào dùng loại nào**

| Loại | Dùng cho | Ví dụ |
|---|---|---|
| Story | Chức năng người dùng thấy/dùng được | Khách lọc xe theo loại và giá |
| Task | Việc kỹ thuật/tài liệu/hạ tầng, không phải chức năng riêng | Dựng Docker Compose |
| Bug | Lỗi đã xảy ra so với hành vi mong đợi | Port MinIO bị chiếm, API crash vì migration lệch |

Việc nhỏ trong một thẻ thì ghi thành checklist trong Description, không tạo Subtask.

**Mẫu Description cho thẻ**

```text
Mục tiêu: [một câu]
Phạm vi: [làm gì / không làm gì]
Tiêu chí nghiệm thu: [điều kiện kiểm tra được]
Phối hợp/Review: [người]
```

**Mẫu comment khi cập nhật**

```text
Đã làm: [việc cụ thể]
Kết quả kiểm tra: [case đã chạy, đạt/chưa đạt]
Còn lại: [việc chưa xong]
Bằng chứng: [commit/PR/log/ảnh thật]
```

### Thành viên

| Thành viên | Vai trò | Phụ trách |
|---|---|---|
| Dũng | Nhóm trưởng | Kiến trúc, contract, review PR, tích hợp, nghiệm thu; làm thay Tuấn/Việt Anh/Năng/Tiến khi cần |
| Tuấn | Backend | Auth, Car, Booking, Payment, nhận/trả xe |
| Việt Anh | Frontend | Toàn bộ giao diện React |
| Năng | AI | Dataset, model, FastAPI, chatbot |
| Tiến | DevOps | Docker, CI/CD, triển khai |

Ghi chú "(Dũng làm thay)" nghĩa là Dũng thực hiện hộ; Assignee vẫn là người phụ trách theo vai trò. Lịch sử Git hiện chỉ có commit của Dũng, Tuấn (`tuanda`/`DoTuan3725`) và Việt Anh (qua PR #11); Năng chủ yếu merge PR, Tiến chưa có commit.

## 2. Epic (theo chức năng)

| Key | Epic | Sprint | Phạm vi |
|---|---|---|---|
| CRAI-1 | Nền tảng & Tài liệu | Sprint 01, 02, 05 | Kiến trúc, ERD, API contract, cấu trúc solution, kế hoạch, README, báo cáo, slide, nghiệm thu |
| CRAI-2 | Xác thực & Tài khoản | Sprint 01, 02, 05 | Đăng ký, đăng nhập JWT, phân quyền, hồ sơ, khôi phục mật khẩu |
| CRAI-3 | Xe & Ảnh xe | Sprint 01, 02, 04 | CRUD xe, danh mục, chi tiết, lọc, phân trang, giao diện quản lý, MinIO, upload WebP, ảnh đại diện |
| CRAI-4 | Đặt xe | Sprint 03, 05 | Tạo đơn, chống trùng lịch, xác nhận/hủy, lịch sử, giao diện đặt xe |
| CRAI-5 | Định giá AI | Sprint 01, 02, 03, 04, 05 | FastAPI, model giá, AiPricingClient, fallback, quy tắc giá |
| CRAI-6 | Thanh toán & Nhận/Trả xe | Sprint 04, 05 | Cọc/phần còn lại, nhiều payment/booking, nhận/trả xe, phí trễ/hư hỏng, hợp đồng PDF |
| CRAI-7 | Chatbot & Báo cáo | Sprint 04, 05 | Chatbot Gemini tư vấn xe, báo cáo doanh thu/vận hành |
| CRAI-8 | DevOps & Triển khai | Sprint 01, 02, 05 | Docker Compose, CI/CD, cấu hình, môi trường, staging |

Khi tạo thẻ trên Jira, Jira tự cấp key theo thứ tự tạo; các key CRAI-9…66 bên dưới (bỏ CRAI-14 vì đã xóa nhầm trên Jira) chỉ là mã tham chiếu trong tài liệu này để ghi quan hệ giữa các thẻ.

## 3. Sprint (chỉ để xếp lịch)

| Sprint | Thời gian | Mục tiêu |
|---|---|---|
| Sprint 01 | 28/09 – 17/10/2026 | Nền tảng, Auth và Quản lý xe chạy được |
| Sprint 02 | 18/10 – 31/10/2026 | Ảnh xe, lọc/quản lý xe, hoàn thiện tích hợp FE–BE |
| Sprint 03 | 01/11 – 14/11/2026 | Đặt xe và AI giá có fallback |
| Sprint 04 | 15/11 – 28/11/2026 | Thanh toán, nhận/trả xe, chatbot, model giá |
| Sprint 05 | 29/11 – 12/12/2026 | Hoàn thiện chatbot/báo cáo/PDF, kiểm thử, triển khai, nghiệm thu |

Khối lượng được rải đều khoảng 30–43 Story Point mỗi Sprint: việc đã làm xong theo Git được chia vào Sprint 01–02, việc chưa làm được xếp tiếp vào Sprint 03–05.

## 4. Danh sách thẻ (theo Sprint)

Mỗi bảng là một Sprint; cột **Epic** cho biết thẻ thuộc nhóm chức năng nào. Cột **TT** là trạng thái đề xuất: `Done` (đã merge/đã chạy kiểm tra), `Review` (có code, cần kiểm thử/duyệt), `To Do` (chưa làm). Story Point là ước lượng ban đầu. **Người tạo** là người tạo thẻ trên Jira (Reporter): thẻ Task/Story của Tuấn và Việt Anh do chính họ tạo; thẻ Bug, thẻ Dũng làm thay và thẻ của Năng/Tiến (chưa có hoạt động trên Git) do Dũng tạo. **Người duyệt** là người kéo thẻ từ In Review sang Done (không phải người làm): theo PR đã duyệt trên GitHub nếu có, còn lại đề xuất theo quy tắc — thẻ backend/tài liệu do Dũng làm → Tuấn; thẻ frontend Dũng làm thay → Việt Anh; thẻ AI/DevOps Dũng làm thay → Năng; thẻ của Tuấn/Việt Anh → Dũng. **Priority** theo thang Jira: Highest, High, Medium, Low (Highest = chặn các thẻ khác hoặc lỗi nghiêm trọng).

### CRAI Sprint 1 (28/09 – 17/10/2026)

**Goal:** Nền tảng, Auth và Quản lý xe chạy được. **Số thẻ:** 17, tổng 43 Story Point.

| Key | Loại | Nội dung | Assignee | Người tạo | Người duyệt | Epic | Story Point | Priority | Ngày xong dự kiến | TT | Căn cứ Git |
|---|---|---|---|---|---|---|---|---|---|---|---|
| CRAI-9 | Task | Chốt quyết định kiến trúc (Clean Architecture + Modular Monolith, Client-Server, Request-Response) | Dũng | Dũng | Tuấn | CRAI-1 Nền tảng & Tài liệu | 2 | High | 28/09/2026 | Done | `a3320d8`, `0c8ed0b`, `3b19a0f`, PR #3 |
| CRAI-10 | Task | Viết API contract REST cho các module và endpoint AI | Dũng | Dũng | Tuấn | CRAI-1 Nền tảng & Tài liệu | 3 | Medium | 29/09/2026 | Review | Contract đã đối chiếu code local: route, status, DTO, enum, JSON và FastAPI stub; cần nghiệm thu API chạy thật |
| CRAI-11 | Task | Thiết kế ERD 6 bảng | Dũng | Dũng | Tuấn | CRAI-1 Nền tảng & Tài liệu | 2 | High | 29/09/2026 | Done | `9f1c742` (`docs/erd.dbml`) |
| CRAI-12 | Task | Dựng solution .NET 10 Clean Architecture 5 project, entity và khung layer | Dũng | Dũng | Tuấn | CRAI-1 Nền tảng & Tài liệu | 3 | Highest | 30/09/2026 | Done | `0f54986`, `9f1c742`, PR #7 |
| CRAI-13 | Task | Lập kế hoạch 10 tuần và backlog Jira | Dũng | Dũng | Năng | CRAI-1 Nền tảng & Tài liệu | 2 | Medium | 30/09/2026 | Done | `31bc07a`, PR #9, `588bb24` |
| CRAI-15 | Story | Đăng ký và đăng nhập, JWT, mật khẩu BCrypt | Tuấn | Tuấn | Dũng | CRAI-2 Xác thực & Tài khoản | 5 | Highest | 13/10/2026 | Done | `3b2c1f1`, `6277714`, `dd90005`, PR #15; đã chạy register/login qua Docker |
| CRAI-16 | Story | Phân quyền Customer/Staff/Admin theo role claim | Tuấn | Tuấn | Dũng | CRAI-2 Xác thực & Tài khoản | 3 | Highest | 14/10/2026 | Done | `842d50e`, `7b64ffb`; Customer confirm booking trả 403, Admin 200, không token 401 |
| CRAI-17 | Bug | JWT sai role claim và payload auth FE/BE lệch tên trường | Dũng | Dũng | Tuấn | CRAI-2 Xác thực & Tài khoản | 2 | High | 14/10/2026 | Done | `7b64ffb`, `4db0d6d`, `b9621bc` (Phone → PhoneNumber) |
| CRAI-18 | Story | CRUD xe, kiểm tra biển số trùng, chặn xóa xe đang thuê | Tuấn | Tuấn | Dũng | CRAI-3 Xe & Ảnh xe | 5 | High | 15/10/2026 | Review | `af98880`, `28d6abc`, PR #15; cần rà validation khi update |
| CRAI-19 | Task | Persistence Customer/Car, unique index Email/Phone/biển số, migration | Tuấn | Tuấn | Dũng | CRAI-3 Xe & Ảnh xe | 3 | Highest | 15/10/2026 | Done | `2cc8fc5`, `e417feb`, `00c775e`; migration chạy được trên DB trống |
| CRAI-20 | Bug | Trùng `ICarRepository` làm build lỗi, cấu hình DI MinIO | Dũng | Dũng | Năng | CRAI-3 Xe & Ảnh xe | 1 | Highest | 16/10/2026 | Done | `674cac4`, PR #16 |
| CRAI-21 | Bug | Response JSON PascalCase làm UI hiển thị rỗng | Dũng | Dũng | Tuấn | CRAI-3 Xe & Ảnh xe | 2 | High | 16/10/2026 | Done | `8d73adc` |
| CRAI-22 | Bug | Cập nhật xe sai và mapping quan hệ Payment | Dũng | Dũng | Tuấn | CRAI-3 Xe & Ảnh xe | 2 | Medium | 16/10/2026 | Done | `479e973` |
| CRAI-23 | Task | FastAPI khung: `/health`, `/predict-price`, `/chat` (stub) | Năng | Dũng | Dũng | CRAI-5 Định giá AI | 2 | Medium | 17/10/2026 | Done | `9f1c742`; hiện trả giá gốc, `model_used=stub_fallback` |
| CRAI-24 | Task | Docker Compose full stack (SQL Server, MinIO, API, AI, frontend) | Tiến (Dũng làm thay) | Dũng | Năng | CRAI-8 DevOps & Triển khai | 3 | High | 17/10/2026 | Done | `9f1c742`; chạy đủ 5 service healthy |
| CRAI-25 | Bug | CI lỗi VSTest `MSB4181` và test `Theory` | Dũng | Dũng | Tuấn | CRAI-8 DevOps & Triển khai | 1 | Medium | 17/10/2026 | Done | `c6965fc`, `c2fb0df` |
| CRAI-26 | Task | Quản lý cấu hình và bí mật (`.env`, bỏ `appsettings.Development.json` khỏi Git, ẩn khóa ngoài Development) | Dũng | Dũng | Tuấn | CRAI-8 DevOps & Triển khai | 2 | Medium | 17/10/2026 | Done | `b4afa40`, `30fe86d`, `2437e37` |

### CRAI Sprint 2 (18/10 – 31/10/2026)

**Goal:** Ảnh xe, lọc/quản lý xe, hoàn thiện tích hợp FE–BE. **Số thẻ:** 14, tổng 43 Story Point.

| Key | Loại | Nội dung | Assignee | Người tạo | Người duyệt | Epic | Story Point | Priority | Ngày xong dự kiến | TT | Căn cứ Git |
|---|---|---|---|---|---|---|---|---|---|---|---|
| CRAI-27 | Task | Báo cáo tiến độ hằng tuần | Dũng | Dũng | Tuấn | CRAI-1 Nền tảng & Tài liệu | 1 | Low | 18/10/2026 | Review | File `.docx` trong `docs/`, chưa commit |
| CRAI-28 | Story | Giao diện đăng ký, đăng nhập, đăng xuất, giữ phiên | Việt Anh | Việt Anh | Dũng | CRAI-2 Xác thực & Tài khoản | 5 | High | 19/10/2026 | Review | `7be7dd4` (PR #11), `4db0d6d`; cần chạy lại trọn luồng trên trình duyệt |
| CRAI-29 | Story | Danh mục và trang chi tiết xe | Việt Anh | Việt Anh | Dũng | CRAI-3 Xe & Ảnh xe | 5 | High | 20/10/2026 | Done | `7be7dd4` (PR #11); đã xem trên trình duyệt |
| CRAI-30 | Task | Nền tảng MinIO (bucket, storage service) và Booking–Payment 1-N | Dũng | Dũng | Năng | CRAI-3 Xe & Ảnh xe | 3 | High | 21/10/2026 | Done | `7dedd4c`, PR #14 |
| CRAI-31 | Task | Quy tắc giá `Override ?? Predicted ?? Base`, giới hạn 400.000–1.500.000đ, có test | Dũng | Dũng | Tuấn | CRAI-5 Định giá AI | 2 | High | 21/10/2026 | Done | `PricingCalculator` và test trong `tests/` |
| CRAI-32 | Task | CI GitHub Actions: build và test trên PR | Tiến | Dũng | Dũng | CRAI-8 DevOps & Triển khai | 3 | Medium | 22/10/2026 | Review | `.github/workflows/ci.yml`; thiếu log pipeline làm bằng chứng |
| CRAI-33 | Task | Viết README theo hiện trạng | Dũng | Dũng | Tuấn | CRAI-1 Nền tảng & Tài liệu | 1 | Medium | 22/10/2026 | Done | `45b3c40`, `95b9f4b` |
| CRAI-34 | Story | Cập nhật hồ sơ người dùng (`PUT /api/auth/me`) | Tuấn | Tuấn | Dũng | CRAI-2 Xác thực & Tài khoản | 2 | Medium | 25/10/2026 | Review | `867493e`; chưa có test riêng |
| CRAI-35 | Story | Lọc xe (loại, giá, trạng thái) và phân trang đồng bộ UI–API | Việt Anh (Dũng làm thay) | Dũng | Việt Anh | CRAI-3 Xe & Ảnh xe | 5 | High | 26/10/2026 | Review | `d1abb80`; lọc Sedan/SUV đúng trên API và trình duyệt, có ảnh bằng chứng |
| CRAI-36 | Story | Giao diện quản lý xe cho Admin/Staff (danh sách, form thêm/sửa, xóa) | Việt Anh | Việt Anh | Dũng | CRAI-3 Xe & Ảnh xe | 5 | High | 27/10/2026 | Review | Có `AdminCarsPage.jsx` và route `/admin/cars` trong code local; cần kiểm thử và review |
| CRAI-37 | Bug | Thẻ xe hiện số `1` thay nhãn loại xe/trạng thái và thiếu style | Việt Anh (Dũng làm thay) | Dũng | Việt Anh | CRAI-3 Xe & Ảnh xe | 2 | Medium | 27/10/2026 | Review | Sửa `carLabels.js`, `CarCard`, `CarDetailPage`, `BookingPage`, `App.css`; đã xem trên trình duyệt, **chưa commit** |
| CRAI-38 | Story | Upload ảnh WebP (kiểm tra đuôi/MIME/header, ≤ 5 MB) và chọn ảnh đại diện | Tuấn | Tuấn | Dũng | CRAI-3 Xe & Ảnh xe | 5 | High | 29/10/2026 | Review | `e1ad234`, `674cac4`; cần kiểm tra URL ảnh trên trình duyệt |
| CRAI-39 | Story | Giao diện quản lý ảnh xe (gallery, upload, đặt ảnh chính) | Việt Anh | Việt Anh | Dũng | CRAI-3 Xe & Ảnh xe | 3 | Medium | 29/10/2026 | Review | `AdminCarImagesPage` trong PR #11; chưa nghiệm thu với API thật |
| CRAI-40 | Bug | Port MinIO 9000/9001 bị chiếm, `docker compose up` xung đột | Dũng | Dũng | Tuấn | CRAI-8 DevOps & Triển khai | 1 | Medium | 29/10/2026 | Done | `7afdab6` (`MINIO_HOST_PORT`, `MINIO_CONSOLE_PORT`) |

### CRAI Sprint 3 (01/11 – 14/11/2026)

**Goal:** Đặt xe và AI giá có fallback. **Số thẻ:** 9, tổng 39 Story Point.

| Key | Loại | Nội dung | Assignee | Người tạo | Người duyệt | Epic | Story Point | Priority | Ngày xong dự kiến | TT | Căn cứ Git |
|---|---|---|---|---|---|---|---|---|---|---|---|
| CRAI-41 | Story | Đặt xe theo khoảng ngày, chống trùng lịch nửa mở `[Start, End)` | Tuấn (Dũng làm thay) | Dũng | Tuấn | CRAI-4 Đặt xe | 8 | Highest | 03/11/2026 | Review | `7dd96c6`; tạo 201, trùng lịch 409 `BOOKING_OVERLAP`, hai khoảng sát nhau được chấp nhận |
| CRAI-42 | Story | Xem, xác nhận, hủy booking và phân quyền theo vai trò | Tuấn (Dũng làm thay) | Dũng | Tuấn | CRAI-4 Đặt xe | 5 | Highest | 04/11/2026 | Review | `7dd96c6`; Customer confirm 403, Admin confirm 200 |
| CRAI-43 | Story | Giao diện đặt xe, lịch sử của tôi, quản lý đơn (Admin/Staff) | Việt Anh | Việt Anh | Dũng | CRAI-4 Đặt xe | 8 | High | 08/11/2026 | Review | `BookingPage`, `MyBookingsPage`, `AdminBookingsPage`; luồng đặt đã chạy qua API, chưa nghiệm thu UI đầy đủ |
| CRAI-44 | Task | Kiểm chứng Booking 1-N Payment và migration | Tuấn | Tuấn | Dũng | CRAI-4 Đặt xe | 3 | High | 09/11/2026 | Review | Có migration index không unique và `PaymentBookingRelationTests`; cần kết quả chạy trên DB mục tiêu |
| CRAI-45 | Task | Chống đặt trùng khi hai request đồng thời cùng xe | Tuấn | Tuấn | Dũng | CRAI-4 Đặt xe | 5 | High | 10/11/2026 | Review | Create đã khóa dòng xe `UPDLOCK, ROWLOCK` và check/save cùng transaction; có integration test, cần bằng chứng chạy |
| CRAI-46 | Bug | Hủy được đơn đang thuê (`DangThue`) | Tuấn | Dũng | Dũng | CRAI-4 Đặt xe | 2 | High | 10/11/2026 | Review | `CancelAsync` đã chặn `DangThue`, `DaTraXe`, `DaHuy`; cần nghiệm thu bản sửa local |
| CRAI-47 | Bug | API crash `PendingModelChangesWarning`: snapshot lệch tên bảng | Dũng | Dũng | Tuấn | CRAI-4 Đặt xe | 3 | Highest | 11/11/2026 | Done | `c80171f` (migration `AlignTableNames`, tự migrate ở Development) |
| CRAI-48 | Task | AiPricingClient: timeout 2s, retry 2, fallback `IsFallback` | Dũng | Dũng | Tuấn | CRAI-5 Định giá AI | 3 | High | 12/11/2026 | Review | Có client và `AiPricingClientRetryTests`; timeout 2s mỗi lần, tối đa 3 lần gọi; cần kiểm tra tắt container AI |
| CRAI-49 | Bug | Đồng bộ schema và feature client .NET với FastAPI | Dũng | Dũng | Tuấn | CRAI-5 Định giá AI | 2 | High | 12/11/2026 | Review | `car_type` số và `predicted_price_per_day` đã khớp; FastAPI còn bỏ qua `rental_days`, `start_date`; stub 200 chưa được nhận diện là fallback |

### CRAI Sprint 4 (15/11 – 28/11/2026)

**Goal:** Thanh toán, nhận/trả xe, chatbot, model giá. **Số thẻ:** 8, tổng 36 Story Point.

| Key | Loại | Nội dung | Assignee | Người tạo | Người duyệt | Epic | Story Point | Priority | Ngày xong dự kiến | TT | Căn cứ Git |
|---|---|---|---|---|---|---|---|---|---|---|---|
| CRAI-50 | Story | Xóa ảnh xe | Tuấn | Tuấn | Dũng | CRAI-3 Xe & Ảnh xe | 2 | Low | 15/11/2026 | Review | Có DELETE endpoint, xóa object MinIO, chuyển ảnh chính và integration test trong code local |
| CRAI-51 | Story | Dataset, huấn luyện và phục vụ model dự đoán giá (RF/XGBoost) | Năng (Dũng làm thay) | Dũng | Năng | CRAI-5 Định giá AI | 8 | High | 17/11/2026 | To Do | Chưa có model thật; cần ghi MAE/RMSE thực khi làm |
| CRAI-52 | Story | API thanh toán cọc/phần còn lại và lịch sử thanh toán | Tuấn | Tuấn | Dũng | CRAI-6 Thanh toán & Nhận/Trả xe | 5 | High | 19/11/2026 | Review | Code local: `RentalController`/`RentalService`, 3 endpoint payment; chưa commit, chưa có test tích hợp DB |
| CRAI-53 | Story | Giao diện thanh toán và lịch sử thanh toán | Việt Anh | Việt Anh | Dũng | CRAI-6 Thanh toán & Nhận/Trả xe | 3 | Medium | 19/11/2026 | Review | `PaymentsModal.jsx` dùng ở MyBookings và AdminBookings; esbuild parse OK, chưa chạy trên trình duyệt |
| CRAI-54 | Story | API nhận/trả xe, tính phí trễ và hư hỏng | Tuấn | Tuấn | Dũng | CRAI-6 Thanh toán & Nhận/Trả xe | 5 | High | 23/11/2026 | Review | Code local: pickup, return, `CalculateReturn` có 3 unit test; chưa có test tích hợp DB; chưa đổi `Car.Status` |
| CRAI-55 | Story | Nối giao diện trả xe với API | Việt Anh | Việt Anh | Dũng | CRAI-6 Thanh toán & Nhận/Trả xe | 3 | Medium | 24/11/2026 | Review | `AdminReturnPage` đã đổi sang DangThue, `returnedAt/damageFee/damageNote`, đọc `FinalAmount`; chưa chạy trên trình duyệt |
| CRAI-56 | Story | Chatbot Gemini tư vấn chọn xe (`/chat`) | Năng | Dũng | Dũng | CRAI-7 Chatbot & Báo cáo | 5 | Medium | 25/11/2026 | To Do | Endpoint mới là stub |
| CRAI-57 | Story | API báo cáo doanh thu và vận hành | Tuấn | Tuấn | Dũng | CRAI-7 Chatbot & Báo cáo | 5 | Medium | 26/11/2026 | To Do | Chưa có |

### CRAI Sprint 5 (29/11 – 12/12/2026)

**Goal:** Hoàn thiện chatbot/báo cáo/PDF, kiểm thử, triển khai, nghiệm thu. **Số thẻ:** 9, tổng 30 Story Point.

| Key | Loại | Nội dung | Assignee | Người tạo | Người duyệt | Epic | Story Point | Priority | Ngày xong dự kiến | TT | Căn cứ Git |
|---|---|---|---|---|---|---|---|---|---|---|---|
| CRAI-58 | Task | Viết test overlap, ownership, chuyển trạng thái booking | Tuấn | Tuấn | Dũng | CRAI-4 Đặt xe | 3 | Medium | 29/11/2026 | Review | Đã có integration booking/ảnh, soft-delete và test validation/metadata; cần chạy và rà đủ ownership/transition qua HTTP |
| CRAI-59 | Bug | Correlation ID của booking và request AI không đảm bảo trùng | Dũng | Dũng | Tuấn | CRAI-5 Định giá AI | 2 | Medium | 30/11/2026 | Review | Booking đã truyền ID riêng qua request AI, client ưu tiên ID này; có `CorrelationIdTests`. Header request ngoài có thể khác ID booking |
| CRAI-60 | Story | Xuất hợp đồng thuê xe PDF | Tuấn | Tuấn | Dũng | CRAI-6 Thanh toán & Nhận/Trả xe | 5 | Medium | 02/12/2026 | To Do | Chưa có |
| CRAI-61 | Task | Nối khung chat trên giao diện với dịch vụ chatbot | Việt Anh | Việt Anh | Dũng | CRAI-7 Chatbot & Báo cáo | 2 | Low | 02/12/2026 | To Do | Giao diện đã có nút chat, chưa có chức năng thật |
| CRAI-62 | Story | Nối trang báo cáo với API | Việt Anh | Việt Anh | Dũng | CRAI-7 Chatbot & Báo cáo | 3 | Low | 03/12/2026 | To Do | Có `AdminReportsPage`, chưa có API |
| CRAI-63 | Task | Slide và kịch bản demo | Dũng | Dũng | Tuấn | CRAI-1 Nền tảng & Tài liệu | 2 | Medium | 06/12/2026 | To Do | Chưa có |
| CRAI-64 | Task | Kiểm thử tổng thể và nghiệm thu | Dũng | Dũng | Tuấn | CRAI-1 Nền tảng & Tài liệu | 3 | High | 07/12/2026 | To Do | Chưa có |
| CRAI-65 | Story | Khôi phục mật khẩu qua email (token một lần, hạn 15 phút) | Tuấn | Tuấn | Dũng | CRAI-2 Xác thực & Tài khoản | 5 | Low | 09/12/2026 | To Do | Chưa có code |
| CRAI-66 | Task | Triển khai staging (Railway/Render) | Tiến | Dũng | Dũng | CRAI-8 DevOps & Triển khai | 5 | Medium | 10/12/2026 | To Do | Chưa có |

## 5. Quan hệ giữa các thẻ

| Thẻ A | Quan hệ | Thẻ B | Lý do |
|---|---|---|---|
| CRAI-19 | blocks | CRAI-15 | Auth cần lưu/đọc Customer |
| CRAI-19 | blocks | CRAI-18 | CRUD cần Car repository và migration |
| CRAI-18 | blocks | CRAI-38 | Ảnh gắn với xe tồn tại |
| CRAI-30 | blocks | CRAI-38 | Upload cần MinIO |
| CRAI-15 | blocks | CRAI-28 | UI cần contract token và role |
| CRAI-18 | blocks | CRAI-35 | Lọc cần API query ổn định |
| CRAI-18 | blocks | CRAI-36 | UI quản lý dùng CRUD API |
| CRAI-21 | relates to | CRAI-37 | Cùng nhóm lỗi lệch contract giữa API và UI |
| CRAI-15 | blocks | CRAI-41 | Booking lấy khách từ JWT |
| CRAI-47 | blocks | CRAI-41 | Booking cần migration/DB khớp model |
| CRAI-41 | blocks | CRAI-45 | Chống đồng thời bổ sung vào luồng đặt xe |
| CRAI-41 | blocks | CRAI-43 | UI đặt xe cần API đúng contract |
| CRAI-41 | blocks | CRAI-54 | Nhận/trả xe cần booking đã xác nhận |
| CRAI-41 | blocks | CRAI-52 | Payment cần booking và giá chốt |
| CRAI-44 | blocks | CRAI-52 | Nhiều payment/booking phải kiểm chứng trước |
| CRAI-52 | blocks | CRAI-54 | Đối soát tiền khi trả xe |
| CRAI-51 | relates to | CRAI-48 | Chốt schema chung trước, phát triển song song bằng fixture |
| CRAI-49 | blocks | CRAI-48 | Sửa schema trước khi nghiệm thu client |
| CRAI-48 | blocks | CRAI-41 | Booking cần giá và fallback (làm được bằng fallback trước khi có model) |
| CRAI-20 | blocks | CRAI-32 | CI cần build và DI không lỗi |
| CRAI-32 | blocks | CRAI-66 | Staging cần pipeline ổn định |

Huấn luyện AI (CRAI-51) không phụ thuộc bảng Payment; booking kiểm thử được bằng AI giả lập hoặc fallback.

## 6. Quy tắc làm việc trên board

- Mỗi thẻ một Assignee chính; người phối hợp/review ghi trong Description.
- Bug phải ghi: cách tái hiện, kết quả thực tế, kết quả mong đợi, commit sửa.
- Chuyển **In Review** khi đã có PR; chuyển **Done** khi PR được duyệt, đã merge và đạt tiêu chí nghiệm thu.
- Quy trình Git: `feature/*` từ `develop` → Pull Request → 1 approval (reviewer Tuấn) → squash merge. Không push thẳng vào `main`/`develop`.
- Tên nhánh/commit ghi key thẻ, ví dụ `feature/CRAI-41-booking-overlap`, `fix: CRAI-37 show car type labels`.
- Không commit `.env`, khóa API hoặc chuỗi kết nối.
- Không ghi PASS hay Done khi chưa chạy kiểm tra.

## 7. Việc Còn Mở Theo Code Local

- Có nhiều thay đổi local chưa commit, gồm frontend quản trị xe, booking/AI client, migration và test; cần review theo diff thực tế. Trạng thái PR/Jira phải kiểm tra trên hệ thống tương ứng.
- Đã có test giá, AI retry/correlation, validation/metadata, booking/ảnh, soft-delete và quan hệ payment. Chưa có test Python; còn cần kiểm tra đủ coverage Auth, ownership và luồng HTTP.
- Integration test cần môi trường DB phù hợp; kết quả chạy thực và tình huống tắt container AI cần ghi bằng chứng riêng.
- FastAPI vẫn là stub, bỏ qua thời hạn/ngày thuê; cần dataset, model thật và cách nhận diện model chưa sẵn sàng.
- Chat UI gọi `/api/ai/chat` nhưng .NET chưa có proxy; payment, nhận/trả xe, PDF và báo cáo chưa có API.
- Trang ảnh còn dùng casing khác DTO, storage trả object key; cần thống nhất đường dẫn ảnh. Trang trả xe còn dùng trạng thái `Confirmed`, khác enum code `DaXacNhan`.
- CI hiện chỉ build/test .NET; chưa có job frontend/Python trong workflow.
