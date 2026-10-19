# Quy trình Sprint và luồng thẻ Backlog — Car Rental AI

> Tài liệu vận hành board cho dự án Car Rental AI.
>
> Phạm vi dự án: 10 tuần, 5 thành viên, chia thành 5 sprint, mỗi sprint 2 tuần.
>
> Tài liệu liên quan: [Kế hoạch triển khai 10 tuần](weekly-plan.md), [Hướng dẫn quản lý Jira](jira-project-management-guide.md), [Quyết định kiến trúc](decisions.md).

---

## 1. Mô hình Sprint

### 1.1. Thời lượng

- **1 Sprint = 2 tuần = 10 ngày làm việc.**
- Tổng dự án gồm **5 Sprint trong 10 tuần**.
- Mỗi Sprint bắt đầu vào **thứ Hai** và kết thúc vào **thứ Sáu của tuần thứ hai**.
- Không đưa thêm phạm vi mới vào Sprint sau khi đã chốt, trừ lỗi P0 hoặc quyết định được Team Lead phê duyệt.

### 1.2. Lịch cố định trong Sprint

| Thời điểm | Hoạt động | Kết quả cần có |
|---|---|---|
| Thứ Hai, tuần 1 | Sprint Planning | Sprint Goal, danh sách thẻ, người phụ trách, dependency |
| Mỗi ngày | Daily Stand-up 15 phút | Cập nhật tiến độ, trở ngại và kế hoạch trong ngày |
| Thứ Tư, tuần 1 | Kiểm tra giữa Sprint | Phát hiện trễ, xử lý dependency, cân bằng lại tải |
| Thứ Tư, tuần 2 | Code freeze tương đối | Không nhận feature mới; ưu tiên test và sửa lỗi |
| Thứ Năm, tuần 2 | Chuẩn bị Review | Hoàn tất evidence, demo script và test result |
| Thứ Sáu, tuần 2 | Sprint Review | Demo phần hoàn thành, nghiệm thu thẻ |
| Thứ Sáu, tuần 2 | Retrospective | Ghi lại điều làm tốt, vấn đề và hành động cải thiện |
| Sau Retrospective | Backlog Refinement | Làm rõ thẻ cho Sprint kế tiếp |

### 1.3. Sprint Goal và phạm vi dự kiến

| Sprint | Tuần | Sprint Goal | Kết quả chính |
|---|---:|---|---|
| Sprint 1 | W1–W2 | Chốt nền tảng và dựng hệ thống chạy được | Architecture, ERD, API contract, solution, Docker, CI, health check |
| Sprint 2 | W3–W4 | Hoàn thiện người dùng và danh mục xe | Register/login/JWT, role, customer profile, Car CRUD, ảnh, filter |
| Sprint 3 | W5–W6 | Hoàn thiện booking và giá AI | Booking, chống trùng lịch, confirm/cancel, predict price, fallback |
| Sprint 4 | W7–W8 | Hoàn thiện vòng đời thuê và tính năng thông minh | Payment, nhận/trả xe, late fee, PDF, chatbot, báo cáo |
| Sprint 5 | W9–W10 | Ổn định, triển khai và nghiệm thu | Integration test, security review, staging, demo, slide, rehearsal |

---

## 2. Các cột trên Board

Board dùng các cột theo thứ tự sau:

```text
Backlog
  → Selected for Development
  → In Progress
  → Code Review
  → Testing
  → Ready for Acceptance
  → Done
```

Nhánh xử lý ngoại lệ:

```text
In Progress → Blocked → In Progress
Testing → Reopened → In Progress
Ready for Acceptance → Reopened → In Progress
```

### 2.1. Ý nghĩa từng cột

| Cột | Thẻ được đưa vào khi | Không được chuyển tiếp nếu |
|---|---|---|
| **Backlog** | Ý tưởng hoặc công việc đã ghi nhận nhưng chưa cam kết | Thiếu mô tả, owner, độ ưu tiên hoặc acceptance criteria |
| **Selected for Development** | Đã chọn cho Sprint hiện tại và đạt Definition of Ready | Chưa rõ dependency, đầu vào/đầu ra hoặc người xử lý |
| **In Progress** | Người phụ trách đã bắt đầu thực hiện | Không có branch hoặc không cập nhật tiến độ |
| **Code Review** | Đã hoàn thành phần code/tài liệu và đã mở Pull Request | Chưa có PR, test cơ bản hoặc mô tả thay đổi |
| **Testing** | PR đã được review chấp thuận hoặc merge theo quy trình | CI còn đỏ hoặc chưa có hướng dẫn kiểm thử |
| **Ready for Acceptance** | Đã đạt Definition of Done kỹ thuật và có evidence | Thiếu test result, screenshot, log hoặc tài liệu liên quan |
| **Done** | Owner/Team Lead đã nghiệm thu acceptance criteria | Còn comment PR, lỗi bắt buộc hoặc thiếu evidence |
| **Blocked** | Không thể tiếp tục do dependency, môi trường hoặc quyết định chưa có | Không ghi rõ lý do chặn và hành động tiếp theo |
| **Reopened** | Không đạt kiểm thử hoặc nghiệm thu | Không ghi rõ tiêu chí nào chưa đạt |

---

## 3. Quy tắc kéo thẻ

### 3.1. Từ Backlog sang Selected for Development

Chỉ kéo thẻ vào Sprint khi đủ các thông tin sau:

- Summary rõ ràng, mô tả một kết quả cụ thể.
- Có `Epic`, `Sprint`, `Area`, `Priority` và `Assignee`.
- Có mô tả mục tiêu, phạm vi và đầu vào/đầu ra.
- Có acceptance criteria kiểm chứng được.
- Dependency đã được liên kết bằng `blocks` hoặc `is blocked by`.
- Có file, module, endpoint hoặc service liên quan.
- Ước lượng vừa với một Sprint; thẻ quá lớn phải tách nhỏ.
- Không trùng với thẻ đang tồn tại.

### 3.2. Từ Selected for Development sang In Progress

Người được giao kéo thẻ sang `In Progress` khi bắt đầu làm việc và phải:

1. Tạo branch từ `develop`.
2. Đặt tên branch theo dạng `feature/<ten-ngan>` hoặc `fix/<ten-ngan>`.
3. Ghi comment kế hoạch ngắn nếu thẻ có nhiều bước.
4. Cập nhật dependency nếu phát hiện điều kiện mới.

Một người không nên có quá **2 thẻ In Progress cùng lúc**, trừ khi đang hỗ trợ xử lý Blocked hoặc review.

### 3.3. Từ In Progress sang Code Review

Chỉ chuyển khi:

- Code, tài liệu hoặc cấu hình đã hoàn thành theo phạm vi thẻ.
- Có test phù hợp với loại thay đổi.
- Đã mở Pull Request vào `develop`.
- PR liên kết đúng issue/thẻ.
- CI đã chạy; lỗi CI phải được xử lý trước khi chờ review.
- Mô tả PR có mục tiêu, file chính, cách kiểm thử và evidence UI nếu có.

### 3.4. Từ Code Review sang Testing

Sau khi reviewer chấp thuận và PR được merge theo quy trình nhóm:

- Cập nhật thẻ sang `Testing`.
- Chạy test tương ứng trên nhánh `develop`.
- Ghi kết quả test, lệnh đã chạy và log quan trọng vào thẻ.
- Nếu test không đạt, chuyển sang `Reopened`, ghi rõ lỗi rồi quay lại `In Progress`.

### 3.5. Từ Testing sang Ready for Acceptance

Thẻ phải có đầy đủ:

- Test result thành công.
- Evidence phù hợp: screenshot, response API, log, video demo hoặc kết quả migration.
- Tài liệu liên quan đã cập nhật nếu thay đổi contract, ERD, kiến trúc hoặc hướng dẫn chạy.
- Không còn lỗi P0/P1 liên quan trực tiếp.

### 3.6. Từ Ready for Acceptance sang Done

Team Lead hoặc owner nghiệm thu kiểm tra acceptance criteria. Chỉ chuyển `Done` khi:

- Tất cả acceptance criteria đạt.
- Definition of Done hoàn tất.
- Không còn comment bắt buộc xử lý trong PR.
- Không chứa secret, token hoặc password.
- Dependency bắt buộc đã được giải quyết hoặc có thẻ liên kết rõ ràng.

Không chuyển `Done` chỉ vì PR đã merge.

---

## 4. Xử lý thẻ Blocked

Khi không thể tiếp tục, chuyển thẻ sang `Blocked` ngay trong ngày và ghi theo mẫu:

```text
Blocked reason:
Blocking issue:
Impact:
Owner cần hỗ trợ:
Expected unblock date:
Next action:
```

Quy tắc xử lý:

- Thẻ Blocked phải có người chịu trách nhiệm gỡ chặn.
- Dependency phải liên kết với thẻ đang chặn nó.
- Nếu bị Blocked quá 1 ngày làm việc, báo trong Daily Stand-up và nhắn Team Lead.
- Khi dependency được giải quyết, cập nhật comment bằng bằng chứng rồi chuyển về `In Progress`.
- Không để thẻ Blocked im lặng đến cuối Sprint.

---

## 5. Cấu trúc thẻ Backlog

### 5.1. Story nghiệp vụ

```markdown
## Mục tiêu
Là [vai trò], tôi muốn [khả năng] để [giá trị].

## Phạm vi
- Bao gồm:
- Không bao gồm:

## Acceptance Criteria
- [ ]
- [ ]
- [ ]

## Dependencies
- Blocks:
- Is blocked by:

## Area / Priority / Sprint
- Area:
- Priority: P0 / P1 / P2
- Sprint:

## Test Scenarios
- Happy path:
- Error path:
- Edge case:

## Evidence khi hoàn thành
- PR:
- Test result:
- Screenshot / log / response:
```

### 5.2. Task kỹ thuật

```markdown
## Mục tiêu

## Deliverable

## Files / Services liên quan

## Acceptance Criteria
- [ ]
- [ ]

## Verification

## Evidence
```

### 5.3. Bug

```markdown
## Summary

## Environment

## Steps to Reproduce
1.
2.
3.

## Expected Result

## Actual Result

## Impact
- Priority: P0 / P1 / P2

## Acceptance Criteria for Fix
- [ ]

## Evidence
- Log:
- Test:
- PR:
```

---

## 6. Quy tắc ưu tiên và đưa thẻ vào Sprint

### P0 — Bắt buộc cho demo

Thẻ ảnh hưởng đến khả năng chạy hoặc demo end-to-end: login, booking, tính giá, fallback AI, thanh toán, nhận/trả xe, Docker/CI và lỗi làm hệ thống không khởi động.

### P1 — Chức năng chính

Thẻ cần cho sản phẩm hoàn chỉnh nhưng có thể xử lý sau P0: filter nâng cao, PDF, chatbot, báo cáo, validation bổ sung và test tích hợp.

### P2 — Hoàn thiện

Cải tiến giao diện, tối ưu trải nghiệm, logging bổ sung, refactor nhỏ và tài liệu không chặn demo.

Thứ tự chọn thẻ vào Sprint:

1. Thẻ P0 đang chặn Sprint Goal.
2. Thẻ P0 có dependency đã sẵn sàng.
3. Thẻ P1 tạo thành luồng nghiệp vụ hoàn chỉnh.
4. Thẻ P2 còn đủ năng lực sau khi đã dự phòng thời gian kiểm thử.

Luôn giữ lại **20% năng lực Sprint** cho sửa lỗi, tích hợp và xử lý dependency.

---

## 7. Definition of Done theo Area

| Area | Điều kiện hoàn thành tối thiểu |
|---|---|
| Backend | Unit/integration test, status code đúng, authorization, error code và API contract |
| Frontend | Kiểm tra trên trình duyệt, loading/error state, route guard và API base URL |
| AI | Schema validation, model loading, timeout/failure behavior, không lộ API key |
| Database | Migration/SQL chạy được, relationship, constraint và index khớp ERD |
| DevOps | Docker Compose, health check, CI, environment và log kiểm tra được |
| Docs | Nội dung khớp code, có ví dụ hoặc evidence kiểm chứng |

Checklist dùng trên mọi thẻ:

```markdown
- [ ] Hoàn thành đúng phạm vi thẻ
- [ ] Có test hoặc verification phù hợp
- [ ] Build/test chạy thành công
- [ ] Không chứa secret trong source
- [ ] Đã mở PR và được review
- [ ] CI xanh
- [ ] Có evidence nghiệm thu
- [ ] Đã cập nhật tài liệu liên quan nếu cần
```

---

## 8. Daily Stand-up và báo cáo tiến độ

Mỗi thành viên trả lời ngắn ba ý:

1. Hôm qua đã hoàn thành thẻ nào hoặc bước nào?
2. Hôm nay sẽ làm gì và dự kiến kéo thẻ sang cột nào?
3. Có trở ngại hoặc dependency nào không?

Khi cập nhật board, comment nên có dạng:

```text
Update: [đã làm gì]
Next: [việc tiếp theo]
Risk/Blocker: [không có hoặc mô tả cụ thể]
Evidence: [PR, commit, test hoặc screenshot]
```

Không dùng comment chung chung như `đang làm`, `gần xong` nếu không kèm kết quả hoặc bước tiếp theo.

---

## 9. Quy trình cuối Sprint

### Sprint Review

- Demo theo Sprint Goal, không demo các thẻ chưa đạt `Ready for Acceptance`.
- Mỗi thẻ demo phải chỉ ra acceptance criteria và evidence.
- Thẻ đạt yêu cầu chuyển `Done`.
- Thẻ chưa xong không được giữ nguyên trạng thái gây hiểu nhầm; chuyển về `Backlog` hoặc đưa sang Sprint kế tiếp sau khi re-estimate.

### Sprint Retrospective

Ghi lại tối thiểu:

| Mục | Nội dung |
|---|---|
| Làm tốt | Điều nên tiếp tục giữ |
| Chưa tốt | Vấn đề gây chậm hoặc tạo lỗi |
| Hành động | Một thay đổi cụ thể cho Sprint kế tiếp |
| Owner | Người chịu trách nhiệm theo dõi hành động |

### Quy tắc carry-over

- Không tự động kéo toàn bộ thẻ chưa xong sang Sprint mới.
- Xác định nguyên nhân: ước lượng sai, dependency, scope creep hay lỗi kỹ thuật.
- Cập nhật lại acceptance criteria, estimate và priority nếu cần.
- Thẻ P0 còn dang dở được ưu tiên trước; thẻ P2 có thể trả về Backlog.

---

## 10. Quy ước liên kết Git

| Đối tượng | Quy ước |
|---|---|
| Branch | `feature/<ten-ngan>`, `fix/<ten-ngan>`, `docs/<ten-ngan>` |
| Commit | `feat:`, `fix:`, `test:`, `docs:`, `chore:` |
| Pull Request | Liên kết issue, mô tả test và evidence |
| Merge | PR vào `develop`, tối thiểu 1 approval, CI xanh, squash merge |
| Release | Chỉ Team Lead xác nhận sau khi hoàn tất kiểm tra tích hợp |

Không push trực tiếp vào `main` hoặc `develop`.
