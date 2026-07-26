# GCAE DOCUMENT AUTHORITY

Trước mọi task, phải đọc ba tài liệu:

1. `docs/spec/GC_AuctionFlow_Engine_v1.2_Post_Cross_Review_Final_Spec_VI.md`
2. `docs/spec/GC_AuctionFlow_Engine_v1.3_Knowledge_Grounded_Spec_VI.md`
3. `docs/implementation/IMPLEMENTATION_STATUS.md`

## Vai trò

- **v1.2** quyết định kiến trúc, module boundaries, một-DLL, roadmap,
  governance, feature flags và phạm vi hệ thống.
- **v1.3** quyết định ngữ nghĩa AMT/Order Flow, discriminator,
  measurement contracts, calibration gates và anti-pattern guards.
- **IMPLEMENTATION_STATUS.md** quyết định trạng thái triển khai hiện tại
  và phase nào được phép thực hiện.
- **Source code cùng tests đã khóa** quyết định tên enum, policy version,
  schema hiện hữu và runtime behavior đã được chứng minh.

## Thứ tự ưu tiên

Khi xung đột:

1. Invariant và behavior của phase LOCKED trong source/tests
2. `IMPLEMENTATION_STATUS.md` mới nhất
3. Milestone được operator giao rõ ràng
4. v1.3 đối với ngữ nghĩa miền
5. v1.2 đối với kiến trúc và roadmap

Không dùng một tài liệu để vượt quyền tài liệu khác.

## Các lệnh cấm

- Không coi v1.3 là tài liệu thay thế v1.2.
- Không dùng trạng thái tiến độ ghi trong v1.3 thay cho IMPLEMENTATION_STATUS.
- Không triển khai mọi mục `[N]` cùng lúc.
- `[N]` chỉ có nghĩa là bắt buộc khi module tương ứng được authorized.
- Không mở khóa bất kỳ trạng thái `[C]` nào.
- Không tự đặt threshold.
- Không đổi tên enum hoặc policy đã build chỉ để khớp văn bản.
- Không tạo GEX module, field, enum hoặc GPS row.
- Không dùng Implementation Bible đã archive làm nguồn có thẩm quyền.
- Không sửa phase LOCKED nếu milestone không bắt buộc.
- Không gọi CODE/TEST PASS là FINAL PASS khi live acceptance còn pending.

## Quy trình mỗi task

```text
PLAN
→ AUDIT ba tài liệu và code hiện tại
→ xác định phase boundary
→ liệt kê invariants
→ IMPLEMENT tối thiểu
→ chạy targeted tests
→ chạy full regression
→ kiểm tra semantics theo v1.3
→ kiểm tra scope theo v1.2
→ cập nhật IMPLEMENTATION_STATUS
→ chỉ commit/tag sau khi đúng gate.
```

## Build & Deploy

- `D-P0-02-002` — Chỉ build một artifact: `GC.AuctionFlow.dll`.
  **KHÔNG BAO GIỜ** build `Oac.Core` hoặc `Oac.Atas`.
- `D-P0-02A-003` — Deploy đúng một nơi:
  `C:\Users\LOQ\AppData\Roaming\ATAS\Indicators\GC.AuctionFlow.dll`
  **KHÔNG** dual-install sang `Documents`.
- Xác minh SHA-256 `source == deployed` tại mỗi phase closeout.
