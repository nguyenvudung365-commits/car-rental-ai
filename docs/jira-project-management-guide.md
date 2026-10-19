# Hướng dẫn quản lý dự án Jira — Car Rental AI

> Phiên bản: 1.0  
> Cập nhật: 17/09/2026  
> Dự án: Hệ thống Cho thuê Xe Thông minh — Tối ưu Giá thuê và Tư vấn Đặt xe bằng AI  
> Repository: https://github.com/nguyenvudung365-commits/car-rental-ai

Tài liệu này hướng dẫn nhóm chuyển kế hoạch triển khai 10 tuần trong `docs/weekly-plan.md` sang Jira và vận hành hằng ngày. Nội dung được thiết kế cho nhóm 5 thành viên, kiến trúc Clean Architecture + Modular Monolith, bốn service chạy bằng Docker Compose và quy trình phát triển thông qua Pull Request.

---

## 1. Mục tiêu khi dùng Jira

Jira phải giúp nhóm trả lời được các câu hỏi sau tại mọi thời điểm:

1. Tuần này nhóm phải hoàn thành những kết quả nào?
2. Mỗi công việc đang do ai phụ trách?
3. Công việc nào bị chặn bởi công việc khác?
4. Issue đã đủ điều kiện bắt đầu chưa?
5. Issue đã đủ điều kiện nghiệm thu chưa?
6. Code, Pull Request, test và tài liệu liên quan nằm ở đâu?
7. Rủi ro hoặc lỗi nào có thể ảnh hưởng đến buổi demo?
8. Phạm vi hiện tại còn bao nhiêu việc P0/P1?

Jira là nơi quản lý **công việc và trạng thái**. GitHub là nơi quản lý **source code, branch, commit, Pull Request và CI**. Hai hệ thống phải liên kết với nhau, không sao chép toàn bộ nội dung code sang Jira.

---

## 2. Thông tin chuẩn của dự án

### 2.1. Tên và mã dự án

Khi tạo Jira project, nên dùng:

- **Project name:** Car Rental AI
- **Project key:** CRA
- **Project type:** Software
- **Template:** Scrum nếu nhóm làm theo sprint 1 tuần; Kanban nếu Jira bản miễn phí không cần sprint. Khuyến nghị dùng Scrum.
- **Project lead:** Nguyễn Vũ Dũng
- **Default assignee:** Unassigned hoặc Project Lead; không tự động giao tất cả issue cho trưởng nhóm.

Sau khi tạo project, URL issue sẽ có dạng:

```text
CRA-1, CRA-2, CRA-3, ...
```

### 2.2. Mô tả dự án để dán vào Project details

```text
Car Rental AI là hệ thống cho thuê xe gồm React + Vite frontend, .NET 10 API theo Clean Architecture và Modular Monolith, FastAPI AI service, và SQL Server. Sản phẩm hỗ trợ đăng ký/đăng nhập, quản lý xe, đặt xe, kiểm tra trùng lịch, thanh toán, nhận/trả xe, hợp đồng PDF, dự đoán giá bằng AI, chatbot và báo cáo doanh thu.

Backend .NET chạy như một monolith có các module Cars, Bookings và Customers. AI là process độc lập giao tiếp qua HTTP đồng bộ, timeout 2 giây, retry tối đa 2 lần và fallback về giá niêm yết khi AI lỗi.
```

### 2.3. Thành viên và vai trò

| Mã | Thành viên | Vai trò Jira | Phạm vi chính |
|---|---|---|---|
| A | Nguyễn Vũ Dũng | Project Lead / Architect | Kiến trúc, ERD, API contract, review, tích hợp cuối, Risk Register, DoD |
| B | Đỗ Anh Tuấn | Backend Developer / Required Reviewer | Auth/JWT, Customer, Cars, Bookings, Payments, Returns, PDF, backend test |
| C | Nguyễn Minh Năng | AI Developer | Dataset, model, FastAPI `/predict-price`, `/chat`, AI test |
| D | Lê Việt Anh | Frontend Developer | React/Vite, auth UI, cars, booking, giá AI, chatbot, báo cáo |
| E | Vũ Duy Tiến | DevOps / Release Manager | Docker Compose, Dockerfile, CI, secrets, logging, staging, release |

### 2.4. Quy tắc gán người

- Mỗi Story, Task hoặc Bug phải có đúng một **Assignee** chịu trách nhiệm chính.
- Có thể có nhiều người trong **Collaborators** hoặc **Participants** nếu Jira instance hỗ trợ; nếu không, ghi trong description.
- Người phụ trách không đồng nghĩa với người review.
- Mọi Pull Request backend phải yêu cầu Đỗ Anh Tuấn review theo quy ước dự án.
- Issue giao cho cả nhóm chỉ dùng cho rehearsal, nghiệm thu và hoạt động cần mọi người tham gia.

---

## 3. Cấu hình Issue Type

Tạo hoặc giữ các issue type sau:

| Issue type | Dùng khi | Ví dụ |
|---|---|---|
| Epic | Nhóm một năng lực lớn hoặc một milestone | M6 — AI Pricing |
| Story | Một năng lực có giá trị sử dụng hoặc kết quả người dùng | Build booking form and history |
| Task | Công việc kỹ thuật, tài liệu hoặc vận hành độc lập | Tạo Docker Compose và env example |
| Bug | Hành vi sai so với contract hoặc acceptance criteria | Booking overlap lọt qua khi có hai request đồng thời |
| Test | Kiểm thử độc lập có đầu ra cụ thể | Test pricing integration |
| Risk | Rủi ro cần owner, mức độ và kế hoạch giảm thiểu | AI timeout làm chậm luồng booking |
| Spike | Nghiên cứu ngắn trước khi quyết định kỹ thuật | Chọn XGBoost hay Random Forest |
| Sub-task | Phần việc nhỏ của một Story/Task | Viết test timeout cho AI client |

Không tạo một Epic cho mỗi thành viên. Epic phải đại diện cho phạm vi sản phẩm hoặc milestone, không đại diện cho người làm.

### 3.1. Khi nào dùng Story và Task

Dùng **Story** khi có người dùng hoặc stakeholder nhận được khả năng mới:

```text
Là Customer, tôi có thể tạo booking cho một xe trong khoảng ngày hợp lệ.
```

Dùng **Task** khi đầu ra là nền tảng kỹ thuật, tài liệu hoặc vận hành:

```text
Tạo Docker Compose cho bốn service.
```

Dùng **Bug** khi có một acceptance criterion không đạt, kể cả lỗi được phát hiện trong test hoặc staging.

---

## 4. Cấu hình Epic và Milestone

Tạo 10 Epic tương ứng với 10 tuần. Dùng Summary theo mẫu thống nhất:

| Epic key nội bộ | Epic name | Mục tiêu |
|---|---|---|
| E1 | W1 — Foundation | Chốt kiến trúc, dữ liệu, API và quy trình |
| E2 | W2 — Skeleton | Dựng solution, service stub, Docker và CI |
| E3 | W3 — Auth | Register, login, JWT và role authorization |
| E4 | W4 — Cars | Quản lý xe, ảnh, filter và phân quyền |
| E5 | W5 — Booking | Đặt xe, overlap, confirm và cancel |
| E6 | W6 — AI Pricing | Model, quote, timeout/retry/fallback và final price |
| E7 | W7 — Rental | Payment, nhận/trả xe, phí trễ và hợp đồng PDF |
| E8 | W8 — Intelligence | Chatbot, search/filter và báo cáo |
| E9 | W9 — Release | Hardening, test, security, staging và release candidate |
| E10 | W10 — Final | Freeze scope, demo, rehearsal và nghiệm thu |

Nếu Jira đã có trường **Fix Version**, tạo thêm các version:

```text
v0.1-foundation
v0.2-skeleton
v0.3-auth
v0.4-cars
v0.5-booking
v0.6-ai-pricing
v0.7-rental
v0.8-intelligence
v0.9-release-candidate
v1.0-final
```

Epic là nhóm công việc. Fix Version là mốc phiên bản/deliverable. Không bắt buộc phải dùng cả hai, nhưng nếu dùng thì phải thống nhất: Epic thể hiện **phạm vi**, Fix Version thể hiện **mốc phát hành**.

---

## 5. Custom Fields cần tạo

Giữ số lượng field vừa phải. Chỉ tạo field phục vụ quyết định hoặc báo cáo.

| Field | Kiểu | Giá trị đề xuất | Bắt buộc |
|---|---|---|---|
| Area | Single select | architecture, backend, database, ai, frontend, devops, testing, docs | Có |
| Priority Class | Single select | P0, P1, P2 | Có |
| Week | Single select | W1 đến W10 | Có |
| Functional Owner | User picker | A, B, C, D, E | Có nếu Assignee là cả nhóm |
| Dependency Status | Single select | None, Waiting, Ready, Blocked | Có |
| Depends On | Issue picker hoặc link | Issue liên quan | Khi có phụ thuộc |
| Acceptance Evidence | URL/text | PR, CI run, screenshot, staging URL, test report | Khi chuyển Done |
| Risk Level | Single select | Low, Medium, High, Critical | Chỉ cho Risk/Bug |
| Environment | Multi select | Local, Docker, CI, Staging, Production-like | Khi cần tái hiện lỗi |
| Demo Critical | Checkbox | Yes/No | Có thể dùng thay P0 |

### 5.1. Ý nghĩa Priority Class

- **P0 — Bắt buộc demo:** thiếu sẽ làm hỏng luồng end-to-end hoặc không thể nghiệm thu.
- **P1 — Chức năng chính:** quan trọng nhưng có thể giảm phạm vi demo nếu thiếu.
- **P2 — Hoàn thiện:** tối ưu giao diện, tiện ích hoặc cải tiến không chặn demo.

Không dùng Priority Class thay cho độ khẩn cấp của Bug. Với Bug, dùng đồng thời:

- Jira Priority: Highest/High/Medium/Low để thể hiện mức độ cần xử lý.
- Priority Class: P0/P1/P2 để thể hiện ảnh hưởng tới phạm vi demo.

---

## 6. Workflow chuẩn

### 6.1. Trạng thái

Dùng workflow sau:

```text
Backlog
  → Selected for Development
  → In Progress
  → Code Review
  → Testing
  → Ready for Acceptance
  → Done
```

Nhánh xử lý trở ngại:

```text
In Progress → Blocked
Blocked → In Progress
Testing → Reopened
Ready for Acceptance → Reopened
```

### 6.2. Ý nghĩa từng trạng thái

| Trạng thái | Điều kiện vào | Việc phải làm |
|---|---|---|
| Backlog | Issue mới tạo, chưa đủ thông tin hoặc chưa ưu tiên | Bổ sung mô tả, owner, priority, dependency |
| Selected for Development | Đã sẵn sàng đưa vào sprint | Có DoR, assignee và acceptance criteria |
| In Progress | Đã bắt đầu code/tài liệu/cấu hình | Tạo branch và liên kết issue với commit/PR |
| Blocked | Không thể tiếp tục vì dependency, môi trường hoặc quyết định chưa có | Ghi rõ lý do, issue chặn và người cần hỗ trợ |
| Code Review | Đã có PR mở | PR trỏ về issue, CI đang chạy hoặc đã xanh |
| Testing | PR đã được review/merge vào develop hoặc có build kiểm thử | Chạy test theo loại issue và ghi evidence |
| Ready for Acceptance | Đã đạt DoD kỹ thuật, chờ owner nghiệm thu | Người phụ trách/Lead kiểm tra acceptance criteria |
| Reopened | Không đạt test hoặc acceptance | Ghi rõ tiêu chí chưa đạt và quay lại người xử lý |
| Done | Đạt DoD, evidence đầy đủ và không còn việc bắt buộc | Không tự chuyển Done chỉ vì đã merge code |

### 6.3. Quy tắc chuyển trạng thái

- Chỉ chuyển sang **In Progress** khi đạt Definition of Ready.
- Chuyển sang **Code Review** phải có URL Pull Request.
- Chuyển sang **Testing** chỉ sau khi reviewer đã chấp thuận hoặc PR đã merge theo quy trình nhóm.
- Chuyển sang **Ready for Acceptance** phải có test result và evidence.
- Chuyển sang **Done** cần một người có quyền nghiệm thu xác nhận. Với issue quan trọng, Nguyễn Vũ Dũng xác nhận tích hợp cuối.
- Issue P0 không được đóng khi CI đỏ, thiếu test, thiếu screenshot UI hoặc thiếu health check tương ứng.
- Không xóa issue vì làm sai; dùng Reopened, Won't Do hoặc Duplicate và ghi lý do.

### 6.4. Quy tắc cho Blocked

Khi chuyển sang `Blocked`, description hoặc comment bắt buộc có:

```text
Blocked reason:
Blocking issue:
Impact:
Owner cần hỗ trợ:
Expected unblock date:
Next action:
```

Ví dụ:

```text
Blocked reason: Chưa có DbContext và migration cho sáu entity.
Blocking issue: CRA-24
Impact: Không thể chạy integration test cho Auth.
Owner cần hỗ trợ: Đỗ Anh Tuấn
Expected unblock date: 2026-09-25
Next action: Hoàn thiện migration đầu tiên và gửi link PR.
```

---

## 7. Definition of Ready và Definition of Done trong Jira

### 7.1. Definition of Ready

Issue chỉ được đưa vào `Selected for Development` khi đã có:

- Summary rõ, bắt đầu bằng động từ hoặc mô tả capability.
- Epic và Week.
- Assignee hoặc Functional Owner.
- Area và Priority Class.
- Mô tả đầu vào, đầu ra.
- Acceptance criteria kiểm chứng được.
- Dependency đã liên kết bằng `blocks`/`is blocked by` hoặc ghi rõ không có.
- File, endpoint, module hoặc service liên quan.
- Quyết định kỹ thuật cần thiết đã có trong tài liệu/ADR nếu đây là issue kiến trúc.
- Không trùng với issue đang tồn tại.

### 7.2. Definition of Done dùng làm checklist

Tạo checklist trong description hoặc dùng checklist plugin:

```markdown
- [ ] Đã hoàn thành code/tài liệu/cấu hình theo mô tả
- [ ] Đã viết hoặc cập nhật test phù hợp
- [ ] Build/test tương ứng chạy thành công
- [ ] Không chứa secret, token hoặc password trong source
- [ ] API contract/ERD/README đã cập nhật nếu có thay đổi
- [ ] Pull Request đã mở và được reviewer chấp thuận
- [ ] CI xanh
- [ ] Frontend đã kiểm tra trên trình duyệt nếu issue có UI
- [ ] Service health endpoint đã kiểm tra nếu issue có service
- [ ] Migration hoặc SQL script đã được kiểm tra nếu issue có database
- [ ] Acceptance evidence đã đính kèm
- [ ] Không còn comment chưa xử lý trong Pull Request
```

### 7.3. DoD theo Area

**Backend:** unit/integration test, status code, authorization, error code và contract.  
**Frontend:** browser check, loading/error state, responsive ở mức cần thiết, route guard và API base URL.  
**AI:** schema test, validation, model loading, timeout/failure behavior và không lộ key.  
**Database:** migration/SQL chạy được, constraint/index/relationship đúng ERD.  
**DevOps:** Docker Compose, health check, CI, environment và log kiểm tra được.  
**Docs:** nội dung khớp code, có ví dụ chạy hoặc acceptance evidence.

---

## 8. Template Description cho từng loại issue

### 8.1. Template Story

```markdown
## Mục tiêu
Là [vai trò], tôi muốn [khả năng] để [giá trị].

## Phạm vi
- Trong phạm vi:
- Ngoài phạm vi:

## Input / Output
- Input:
- Output:
- API/module/file liên quan:

## Acceptance Criteria
- [ ]
- [ ]
- [ ]

## Dependencies
- Blocks:
- Is blocked by:

## Technical Notes

## Test Scenarios
- Happy path:
- Validation/error:
- Edge case:

## Evidence khi hoàn thành
- Pull Request:
- CI:
- Screenshot/log/staging URL:
```

### 8.2. Template Task kỹ thuật

```markdown
## Mục tiêu

## Deliverable
- [ ]

## Files / Services liên quan
- 

## Acceptance Criteria
- [ ]
- [ ]

## Dependencies
- Blocks:
- Is blocked by:

## Verification
- Command/check:
- Expected result:

## Evidence
- PR:
- CI/log:
```

### 8.3. Template Bug

```markdown
## Summary

## Environment
- Environment: Local / Docker / CI / Staging
- Commit hoặc version:
- Browser/OS nếu là frontend:

## Steps to Reproduce
1.
2.
3.

## Expected Result

## Actual Result

## Impact
- Demo impact: P0 / P1 / P2
- Affected area:

## Logs / Screenshot / Request

## Suspected Cause

## Acceptance Criteria for Fix
- [ ] Có test tái hiện lỗi trước khi sửa hoặc giải thích vì sao không thể
- [ ] Lỗi không còn tái hiện
- [ ] Regression test đã thêm/cập nhật
- [ ] CI xanh
```

### 8.4. Template Risk

```markdown
## Risk Statement
Nếu [nguyên nhân], thì [sự kiện] có thể xảy ra, dẫn tới [ảnh hưởng].

## Probability
Low / Medium / High

## Impact
Low / Medium / High / Critical

## Risk Owner

## Mitigation

## Contingency / Fallback

## Trigger
Dấu hiệu cho biết rủi ro đã xảy ra:

## Review Date

## Related Issues
```

### 8.5. Template Spike

```markdown
## Question

## Context

## Options Considered
1.
2.

## Evaluation Criteria

## Recommendation

## Decision / ADR

## Follow-up Issues
```

---

## 9. Backlog Jira theo kế hoạch 10 tuần

Sử dụng toàn bộ 50 issue trong bảng backlog của `docs/weekly-plan.md`. Mỗi issue giữ nguyên mã kế hoạch trong Summary hoặc thêm vào trường `External ID` nếu Jira có field này.

### 9.1. Quy tắc đặt Summary

Dùng mẫu:

```text
[W1-01] Chốt Clean Architecture và Modular Monolith
[W6-04] Implement .NET AI HttpClient
[W10-06] Run final end-to-end acceptance
```

Giữ mã `Wn-xx` để tìm kiếm, đối chiếu tài liệu và import CSV dễ dàng.

### 9.2. Mapping backlog thành Jira

| Mã | Loại | Epic | Owner | Area | Priority |
|---|---|---|---|---|---|
| W1-01 đến W1-05 | Task | E1 W1 Foundation | A/A+B/E | architecture/database/devops | P0 |
| W2-01 đến W2-08 | Task/Test | E2 W2 Skeleton | A/B/C/D/E | backend/database/ai/frontend/devops/testing | P0 |
| W3-01 đến W3-06 | Story/Task/Test | E3 W3 Auth | B/D/A+B | backend/frontend/testing | P0 |
| W4-01 đến W4-06 | Story/Task/Test | E4 W4 Cars | B/D/A+B | backend/frontend/testing | P0/P1 |
| W5-01 đến W5-05 | Story/Task/Test | E5 W5 Booking | B/D/A+B | backend/frontend/testing | P0 |
| W6-01 đến W6-08 | Story/Task/Test | E6 W6 AI Pricing | C/B/D/A+B+C | ai/backend/frontend/testing | P0 |
| W7-01 đến W7-06 | Story/Task/Test | E7 W7 Rental | B/D/A+B | backend/frontend/testing | P0 |
| W8-01 đến W8-06 | Story/Task/Test | E8 W8 Intelligence | C/B/D/A | ai/backend/frontend/testing | P0/P1 |
| W9-01 đến W9-07 | Task/Test/Risk | E9 W9 Release | A/B/C/D/E | testing/frontend/devops/docs | P0/P1 |
| W10-01 đến W10-06 | Task/Test | E10 W10 Final | A/B/C/D/E/Cả nhóm | docs/testing | P0 |

Khi nhập backlog, lấy title, mô tả đầu ra, assignee, milestone, area, priority và dependency trực tiếp từ bảng backlog trong `docs/weekly-plan.md`. Không tự đổi dependency nếu chưa có quyết định của Team Lead.

### 9.3. Mapping dependency

Dùng Jira issue links:

- `blocks`: issue hiện tại chặn issue khác.
- `is blocked by`: issue hiện tại đang chờ issue khác.
- `relates to`: có liên quan nhưng không bắt buộc thứ tự.
- `duplicates`: trùng issue; sau khi xác nhận thì đóng issue trùng.

Ví dụ:

```text
W1-01 blocks W1-02
W1-03 blocks W2-04
W2-04 blocks W3-01
W3-04 blocks W4-02
W4-01 blocks W5-01
W5-01 blocks W6-04
W6-05 blocks W6-06
W9-05 blocks W9-06
W10-05 blocks W10-06
```

---

## 10. Sprint và kế hoạch 10 tuần

### 10.1. Nguyên tắc sprint

Mỗi tuần tương ứng một sprint. Có thể đặt tên:

```text
Sprint 01 — W1 Foundation
Sprint 02 — W2 Skeleton
Sprint 03 — W3 Auth
Sprint 04 — W4 Cars
Sprint 05 — W5 Booking
Sprint 06 — W6 AI Pricing
Sprint 07 — W7 Rental
Sprint 08 — W8 Intelligence
Sprint 09 — W9 Release Candidate
Sprint 10 — W10 Final
```

Nếu lịch thực tế không bắt đầu đúng ngày nêu trong tài liệu, giữ số sprint và đổi ngày bắt đầu/kết thúc theo lịch nhóm. Không đổi mã W1–W10 trong Summary vì chúng là mã liên kết với kế hoạch gốc.

### 10.2. Sprint planning

Trước khi bắt đầu mỗi sprint:

1. Lọc issue thuộc Week tương ứng.
2. Xác nhận issue P0 trước, sau đó mới chọn P1/P2.
3. Kiểm tra tất cả `is blocked by`.
4. Xác nhận mỗi issue có Assignee.
5. Kiểm tra tổng tải của từng thành viên.
6. Đưa issue đủ DoR vào `Selected for Development`.
7. Ghi Sprint Goal trong Jira.
8. Không đưa issue thiếu acceptance criteria vào sprint chỉ để tăng số lượng.

Mẫu Sprint Goal:

```text
Sprint Goal — W5 Booking
Hoàn thiện luồng tạo booking, kiểm tra overlap theo khoảng nửa mở [StartDate; EndDate), danh sách/confirm/cancel và test các trường hợp biên để sẵn sàng tích hợp giá AI ở W6.
```

### 10.3. Daily stand-up trong Jira

Mỗi thành viên cập nhật comment hoặc Slack/meeting theo ba dòng:

```text
Done since last update:
Next:
Blockers:
```

Không cần viết nhật ký dài. Nếu có blocker, phải cập nhật trạng thái Jira thành `Blocked` và liên kết issue chặn.

### 10.4. Sprint review

Cuối tuần, chỉ demo những issue ở `Ready for Acceptance` hoặc `Done`. Với mỗi issue P0:

- Mở acceptance criteria.
- Chạy hoặc trình bày verification.
- Đính kèm evidence.
- Ghi issue phát sinh nếu không đạt.

### 10.5. Sprint retrospective

Tạo một Task riêng cho retrospective của mỗi sprint hoặc ghi vào page liên kết với Epic. Nội dung tối thiểu:

```text
Keep:
Stop:
Start:
Một hành động cải thiện cho sprint kế tiếp:
Owner:
```

---

## 11. Liên kết Jira với GitHub

### 11.1. Quy ước branch

Branch phải chứa Jira key:

```text
feature/CRA-123-auth-jwt
feature/CRA-145-booking-overlap
fix/CRA-188-ai-timeout-fallback
chore/CRA-201-production-dockerfile
```

Vẫn giữ quy ước loại branch của dự án; chỉ bổ sung Jira key sau loại branch.

### 11.2. Commit message

Mẫu đề xuất:

```text
feat(CRA-123): implement JWT login
fix(CRA-145): reject overlapping booking
 test(CRA-188): cover AI timeout fallback
 docs(CRA-201): update staging runbook
```

Không đưa secret, token, password hoặc dữ liệu khách hàng thật vào commit message.

### 11.3. Pull Request

Tiêu đề PR:

```text
feat: implement JWT login (CRA-123)
fix: reject overlapping bookings (CRA-145)
docs: update Jira operating guide (CRA-220)
```

Description PR phải có:

```markdown
## Mục tiêu

## Jira
CRA-123

## Thay đổi chính
- 

## Kiểm thử
- [ ] dotnet build
- [ ] dotnet test
- [ ] AI test / browser test / Docker smoke test nếu liên quan

## Acceptance evidence
- 

## Checklist
- [ ] Không có secret
- [ ] API contract/ERD/docs đã cập nhật nếu cần
- [ ] CI xanh
- [ ] Reviewer đã được yêu cầu
```

Dùng câu lệnh `Closes CRA-123` chỉ khi merge PR thực sự hoàn tất công việc và Jira được phép tự động đóng issue. Nếu PR chỉ hoàn thành một phần, dùng `Relates to CRA-123` hoặc ghi key mà không dùng `Closes`.

### 11.4. Trạng thái Jira từ GitHub

Nếu tích hợp Jira–GitHub đã bật, cấu hình tối thiểu:

- Branch có Jira key → Jira hiển thị branch.
- Commit có Jira key → Jira hiển thị commit.
- Pull Request có Jira key → Jira hiển thị PR và trạng thái CI.
- Merge PR không tự động đồng nghĩa với Done; vẫn cần Testing/Acceptance theo DoD.

---

## 12. Quy trình xử lý từng loại công việc

### 12.1. Feature mới

```text
Backlog
→ Refinement
→ Selected for Development
→ In Progress
→ Code Review
→ Testing
→ Ready for Acceptance
→ Done
```

Các bước:

1. Tạo Story/Task thuộc Epic đúng tuần.
2. Điền Area, Priority, Assignee, acceptance criteria.
3. Liên kết dependency.
4. Tạo branch từ `develop`.
5. Cập nhật Jira key vào branch và commit.
6. Mở PR vào `develop`.
7. Đỗ Anh Tuấn review backend; người phụ trách domain review phần tương ứng.
8. CI xanh, merge theo quy tắc repository.
9. Chạy test hoặc browser/Docker verification.
10. Gắn evidence và chuyển Done sau nghiệm thu.

### 12.2. Bug trong lúc phát triển

- Bug làm hỏng P0 hoặc luồng demo: Priority Highest, Priority Class P0.
- Bug làm sai một chức năng chính nhưng có workaround: High, P1.
- Bug giao diện nhỏ hoặc cải tiến: Medium/Low, P2.
- Bug phải có bước tái hiện, expected, actual và môi trường.
- Không sửa trực tiếp trên issue gốc bằng cách che acceptance criteria; tạo Bug liên kết `is caused by` hoặc `relates to`.

### 12.3. Thay đổi API/ERD/kiến trúc

1. Tạo Task hoặc Spike mô tả thay đổi.
2. Liên kết issue đang bị ảnh hưởng.
3. Cập nhật ADR nếu thay đổi quyết định kiến trúc.
4. Cập nhật API contract/ERD trước hoặc cùng PR implementation.
5. Team Lead review tác động dependency.
6. Chỉ sau khi được chấp thuận mới chuyển các issue liên quan sang In Progress.

### 12.4. Incident ở staging

Tạo Bug với:

- Environment = Staging.
- Risk Level theo ảnh hưởng.
- URL/log/request correlation ID nếu có.
- Issue liên quan tới release hoặc deployment.
- Nếu cần rollback, liên kết issue với deployment task và ghi người quyết định.

---

## 13. JQL dùng hằng ngày

Thay `CRA` bằng project key thực tế nếu nhóm chọn key khác.

### 13.1. Toàn bộ việc P0 chưa hoàn thành

```jql
project = CRA
AND "Priority Class" = "P0"
AND statusCategory != Done
ORDER BY priority DESC, created ASC
```

### 13.2. Issue đang bị chặn

```jql
project = CRA
AND status = Blocked
ORDER BY priority DESC, updated ASC
```

### 13.3. Việc của tôi đang làm

```jql
project = CRA
AND assignee = currentUser()
AND statusCategory = "In Progress"
ORDER BY priority DESC, updated DESC
```

### 13.4. Issue thiếu người phụ trách

```jql
project = CRA
AND assignee IS EMPTY
AND statusCategory != Done
ORDER BY priority DESC, created ASC
```

### 13.5. Issue thiếu acceptance criteria

Nếu Jira có custom field hoặc dùng text search:

```jql
project = CRA
AND statusCategory != Done
AND description !~ "Acceptance Criteria"
```

JQL text search không hoàn toàn chính xác; vẫn cần kiểm tra thủ công trong refinement.

### 13.6. Bug chưa xử lý

```jql
project = CRA
AND issuetype = Bug
AND statusCategory != Done
ORDER BY priority DESC, created ASC
```

### 13.7. Việc cần nghiệm thu

```jql
project = CRA
AND status IN ("Testing", "Ready for Acceptance")
ORDER BY "Priority Class" ASC, updated ASC
```

### 13.8. Việc W6 AI Pricing

```jql
project = CRA
AND "Week" = W6
ORDER BY status ASC, priority DESC
```

### 13.9. Việc quá hạn

```jql
project = CRA
AND duedate < startOfDay()
AND statusCategory != Done
ORDER BY duedate ASC
```

### 13.10. Việc cập nhật lâu không có hoạt động

```jql
project = CRA
AND statusCategory != Done
AND updated < -3d
ORDER BY updated ASC
```

### 13.11. P0 của từng người

```jql
project = CRA
AND "Priority Class" = "P0"
AND assignee = "account-id-or-user"
AND statusCategory != Done
ORDER BY duedate ASC
```

Thay `account-id-or-user` bằng định danh mà Jira instance chấp nhận; không đoán account ID.

---

## 14. Board cần tạo

### 14.1. Board chính: Car Rental AI Delivery

Filter:

```jql
project = CRA ORDER BY Rank ASC
```

Cột:

```text
Backlog | Selected for Development | In Progress | Blocked | Code Review | Testing | Ready for Acceptance | Done
```

Quick filters:

```jql
P0: "Priority Class" = P0
P1: "Priority Class" = P1
My work: assignee = currentUser()
Blocked: status = Blocked
Backend: Area = backend
Frontend: Area = frontend
AI: Area = ai
Release: "Week" IN (W9, W10)
```

### 14.2. Board release/demo

Dùng cho W9–W10:

```jql
project = CRA
AND "Week" IN (W9, W10)
ORDER BY "Priority Class" ASC, priority DESC, Rank ASC
```

Board này giúp Team Lead nhìn riêng các việc hardening, staging, rehearsal và nghiệm thu cuối mà không bị lẫn backlog chức năng cũ.

### 14.3. Swimlane

Khuyến nghị swimlane theo:

1. Blocked/P0 trước.
2. Epic sau.

Không chia swimlane theo thành viên vì sẽ khó nhìn luồng dependency và dễ tạo silo.

---

## 15. Automation nên cấu hình

Chỉ bật automation sau khi thử trên một vài issue. Automation không được tự động đóng issue khi chưa có evidence.

### 15.1. Nhắc thiếu assignee

- Trigger: issue created hoặc scheduled hằng ngày.
- Condition: status khác Done và assignee trống.
- Action: comment hoặc thông báo cho Project Lead.

### 15.2. Nhắc issue Blocked

- Trigger: issue chuyển sang Blocked.
- Action: thông báo cho Assignee, Project Lead và người phụ trách issue chặn.
- Nội dung: key, lý do, issue chặn, expected unblock date.

### 15.3. Tự động gắn Sprint/Epic theo Week

Chỉ dùng nếu Jira hỗ trợ rule ổn định. Nếu không, cập nhật thủ công trong sprint planning để tránh gán sai issue.

### 15.4. Nhắc issue gần quá hạn

- Trigger: 2 ngày trước due date.
- Condition: statusCategory khác Done.
- Action: thông báo Assignee và comment yêu cầu cập nhật blocker.

### 15.5. Đồng bộ PR

Khi PR được mở:

```text
In Progress → Code Review
```

Khi PR được merge:

```text
Code Review → Testing
```

Không tự động chuyển Done. Nếu CI fail, giữ ở Testing hoặc chuyển Reopened tùy quy trình.

### 15.6. Chặn đóng issue nếu thiếu evidence

Nếu Jira workflow hỗ trợ validator, khi chuyển `Ready for Acceptance → Done`, yêu cầu:

- Acceptance Evidence không rỗng.
- Assignee không rỗng.
- Issue không có trạng thái Blocked.
- Với P0, có CI/evidence phù hợp.

---

## 16. Permission và trách nhiệm

### 16.1. Nhóm quyền

| Quyền | Project Lead | Developer | Reviewer | Release Manager |
|---|---:|---:|---:|---:|
| Tạo issue | Có | Có | Có | Có |
| Sửa mô tả/field | Có | Có issue mình phụ trách | Có | Có |
| Chuyển workflow | Có | Có trong phần việc mình làm | Có thể nghiệm thu theo phân công | Có |
| Xóa issue | Có, hạn chế | Không | Không | Không |
| Đóng issue P0 | Có | Không tự đóng | Có thể xác nhận test | Có thể xác nhận release |
| Quản lý sprint | Có | Không | Không | Có |
| Cấu hình workflow/field | Có | Không | Không | Có nếu được ủy quyền |
| Quản lý release/version | Có | Không | Không | Có |

Không cấp quyền xóa issue cho tất cả thành viên. Khi issue tạo nhầm, dùng `Duplicate`, `Won't Do` hoặc comment giải thích thay vì xóa lịch sử.

### 16.2. Quyền hiển thị

- Không đưa secret, JWT, Gemini key, password DB hoặc dữ liệu cá nhân thật vào Jira.
- Không đưa token trong screenshot/log.
- Dùng dữ liệu mẫu khi đính kèm request/response.
- Nếu phải ghi lỗi có dữ liệu nhạy cảm, che/mask trước khi upload.

---

## 17. Dashboard cho Team Lead

Tạo dashboard `CRA — Project Control`. Các gadget nên có:

1. **Filter Results — P0 chưa Done**
2. **Two Dimensional Statistics — Assignee x Status**
3. **Created vs Resolved Chart**
4. **Pie Chart — Issues by Area**
5. **Filter Results — Blocked Issues**
6. **Sprint Burndown**
7. **Roadmap/Release Progress**
8. **Recently Updated**
9. **Open Bugs by Priority**

### 17.1. Chỉ số cần theo dõi mỗi tuần

- Số P0 còn lại.
- Số issue Blocked và tuổi blocker.
- Số Bug Highest/High chưa xử lý.
- Số issue đã Done so với Sprint Goal.
- Số issue thiếu Assignee hoặc thiếu acceptance criteria.
- Số PR mở chưa review.
- CI pass/fail trên các PR liên quan.
- Tỷ lệ hoàn thành từng Epic.

Không dùng số story point để kết luận một thành viên làm nhiều hay ít. Story point chỉ phục vụ ước lượng capacity và dự báo sprint.

---

## 18. Quy trình họp và cập nhật Jira

### Trước sprint planning

- Project Lead rà backlog theo P0/P1.
- Kiểm tra issue trùng, issue cũ và dependency.
- Cập nhật Risk Register.
- Xác định mục tiêu sprint.

### Trong sprint planning

- Chọn issue theo thứ tự dependency.
- Kiểm tra tải từng người.
- Xác nhận acceptance criteria.
- Gán due date hợp lý.
- Không đưa toàn bộ backlog vào sprint chỉ để Jira hiển thị nhiều việc.

### Hằng ngày

Mỗi thành viên:

- Cập nhật status trước hoặc sau stand-up.
- Ghi blocker ngay khi phát sinh.
- Liên kết branch/PR.
- Không để issue In Progress quá lâu mà không có comment hoặc commit liên quan.

### Cuối sprint

- Demo issue Done/Ready for Acceptance.
- Đóng hoặc carry-over issue có lý do.
- Không chuyển việc chưa đạt DoD sang Done để làm đẹp burndown.
- Tạo Bug cho mọi lỗi phát hiện trong demo.
- Ghi retrospective action item thành issue nếu cần theo dõi.

### Hằng tuần — trách nhiệm của Team Lead

1. Kiểm tra P0/P1 còn mở.
2. Kiểm tra dependency bị chặn.
3. Kiểm tra PR và CI.
4. Kiểm tra thay đổi API/ERD có cập nhật tài liệu.
5. Kiểm tra các issue W9/W10 có đủ evidence.
6. Cập nhật Risk Register và quyết định phạm vi nếu trễ tiến độ.

---

## 19. Quản lý dependency theo từng giai đoạn

### Foundation và Skeleton

Các dependency quan trọng:

```text
W1-01 → W1-02, W2-01
W1-03 → W1-04, W2-02, W2-04
W1-04 → W2-03, W2-05, W2-06
W2-01/W2-02 → W2-04
W2-01/W2-03 → W2-08
```

### Auth và Cars

```text
W2-04 → W3-01
W3-01 → W3-02 → W3-03 → W3-04
W3-03 → W3-05
W3-04 → W4-02
W4-01 → W4-04, W5-01
W4-02 → W4-03, W4-05, W4-06
```

### Booking và AI Pricing

```text
W5-01 → W5-02 → W5-03
W5-02/W5-03 → W5-05
W5-01 → W6-04
W6-01 → W6-02 → W6-03
W6-04 → W6-05 → W6-06 → W6-07
W6-03/W6-06 → W6-08
```

### Rental, Intelligence và Release

```text
W5-03 → W7-01 → W7-02 → W7-03
W5-03 → W7-04
W7-01/W7-03/W7-04 → W7-05/W7-06
W7-01 → W8-03
W8-01/W8-03 → W8-04/W8-05/W8-06
W9-01/W9-04 → W9-05 → W9-06
W9-01/W9-05 → W9-07
W9-06/W9-07 → W10-01 → W10-02/W10-03/W10-04 → W10-05 → W10-06
```

Nếu dependency làm một sprint không thể hoàn thành, không tự gỡ liên kết để issue trông không bị chặn. Đánh dấu Blocked, báo Team Lead và quyết định điều chỉnh thứ tự hoặc phạm vi.

---

## 20. Risk Register trong Jira

Tạo một issue type `Risk` hoặc một Epic/label riêng `risk-register`. Mỗi Risk phải có:

- Risk statement.
- Probability.
- Impact.
- Risk owner.
- Mitigation.
- Contingency.
- Trigger.
- Review date.
- Issue liên quan.

### Rủi ro ban đầu nên tạo

| Risk | Owner | Mức đề xuất | Mitigation |
|---|---|---|---|
| AI service timeout làm gián đoạn booking | B/C/E | High | Timeout 2 giây, retry 2, fallback base price, test tắt container |
| Booking overlap khi có request đồng thời | B/A | Critical | Enforce ở backend, transaction/concurrency test, test boundary |
| Secret JWT/Gemini/DB bị commit | E/A | Critical | `.env.example`, secret scan, review PR, không log secret |
| Payment model không khớp quy định 1 booking nhiều payment | A/B | High | Chốt ERD/API contract trước implementation, test nhiều payment |
| PDF library không chạy trong Linux container | B/E | Medium | Kiểm tra sớm ở W7, test Docker image production |
| Frontend/API base URL sai giữa local/Docker/staging | D/E | High | Dùng environment, smoke test từng môi trường |
| CI fail do chạy sai solution/project | E/A | High | Chốt command CI, chạy local giống pipeline |
| Scope W8/W10 trễ làm ảnh hưởng demo | A/cả nhóm | High | Ưu tiên P0, freeze scope, chuẩn bị fallback demo |

---

## 21. Quy tắc release và nghiệm thu

### Release Candidate — W9

Không chuyển release candidate sang `Ready for Acceptance` nếu thiếu:

- CI restore/build/test xanh.
- Dockerfile production cho bốn service.
- Staging hoặc môi trường tương đương chạy được.
- Health check API và AI trả 200.
- Secret scan không phát hiện secret.
- Test Auth, Cars, Booking overlap, pricing/fallback, payment và return.
- Risk Register có owner và mitigation.
- Smoke test có kết quả.

### Final — W10

Epic W10 chỉ Done khi:

- P0 trước demo đã xử lý hoặc có quyết định phạm vi được ghi nhận.
- Có tag/release cuối.
- README và demo guide khớp code.
- Có slide kiến trúc, ERD, AI, DevOps và phân công.
- Đã chạy hai lần rehearsal.
- Luồng end-to-end chạy được:

```text
Đăng ký/đăng nhập
→ xem và lọc xe
→ xem giá AI hoặc fallback
→ tạo booking
→ kiểm tra overlap
→ xác nhận booking
→ tạo payment cọc
→ nhận xe
→ trả xe
→ tính phí trễ/hư hỏng
→ thanh toán phần còn lại
→ tải hợp đồng PDF
→ Admin xem báo cáo
```

### Kịch bản fallback bắt buộc

Trong demo phải có thể trình bày cả hai trường hợp:

1. AI hoạt động và trả giá dự đoán.
2. AI tắt hoặc timeout; .NET vẫn trả HTTP 200, giá niêm yết và `isFallback: true`.

---

## 22. Import backlog bằng CSV

Nếu nhập thủ công quá lâu, tạo CSV với các cột tối thiểu:

```text
Issue Type,Summary,Description,Assignee,Priority,Epic Link,Labels,Area,Priority Class,Week,Depends On
```

Quy tắc khi import:

- Không import secret hoặc token.
- Kiểm tra tên user/email đúng với Jira trước khi import.
- Import Epic trước, sau đó import issue con.
- Import dependency sau khi các issue đã có key Jira; mã `W1-01` không tự là Jira key.
- Sau import phải kiểm tra ngẫu nhiên tối thiểu 10 issue ở các tuần W1, W5, W6, W9 và W10.
- Kiểm tra lại Unicode tiếng Việt, line break trong Description và mapping Priority.

Có thể dùng mã `W1-01` trong Summary/External ID trong lúc import, sau đó liên kết dependency bằng Jira key thật như `CRA-101`.

---

## 23. Checklist triển khai Jira lần đầu

### Cấu hình project

- [ ] Tạo project `Car Rental AI`, key `CRA`.
- [ ] Chọn template Scrum.
- [ ] Đặt Nguyễn Vũ Dũng làm Project Lead.
- [ ] Thêm đủ 5 thành viên và kiểm tra account.
- [ ] Cấu hình issue types.
- [ ] Tạo 10 Epic W1–W10.
- [ ] Tạo Fix Version nếu nhóm dùng release tracking.
- [ ] Tạo custom fields tối thiểu.
- [ ] Cấu hình workflow.
- [ ] Tạo board chính và board release.
- [ ] Tạo Quick Filters.
- [ ] Tạo dashboard Team Lead.

### Nhập backlog

- [ ] Nhập W1–W10 theo thứ tự.
- [ ] Gán Epic, owner, Area, Priority Class và Week.
- [ ] Gắn dependency `blocks/is blocked by`.
- [ ] Kiểm tra issue P0 trước.
- [ ] Kiểm tra các issue dùng chung một dependency.
- [ ] Tạo các Risk ban đầu.
- [ ] Tạo Sprint 01 đến Sprint 10.

### Tích hợp GitHub

- [ ] Kết nối repository GitHub đúng repository của nhóm.
- [ ] Kiểm tra branch có Jira key hiển thị trong issue.
- [ ] Kiểm tra Pull Request hiển thị trong Jira.
- [ ] Kiểm tra CI status hiển thị.
- [ ] Thống nhất commit/PR naming.
- [ ] Kiểm tra rule không tự đóng issue sai thời điểm.

### Kiểm tra vận hành

- [ ] Tạo thử một Task.
- [ ] Tạo branch có Jira key.
- [ ] Mở PR thử nghiệm.
- [ ] Cập nhật Code Review → Testing.
- [ ] Đính kèm evidence.
- [ ] Chuyển Done thử nghiệm.
- [ ] Kiểm tra dashboard/JQL.
- [ ] Xóa hoặc đóng issue thử nghiệm theo quy định, không xóa lịch sử cần thiết.

---

## 24. Quy tắc ngắn gọn bắt buộc cho cả nhóm

1. Không có Assignee thì chưa được bắt đầu.
2. Không có Acceptance Criteria thì chưa được đưa vào sprint.
3. Không có Jira key trong branch/PR thì chưa coi là liên kết hoàn chỉnh.
4. Không chuyển Done chỉ vì code đã merge.
5. P0 phải có evidence và CI xanh.
6. Blocker phải có issue liên quan và owner xử lý.
7. Thay đổi API/ERD/kiến trúc phải cập nhật tài liệu.
8. Không ghi secret hoặc dữ liệu thật vào Jira, GitHub hay log đính kèm.
9. Issue bị trễ phải cập nhật trạng thái và lý do, không để im lặng.
10. Khi phải cắt scope, giữ P0 trước, ghi quyết định vào Jira và cập nhật kế hoạch.

---

## 25. Tài liệu tham chiếu trong repository

- [Kế hoạch triển khai 10 tuần](weekly-plan.md)
- [Kiến trúc hệ thống](architecture.md)
- [API contract](api-contract.md)
- [Quyết định kiến trúc và phân rã chức năng](decisions.md)
- [README dự án](../README.md)
