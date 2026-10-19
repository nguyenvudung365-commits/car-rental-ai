# Jira Scrum - Car Rental AI

> Backlog được đối chiếu với mã nguồn tại `7dd96c6` và lịch sử Git local. Các mốc thời gian giữ theo bản cũ để người phụ trách tự điều chỉnh.
> “Đã có code” không đồng nghĩa đã nghiệm thu. Chỉ chuyển Done khi đạt tiêu chí của thẻ và có kết quả kiểm thử. Phân công dưới đây là đề xuất theo vai trò trong dự án, không phải xác nhận người đã viết từng phần code.

## 0. Thiết lập Space Scrum

### 0.1 Thông tin dự án

- **Site:** car-rental-ai.atlassian.net
- **Project key:** CRAI.
- **Template:** Scrum, Team-managed.
- **Board:** To Do -> In Progress -> In Review -> Done. Với board rút gọn, ghi rõ đang review code hay đang kiểm thử trong comment.
- **Quy trình tham chiếu:** [Sprint backlog workflow](sprint-backlog-workflow.md).
- Giữ mã CRAI-1 đến CRAI-27 để đối chiếu bản cũ; tài liệu này không xác nhận trạng thái trực tiếp trên Jira.

### 0.2 Danh sách Sprint

| Sprint Jira | Tên đầy đủ | Thời gian (Add dates) | Goal | Epic chứa | Trạng thái |
|---|---|---|---|---|---|
| CRAI Sprint 01 | W1-W2: Nền tảng & Khung | 28/09 - 11/10/2026 | Chốt kiến trúc, ERD, API contract; dựng solution, Docker và CI nền tảng | CRAI-1, CRAI-2 | Active |
| CRAI Sprint 02 | W3-W4: Xác thực & Xe | 12/10 - 25/10/2026 | Tích hợp đăng ký/đăng nhập, phân quyền, danh mục xe và ảnh WebP | CRAI-3, CRAI-4 | Planned |
| CRAI Sprint 03 | W5-W6: Đặt xe & AI giá | 26/10 - 08/11/2026 | Hoàn thiện đặt xe, chống trùng lịch, quản lý booking và giá AI có fallback | CRAI-5, CRAI-6 | Planned |
| CRAI Sprint 04 | W7-W8: Thuê & Thông minh | 09/11 - 22/11/2026 | Thanh toán, nhận/trả xe, phí, hợp đồng và chức năng thông minh | CRAI-7, CRAI-8 | Planned |
| CRAI Sprint 05 | W9-W10: Release & Final - Ổn định, triển khai và nghiệm thu | 23/11 - 06/12/2026 | Kiểm thử tổng thể, triển khai, tài liệu và nghiệm thu | CRAI-9, CRAI-10 | Planned |

### 0.3 Phân công

| Thành viên | Vai trò | Trách nhiệm công việc |
|---|---|---|
| Dũng | Nhóm trưởng, kiến trúc, tích hợp | Chốt contract, review PR, kiểm tra tích hợp, nghiệm thu; làm thay Tuấn/Việt Anh khi cần |
| Tuấn | Backend | Auth, Customer/Car persistence, CRUD xe, booking, payment, nhận/trả xe; review PR của nhóm |
| Việt Anh | Frontend | Auth UI, danh mục/chi tiết xe, màn hình quản lý và luồng booking |
| Năng | AI | Dataset, huấn luyện, inference và kiểm thử dịch vụ giá |
| Tiến | DevOps | Docker, CI, dữ liệu demo và bằng chứng triển khai |

Mỗi thẻ giao một Assignee chính; người hỗ trợ/review ghi trong Description. Ghi chú "Dũng làm thay" nghĩa là Dũng thực hiện hộ trong giai đoạn này, Assignee chính vẫn là người phụ trách theo vai trò.

### 0.4 Epic

| Epic Jira | Tên | Nội dung công việc | Sprint |
|---|---|---|---|
| CRAI-1 | W1 — Foundation | Quyết định kiến trúc, ERD, API contract, cấu trúc solution | Sprint 01 |
| CRAI-2 | W2 — Skeleton | Entity, khung các layer, Docker, FastAPI stub, công thức giá và CI ban đầu | Sprint 01 |
| CRAI-3 | W3 — Auth | Đăng ký, đăng nhập, JWT/role, cập nhật hồ sơ và frontend xác thực | Sprint 02 |
| CRAI-4 | W4 — Cars | Persistence, CRUD, danh mục/chi tiết, lọc xe và ảnh WebP/MinIO | Sprint 02 |
| CRAI-5 | W5 — Booking | Tạo đơn, lịch thuê, chống trùng, lịch sử, xác nhận/hủy và phân quyền | Sprint 03 |
| CRAI-6 | W6 — AI Pricing | Contract FastAPI, model giá, HttpClient, fallback và giá chốt booking | Sprint 03 |
| CRAI-7 | W7 — Rental | Nhiều payment/booking, nhận/trả xe, phí và hợp đồng PDF | Sprint 04 |
| CRAI-8 | W8 — Intelligence | Chatbot và báo cáo vận hành/doanh thu | Sprint 04 |
| CRAI-9 | W9 — Release | Kiểm thử tổng thể, cấu hình production, CI và staging | Sprint 05 |
| CRAI-10 | W10 — Final | Tài liệu, slide, kịch bản demo, nghiệm thu và bàn giao | Sprint 05 |

### 0.5 Product Backlog

Story Point giữ theo bản cũ; chưa ước lượng thêm cho các subtask. Mỗi thẻ chỉ có một Epic cha. Thẻ bổ sung dùng mã nội bộ `S2-*`/`S3-*`, Jira tự cấp key khi tạo; không coi đây là issue đã tồn tại.

| Key | Loại | Nội dung công việc | Epic | Sprint | Assignee chính | SP | Priority | Hiện trạng đối chiếu mã nguồn |
|---|---|---|---|---|---|---|---|---|
| CRAI-11 | Task | Dựng solution, kiến trúc, entity, Compose và CI nền tảng | CRAI-1 | Sprint 01 | Dũng | 3 | Highest | Có skeleton/tài liệu trong `92df951`, `467087e`, `f416c67`; liên quan CRAI-2 |
| CRAI-12 | Story | Đăng ký, đăng nhập JWT, phân quyền và cập nhật hồ sơ | CRAI-3 | Sprint 02 | Tuấn | 5 | Highest | Có API/service trong `fada926`; cần kiểm thử tích hợp Auth/role |
| CRAI-13 | Task | Hoàn thiện Customer/Car persistence và ràng buộc dữ liệu | CRAI-3 | Sprint 02 | Tuấn | 3 | Highest | Có DbContext, repository và migration; cần kiểm tra DB thực |
| CRAI-14 | Story | Quản lý xe, kiểm tra biển số và trạng thái xe | CRAI-4 | Sprint 02 | Tuấn | 5 | High | Có CRUD, check biển số trùng, chặn xóa xe đang thuê; cần rà validation update |
| CRAI-15 | Story | Upload WebP lên MinIO và chọn ảnh đại diện | CRAI-4 | Sprint 02 | Dũng | 3 | High | Có validator/storage/API; chưa có endpoint xóa ảnh; cần kiểm tra UI/URL ảnh |
| CRAI-16 | Bug | Sửa trùng interface repository và cấu hình MinIO/DI | CRAI-4 | Sprint 02 | Dũng | 1 | Highest | Có bản sửa `674cac4`, merge `11812b0`; không suy ra CI hiện tại đã xanh |
| CRAI-27 | Story | Tích hợp frontend xác thực và danh mục xe | CRAI-3 | Sprint 02 | Việt Anh | 5 | High | Có React UI tại `7be7dd4`, sửa casing tại `8d73adc`; còn sai khác contract |
| CRAI-17 | Story | Đồng bộ lọc xe và phân trang giữa UI/API | CRAI-4 | Sprint 02 | Việt Anh | 5 | High | Đã sửa `CarListPage`: gửi `CarType/MinPrice/MaxPrice/Status/Page/PageSize`, đọc `total`, đổi giá trị dropdown theo enum API; API xác nhận lọc đúng bằng curl. Chưa có ảnh UI nên giữ In Review, chưa Done |
| CRAI-18 | Story | Khôi phục mật khẩu qua email | CRAI-3 | Product Backlog | Tuấn | 2 | Medium | Chưa có implementation; không cam kết trong Sprint 03 |
| CRAI-19 | Story | Đặt xe, chống trùng lịch, xem/xác nhận/hủy booking | CRAI-5 | Sprint 03 | Tuấn | 8 | Highest | Đã chạy thật qua Docker: tạo booking 201, trùng lịch 409 `BOOKING_OVERLAP`, `[13;15)` sau `[10;13)` được chấp nhận, Customer confirm 403, Admin confirm 200, không token 401. Chưa có unit test và chưa kiểm tra đặt đồng thời |
| CRAI-20 | Task | Kiểm chứng Booking 1-N Payment và migration | CRAI-5 | Sprint 03 | Tuấn | 3 | Highest | Phát hiện snapshot lệch tên bảng (Booking/Payment/ReturnRecord) so với model; đã thêm migration `AlignTableNames` chỉ đổi tên bảng/index/PK/FK. Migrate chạy được trên DB trống trong Docker; chưa kiểm thử nhiều Payment cùng Booking và chưa chạy trên DB đã có dữ liệu |
| CRAI-21 | Story | Xây dựng model và API dự đoán giá thuê xe | CRAI-6 | Sprint 03 | Năng (Dũng làm thay) | 5 | High | FastAPI mới trả giá gốc với `model_used=stub_fallback`; chưa có model thật |
| CRAI-22 | Task | Hoàn thiện AiPricingClient, fallback và truy vết giá | CRAI-6 | Sprint 03 | Dũng | 3 | High | Có timeout/retry/fallback; schema lệch FastAPI, correlation chưa xuyên suốt |
| CRAI-23 | Story | Thanh toán cọc/phần còn lại và lịch sử thanh toán | CRAI-7 | Sprint 04 | Tuấn | 5 | High | Mới có entity Payment và quan hệ 1-N với Booking; chưa có Payment API hoặc UI. Phụ thuộc CRAI-19 và CRAI-20 |
| CRAI-24 | Story | Nhận/trả xe và tính phí trễ/hư hỏng | CRAI-7 | Sprint 04 | Tuấn | 5 | High | Có entity ReturnRecord và UI trả xe (`AdminReturnPage`); chưa có API nhận/trả xe nên UI chưa dùng được thật. Phụ thuộc CRAI-23 |
| CRAI-25 | Task | Hoàn thiện CI và triển khai staging | CRAI-9 | Sprint 05 | Tiến (Dũng làm thay) | 3 | Medium | Docker Compose chạy đủ 5 service sau khi đổi port MinIO ngoài sang biến `MINIO_HOST_PORT`; có workflow trong `.github`. Còn thiếu log pipeline và bằng chứng deploy staging |
| CRAI-26 | Task | Báo cáo, slide và nghiệm thu | CRAI-10 | Sprint 05 | Dũng | 2 | Medium | Có tài liệu kế hoạch; cần hoàn thiện theo kết quả thực tế |

CRAI-17 xếp về Sprint 02 vì lọc danh mục là đầu ra của module Cars. CRAI-18 giữ trong Product Backlog để Sprint 03 tập trung booking và AI. Đây là điều chỉnh phân bổ nội dung, không đổi ngày Sprint.

### 0.6 Phân bổ thẻ và mốc Sprint

- **Sprint 01:** CRAI-11, liên kết CRAI-1/2 -> Start 28/09 -> Complete 11/10. Đối chiếu sản phẩm nền tảng đã có trong Git.
- **Sprint 02:** CRAI-12..17, CRAI-27 và S2-01/S2-02 -> Start 12/10 -> Complete 25/10. Ưu tiên tích hợp, sửa lỗi contract và nghiệm thu Auth/Cars.
- **Sprint 03:** CRAI-19..22 và S3-01/S3-02 -> Start 26/10 -> Complete 08/11. CRAI-20 làm trước vì booking cần DB nhất quán; CRAI-18 chưa đưa vào sprint.
- **Sprint 04:** CRAI-23, CRAI-24; bổ sung thẻ PDF/chatbot/báo cáo khi refinement -> Start 09/11 -> Complete 22/11.
- **Sprint 05:** CRAI-25, CRAI-26 -> Start 23/11 -> Complete 06/12.

Kéo Story/Task/Bug vào sprint; dùng Epic để nhóm công việc. Không chuyển toàn bộ sprint sang Done chỉ dựa vào việc một nhánh đã merge.

## 1. Quan hệ giữa các thẻ

| Thẻ A | Quan hệ | Thẻ B | Phụ thuộc cụ thể |
|---|---|---|---|
| CRAI-13 | blocks | CRAI-12 | Auth cần truy vấn/lưu Customer và ràng buộc dữ liệu |
| CRAI-13 | blocks | CRAI-14 | CRUD cần Car repository và migration |
| CRAI-14 | blocks | CRAI-15 | Upload/chọn ảnh phải gắn với xe tồn tại |
| CRAI-12 | blocks | CRAI-27 | UI xác thực cần contract token và role |
| CRAI-14 | blocks | CRAI-17 | Danh mục cần API query ổn định |
| CRAI-14 | blocks | S2-01 | UI quản lý xe dùng CRUD API |
| CRAI-12 | blocks | CRAI-19 | Booking lấy CustomerId từ JWT |
| CRAI-20 | blocks | CRAI-19 | Lưu/đọc booking cần model và DB nhất quán |
| CRAI-19 | blocks | S3-01 | Form/lịch sử/quản lý booking cần API đúng contract |
| CRAI-21 | relates to | CRAI-22 | Chốt schema chung trước; model và client có thể phát triển bằng fixture |
| CRAI-22 | blocks | S3-02 | Nghiệm thu tích hợp cần client xử lý thành công và lỗi |
| CRAI-17 | relates to | CRAI-27 | Cùng sửa casing/contract UI; CRAI-17 chỉ phần lọc và phân trang |
| CRAI-19 | blocks | CRAI-24 | Nhận/trả xe cần booking đã xác nhận |
| CRAI-16 | blocks | CRAI-25 | CI/staging cần build và DI không lỗi |
| CRAI-19 | blocks | CRAI-23 | Payment cần booking và giá chốt |
| CRAI-23 | blocks | CRAI-24 | Hoàn thiện đối soát tiền trong luồng trả xe |
| CRAI-16 | relates to | CRAI-15 | Sửa wiring repository/storage để build và resolve dependency |

Huấn luyện AI không phụ thuộc bảng Payment. Booking core kiểm thử được bằng AI giả lập/fallback; nhánh AI thật chỉ nghiệm thu khi CRAI-21 và CRAI-22 cùng đạt.

## 2. Kế hoạch chi tiết Sprint 02: Xác thực và quản lý xe

**Sprint Goal:** Khách đăng ký, đăng nhập, xem và lọc danh mục xe; Admin/Staff quản lý xe và ảnh qua luồng đã kiểm thử với API/DB thật.

**Thời gian:** Người dùng tự cập nhật | **Epic:** CRAI-3, CRAI-4.

Các bước dưới đây gồm phần đã có và phần cần hoàn thiện, dùng làm Description/subtask trên Jira. Không tạo lại chức năng đã có nếu chỉ còn thiếu kiểm thử.

### CRAI-13 — Customer/Car persistence và migration

**Owner:** Tuấn. **Review:** Dũng. **Đã có:** DbContext, repository Customer/Car và migration ban đầu.

**Subtask:**
1. Đối chiếu entity, mapping và migration Customer/Car/CarImage; kiểm tra khóa ngoại và kiểu dữ liệu.
2. Kiểm tra unique index Email/Phone/LicensePlate và lỗi khi dữ liệu trùng.
3. Apply migration trên DB kiểm thử; chuẩn bị tài khoản Customer/Staff/Admin và xe ở các trạng thái phục vụ demo.
4. Kiểm thử đọc/ghi repository, Include ảnh và phân trang; ghi cách dựng lại dữ liệu mẫu.

**Nghiệm thu:** DB dựng được từ migration; dữ liệu mẫu truy vấn được; vi phạm unique không tạo bản ghi trùng; có log migration và kết quả kiểm tra ràng buộc.

### CRAI-12 — Xác thực JWT và hồ sơ người dùng

**Owner:** Tuấn. **Review:** Dũng. **Đã có:** register/login, BCrypt, JWT và `PUT /api/auth/me`.

**Subtask:**
1. Rà DTO đăng ký, trường bắt buộc, email/số điện thoại trùng và mật khẩu không hợp lệ.
2. Kiểm tra hash mật khẩu, đăng nhập sai, claims định danh/role và thời hạn token; không trả password hash.
3. Kiểm tra JWT middleware và quyền public/Customer/Staff/Admin bằng token của từng vai trò.
4. Kiểm tra cập nhật hồ sơ lấy ID từ token; không cho sửa người khác hoặc tự nâng quyền.
5. Thêm kiểm thử Auth và ghi response mẫu cho frontend.

**Nghiệm thu:** Đăng ký/đăng nhập hợp lệ thành công; thông tin sai bị từ chối; endpoint bảo vệ trả 401 khi thiếu/hỏng/hết hạn token, 403 khi sai quyền; hồ sơ cập nhật đúng tài khoản.

### CRAI-14 — CRUD xe và quy tắc dữ liệu

**Owner:** Tuấn. **Review:** Dũng. **Đã có:** CRUD, check biển số trùng và chặn xóa xe `DangThue`.

**Subtask:**
1. Rà `GET/POST/PUT/DELETE /api/cars`; thống nhất response và lỗi với frontend.
2. Bổ sung validation update tương đương create: biển số, hãng/model, số ghế, tuổi xe, giá gốc và giá ghi đè.
3. Kiểm tra create/update trùng biển số; sửa chính xe đó không bị báo trùng sai.
4. Kiểm tra xóa xe đang thuê và xe có booking tham chiếu; từ chối rõ ràng thay vì lỗi FK/500. Chốt quy tắc trước khi đổi cách xóa hiện có.
5. Test quyền Admin/Staff được ghi, khách được đọc; xe không tồn tại trả 404.

**Nghiệm thu:** CRUD hợp lệ thành công; dữ liệu sai bị từ chối; trùng biển số trả 409; xóa không làm mất lịch sử booking hoặc gây lỗi không xử lý; Customer không sửa/xóa được xe.

### CRAI-15 — WebP, MinIO và ảnh đại diện

**Owner:** Dũng. **Phối hợp:** Việt Anh. **Đã có:** kiểm tra đuôi/MIME/header WebP, giới hạn 5 MB, storage và API chọn ảnh chính.

**Subtask:**
1. Kiểm tra cấu hình MinIO, bucket, credential qua môi trường và URL ảnh truy cập được từ trình duyệt.
2. Test WebP hợp lệ, file rỗng, quá dung lượng, giả đuôi WebP và MIME sai.
3. Kiểm tra ảnh đầu tiên là ảnh chính; đổi ảnh chính chỉ tác động ảnh của đúng xe.
4. Đồng bộ `CarDto.Images` và `CarImageDto.FilePath/IsPrimary` với gallery, card và trang quản lý ảnh.
5. Điều chỉnh UI upload theo định dạng API chấp nhận; xử lý upload thất bại và ảnh không tải được.

**Nghiệm thu:** Upload ảnh thật, refresh vẫn xem được; đổi ảnh chính phản ánh ở danh mục/chi tiết; file sai bị từ chối; Customer không upload/chọn ảnh chính. Xóa ảnh chưa có API, không đưa vào phạm vi đã hoàn thành.

### CRAI-16 — Khép lại lỗi build/repository

**Owner:** Dũng. **Đã có:** bản sửa `674cac4`, merge `11812b0`.

**Subtask:** Kiểm tra không còn interface trùng, dependency MinIO và DI resolve đúng; chạy restore/build/test trên revision nghiệm thu; gắn kết quả thực vào thẻ.

**Nghiệm thu:** Build/test thành công, API khởi động và resolve storage/repository. Giữ đây là bug đã có bản sửa, không mô tả thành tính năng mới.

### CRAI-27 — Frontend xác thực và danh mục/chi tiết

**Owner:** Việt Anh. **Phối hợp:** Tuấn. **Đã có:** router/layout, AuthContext, axios client, login/register và trang xe.

**Subtask:**
1. Đồng bộ trường register/login; kiểm tra lưu phiên, logout và xử lý 401.
2. Rà casing JSON và cấu trúc response từng màn hình; dùng API thật để phát hiện trường hiển thị rỗng.
3. Rà PrivateRoute; thống nhất quyền Staff với backend cho màn hình quản lý trong phạm vi sprint.
4. Hoàn thiện tên xe, biển số, giá, trạng thái, gallery và ảnh chính đúng DTO.
5. Kiểm tra loading/empty/error, URL xe không tồn tại và màn hình nhỏ.

**Nghiệm thu:** Đăng ký -> đăng nhập -> danh mục -> chi tiết -> logout chạy trên browser; route bảo vệ đúng quyền; không lỗi console do sai contract. Filter nghiệm thu riêng ở CRAI-17.

### CRAI-17 — Lọc xe và phân trang

**Owner:** Việt Anh. **Phối hợp:** Tuấn.

**Vấn đề hiện tại:** `CarListPage` gửi `type`, API nhận `CarType`; UI đọc `totalCount`, controller trả `{ items, total }`. API chưa có filter số ghế.

**Subtask:**
1. Thống nhất query loại xe, khoảng giá, trạng thái, Page/PageSize và mapping enum.
2. Đồng bộ tổng bản ghi với `total`; kiểm tra bằng dữ liệu nhiều hơn một trang.
3. Kiểm tra kết hợp filter, đặt lại filter và quay về trang đầu khi điều kiện thay đổi.
4. Validate min/max price, page/pageSize; giữ thông báo lỗi và trạng thái không có kết quả.

**Nghiệm thu:** Từng filter và tổ hợp cho đúng xe; tổng số/trang khớp DB; trang cuối không mất dữ liệu. Lọc số ghế/tìm xe trống theo ngày là mở rộng, không ghi nhận đã có.

### S2-01 — Hoàn thiện giao diện quản lý xe

**Loại:** Task bổ sung dưới CRAI-4. **Owner:** Việt Anh. **Priority:** High.

**Hiện trạng:** Router có trang quản lý ảnh; chưa thấy trang danh sách/form CRUD xe dành cho quản trị.

**Subtask:** Tạo danh sách quản lý xe; form thêm/sửa thông tin và trạng thái; xác nhận xóa; hiển thị lỗi 400/403/404/409; nối tới quản lý ảnh; bảo vệ route theo quyền đã thống nhất.

**Nghiệm thu:** Admin/Staff tạo/sửa xe trên UI, dữ liệu phản ánh ở danh mục; thao tác xóa bị chặn có thông báo; Customer không vào được màn hình hoặc gọi API ghi.

### S2-02 — Kiểm thử tích hợp và nghiệm thu Sprint 02

**Loại:** Task bổ sung dưới CRAI-4. **Owner:** Dũng. **Phối hợp:** Tuấn, Việt Anh. **Priority:** Highest.

**Subtask:** Dựng API/SQL/MinIO/frontend cùng môi trường; chạy case Auth, quyền, CRUD, ảnh, filter; ghi lỗi và kiểm tra lại; lưu build/test log, response và ảnh demo.

**Điều kiện kết thúc:** Luồng khách/quản trị đạt AC; không còn lỗi chặn đăng nhập, quản lý/xem xe hoặc tải ảnh. Phần chưa đạt giữ đúng trạng thái và ghi việc còn lại.

### Trình tự triển khai Sprint 02

1. Tuấn kiểm tra persistence/Auth; Dũng chuẩn bị môi trường và bản sửa build; Việt Anh rà contract UI.
2. Tuấn hoàn thiện CRUD/validation; Việt Anh tích hợp auth/catalog/filter sau khi contract ổn định.
3. Dũng kiểm tra MinIO; Việt Anh hoàn thiện ảnh và UI quản lý xe.
4. Cả nhóm chạy S2-02, sửa lỗi theo ảnh hưởng và kiểm tra lại từng AC.

**Bàn giao sang Sprint 03:** Tài khoản theo vai trò, xe mẫu có giá/trạng thái/ảnh, contract Auth/Cars thống nhất, DB dựng lại được và kết quả smoke test.

## 3. Kế hoạch chi tiết Sprint 03: Đặt xe và AI Pricing

**Sprint Goal:** Khách đặt xe theo khoảng ngày, xem/hủy đơn của mình; Admin/Staff xác nhận và quản lý đơn; backend ngăn trùng lịch, lưu giá chốt và vẫn đặt được khi AI lỗi.

**Thời gian:** Người dùng tự cập nhật | **Epic:** CRAI-5, CRAI-6.

**Đầu vào:** Auth/Car API và dữ liệu mẫu Sprint 02 dùng được. Chốt schema `/predict-price` trước khi tích hợp model/client.

### CRAI-20 — Kiểm chứng Booking 1-N Payment

**Owner:** Tuấn. **Review:** Dũng. **Đã có:** `Booking.Payments` và migration bỏ unique trên `Payment.BookingId`.

**Subtask:**
1. Đối chiếu entity, DbContext, snapshot và migration; kiểm tra migration được EF nhận diện.
2. Apply trên DB mới và DB ở migration trước; xác nhận index không unique, FK đúng.
3. Tạo dữ liệu kiểm thử nhiều Payment cùng Booking; kiểm tra repository đọc được quan hệ.
4. Ghi kết quả và cách chuẩn bị DB cho booking test.

**Nghiệm thu:** Hai payment cùng tham chiếu một booking; đọc lại đúng; migration thực thi thành công. Đây là nền tảng dữ liệu, chưa bao gồm API thanh toán.

### CRAI-19 — Booking core, lịch thuê và quyền truy cập

**Owner:** Tuấn. **Review/tích hợp:** Dũng. **Đã có:** tạo/list/detail/confirm/cancel và overlap query trong `7dd96c6`.

**Subtask:**
1. Rà `POST /api/bookings`: lấy khách từ JWT, kiểm tra xe tồn tại/sẵn sàng, ngày nhận không ở quá khứ và ngày trả sau ngày nhận.
2. Chốt dữ liệu thuê theo ngày; đồng bộ RentalDays giữa UI/backend, tránh giờ lẻ bị tính 0 ngày hoặc khác tiền.
3. Kiểm tra overlap nửa mở `[StartDate, EndDate)` với `ChoXacNhan`, `DaXacNhan`, `DangThue`; bỏ qua đơn hủy/đã trả.
4. Bảo vệ bước kiểm tra và lưu trước hai request đồng thời cùng xe; chọn transaction/locking phù hợp SQL Server và viết integration test tái hiện.
5. Rà list/detail: Customer chỉ đọc đơn mình, Admin/Staff theo quyền; kiểm tra filter trạng thái trên EF/SQL thật và phân trang.
6. Chốt chuyển trạng thái: confirm chỉ từ `ChoXacNhan`; cancel trong trạng thái cho phép, lưu lý do; không hủy đơn đang thuê/hoàn tất nếu chưa có nghiệp vụ xử lý tương ứng.
7. Thêm test ngày, overlap, ownership và trạng thái; chuẩn hóa lỗi cho UI.

**Nghiệm thu:**
- Đặt hợp lệ trả 201, lưu đúng khách/xe/ngày/số ngày/giá và trạng thái chờ xác nhận.
- Ngày sai trả 400; xe không có trả 404; xe không sẵn sàng/lịch trùng trả 409.
- Hai khoảng sát nhau được chấp nhận; giao một phần, trùng hoàn toàn hoặc bao phủ bị chặn; khác xe không bị chặn nhầm.
- Hai request đồng thời trùng lịch chỉ tạo một booking; request còn lại có lỗi nghiệp vụ rõ.
- Customer không xem/hủy đơn người khác hoặc xác nhận đơn; confirm/cancel lặp không tạo chuyển trạng thái sai.

**Khoảng trống hiện tại:** Code kiểm tra overlap rồi lưu ở bước riêng, chưa thấy bảo vệ concurrency; CancelAsync chưa chặn `DangThue`. Đây là việc cần làm, chưa phải bản sửa đã thực hiện.

### CRAI-21 — Dataset, model và FastAPI giá thuê

**Owner:** Dũng (kiêm Năng). **Phối hợp contract:** Tuấn.

**Hiện trạng:** `ai-service/main.py` mới trả base price; không coi `stub_fallback` là model đã huấn luyện.

**Subtask:**
1. Chốt schema: `car_type`, `seats`, `car_age_years`, `base_price_per_day`; quyết định có dùng `rental_days/start_date` rồi đồng bộ hai phía. Ưu tiên tên output `predicted_price_per_day` đang có ở FastAPI.
2. Chuẩn bị dataset có nguồn/mô tả; nếu mô phỏng ghi rõ cách sinh, seed và giới hạn, không gọi là giao dịch thực.
3. Tách train/test, xây baseline, huấn luyện Random Forest hoặc XGBoost; lưu script, preprocessing, model và feature.
4. Đánh giá MAE/RMSE trên tập test, so với baseline và ghi kết quả thực; không tự điền số liệu.
5. Load model khi service khởi động; inference, validation và metadata model. Không trình bày `confidence` như độ tin cậy đã hiệu chuẩn nếu chưa có cách tính.
6. Test schema, input sai, thiếu/hỏng model và khởi động lại container.

**Nghiệm thu:** Request hợp lệ gọi model đã lưu, trả giá hữu hạn/dương đúng schema; có dataset/script/model và đánh giá tái lập được; thiếu model có lỗi rõ để backend fallback. Stub chỉ phục vụ phát triển, không chứng minh AI hoàn thành.

### CRAI-22 — AiPricingClient, fallback và giá chốt

**Owner:** Dũng. **Phối hợp:** Tuấn.

**Vấn đề hiện tại:** Client gửi `car_type` chuỗi trong khi FastAPI nhận số; client đọc `predicted_price/predictedPrice` nhưng FastAPI trả `predicted_price_per_day`. Booking tự sinh correlation ID, client lại lấy từ request header nên chưa bảo đảm trùng.

**Subtask:**
1. Sửa mapping request/response theo CRAI-21; thêm contract test bằng JSON FastAPI thực trả.
2. Kiểm tra timeout mỗi lần gọi 2 giây, tối đa 2 retry; phân biệt lỗi tạm thời và 4xx do request sai để tránh retry vô ích.
3. Xử lý mất kết nối, timeout, 5xx, JSON hỏng/thiếu trường, giá không hợp lệ; fallback có log lý do và không làm mất booking hợp lệ.
4. Dùng cùng correlation ID cho booking, request AI và log; sinh ID nếu chưa có và truyền xuyên suốt.
5. Kiểm tra `OverridePrice ?? PredictedPrice ?? BasePricePerDay`, clamp 400.000–1.500.000 đồng/ngày; lưu giá dự đoán/giá chốt/tổng tiền/cờ fallback.
6. Thêm test success, retry/fallback, ưu tiên override, biên clamp và dữ liệu đã lưu; giữ test PricingCalculator hiện có.

**Nghiệm thu:** AI thành công đọc đúng và `IsFallback=false`; AI lỗi dùng base price làm đầu vào dự phòng và `IsFallback=true`. Giá cuối vẫn theo override/clamp, không mặc định luôn bằng giá gốc. Booking cũ giữ giá chốt khi giá xe/model thay đổi.

**Kiểm thử thời gian phản hồi:** Timeout 2 giây là mỗi lần gọi; tổng thời gian có retry dài hơn. Kiểm tra thời gian toàn luồng và UI chờ, không ghi cam kết toàn bộ booking dưới 2 giây.

### S3-01 — Frontend booking và quản lý đơn

**Loại:** Story bổ sung dưới CRAI-5. **Owner:** Việt Anh. **Phối hợp:** Tuấn. **Priority:** Highest.

**Đã có:** BookingPage, MyBookingsPage, AdminBookingsPage; cần kiểm thử với backend thật.

**Subtask:**
1. Hoàn thiện danh mục -> chi tiết -> chọn ngày -> tạo đơn; đồng bộ số ngày, ngăn submit lặp trong lúc chờ.
2. Hiển thị giá tạm tính trước gửi và giá chốt từ server sau tạo; lấy ảnh/tên xe đúng DTO.
3. Hiển thị giá AI/giá áp dụng/tổng tiền/fallback; không nói chỉ áp dụng giá niêm yết khi có override/clamp.
4. Tích hợp lịch sử, lọc/phân trang, hủy đơn của khách; refresh dữ liệu sau thao tác.
5. Hoàn thiện danh sách quản trị, chi tiết, confirm/cancel theo trạng thái và quyền Staff/Admin đã thống nhất.
6. Hiển thị lỗi 400/401/403/404/409, lỗi mạng và loading khi AI retry; không báo thành công nếu API thất bại.

**Nghiệm thu:** Customer tạo/xem/hủy đúng quyền; Admin/Staff xác nhận được; UI phản ánh trạng thái mới; giá khớp DB/response; lỗi trùng lịch đủ rõ để chọn lại ngày. Nhận/trả xe thuộc Sprint 04.

### S3-02 — Kiểm thử tích hợp booking và AI

**Loại:** Task bổ sung dưới CRAI-6. **Owner:** Dũng. **Phối hợp:** Tuấn, Việt Anh. **Priority:** Highest.

| Nhóm kiểm thử | Kịch bản | Kết quả cần lưu |
|---|---|---|
| Booking hợp lệ | Customer đặt xe/xem lịch sử; Staff/Admin xác nhận | Response, bản ghi DB, ảnh UI |
| Biên lịch thuê | Sát nhau, giao một phần, bao phủ, khác xe, đơn đã hủy | Kết quả cho phép/từ chối từng case |
| Đặt đồng thời | Hai request cùng xe/khoảng ngày | Chỉ một booking được lưu, response cả hai request |
| Quyền/trạng thái | Xem/hủy đơn người khác, tự confirm, confirm/hủy lặp, hủy đơn đang thuê | Lỗi phù hợp, DB không chuyển trạng thái sai |
| AI thật | .NET gọi model FastAPI | Schema đúng, giá lưu đúng, fallback false |
| AI lỗi | Tắt service, timeout, 5xx, JSON hỏng, giá không hợp lệ | Booking vẫn lưu, fallback đúng, log/correlation đối chiếu được |
| Giá | Override, không override, clamp hai biên, đổi giá sau khi đặt | Giá cuối/tổng tiền đúng, giá chốt cũ không đổi |
| UI/DB | Phân trang, filter trạng thái trên SQL thật, refresh | Tổng bản ghi/trạng thái đúng, không lỗi dịch truy vấn hoặc casing |

**Đầu ra:** Test report ghi case đạt/chưa đạt, revision, môi trường, log build/test, response và ảnh demo. Không điền PASS trước khi thực hiện.

### Trình tự triển khai Sprint 03

1. Chốt schema AI, quy tắc ngày/trạng thái và dữ liệu demo. Tuấn kiểm chứng CRAI-20; Việt Anh rà DTO UI.
2. Tuấn hoàn thiện booking/concurrency; Việt Anh nối form/lịch sử/quản trị theo contract.
3. Dũng thực hiện dataset/model CRAI-21, sau đó hoàn thiện client/correlation CRAI-22; dùng fixture để các phần khác không phải chờ model.
4. Tích hợp API/SQL/AI/frontend, chạy nhánh AI thành công và fallback.
5. Chạy S3-02, sửa lỗi bắt buộc, kiểm tra lại và bàn giao booking/giá chốt cho Sprint 04.

### Phạm vi và điều kiện kết thúc Sprint 03

- **Bắt buộc:** Booking, ownership, confirm/cancel, chống trùng kể cả đồng thời, model giá, contract AI, fallback và UI tương ứng.
- **Ngoài phạm vi:** Payment API, nhận/trả xe, phí, PDF, chatbot, báo cáo và reset mật khẩu.
- **Được nghiệm thu:** AC bắt buộc đạt và có bằng chứng tích hợp; không còn lỗi chặn tạo đơn, sai quyền, trùng lịch hoặc sai tiền.
- **Nếu model chưa đạt:** Demo booking với fallback và ghi rõ giới hạn; giữ CRAI-21 chưa hoàn thành, không kết luận mục tiêu AI đã đạt.

## 4. Thẻ dự phòng CRAI-18 — Khôi phục mật khẩu

**Epic:** CRAI-3. **Vị trí:** Product Backlog, chưa cam kết Sprint 03. **Owner dự kiến:** Tuấn.

**Công việc khi được chọn:** Token một lần, lưu hash và hạn dùng 15 phút; email provider phía .NET; API yêu cầu/đặt lại mật khẩu; UI nhập email/mật khẩu mới; response không tiết lộ tài khoản tồn tại; test token hết hạn/đã dùng.

**Nghiệm thu dự kiến:** Đặt lại mật khẩu bằng link hợp lệ; token hết hạn/đã dùng bị từ chối; mật khẩu mới được hash. Chưa có bằng chứng triển khai hoặc gửi mail, không ghi comment Done/PR giả định.

## 5. Ghi nhận tiến độ và bằng chứng

Comment chỉ ghi kết quả đã làm và việc tiếp theo. Không dựng lịch sử trao đổi, số PR hoặc kết quả test để minh họa như thể đã xảy ra.

**Mẫu cập nhật để điền khi thực hiện:**

```text
Đã thực hiện: [chức năng/bản sửa cụ thể]
Kết quả kiểm tra: [case đã chạy, đạt/chưa đạt]
Còn lại: [subtask hoặc AC chưa đạt]
Vướng mắc: [phụ thuộc và người xử lý, nếu có]
Bằng chứng: [commit/PR/log/response/ảnh thực tế]
```

**Mốc đối chiếu được trong Git local:**

| Revision | Nội dung làm căn cứ |
|---|---|
| `92df951` | Skeleton Domain/API/Docker/CI/ERD, PR #7 |
| `fada926` | Auth, Customers và Cars, PR #15 |
| `7be7dd4` | Frontend React, PR #11 |
| `674cac4`, `11812b0` | Sửa duplicate repository và wiring MinIO, merge PR #16 |
| `8d73adc` | Điều chỉnh hỗ trợ response PascalCase |
| `7dd96c6` | Booking flow, repository, controller và tích hợp client AI |

Nguồn đối chiếu: `src/CarRental.API/Controllers`, `src/CarRental.Application/Modules`, `src/CarRental.Infrastructure`, `frontend/src`, `ai-service/main.py`, `tests/CarRental.Tests`. Thư mục test hiện chỉ thấy test PricingCalculator; các bộ test Auth/Car/Booking/AI liệt kê trên là công việc cần bổ sung.
