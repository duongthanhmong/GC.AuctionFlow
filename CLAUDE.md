# GCAE DOCUMENT AUTHORITY

Trước mọi task, phải đọc các tài liệu sau:

1. `docs/spec/GC_AuctionFlow_Engine_v1.2_Post_Cross_Review_Final_Spec_VI.md`
2. `docs/spec/GC_AuctionFlow_Engine_v1.3_Knowledge_Grounded_Spec_VI.md`
3. `docs/implementation/IMPLEMENTATION_STATUS.md`
4. `docs/spec/GCAE_IMPLEMENTATION_BIBLE_SINGLE_SOURCE_FINAL.md`

---

## Vai trò của từng tài liệu

### v1.2 — Architecture Authority

`docs/spec/GC_AuctionFlow_Engine_v1.2_Post_Cross_Review_Final_Spec_VI.md`

Quyết định:

- kiến trúc tổng thể;
- một DLL, một indicator, nhiều engine;
- module boundaries;
- canonical data flow;
- roadmap gốc;
- governance;
- feature flags;
- phạm vi sản phẩm;
- Production Core và Research Lab.

### v1.3 — Domain Semantics Authority

`docs/spec/GC_AuctionFlow_Engine_v1.3_Knowledge_Grounded_Spec_VI.md`

Quyết định:

- ngữ nghĩa AMT và Order Flow;
- tiêu chí phân biệt các khái niệm gần nhau;
- measurement contracts;
- trường nullable và tiêu chí unavailable;
- calibration gates;
- anti-pattern guards;
- điều kiện cần của FAR, AAC, Signal Maturity và Invalidation;
- những suy luận hệ thống bị cấm thực hiện.

v1.3 **bổ sung, không thay thế** v1.2.

### Implementation Status — Progress Authority

`docs/implementation/IMPLEMENTATION_STATUS.md`

Là nguồn sự thật duy nhất về tiến độ hiện tại:

- Current Phase;
- LOCKED;
- CODE/TEST PASS;
- LIVE ACCEPTANCE PENDING;
- NOT STARTED;
- BLOCKED;
- NOT CALIBRATED;
- schema hiện tại;
- policy hiện tại;
- test count;
- DLL hash;
- phase được phép triển khai tiếp theo.

Không dùng trạng thái tiến độ ghi trong v1.2, v1.3, Implementation Bible hoặc prompt cũ
để thay thế `IMPLEMENTATION_STATUS.md`.

### Implementation Bible — Implementation Guidance

`docs/spec/GCAE_IMPLEMENTATION_BIBLE_SINGLE_SOURCE_FINAL.md`

Dùng như tài liệu triển khai tổng hợp để:

- định hướng cách tổ chức source code;
- hiểu module contracts;
- hiểu snapshot, identity, revision và lifecycle;
- hiểu threading, recorder, persistence và shutdown;
- hiểu test contract;
- hiểu quy trình PLAN → AUDIT → IMPLEMENT → VERIFY → REVIEW → DEPLOY;
- tra cứu các invariant xuyên suốt dự án.

Implementation Bible **không được vượt quyền**:

- source code và tests của phase đã LOCKED;
- `IMPLEMENTATION_STATUS.md` mới nhất;
- ngữ nghĩa chuẩn tắc của v1.3;
- kiến trúc chuẩn tắc của v1.2.

> **Cảnh báo đã biết:** frontmatter của Implementation Bible ghi
> `current_phase: "Phase 1G CODE/TEST PASS"`. Đây là trạng thái **đã cũ**.
> Luôn dùng `IMPLEMENTATION_STATUS.md` mới nhất.

---

## Thứ tự ưu tiên khi xung đột

1. Runtime behavior và automated tests của phase đã `LOCKED`
2. `IMPLEMENTATION_STATUS.md` mới nhất
3. Milestone hiện tại được operator giao rõ ràng
4. **v1.3** đối với ngữ nghĩa miền, đo lường, calibration và guard
5. **v1.2** đối với kiến trúc, boundaries, roadmap và governance
6. **Implementation Bible** đối với hướng dẫn triển khai tổng hợp

Không được âm thầm tự chọn khi có xung đột.

Phải dừng và báo:

```text
CONFLICT DETECTED

- Yêu cầu hiện tại:
- Tài liệu hoặc code xung đột:
- Invariant có nguy cơ bị phá:
- Phương án tối thiểu an toàn:
- Quyết định cần operator xác nhận:
```

---

## Quy tắc bắt buộc

- Không coi v1.3 là tài liệu thay thế v1.2.
- Không coi Implementation Bible là nguồn tiến độ mới nhất.
- Không triển khai tất cả nội dung `[N]` trong v1.3 cùng lúc.
- `[N]` chỉ bắt buộc khi module hoặc phase tương ứng đã được authorized.
- Không mở khóa bất kỳ trạng thái `[C]` nào khi chưa vượt Calibration Ledger.
- Không tự đặt threshold hoặc magic number.
- Không đổi tên enum, policy hoặc schema đã build chỉ để khớp văn bản.
- Không sửa behavior của phase đã LOCKED nếu milestone không bắt buộc.
- Không tạo đường normalized trade thứ hai.
- Không bịa Bid/Ask khi aggressor classification không khả dụng.
- Không biến dữ liệu unavailable thành số 0.
- Không dựng lịch sử Episode hoặc Order Flow từ candle hoặc chart visual.
- Không dùng MBO khi capability hiện tại là BLOCKED.
- Không tạo module GEX, enum GEX, snapshot GEX hoặc GPS row GEX.
- Không tạo Buy/Sell automation trong baseline hiện tại.
- Không gọi CODE/TEST PASS là FINAL PASS khi live acceptance còn pending.
- Không commit hoặc tag closeout trước khi vượt đúng gate.

### Build & Deploy invariants

- `D-P0-02-002` — Chỉ build **một** artifact: `GC.AuctionFlow.dll`.
  **KHÔNG BAO GIỜ** build `Oac.Core` hoặc `Oac.Atas`.
- `D-P0-02A-003` — Deploy đúng **một** nơi:
  `C:\Users\LOQ\AppData\Roaming\ATAS\Indicators\GC.AuctionFlow.dll`
  **KHÔNG** dual-install sang `Documents`.
- Xác minh SHA-256 `source == deployed` tại mỗi phase closeout.

---

## Quy trình trước mỗi task

1. Đọc toàn bộ `IMPLEMENTATION_STATUS.md`.
2. Xác định Current Phase.
3. Liệt kê các phase LOCKED.
4. Liệt kê các state NOT CALIBRATED.
5. Liệt kê các capability BLOCKED hoặc UNAVAILABLE.
6. Đọc phần kiến trúc liên quan trong v1.2.
7. Đọc phần ngữ nghĩa và guard liên quan trong v1.3.
8. Đọc contract tương ứng trong Implementation Bible.
9. Audit source code và tests hiện tại.
10. Chỉ sau đó mới lập PLAN.

---

## Quy trình thực hiện

```text
PLAN → AUDIT → IMPLEMENT → VERIFY → REVIEW → DEPLOY
```

### PLAN

Phải nêu:

- milestone được phép thực hiện;
- file dự kiến thay đổi;
- file **không** được phép thay đổi;
- invariant phải giữ;
- schema hoặc policy có cần bump không;
- targeted test plan;
- full regression plan;
- live gate cần thiết.

### IMPLEMENT

- Chỉ sửa tối thiểu cần thiết.
- Không mở rộng sang phase kế tiếp.
- Không thêm placeholder cho tương lai nếu milestone không yêu cầu.
- Không refactor diện rộng chỉ vì code có thể đẹp hơn.
- Giữ module ownership và immutable snapshot boundaries.

### VERIFY

Phải chạy:

- targeted tests;
- full regression suite;
- build với 0 error;
- build với 0 warning;
- duplicate tests;
- stale và out-of-order rejection tests;
- revision tests;
- reset tests;
- disable/re-enable tests;
- schema compatibility tests;
- guard tests từ v1.3;
- regression tests cho mọi phase LOCKED bị chạm tới.

### REVIEW

Kiểm tra:

- scope creep;
- semantic leakage;
- fabricated data;
- unauthorized classifier;
- unauthorized calibrated state;
- MBO leakage;
- mutation của snapshot khác;
- second trade-normalization path;
- overlay hoặc alert ngoài phạm vi;
- default settings;
- schema/version mismatch;
- status documentation mismatch.

### DEPLOY

- Chỉ deploy đúng một `GC.AuctionFlow.dll`.
- Xác minh SHA-256 source bằng deployed.
- Không dual-install.
- Cập nhật `IMPLEMENTATION_STATUS.md`.
- Chỉ commit/tag khi milestone đã vượt toàn bộ gate được yêu cầu.
