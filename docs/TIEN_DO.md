# TIẾN ĐỘ DỰ ÁN GCAE — Bản đồ thống nhất theo KDK

**Ngày lập:** 2026-07-31 · **Nhánh:** `wp-l1-rithmic-data-surface-discovery` · **HEAD:** `308ba3b`

**Nguồn thẩm quyền (ba file gốc):**
KDK v4 `docs/spec/KDK_KIM_DAU_KINH_CHUYEN_SAU_OPTIONS.md` (6382 dòng, SHA-256 `4cf22c02…`) ·
MRBS v1.1 `docs/review/kdk_v4/mrbs_adoption_r1/inputs/KDK_MRBS_v1.1.md` (1969 dòng) ·
Parameter Registry v1.0 `docs/review/kdk_v4/mrbs_param_recon_r3/inputs/KDK_Parameter_Registry_v1.0.md` (1005 dòng).

**Tài liệu phái sinh được dùng (KHÔNG phải nguồn gốc, do gói rà soát tạo ra):**
`mrbs_param_recon_r3e/MRBS_V1_1_CODE_ALIGNMENT_MATRIX_R3E.csv` (bản hiện hành — R1 **đã bị bác
bỏ**, xem `mrbs_adoption_r2/README_R2.md:3` *"Supersedes R1 (rejected)"*) ·
`mrbs_param_recon_r3/MRBS_V1_1_PHASE_OWNERSHIP_MAP_R3.csv` (mã pha B0–B6/C/D) ·
`mrbs_param_recon_r3/KDK_PARAMETER_CANONICAL_REGISTRY_R3.csv` (134 tham số) ·
`mrbs_adoption_r2/MRBS_V1_1_CONFLICT_REGISTER_R2.csv`.

> File này thay cho cảm giác "các viên gạch chồng lên nhau không có bản thảo". Kế hoạch đi
> tiếp: [`BAN_THAO.md`](BAN_THAO.md).

---

## 1. Vì sao dự án "trông rời rạc" — và vì sao thực ra không phải

### 1.1 Tháp bằng chứng là thang **quyền hạn**, không phải thứ tự thi công

KDK dòng 344-363 định nghĩa **"Tháp quyền hạn của bằng chứng"** — 9 tầng, **tầng 1 ở vị trí
quyền hạn cao nhất**. Luật (dòng 359): **"Tầng dưới không được phủ quyết tầng trên."**

Chiều của tháp được chứng minh bằng chính ví dụ của KDK (dòng 360-362):

- *"Dòng lệnh mạnh không phủ quyết sự chấp nhận giá theo hướng ngược lại"* → tầng **5** không
  phủ quyết tầng **4**
- *"Options không phủ quyết bằng chứng chấp nhận rõ ràng từ giá"* → tầng **7** không phủ quyết
  tầng **4**
- *"Một mẫu hình đẹp không cứu được vị trí sai"* → tầng **9** không phủ quyết tầng **2**

Vậy trong ngôn ngữ KDK, **tầng 1 là "tầng trên"** (quyền cao nhất), tầng 9 là "tầng dưới".

> **Đính chính quan trọng.** KDK **không** có luật nào bảo "không xây tầng N khi tầng N−1 chưa
> xong". Ngược lại, dòng 2447 nói rõ: *"Trong giai đoạn chuẩn bị, AMT và Options có thể được
> đọc song song vì chúng trả lời hai nhóm câu hỏi khác nhau."* Việc **ưu tiên thi công theo thứ
> tự tầng là quyết định kỹ thuật của dự án này**, không phải điều khoản KDK. Cơ sở KDK cho việc
> ưu tiên dữ liệu là: tầng 1 có **quyền hạn cao nhất**, và Ch82 (5235-5246) liệt kê các điều
> kiện dữ liệu **chặn tuyệt đối** việc thực thi.

### 1.2 Chuyện đã xảy ra

- Đến **2026-07-28**, skeleton Phase 0–5 đã dựng xong và chạy full-chain live một lần
  (`IMPLEMENTATION_STATUS.md:8`).
- **Cùng ngày**, audit phát hiện trụ Options có **7 BLOCKER** (6 lỗi gốc sidecar `OPT-001..006`
  **+ 1 BLOCKER phạm vi**: DLL chỉ là GEX overlay, **không phải** trụ Options) và 12 lỗi lớn
  (`03_OPTIONS_DEFECT_REGISTER.md:151-155`); đồng thời nền dữ liệu tầng 1 chưa được chứng minh.
- Từ **29/07 đến 31/07**, toàn bộ công sức dồn về **tầng 1**: adopt KDK v4, dựng lại catalog
  679 yêu cầu, rồi cả ngày 31/07 điều tra khả năng thu thập dữ liệu Rithmic trực tiếp.
- **`IMPLEMENTATION_STATUS.md` dừng ghi ở 28/07** → nhìn vào nó thấy "skeleton xong", rồi thấy
  một loạt probe/recorder/schema/ADR không rõ liên hệ. **Đó chính là ấn tượng rời rạc.**

Ví von (**ẩn dụ của tài liệu này, không phải ngôn ngữ KDK**): nhà đã dựng khung tới mái, kiểm
tra thì móng chưa đạt, cả đội quay xuống làm móng — nhưng không ai treo bảng "đang làm móng".

---

## 2. Trạng thái theo 9 tầng Tháp bằng chứng

> **Từ vựng trạng thái:** cột "Trạng thái" lấy `alignment_status` từ
> `MRBS_V1_1_CODE_ALIGNMENT_MATRIX_R3E.csv` (`ALIGNED` / `PARTIAL` / `NOT_IMPLEMENTED` /
> `CONFLICT`) và các nhãn từ ADR/CLAUDE.md. **Đây không phải từ vựng của KDK hay MRBS.** Hai
> nhãn tiến độ `ĐANG GIA CỐ` và `CHƯA MỞ` là **nhãn của tài liệu này**.
>
> Cột "Module" gồm cả **thư mục `src/`** (có dấu `/`) và **nhóm module theo bản đồ sở hữu**
> (không dấu `/`) — ví dụ `DeltaCvd` là nhãn nhóm, hiện thực nằm trong `Orderflow/MutableOrderflow.cs`.

| # | Tầng KDK (nguyên văn, dòng 346-354) | Module | Trạng thái | Ghi chú |
|---|---|---|---|---|
| **1** | Tính toàn vẹn dữ liệu | `Probe/` (39), `Recorder/` (28), `Data/` (6), `Core/` (5) | **ĐANG GIA CỐ** | Đường ATAS thiếu gap-recovery, mất native sequence. Đường Rithmic trực tiếp đã chứng minh lấy đủ. Xem §4 |
| **2** | Vị trí cấu trúc | `Profile/` (12), `Reference/` (8), `Directional/` (10), `Composite/` (7), `Memory/` (4) | **PARTIAL** | `ReferenceZone` PARTIAL, **impact High** (R3E dòng 10) — registry đã chạy, còn thiếu half-width / `Role` / `TouchCount` |
| **3** | Diễn biến cuộc đấu giá | `Episode/` (11), `Participation/` (5), `DayStructure/` (1) | **PARTIAL** | Episode FSM quan sát được; **chưa có timeout** |
| **4** | Sự chấp nhận hoặc tái chấp nhận của giá | `Evidence/` (9), `Resolution/` (5) | **PARTIAL**, threshold `NotCalibrated` | **Quyền phán quyết thuộc trụ AMT** (KDK 3457). Kích hoạt ở B5 |
| **5** | Dòng lệnh đã khớp | `Orderflow/` (7), `EffortResult/` (5), `Facilitation/` (4), `Cluster/` (6) | **PARTIAL** | CVD PARTIAL (R3E dòng 17) — có 1 `ClassifiedCvd` scoped theo auction, **thiếu "families"** |
| **6** | Vi cấu trúc và thanh khoản hiển thị | `Imbalance/` (2), `Efficiency/` (6), DOM/MBO probes | **RESEARCH_TELEMETRY** | MBO không dựng lại full-book (ADR-002). MRBS DQ-002: không phải dependency core |
| **7** | Cấu trúc Options, OI, COT và vĩ mô | `OptionFlow/` (6) trong DLL, `research/optionflow/` sidecar | **PARTIAL + INVALIDATED** | R3E dòng 23 = `PARTIAL`, `Defer(Phase C; OPT-002/004)`. Xem §5 |
| **8** | Kỹ thuật vào lệnh | `Entry/` (1), `Execution/` (2), `Plar/` (4), `Maturity/` (4) | **STUB** | Entry chỉ phát `ObserveOnly`; CFD không có feed |
| **9** | Câu chuyện, ẩn dụ và mẫu hình | `Thesis/` (12) | **CHƯA MỞ** | Quyền hạn thấp nhất trong tháp; chưa đến lượt |

**Thư mục không thuộc một tầng cụ thể** (39 file, ghi ra để không bỏ sót): `Research/` (18 —
dataset store, calibration protocol, research bridge), `Runtime/` (11 — `GcaeRuntimeEngine`,
`ModuleDriveSchedule`, `RuntimeTransitionLedger`), `Atas/` (5), `UI/` (4), `Logging/` (1),
`Contract/` (rỗng).

**Tổng: 248 file `.cs`** được git theo dõi trong `src/GC.AuctionFlow` (không tính `bin/`, `obj/`).

**Đọc bảng này thế nào:** skeleton có ở hầu hết các tầng, nhưng **không tầng nào được
calibrate** — vì luật cấm tự đặt threshold, và dataset hợp lệ chưa tồn tại (§6). Việc các
classifier báo `NotCalibrated` là **kỷ luật, không phải dở dang**.

---

## 3. Trạng thái theo mã pha sở hữu (B0–B6 / C / D)

> **Nguồn:** `MRBS_V1_1_PHASE_OWNERSHIP_MAP_R3.csv` — do **gói rà soát R3** tạo ra, **không
> phải MRBS**. MRBS §26 (dòng 792) chỉ định nghĩa **Phase 1–5** theo tên; chuỗi `B0`…`B6`,
> `Phase C`, `Phase D` **không xuất hiện lần nào** trong MRBS v1.1 hay Registry v1.0.

| Pha | Sở hữu | Tầng KDK | Tham số /134 | Trạng thái code (R3E) |
|---|---|---|---|---|
| **B0** | Contract/provenance/versioning · DataQuality + capability gate · engine lifecycle + warm-up · deterministic scheduling + bar finalization · công bố analysis-only output contract (`StructuralAnalysisSnapshot`) | 1 | 12 | PARTIAL — `EngineLifecycleWarmup` chưa có; gap/duplicate/out-of-order **chưa đo**; `StructuralAnalysisSnapshot` thiếu `config_version`/`SessionId`/`CapabilitySnapshotId`, **impact High** |
| **B1** | Session · TPO/VP · Composite · Regime + ValueMigration · OneTimeFraming · ReferenceZone + PriceMemory · OpeningContextIB | 2 | **40** | PARTIAL — `SessionTradingDate` **CONFLICT** (1 cửa sổ 24h cứng, `CONF-001`) |
| **B2** | M1 order flow thô (trades + Bid/Ask) · Delta/CVD · Imbalance/footprint | 5–6 | 23 | PARTIAL — M1 **event-driven per-trade, không phải 60s bar** (`RECL-03`) |
| **B3** | AuctionEpisode · IndependentAttemptRearm/retest | 3 | 13 | PARTIAL — chưa timeout |
| **B4** | Effort/Result + AuctionEfficiency **thô**; FAR/AAC evidence là `candidate_only`, **"NOT final"** | 5 | 9 | PARTIAL |
| **B5** | Acceptance join (tiêu thụ bằng chứng B4) + SetupEngine = **thẩm quyền cuối** FAR/AAC/ValueRotation/Unresolved | 4 | 27 | PARTIAL — threshold `NotCalibrated` |
| **B6** | DOM/MBO **research-only** | 6 | **0** — Registry không có tham số DOM/MBO nào | Telemetry (MRBS DQ-002) |
| **Phase C** | OptionsContext exposure, **sau B5**, gắn OPT-002/004 | 7 | 10 | `AWAITING_DOMAIN_SPEC` + sidecar phải dựng lại |
| **Phase D / Governance** | Phê duyệt tham số / participation / policy qua Decision Record | (xuyên suốt) | — | **0 `APPROVED-RESEARCH`, 0 `APPROVED-PRODUCTION`** |
| **External** | GC-CFD mapping + broker execution (`non_authoritative`) | 8 | — | Stub; MRBS §19/Phase 4 bị gắn `CANDIDATE-DEPRECATION` |

> **Cảnh báo va tên (đã xác minh):** trong `ADOPTION_CLOSEOUT.md`, nhãn "B1/B2…" chỉ **các bước
> của gói dựng lại catalog KDK v4** (B1 = đóng băng định danh KDK, B2 = migration baseline),
> **không phải** mã pha sở hữu ở bảng trên.

---

## 4. Tầng 1 — nơi đang thi công (WP-L1-RITHMIC, ngày 31/07)

Kết tinh trong Stage 2 Closure Package (`docs/review/kdk_v4/wp_l1_rithmic/stage2_closure/`):
7 ADR, 6 JSON Schema, manifest 32 surface, 15 EvidenceId, **47/47 test**, kiểm chứng độc lập
hai đường (ZIP + git bundle).

**Đã chứng minh — mỗi mục có EvidenceId re-hash được** (`STAGE2_CHECKLIST.md` A1–A14):

| | |
|---|---|
| Futures: trades + aggressor (trường vendor) + BBO + aggregated depth + 3 miền đồng hồ | **PROVEN** |
| Entitlement L1+L2 trên COMEX/NYMEX/CBOT/CME | **PROVEN** |
| Options identity (underlying/strike/expiry/put-call/point-value) + quotes | **VERIFIED** — 4 hợp đồng `OGU6` |
| Mapping option→futures **không đồng nhất** (`OGU6`→`GCV6` ≠ front month `GCZ6`) | **VERIFIED** |
| Historical/live parity | **PROVEN** — 76/77 |

**Đã chứng minh là VẮNG hoặc BỊ CHẶN** (kết quả âm tính cũng là kết quả):

| | |
|---|---|
| Gap recovery | **VẮNG** — mất 3.975 sequence qua 12,4s, client không báo (ADR-003) |
| Dựng lại full-book từ template 116+160 | **BẤT KHẢ** trên đường này — 116 seed 1 order/giá, khớp 3/21 mức (ADR-002) |
| OI / settlement / market mode của options | **CLIENT_BLOCKED** — có trên dây (template 158/155/157), `async_rithmic 1.6.3` vứt bỏ (ADR-004) |

**Còn mở (D1–D4):** tải bền (cửa sổ dài nhất 90 giây), writer path (chưa có implementation),
dataset hiệu chỉnh (chưa tồn tại), 5 `SPEC_CONFLICT`.

---

## 5. Tầng 7 — trụ Options: hiện trạng đúng

| Lớp | Trạng thái | Căn cứ |
|---|---|---|
| Overlay trong DLL | **PARTIAL** — `DISPLAY_ONLY`, đọc `levels.json` render. **Mang 1 BLOCKER phạm vi:** DLL chỉ là GEX overlay, **không phải trụ Options** | R3E dòng 23 (`Defer(Phase C; OPT-002/004)`); `03:151-155`. Không có enum `OptionsState` nào trong `src/` |
| Analytics trong sidecar | `IMPLEMENTED_BUT_INVALIDATED` — GEX/flip/EM/skew tính **sai** | 6 lỗi gốc `OPT-001..006` |
| Live 28/07 | chỉ chứng minh DLL nhận & render dữ liệu | **không** chứng minh phép tính đúng |

Sáu lỗi gốc: mất định danh expiry/underlying (`OPT-001`), một front-month suy vol cho mọi option
(`OPT-002`), ATM straddle ghép nhầm kỳ hạn (`OPT-003`), EM trộn horizon (`OPT-004`),
`dealer_positioning` khẳng định như sự thật (`OPT-005`), thiếu cổng chất lượng quote (`OPT-006`).

Kế hoạch dựng lại (`04A`, schema `gcae-optionflow-v2`) là **ứng viên — chưa được phép triển
khai** (`04A:3`). **Không dùng ATM/walls/flip/regime/EM/skew làm domain truth.**

---

## 6. Vì sao chưa thể calibrate (và đó là đúng)

- Registry: **134 tham số, 0 `APPROVED-RESEARCH`, 0 `APPROVED-PRODUCTION`** (92 `Proposed` /
  38 `UnderReview` / 4 `Deferred`). Registry dòng 49 cấm dùng từ `APPROVED` trơ trọi.
- Chỉ **2/134** có member code (`tpo_bracket_minutes`, `value_area_percent`); 132 còn lại
  `NOT_PRESENT`.
- Muốn thăng hạng cần **dataset point-in-time có provenance + kết quả ngoài mẫu + Decision
  Record** — MRBS **§44** (Parameter Approval Registry, dòng 1806) + **§24.3** (dòng 720),
  Registry §1, ADR-005.
- Dataset đó **chưa tồn tại**: `DATASET_CONTRACTS.md` nêu 6 điều kiện, hiện **1, 3, 5, 6 chưa
  đạt**. Cửa sổ dữ liệu thật dài nhất mới **160 giây**.

---

## 7. Hàng chờ quyết định (chỉ chủ dự án / reviewer mở được)

| # | Mục | Loại | Chặn gì |
|---|---|---|---|
| **Q1** | **Execution brief** (`KDK_Claude_Continuation_Package_v1.0.zip`) không có trong workspace tại thời điểm đóng gói Stage 2 (`stage2_closure/README.md` §Missing input) | Cung cấp file | Định dạng báo cáo §15 |
| **Q2** | Phê duyệt 7 ADR (đang `AWAITING_OWNER_APPROVAL`) | Quyết định | Kiến trúc recorder chưa có hiệu lực |
| **Q3** | `SC-005..009` + `CONF-001` + `RECL-01/02/03` | `SPEC_CONFLICT` | Rearm, Acceptance, taxonomy DataQuality, tên `score`, output surface, M1 scope |
| **Q4** | Nghiệm thu Stage 2 (D1–D4) | Quyết định | Đóng tầng 1 |
| **Q5** | Vendor `.proto` cho template 152/155/157/158… | Ủy quyền | Mở khóa OI/settlement options |
| **Q6** | Domain spec Ch76/77/79 | Ủy quyền | 28 yêu cầu `AWAITING_DOMAIN_SPEC` |
| **Q7** | Task D (porting) | Ủy quyền | Chưa bắt đầu |
| **Q8** | `SEC-001` xoay credential | Vận hành | Bảo mật (`OBS-R8`: log cleartext password) |
| **Q9** | **Phê chuẩn `02A/02B/02D`** — 02B hiện vẫn `provisional` (`SUPERSESSION_REGISTER.md:144`) | Quyết định | `04A:58` đặt đây là **điều kiện cứng** của Phase B → chặn Chặng 1–3 |
| **Q10** | **Ủy quyền `04A` thành binding spec** — hiện *"Nothing here is authorized for implementation"* (`04A:3`) | Ủy quyền | Chặn Chặng 4 / Phase C |

---

## 8. Một câu tóm tắt

**Dự án đang gia cố tầng 1 — tầng có quyền hạn cao nhất trong tháp KDK — đã chứng minh xong khả
năng thu thập dữ liệu và đóng gói thành Stage 2 Closure Package chờ nghiệm thu. Các tầng 2–8 có
skeleton nhưng cố ý chưa calibrate vì chưa có dataset hợp lệ. Trụ Options (tầng 7) cần dựng lại.**
Kế hoạch: [`BAN_THAO.md`](BAN_THAO.md).
