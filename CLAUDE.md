# GCAE DOCUMENT AUTHORITY

Trước mọi task, phải đọc ba tài liệu:

1. `docs/spec/GC_AuctionFlow_Engine_v1.2_Post_Cross_Review_Final_Spec_VI.md`
2. `docs/spec/GC_AuctionFlow_Engine_v1.3_Knowledge_Grounded_Spec_VI.md`
3. `docs/implementation/IMPLEMENTATION_STATUS.md`

**Tài liệu tri thức miền tối cao (KDK):**
`docs/spec/KDK_KIM_DAU_KINH_CHUYEN_SAU_OPTIONS.md` — KIM ĐẤU KINH, Phương pháp
Tam Trụ (AMT + Order Flow + Options). Đây là **thẩm quyền miền (domain authority)
cao nhất** cho ý nghĩa mọi khái niệm AMT / Order Flow / Options. Mọi tài liệu khác
(v1.2, v1.3, README, IMPLEMENTATION_STATUS) là **tài liệu triển khai lịch sử phụ
thuộc KDK**, không được định nghĩa lại một khái niệm miền trái với KDK. Không đọc
hết mỗi task (6000+ dòng); **tra chương liên quan** khi đụng ngữ nghĩa, rồi đối
chiếu — nếu tài liệu triển khai mâu thuẫn KDK thì **KDK thắng về ngữ nghĩa miền**
và mâu thuẫn phải ghi vào `docs/governance/SUPERSESSION_REGISTER.md`.

> **GOV-001 (đã sửa 2026-07-28, Round 1B).** Bản CLAUDE.md trước đây ghi "KDK không
> quyết định scope; v1.3/STATUS thắng KDK" — **SAI thứ tự thẩm quyền và đã bị thay
> thế**. Xem `docs/governance/AUTHORITY_ORDER.md`.

## Thứ tự thẩm quyền (BINDING — thay thế bản cũ, GOV-001)

Khi xung đột, áp dụng đúng thứ tự này (chi tiết: `docs/governance/AUTHORITY_ORDER.md`):

1. **Mục tiêu và ràng buộc hiện hành mà Product Owner nêu rõ.** PO chọn ưu tiên và
   phạm vi sản phẩm (vd "GC trước"). PO **không** được lặng lẽ định nghĩa lại một
   khái niệm AMT/Order Flow/Options trái KDK.
2. **KIM ĐẤU KINH (KDK)** — thẩm quyền miền cao nhất về ý nghĩa khái niệm, tháp
   bằng chứng 9 tầng, quy ước `*`, và các bất biến miền.
3. **Specification do reviewer ban hành** — bản dịch KDK thành công việc triển khai
   (phase spec, acceptance gate). Đây là cách hợp lệ để biến KDK thành scope.
4. **Spec kiến trúc hiện hành (v1.2 / v1.3)** — chỉ có hiệu lực **ở nơi không mâu
   thuẫn KDK**. Nơi mâu thuẫn: KDK thắng về ngữ nghĩa, ghi vào SUPERSESSION_REGISTER.
5. **IMPLEMENTATION_STATUS và hồ sơ phase lịch sử** — trạng thái triển khai, không
   phải thẩm quyền miền.
6. **Source code và tests hiện hữu** — **bằng chứng về hành vi hiện tại**, KHÔNG
   phải thẩm quyền để sửa nghĩa KDK. Một tag LOCK lịch sử là bằng chứng của một
   gate đã qua, **không** cấp quyền miễn nhiễm cho một lỗi miền.

Làm rõ:
- Không ai — PO, Claude, reviewer, code, tests, hay tài liệu cũ — được **lặng lẽ**
  định nghĩa lại một khái niệm AMT / Order Flow / Options trái với KDK.
- Khi ràng buộc triển khai cản một tính năng KDK, đánh dấu `NOT_IMPLEMENTED`,
  `BLOCKED` hoặc `AWAITING_SPEC` — **không** định nghĩa lại khái niệm cho khớp code.
- **Chương 76, 77, 79 nằm trong phạm vi sản phẩm mục tiêu** nhưng vẫn
  `AWAITING_DOMAIN_SPEC` và **chưa được phép triển khai** (cần reviewer domain spec).
- **GEX là một mô-đun exposure BÊN TRONG trụ Options, không phải trụ Options.**
- Sửa nghĩa KDK chỉ qua `docs/governance/KDK_CHANGE_CONTROL.md`.

## Nguyên tắc KDK ràng buộc (bên giám sát soi theo đây)

Các nguyên tắc phương pháp trong KDK **ràng buộc** mọi phần domain, kể cả Options:

- **Tháp bằng chứng (9 tầng, KDK v4 dòng 344–363):** 1 toàn vẹn dữ liệu · 2 vị trí cấu
  trúc · 3 diễn biến đấu giá · 4 chấp nhận/tái chấp nhận của giá · 5 dòng lệnh đã
  khớp · 6 vi cấu trúc & thanh khoản hiển thị · 7 **Options/OI/COT/vĩ mô** · 8 kỹ
  thuật vào lệnh · 9 câu chuyện/mẫu hình. **Tầng dưới không phủ quyết tầng trên.**
  Options (tầng 7) **không phủ quyết** sự chấp nhận rõ ràng của giá (tầng 4); xung
  đột chỉ làm giảm độ chắc chắn hoặc đổi quản trị theo quy tắc đã kiểm chứng — khớp
  bất biến §50 (GEX không bao giờ là điều kiện cần).
- **Quy ước `*`:** nội dung `*` (kinh nghiệm/nghiên cứu) **không** tự tạo entry, tăng
  size, xác định hướng, hay phủ quyết chấp nhận giá. Không nâng `*` thành quy luật
  nếu chưa qua lộ trình kiểm chứng (phát lại → quan sát → mô phỏng → ngoài mẫu).
- **"Không giao dịch một tín hiệu"** — luôn cần: bằng chứng + điều kiện sai + không
  gian mục tiêu + dữ liệu đủ tin cậy.
- Không tự đặt threshold (trùng `G-CAL-001`).

## Các lệnh cấm

- Không coi v1.3 là tài liệu thay thế v1.2.
- Không dùng trạng thái tiến độ ghi trong v1.3 thay cho IMPLEMENTATION_STATUS.
- Không triển khai mọi mục `[N]` cùng lúc.
- `[N]` chỉ có nghĩa là bắt buộc khi module tương ứng được authorized.
- Không mở khóa bất kỳ trạng thái `[C]` nào.
- Không tự đặt threshold.
- Không đổi tên enum hoặc policy đã build chỉ để khớp văn bản.
- **GEX/OptionFlow — ĐÃ AUTHORIZED (operator 2026-07-28).** Được tạo module/field/
  enum OptionFlow trong DLL, đọc dữ liệu từ sidecar `artifacts/optionflow/<PRODUCT>/`.
  Bất biến bắt buộc giữ (§50 / KDK Ch50–51): **GEX là một mô-đun exposure bên trong
  trụ Options, không phải trụ Options**; Options là **context tùy chọn tầng 7**,
  không bao giờ là điều kiện cần; khi vắng GEX (file thiếu/cũ/tắt) mọi logic
  AMT+OrderFlow vẫn chạy **byte-identical** và cho kết luận y hệt. GPS row cho GEX
  chỉ tạo khi phase OptionFlow yêu cầu rõ.
- **KDK Ch 76/77/79 (họ chiến lược điều kiện-Options)** — **nằm trong phạm vi sản
  phẩm mục tiêu** nhưng trạng thái `AWAITING_DOMAIN_SPEC`, **chưa được phép triển
  khai**; cần reviewer ban hành domain spec trước. Không mô tả là "out of scope".
- Không dùng Implementation Bible đã archive làm nguồn có thẩm quyền.
- Không sửa phase LOCKED nếu milestone không bắt buộc.
- Không gọi CODE/TEST PASS là FINAL PASS khi live acceptance còn pending.

## Quy trình mỗi task

```text
PLAN
→ TRA CỨU requirement KDK liên quan (KDK-CHxx-REQ-yyy trong 02A/02B)
→ đối chiếu reviewer specification (bản dịch KDK → công việc)
→ AUDIT tài liệu triển khai (v1.2/v1.3/STATUS) + code hiện tại
→ kiểm tra tương thích kiến trúc (chỉ nơi không mâu thuẫn KDK)
→ xác định phase boundary
→ liệt kê invariants (gồm bất biến miền KDK)
→ IMPLEMENT tối thiểu
→ chạy targeted tests
→ chạy full regression
→ KIỂM TRA NGỮ NGHĨA MIỀN THEO KDK (ánh xạ về requirement ID)
→ kiểm tra tương thích v1.3/v1.2 (phụ thuộc KDK, không phủ quyết KDK)
→ cập nhật IMPLEMENTATION_STATUS + trạng thái requirement (02B)
→ chỉ commit/tag sau khi đúng gate.
```

## Phase 5 — OptionFlow / GEX

> **⚠️ HISTORICAL / SUPERSEDED (GOV-002, sửa 2026-07-28 Round 1B).** Đoạn "đã live /
> LOCK / hoàn thiện" bên dưới là **ghi chép lịch sử của các gate cũ, KHÔNG phải trạng
> thái hiện tại**. Audit đã xác định: trụ Options **chưa phải pillar hoàn chỉnh**.
> Không trình bày Phase 5 như một Options pillar đã xong.

### TRẠNG THÁI HIỆN TẠI (đúng, thay cho mọi tuyên bố cũ)
- **Overlay Options = `DISPLAY_ONLY`.** DLL đọc `levels.json` và render; không gate
  AMT/OrderFlow (bất biến §50 giữ: `GexContext=null` ⇒ phase 1–4 byte-identical).
- **Analytics Options = `IMPLEMENTED_BUT_INVALIDATED` / `BLOCKED_BY_DEFECT`.** Sidecar
  tính GEX/flip/regime/EM/skew nhưng **sai** do 6 lỗi đã xác định (OPT-001..006, xem
  `docs/review/03_OPTIONS_DEFECT_REGISTER.md`): mất định danh (expiry/underlying),
  trộn kỳ hạn, thiếu cổng chất lượng quote, `dealer_positioning` khẳng định như sự
  thật. **Không dùng ATM/walls/flip/regime/vanna/charm/EM/skew làm domain truth.**
- **Live 2026-07-28 chỉ chứng minh:** DLL nhận & render dữ liệu trực tiếp
  (`LIVE_DATA_INGRESS`), **không** chứng minh phép tính đúng, module calibrated, hay
  hệ sẵn sàng. Không gọi đây là live-accept/LOCK của một Options pillar.
- **Rebuild pending:** roadmap Phase C (schema `gcae-optionflow-v2`, định danh theo
  (expiry,strike,underlying) + cổng QC + exposure gắn nhãn kịch bản). Chi tiết
  `docs/review/04A_BINDING_ROADMAP_CANDIDATE.md`.

### Ghi chép lịch sử (HISTORICAL — không phải trạng thái hiện tại)
Sidecar `research/optionflow/` kéo option chain từ Rithmic, tính GEX + greeks, ghi
`artifacts/optionflow/<PRODUCT>/levels.json` (schema `gcae-optionflow-v1`). Các gate
cũ từng ghi: 5-0 Governance, 5A schema, 5B reader/GexContext, 5C render, 5D confluence,
5E "GC live accept → LOCK". **Những nhãn PASS/LOCK đó là mốc lịch sử; tag được giữ
nguyên nhưng không cấp miễn nhiễm cho lỗi domain** (xem AUTHORITY_ORDER §6,
SUPERSESSION_REGISTER SUP-004/005/006). **Ưu tiên GC trước** vẫn đúng theo operator.

Bất biến (bắt buộc, §50): `GexContext = null` khi file thiếu/cũ/tắt ⇒ mọi phase 1–4
chạy **byte-identical**; GEX không bao giờ là điều kiện cần; **GEX là một mô-đun bên
trong trụ Options**. DLL **chỉ đọc, không recompute**. Cấm GPS-row/alert/thesis-gating
từ GEX; regime tham gia thesis cần Ch 76/77/79 = `AWAITING_DOMAIN_SPEC`.

**Order flow trực tiếp từ Rithmic (MBO/DOM/time&sales)** — khả thi trên cùng kết nối
(async_rithmic có `ORDER_BOOK`/`depth_by_order`), giàu hơn ATAS (giữ order_id/priority)
nhưng là firehose nặng hơn và cần entitlement riêng. **Chưa authorize** — Phase 6+
riêng, phải probe entitlement trước.

## Build & Deploy

- `D-P0-02-002` — Chỉ build một artifact: `GC.AuctionFlow.dll`.
  **KHÔNG BAO GIỜ** build `Oac.Core` hoặc `Oac.Atas`.
- `D-P0-02A-003` — Deploy đúng một nơi:
  `C:\Users\LOQ\AppData\Roaming\ATAS\Indicators\GC.AuctionFlow.dll`
  **KHÔNG** dual-install sang `Documents`.
- Xác minh SHA-256 `source == deployed` tại mỗi phase closeout.

## KDK v4 — bản canonical mới (Product Owner, 2026-07-28)

Kim Đấu Kinh canonical hiện tại là `docs/spec/KDK_KIM_DAU_KINH_CHUYEN_SAU_OPTIONS.md`
(SHA-256 repo `4cf22c028d682a997e73571462b3579aab64f996cfdc0d00f886a47e529f8c5c`, 6382 dòng),
thay thế bản v3 (6018 dòng). Bản PO cung cấp `51cbf108…` (6309 dòng); repo khác vì đã áp
**2 chỉnh sửa biên tập được ủy quyền** (bỏ escape Markdown ở Ch51; thêm mục Ch50 "Giao dịch
phản ứng tại vùng Options"). Chi tiết: `docs/governance/KDK_CHANGE_CONTROL.md`.

**Flow doctrine v4 (bắt buộc):** Toàn vẹn dữ liệu → AMT và Options chuẩn bị song song nhưng
khác câu hỏi → AMT xây bản đồ/trạng thái → Options xây regime/horizon/vùng nhạy cảm → giá
tiếp cận vùng → Episode mở → Order Flow đánh giá nỗ lực/kết quả → Acceptance phán quyết
FAR/AAC/Rotation/Unresolved → Options có thể điều kiện hóa/chặn qua policy đã kiểm chứng →
Governance quyết Trade/Wait/No-Trade → GC ánh xạ CFD chỉ để thực thi. **"Song song" KHÔNG
phải mô hình bỏ phiếu điểm.** AMT sở hữu vị trí/trạng thái/acceptance; Order Flow sở hữu nỗ
lực đã thực thi; Options sở hữu định giá rủi ro/horizon/vùng nhạy cảm; Governance sở hữu quyền
tham gia; Execution sở hữu quy đổi CFD. Một đường Options đơn lẻ không bao giờ là tín hiệu vào.

**Catalog requirement (02A/02B) ĐÃ regenerate từ v4** — **679 active requirements** (668
LINE_SHIFT_ONLY + 1 BASELINE_ANCHOR_CORRECTION + 10 NEW); số 669 cũ và v3 line refs **vô hiệu**.
02B **vẫn provisional**. Catalog đã qua review độc lập; full adoption chờ commit được ủy quyền
(status SUP-011: ADOPTED_IN_DOCS / CATALOG_REGENERATED).
