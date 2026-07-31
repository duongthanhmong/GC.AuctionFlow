# BẢN THẢO GCAE — Kế hoạch tổng thể đi tiếp

**Ngày lập:** 2026-07-31 · **Trạng thái:** `ĐỀ XUẤT — CHỜ CHỦ DỰ ÁN DUYỆT`
**Đọc kèm:** [`TIEN_DO.md`](TIEN_DO.md) (hiện trạng)
**Nguồn thẩm quyền:** KDK v4 · MRBS v1.1 §26 (lộ trình) · Parameter Registry v1.0
**Tài liệu phái sinh:** `MRBS_V1_1_CODE_ALIGNMENT_MATRIX_R3E.csv` (hiện hành; R1 đã bị bác bỏ) ·
`MRBS_V1_1_PHASE_OWNERSHIP_MAP_R3.csv` · `MRBS_V1_1_CONFLICT_REGISTER_R2.csv`

> **Bản thảo này không phải kế hoạch mới.** Lộ trình đã có sẵn trong MRBS §26 (Phase 1–5) và
> bản đồ sở hữu B0–B6/C/D của gói rà soát R3. File này **ghép ba tài liệu thành một trình tự
> thi công duy nhất**, gắn cổng vào/ra và chỉ rõ bước nào bị chặn bởi ai.
>
> **Không mục nào được tự khởi động.** Mỗi chặng đều có ô **"Ủy quyền cần có"**.

---

## 0. Nguyên tắc chi phối (và ranh giới thẩm quyền của chúng)

Bốn nguyên tắc. **Ba là luật từ nguồn gốc; một là quyết định kỹ thuật của dự án — ghi rõ để
không nhầm.**

| # | Nguyên tắc | Thẩm quyền |
|---|---|---|
| **1** | **Thang quyền hạn.** *"Tầng dưới không được phủ quyết tầng trên"* — tầng 1 quyền cao nhất, tầng 9 thấp nhất | **LUẬT KDK** (dòng 359) |
| **2** | **Không tự đặt threshold.** 134 tham số, 0 `APPROVED-RESEARCH` / 0 `APPROVED-PRODUCTION`. Thăng hạng cần dataset point-in-time + kết quả ngoài mẫu + Decision Record | **LUẬT** — MRBS §44 (dòng 1806) + §24.3 (dòng 720); Registry §1; `G-CAL-001`; ADR-005 |
| **3** | **Xung đột thì dừng, không tự chọn.** *"không tự chọn một bên… phải trả về `SPEC_CONFLICT_REVIEW_REQUIRED` cho đến khi có decision record"* | **LUẬT MRBS** (dòng 62) |
| **4** | **Ở Phase 1, không triển khai 14 setup như 14 bộ logic độc lập.** Khóa trước 3 trạng thái lõi FAR/AAC/Rotation; các setup còn lại là thuộc tính location/confirmation/policy | **LUẬT MRBS** §26 (dòng 850) — lưu ý phạm vi gốc là **"ở Phase 1"** |

**Nguyên tắc thứ năm — QUYẾT ĐỊNH KỸ THUẬT CỦA DỰ ÁN, KHÔNG PHẢI LUẬT KDK:**

> **Ưu tiên hoàn thiện tầng 1 trước khi calibrate các tầng trên.**
>
> KDK **không** có điều khoản "không xây tầng N khi tầng N−1 chưa đạt"; ngược lại dòng 2447
> cho phép đọc AMT và Options **song song**. Cơ sở của quyết định này là: (a) tầng 1 có quyền
> hạn cao nhất (dòng 359); (b) Ch82 (5235-5246) liệt kê các điều kiện dữ liệu **chặn tuyệt đối**
> việc thực thi; (c) nguyên tắc 2 khiến việc calibrate bất khả nếu chưa có dataset hợp lệ.
> **Chủ dự án có quyền bác nguyên tắc này** và cho chạy song song nhiều chặng.

---

## 1. Bức tranh tổng: 6 chặng

| Chặng | Tên | MRBS §26 | Phase owner | Tầng KDK | Trạng thái |
|---|---|---|---|---|---|
| **0** | Đóng nền dữ liệu | (tiên quyết) | B0 | 1 | **ĐANG LÀM — 4 mục mở** |
| **1** | Nền tất định | Phase 1 | B0 + B1 | 1–2 | Skeleton có, chưa đạt cổng |
| **2** | Trạng thái đấu giá | Phase 2 | B3 + B5 | 3–4 | Skeleton có, chưa calibrate |
| **3** | Trí tuệ thực thi M1 | Phase 3 | B2 + B4 | 5 | Skeleton có, chưa calibrate |
| **4** | Trụ Options (dựng lại) | Phase 5 (phần Options) | Phase C | 7 | Cần dựng lại |
| **5** | Cầu thực thi | Phase 4 | External | 8 | Bị gắn `CANDIDATE-DEPRECATION` |

> **Vì sao đảo chỗ hai chặng cuối:** MRBS đánh số Phase 4 = execution bridge, Phase 5 = optional
> intelligence. Bản thảo này đặt Options **trước** CFD vì Addendum v1.1 §43.4 đã gắn *"Mục 19 và
> Phase 4"* nhãn `CANDIDATE-DEPRECATION`, chờ quyết định có giữ trong analysis core không. Làm
> trước có nguy cơ phí công. **Đây là đề xuất, cần Q3/`SC-009` trả lời.**

---

## 2. CHẶNG 0 — Đóng nền dữ liệu (đang dở, chặn mọi thứ)

**Ủy quyền cần có:** Q2 (duyệt ADR) · Q3 (giải xung đột) · Q4 (nghiệm thu Stage 2)

**Đã xong:** Stage 2 Closure Package — 7 ADR, 6 JSON Schema, manifest 32 surface, 15 EvidenceId,
47/47 test, kiểm chứng độc lập hai đường.

| # | Việc | Cổng ra | Chặn bởi |
|---|---|---|---|
| **0.1** | Chủ dự án duyệt 7 ADR | ADR → `ACCEPTED`, kiến trúc recorder có hiệu lực | **Q2** |
| **0.2** | Decision Record cho `SC-005..009` + `CONF-001` + `RECL-01/02/03` | Hết `SPEC_CONFLICT_REVIEW_REQUIRED` | **Q3** |
| **0.3** | **D2 — Writer path**: hiện thực writer ghi theo 6 schema đã chốt | Dataset đầu tiên validate qua schema | 0.1 |
| **0.4** | **D1 — Tải bền**: chạy recorder dài dưới tải thật; đo throughput, queue depth, dropped event, writer latency, CPU, RAM, disk, reconnect | Bằng chứng thay cho cửa sổ 90 giây | 0.1 + phiên live |
| **0.5** | Nghiệm thu Stage 2 | Tầng 1 đạt | **Q4** |

**Ràng buộc kỹ thuật bắt buộc cho 0.3 (đã đo, không phải giả định):**

- Core authoritative = **trades + BBO + aggregated depth** (ADR-001). MBO giữ nguyên
  `RESEARCH_TELEMETRY`, **không** dùng làm nguồn full-book (ADR-002).
- Gap **fail-closed**: `READY → GAP_DETECTED → RECOVERING → READY/DEGRADED`. Recorder tự theo
  dõi `last_sequence` theo (instrument, scope); gap trùng sự kiện kết nối ⇒ `Invalid` +
  `DATA_INVALID` + bản ghi discontinuity (ADR-003).
- Depth/MBO mất trong gap ⇒ `UNRECOVERABLE`; **cấm tạo liên tục giả**.
- Sau reconnect chỉ về `READY` khi có snapshot/resync hợp lệ.
- Backfill trades **chỉ bằng lịch sử UTC-aware** (`base.py:567-577` từng lệch 7 tiếng, im lặng).
- Raw frame 152/155/157/158 **phải bảo tồn** verbatim (ADR-007).

---

## 3. CHẶNG 1 — Nền tất định (MRBS Phase 1 · B0 + B1 · tầng 1–2)

**MRBS §26 Phase 1:** Market data normalization · Session Engine · TPO/VP deterministic engine ·
ReferenceZone model · Data quality/versioning/event log.

**Cổng vào:** Chặng 0 đạt · **Ủy quyền cần có:** Q4 + **Q9** (phê chuẩn `02A/02B/02D` — `04A:58`
đặt đây là điều kiện cứng của Phase B)

| # | Việc | Vì sao | Căn cứ (R3E) |
|---|---|---|---|
| 1.1 | **Sửa `SessionTradingDate` — CONFLICT** | Hiện là **một cửa sổ 24h cứng** neo 08:20 New York, không ngày lễ / phiên ngắn | `CONF-001` |
| 1.2 | **Mở rộng `ReferenceZone`** — hiện `PARTIAL`, **impact High** | Registry đã chạy (`ReferenceType`, `StructuralReferenceRegistry`); thiếu half-width, `Role` enum, `TouchCount`, reason code | R3E dòng 10 |
| 1.3 | **Hiện thực `EngineLifecycleWarmup` — `NOT_IMPLEMENTED`** | Không có `ColdStart/HistoricalWarmup/AnalysisReady/Recovering`, không `RestoreSnapshotId` | MRBS §39 |
| 1.4 | **Đo gap / duplicate / out-of-order** (hiện chỉ có `NextSequence`) | DQ-001 bắt buộc | R3E |
| 1.5 | Bổ sung `ConfigVersion`/`AlgorithmVersion`/`DataSchemaVersion`; `StructuralAnalysisSnapshot` thiếu `SessionId`/`CapabilitySnapshotId` (**impact High**) | VER-001: thiếu version ⇒ record invalid | R3E |
| 1.6 | Nối `TouchCount` của PriceMemory vào zone; bỏ `row_size = 1 tick` hard-code của TPO | | R3E |

**Cổng ra:** 5 mục MRBS Phase 1 đạt · **replay golden test cấp engine chạy được** (hiện
`DeterministicReplayAudit` PARTIAL — có ledger reason-code nhưng chưa có golden test) · dataset
B1 có provenance.

---

## 4. CHẶNG 2 — Trạng thái đấu giá (MRBS Phase 2 · B3 + B5 · tầng 3–4)

**MRBS §26 Phase 2:** Episode state machine · Acceptance classifier · Attempt/rearm · Composite
lifecycle · Target graph.

**Cổng vào:** Chặng 1 đạt **+ dataset đủ dài để calibrate** (§7) · **Ủy quyền:** Q3 (cho 2.5, 2.6) + Q9

| # | Việc | Ghi chú |
|---|---|---|
| 2.1 | Thêm **timeout** cho Episode FSM | hiện thiếu |
| 2.2 | Kích hoạt **Acceptance** ở B5 — **thẩm quyền cuối của trụ AMT** | KDK **3457**: *"Acceptance thuộc miền AMT"* |
| 2.3 | **Khóa 3 trạng thái lõi FAR / AAC / Rotation** trước | MRBS §26 dòng 850 |
| 2.4 | `ValueRotation` — hiện là context, `ThesisFamily = {Unknown, Far, Aac}` | `RECL-02` (`NOT_IMPLEMENTED` cho tới B5) |
| 2.5 | Attempt/Rearm: **giải `SC-005` trước khi code** | `ANY` điều kiện (MRBS) vs hình học + thời gian (Registry) |
| 2.6 | Acceptance: **giải `SC-006` trước khi code** | 3 bar M1 + local POC (MRBS) vs evidence group (Registry) |

**Cổng ra:** FAR/AAC/Rotation phát được nhãn có `ResearchConfidence`; mọi threshold trỏ về
Registry với `approval_status` rõ ràng; **không hard-code**.

---

## 5. CHẶNG 3 — Trí tuệ thực thi M1 (MRBS Phase 3 · B2 + B4 · tầng 5)

**MRBS §26 Phase 3:** M1 Footprint/Delta · Effort-Result · FAR/AAC/Rotation Standard trigger ·
Stop/timeout/no-chase · Replay UI/log.

**Cổng vào:** Chặng 1 đạt · **Ủy quyền:** Q3 (bắt buộc cho 3.1 và 3.6) + Q9

| # | Việc | Ghi chú |
|---|---|---|
| **3.1** | **Quyết định M1: 60s bar hay event-scope?** | Code hiện **event-driven per-trade, không phải 60s bar**. `RECL-03` — **phải quyết trước khi làm tiếp** |
| 3.2 | Bổ sung **CVD families** — hiện `PARTIAL`, chỉ 1 `ClassifiedCvd` scoped theo auction | phụ thuộc SessionEngine (1.1); R3E dòng 17 |
| 3.3 | Effort/Result: bổ sung P80/P35 | R3E |
| 3.4 | Imbalance: **bind** ratio 3.0 / stack 3 từ Registry | hiện `NOT_PRESENT` — **chưa có giá trị nào trong code**, không phải hard-code |
| 3.5 | Sửa event-ordering: *"late trade after publish silently absent (no revision event)"* | **impact High**; điều khoản chi phối là **MRBS §40.2** (dòng 1573) + seed `late_event_grace_period_ms` (§40.3) |
| 3.6 | Stop / timeout / no-chase | §43.5 gắn MRBS §15 nhãn `CANDIDATE-DEPRECATION` — **chờ Q3** |

---

## 6. CHẶNG 4 — Trụ Options, dựng lại (Phase C · tầng 7)

**Đây là dựng lại, không phải sửa.** Sidecar tính sai 6 lỗi gốc; DLL overlay giữ nguyên hành vi
`DISPLAY_ONLY` nhưng **mang BLOCKER phạm vi thứ 7**: nó chỉ là GEX overlay, **không phải trụ
Options** (`03:151-155`).

**Cổng vào:** Chặng 2 đạt (Phase C nằm **sau B5**) · **Ủy quyền:** **Q5** (vendor `.proto`) +
**Q10** (ủy quyền `04A` thành binding spec — hiện *"Nothing here is authorized for implementation"*)

| # | Việc | Đóng lỗi |
|---|---|---|
| 4.1 | Schema `gcae-optionflow-v2`: định danh theo **(expiry, strike, underlying)** | `OPT-001`, `OPT-003` |
| 4.2 | Mỗi option resolve **underlying riêng** — `OGU6`→`GCV6`, không dùng front-month chung | `OPT-002`; ROLL-004 |
| 4.3 | IV/skew **theo từng expiry**; EM không trộn horizon | `OPT-004` |
| 4.4 | **Quote Quality Gate**: tuổi quote, spread, crossed, stale, no-arb | `OPT-006` |
| 4.5 | Exposure gắn **nhãn kịch bản** + `{assumption, formula, unit, confidence}` | `OPT-005` |
| 4.6 | Mở khóa OI/settlement: cần `.proto` cho template **158/155/157** | ADR-004 (`CLIENT_BLOCKED`) |
| 4.7 | Nâng overlay từ "GEX display" thành module trong trụ Options | BLOCKER phạm vi |

**Bất biến phải giữ — phân biệt rõ hai nguồn:**

- **Bất biến miền (KDK):** GEX là **mô-đun, không phải trụ** (KDK 275 và Ch48 dòng 2905);
  Options là context tầng 7, **không bao giờ là điều kiện cần**; khi Options không dùng được thì
  vận hành bằng hai trụ còn lại (Ch50).
- **Bất biến triển khai (CLAUDE.md §50 — repo governance, KHÔNG có trong KDK):**
  `GexContext = null` ⇒ phase 1–4 **byte-identical**; cấm GPS-row / alert / thesis-gating từ GEX.

**Ngoài phạm vi:** Ch76/77/79 (`AWAITING_DOMAIN_SPEC`, cần **Q6**). KDK có gating nội tại riêng:
Ch77 *"chỉ được giao dịch khi AAC hoặc bằng chứng khởi xướng đã xuất hiện"*; Ch79 *"Xung đột
Options không có nghĩa Options phủ quyết giá"*.

---

## 7. Điều kiện then chốt xuyên suốt: DATASET

Không có dataset thì **Chặng 2 trở đi không thể bắt đầu** — không phải vì thiếu code, mà vì
nguyên tắc 2 cấm tự đặt threshold.

`DATASET_CONTRACTS.md` nêu 6 điều kiện; hiện **1, 3, 5, 6 chưa đạt**:

| # | Điều kiện | Trạng thái |
|---|---|---|
| 1 | Point-in-time, không look-ahead | schema hỗ trợ, **chưa có dataset** |
| 2 | Provenance đầy đủ mỗi record | **schema đã ép** |
| 3 | `dataQuality = Ready` suốt cửa sổ calibrate | **chưa đo trên cửa sổ thật nào** |
| 4 | Không gap chưa hồi phục trong cửa sổ | **schema đã ép** |
| 5 | Đủ mẫu + warm-up | **tham số chưa bind** |
| 6 | Giữ lại kỳ ngoài mẫu | **chưa chạy** |

**Cấm tuyệt đối:** dùng fixture, dữ liệu trùng, hoặc dữ liệu thiếu provenance để calibrate.

---

## 8. Đường tới hạn

```
Q2 duyệt ADR ─┐
              ├─→ 0.3 Writer ──→ 0.4 Tải bền ──→ Q4 nghiệm thu Stage 2
Q3 giải SC ───┘                                        │
                                                       ▼
                                  Q9 phê chuẩn 02A/02B/02D
                                                       │
                                                       ▼
                        Chặng 1 (Session · Zone · Warmup · DQ · Version)
                                                       │
                                       ┌───────────────┴──────────────┐
                                       ▼                              ▼
                            THU DATASET DÀI HẠN            Chặng 3 (M1 · CVD · E/R)
                                       │                    (cần 3.1 quyết M1 trước)
                                       ▼
                        Chặng 2 (Episode · Acceptance · FAR/AAC/Rotation)
                                       │
                                       ▼
                Chặng 4 (Options v2) ── cần Q5 (.proto) + Q10 (04A) + Q6 (Ch76/77/79)
                                       │
                                       ▼
                        Chặng 5 (CFD) ── chờ Q3/SC-009 xem có giữ không
```

**Nút thắt lớn nhất không phải code — là 5 chữ ký (Q2, Q3, Q4, Q9, Q10) và 1 dataset.**

---

## 9. Việc tiếp theo được đề xuất

| Ưu tiên | Việc | Ai làm | Chặn bởi |
|---|---|---|---|
| **1** | Đưa **execution brief** vào repo (commit thẳng, đừng đính kèm) | Chủ dự án | — |
| **2** | Duyệt / bác 7 ADR | Chủ dự án | — |
| **3** | Decision Record cho `SC-005..009` + `CONF-001` + `RECL-01/02/03` | Reviewer | — |
| **4** | Nghiệm thu hoặc trả lại Stage 2 Closure Package | Reviewer | — |
| **5** | Phê chuẩn `02A/02B/02D` (Q9) | Reviewer | — |
| **6** | Hiện thực **writer path** (0.3) theo 6 schema | Claude | #2 |
| **7** | Chạy **tải bền** (0.4) và thu dataset dài hạn | Claude + phiên live | #2, #6 |
| **8** | Chặng 1: Session / ReferenceZone / Warmup / DQ / Version | Claude | #4, #5 |
| **9** | Xoay credential `SEC-001` | Vận hành | — |

---

## 10. Ba điều bản thảo này **không** làm

1. **Không giải bất kỳ `SPEC_CONFLICT` nào.** MRBS dòng 62 cấm tự chọn một bên.
2. **Không bind tham số nào.** 134 vẫn 0 `APPROVED-RESEARCH` / 0 `APPROVED-PRODUCTION`.
3. **Không tự khởi động chặng nào.** Mọi chặng đều có ô ủy quyền; đây là bản vẽ, không phải lệnh
   thi công.
