---
title: "GCAE Implementation Bible — Single Source of Truth"
document_id: "GCAE-IMPLEMENTATION-BIBLE-SSOT"
document_version: "1.0.0"
language: "vi-VN"
product: "GC AuctionFlow Engine"
deployment_form: "Một Custom Indicator DLL chạy trong ATAS Ultra"
analysis_market: "GC COMEX Futures / Rithmic"
execution_market: "CFD riêng, thực thi thủ công"
current_phase: "Phase 1G CODE/TEST PASS — LIVE ACCEPTANCE PENDING"
status_model: "Living Specification"
---

# GCAE IMPLEMENTATION BIBLE

## Single Source of Truth để xây dựng toàn bộ GC AuctionFlow Engine

> **Mục đích:** Đây là tài liệu duy nhất IDE/Cursor phải tham chiếu khi phân tích, lập kế hoạch, viết mã, kiểm thử, chạy live gate, đóng phase và cập nhật tiến độ cho GCAE.
>
> **Hình thức:** Một file Markdown sống xuyên suốt vòng đời dự án. Tên file không đổi. Nội dung được cập nhật có kiểm soát sau mỗi closeout.
>
> **Ranh giới:** GCAE là hệ thống phân tích nhiều mô-đun, được đóng gói thành một DLL duy nhất trong ATAS. GCAE không tự đặt lệnh trên tài khoản CFD trong phạm vi hiện tại.

---

# 0. CÁCH IDE PHẢI DÙNG FILE NÀY

## 0.1 Một file, ba vùng quyền lực

Tài liệu này có ba vùng:

1. **VÙNG A — IMPLEMENTATION AUTHORITY**  
   Phần 0 đến Phần 19. Đây là nguồn chỉ đạo trực tiếp để code.

2. **VÙNG B — CURRENT IMPLEMENTATION LEDGER**  
   Phụ lục A. Đây là trạng thái triển khai mới nhất được nhúng nguyên văn.

3. **VÙNG C — EMBEDDED METHOD & ARCHITECTURE BASELINE**  
   Phụ lục B. Đây là kiến thức và kiến trúc nguồn từ v1.2, được nhúng nguyên văn để file này tự đầy đủ.

Khi có mâu thuẫn, thứ tự ưu tiên là:

```text
VÙNG A: Implementation Authority
→ VÙNG B: Current Implementation Ledger
→ source code + tests + closeout đã LOCKED
→ VÙNG C: Embedded v1.2 Baseline
```

Vùng C mô tả toàn bộ đích đến. Nó không tự cấp quyền triển khai mọi nội dung ngay lập tức.

## 0.2 Quy tắc "một lần và mãi mãi"

"Một lần và mãi mãi" không có nghĩa trạng thái dự án đứng yên. Nó có nghĩa:

- Luôn dùng cùng một tên file.
- Không tạo thêm một Implementation Bible cạnh tranh.
- Kiến trúc cốt lõi chỉ thay đổi qua quyết định governance rõ ràng.
- Tiến độ được cập nhật ngay trong file này sau mỗi closeout.
- Mọi thay đổi đều để lại Decision Log.
- IDE không phải đoán tài liệu nào mới nhất.

## 0.3 Những gì được phép thay đổi

Được thay đổi sau closeout hợp lệ:

- `current_phase` trong front matter.
- Bảng trạng thái hiện tại.
- Runtime/schema/policy versions.
- Trạng thái phase.
- Known limitations.
- Next authorized milestone.
- Decision Log và Change Log.
- Module contract khi semantics mới đã được người vận hành phê duyệt.

Không được thay đổi âm thầm:

- Data authority.
- Không-lookahead.
- Không bịa Bid/Ask.
- Snapshot immutability.
- Phase semantics đã LOCKED.
- GCAE analysis-first, không auto trade.
- GC/Rithmic là nguồn phân tích và CFD là nơi thực thi thủ công.

## 0.4 Dấu vân tay nguồn khi tạo bản 1.0.0

```text
Embedded master baseline SHA-256: bc0010a60fc734b3256d734775a2043d5dfaa5cc5b2a38480d36f7607b30f44d
Embedded implementation status SHA-256: a804369e292619840d64e4d32a0c0f31b406713a2fc74f6673473994522ee0f6
```

Nếu IDE thay phần nhúng, phải cập nhật hash và ghi Decision Log. Không sửa phần nhúng chỉ để làm đẹp.

## 0.5 Lệnh bắt buộc trước mỗi task

IDE phải làm đúng thứ tự:

```text
READ CURRENT STATE
→ IDENTIFY AUTHORIZED SCOPE
→ LIST LOCKED INVARIANTS
→ AUDIT SOURCE + TESTS
→ PLAN
→ IMPLEMENT MINIMAL DIFF
→ VERIFY
→ REVIEW SEMANTICS
→ LIVE GATE NẾU BẮT BUỘC
→ CLOSEOUT
→ UPDATE THIS FILE
```

Không được bắt đầu code chỉ vì tìm thấy một heading hấp dẫn trong phần baseline.

---

# 1. CURRENT STATE — NGUỒN SỰ THẬT HIỆN TẠI

## 1.1 Snapshot hiện tại

| Trường | Giá trị hiện tại |
|---|---|
| Current phase | **Phase 1G CODE/TEST PASS — LIVE ACCEPTANCE PENDING** |
| Assembly / Probe | `0.0.6` |
| Raw Event Recorder schema | `1.2.0` |
| Runtime snapshot schema | `0.14.0` |
| Profile snapshot schema | `1.0.2` |
| MBO live capability | **BLOCKED** |
| Auto trading | **OUT OF SCOPE** |
| Analysis source | GC Futures / Rithmic / ATAS |
| Execution source | CFD riêng, thủ công |

## 1.2 Các mốc đã LOCKED

```text
P0-07C3D  Live Trade Recorder                         PASS + LOCKED
P0-08A    Runtime Data Gate / GPS Card                PASS + LOCKED
Phase 1A  Primary TPO + Volume Profile                LOCKED
Phase 1B  Composite Profile Foundation                LOCKED
Phase 1C  Structural Reference Foundation             LOCKED
Phase 1D  Multi-Horizon Directional Context + OTF     LOCKED
Phase 1E  Auction Episode Observation                 LOCKED
Phase 1F  Acceptance/Re-entry Evidence Measurement    LOCKED
Phase 2A  Executed Orderflow Raw                      LOCKED
Phase 2B  Cluster Raw Feature Measurement             LOCKED
```

## 1.3 Có code nhưng chưa được gọi là LOCKED

```text
Phase 2C  Auction Efficiency Raw Evidence             CODE/TEST PASS — LIVE PENDING
Phase 2D  Acceptance/Re-entry Resolution Foundation   CODE/TEST PASS — LIVE PENDING
Phase 2E  Effort/Result Classifier Foundation          CODE/TEST PASS — LIVE PENDING
Phase 1G  Participation + Settlement + Thin            CODE/TEST PASS — LIVE PENDING
Phase 3A  FAR/AAC Thesis State-Machine Foundation      CODE/TEST PASS — LIVE PENDING
```

`CODE/TEST PASS` không đồng nghĩa:

- live pass,
- calibrated,
- executable,
- production-ready,
- có quyền phát lệnh.

## 1.4 Chưa bắt đầu

```text
Phase 2F   Trade Facilitation Index
Phase 3B   Signal Maturity
P0-07C4    Recorder extension được định nghĩa riêng
Các phase Entry / Risk / UI / Telegram / Scanner / Microstructure chưa được milestone hiện tại cấp quyền
```

## 1.5 Trạng thái hiệu chỉnh

Các nhóm sau vẫn phải fail-closed nếu chưa có evidence gate:

```text
Established / Failed Acceptance
Stable Reacceptance / Re-entry Failed
Aggression Effective / Ineffective
Potential Absorption / Exhaustion
Trade Facilitation Healthy / Failing
FAR/AAC Armed / Executable / Managing / Completed
FAST executable planning và risk sizing
Entry, stop, target, position size tự động
```

## 1.6 Work order hiện tại

Mặc định chỉ được làm:

1. Hoàn tất live acceptance cho Phase 1G.
2. Nếu live gate phát hiện defect, vá tối thiểu trong scope Phase 1G.
3. Chạy lại targeted + full regression theo closeout contract.
4. Chỉ commit/tag khi live gate PASS.
5. Cập nhật trạng thái Phase 1G trong chính file này.
6. Không tự nhảy sang phase khác nếu chưa có work order mới.

---

# 2. PRODUCT CONTRACT

## 2.1 GCAE là gì?

GCAE là một hệ thống phân tích đấu giá và dòng lệnh cho GC, được đóng gói thành một Custom Indicator DLL trong ATAS Ultra.

```text
GC Futures + Rithmic
→ kiểm tra năng lực và tính toàn vẹn dữ liệu
→ xây bản đồ đấu giá nhiều khung
→ xây Profile / Composite / Reference
→ theo dõi Auction Episode
→ đo Acceptance / Re-entry
→ đo Orderflow và Effort / Result
→ xây thesis FAR / AAC khi được hiệu chỉnh
→ đề xuất kế hoạch phân tích và quản trị
→ ánh xạ sang CFD
→ hiển thị / cảnh báo / ghi log / nghiên cứu
```

## 2.2 GCAE không phải gì?

- Không phải robot auto trade.
- Không phải mũi tên Buy/Sell từ Delta.
- Không phải một God Class.
- Không phải score tổng hợp bù được hard veto.
- Không phải DOM wall detector.
- Không phải máy đọc ý định "cá lớn".
- Không coi crossing là sweep.
- Không coi enum tồn tại là tính năng đã được phép dùng.

## 2.3 Một DLL, nhiều engine

Artifact duy nhất cài vào ATAS:

```text
GC.AuctionFlow.dll
```

Indicator entry point chỉ phụ trách:

- ATAS lifecycle,
- settings,
- callback admission,
- orchestration,
- rendering,
- health/status publication.

Logic nghiệp vụ phải nằm trong module độc lập, kiểm thử được ngoài ATAS.

---

# 3. HIẾN PHÁP KỸ THUẬT KHÔNG ĐƯỢC VI PHẠM

## 3.1 Data truth

1. Không có dữ liệu thì xuất `Unavailable`, `Partial`, `Degraded` hoặc `Invalid`.
2. Không dùng số 0 để thay cho dữ liệu không có.
3. Không dùng hướng nến hoặc tick rule để bịa aggressor.
4. Delta/CVD chỉ tính trên phần Bid/Ask được phân loại hợp lệ.
5. `Unknown` là dữ liệu trung thực, không phải lỗi cần che giấu.

## 3.2 Semantic truth

1. Raw measurement khác classifier.
2. Classifier khác thesis.
3. Thesis khác executable plan.
4. Executable plan khác đặt lệnh thật.
5. `NOT CALIBRATED` phải chặn state có quyền lực cao hơn.
6. Không một module nghiên cứu nào được tự thay đổi Production Thesis.

## 3.3 Time truth

1. Không lookahead.
2. Không dùng thanh đang hình thành để xác nhận completed-period state.
3. Không dựng lại Episode trước thời điểm module bắt đầu nếu không có historical event source hợp lệ.
4. Mỗi snapshot phải có timestamp, source horizon, coverage mode và revision.
5. Stale hoặc out-of-order input phải bị từ chối có log.

## 3.4 Lifecycle truth

1. Một active Episode cho mỗi `(PrimaryAuctionId, ReferenceId)`.
2. Crossing lặp lại cập nhật AttemptCount, không mở Episode mới vô hạn.
3. Auction change phải expire/freeze/clear theo contract.
4. Disable/re-enable không được tạo double-count hoặc hồi sinh state cũ trái phép.
5. Recalculation không được phát notification lặp.

## 3.5 Boundary truth

1. Module không đọc mutable internal state của module khác.
2. Giao tiếp bằng immutable/versioned snapshots.
3. Module đo lường không tự phát hard veto toàn hệ thống.
4. Hard veto tập trung trong policy có thẩm quyền.
5. Indicator shell không chứa thuật toán domain dài.

## 3.6 Research truth

1. Không magic number.
2. Không tối ưu threshold sau khi xem outcome rồi gọi là rule.
3. Mọi feature nghiên cứu phải có provenance.
4. So sánh với baseline đơn giản.
5. Chỉ promotion sau historical, replay, out-of-sample và live shadow phù hợp.

---

# 4. EVIDENCE AUTHORITY

## 4.1 Tier 1 — Executed evidence

Quyền cao nhất trong microstructure:

```text
Trade price / size / time
Bid executions / Ask executions / Unknown
Executed volume
Trade count
Delta / CVD trên classified subset
Footprint raw ledger
Price progress
Time between trades
```

## 4.2 Tier 2 — Displayed liquidity

```text
DOM depth
Displayed size
Queue position
Pulling / Stacking
MBO resting orders
Heatmap persistence
```

Tier 2 có thể bị rút, thay đổi hoặc không bao giờ khớp. Chỉ dùng cho execution condition và nghiên cứu.

## 4.3 Tier 3 — Model inference

```text
Iceberg candidate
Stops classification
MBO Sweep grouping
Absorption candidate
Exhaustion candidate
Market Power
Limit Tracing candidate
Spoofing candidate
```

Tier 3 không được phủ quyết Tier 1/AMT acceptance nếu chưa được hiệu chỉnh riêng.

## 4.4 Tháp quyền lực tổng thể

```text
Data Integrity
→ Structural Location
→ Auction Development
→ Acceptance / Reacceptance
→ Executed Orderflow
→ Research context
→ Entry tactics
→ Narrative / analogy
```

Tầng dưới không được lật ngược tầng trên bằng một indicator đẹp mắt.

---

# 5. CANONICAL DATA FLOW

## 5.1 Luồng trade duy nhất

```text
ATAS OnNewTrade / OnNewTrades
→ TradeStreamAtasMapper.MapNewTrade
→ canonical ExecutedTradeEvent
→ Phase 2A authoritative aggregate + per-price ledger
→ Phase 2B Cluster Raw
→ Phase 2C Auction Efficiency Raw
→ classifier / thesis modules về sau
```

Không tạo đường normalize trade thứ hai.

Cumulative callbacks không phải authoritative source cho executed totals trừ khi một policy mới được phê duyệt rõ.

## 5.2 Luồng profile

```text
ATAS candles / completed periods
→ time normalization
→ Primary TPO + Volume Profile
→ completed auction snapshot
→ Composite
→ References
→ Directional Context
→ Episode eligibility
```

## 5.3 Luồng Episode

```text
Confirmed reference
+ normalized live trade
→ Episode observation
→ attempts / excursions / re-entry geometry
→ Acceptance/Re-entry evidence
→ Resolution foundation
→ FAR/AAC state-machine foundation
```

## 5.4 Versioning tối thiểu

Mỗi snapshot có:

```text
Identity
PolicyVersion
SchemaVersion
StateVersion hoặc SnapshotVersion
InputFingerprint
EventRevision khi phù hợp
Timestamp
CoverageMode
DataQuality
KnownLimitations
```

Phiên bản chỉ tăng khi output có thay đổi ý nghĩa theo contract. Price-only refresh không được giả làm evidence revision mới nếu evidence không đổi.

---

# 6. THREADING, PERFORMANCE VÀ RECORDER

## 6.1 Callback rule

Callback ATAS phải ngắn, xác định và không block:

```text
validate tối thiểu
→ normalize
→ update in-memory state hoặc enqueue
→ return
```

Không làm trong callback:

- I/O đồng bộ nặng,
- HTTP đồng bộ,
- quét lịch sử lớn,
- serialize object graph khổng lồ,
- sleep/retry,
- lock dài.

## 6.2 Recorder architecture

```text
Market callback
→ bounded queue
→ background writer
→ batch / flush policy
→ versioned record
→ clean shutdown
```

Bắt buộc:

- backpressure/drop policy minh bạch,
- counter cho enqueue/write/drop/failure,
- atomic rotation hoặc file handoff,
- schema version,
- timestamps UTC và exchange/local mapping khi cần,
- instrument/contract/source/sequence,
- restart safety,
- không nuốt exception âm thầm.

## 6.3 Live-only data

Cần recorder live cho:

```text
DOM changes
MBO queue lifecycle
Pulling / Stacking
Order cancellation
một số Iceberg / Stops / Sweeps events
```

Không có recorder ngày đó thì không được giả vờ tái tạo đầy đủ lifecycle từ candles.

## 6.4 MBO guardrail

Trạng thái hiện tại:

```text
MBO = BLOCKED
```

Do đó IDE không được bật runtime semantics dựa trên:

- ExchangeOrderId,
- queue priority,
- MBO lifecycle,
- native iceberg,
- order-level pulling/stacking,
- MBO sweep,
- Limit Tracing.

Chỉ được làm khi milestone riêng xác nhận capability và live gate.

---

# 7. CAPABILITY MATRIX CONTRACT

Mỗi module tự khai báo dependency. Không có một cờ `DLL Ready` chung.

| Capability | Trạng thái chuẩn |
|---|---|
| Trades | UNKNOWN / INVALID / LIVE / HISTORICAL / BOTH |
| Bid/Ask | UNKNOWN / INVALID / LIVE / HISTORICAL / BOTH |
| Footprint | COMPUTABLE / NOT_COMPUTABLE |
| TPO | COMPUTABLE / NOT_COMPUTABLE |
| Volume Profile | COMPUTABLE / NOT_COMPUTABLE |
| Live DOM | AVAILABLE / UNAVAILABLE |
| Historical DOM | AVAILABLE / PARTIAL / UNAVAILABLE |
| MBO | AVAILABLE / PARTIAL / UNAVAILABLE / BLOCKED |
| Order IDs | AVAILABLE / UNAVAILABLE |
| Iceberg | NATIVE / INFERRED / UNAVAILABLE |
| Stops classification | AVAILABLE / PARTIAL / UNAVAILABLE |
| MBO sweeps | AVAILABLE / PARTIAL / UNAVAILABLE |
| Open Interest | UNKNOWN / EOD_ONLY / PARTIAL / VALIDATED |
| Replay fidelity | UNKNOWN / PARTIAL / VALIDATED |

Ví dụ gate:

```text
Auction Episode
→ Price + Trades + confirmed references

Aggression / Progress
→ classified Bid/Ask + timestamps

DOM persistence
→ repeated depth updates

MBO lifecycle
→ order-level events + stable order IDs
```

---

# 8. MODULE CONTRACTS

Mỗi module phải có cùng cấu trúc triển khai:

```text
Purpose
Authorized Inputs
Immutable Outputs
Status / Quality
Policy Version
Coverage Mode
Forbidden Conclusions
Lifecycle / Reset
Tests
Telemetry
```

## 8.1 Data Capability & Integrity

**Mục đích:** quyết định từng module có đủ dữ liệu để chạy hay không.

**Input:** connection, instrument, tick size, timestamps, trade stream, Bid/Ask, history state, DOM/MBO/OI/replay capability.

**Output:** `CapabilitySnapshot`, module gates, integrity failures.

**Trạng thái:** `Invalid`, `Degraded`, `Ready`.

**Cấm:** một module thiếu MBO làm tắt toàn bộ Profile/Orderflow nếu các dependency cốt lõi vẫn đủ.

**Test tối thiểu:** disconnect/reconnect, wrong instrument, timestamp jump, late history, stale snapshot, module independence.

## 8.2 Raw Event Recorder

**Mục đích:** lưu raw events có schema để nghiên cứu và tái xử lý.

**Authority:** recorder không quyết định thesis.

**Hiện tại:** P0-07C3D PASS + LOCKED; schema `1.2.0`.

**Cấm:** đổi schema không bump version; ghi đồng bộ nặng trong callback; mất dữ liệu không telemetry.

## 8.3 Contract / Roll / Participation

**Mục đích:** biết contract nào đang active, trạng thái roll, chất lượng participation và proximity settlement.

**Output tương lai/hiện tại tùy phase:** contract state, settlement tags, thin-participation state.

**Hiện tại:** Phase 1G code/test pass, live pending.

**Cấm:** `thin = vô nghĩa`; tự loại profile khỏi Composite; settlement tag tự tạo fade.

## 8.4 Primary TPO + Volume Profile

**Mục đích:** xây Primary Auction Map.

**Đã khóa:** Phase 1A.

**Invariant:** ATAS candle time chuẩn hóa UTC rồi New York theo policy; TPO 30 phút anchor 08:20 America/New_York; methodology difference với ATAS built-in được chấp nhận nếu deterministic.

**Output:** TPO/VP POC, VAH/VAL, high/low, completed/developing state.

**Cấm:** gọi POC là fair value tuyệt đối; dùng developing snapshot như completed reference.

## 8.5 Composite Profile

**Mục đích:** hợp nhất Auction theo cấu trúc, không theo N ngày cứng.

**Đã khóa:** Phase 1B.

**Input:** completed Primary profiles.

**Output:** confirmed/developing Composite snapshot.

**Cấm:** merge chỉ vì gần ngày; dùng fixed N-day; xóa thin profile tự động nếu chưa có policy hiệu chỉnh.

## 8.6 Structural References

**Mục đích:** sinh reference có identity, role, zone, confluence và lifecycle tối thiểu.

**Đã khóa:** Phase 1C.

**Current implemented source:** previous completed Primary, developing Primary theo policy, confirmed Composite; chỉ những nguồn đã được closeout xác nhận mới có quyền runtime.

**Cấm:** tự thêm Weekly/Monthly/IB/VWAP/Swing/nPOC/HVN/LVN vào runtime chỉ vì baseline có liệt kê đích đến.

## 8.7 Directional Context + OTF

**Mục đích:** mô tả Structural/Tactical context theo nhiều horizon.

**Đã khóa:** Phase 1D.

**OTF:** chỉ completed TPO periods. OTF Up khi low hiện tại không thấp hơn low trước; OTF Down khi high hiện tại không cao hơn high trước.

**Cấm:** OTF break = reversal; OTF = hard trend veto; dùng period đang chạy.

## 8.8 Auction Episode Observation

**Mục đích:** theo dõi toàn vòng đời tương tác giá-reference.

**Đã khóa:** Phase 1E.

**Input:** normalized live trades + confirmed eligible references.

**Output:** one active Episode per key, attempts, excursions, geometric re-entry observation, centerline metrics.

**Cấm:** acceptance conclusion, FAR/AAC, thesis, Buy/Sell, candle reconstruction trước khi bật.

## 8.9 Acceptance / Re-entry Evidence Measurement

**Mục đích:** đo raw outside/inside ratios, local POC displacement, geometric re-entry và maintenance evidence.

**Đã khóa:** Phase 1F measurement-only.

**Cấm:** mutate Episode; phát Established/Failed/Stable/ReentryFailed khi chưa calibrate.

## 8.10 Executed Orderflow Raw

**Mục đích:** authoritative executed trade ledger.

**Đã khóa:** Phase 2A.

**Output:** volume, trades, Ask/Bid/Unknown, classified Delta/CVD, per-price ledger, timing raw, episode aggregates.

**Cấm:** tick-rule fabrication; coi Unknown-only là Invalid; đường normalization thứ hai.

## 8.11 Cluster Raw

**Mục đích:** đo hình học raw trên Phase 2A ledger.

**Đã khóa:** Phase 2B.

**Output:** same-price/diagonal ratios, raw dominance, empirical midranks, visits/revisits, runs không bridge missing ticks.

**Cấm:** gọi raw dominance là imbalance đã hiệu chỉnh; stacked imbalance; Big Trade; absorption.

## 8.12 Auction Efficiency Raw

**Mục đích:** tách Effort vector và Result vector, xây raw relationships null-safe.

**Hiện tại:** Phase 2C code/test pass, live pending.

**Cấm:** EfficiencyScore, Effective/Ineffective, absorption/exhaustion, Trade Facilitation labels.

## 8.13 Acceptance/Re-entry Resolution Foundation

**Mục đích:** map evidence vào resolution state foundation.

**Hiện tại:** Phase 2D code/test pass, live pending.

**Cấm:** Established/Failed/Stable conclusion khi threshold chưa calibrate; FAR/AAC conclusion.

## 8.14 Effort/Result Classifier Foundation

**Mục đích:** host/snapshot/state space cho classifier tương lai.

**Hiện tại:** Phase 2E code/test pass, live pending.

**Cấm:** mọi classification có quyền lực khi state vẫn `NOT CALIBRATED`.

## 8.15 FAR / AAC Thesis Foundation

**Mục đích:** state-machine foundation cho hai thesis families.

**Hiện tại:** Phase 3A code/test pass, live pending.

**FAR direction:** outside Below → Long; outside Above → Short.

**AAC direction:** outside Above → Long; outside Below → Short.

**Cấm:** Armed/Executable/Managing/Completed; signal, entry, risk, alert action.

## 8.16 Trade Facilitation

**Mục đích:** so effort theo hướng đang thử với auction progress thực đạt.

**Hiện tại:** Phase 2F NOT STARTED.

**Yêu cầu trước code:** định nghĩa population, regime segmentation, baseline, zero-progress handling, calibration plan và authority boundary.

## 8.17 Signal Maturity

**Mục đích:** FAST / STANDARD / CONFIRMED.

**Hiện tại:** Phase 3B NOT STARTED.

**FAST default:** shadow-only; action alert, executable plan và risk sizing OFF cho đến validation.

## 8.18 Entry Policy

**Mục đích:** chọn Wait, Passive Limit, Marketable Limit, Market, Stop-Market hoặc Stop-Limit theo maturity và execution condition.

**Trạng thái:** chưa được milestone hiện tại cấp quyền.

**Cấm:** order type cố định cho mọi setup; entry trước invalidation; auto order call.

## 8.19 Invalidation

Bốn chiều:

```text
Price
Auction
Time
Context
```

Protective hard stop là lớp bảo vệ, không thay thế thesis invalidation.

## 8.20 Target / PLAR

**Mục đích:** xây target corridor từ reference và intermediate barriers.

**Cấm:** target theo R tùy ý; weekly target từ micro trigger không có horizon contract; POC/HVN/LVN là mục tiêu bắt buộc.

## 8.21 CFD Mapping

**Mục đích:** dịch GC analytical prices sang CFD execution prices.

**Input:** basis, spread, slippage, contract, CFD Bid/Ask, freshness.

**Cấm:** copy raw GC price sang CFD; dùng CFD order book thay COMEX; tiếp tục khi basis mất ổn định.

## 8.22 Risk

**Nguyên tắc:** invalidation trước, position size sau.

**Cấm:** chọn lot trước rồi kéo stop; tăng risk để cứu setup; score bù data invalid.

## 8.23 Trade Management + Expiry

Mọi action phải có reason liên kết trực tiếp với Thesis Contract. Không đổi horizon để biện hộ.

## 8.24 Hard Veto

Hard veto tập trung, không bị score override.

Ví dụ:

```text
DATA INVALID
wrong contract / roll unknown
no invalidation
mapping stale
risk locked
spread/basis abnormal theo policy
```

## 8.25 UI / Alerts / Telegram

**Nguyên tắc:** UI phản ánh truth, không tạo truth.

Module mới mặc định OFF; diagnostics OFF; alerts chỉ state transition có ý nghĩa; dedup theo stable identity + state version.

## 8.26 Historical Scanner / Research Harness

**Mục đích:** tạo dataset Episode và raw feature để calibration, không đợi nhiều tháng live.

**Cấm:** scanner dùng hindsight field; replay chứng minh edge cuối cùng; đổi rule giữa sample.

## 8.27 ATAS Ultra Microstructure Lab

Bao gồm Smart Tape, Big Trades, DOM/MBO, Pulling/Stacking, Iceberg, Stops, Sweeps, Market Power, Dynamic Levels và Limit Tracing.

**Trạng thái chung:** capability-gated, research-first. Không module black box nào tự tạo/hủy thesis lõi.

## 8.28 Event / Macro / OI / COT

External context chỉ là context, risk review hoặc event veto theo policy. OI phải capability-validated; EOD OI/COT không phải scalp trigger.

---

# 9. STATE-MACHINE CONTRACTS

## 9.1 Data

```text
Invalid
Degraded
Ready
```

Module readiness độc lập. Global state không được tự nâng `Degraded` thành `Ready` chỉ vì một module riêng sẵn sàng.

## 9.2 Episode

Current observation semantics gồm interaction, attempts, developing, geometric re-entry, unresolved/expired theo phase đã khóa. Future reserved states không được emit nếu chưa closeout.

## 9.3 Acceptance / Re-entry

```text
Observation
→ Evidence
→ Resolution
→ Thesis
```

Không bỏ qua tầng.

## 9.4 FAR

```text
Price thử ngoài reference
→ không xây acceptance bền vững ngoài
→ quay vào vùng cũ
→ maintenance trong vùng cũ
→ effort theo excursion không tạo result tương xứng
→ FAR candidate
```

## 9.5 AAC

```text
Price thử ngoài reference
→ time/volume/trades phát triển ngoài
→ local POC/value dịch theo hướng discovery
→ old value reclaim thất bại
→ AAC candidate
```

## 9.6 Signal maturity

```text
FAST       = ít confirmation, shadow-only mặc định
STANDARD   = evidence cân bằng
CONFIRMED  = evidence trưởng thành hơn, entry thường kém hơn
```

Maturity không được bù Data Invalid, no invalidation hoặc contrary acceptance.

---

# 10. SETTINGS VÀ DEFAULTS

## 10.1 Default-off rule

Module mới, diagnostics, overlay nghiên cứu và alerts hành động phải mặc định OFF trừ khi milestone nói khác.

## 10.2 Feature flag không vượt capability

`EnableMboResearch=true` không có nghĩa MBO available.

```text
Feature flag
AND capability gate
AND policy authorization
AND phase status
→ module may run
```

## 10.3 Settings governance

Mỗi setting mới phải có:

- tên rõ,
- default,
- phạm vi,
- policy owner,
- validation,
- persistence behavior,
- backward compatibility,
- test.

Không thêm setting "cho tương lai" nếu chưa có consumer được milestone cho phép.

---

# 11. TEST CONTRACT CHO MỌI MODULE

Mỗi module phải có test cho các nhóm áp dụng:

1. Identity stability.
2. Version/revision increment đúng.
3. Same input reuse hoặc no-op publish.
4. Changed input rebuild.
5. Duplicate admission.
6. Out-of-order/stale rejection.
7. Disable/re-enable.
8. Auction/epoch reset.
9. Contract change.
10. Zero denominator/null safety.
11. Unknown/unavailable semantics.
12. Symmetry Up/Down hoặc Above/Below.
13. Deterministic replay.
14. Snapshot immutability.
15. Module independence.
16. Default OFF.
17. Diagnostics OFF.
18. No unauthorized overlay/alert.
19. Schema compatibility.
20. Regression of all LOCKED phases.

## 11.1 Build gate

```text
0 errors
0 warnings
```

Không che warning bằng suppression diện rộng nếu chưa giải thích.

## 11.2 Full suite gate

Closeout phải chạy full suite theo milestone, thường ít nhất hai lần khi yêu cầu deterministic confidence.

## 11.3 Live gate

Automated pass không thay live gate khi live behavior là acceptance criterion.

Không săn ép mọi branch hiếm bằng thao tác thị trường nhân tạo nếu closeout chấp nhận automated-only coverage. Phải ghi limitation trung thực.

---

# 12. VALIDATION LADDER

```text
Stage 1  Mechanism validation
Stage 2  Historical event study
Stage 3  Entry-policy comparison
Stage 4  MAE/MFE/target study
Stage 5  Walk-forward / out-of-sample
Stage 6  Live shadow
Stage 7  Small-risk production
```

Không nhảy tầng chỉ vì chart nhìn đẹp.

Classifier/threshold promotion tối thiểu cần:

- frozen definition,
- source provenance,
- sample definition,
- session/regime segmentation,
- transaction costs khi liên quan,
- holdout/out-of-sample,
- stability analysis,
- false-positive review,
- live behavior comparison,
- rollback plan.

---

# 13. PHASE ROADMAP VÀ DEPENDENCY-SAFE ORDER

## 13.1 Đã khóa

```text
P0 recorder/data gate
P1A profile
P1B composite
P1C references
P1D directional/OTF
P1E episode observation
P1F evidence measurement
P2A executed orderflow raw
P2B cluster raw
```

## 13.2 Cần closeout live

Khuyến nghị dependency-safe:

```text
Phase 1G
→ Phase 2C
→ Phase 2D
→ Phase 2E
→ Phase 3A
```

Thứ tự thực tế vẫn cần work order của người vận hành. Không tự chạy hàng loạt.

## 13.3 Tiếp theo sau closeout

```text
Phase 2F  Trade Facilitation
Phase 3B  Signal Maturity
Phase 3C+ Entry / Invalidation / Target / CFD / Risk
Phase 4   UI / Alerts / Telegram
Phase 5   Scanner / Calibration
Phase 6   ATAS Microstructure
Phase 7   Advanced Research
```

Mỗi phase phải có spec hẹp, không triển khai cả dòng roadmap trong một diff khổng lồ.

---

# 14. CURSOR EXECUTION PROTOCOL

## 14.1 Phản hồi đầu task

IDE phải xuất:

```text
CURRENT PHASE
AUTHORIZED SCOPE
LOCKED DEPENDENCIES
FILES TO READ
FILES EXPECTED TO CHANGE
FILES FORBIDDEN TO CHANGE
INVARIANTS
TEST PLAN
LIVE GATE
COMMIT/TAG POLICY
```

## 14.2 Khi code

- Diff tối thiểu.
- Không opportunistic refactor.
- Không sửa LOCKED semantics để làm test mới dễ pass.
- Không thêm classifier hoặc threshold ngoài scope.
- Không thêm future enum/field/setting nếu không cần.
- Không đọc output trực quan của indicator khác như raw data authority.

## 14.3 Khi phát hiện mâu thuẫn

IDE phải dừng với:

```text
CONFLICT DETECTED

Request:
Conflicting authority:
Locked semantic at risk:
Minimum safe option:
Decision required:
```

## 14.4 Khi hoàn tất code/test nhưng chưa live

Chỉ được báo:

```text
CODE/TEST PASS — LIVE ACCEPTANCE PENDING
```

Không báo `DONE`, `FINAL PASS`, `LOCKED` hoặc `PRODUCTION READY`.

## 14.5 Khi closeout

Báo cáo bắt buộc:

- exact diff,
- tests,
- live observations,
- schemas/policies,
- source/deployed hashes,
- limitations,
- forbidden semantics audit,
- commit/tag,
- cập nhật file Bible này.

---

# 15. CHANGE CONTROL TRONG MỘT FILE

## 15.1 Sau mỗi closeout

Cập nhật theo thứ tự:

1. Front matter `current_phase`.
2. Phần 1 Current State.
3. Module contract tương ứng nếu status thay đổi.
4. Phase roadmap.
5. Decision Log.
6. Phụ lục A bằng nội dung status mới nhất.
7. Hash của status nhúng.
8. Chạy integrity checker của file.

## 15.2 Không xóa lịch sử quyết định

Khi một khái niệm bị supersede:

- giữ alias/history,
- ghi decision,
- chỉ ra replacement,
- thêm migration/test requirement.

## 15.3 Schema change

Mọi schema change cần:

```text
old version
new version
reason
field diff
backward compatibility
reader behavior
migration behavior
tests
```

## 15.4 Semantic change

Semantic change của phase LOCKED cần authorization riêng, impact audit và regression full-stack. Không gọi là refactor nếu output behavior đổi.

---

# 16. DEFINITION OF DONE TOÀN HỆ THỐNG

GCAE chỉ được gọi là hệ thống hoàn chỉnh khi:

## Core

- Data Gate fail-closed.
- Profile/Composite/Reference deterministic.
- Directional/Episode/Evidence/Resolution đúng lifecycle.
- Orderflow/Cluster/Effort-Result trung thực.
- FAR/AAC được calibrate và không lookahead.

## Planning

- Signal maturity validated.
- Entry policy có study.
- Invalidation đa chiều.
- PLAR targets và barriers.
- CFD mapping robust.
- Risk and drawdown guards.

## Operations

- UI rõ và không gây hiểu nhầm.
- Alerts deduplicated.
- Health/disconnect visible.
- Recorder bền vững.
- Schema/documentation synchronized.

## Research

- Historical scanner.
- Replay audit.
- Walk-forward.
- Live shadow.
- Research ledger/provenance.
- Promotion/rejection workflow.

## Deployment

- Một DLL.
- Build sạch.
- Hash source/deployed khớp.
- ATAS restart/load/unload ổn định.
- Không memory/thread leak nghiêm trọng.
- Roll/contract handling.
- Known limitations được công bố.

---

# 17. DECISION LOG

## D-SSOT-001 — Chuyển sang một Implementation Bible

```text
Decision: Dùng file này làm single source of truth cho code và tiến độ.
Reason: v1.2 giàu kiến thức nhưng chứa cả đích đến tương lai; status riêng dễ bị bỏ sót.
Effect: Baseline và status được nhúng vào cùng file; Vùng A kiểm soát cách diễn giải.
```

## D-SSOT-002 — Master baseline vẫn được bảo toàn

```text
Decision: Nhúng v1.2 nguyên văn thay vì sửa nội dung gốc.
Reason: Bảo toàn kiến thức và audit trail.
Effect: Vùng C không tự cấp quyền implementation.
```

## D-SSOT-003 — Living status

```text
Decision: Sau mỗi phase closeout, cập nhật trạng thái ngay trong cùng file.
Reason: IDE luôn thấy thực tại trước khi đọc roadmap tương lai.
```

---

# 18. TASK TEMPLATE DÙNG TRỰC TIẾP

```text
TASK ID:
MILESTONE:
CURRENT BIBLE VERSION:
CURRENT PHASE:

GOAL:

AUTHORIZED SCOPE:

OUT OF SCOPE:

LOCKED DEPENDENCIES:

INPUT CONTRACTS:

OUTPUT CONTRACTS:

STATE / QUALITY SEMANTICS:

RESET / LIFECYCLE:

SCHEMA / POLICY VERSION:

FILES EXPECTED TO CHANGE:

FILES FORBIDDEN TO CHANGE:

TARGETED TESTS:

FULL REGRESSION:

LIVE GATE:

FORBIDDEN LABELS / CONCLUSIONS:

DEPLOYMENT:

CLOSEOUT REQUIREMENTS:
```

---

# 19. GLOSSARY VẬN HÀNH

```text
LOCKED: code/test/live closeout đã hoàn tất theo gate và semantics được bảo vệ.
CODE/TEST PASS: build và automated tests pass, chưa thay live acceptance.
LIVE ACCEPTANCE PENDING: phải quan sát live trước closeout.
NOT CALIBRATED: state/threshold chưa có quyền kết luận.
NOT AUTHORIZED: milestone hiện tại không cho dùng.
BLOCKED: capability/path không thể sử dụng trong môi trường hiện tại.
PARTIAL: một phần dữ liệu hữu ích có thật, phần còn lại unavailable.
INVALID: dữ liệu đầu vào làm phân tích không còn hợp lệ.
LIVE_ONLY: không tuyên bố lịch sử trước khi module bắt đầu nhận event.
Immutable snapshot: output không bị mutate sau publication.
Input fingerprint: dấu vân tay xác định input có đổi ý nghĩa hay không.
Policy version: phiên bản semantics/rules của module.
Schema version: phiên bản cấu trúc dữ liệu serialize/runtime.
```

---

# PHỤ LỤC A — CURRENT IMPLEMENTATION LEDGER, NHÚNG NGUYÊN VĂN

> Vùng này được cập nhật sau mỗi closeout. Trong trường hợp mâu thuẫn, Phần 0–19 quyết định cách diễn giải.

# Implementation Status

| Field | Value |
|-------|--------|
| Current phase | **Phase 1G CODE/TEST PASS — LIVE ACCEPTANCE PENDING** |
| Probe version | **0.0.6** (unchanged) |
| RawEventRecorderSchemaVersion | **1.2.0** |
| Runtime snapshot schema | **0.14.0** (Phase 1G: Participation — SettlementProximity + ThinParticipation) |
| Profile snapshot schema | **1.0.2** (Completed TPO period feed) |
| Composite policy | **COMPOSITE_POLICY_V1** (unchanged) |
| Reference policy | **REFERENCE_POLICY_V1** (unchanged) |
| Overlay policy | **REFERENCE_OVERLAY_POLICY_V1** (unchanged) |
| Directional policy | **DIRECTIONAL_CONTEXT_POLICY_V1** (unchanged) |
| Episode policy | **AUCTION_EPISODE_POLICY_V1** (unchanged) |
| Evidence policy | **ACCEPTANCE_REENTRY_EVIDENCE_POLICY_V1** (unchanged) |
| Orderflow policy | **EXECUTED_ORDERFLOW_POLICY_V1** (unchanged) |
| Cluster Raw policy | **CLUSTER_RAW_FEATURE_POLICY_V1** (unchanged) |
| Auction Efficiency policy | **AUCTION_EFFICIENCY_EVIDENCE_POLICY_V1** |
| P0-07C3D | **PASS + LOCKED** |
| P0-08A | **PASS + LOCKED** |
| Phase 1A | **LOCKED** — `gcae-p1a-primary-tpo-volume-profile-pass` |
| Phase 1B | **LOCKED FINAL PASS** — `gcae-p1b-composite-profile-foundation-pass` @ `787d0ba` |
| Phase 1C | **LOCKED FINAL PASS WITH DOCUMENTED LIVE COVERAGE LIMITATION** — `gcae-p1c-structural-reference-foundation-pass` @ `cdb2974` |
| Phase 1D | **LOCKED FINAL PASS WITH DOCUMENTED LIVE DIAGNOSTIC COVERAGE LIMITATION** — `gcae-p1d-multi-horizon-directional-context-pass` @ `dcfea72` |
| Phase 1E | **LOCKED FINAL PASS WITH DOCUMENTED LIVE STATE-MACHINE COVERAGE LIMITATION** — `gcae-p1e-auction-episode-observation-pass` @ `9772e48` |
| Phase 1F | **LOCKED FINAL PASS WITH DOCUMENTED LIVE EVIDENCE-LIFECYCLE COVERAGE LIMITATION** — `gcae-p1f-acceptance-reentry-evidence-measurement-pass` @ `bd892ea` |
| Phase 2A | **LOCKED FINAL PASS WITH DOCUMENTED LIVE RAW-FEATURE COVERAGE LIMITATION** — `gcae-p2a-executed-orderflow-raw-feature-foundation-pass` @ `1603dfa` |
| Phase 2B | **LOCKED FINAL PASS WITH DOCUMENTED LIVE CLASSIFIED-CLUSTER COVERAGE LIMITATION** — `gcae-p2b-cluster-raw-feature-measurement-pass` |
| Phase 2C | **CODE/TEST PASS — LIVE ACCEPTANCE PENDING** — Auction Efficiency Raw Evidence (`AUCTION_EFFICIENCY_EVIDENCE_POLICY_V1`) |
| Phase 2D | **CODE/TEST PASS — LIVE ACCEPTANCE PENDING** — Acceptance/Re-entry Resolution (`ACCEPTANCE_REENTRY_RESOLUTION_POLICY_V1`) |
| Phase 2E | **CODE/TEST PASS — LIVE ACCEPTANCE PENDING** — Effort vs Result Classifier (`EFFORT_RESULT_CLASSIFIER_POLICY_V1`) |
| Phase 1G | **CODE/TEST PASS — LIVE ACCEPTANCE PENDING** — Participation Regime: Settlement Proximity Tags + Thin Participation Classifier |
| Phase 2F | **NOT STARTED** — Trade Facilitation Index (§23.4) |
| Phase 3A | **CODE/TEST PASS — LIVE ACCEPTANCE PENDING** — FAR + AAC Thesis State Machine Foundation (`FAR_THESIS_POLICY_V1`, `AAC_THESIS_POLICY_V1`) |
| Phase 3B | **NOT STARTED** — Signal Maturity (§29) |
| P0-07C4 | **NOT STARTED** |

## Phase 1D final closeout (2026-07-24)

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 399 passed / 0 failed / 0 skipped; 0 errors / 0 warnings |
| Live core gate | **PASS** — GCQ6 / Rithmic Live |
| Final verdict | **FINAL PASS WITH DOCUMENTED LIVE DIAGNOSTIC COVERAGE LIMITATION** |
| Structural state live | **PASS** — CONFLICTED published |
| Tactical Developing live | **PASS** — DOWNDISCOVERY (DEVELOPING) |
| OTF descriptive live | **PASS** — DEVELOPINGDOWN |
| Price location live | **PASS** — InsideValue |
| Data Gate independence | **PASS** — DATA DEGRADED while Directional READY |
| MBO | **BLOCKED** |
| Runtime schema | `0.5.0` |
| Policy | `DIRECTIONAL_CONTEXT_POLICY_V1` |
| Assembly | `0.0.6` (unchanged) |
| OTF confirmation | **NOT CALIBRATED** — ConfirmedUp/ConfirmedDown reserved |
| Final DLL SHA-256 | `9976E848578B9057503EC0D8A866C1593CED189EDC5EC04563940F605F0C6AFB` (source = deployed) |
| Tag | `gcae-p1d-multi-horizon-directional-context-pass` |
| Phase 1E | **authorized separately — see Phase 1E section** |

## Phase 1E final closeout (2026-07-24) — LOCKED

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 439 passed / 0 failed / 0 skipped; 0 errors / 0 warnings |
| Live trade admission | **PASS** — left AWAITING TRADES after wiring fix (D-P1E-002) |
| Live natural episodes | **PASS** — ACTIVE EPISODES: 4; LATEST DEVELOPING |
| Centerline live | **PASS** — PreviousPrimaryVpoc @ 4051.2; CENTERLINE; AttemptCount 0; max excursion 60 ticks; Direction DOWN |
| Final verdict | **FINAL PASS WITH DOCUMENTED LIVE STATE-MACHINE COVERAGE LIMITATION** |
| History mode | **LIVE_ONLY** — no candle reconstruction of pre-start activity |
| Runtime schema | `0.6.0` |
| Policy | `AUCTION_EPISODE_POLICY_V1` |
| Assembly | `0.0.6` (unchanged) |
| Final DLL SHA-256 | `924DB65C4D719D831926C81392AF600A332CD6BFF81401C5B6FC9E30CDFACBC2` (source = deployed) |
| Tag | `gcae-p1e-auction-episode-observation-pass` |
| Phase 1F | **authorized separately — see Phase 1F section** |

### Documented live state-machine coverage limitation (accepted)

Focused live gate proved Episode PARTIAL publication, live trade admission, Confirmed-reference eligibility, natural active episodes, and Centerline observation (AttemptCount 0; side excursion tracked). Repeated-attempt / geometric re-entry / auction-expiry / retirement / disable-reenable / stale-order / exact revision / history-overlap / LocalPoc-tie transitions were **not** all manually observed live and remain covered by deterministic automated tests only. This does not alter Episode semantics and does not require further operator-manufactured crossings.

### Live AWAITING TRADES defect (fixed before closeout)

- **Observed:** `TRADES: OBSERVED` while `EPISODES: AWAITING TRADES` with 16 eligible references.
- **Root cause:** `OnNewTrades` gated Episode behind `EnableTradeStreamProbe` (default OFF).
- **Fix (D-P1E-002):** normalize once; Episode admission always; probe gates only for probe enqueue.

### Phase 1E present (locked)

- Event-driven Auction Episode Observation from normalized `NewTradeObservation` (`OnNewTrade` / `OnNewTrades`)
- Trade admission independent of Trade Stream Probe enablement
- Confirmed Previous Primary + Confirmed Composite references only (Developing excluded)
- One active episode per `(PrimaryAuctionId, ReferenceId)`; AttemptCount on boundary outside transitions
- Centerline: CrossCount + side excursions; never OutsideAttempt/ReentryDeveloping; AttemptCount stays 0
- Boundary geometric ReentryDeveloping only (no Acceptance)
- Per-auction dedup ledger; Primary Auction change → Expired + ledger clear
- GPS/card rows; diagnostics OFF by default; module default OFF
- No Episode overlay, alerts, Sweep/FAR/AAC/Long-Short/thesis

### Explicitly deferred (Phase 1F+)

- Acceptance / stable re-entry Resolution
- Approach distance / intra-auction calibrated reset
- FAR / AAC / Orderflow interpretation / Thesis / Entry / Risk
- Episode chart overlay / ATAS alerts
- Exact historical trade reconstruction (unavailable on chart load)

## Phase 1F final closeout (2026-07-24) — LOCKED

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 458 passed / 0 failed / 0 skipped; 0 errors / 0 warnings |
| Focused live gate | **PASS** — GCQ6 / Rithmic Live |
| UpperBoundary evidence live | **PASS** — PreviousPrimaryTpoVah @ 4063.9; UPPER BOUNDARY; REENTRYDEVELOPING AttemptCount 1; max excursion 29 ticks |
| Finite ratios live | **PASS** — time 0.4242 / volume 0.4479 / trade 0.4404 (all in [0,1]) |
| Geometric re-entry live | **PASS** — REENTRY OBS GEOMETRICREENTRY; ACCEPTANCE OBS UNRESOLVED |
| Local POC displacement live | **PASS** — −8 ticks |
| Observer-only | **PASS** — Episode remained REENTRYDEVELOPING; no Stable Reacceptance; no Resolution mutation |
| Final verdict | **FINAL PASS WITH DOCUMENTED LIVE EVIDENCE-LIFECYCLE COVERAGE LIMITATION** |
| History mode | **LIVE_ONLY** (inherited from Episode) — no candle reconstruction |
| Runtime schema | `0.7.0` |
| Policy | `ACCEPTANCE_REENTRY_EVIDENCE_POLICY_V1` |
| Assembly | `0.0.6` (unchanged) |
| Final DLL SHA-256 | `1FDEBBB3E4E94497157FF6FA7D621760028AA516497BAB8800D88CF5C9E07249` (source = deployed) |
| Tag | `gcae-p1f-acceptance-reentry-evidence-measurement-pass` |
| Acceptance/Re-entry Resolution | **NOT STARTED** |
| FAR/AAC | **NOT STARTED** |
| Phase 2 Executed Orderflow | **authorized separately — see Phase 2A section** |

## Phase 2A final closeout (2026-07-24) — LOCKED

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 469 passed / 0 failed / 0 skipped; 0 errors / 0 warnings |
| Focused live gate | **PASS** — GCQ6 / Rithmic Live |
| Ordinary trade admission | **PASS** — Trade Stream Probe OFF; Recorder OFF permitted |
| Raw volume / trade count | **PASS** — EXECUTED VOLUME 113; TRADES 103 |
| Unknown-aggressor accounting | **PASS** — Unknown 113; Ask/Bid unavailable; no fabricated Ask/Bid |
| Classified Delta/CVD safety | **PASS** — CLASSIFIED DELTA 0; CLASSIFIED CVD 0 (classified subset empty) |
| Aggressor coverage | **PASS** — 0 finite and in [0,1] |
| Coverage mode live | **PASS** — LIVEONLYMIDAUCTION |
| Episode Orderflow | **PASS** — EPISODE ORDERFLOW PARTIAL observed |
| Data Gate independence | **PASS** — DATA DEGRADED remains independent |
| MBO | **BLOCKED** |
| Final verdict | **FINAL PASS WITH DOCUMENTED LIVE RAW-FEATURE COVERAGE LIMITATION** |
| History mode | **LIVE_ONLY** — no candle / ATAS visual reconstruction |
| Runtime schema | `0.8.0` |
| Policy | `EXECUTED_ORDERFLOW_POLICY_V1` |
| Assembly | `0.0.6` (unchanged) |
| Final DLL SHA-256 | `F92538852AD2478002F6FCB89B052FC9ACBD3773746F733C31548BF77B5356D2` (source = deployed) |
| Tag | `gcae-p2a-executed-orderflow-raw-feature-foundation-pass` |
| Phase 2B Trade Facilitation | **NOT STARTED** |
| Effort vs Result | **NOT STARTED** |
| Acceptance/Re-entry Resolution | **NOT STARTED** |
| FAR/AAC | **NOT STARTED** |

## Phase 2C code/test (2026-07-25) — LIVE ACCEPTANCE PENDING

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 497 passed ×2 / 0 failed / 0 skipped; Probe/Recorder 183 green; 0 errors / 0 warnings |
| Live acceptance | **REQUIRED** — focused gate pending |
| Commit/tag | **HOLD** until live acceptance |
| Runtime schema | `0.10.0` |
| Policy | `AUCTION_EFFICIENCY_EVIDENCE_POLICY_V1` |
| Assembly | `0.0.6` (unchanged) |
| Source/deployed DLL SHA-256 | `A22FFA7250AC89A26CEC4C92AF5FA81897B4B2ACAC7C3AE58BC03733439EFAA2` (exact match) |
| Effort / Result | **raw evidence vectors only** — classification NOT CALIBRATED |
| History | **LIVE_ONLY** |
| Aggressor | may remain Unknown-only (Partial) |
| Effort vs Result classifier | **NOT STARTED** |
| Trade Facilitation | **NOT STARTED** |
| Absorption / Exhaustion | **NOT STARTED** |
| Resolution / FAR / AAC | **NOT STARTED** |

### Phase 2C present (code/test)

- Immutable Effort + Result vectors from Phase 2A/2B/1E/1F/Profile
- Descriptive raw progress-per-unit relationships (null-safe; no EfficiencyScore)
- Current-auction / active / closed Episode scopes; fingerprint-gated rebuild
- Module default OFF; GPS rows; diagnostics OFF by default; no overlay/alerts
- Does not mutate Orderflow/Cluster/Profile/Composite/Reference/Directional/Episode/Evidence

### Explicitly deferred after Phase 2C

- EffortResultBalanced / AggressionEffective/Ineffective
- PotentialPassiveAbsorption / PotentialExhaustion
- TradeFacilitationHealthy / TradeFacilitationFailing
- Acceptance/Re-entry Resolution / FAR / AAC / Thesis / Entry / Risk

## Phase 2D code/test (2026-07-26) — LIVE ACCEPTANCE PENDING

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 544 passed / 0 failed / 0 skipped; 0 errors / 0 warnings |
| Live acceptance | **REQUIRED** — focused gate pending |
| Commit/tag | **HOLD** until live acceptance |
| Runtime schema | `0.11.0` |
| Policy | `ACCEPTANCE_REENTRY_RESOLUTION_POLICY_V1` |
| Assembly | `0.0.6` (unchanged) |
| Source/deployed DLL SHA-256 | `981F5177721926D83BFFB228D542A0494E7536864D0C69670D6D07DED0DFAE48` (exact match) |
| AcceptanceResolution | Early/Developing pass-through; Unresolved/Established/Failed → **NOT CALIBRATED** |
| ReentryResolution | GeometricReentry/Developing pass-through; Unresolved/Stable/Failed → **NOT CALIBRATED** |
| Overall conclusion | Always **NOT CALIBRATED** — FAR/AAC calibrated thresholds NOT AUTHORIZED |
| History | **LIVE_ONLY** (inherited) |
| New Phase 2D tests | 47 tests (A01–K04); total 544 |

### Phase 2D present (code/test)

- `AuctionResolutionHost` — fingerprint-gated rebuild from `AcceptanceReentryEvidenceSetSnapshot`
- `AuctionResolutionSnapshot` / `AuctionResolutionSetSnapshot` — immutable versioned snapshots
- `ResolutionIdentity.BuildFromEvidenceId()` — `ARES|{sanitized}|{policyVersion}` format
- `ResolutionInputFingerprint` — IEquatable struct gating rebuild on evidence revision change
- `AuctionResolutionPolicyConfig` — `ACCEPTANCE_REENTRY_RESOLUTION_POLICY_V1`; 15 limitation constants
- Resolution enums: `ResolutionModuleState`, `AcceptanceResolutionState`, `ReentryResolutionState`, `AuctionResolutionConclusion`, `ResolutionDataQuality`
- GPS card rows via `AuctionGpsCardMapper.BuildAuctionResolutionLines()`; showDiagnostics-gated ID/version rows
- RuntimeSnapshot schema bumped `0.10.0` → `0.11.0`; `AuctionResolution` property on `GcaeRuntimeSnapshot`
- Indicator: `EnableAcceptanceReentryResolution` / `ShowAuctionResolutionDiagnostics` settings; module default OFF
- GPS diagnostics list: `RESOLUTION:` status row added (9 rows total; was 8)

### Explicitly deferred after Phase 2D

- Established acceptance / Failed acceptance calibration (gated: NOT CALIBRATED)
- Stable re-acceptance / Re-entry failed calibration (gated: NOT CALIBRATED)
- FAR / AAC overall conclusion (NOT CALIBRATED)
- Thesis / Entry / Risk phases
- Overlay alerts

## Phase 2E code/test (2026-07-26) — LIVE ACCEPTANCE PENDING

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 592 passed / 0 failed / 0 skipped; 0 errors / 0 warnings |
| Live acceptance | **REQUIRED** — focused gate pending |
| Commit/tag | **HOLD** until live acceptance |
| Runtime schema | `0.12.0` |
| Policy | `EFFORT_RESULT_CLASSIFIER_POLICY_V1` |
| Assembly | `0.0.6` (unchanged) |
| Source/deployed DLL SHA-256 | `848E0D9417E65051ED70889E9E36CFB1F6326FAE830CF5357CCB6F2708991BB4` (exact match) |
| All classification states | **NOT CALIBRATED** — EffortResultBalanced/AggressionEffective/Ineffective/Absorption/Exhaustion/TradeFacilitationHealthy/Failing reserved |
| FAR/AAC/Thesis/Entry | **NOT AUTHORIZED** in Phase 2E |
| History | **LIVE_ONLY** |
| GPS rows | 10 (was 9); "EFFORT RESULT:" row added |
| New Phase 2E tests | 48 tests (A01–K05); total 592 |

### Phase 2E present (code/test)

- `EffortResultClassifierHost` — fingerprint-gated rebuild from `AuctionEfficiencyEvidenceSetSnapshot`
- `EffortResultClassificationSnapshot` / `EffortResultClassificationSetSnapshot` — immutable versioned snapshots
- `EffortResultIdentity.BuildFromEfficiencyId()` — `ERCL|{sanitized}|{policyVersion}` format
- `EffortResultInputFingerprint` — IEquatable struct gating rebuild on efficiency InputFingerprint change
- `EffortResultClassifierPolicyConfig` — `EFFORT_RESULT_CLASSIFIER_POLICY_V1`; 12 limitation constants
- Effort/Result enums: `EffortResultModuleState`, `EffortResultClassificationState`, `EffortResultDataQuality`
- Supports: CurrentAuction scope + ActiveEpisode scopes + RecentlyClosed (cap 64)
- GPS card rows via `AuctionGpsCardMapper.BuildEffortResultLines()`; showDiagnostics-gated ID/version rows
- RuntimeSnapshot schema bumped `0.11.0` → `0.12.0`; `EffortResult` property on `GcaeRuntimeSnapshot`
- Indicator: `EnableEffortResultClassifier` / `ShowEffortResultDiagnostics` settings; module default OFF
- GPS diagnostics list: `EFFORT RESULT:` status row added (10 rows total; was 9)

### Explicitly deferred after Phase 2E

- EffortResultBalanced / AggressionEffective/Ineffective calibration (gated: NOT CALIBRATED)
- PotentialPassiveAbsorption / PotentialExhaustion calibration (gated: NOT CALIBRATED)
- TradeFacilitationHealthy / TradeFacilitationFailing calibration (gated: NOT CALIBRATED)
- FAR / AAC overall conclusion (NOT AUTHORIZED in Phase 2E)
- Thesis / Entry / Risk phases
- Overlay alerts

## Phase 3A code/test (2026-07-26) — LIVE ACCEPTANCE PENDING

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 651 passed / 0 failed / 0 skipped; 0 errors / 0 warnings |
| Live acceptance | **REQUIRED** — focused gate pending |
| Commit/tag | **HOLD** until live acceptance |
| Runtime schema | `0.13.0` |
| FAR policy | `FAR_THESIS_POLICY_V1` |
| AAC policy | `AAC_THESIS_POLICY_V1` |
| Assembly | `0.0.6` (unchanged) |
| Source/deployed DLL SHA-256 | `237C699C4FBB8E1E326425B3695173F8A8DC52F3911E4F6C083280CA07CAFF9B` (exact match) |
| All calibrated states | **NOT CALIBRATED** — Armed/Executable/Managing/Completed reserved |
| GPS rows | 11 (was 10); "THESIS: NOT AVAILABLE" replaced by "FAR:" + "AAC:" rows |
| New Phase 3A tests | 59 tests (A01–R02); total 651 |

### Phase 3A present (code/test)

- `FarThesisHost` — FAR (Failed Auction Re-entry) state machine; fingerprint-gated rebuild from evidence
- `AacThesisHost` — AAC (Acceptance-Continuation) state machine; fingerprint-gated rebuild from evidence
- `FarState` enum: 15 states (observable 0–5, calibrated 100–105, terminal 200–202)
- `AacState` enum: 14 states (observable 0–6, calibrated 100–102, terminal 200–203)
- FAR direction: `CanonicalOutsideDirection==Below`→Long; `Above`→Short
- AAC direction: `Above`→Long; `Below`→Short (opposite of FAR)
- `ThesisDirection`: Unknown=0, Long=1, Short=2
- Observable state mappings: `Interacting`→EpisodeActive; reentry obs→ReentryDeveloping; `ReentryDeveloping` episode→NOT CALIBRATED
- AAC specific: `ReentryDeveloping` episode→`Invalidated` (re-entry negates continuation)
- GPS diagnostics: "FAR: {state}" and "AAC: {state}" rows
- Indicator settings: `EnableFarThesis`, `ShowFarThesisDiagnostics`, `EnableAacThesis`, `ShowAacThesisDiagnostics`
- RuntimeSnapshot schema bumped `0.12.0` → `0.13.0`; `FarThesis` + `AacThesis` properties added
- RecentlyClosedCapacity=64; ArmableCount/ExecutableCount always 0 (NOT CALIBRATED)

### Explicitly deferred after Phase 3A

- Armed / Executable / Managing / Completed states (gated: NOT CALIBRATED)
- Thesis Signal Maturity, Entry Policy, Invalidation triggers
- PLAR Targets, CFD Mapping, Risk phases
- Overlay alerts / Telegram integration

## Phase 2B final closeout (2026-07-25) — LOCKED

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 482 passed / 0 failed / 0 skipped (×2 full runs); Probe/Recorder 139 green; 0 errors / 0 warnings |
| Focused live gate | **PASS** — GCQ6 / Rithmic Live (two snapshots ~2s apart) |
| Phase 2A authoritative | **PASS** — no second trade normalization |
| Live update propagation | **PASS** — volume 318→324; trades 258→264; levels 5→6; Unknown 318→324 |
| Unknown-only Partial | **PASS** — Classified levels 0; Ask/Bid ratios unavailable (not zero) |
| Empirical Volume Rank | **PASS** — 4/5 → 6/6; latest tick 4071.0 → 4070.7 |
| ClassificationState | **PASS** — NOT CALIBRATED |
| Coverage live | **PASS** — LIVEONLYFROMAUCTIONSTART |
| History | **PASS** — LIVE_ONLY |
| Data Gate / MBO | **PASS** — DATA DEGRADED independent; MBO BLOCKED |
| Final verdict | **FINAL PASS WITH DOCUMENTED LIVE CLASSIFIED-CLUSTER COVERAGE LIMITATION** |
| Runtime schema | `0.9.0` |
| Policy | `CLUSTER_RAW_FEATURE_POLICY_V1` |
| Rank method | `EMPIRICAL_MIDRANK_V1` |
| Assembly | `0.0.6` (unchanged) |
| Final DLL SHA-256 | `A15CC6A85AA9562E59CA8B66140AAD017E957F96E96C7BBF47E620E6E0E71A39` (source = deployed) |
| Tag | `gcae-p2b-cluster-raw-feature-measurement-pass` |
| Imbalance / Stacked Imbalance | **NOT STARTED** |
| Extreme Delta / Extreme Volume | **NOT STARTED** |
| Big Trade | **NOT STARTED** |
| Effort vs Result classifier | **NOT STARTED** (Phase 2C is raw evidence only) |
| Trade Facilitation | **NOT STARTED** |
| Resolution/FAR/AAC | **NOT STARTED** |

### Documented live classified-cluster coverage limitation (accepted)

Focused live gate proved Cluster Raw PARTIAL publication, Phase 2A→2B revision propagation across two live snapshots, Unknown-only level growth, truthful unavailable Ask/Bid ratios, empirical Volume Rank updates, ClassificationState NOT CALIBRATED, Episode Cluster Raw PARTIAL, DATA DEGRADED independence, and MBO BLOCKED. LEVEL TRADES 90→84 is not a decrement — latest displayed tick changed (4071.0→4070.7). Classified Ask/Bid ratios, diagonal classified paths, classified dominant sides/runs, absolute-Delta rank, percentile ties, multi-visit lifecycle, classified Episode aggregates, Centerline, auction/epoch/disable resets, revision sequences, stale rejection, and deterministic replay remain **automated-only** coverage. This does not alter Cluster Raw semantics and does not require further Ask/Bid event hunting.

### Phase 2B present (locked)

- Cluster Raw host over Phase 2A snapshots; version-gated rebuild; visit tracking via changed tick
- Same-price / diagonal raw ratios; RawDominantSide; consecutive dominance (no stacked label)
- EMPIRICAL_MIDRANK_V1 ranks/percentiles; visits/revisits; Episode/auction cluster summaries
- Module default OFF; GPS rows; diagnostics OFF by default; no overlay/alerts/thresholds
- Does not mutate Orderflow/Profile/Composite/Reference/Directional/Episode/Evidence

### Explicitly deferred after Phase 2B

- Bid/Ask / stacked imbalance classification; Extreme Delta/Volume; Big Trade; tape-speed
- Absorption / exhaustion / Effort vs Result / Trade Facilitation
- Acceptance/Re-entry Resolution / FAR / AAC / Thesis / Entry / Risk

### Documented live raw-feature coverage limitation (accepted)

Focused live gate proved Orderflow PARTIAL publication, ordinary trade admission with Probe OFF, truthful Unknown-aggressor accounting (Ask/Bid unavailable), mathematically safe Classified Delta/CVD/coverage, LIVEONLYMIDAUCTION coverage, Episode Orderflow PARTIAL, DATA DEGRADED independence, and MBO BLOCKED. Ask/Bid classified paths, complete classification READY, mixed-side reconciliation, nonzero Delta/CVD, full per-price ledger, complete Episode aggregates, timing metrics, cumulative revision replacement, auction/epoch/disable–re-enable resets, LiveOnlyFromAuctionStart transition, revision sequences, out-of-order rejection, and deterministic replay remain **automated-only** coverage. This does not alter raw Orderflow semantics and does not require further operator-manufactured aggressor events.

### Phase 2A present (locked)

- One authoritative `TradeStreamAtasMapper.MapNewTrade` → `ExecutedTradeEvent` path
- Cumulative callbacks not authoritative for executed totals (`CUMULATIVE_CALLBACKS_NOT_AUTHORITATIVE_FOR_EXECUTED_TOTALS`)
- Current-auction aggregate + per-price ledger + Episode raw aggregate
- Classified Delta/CVD; Unknown aggressor explicit; timing raw only
- Module default OFF; GPS rows; diagnostics OFF by default; no overlay/alerts
- Does not mutate Profile/Composite/Reference/Directional/Episode/Evidence

### Explicitly deferred (Phase 2B+)

- ~~Cluster Raw Feature Measurement~~ — **Phase 2B LOCKED**
- Imbalance / stacked imbalance / Big Trade classification
- Absorption / exhaustion / Effort vs Result / Trade Facilitation
- Acceptance/Re-entry Resolution / FAR / AAC / Thesis / Entry / Risk

### Documented live evidence-lifecycle coverage limitation (accepted)

Focused live gate proved Evidence PARTIAL publication, natural UpperBoundary evidence set, finite descriptive ratios, geometric re-entry observation, Local POC displacement, observer-only behavior (Episode REENTRYDEVELOPING unchanged), DATA DEGRADED independence, and MBO BLOCKED. Full outside/inside segmentation, LowerBoundary symmetry, zero-denominator ratios, repeated attempts, exact revision sequences, lifecycle freeze/reset, Centerline not-applicable, bid/ask variants, compatibility fail-closed, and deterministic replay remain **automated-only** coverage. This does not alter Evidence/Episode semantics and does not require further operator-manufactured crossings.

### Phase 1F present (locked)

- Read-only `EpisodeMeasurementEvent` feed from Phase 1E admission
- Immutable Acceptance/Re-entry evidence vectors (measurement only)
- Observation states: Acceptance Early/Developing/Unresolved; Reentry GeometricReentry/Developing/Unresolved
- Centerline: `CENTERLINE_ACCEPTANCE_GEOMETRY_NOT_APPLICABLE` — no canonical outside ratios
- EvidenceId `AREV|{EpisodeId}|ACCEPTANCE_REENTRY_EVIDENCE_POLICY_V1` stable per EpisodeId
- LIVE_ONLY history; unavailable fields explicit (no fabricated zeros)
- Module default OFF; GPS rows; diagnostics OFF by default; no overlay/alerts
- Does not mutate Episode State / Resolution / AttemptCount

### Explicitly deferred (resolution / later phases)

- Acceptance Established / Failed
- Stable Reacceptance / Reentry Failed
- FAR / AAC / Thesis / Entry / Risk / Long-Short
- OutsideCloseRatio / TPO outside / Local Value rebuild / OldValueReclaimFailure / RetestHoldQuality
- Opposite-aggression effectiveness
- Invented acceptance/maintenance/stable-reentry thresholds

### Phase 1D present (locked)

- Multi-horizon descriptive Directional Context (Structural / Tactical / OTF / Execution=Unavailable)
- Deterministic pairwise categorical migration + conservative state table
- Completed-auction structural aggregation (no fixed N-day window)
- Developing tactical context vs previous completed Primary
- One-Time Framing from completed TPO periods only (DevelopingUp/Down/Broken/Mixed)
- Narrow completed TPO period production feed from ClassicTpoEngine
- Input fingerprint publish reuse; price-only location refresh when evidence unchanged
- GPS card rows; diagnostics OFF by default; no Long/Short/Buy/Sell/Thesis/score
- Module default OFF; Directional Ready does not clear global DATA DEGRADED

### Explicitly deferred after Phase 1D (partially superseded)

- ~~Auction Episode~~ — **Phase 1E LOCKED**
- Acceptance / Re-entry Resolution — **Phase 1F LOCKED (measurement only; Resolution still NOT STARTED)**
- ~~Orderflow raw features~~ — **Phase 2A LOCKED**
- Orderflow interpretation / FAR/AAC / Thesis / Entry / Risk
- Execution directional horizon
- Calibrated OTF ConfirmedUp/ConfirmedDown threshold
- Weekly/monthly profile horizons
- Thin Participation / Settlement / Day Structure production classifier
- Score / probability / automatic execution

## Phase 1C final closeout (2026-07-24) — LOCKED

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 372 passed at lock |
| Live acceptance | **PASS WITH DOCUMENTED LIVE COVERAGE LIMITATION** |
| Final DLL SHA-256 | `9CAE06B9091D23854DA60A425E45884D2CC0D5C50E501A4E9EB1B12A58FAF3CA` |
| Tag | `gcae-p1c-structural-reference-foundation-pass` |

## Phase 1B / 1A locks (unchanged)

See prior closeout sections; tags and hashes unchanged.


---

# PHỤ LỤC B — MASTER METHOD & ARCHITECTURE BASELINE v1.2, NHÚNG NGUYÊN VĂN

> Vùng này bảo tồn toàn bộ kiến thức, kiến trúc, research inventory và roadmap của baseline. Nó là đích đến, không phải giấy phép tự động triển khai mọi module.

# GC AuctionFlow Engine

## Bản đặc tả hợp nhất hậu phản biện chéo về phương pháp phân tích, kiến trúc DLL, ATAS Ultra, nghiên cứu và thông báo từ xa

**Tên chính thức:** `GC AuctionFlow Engine`  
**Tên rút gọn:** `GCAE`  
**Phiên bản tài liệu:** `v1.2 Post-Cross-Review Final Baseline`  
**Ngày khóa baseline:** `2026-07-21`  
**Ngôn ngữ:** Tiếng Việt  
**Đối tượng chính:** GC COMEX Futures trên ATAS Ultra với dữ liệu Rithmic  
**Nền tảng thực thi lệnh:** CFD riêng, không đặt lệnh trực tiếp từ ATAS trong phạm vi hiện tại

> **Tuyên bố trạng thái:** “Final Baseline” trong tài liệu này có nghĩa là **bản kiến trúc và phương pháp hợp nhất đã được kiểm toán theo chuỗi trao đổi của anh Fen, sau đó vá thêm Audit Patch 01 từ phản biện chéo giữa nhánh chưng cất và nhánh kiến trúc GCAE**. Nó không có nghĩa rằng mọi threshold đã được hiệu chỉnh, mọi module đã được code xong hoặc edge đã được chứng minh bằng tiền thật.

> **Tuyên bố sứ mệnh:** Không sao chép khóa học thành indicator. Dùng kiến thức của giảng viên làm nền móng, dịch sang GC, mở rộng bằng AMT, Market Profile, Volume Profile, executed orderflow, ATAS Ultra, Rithmic, MBO, quản trị rủi ro và nghiên cứu định lượng để xây một hệ thống phân tích mà khóa học chưa từng xây dựng.

---

# 0. Tài liệu này hợp nhất những gì?

Tài liệu này thay thế `GC_AuctionFlow_Engine_v0.2_Full_Spec_VI.md`, kế thừa v1.0 và v1.1, đồng thời tích hợp phản biện chéo chính thức qua `GCAE_SPEC_v1.1_AUDIT_PATCH_01`, bao gồm:

```text
CHAPTER_II_RESEARCH_BASELINE_v1
+
CHAPTER_III lessons 1–9
+
Toàn bộ trao đổi về AMT, xu hướng, scalp, entry, SL, TP
+
Sửa sai khái niệm “sweep”
+
Auction Episode Engine
+
ATAS Ultra Indicator Inventory
+
Cluster Search / Smart Tape / Big Trades / MBO / DOM / Iceberg / Stops / Sweeps
+
TPO 30 phút và nghiên cứu Adaptive TPO
+
Auction Energy / Tempo / Pressure / Quality / Genome / GPS
+
Historical Scanner và Research Harness
+
CFD Mapping
+
Telegram Notification Gateway
+
Trading System, backtest, Double Distribution, Market Maker, Limit Tracing
+
Conversation Coverage Audit và Supersession Matrix
+
Cross-Review Audit Patch 01
+
Unified Research Ledger schema
+
Source Provenance Map
```

Các nội dung được phân loại theo sáu nhãn nghiên cứu:

```text
COURSE_STATEMENT
OUR_INTERPRETATION
GC_TRANSLATION
CODE_READY_RULE
WORKING_HYPOTHESIS
EXCLUDED
```

Không có phát biểu nào được biến thành luật máy chỉ vì nó xuất hiện trong khóa học, ảnh mạng xã hội, indicator ATAS hoặc trong chính cuộc trao đổi giữa chúng ta.


---

# 0A. Conversation Coverage Audit

Phần này được thêm để kiểm toán trực tiếp các cụm trao đổi mà anh Fen đánh dấu trong ảnh chụp danh sách chat. Mục đích không phải lặp lại hội thoại, mà chứng minh mỗi ý tưởng giá trị đã được chuyển thành **quyết định kiến trúc, rule, hypothesis, research module hoặc nội dung bị loại**.

## 0A.1 Kết luận sau lần đọc lại Chương II

Đã giữ và tích hợp:

```text
Auction State
→ Structural Location
→ Attempted Auction
→ Acceptance / Rejection
→ Aggressive–Passive Interaction
→ Re-entry hoặc Value Formation
→ Entry Policy
→ Structural Invalidation
→ Auction Target
```

Các quyết định đi kèm:

```text
Composite Profile là context bắt buộc.
Absorption phải được đọc bằng Effort versus Result.
Retest có giá trị lớn nhưng không phải định luật tuyệt đối.
Các heuristic như 4 TPO, first retest, POC magnet, poor extreme repair
không được hardcode trước kiểm định GC.
```

Nội dung này được triển khai trong các phần 11–23, 26–27, 32–33 và 59–62.

## 0A.2 “AMT được dùng trong hệ thống như thế nào?”

Đã khóa:

```text
AMT = framework tổ chức cuộc đấu giá.
AMT không tự phát lệnh.
AMT tạo Context + Location + Auction Hypothesis.
Orderflow kiểm tra effort, response và timing.
Auction Resolution quyết định acceptance, re-entry hay unresolved.
```

Hệ thống là **analysis-first**:

```text
BuildContext()
ClassifyState()
TrackEpisode()
EvaluateEvidence()
ResolveAuction()
BuildThesis()
PublishAnalysis()
```

Không có quyền đặt lệnh tự động trong baseline hiện tại.

## 0A.3 “Tín hiệu có quá chặt và có hợp scalp không?”

Đã sửa thiết kế một mức xác nhận duy nhất thành ba maturity modes:

```text
FAST
STANDARD
CONFIRMED
```

Giữ chặt:

```text
Data Integrity
Location
Thesis
Invalidation
Contrary Acceptance Veto
Target Space
Executable RR
```

Cho phép linh hoạt:

```text
Retest depth
Retest duration
Micro-retest hay structural retest
Entry confirmation
POC migration maturity
```

Retest **không còn là hard gate cho mọi production signal**. No-retest và micro-retest được phân nhóm riêng, có policy/risk khác và phải được kiểm nghiệm.

## 0A.4 Sửa sai khái niệm “sweep”

Đã retire:

```text
Price crosses reference
→ gọi là sweep
→ báo setup mới sau mỗi lần cross
```

Đã thay bằng:

```text
Reference Excursion
→ Auction Episode
→ Attempt Count
→ Evidence Accumulation
→ Accepted Outside / Reaccepted Inside / Unresolved / Expired
```

`MBO Sweep` chỉ là event aggressive removal trên nhiều mức book, không đồng nghĩa failed auction và không tự tạo signal.

Một reference chỉ có **một episode đang hoạt động**, nên giá xuyên lên xuống nhiều lần chỉ cập nhật `AttemptCount`, không bắn Telegram lặp lại.

## 0A.5 “Không muốn treo indicator vài tuần/tháng để hấp thụ dữ liệu”

Đã khóa quy trình hai đường song song:

```text
Historical Scanner + Targeted Replay
song song với
Live Shadow Recorder
```

Production Core không được phụ thuộc vào việc chờ DOM/MBO vài tháng. Nó phải hoạt động bằng historical trades, Bid/Ask executions, Profile và reconstructed orderflow có thể kiểm tra.

Nguyên tắc dữ liệu:

```text
Thu raw features và Auction Episodes.
Không chỉ thu signal outcomes.
Threshold thay đổi sau không cần thu lại toàn bộ dữ liệu.
```

## 0A.6 “Hệ thống có giải quyết vấn đề trader không?”

Đã chuyển thành mục tiêu chống tự lừa mình:

```text
Không biết market đang làm gì
→ Auction State

Không biết level nào quan trọng
→ Reference Hierarchy + Lifecycle

Không biết breakout thật/giả
→ Auction Episode + Resolution

Không biết Delta mạnh nghĩa gì
→ Effort versus Result

Không biết đứng ngoài khi nào
→ Hard Veto + Unresolved Rotation

Không biết edge có thật không
→ Historical Scanner + Walk-forward + Logger
```

GCAE không hứa nhìn thấy tương lai. Nó phải cho biết:

```text
Ta biết gì?
Chưa biết gì?
Đang chờ gì?
Điều gì chứng minh thesis sai?
```

## 0A.7 Directional context thay cho Trend Filter

Đã tích hợp `Multi-Horizon Directional Auction Context`:

```text
Structural Bias
Tactical Bias
Execution Bias
```

Direction được suy ra từ:

```text
Value migration
POC migration
Acceptance direction
Episode resolution history
Price progress
Orderflow effectiveness
```

Xu hướng không phải hard veto tuyệt đối. Nó điều chỉnh setup preference, target, expiry, evidence requirement và risk policy.

## 0A.8 TPO/VP có phải dùng 30 phút?

Đã khóa:

```text
Classic TPO 30 phút = Production reference baseline.
Volume Profile không phụ thuộc 30 phút.
Adaptive TPO = Research-only.
Auction-based / volume-based / trade-count-based TPO = Research-only.
```

Acceptance cuối cùng không chỉ bằng TPO:

```text
Time Acceptance
+ Volume Acceptance
+ Orderflow Acceptance
```

## 0A.9 Các ý tưởng rút từ bộ ảnh AMT/Orderflow

Đã giữ dưới đúng trạng thái:

```text
Effort vs Result / Trade Facilitation
POC Migration Velocity
POC Strength
nPOC Lifecycle
HVN Friction
LVN Vacuum
TPOC–VPOC alignment
Relative Volume by Participation Regime
Trade Facilitation Quality
```

Đã hạ cấp hoặc sửa ngôn ngữ:

```text
“CVD absorption = đảo chiều” → không hợp lệ.
“POC là institutional magnet” → không dùng.
“HVN là support/resistance chắc chắn” → không dùng.
“LVN luôn chạy xuyên nhanh” → hypothesis cần regime/context.
```

## 0A.10 Auction Energy, Tempo, Pressure, Quality, Genome và GPS

Đã đưa vào Research Layer, không trao quyền production trước validation:

```text
Auction Energy / Potential
Auction Tempo
Auction Pressure
Auction Quality / Breakout Quality
Auction Genome
Auction GPS
```

Những index này phải có feature provenance. Chúng không được giả danh xác suất và không được dùng một tổng điểm để bù hard veto.

## 0A.11 “Những gì khóa học không dạy” và ATAS Ultra

Đã ánh xạ toàn bộ inventory quan trọng:

```text
Cluster Search
Cluster Statistic
Smart Tape
Speed of Tape
Big Trades / Adaptive Big Trades
Smart DOM / Heatmap / DOM Levels
MBO DOM
Pulling / Stacking
Iceberg
Stops Tracker
Sweeps Tracker
Market Power / DOM Power / DOM Strength
Dynamic Levels
Volume Statistic
```

Nguyên tắc cuối:

```text
AMT quyết định nơi và lúc mở kính hiển vi.
Executed data là Tier 1.
Displayed liquidity là Tier 2.
Iceberg/Stops/Sweeps/Market Power là model/event tag có provenance.
Không indicator black box nào tự phát hoặc hủy thesis lõi.
```

## 0A.12 Chapter III Bài 1–5

Đã tích hợp:

```text
Thesis Contract
Price / Auction / Time / Context Invalidation
Position size sau structural invalidation
Trade Affordability Gate
Multi-horizon expectation
Path of Least Auction Resistance
Intermediate Barriers
Confirmation Premium
Execution Policy
Trade Management Reason Ledger
Thesis Expiry
```

## 0A.13 Chapter III Bài 6–9

Đã tích hợp:

```text
Trading System và Backtest Governance
Macro/Liquidity Regime như context hoặc veto
Double Distribution follow-through/failure
Thesis consistency
Market Maker inventory-risk context
Pre-event liquidity withdrawal
Limit Tracing Candidate research
```

Bài 10 được loại khỏi baseline vì là buổi tổng kết/tạm biệt, không có giá trị chuyên môn cần đưa vào engine.

## 0A.14 Limit hay trade trực tiếp?

Đã khóa `Entry Policy Engine`, không cố định một loại lệnh:

```text
Passive Limit
Marketable Limit
Stop-Market
Stop-Limit
Market
Wait / No Trade
```

Quyết định dựa trên:

```text
Auction maturity
Location quality
Tempo
Spread
Depth
Urgency
Slippage risk
Distance to invalidation
Missed-trade risk
```

Location được chuẩn bị trước. Auction resolution và execution condition quyết định cách tham gia.

## 0A.15 Một DLL, OI và Telegram

Đã giữ:

```text
Một codebase
Một DLL cài vào ATAS tại một thời điểm
Một indicator hiển thị
Nhiều engine nội bộ
```

Open Interest:

```text
Không thuộc Production Core intraday.
Chỉ bật khi Capability Probe xác nhận cadence, history, live compatibility và roll mapping.
EOD OI/COT chỉ là higher-timeframe context.
```

Telegram:

```text
ATAS AddAlert → Telegram là production path ưu tiên.
Direct Bot API và Webhook Relay là adapter tùy chọn.
Deduplication dùng EpisodeID + StateVersion.
ATAS/Rithmic/máy/VPS vẫn phải đang hoạt động.
```

---

## 0A.16 Phản biện chéo chính thức và Audit Patch 01

Phản biện giữa nhánh chưng cất và nhánh kiến trúc xác nhận v1.1 đủ tư cách làm baseline, nhưng phát hiện chín mục từ dòng chưng cất Kỳ 2 và các vòng review cũ chưa được đưa vào một cách tường minh.

Quyết định cuối:

```text
1. OneTimeFramingTracker
   → PRODUCTION_CORE, descriptive context only

2. SinglePrintFormationContext
   → RESEARCH_ONLY + CAPABILITY_GATED

3. Primary Intraday TPO anchor 08:20 ET
   → PRODUCTION_CORE configurable default

4. IBExtremeIsDayExtreme / next-auction revisit study
   → RESEARCH_ONLY, high priority

5. ThinParticipationClassifier
   → PRODUCTION_CORE detection/tagging
   → weighting/exclusion remains RESEARCH_ONLY

6. AdjacentBuildRatio
   → RESEARCH_ONLY

7. PreSettlementWindow tag around 13:30 ET
   → PRODUCTION_CORE logging/regime tag only

8. DayStructureClassifier
   → RESEARCH_ONLY; restored after accidental omission

9. FAST maturity mode
   → SHADOW_ONLY by default until validation
```

Các mục này không làm nở phạm vi implementation v0.1 hiện tại. Chúng bổ sung interface, provenance, logging và research governance để dữ liệu được tích lũy đúng ngay từ đầu.

# 0B. Supersession Matrix: v0.2/v1.1 → v1.2

| Nội dung v0.2 hoặc ý tưởng trung gian | Quyết định cuối trong v1.1 |
|---|---|
| `SweepDetector` dựa vào crossing | Retired, thay bằng `AuctionEpisodeEngine` |
| `Sweep-Reclaim Reversal` là tên lõi | Legacy alias, thesis chính thức là `Failed Auction Re-entry` |
| `Break-Accept-Retest` là tên lõi | Legacy alias, thesis chính thức là `Accepted Auction Continuation` |
| Mỗi lần xuyên level có thể tạo event mới | Một Episode tích lũy nhiều attempt và chỉ resolve một lần |
| Retest là hard gate cho mọi production signal | FAST/STANDARD/CONFIRMED với micro/structural/no-retest policies |
| Score tổng có thể đại diện confidence | Chỉ dùng descriptive states/indexes có provenance, không giả xác suất |
| OI nằm trong Positioning Core | Capability-gated external/EOD context |
| Session như các cuộc đấu giá tách biệt | GC continuous auction, session là participation regime |
| Static thresholds | Regime-normalized percentiles và volatility adjustment |
| Chờ live nhiều tháng mới nghiên cứu | Historical Scanner + Replay + Live Shadow song song |
| Order type cố định | Entry Policy chọn Limit/Marketable/Stop/Market/Wait |
| Trend Filter một chiều | Multi-Horizon Directional Auction Context |
| TPO 30 phút là luật duy nhất | 30m production convention, adaptive variants research-only |
| ATAS tools là checklist indicator | Capability-gated integration matrix và research provenance |
| DOM snapshot xác nhận direction | DOM/MBO chủ yếu mô tả execution condition và liquidity lifecycle |
| Absorption = volume lớn không progress | Potential absorption chỉ sau effort/result + location + resolution |
| Psychology là lời khuyên mềm | System Governance, reason ledger, cooldown, horizon/risk violations |
| Day-Type Engine tồn tại ở v0.2 nhưng biến mất khỏi v1.1 | Xác nhận là sơ suất coverage; khôi phục dưới tên `DayStructureClassifier`, RESEARCH_ONLY |
| Directional Context chưa có One-Time Framing | Thêm `OneTimeFramingTracker` vào PRODUCTION_CORE như descriptive context, không tự phát signal |
| Classic TPO có period 30m nhưng anchor policy để mở | Primary Intraday TPO mặc định 08:20 `America/New_York`, vẫn configurable |
| Thin session chưa có participation-quality policy | Detection/tagging là CORE; attenuation/exclusion chỉ Research và tắt mặc định |
| FAST tồn tại về taxonomy nhưng chưa có deployment guardrail | FAST detection/logging bật; action alert, executable plan và risk sizing tắt mặc định |
| Single Print chỉ được mô tả hình học | Thêm `SinglePrintFormationContext` để nghiên cứu corresponding executed activity |
| IBH/IBL chưa có nhãn day-extreme và next-auction study | Thêm EOD labels và nghiên cứu revisit/reaction không look-ahead |
| HVN barrier chưa có adjacent one-sided build feature | Thêm `AdjacentBuildRatio` dưới Research-only barrier permeability |
| Settlement proximity chưa được log | Thêm `PreSettlementWindow` / `SettlementTransition` tags, không tự tạo fade |

---

# 0C. Operator Inputs cần chuẩn bị trước khi triển khai

GCAE không cần chờ vài tháng để bắt đầu, nhưng implementation cần những input thực tế sau:

```text
1. GC contract policy
   - Symbol đang dùng
   - Roll convention
   - Số ngày historical trades/Bid-Ask tải ổn định

2. ATAS workspace
   - Context chart
   - Execution footprint chart
   - Bar type và aggregation hiện tại
   - Profile/TPO settings dùng để benchmark

3. CFD execution mapping
   - CFD symbol
   - Value per point per lot
   - Minimum lot step
   - Normal spread / stressed spread
   - Basis observation method
   - Slippage assumptions

4. Risk inputs
   - Equity
   - Maximum risk per trade
   - Daily/weekly loss limits
   - Maximum open risk

5. Notification operations
   - ATAS alert/Telegram route
   - VPS hoặc máy chạy liên tục
   - Disconnect/reconnect health policy
```

Những input này là cấu hình vận hành. Chúng không thay đổi cơ chế AMT, Auction Episode hay Thesis Contract.

---

# 0D. Quy tắc bảo toàn ý tưởng

Mỗi ý tưởng mới hoặc cũ phải nằm trong một trong năm trạng thái kỹ thuật:

```text
PRODUCTION_CORE
RESEARCH_ONLY
CAPABILITY_GATED
WORKING_HYPOTHESIS
RETIRED_OR_EXCLUDED
```

Một ý tưởng “nghe rất hay” không được biến mất, nhưng cũng không được nhảy thẳng vào signal engine. Nó phải được lưu trong `Idea Register`, có provenance, feature yêu cầu, test plan và điều kiện nâng cấp.

# 0E. Audit Patch 01 — Governance Contract

Audit Patch 01 là bản vá append-only. Nó không xóa lịch sử v1.1 và không cho phép một ý tưởng mới nhảy thẳng vào Signal Engine.

## 0E.1 Quyền lực theo trạng thái 0D

```text
PRODUCTION_CORE
→ được tính ổn định, log và hiển thị trong runtime core
→ không đồng nghĩa được quyền tự phát signal

RESEARCH_ONLY
→ được tính/log trong Historical Scanner, Replay hoặc Live Shadow
→ không được thay đổi Production Thesis

CAPABILITY_GATED
→ chỉ hoạt động khi Data Capability Probe xác nhận dữ liệu cần thiết

WORKING_HYPOTHESIS
→ có câu hỏi, feature và outcome rõ nhưng chưa đủ định nghĩa hoặc validation

RETIRED_OR_EXCLUDED
→ giữ lịch sử quyết định, không chạy trong core
```

## 0E.2 Nguyên tắc không phá phạm vi v0.1

```text
Implementation v0.1:
Data Gate + Profile + Structural Reference Overlay

Patch 01:
chỉ yêu cầu chuẩn bị config, interface, tags và provenance phù hợp
```

Các engine nghiên cứu trung bình hoặc lớn như Day Structure, Single Print Formation Context và Adjacent Build không được chen vào milestone v0.1.

# 1. Executive Summary

GC AuctionFlow Engine là một **Auction Intelligence System dùng để phân tích**, không phải robot tự động giao dịch.

Luồng vận hành tổng quát:

```text
GC Futures + Rithmic
→ Data Capability & Integrity
→ Multi-Horizon Auction Map
→ Active Reference Hierarchy
→ Auction Episode
→ Acceptance / Re-entry Resolution
→ Executed Orderflow & Trade Facilitation
→ Thesis Construction
→ Entry Policy / Invalidation / Target Corridor
→ CFD Mapping / Account Risk
→ Analysis Output / Telegram
→ Logging / Historical Study / Calibration
```

Hệ thống phải trả lời:

```text
1. Cuộc đấu giá nào đang được phân tích?
2. Thị trường đang Balance, Discovery hay Transition?
3. Value và POC đang di chuyển theo hướng nào trên từng horizon?
4. Giá đang ở đâu trong Auction Map?
5. Reference hiện tại còn hiệu lực hay đã bị giao dịch qua lại đến mất ý nghĩa?
6. Giá đang thử xây giá trị ở phía nào?
7. Cuộc thử giá đang được chấp nhận, bị tái chấp nhận vào vùng cũ hay chưa giải quyết?
8. Bên chủ động đã bỏ bao nhiêu nỗ lực?
9. Nỗ lực đó tạo được bao nhiêu price progress và trade facilitation?
10. Thesis nào đang hợp lý và còn thiếu điều kiện gì?
11. Entry nào có thể thực thi bằng Limit, Marketable Limit, Stop hay Market?
12. Thesis sai ở đâu theo giá, Auction, thời gian và context?
13. Target nào nằm trên Path of Least Auction Resistance?
14. RR sau spread, basis và slippage CFD còn đủ không?
15. Tín hiệu còn sống, đang yếu, đã invalidated hay expired?
```

GCAE có thể kết luận:

```text
LONG THESIS
SHORT THESIS
NEUTRAL
OBSERVE
NO TRADE
DATA DEGRADED
DATA INVALID
```

`NO TRADE` không phải thất bại của hệ thống. Đó là một output phân tích đầy đủ.

---

# 2. Hiến pháp GCAE

## 2.1 Không tin tuyệt đối

Không tin tuyệt đối:

```text
Khóa học
Sách
Trader nổi tiếng
Ảnh TikTok
Indicator ATAS
DOM snapshot
Black-box model
Tôi
Anh Fen
```

Mọi ý tưởng phải đi qua:

```text
Ý tưởng
→ Cơ chế Auction hoặc microstructure
→ Dữ liệu có quan sát được không?
→ Định nghĩa có khách quan không?
→ Có thể log raw features không?
→ Historical event study
→ Replay không lookahead
→ Walk-forward / out-of-sample
→ Live shadow
→ Mới đủ điều kiện Production
```

## 2.2 AMT là framework, không phải entry signal

AMT trả lời:

```text
Market đang ở trạng thái nào?
Value nằm ở đâu?
Giá đang thử đi đâu?
Mức giá mới có được chấp nhận không?
```

AMT không tự trả lời:

```text
Bấm Buy ở tick nào?
Đặt Limit hay Market?
Stop chính xác ở đâu trên CFD?
```

## 2.3 Orderflow chỉ có ý nghĩa trong Context và Location

Thứ tự bắt buộc:

```text
Context
→ Location
→ Auction Episode
→ Orderflow
→ Price Result
→ Resolution
→ Execution
```

Không đảo thành:

```text
Delta đỏ
→ kể câu chuyện
→ tìm level để hợp thức hóa
```

## 2.4 Hard Veto không thể bị score bù

```text
Không có invalidation
+
10 yếu tố thuận lợi
=
NO TRADE
```

Score, index hoặc xác suất không được phép vượt qua lỗi sống còn.

## 2.5 Score không được giả danh xác suất

Các chỉ số như:

```text
Acceptance Index
Auction Energy
Pressure Index
Trade Facilitation Score
Breakout Quality
POC Strength
Friction Score
Vacuum Strength
```

chỉ được gọi là `Probability` khi đã được hiệu chỉnh xác suất và kiểm định out-of-sample.

Trước đó, chúng chỉ là:

```text
Normalized Research Index
Evidence Vector
Descriptive State
```

Không được hiển thị “Acceptance Probability = 87%” chỉ vì tổng trọng số bằng 87.

## 2.6 Một DLL, một indicator, nhiều engine

Trong ATAS chỉ cài:

```text
GC.AuctionFlow.dll
```

Trong danh sách indicator chỉ thấy:

```text
GC AuctionFlow Engine
```

Bên trong là nhiều module độc lập, capability-gated và có hợp đồng dữ liệu rõ ràng.

## 2.7 Phân tích GC, thực thi CFD

```text
GC Futures
→ nguồn dữ liệu phân tích chính

CFD
→ nơi thực thi thủ công
```

Không dùng order book CFD giả lập để thay thế COMEX/Rithmic.

## 2.8 Không tự động đặt lệnh trong baseline này

GCAE xuất:

```text
Analysis State
Thesis
Entry Zone
Execution Policy
Invalidation
Protective Stop Reference
TP1 / TP2 / TP3
Expiry
Risk / CFD Mapping
```

Nó không tự gọi Buy/Sell trên CFD.

---

# 3. Những điều hệ thống không được trở thành

GCAE không phải:

```text
Indicator Delta đổi màu
Máy bắn mũi tên Buy/Sell
Tổ hợp 20 indicator chồng lên chart
Score 8/10 rồi vào lệnh
Mô hình nến được đổi tên thành AMT
Máy nhận diện wick rồi gọi là liquidity sweep
Black box nói cá lớn đang mua
DOM wall detector = support/resistance
Iceberg xuất hiện = đảo chiều
POC = nam châm bất tử
TPO 4 chữ = acceptance chắc chắn
Trend filter MA dốc lên = cấm mọi Short
Robot phải chờ vài tháng mới “học” được thị trường
```

Nó không được phép:

```text
Dùng một footprint cell để kết luận absorption
Dùng Delta dương để Long hoặc Delta âm để Short
Dùng displayed liquidity như cam kết sẽ được khớp
Dùng OI intraday khi connector không chứng minh dữ liệu hợp lệ
Gọi price crossing là sweep
Mở episode mới mỗi lần giá tick qua reference
Giữ thesis sau Auction Invalidation
Đổi timeframe sau entry để biện hộ
Chọn lot trước rồi kéo stop cho vừa tiền
Dùng target weekly cho thesis micro không có nguồn weekly
Gửi Telegram lặp lại trên mỗi recalculation
```

---

# 4. Sứ mệnh và phạm vi sản phẩm

## 4.1 Vai trò

GCAE đóng vai:

```text
Auction GPS
Radar reference và episode
Bộ não phân loại trạng thái
Kính hiển vi orderflow/microstructure
Bộ dựng Thesis Contract
Bộ tính entry, invalidation, target corridor
Bộ kiểm soát CFD execution risk
Bộ thông báo từ xa
Máy ghi dữ liệu nghiên cứu
```

## 4.2 Không hứa nhìn thấy tương lai

Hệ thống không dự đoán chắc chắn cây nến kế tiếp. Nó phải giúp trader biết:

```text
Ta đang biết gì?
Ta chưa biết gì?
Ta đang chờ điều gì?
Điều gì sẽ chứng minh thesis sai?
```

## 4.3 Vấn đề trader mà hệ thống hướng tới

```text
Không biết thị trường đang làm gì
→ Auction State

Không biết level nào đáng chú ý
→ Reference Hierarchy & Lifecycle

Không biết breakout thật hay giả
→ Auction Episode Resolution

Không biết Delta mạnh là tốt hay xấu
→ Effort vs Result / Trade Facilitation

Không biết entry khi nào
→ Signal Maturity & Execution Policy

Không biết SL ở đâu
→ Multi-Dimensional Invalidation

Không biết TP ở đâu
→ Path of Least Auction Resistance

Không biết khi nào đứng ngoài
→ Hard Veto / Unresolved Rotation

Không ngồi máy
→ Telegram state notifications

Không biết edge có thật không
→ Historical Scanner / Logger / Walk-forward
```

---

# 5. Nguồn kiến thức và ma trận tích hợp khóa học

## 5.1 Chương II

```text
Bài 1: Balance, Imbalance, Acceptance, Rejection
Bài 2: TPO, Volume Profile, Value, POC, Excess, Single Prints
Bài 3: Initial Balance, Day Type
Bài 4: Composite Profile, merge, POC/Value migration
Bài 5: Active/Passive, Delta, Imbalance, Absorption
Bài 6: CVD/OI/positioning, nhưng OI phải capability-gated
Bài 7: Failed Auction, SFP, retest, Two-Attempt Failure
Bài 8: External context, event regime, correlations
```

Xương sống được giữ:

```text
Auction State
→ Structural Location
→ Attempted Auction
→ Acceptance / Rejection
→ Aggression / Price Result
→ Re-entry / Value Formation
→ Entry Policy
→ Invalidation
→ Auction Target
```

## 5.2 Chương III, Bài 1–5

```text
Bài 1: Thesis, invalidation, risk
Bài 2: Account survival, affordability, retest
Bài 3: Multi-timeframe, context, expectation
Bài 4: Path to target, intermediate barriers, confirmation premium
Bài 5: Execution, management, expiry, order types
```

## 5.3 Chương III, Bài 6–9

```text
Bài 6: Trading System, backtest, oscillator/indicator, context
Bài 7: Macro regime, liquidity regime, dòng tiền
Bài 8: Double Distribution, follow-through, failed continuation
Bài 9: Psychology, thesis consistency, market making, order book, Limit Tracing
```

Bài 10 là buổi tổng kết/tạm biệt và không thuộc baseline chuyên môn.

## 5.4 Những nội dung hạ cấp

Các phát biểu sau không thành luật cứng:

```text
First retest luôn tốt nhất
4 TPO luôn là acceptance
Single Print bắt buộc fill
POC luôn hút giá
Poor High chắc chắn phải repair
IB nhỏ chắc chắn thành Trend Day
Delta divergence chắc chắn đảo chiều
Double Distribution chắc chắn follow-through
Market maker luôn đẩy giá theo một cơ chế duy nhất
Limit Tracing có thể nhận diện chỉ bằng mắt
```

---

# 6. Kiến trúc tổng thể

```text
GC AUCTIONFLOW ENGINE
│
├── 1. Data Capability & Integrity Layer
├── 2. Contract / Roll / Participation Layer
├── 3. Multi-Horizon Auction Map
├── 4. Profile, Value & Reference Layer
├── 5. Directional Auction Context
├── 6. Auction Episode & Resolution Layer
├── 7. Executed Orderflow Intelligence
├── 8. ATAS Ultra Microstructure Lab
├── 9. Thesis & Setup Layer
├── 10. Execution Policy Layer
├── 11. Invalidation, Target & CFD Risk Layer
├── 12. Trade Management & Expiry Layer
├── 13. Hard Veto Layer
├── 14. UI / Analysis Card / Telegram
└── 15. Logging / Historical Scanner / Research Lab
```

## 6.1 Hai buồng

### Production Core

Chỉ chứa cơ chế đã hiểu, dữ liệu đủ tin và đã kiểm thử:

```text
Trade / Bid-Ask / Volume
Classic TPO / VP references
Composite Auction
Reference Lifecycle
Directional Auction Context
One-Time Framing descriptive context
Thin Participation and Settlement tags
Auction Episode
Acceptance / Re-entry Resolution
Aggression vs Progress
Failed Auction Re-entry
Accepted Auction Continuation
Thesis Contract
Invalidation / Target / CFD Mapping
Hard Veto
Logging / Notifications
```

### Research Lab

```text
Adaptive TPO
Single Print Formation Context
Day Structure Classifier
IB Extreme Revisit Study
Adjacent Build Ratio
Thin-session weighting/exclusion study
Auction Energy / Tempo / Pressure
Auction Genome / GPS indexes
Big Trades
Smart Tape reconstruction
DOM persistence
MBO queue lifecycle
Pulling / Stacking
Iceberg
Stops Tracker
MBO Sweeps
Market Power
Dynamic Levels
Limit Tracing Candidate
OI / COT / external context
```

Research Lab được phép:

```text
Hiển thị
Ghi log
Tạo tags
So sánh outcome
Đề xuất giả thuyết
```

Không được phép tự tạo hoặc hủy Production Thesis cho đến khi vượt kiểm định.

---

# 7. Hợp đồng module

Mỗi engine nhận snapshot chuẩn hóa và trả output immutable hoặc versioned:

```text
DataCapabilityEngine
→ CapabilitySnapshot

ProfileEngine
→ AuctionMapSnapshot

ReferenceEngine
→ ActiveReferenceSet

AuctionEpisodeEngine
→ AuctionEpisodeSnapshot

OrderflowEngine
→ OrderflowEvidence

ResolutionEngine
→ AuctionResolution

ThesisEngine
→ ThesisContract

ExecutionPolicyEngine
→ ExecutionPlan

RiskEngine
→ RiskPlan

NotificationGateway
→ NotificationEvent
```

Không module nào được tự đọc trực tiếp state nội bộ của module khác.

Mọi output phải có:

```text
Timestamp
Version
Source horizon
Data quality
Evidence provenance
State
Known limitations
```

---

# 8. Phân cấp bằng chứng

## 8.1 Tier 1: Executed Evidence

```text
Trade price
Trade size
Bid executions
Ask executions
Volume
Delta
Trade count
Footprint clusters
Cumulative trades
Tape events
Price progress
Time between executions
```

Tier 1 là bằng chứng trực tiếp nhất, nhưng vẫn bị ảnh hưởng bởi feed aggregation, Bid/Ask classification, historical fidelity và replay differences.

## 8.2 Tier 2: Advertised Liquidity

```text
DOM depth
Displayed size
Queue position
Pulling
Stacking
MBO resting orders
DOM levels
Heatmap persistence
```

Tier 2 có thể bị:

```text
Rút
Di chuyển
Thay đổi
Không được khớp
Dùng để quản lý queue
```

Tier 2 dùng cho execution condition và research, không tự xác nhận hướng.

## 8.3 Tier 3: Model Inference

```text
Iceberg inference
Stops classification
MBO Sweep grouping
Absorption model
Exhaustion model
Market Power
Limit Tracing Candidate
Spoofing Candidate
```

Mỗi Tier 3 feature phải có `Feature Provenance`:

```text
Nguồn dữ liệu
Công thức / thuật toán
Live / Historical compatibility
Độ trễ
Repaint behavior
False-positive rate
Regime sensitivity
Version
```

---

# 9. Data Capability & Integrity Engine

Đây là cổng đầu tiên. Không có dữ liệu hợp lệ thì mọi lập luận phía sau đều là lâu đài trên cát.

## 9.1 Input

```text
Connection state
Instrument / contract
Tick size
Exchange timestamps
Local timestamps
Trade stream
Bid/Ask classification
Historical loading state
DOM availability
MBO availability
Order IDs
Open Interest field
Replay state
Profile initialization
CFD mapping input
```

## 9.2 Capability Matrix

```text
TRADES                   UNKNOWN / INVALID / LIVE / HISTORICAL / BOTH
BID_ASK                  UNKNOWN / INVALID / LIVE / HISTORICAL / BOTH
FOOTPRINT                COMPUTABLE / NOT_COMPUTABLE
TPO                      COMPUTABLE / NOT_COMPUTABLE
VOLUME_PROFILE           COMPUTABLE / NOT_COMPUTABLE
LIVE_DOM                 AVAILABLE / UNAVAILABLE
HISTORICAL_DOM           AVAILABLE / PARTIAL / UNAVAILABLE
MBO                      AVAILABLE / PARTIAL / UNAVAILABLE
ORDER_IDS                AVAILABLE / UNAVAILABLE
ICEBERG_NATIVE           AVAILABLE / INFERRED / UNAVAILABLE
STOPS_CLASSIFICATION     AVAILABLE / PARTIAL / UNAVAILABLE
MBO_SWEEPS               AVAILABLE / PARTIAL / UNAVAILABLE
OPEN_INTEREST            UNKNOWN / EOD_ONLY / PARTIAL / VALIDATED
REPLAY_FIDELITY          UNKNOWN / PARTIAL / VALIDATED
```

## 9.3 Data State

```csharp
public enum DataState
{
    Invalid,
    Degraded,
    Ready
}
```

### Invalid

```text
Sai instrument
Sai tick size
Mất feed
Timestamp discontinuity nghiêm trọng
Profile chưa sẵn sàng
Contract hết hạn hoặc roll không xác định
```

Output:

```text
ANALYSIS DISABLED
DATA INVALID
```

### Degraded

Ví dụ:

```text
Có trades và VP
Không có historical Bid/Ask
→ Auction Map hoạt động
→ Orderflow trigger tắt
```

Hoặc:

```text
Có live DOM
Không có MBO
→ Liquidity snapshot hoạt động
→ Queue lifecycle và native iceberg tắt
```

### Ready

Tất cả capability bắt buộc của module đang xét đều sẵn sàng.

## 9.4 Module-level capability gate

Không có khái niệm “DLL Ready” chung cho mọi feature. Mỗi module tự khai báo yêu cầu:

```text
AuctionEpisode Core
→ Price + Trades + Profile references

Aggression vs Progress
→ Bid/Ask executions + timestamps

Smart Tape Research
→ tick/cumulative trade reconstruction

DOM Persistence
→ repeated depth updates

MBO Lifecycle
→ order-level events + order IDs

Open Interest Context
→ cadence và provenance được xác nhận
```

## 9.5 Integrity failures phải được log

```text
FeedGap
LateHistoricalLoad
Recalculation
ContractChange
TimestampJump
BidAskUnavailable
MboUnavailable
DomSnapshotStale
CfdInputStale
```

---

# 10. Contract, Roll và Participation Regime

## 10.1 Contract Engine

Theo dõi:

```text
Current Contract
Next Contract
Days to Expiration
Current Volume
Next Contract Volume
Rollover date
User-selected symbol
Continuous-contract mapping
```

Trạng thái:

```text
NORMAL_CONTRACT
EARLY_ROLL
ACTIVE_ROLL
POST_ROLL
UNKNOWN
```

Trong `ACTIVE_ROLL`:

```text
Không so raw volume giữa hai contract
Big Trade threshold tách theo contract
Composite dài ngày phải nối hoặc reset có kiểm soát
OI context bị hạ confidence
CFD basis cần tái hiệu chỉnh
```

## 10.2 GC là một cuộc đấu giá liên tục

Asia, London và COMEX không phải ba thị trường độc lập. Chúng là các chế độ tham gia:

```csharp
public enum ParticipationRegime
{
    ThinOvernight,
    AsiaActive,
    EuropeTransition,
    LondonActive,
    PreComex,
    ComexOpen,
    ComexActive,
    PostComex,
    EventRegime
}
```

Mỗi regime có distribution riêng:

```text
Volume
Trade count
Delta
Tape speed
Spread
Depth
Big Trade size
Volatility
Episode duration
Retest duration
Slippage
```

Không dùng threshold tuyệt đối xuyên mọi regime.

Ví dụ:

```text
40 contracts ở ThinOvernight
≠
40 contracts tại COMEX Open
```

## 10.3 Regime normalization

Mỗi feature lưu:

```text
RawValue
RollingMean
RollingMedian
RollingPercentile
ParticipationRegimePercentile
VolatilityAdjustedValue
ContractNormalizedValue
```

---

## 10.4 Thin Participation Classifier

`ThinParticipationClassifier` là thành phần Production Core dùng để mô tả chất lượng participation, không tự quyết định hướng.

Feature:

```text
VolumePercentileByClockBucket
TradeCountPercentile
RangePercentile
DepthPercentile nếu có
SpreadPercentile
ExecutedVolumeDensity
Duration
```

Output:

```text
NormalParticipation
ReducedParticipation
ThinParticipation
DislocatedParticipation
```

Quyền lực:

```text
Detection và tagging
→ PRODUCTION_CORE

Giảm trọng số profile contribution
→ RESEARCH_ONLY

Loại profile khỏi composite
→ DISABLED_BY_DEFAULT
```

Một overnight auction mỏng vẫn có thể mang thông tin thật sau event hoặc geopolitical shock. Vì vậy không dùng luật `thin = vô nghĩa`.

## 10.5 Settlement Proximity Tags

GC logger dùng timezone `America/New_York` và mặc định:

```text
SettlementAnchor: 13:30 ET
PreSettlementWindow: configurable offset trước anchor
SettlementTransition: cửa sổ bao quanh anchor
PostSettlementWindow: configurable offset sau anchor
```

Tag này chỉ phục vụ regime context và nghiên cứu:

```text
PRE_SETTLEMENT
SETTLEMENT_TRANSITION
POST_SETTLEMENT
```

Nó không tự tạo fade, không tự veto mọi trade và không được diễn giải thành nguyên nhân dealer forcing nếu chưa có bằng chứng.

# 11. Multi-Horizon Auction Map

GCAE không có một “trend” duy nhất. Nó có nhiều cuộc đấu giá lồng nhau.

## 11.1 Horizons

```text
Monthly / Multi-month
Weekly / Multi-week
Multi-day Composite
Daily Auction
Intraday Auction
Local Balance
Micro Execution Episode
```

Mỗi horizon có:

```text
Auction ID
Profile anchor
Value Area
POC
Balance boundaries
Directional state
Active references
Start / end / age
```

## 11.2 Năm horizon của một thesis

```text
Context Horizon
Thesis Horizon
Trigger Horizon
Management Horizon
Target Horizon
```

Ví dụ hợp lệ:

```text
Context: 5-day Composite VAL
Thesis: Intraday Failed Auction Re-entry
Trigger: 1-minute footprint/event stream
Management: Intraday
Target: Session POC → Composite POC
```

Ví dụ không hợp lệ:

```text
Trigger: 30-second imbalance
Context: Không có
Target: Weekly POC xa hàng chục USD
```

## 11.3 Source-of-Move

Hệ thống phải lưu nguồn gốc của move:

```text
Monthly/Weekly structural level
Daily composite edge
Event impulse
Short-lived liquidation impulse
Local balance breakout
Unknown
```

Không được dùng trigger micro để tự nâng thesis thành multi-session.

---

# 12. Directional Auction Context, thay cho Trend Filter

Tên module:

```text
Multi-Horizon Directional Auction Engine
```

## 12.1 Output theo horizon

```csharp
public enum DirectionalAuctionState
{
    UpDiscovery,
    DownDiscovery,
    UpRotation,
    DownRotation,
    Balance,
    Transition,
    Conflicted,
    Unknown
}
```

Mỗi snapshot gồm:

```text
Structural Bias
Tactical Bias
Execution Bias
```

Ví dụ:

```text
MULTI-DAY: UP_DISCOVERY
DAILY: BALANCE
INTRADAY: DOWN_ROTATION
MICRO: REENTRY_UP
```

Không có mâu thuẫn. Đây là nhiều horizon khác nhau.

## 12.2 Dữ liệu xác định direction

```text
Value migration
TPO POC migration
Volume POC migration
Composite centroid migration
Acceptance direction
Episode resolution history
One-Time Framing state
Price progress
Orderflow effectiveness
```

Không dùng HH/HL hoặc MA như luật duy nhất.

## 12.3 Direction ảnh hưởng thế nào?

Direction không phải hard veto tuyệt đối.

Nó điều chỉnh:

```text
Setup preference
Evidence requirement
Target distance
Expiry
Risk allowance
Execution maturity
```

Ví dụ:

```text
Long thuận structural direction
→ first valid retest có thể đủ
→ target corridor rộng hơn

Short ngược structural direction
→ cần re-entry rõ hơn
→ target gần hơn
→ expiry nhanh hơn
```

Stop vẫn dựa trên invalidation, không được nới chỉ vì “thuận trend”.

---

## 12.4 One-Time Framing Tracker

One-Time Framing là persistence measure auction-native, không phải mô hình HH/HL đơn giản và không phải trend veto.

Định nghĩa trên các TPO period đã hoàn tất:

```text
OTF Up:
Low của period hiện tại không thấp hơn low của period trước

OTF Down:
High của period hiện tại không cao hơn high của period trước
```

State:

```csharp
public enum OneTimeFramingState
{
    Unknown,
    DevelopingUp,
    ConfirmedUp,
    DevelopingDown,
    ConfirmedDown,
    Broken,
    Mixed
}
```

Lan can:

```text
Không dùng period đang hình thành để xác nhận.
Broken không tự tạo thesis đảo chiều.
OTF phải mang horizon và TPO anchor rõ.
OTF không cấm counter-trend tuyệt đối.
OTF chỉ điều chỉnh preference, maturity, target và expiry.
```

# 13. Profile Engine

## 13.1 Các profile

```text
Current Auction TPO
Current Auction Volume Profile
Previous Auction Profile
Rolling Daily Profile
Weekly Profile
Monthly Reference Profile
Composite Profile
Event-Anchored Profile
Launch-Base Profile
Local Balance Profile
```

## 13.2 Output

```text
Profile High / Low
TPO POC
Volume POC
TPO VAH / VAL
Volume VAH / VAL
HVN
LVN
nPOC
Excess Candidate
Single Prints
Poor High / Poor Low Candidate
Value Migration
POC Migration
Profile Shape
Profile Overlap
```

## 13.3 Không gọi POC là fair value tuyệt đối

POC chỉ là nơi có nhiều TPO hoặc executed volume nhất trong profile tương ứng.

Tên diễn giải ưu tiên:

```text
Liquidity Agreement Zone
```

thay vì tuyên bố:

```text
Fair Value thật
```

## 13.4 VAH / VAL là Auction Transition Zone

VAH/VAL không phải điểm Sell/Buy tự động.

Chúng là nơi Auction có thể:

```text
Reject → Rotate
Accept → Discover
Oscillate → Transition / No Trade
```

---

# 14. TPO, 30 phút và Adaptive TPO Research

## 14.1 Classic TPO

Production baseline dùng:

```text
30-minute TPO
PrimaryIntradayTpoAnchor: 08:20 America/New_York
AnchorPolicy: Configurable
DstPolicy: Timezone-aware
```

Lý do:

```text
Quy ước AMT phổ biến
Dễ đối chiếu khóa học và tài liệu
Ổn định cho context
Không tối ưu hóa sớm
```

30 phút không phải định luật thị trường.

Mặc định 08:20 ET chỉ áp dụng cho `Primary COMEX Intraday TPO`. Continuous Global Auction, Weekly, Composite, Event-Anchored, Launch-Base và Local Balance Profile có anchor policy riêng.

## 14.2 Volume Profile không phụ thuộc 30 phút

VP đo executed volume theo price và có thể anchor theo:

```text
Daily
Weekly
Composite
Session
Event
Swing
Balance
Launch Base
```

## 14.3 Adaptive TPO, Research-only

Các biến thể nghiên cứu:

```text
5m / 10m / 15m / 20m / 30m
Volatility-based TPO
Volume-based TPO
Trade-count-based TPO
Participation-normalized TPO
Auction-state-based segmentation
```

## 14.4 Auction-based TPO

Ý tưởng:

```text
Balance chưa đổi trạng thái
→ tiếp tục cùng Auction Segment

Break + developing acceptance
→ mở segment mới

New value established
→ profile mới hoặc merge theo rule
```

Đây là `WORKING_HYPOTHESIS`, không thay Classic TPO trong Production cho đến khi chứng minh incremental value.

## 14.5 Acceptance không chỉ là Time

```text
Time Acceptance
+
Volume Acceptance
+
Orderflow Acceptance
+
Price Maintenance
=
Acceptance Evidence Vector
```

Một vùng ở lâu nhưng volume thấp và aggression thất bại không mặc nhiên là value mạnh.

Một vùng tồn tại ngắn nhưng executed volume lớn, local POC hình thành và price maintenance tốt có thể là early value formation.

---

# 15. Composite Profile Engine

Composite không được merge cứng theo N ngày.

## 15.1 Tiếp tục merge khi

```text
Value overlap đủ lớn
POC chưa dịch bền vững khỏi vùng cũ
Chưa xây acceptance rõ bên ngoài
Không có structural separation
Profile mới vẫn xoay quanh composite core
```

Ngoài các điều kiện trên, mỗi profile contribution phải mang `ParticipationQuality`. Thin participation không bị xóa tự động; core chỉ gắn tag, còn attenuation/exclusion là Research-only.

## 15.2 Đóng composite khi

```text
POC migration bền vững
Value Area mới tách khỏi old value
Nhiều period duy trì phía ngoài
Retest old value thất bại
Volume build-up hình thành ở vùng mới
```

## 15.3 Feature

```text
ValueOverlapRatio
PocDisplacement
ValueCentroidDisplacement
PocMigrationVelocity
CompositeStability
TimeInsideValue
FailedExcursionCount
ParticipationQuality
ThinContributionFlag
```

## 15.4 Composite State

```csharp
public enum CompositeState
{
    StableBalance,
    ExpandingUp,
    ExpandingDown,
    BreakingUp,
    BreakingDown,
    NewValueUp,
    NewValueDown,
    Transition
}
```

---

# 16. Value, POC, HVN, LVN và nPOC Intelligence

## 16.1 POC Migration Velocity

Không chỉ đo POC đã dịch bao nhiêu tick, mà đo:

```text
Distance
÷
Time
```

Ví dụ:

```text
+8 ticks trong 15 phút
≠
+8 ticks trong 5 giờ
```

Lưu cả:

```text
TPO POC velocity
Volume POC velocity
Local POC velocity
Composite POC velocity
```

## 16.2 POC Strength

POC Strength là research index cấu thành từ:

```text
Time concentration
Executed volume concentration
Age
Repeated acceptance
Profile stability
Test history
Source horizon
```

Không được gọi “Institutional Magnet”.

## 16.3 nPOC Lifecycle

```text
Fresh
Untested
Partially Tested
Fully Tested
Accepted Through
Reactivated
Expired
```

Metadata:

```text
Origin auction
Age
Distance
Source horizon
Intervening value
Test count
Reaction history
```

## 16.4 HVN, Liquidity Friction

HVN có thể làm tăng rotational friction, nhưng không phải luôn support/resistance.

Research output:

```text
FrictionIndex
ExpectedTempoReduction
RotationProbabilityCandidate
```

## 16.5 LVN, Auction Vacuum

LVN có thể là vùng ít agreement và giá có thể đi nhanh nếu được chấp nhận xuyên qua.

Research output:

```text
VacuumWidth
RelativeVolumeDeficit
VacuumStrengthIndex
NearestFrictionZone
```

Không dùng “LVN rộng = chắc chắn chạy”.

---

## 16.6 Single Print Formation Context

Single Print không được mặc định là magnet hoặc bắt buộc fill. `SinglePrintFormationContext` nghiên cứu activity đúng lúc corridor được hình thành.

Trạng thái:

```text
RESEARCH_ONLY
CAPABILITY_GATED khi cần Bid/Ask execution history chi tiết
```

Feature:

```text
SinglePrintStartTime
SinglePrintPriceRange
BidExecutedAtFormation
AskExecutedAtFormation
ZeroBidPriceCount
ZeroAskPriceCount
TradeCountAtFormation
VolumeAtFormation
PriceVelocity
CorrespondingActivity
SubsequentFillDepth
SubsequentAcceptance
```

Output mô tả:

```text
OneSidedBuyExecutionDominant
OneSidedSellExecutionDominant
TwoSidedThinExecution
ExecutionClassificationUnavailable
Unknown
```

Không xuất `Forced Buyer`, `Forced Seller` hoặc `Institutional Initiative Confirmed` nếu chưa có mô hình độc lập được kiểm định.

# 17. Structural Reference Engine

Mỗi reference là object có vòng đời.

## 17.1 Types

```text
Prior Day High / Low
Prior Day VAH / VAL / POC
Weekly High / Low
Weekly VAH / VAL / POC
Monthly references
Composite VAH / VAL / POC
IB High / Low
Current Auction High / Low
TPO POC / VPOC
VWAP
Swing High / Low
Launch Base
Single Print Boundary
HVN / LVN Boundary
nPOC
Poor High / Poor Low Candidate
Origin of Move
Event High / Low
Dynamic Executed Level
DOM-derived Candidate Level
```

## 17.2 Lifecycle

```csharp
public enum ReferenceStatus
{
    Fresh,
    Active,
    Approaching,
    Interacting,
    OutsideAttemptActive,
    AcceptedThrough,
    Reaccepted,
    Rotational,
    Exhausted,
    Expired,
    Retired
}
```

Một reference bị giá xuyên qua lại nhiều lần có thể trở thành vùng rotation và mất tính phân định.

## 17.3 Metadata

```csharp
public sealed record StructuralReference(
    string ReferenceId,
    ReferenceType Type,
    decimal ZoneLow,
    decimal ZoneHigh,
    TimeframeSource Source,
    DateTimeOffset CreatedAt,
    int TestCount,
    ReferenceStatus Status,
    EvidenceTier EvidenceTier,
    IReadOnlyDictionary<string, double> Features);
```

## 17.4 Reference Hierarchy

Không xếp hạng bằng một điểm duy nhất. Hệ thống hiển thị cấu thành:

```text
Source horizon
Composite location
Profile confluence
Age
Test count
Reaction history
Executed activity
Current state
IBHighWasDayHigh / IBLowWasDayLow khi reference thuộc IB và ngày đã hoàn tất
CurrentDayExtremeStillEqualsIBExtreme trong live session
```

---

# 18. Auction State và Auction Cycle

## 18.1 Core states

```csharp
public enum AuctionState
{
    Unknown,
    ValueBuilding,
    Balance,
    ExtremeTesting,
    UpDiscovery,
    DownDiscovery,
    ReentryDeveloping,
    BuildingNewValueUp,
    BuildingNewValueDown,
    Rotation,
    Compression,
    Distribution,
    Transition,
    Conflicted
}
```

## 18.2 Chu kỳ

```text
Value Building
→ Extreme Testing
→ Rejection / Re-entry
→ Rotation
```

hoặc:

```text
Value Building
→ Extreme Testing
→ Acceptance
→ Trend Development / Discovery
→ Distribution / New Value
→ Extreme Testing mới
```

## 18.3 Tám microstates nghiên cứu

```text
Strong Trend
Weak Trend
Momentum Exhaustion Candidate
Passive Absorption Candidate
Aggressive Reversal Candidate
Rotation
Compression
Transition
```

Những nhãn này là descriptive states, không tự tạo lệnh.

---

## 18.4 Day Structure Classifier, phục hồi từ v0.2

Việc Day-Type biến mất khỏi v1.1 là sơ suất coverage, không phải quyết định loại bỏ. Nó được khôi phục dưới tên `DayStructureClassifier` với trạng thái `RESEARCH_ONLY`.

```csharp
public enum DayStructureState
{
    Unknown,
    NormalCandidate,
    NormalVariationCandidate,
    NeutralCandidate,
    NonTrendCandidate,
    TrendUpCandidate,
    TrendDownCandidate,
    DoubleDistributionUpCandidate,
    DoubleDistributionDownCandidate,
    Transition,
    PostSessionConfirmed
}
```

Nguyên tắc:

```text
Candidate label được revision trong ngày.
Current Day Structure là classification, không phải prediction.
Không tự tạo entry.
Không khóa quá sớm.
Không feed trực tiếp vào Hard Veto.
```

Giai đoạn đầu engine chỉ log:

```text
DayStructureState
RevisionCount
IBWidthPercentile
RangeExtension
ValueMigration
POCMigration
CloseLocation
DoubleDistributionEvidence
Outcome
```

Sau validation, từng feature mới được phép điều chỉnh Expectation, Target Horizon, Expiry, fade/continuation preference hoặc Management Hold Class.

### 18.4.1 IB Extreme Study

Để tránh look-ahead, tách hai lớp:

```text
Live feature:
CurrentDayExtremeStillEqualsIBExtreme

End-of-day labels:
IBHighWasDayHigh
IBLowWasDayLow
```

Historical Scanner đo ngày kế tiếp:

```text
FirstTouchOccurred
TimeToFirstTouch
ApproachDirection
ReactionMFE
PenetrationMAE
AcceptedThrough
Rejected
NoTouch
ParticipationRegimeAtTouch
```

Tên nghiên cứu chuẩn: `NextAuctionIBExtremeRevisitStudy`. Không gọi IB extreme là attractor trước validation.

# 19. Sửa sai “Sweep”: Reference Excursion khác MBO Sweep

Đây là thay đổi kiến trúc quan trọng nhất sau v0.2.

## 19.1 Reference Excursion

Khi giá giao dịch ra ngoài một reference:

```text
Reference Excursion
hoặc
Outside Auction Attempt
```

Nó không chứng minh:

```text
Stop đã bị quét
Liquidity đã bị săn
Trader bị trapped
Giá sẽ đảo chiều
```

## 19.2 MBO Sweep

`MboSweepEvent` là aggressive flow lấy thanh khoản nhanh qua nhiều mức order book, được phát hiện từ dữ liệu trades/order book phù hợp.

```text
ReferenceExcursion
≠
MboSweepEvent
```

MBO Sweep chỉ là tag microstructure. Nó có thể xuất hiện trong continuation hoặc failure.

## 19.3 Thuật ngữ bị retire

```text
SweepDetector            → RETIRED
SweepReclaimEngine       → RETIRED AS CORE NAME
```

Legacy display alias có thể giữ để người dùng quen thuật ngữ, nhưng logic chính thức dùng:

```text
Failed Auction Re-entry
Accepted Auction Continuation
```

---

# 20. Auction Episode Engine

Một lần giá tương tác với reference tạo một **Episode**, không tạo tín hiệu ngay.

## 20.1 Episode object

```csharp
public sealed class AuctionEpisode
{
    public string EpisodeId { get; init; }
    public string ReferenceId { get; init; }
    public AuctionDirection Direction { get; init; }
    public DateTimeOffset StartedAt { get; init; }

    public int AttemptCount { get; set; }
    public decimal MaximumOutsideDistance { get; set; }
    public TimeSpan OutsideDuration { get; set; }
    public decimal OutsideExecutedVolume { get; set; }
    public decimal OutsideAggressiveVolume { get; set; }
    public decimal OutsideDelta { get; set; }
    public long OutsideTradeCount { get; set; }

    public decimal? LocalPoc { get; set; }
    public decimal? LocalValueLow { get; set; }
    public decimal? LocalValueHigh { get; set; }

    public EpisodeState State { get; set; }
    public EpisodeResolution Resolution { get; set; }
}
```

## 20.2 State machine

```text
IDLE
→ APPROACHING_REFERENCE
→ INTERACTING
→ OUTSIDE_ATTEMPT
→ DEVELOPING
```

Từ `DEVELOPING`:

```text
→ ACCEPTANCE_OUTSIDE
→ REENTRY_DEVELOPING
→ UNRESOLVED_ROTATION
→ EPISODE_EXPIRED
```

Từ `REENTRY_DEVELOPING`:

```text
→ REACCEPTED_INSIDE
→ REENTRY_FAILED
```

## 20.3 Episode resolution

```csharp
public enum EpisodeResolution
{
    None,
    AcceptedOutside,
    ReacceptedInside,
    UnresolvedRotation,
    TransitionedToNewBalance,
    Expired,
    InvalidData
}
```

## 20.4 Nhiều lần xuyên cùng reference

```text
Attempt 1
→ quay vào

Attempt 2
→ xuyên sâu hơn

Attempt 3
→ local POC bắt đầu dịch ra ngoài
```

Hệ thống cập nhật:

```text
AttemptCount += 1
```

Không tạo ba Long alerts.

## 20.5 Episode reset

Một episode mới chỉ được mở khi episode trước đã đóng và có reset condition:

```text
Giá rời reference đủ xa
Local balance mới hình thành
Value tái cấu trúc
Context horizon đổi
Reference mới được tạo
```

Khoảng reset là calibrated parameter, không hardcode cảm tính.

---

# 21. Acceptance, Re-entry và Auction Resolution

## 21.1 Acceptance là quá trình liên tục

```text
No Evidence
Early Outside Activity
Developing Acceptance
Probable Acceptance
Established Value
Acceptance Failed
```

Production không cần dùng chữ `Probability` trừ khi được calibrate. Enum có thể là:

```csharp
public enum AcceptanceState
{
    None,
    Early,
    Developing,
    Established,
    Failed,
    Unknown
}
```

## 21.2 Acceptance Evidence Vector

```text
OutsideTimeRatio
OutsideVolumeRatio
OutsideTradeCountRatio
OutsideCloseRatio
TpoCountOutside
LocalPocDisplacement
ValueCentroidDisplacement
OldValueReclaimFailure
RetestHoldQuality
PriceMaintenance
```

## 21.3 Re-entry Evidence Vector

```text
ReentrySpeed
DistanceReturnedInside
TimeMaintainedInside
ExecutedVolumeDuringReentry
OppositeAggression
LocalPocResponse
LocalValueRebuildInside
SubsequentReferenceTest
OldDirectionAggressionEffectiveness
```

## 21.4 Phân biệt các dạng quay vào

```text
Temporary Cross Back
Mechanical Bounce
Partial Re-entry
Stable Reacceptance
```

Chỉ `Stable Reacceptance` mới đủ nền cho Failed Auction Re-entry thesis.

## 21.5 Không dùng một cây nến

```text
Wick xuyên low
+
Close phía trên
≠
Failed Auction
```

Cần dữ liệu hậu sự kiện.

---

# 22. Orderflow Core

## 22.1 Raw features

```text
Ask Volume
Bid Volume
Delta
CVD
Trade Count
Executed Volume
Bid Imbalance
Ask Imbalance
Stacked Imbalance
Extreme Delta
Extreme Volume
Close Position
Price Progress
Repeated Extreme Tests
Time Between Trades
Contracts Per Second
```

## 22.2 Delta

```text
Delta = Ask Volume − Bid Volume
```

Delta không cho biết:

```text
Mở hay đóng vị thế
Hedge hay speculation
Fresh long hay short covering
Trader lớn hay nhỏ
Ý định người giao dịch
```

Khi thiếu OI/position data hợp lệ, chỉ được nói:

```text
Aggressive buying
Aggressive selling
```

## 22.3 CVD

CVD là cumulative executed aggression. Nó là context/evidence, không phải direction signal độc lập.

---

# 23. Auction Efficiency và Trade Facilitation Engine

Đây là cầu nối trung tâm giữa AMT và Orderflow.

## 23.1 Effort

```text
Aggressive volume
Absolute Delta
Imbalance count
Stacked imbalance
Trade count
Tape speed
Big Trade activity
MBO Sweep activity
Stop activity tag
```

## 23.2 Result

```text
Net ticks moved
Range expansion
Maximum favorable progress
Close location
Time required to move
Acceptance development
POC migration
Value migration
Price maintenance
```

## 23.3 Output states

```text
EffortResultBalanced
AggressionEffective
AggressionIneffective
PotentialPassiveAbsorption
PotentialExhaustion
TradeFacilitationHealthy
TradeFacilitationFailing
Unknown
```

## 23.4 Trade Facilitation

Hai câu hỏi:

```text
Market đang cố đi đâu?
Market có làm tốt việc đi theo hướng đó không?
```

Research index:

```text
TradeFacilitationIndex = normalized relationship of direction-consistent effort and achieved auction progress
```

Không dùng công thức một chiều `Delta / ticks` vì zero progress, volatility và regime sẽ làm méo kết quả.

## 23.5 Absorption language

Không báo:

```text
Confirmed Whale Buying
```

Chỉ báo:

```text
Sell aggression elevated
Downside progress inefficient
No acceptance below reference
Potential passive buy-side absorption
```

Sau đó vẫn cần opposite active flow hoặc stable re-entry.

---

# 24. ATAS Ultra Integration Matrix

AMT quyết định **ở đâu và khi nào mở kính hiển vi**. Không chạy mọi module với cùng mức ưu tiên trên toàn chart.

```text
Middle of Composite Value
→ microstructure priority thấp

Approaching Composite Edge
→ executed response monitoring tăng

Auction Episode active
→ high-resolution recorder bật

Acceptance / Re-entry decision
→ DOM/MBO research tags tăng ưu tiên
```

## 24.1 Cluster Search

Dùng để tìm:

```text
Executed concentration tại extreme
Repeated aggressive clusters
Opposite clusters trong re-entry
Cluster persistence qua attempts
```

Không dùng:

```text
Cluster xuất hiện → Buy/Sell
```

## 24.2 Cluster Statistic

Dùng cho:

```text
Bid
Ask
Volume
Delta
Trade count
Range
Close position
```

Là benchmark bar-level cho Effort vs Result.

## 24.3 Smart Tape

Nghiên cứu:

```text
Cumulative trade reconstruction
Contracts per second
Number of levels crossed
Burst duration
Pause duration
Progress per contract
Opposite response
```

Output:

```text
AccelerationEffective
AccelerationIneffective
BurstIntoLiquidity
TapeExhaustionCandidate
```

## 24.4 Big Trades / Adaptive Big Trades

Không nhập thẳng threshold mặc định.

GCAE tự lưu:

```text
TradeSizePercentileByRegime
SequenceSize
Direction
Location
FollowThrough
OppositeResponse
```

Big Trade chỉ có ý nghĩa khi đặt trong Location và Episode.

## 24.5 DOM / Smart DOM / Heatmap / DOM Levels

Đo theo chuỗi thời gian:

```text
LiquidityAdded
LiquidityRemoved
Persistence
DistanceFromPrice
PriceApproach
ExecutionOutcome
```

Snapshot đơn lẻ không đủ.

## 24.6 MBO DOM

Nếu capability cho phép:

```text
Order lifetime
Order modification
Queue priority
Queue survival
Cancellation
Executed portion
Reload behavior
Order ID lifecycle
```

MBO là kính hiển vi cho order lifecycle, không phải máy đọc ý định.

## 24.7 Pulling / Stacking

Câu hỏi nghiên cứu:

```text
Stacking tồn tại bao lâu?
Giá có tiến tới không?
Liquidity bị khớp hay bị rút?
Pulling xảy ra trước hay sau price movement?
```

## 24.8 Iceberg

Phân biệt:

```text
Native/absolute confirmation
Medium/indirect confirmation
Synthetic reconstruction
Statistical inference
```

Một bid iceberg có thể bị tiêu thụ và giá vẫn giảm. Iceberg không tự tạo Long.

## 24.9 Stops Tracker

Stop run cho thấy stop mechanics được kích hoạt. Nó không cho biết stop dùng để exit hay enter.

Tag:

```text
STOP_EVENT_PRESENT
```

## 24.10 Sweeps Tracker

Tag:

```text
MBO_SWEEP_PRESENT
```

MBO Sweep là aggressive liquidity removal, không phải Failed Auction.

## 24.11 Market Power, DOM Power, DOM Strength

Black box cho đến khi audit:

```text
Algorithm
Latency
Repaint
Historical/live compatibility
Incremental value
```

## 24.12 Dynamic Levels

Chỉ trở thành candidate reference khi biết:

```text
Source data
Executed hay displayed
Age
Test count
Acceptance-through state
```

## 24.13 Volume Statistic

Dùng làm regime normalization và benchmark, không tạo direction độc lập.

---

# 25. Auction Energy, Tempo, Pressure, Quality, Genome và GPS

Đây là các ý tưởng nghiên cứu quan trọng được hình thành sau v0.2.

## 25.1 Auction Energy / Potential

Không tuyên bố thị trường “tích năng lượng” theo nghĩa vật lý.

Research index có thể kết hợp:

```text
Compression duration
Rotation count
Range compression
Volume compression
Delta build-up
POC stability/drift
DOM compression
Time in balance
```

Tên an toàn:

```text
AuctionPotentialIndex
```

Câu hỏi kiểm định:

```text
Index cao có dự báo expansion tốt hơn baseline volatility không?
```

## 25.2 Auction Tempo

```text
Slow
Normal
Fast
Dislocated
```

Feature:

```text
Rotation frequency
Trade rate
Volume rate
POC drift rate
Range expansion rate
Episode state-change rate
```

Tempo ảnh hưởng:

```text
Entry urgency
Order type
Expiry
Slippage allowance
Retest expectation
```

## 25.3 Auction Pressure

Pressure không đồng nghĩa Delta.

Evidence vector:

```text
Aggressive volume
Tape speed
Big Trades
MBO Sweeps
Displayed liquidity change
Pulling / Stacking
Iceberg / reload tags
Price progress
```

Không gộp Tier 1, Tier 2 và Tier 3 thành một con số mà quên provenance.

## 25.4 Auction Quality / Breakout Quality

Một breakout 10 ticks có thể có chất lượng khác nhau.

Feature:

```text
Acceptance
POC migration
Value formation
Trade facilitation
Liquidity condition
Follow-through
Re-entry failure
```

## 25.5 Auction Genome

Mỗi snapshot có thể mã hóa:

```text
State
Directional Context
Energy/Potential
Tempo
Acceptance
Pressure
Liquidity Condition
Trade Facilitation
POC Migration
Composite State
Execution Readiness
```

Ví dụ:

```text
State: Balance
Potential: High
Acceptance Outside: Low
Pressure: Up, unproven
Tempo: Slow
POC Migration: Mild Up
Execution: Observe
```

## 25.6 Auction GPS

GCAE ưu tiên nói:

```text
Bạn đang ở đâu trong chu kỳ Auction?
```

trước khi nói:

```text
Long hay Short?
```

GPS output:

```text
Market Phase
Value Location
Episode State
Auction Health
Target Corridor
Execution Readiness
```

## 25.7 Production eligibility

Các index trên ở `Research Lab` cho đến khi:

```text
Feature definition ổn định
No lookahead
Distribution theo regime rõ
Calibration out-of-sample
Incremental value vượt core state machine
```

---

# 26. Thesis Family 1: Failed Auction Re-entry (FAR)

Legacy alias:

```text
Sweep-Reclaim Reversal
```

Tên legacy chỉ dùng cho giao diện hoặc log migration. Logic chính thức không bắt đầu từ “sweep”.

## 26.1 Cơ chế

```text
Giá thử đấu giá bên ngoài reference
→ không xây được acceptance bền vững bên ngoài
→ quay lại vùng Auction trước
→ duy trì được bên trong
→ aggression theo hướng excursion mất hiệu quả
→ opposite flow hoặc price maintenance xác nhận
→ Failed Auction Re-entry Thesis
```

## 26.2 FAR Long

```text
Outside attempt below reference
→ no established value below
→ stable re-entry above/inside
→ sell effort ineffective
→ re-entry defended or maintained
→ Long thesis
```

## 26.3 FAR Short

Đối xứng phía trên reference.

## 26.4 State machine

```csharp
public enum FarState
{
    Idle,
    Approaching,
    EpisodeActive,
    OutsideAttempt,
    ReentryDeveloping,
    ReacceptedInside,
    Armed,
    MicroRetest,
    StructuralRetest,
    ConfirmedAttempt,
    Executable,
    Managing,
    Invalidated,
    Expired,
    Completed
}
```

## 26.5 Điều kiện cần

```text
Data valid
Reference active
Episode resolved hoặc đủ evidence re-entry
No established acceptance against thesis
Entry-to-invalidation geometry xác định được
Target corridor còn khoảng trống
No hard veto
```

## 26.6 Two-Attempt Failure

Là variant của FAR:

```text
Attempt 1
→ re-entry/rejection

Attempt 2
→ aggression lặp lại
→ price progress kém hơn hoặc không tạo acceptance
→ stable re-entry
```

Log riêng:

```text
AttemptCount
EffortAttempt1 / ResultAttempt1
EffortAttempt2 / ResultAttempt2
Distance / time between attempts
```

Không mặc định attempt 2 luôn mạnh hơn attempt 1.

---

# 27. Thesis Family 2: Accepted Auction Continuation (AAC)

Legacy alias:

```text
Break-Accept-Retest Continuation
```

## 27.1 Cơ chế

```text
Giá thử đấu giá bên ngoài reference
→ time/volume/trades phát triển bên ngoài
→ local POC hoặc value hình thành
→ old value không được reclaim bền vững
→ pullback không tạo reacceptance vào vùng cũ
→ continuation thesis
```

## 27.2 AAC Long

```text
Outside attempt above reference
→ developing acceptance up
→ value/POC migration up
→ old value reclaim fails
→ pullback holds outside or quickly restores accepted zone
→ Long continuation thesis
```

## 27.3 AAC Short

Đối xứng phía dưới.

## 27.4 State machine

```csharp
public enum AacState
{
    Idle,
    Approaching,
    EpisodeActive,
    OutsideAttempt,
    AcceptanceDeveloping,
    AcceptedOutside,
    Pullback,
    Armed,
    Executable,
    Managing,
    ReacceptedOldValue,
    Invalidated,
    Expired,
    Completed
}
```

## 27.5 Invalidation cốt lõi

```text
Reacceptance bền vững vào old value
```

Một wick quay vào chưa đủ. Cần xem:

```text
Dwell time
Executed volume
Local POC
Local value
Price maintenance
```

---

# 28. Double Distribution Intelligence

Double Distribution chưa được nâng thành setup thứ ba độc lập. Nó là:

```text
Market State Evidence
Value Migration Evidence
Continuation / Failure Context
```

## 28.1 Healthy Double Distribution Candidate

```text
Distribution 1 đủ cô đặc
→ initiative move rõ
→ low-volume separation / single-print corridor
→ Distribution 2 hình thành
→ value mới được duy trì
→ follow-through xuất hiện
```

## 28.2 Follow-through Validator

Theo dõi:

```text
Next-period acceptance
Second distribution maintenance
POC migration
Line-in-the-sand hold
Single-print corridor behavior
Old distribution re-entry
```

## 28.3 Failure

```text
Distribution 2 yếu
Poor extreme candidate
Không có follow-through
Quay xuyên line in the sand
Reaccepted into distribution 1
```

Có thể chuyển:

```text
AAC Candidate
→ Thesis Weakening
→ Failed Continuation
→ FAR Candidate theo hướng ngược
```

## 28.4 Không hardcode

Các phát biểu như “distribution đầu phải nhỏ” hoặc “ngày sau bắt buộc follow-through” được lưu thành hypotheses và feature, không luật tuyệt đối.

---

# 29. Signal Maturity và Scalp Modes

GCAE phục vụ scalp, nhưng không nới AMT Context và Hard Veto.

## 29.1 Analysis lifecycle

```text
OBSERVATION
→ APPROACHING
→ EPISODE_ACTIVE
→ CANDIDATE
→ ARMED
→ EXECUTABLE
→ MANAGING
→ COMPLETED / INVALIDATED / EXPIRED
```

## 29.2 Fast, Standard, Confirmed

### FAST

Dùng khi:

```text
Location mạnh
Episode resolution đủ rõ
No contrary acceptance
Re-entry/acceptance mạnh
Micro pullback hoặc micro trigger hợp lệ
Target space tốt
```

Không cần structural retest sâu, nhưng không được vào chỉ vì một wick.

### STANDARD

```text
Episode resolution
→ first structural retest/pullback
→ opposing aggression không tạo progress
→ execution trigger
```

Đây là mode mặc định cho scalp cân bằng.

### CONFIRMED

```text
Episode resolution
→ structural retest
→ value/POC confirmation hoặc second attempt failure
→ execution trigger
```

Dùng khi volatility cao, event vừa xảy ra hoặc context còn transition.

## 29.3 Retest policy cuối cùng

Retest không phải định luật tuyệt đối.

```text
No retest
→ không tự động loại
→ chỉ được FAST khi có micro-confirmation đủ mạnh

Structural retest
→ STANDARD

Second attempt / mature confirmation
→ CONFIRMED
```

Không còn rule cứng “mọi Production signal bắt buộc structural retest”.

## 29.4 Tần suất tín hiệu

Không định trước số tín hiệu/ngày.

Logger phải đo:

```text
Candidates per day
Armed per day
Fast executable
Standard executable
Confirmed executable
Signals by regime
Signals by volatility
Signals by reference type
```

---

## 29.5 FAST Deployment Guardrail

FAST tồn tại trong taxonomy nhưng mặc định chỉ chạy shadow:

```csharp
EnableFastCandidateDetection = true;
EnableFastShadowLogging = true;
EnableFastActionAlerts = false;
EnableFastExecutablePlans = false;
EnableFastRiskSizing = false;
```

Operating policy:

```text
FAST       → SHADOW_ONLY by default
STANDARD   → DEFAULT_EXECUTION_MODE
CONFIRMED  → ENABLED when context permits
```

FAST chỉ được promotion sau:

```text
Historical Scanner
→ Targeted Replay
→ Live Shadow
→ Out-of-sample validation
→ Limited-risk pilot
→ Production promotion
```

Đánh giá không chỉ bằng win rate. Phải phân tầng theo ReferenceType, ParticipationRegime, VolatilityRegime, ThesisType, EpisodeResolutionType, EntryPolicy, MAE, MFE, TimeToProgress, ExecutableRR, Slippage, FalseReentryRate và ContraryAcceptanceAfterEntry.

# 30. Entry Policy Engine

GCAE không cố định Limit hay Market.

## 30.1 Các kiểu execution plan

```text
Passive Limit
Marketable Limit
Stop-Market
Stop-Limit
Market
Hybrid / Staged
Observe Only
```

## 30.2 Nguyên tắc

```text
Location được chuẩn bị trước
Trigger quyết định cách vào
```

## 30.3 Passive Limit

Phù hợp khi:

```text
Reference và defended zone rõ
Tempo không quá nhanh
Spread/depth bình thường
Risk of missed trade chấp nhận được
Episode đã resolve đủ
```

Không dùng blind limit chỉ vì giá chạm VAH/VAL/LVN/nPOC.

## 30.4 Market / Marketable Limit

Phù hợp khi:

```text
Market vừa chứng minh resolution
Tempo nhanh
Re-entry/acceptance đang mở rộng
Risk bỏ lỡ lớn hơn slippage kỳ vọng
Liquidity condition còn chấp nhận được
```

## 30.5 Stop entry

Có thể dùng khi cần xác nhận giá vượt micro trigger sau pullback/retest.

## 30.6 Order Type Selector input

```text
Auction Tempo
Spread
Depth
DOM persistence
Distance to invalidation
Urgency
Expected slippage
Missed-trade cost
Signal maturity
CFD broker constraints
```

## 30.7 Không dùng tỷ lệ cố định

Không khóa “70–80% Market” hoặc tỷ lệ bất kỳ. Distribution order type phải được đo thực tế.

---

# 31. Entry Zone Calculation

Entry là zone, không nhất thiết một giá.

## 31.1 FAR Long

Candidate inputs:

```text
Reference Zone
Episode Extreme
Re-entry Boundary
Re-entry Confirmation
Micro Pullback Low
Structural Retest Range
Defended Execution Area
Micro Swing Trigger
```

### FAST

```text
Stable re-entry
+
Micro trigger / shallow pullback hold
```

### STANDARD

```text
Intersection of Reference Zone
+ Structural Retest Range
+ Executed Defense Area
```

### CONFIRMED

```text
Break of micro swing after mature retest/second attempt
```

## 31.2 AAC Long

Inputs:

```text
Old Value Edge
Accepted Zone
New Local Value
New Local POC
Pullback Extreme
Continuation Trigger
```

Entry chỉ hợp lệ khi old value chưa được reaccepted.

## 31.3 Entry feasibility

```text
EntryZoneWidth
ExpectedFillProbabilityCandidate
SlippageAllowance
DistanceToInvalidation
DistanceToFirstBarrier
ExecutableRR
```

---

# 32. Thesis Contract và Invalidation

Mỗi trade idea phải có `Thesis Contract` trước khi executable.

## 32.1 Thesis Contract

```csharp
public sealed record ThesisContract(
    string ThesisId,
    ThesisType Type,
    AuctionDirection Direction,
    TimeframeSource ContextHorizon,
    TimeframeSource ThesisHorizon,
    TimeframeSource TriggerHorizon,
    TimeframeSource ManagementHorizon,
    TimeframeSource TargetHorizon,
    string ExpectedBehavior,
    PriceInvalidation PriceInvalidation,
    AuctionInvalidation AuctionInvalidation,
    TimeInvalidation TimeInvalidation,
    ContextInvalidation ContextInvalidation,
    DateTimeOffset ExpiresAt);
```

## 32.2 Bốn loại invalidation

### Price Invalidation

Giá vượt structural protection level.

### Auction Invalidation

Thị trường xây acceptance chống lại thesis.

### Time Invalidation

Thesis không tạo progress trong thời gian hợp lý.

### Context Invalidation

Higher-horizon state thay đổi hoặc reference mất hiệu lực.

## 32.3 Protective hard stop

```text
Analytical Invalidation
≠
Protective Hard Stop
```

Hard stop là bảo hiểm cho:

```text
Disconnect
News shock
Slippage
Human delay
CFD platform issue
```

Có thể chủ động exit trước hard stop, nhưng baseline không khuyến nghị mental stop thay thế hoàn toàn.

## 32.4 Stop calculation

```text
Structural Invalidating Price
+
Adaptive Execution Buffer
+
CFD Mapping Allowance
```

Buffer xét:

```text
Tick volatility
Recent MAE distribution
Participation regime
Event state
Spread
Basis uncertainty
Expected slippage
```

Không dùng fixed ticks xuyên mọi regime.

---

# 33. Path of Least Auction Resistance và Target Engine

Tên module:

```text
PathOfLeastAuctionResistanceEngine
```

## 33.1 Target candidates

```text
Local POC
Session POC
VWAP
Prior POC
Composite POC
nPOC
HVN
LVN boundary
Origin of Move
Opposing Value Edge
Prior High / Low
Weekly / Monthly Reference
Launch Base
Event reference
```

## 33.2 Intermediate Barriers

Từ entry đến target, hệ thống lập corridor:

```text
Barrier 1
Barrier 2
Barrier 3
Final Target
```

Mỗi barrier có:

```text
Source horizon
Distance
Expected friction
Reaction history
Current value state
Progress milestone
```

### 33.2.1 Adjacent Build Ratio, Research-only

Đối với HVN hoặc intermediate barrier, engine nghiên cứu mức time và executed volume được xây sát một phía trước khi xuyên.

```text
ApproachSideBuild
= time + executed volume trong dải sát barrier phía đang tiếp cận

OppositeSideBuild
= time + executed volume trong dải đối xứng phía bên kia

AdjacentBuildRatio
= ApproachSideBuild
  ÷ (ApproachSideBuild + OppositeSideBuild)
```

Lưu riêng:

```text
TimeBuildRatio
VolumeBuildRatio
TradeCountBuildRatio
LocalPocDistance
ValueCentroidDistance
BarrierPenetration
PostPenetrationMaintenance
```

Không dùng luật `ratio cao = chắc chắn xuyên`. Câu hỏi nghiên cứu là liệu one-sided adjacent build có cải thiện ước lượng barrier permeability ngoài baseline location, tempo và volatility hay không.

## 33.3 Progress Milestones

Nếu thesis higher timeframe đúng, giá phải xử lý các barrier trung gian.

Không phá barrier:

```text
không tự động invalid thesis
```

nhưng cập nhật:

```text
SlowProgress
EarlyWeakness
RetestRisk
TargetDowngradeCandidate
```

## 33.4 TP structure

### FAR scalp

```text
TP1: Local POC / micro balance
TP2: VWAP / Session POC
TP3: Composite POC / opposing value edge
```

### AAC scalp

```text
TP1: Local accepted-value objective
TP2: Next HVN / nPOC
TP3: Prior structural high/low or higher-horizon reference
```

## 33.5 Target horizon alignment

```text
Micro trigger + intraday thesis
→ intraday target

Micro trigger + weekly source thesis
→ target xa hơn chỉ khi thesis thật sự weekly và management phù hợp
```

## 33.6 Confirmation Premium

Entry muộn sau confirmation:

```text
Evidence trưởng thành hơn
nhưng giá xấu hơn
SL geometry thay đổi
RR giảm
```

GCAE có thể xuất:

```text
ANTICIPATORY PLAN
CONFIRMATION PLAN
```

Không mặc định một plan vượt trội.

---

# 34. RR và CFD Mapping

## 34.1 Theoretical RR

Dựa trên GC structure.

## 34.2 Executable RR

Sau:

```text
CFD spread
Basis
Basis volatility allowance
Entry slippage
Stop slippage
Lot rounding
```

Chỉ Executable RR được dùng cho execution feasibility.

## 34.3 Basis

```text
Basis = CFD Price − GC Price
```

Engine theo dõi:

```text
Current Basis
Rolling Median Basis
Basis Volatility
Basis Staleness
CFD Spread
Expected Slippage
```

## 34.4 Output

```text
GC Entry Zone
Estimated CFD Entry Zone
GC Invalidation
Estimated CFD Stop Zone
GC Targets
Estimated CFD Targets
Basis Confidence
Executable RR
```

## 34.5 Mapping states

```text
VALID
DEGRADED
INVALID
```

Nếu mapping invalid:

```text
GC ANALYSIS VALID
CFD EXECUTION MAP INVALID
```

Hệ thống vẫn phân tích nhưng không khuyến nghị size.

---

# 35. Position Sizing và Account Risk

Thứ tự bắt buộc:

```text
Thesis
→ Structural Invalidation
→ Stop Distance
→ CFD Effective Stop Distance
→ Risk Per Lot
→ Allowed Position Size
```

Không đảo ngược.

## 35.1 Công thức khái niệm

```text
GC_StopDistance = |GC_Entry − GC_Invalidation|

CFD_EffectiveStopDistance =
GC_StopDistance
+ BasisUncertainty
+ SpreadAllowance
+ SlippageAllowance

RiskPerLot = CFD_EffectiveStopDistance × CFD_ValuePerPoint

AllowedSize = MaximumTradeRisk ÷ RiskPerLot
```

## 35.2 Trade Affordability Gate

Nếu structural stop quá xa:

```text
Giảm lot
hoặc
Bỏ trade
```

Không kéo stop vào vùng noise cho vừa tài khoản.

## 35.3 Input

```text
Account Equity
Account Currency
Maximum Risk per Trade
Maximum Daily Loss
Maximum Weekly Loss
Maximum Open Risk
CFD Value per Point
Minimum Lot Step
Spread
Slippage
Basis Allowance
```

---

# 36. Drawdown, Survival và Frequency Guards

```csharp
public enum RiskState
{
    Normal,
    Reduced,
    Recovery,
    Locked
}
```

## Normal

Risk budget chuẩn.

## Reduced

Khi:

```text
Drawdown tăng
Losing streak vượt ngưỡng
Volatility bất thường
Slippage xấu
Data degraded
```

## Recovery

```text
Chỉ setup trưởng thành hơn
Giảm risk
Giới hạn trade
Không scale in
```

## Locked

```text
Daily loss limit
Weekly loss limit
Manual lock
Critical data failure
```

Trong Locked:

```text
Không có EXECUTABLE signal
Chỉ hiển thị observation
```

## Frequency Guard

```text
Candidate limit
Executable signal limit
Cooldown after loss
Cooldown after event shock
Duplicate thesis suppression
```

---

# 37. Trade Management Engine

Trade management là quản lý thesis, không phải phản ứng cảm xúc với từng tick.

## 37.1 Theo dõi

```text
MFE
MAE
Progress speed
Barrier interaction
Orderflow continuation
Acceptance / re-entry
POC migration
Reference integrity
Target approach
Time elapsed
Spread / basis
```

## 37.2 States

```text
Healthy
SlowProgress
NoProgress
AdversePressure
ThesisWeakening
BarrierFailed
TargetApproaching
ExitWarning
Invalidated
Completed
```

## 37.3 Management Reason Ledger

Mọi hành động cần reason code:

```text
TP1_REACHED
INTERMEDIATE_BARRIER_FAILED
NEW_VALUE_FORMED
AUCTION_INVALIDATED
TIME_EXPIRY
CONTEXT_CHANGED
VOLATILITY_EXPANSION
SPREAD_ABNORMAL
BASIS_UNSTABLE
RISK_LOCK
```

Không dùng:

```text
Sợ mất lời
Thấy nến xấu
Group chat đổi view
Muốn gỡ lỗ
```

## 37.4 Scale in

Chỉ khi:

```text
Thesis không yếu đi
Evidence milestone mới xuất hiện
Tổng open risk còn hợp lệ
Average entry không phá RR
```

## 37.5 Scale out

Chỉ khi:

```text
Chạm auction objective
Barrier tiếp theo có friction cao
Thesis weakening
Risk state yêu cầu giảm exposure
```

Không mặc định 33/33/33.

## 37.6 Break-even

Không kéo BE chỉ vì lời X ticks.

Chỉ xem xét khi:

```text
Auction rời entry zone
Reference mới hình thành
Khả năng quay lại entry giảm đáng kể
```

## 37.7 Management Horizon Guard

```text
Entry thesis daily
→ không để cây 1 phút thay đổi thesis daily
```

Nhưng hard risk event vẫn có quyền override.

---

# 38. Thesis Expiry

Mỗi thesis có expiration date.

Expiry có thể dựa trên:

```text
Clock time
Bars on trigger horizon
Episode state-change count
No-progress duration
Participation regime transition
Event arrival
Reference retirement
```

Fast scalp expiry ngắn hơn confirmed thesis.

Một trade chưa chạm price stop nhưng không tạo expected behavior có thể bị:

```text
THESIS_WEAKENING
TIME_INVALIDATED
EXPIRED
```

---

# 39. Hard Veto Engine

```csharp
public enum VetoReason
{
    DataInvalid,
    RequiredCapabilityMissing,
    ContractRollUnknown,
    EventWindow,
    TimeframeMismatch,
    NoThesis,
    NoInvalidation,
    NoTargetSpace,
    ExecutableRRTooLow,
    AcceptanceAgainstThesis,
    EpisodeUnresolved,
    ReferenceExpired,
    ThesisExpired,
    CfdMappingInvalid,
    SpreadAbnormal,
    BasisUnstable,
    DailyRiskLocked,
    ContextTransition,
    ConflictingAuctionState,
    DuplicateEpisode,
    LiquidityDislocation
}
```

Hard veto không bị score override.

---

# 40. Event, Macro, External Context, OI và COT

## 40.1 Event Regime

```text
NORMAL
PRE_EVENT
EVENT_IMPULSE
EVENT_DISCOVERY
POST_EVENT_STABILIZING
```

### PRE_EVENT

```text
Giảm/khóa executable signals
Cảnh báo spread/depth withdrawal
Không tin DOM snapshot
Không dùng stop quá ngắn
```

### EVENT_IMPULSE

Không đuổi impulse đầu chỉ vì tape mạnh.

### EVENT_DISCOVERY

```text
Impulse
→ Pullback
→ Auction Episode
→ Acceptance / Re-entry
→ Value formation
→ Execution evidence
```

## 40.2 Macro Regime

Macro chỉ dùng:

```text
Context tag
Risk modifier
Event veto
Horizon expectation
```

Không tạo trigger GC intraday.

## 40.3 Correlation

Correlation phải dùng returns/changes phù hợp, rolling window rõ và không suy ra causality.

External markets chỉ là context hoặc veto.

## 40.4 Open Interest

ATAS có OI indicator không có nghĩa GC/Rithmic cung cấp OI intraday hợp lệ.

Capability states:

```text
UNAVAILABLE
UNKNOWN
EOD_ONLY
INTRADAY_PARTIAL
INTRADAY_VALIDATED
```

Production Core không phụ thuộc OI.

Nếu `EOD_ONLY`:

```text
Daily OI change
Contract roll participation
Multi-day context
```

Không dùng để trigger footprint.

Không được gọi:

```text
Fresh Long
Fresh Short
Short Covering
Long Liquidation
```

khi không có position data hợp lệ.

## 40.5 COT

COT là low-frequency positioning context:

```text
Weekly / multi-week bias tag
Extreme positioning study
Regime context
```

Không dùng làm scalp entry.

---

# 41. Market Maker, Inventory Risk và Limit Tracing

## 41.1 Market Maker context

Giữ cơ chế hợp lý:

```text
Liquidity provision
Spread capture
Inventory risk
Volatility risk
Pre-event quote withdrawal
```

Không kể chuyện “market maker luôn lái giá”.

## 41.2 Pre-event liquidity withdrawal

Research features:

```text
Depth reduction
Spread expansion
Quote persistence decline
Cancellation rate increase
Tape/depth dislocation
```

Có thể tạo:

```text
PRE_EVENT_LIQUIDITY_VETO
```

## 41.3 Limit Tracing

Limit Tracing trong khóa học được giữ là:

```text
WORKING_HYPOTHESIS
MBO_RESEARCH_REQUIRED
```

Candidate pattern:

```text
Sequential quote withdrawal
+ tracking aggressive/marketable flow
+ rapid price-level progression
+ thin opposing book
```

Không thể xác nhận bằng mắt hoặc DOM screenshot.

Module nghiên cứu:

```text
BookWithdrawalSequenceDetector
LimitChasingCandidate
LiquidityCascadeTagger
```

Cần MBO/order IDs/timestamp đủ chi tiết và kiểm tra alternative explanations.

---

# 42. Psychology được cơ khí hóa thành System Governance

## 42.1 Thesis consistency

Luật mạnh:

```text
Không mở vị thế vì lý do A
rồi đóng vì lý do B không liên quan
```

Mỗi entry/exit có reason ledger.

## 42.2 FOMO

```text
Entry trước EXECUTABLE state
```

## 42.3 Revenge trade

```text
Entry trong cooldown hoặc Risk Locked
```

## 42.4 Overtrade

```text
Vượt frequency/risk budget
```

## 42.5 Gồng lỗ

```text
Giữ sau Auction Invalidation
```

## 42.6 Chốt non

```text
Exit không dựa trên target, management state hoặc risk reason
```

## 42.7 Thesis Horizon Mutation

```text
Scalp thua
→ đổi thành swing
```

là system violation.

## 42.8 Risk Escalation

Tăng size sau chuỗi thua là violation.

## 42.9 Human state

Sleep/stress không được “đoán” từ market data. User có thể nhập manual readiness:

```text
READY
FATIGUED
STRESSED
DO_NOT_TRADE
```

Đây là user governance, không phải market signal.

---

# 43. Notification Gateway và Telegram

## 43.1 Kiến trúc

```text
State Change
→ Notification Policy
→ Deduplication
→ Priority
→ Background Queue
→ Adapter
```

Adapters:

```text
ATAS Native Alert
Telegram Direct Bot
ATAS Webhook Relay
Local Sound
Log Only
```

## 43.2 Production mặc định

```text
GCAE AddAlert()
→ ATAS Alerts
→ Telegram forwarding theo cấu hình ATAS
```

Lợi ích:

```text
Không hardcode bot token
Dùng Alerts chính thức
Một alert tồn tại cả local và Telegram
```

## 43.3 Direct Telegram Bot

Tùy chọn khi cần format/routing riêng.

Yêu cầu:

```text
HttpClient async
Không block calculation/UI thread
Token không ghi log
Chat ID cấu hình an toàn
Rate limit
Retry
Circuit breaker
```

## 43.4 Webhook Relay

```text
GCAE / ATAS
→ Webhook
→ Relay service
→ Telegram / database / other destination
```

## 43.5 Notification priorities

```text
INFO
WATCH
ACTION
RISK
CRITICAL
```

## 43.6 Events

```text
SYSTEM_STARTED
DATA_READY
DATA_DEGRADED
DATA_LOST
REFERENCE_APPROACHING
EPISODE_STARTED
ACCEPTANCE_DEVELOPING
REENTRY_DEVELOPING
EPISODE_UNRESOLVED
THESIS_CANDIDATE
THESIS_ARMED
FAST_EXECUTABLE
STANDARD_EXECUTABLE
CONFIRMED_EXECUTABLE
THESIS_WEAKENING
INVALIDATION
TP1_REACHED
TP2_REACHED
TP3_REACHED
THESIS_EXPIRED
RISK_LOCKED
RITHMIC_DISCONNECTED
RITHMIC_RECONNECTED
```

## 43.7 Deduplication

Key:

```text
Instrument
+ Contract
+ ReferenceId
+ EpisodeId
+ StateVersion
+ NotificationType
```

Không gửi lại khi:

```text
Recalculation
Reconnect replay
Giá tick qua lại cùng reference
State chưa đổi
```

## 43.8 Telegram message mẫu

```text
GC AUCTIONFLOW ENGINE

STATE: STANDARD_EXECUTABLE
THESIS: FAR LONG
CONTEXT: Multi-day Balance
REFERENCE: Composite VAL
EPISODE: Reaccepted Inside
ORDERFLOW: Sell effort elevated, downside progress inefficient
ENTRY: xxxx.x–xxxx.x GC
INVALIDATION: xxxx.x GC
TP1: Local POC
TP2: Session POC
CFD MAP: VALID
EXECUTABLE RR: 2.1R
EXPIRY: 8 trigger bars
VETO: NONE
```

## 43.9 Vận hành khi ít ngồi máy

Cần:

```text
Máy/VPS chạy ATAS
ATAS không sleep
Rithmic kết nối
GCAE trên chart đúng contract
Internet hoạt động
Notification adapter healthy
```

Tin nhắn cũ không được dùng nếu sau đó đã có `INVALIDATED` hoặc `EXPIRED`.

---

# 44. Giao diện ATAS

## 44.1 Overlay tối giản

```text
Active Reference Zone
Episode Boundary
Accepted / Reaccepted Zone
Entry Zone
Invalidation Zone
TP1 / TP2 / TP3
Current Composite Value
Local POC / Value
Retest / Pullback Zone
```

Legacy `Sweep Extreme` đổi thành:

```text
Episode Extreme
```

## 44.2 Auction GPS Card

```text
DATA: READY
CONTRACT: GC...
PARTICIPATION: COMEX ACTIVE

STRUCTURAL CONTEXT: MULTI-DAY UP DISCOVERY
TACTICAL CONTEXT: DAILY BALANCE
AUCTION STATE: EXTREME TESTING
LOCATION: COMPOSITE VAL

EPISODE: REENTRY DEVELOPING
ATTEMPTS: 2
ACCEPTANCE OUTSIDE: DEVELOPING / FAILED / ESTABLISHED
TRADE FACILITATION: SELLING INEFFICIENT

THESIS: FAR LONG CANDIDATE
MATURITY: ARMED
MISSING: MICRO RETEST

ENTRY POLICY: WAIT / LIMIT / MARKETABLE LIMIT / STOP
ENTRY ZONE: ...
INVALIDATION: ...
TP1 / TP2 / TP3: ...
CFD MAP: VALID / DEGRADED / INVALID
VETO: ...
EXPIRY: ...
```

## 44.3 Research panel

Có thể bật:

```text
Auction Potential
Tempo
Pressure evidence
POC Migration Velocity
Friction / Vacuum
DOM persistence
MBO tags
Big Trade tags
```

Mặc định tắt trong Production Mode.

---

# 45. Logging Schema

Mọi episode được log, kể cả không trade.

## 45.1 Episode record

```text
EpisodeId
Timestamp
Instrument
Contract
ParticipationRegime
ContextHorizons
AuctionState
CompositeState
ReferenceId / Type / Zone / Status
Direction
AttemptCount
OutsideDuration
OutsideDistance
OutsideVolume
OutsideDelta
OutsideTradeCount
LocalPOC
LocalValue
Resolution
```

## 45.2 Orderflow record

```text
AggressiveVolume
Delta
CVD state
Imbalance count
Stacked imbalance
Trade count
Tape speed
Big Trade percentiles
MBO Sweep tags
Stop tags
Iceberg tags
Price progress
Trade facilitation state
```

## 45.3 Thesis record

```text
ThesisId
Type FAR/AAC
Direction
Signal maturity
Context/Thesis/Trigger/Management/Target horizons
Entry Zone
Invalidation types
Targets
Intermediate barriers
Theoretical RR
Executable RR
Order type policy
Expiry
Veto reasons
```

## 45.4 Outcome record

```text
Executable?
Filled?
Entry price
MFE
MAE
Time to MFE
Time to MAE
Barrier outcomes
TP1 / TP2 / TP3
Price stop
Auction invalidation
Time expiry
Final R
CFD slippage
Spread
Basis error
```

## 45.5 Research tags

```text
NoRetest
MicroRetest
StructuralRetest
SecondAttempt
DoubleDistribution
FailedFollowThrough
BigTradePresent
TapeBurst
DomPulling
DomStacking
IcebergCandidate
StopEvent
MboSweep
LimitTracingCandidate
OneTimeFramingUp
OneTimeFramingDown
ThinParticipation
PreSettlement
SettlementTransition
PostSettlement
IbExtremeStillActive
DayStructureCandidate
SinglePrintFormationContext
AdjacentBuildObserved
FastShadowCandidate
EventDay
RollRegime
```

---

# 46. Research Harness, không chờ vài tháng

GCAE không phải AI cần “hấp thụ” vài tháng mới hoạt động.

## 46.1 Hai đường song song

```text
Mechanism knowledge
→ build usable core now

Historical + replay + live shadow
→ calibrate and improve
```

## 46.2 Historical Scanner

Quét dữ liệu lịch sử đã tải:

```text
AuctionMapEngine
ReferenceEngine
AuctionEpisodeEngine
Acceptance/Reentry Engine
Orderflow Feature Extractor
Outcome Analyzer
```

Output hàng trăm/hàng nghìn episodes thay vì chờ live.

## 46.3 Thu raw features, không chỉ tín hiệu

Không chỉ log:

```text
Signal thắng/thua
```

Phải log đủ raw features để tái chạy nhiều rule versions mà không thu lại dữ liệu.

## 46.4 Historical data phù hợp

Tương đối tốt cho:

```text
Trades
Bid/Ask
Volume
Delta
Footprint
Trade count
TPO/VP
POC/Value
Price progress
Một phần Big Trades/Tape nếu timestamp đủ
```

## 46.5 Live-only / local-history data

Cần live recorder cho:

```text
DOM changes
MBO queue lifecycle
Pulling/Stacking
Order cancellation
Một số Iceberg/Stops/Sweeps events
```

Những module này không được chặn Production Core.

## 46.6 Replay có mục tiêu

Chọn nhóm:

```text
Accepted Outside rõ
Reaccepted Inside rõ
Unresolved Rotation
False Re-entry
Event Episodes
Double Distribution healthy/failed
```

Replay kiểm tra state timing và lookahead, không phải chứng minh edge cuối cùng.

---

## 46.7 First Low-Cost Scanner Workload

Ưu tiên đầu tiên của Historical Scanner:

```text
IB width percentile
Day Structure distribution và revision count
IBExtreme end-of-day labels + next-auction revisit outcomes
Composite merge tolerance
LVN/HVN base rates
Thin-participation effect
One-Time Framing persistence/outcome
Pre-settlement episode outcome
Adjacent-build barrier outcome
```

Nhóm này chủ yếu dùng Profile, trades, Bid/Ask và timestamps, không cần chờ DOM/MBO nhiều tháng.

# 47. Validation Framework

## Stage 1: Mechanism Validation

```text
Profile calculation đúng?
Reference lifecycle đúng?
Episode không bị tách lặp?
State transition đúng?
Không lookahead?
Không repaint critical state?
```

## Stage 2: Historical Event Study

So sánh:

```text
AcceptedOutside
vs ReacceptedInside
vs Unresolved
```

## Stage 3: Entry Policy Study

```text
FAST vs STANDARD vs CONFIRMED
Limit vs Marketable Limit vs Stop
```

## Stage 4: MAE/MFE and Target Study

```text
Stop buffer
Expiry
Barrier behavior
Target hit distribution
```

## Stage 5: Walk-forward

```text
Calibration window
→ next out-of-sample window
```

## Stage 6: Live Shadow

Không trade, so:

```text
Historical/live feature differences
Alert timing
DOM/MBO availability
CFD basis
Telegram reliability
```

## Stage 7: Small-risk Production

Chỉ sau:

```text
No critical repaint
Risk mapping đúng
Logging đầy đủ
Core expectancy đủ bằng chứng
```

---

# 48. Backtest Governance

Bài học Trading System được chuyển thành luật:

```text
Idea rõ trước backtest
Trade list / event definition cố định
Không sửa rule giữa sample để cứu kết quả
Ghi mọi failure
Tách calibration và evaluation
```

Không dùng backtest để chứng minh điều mình đã muốn tin.

## 48.1 Overfitting guard

```text
Không tối ưu quá nhiều thresholds
Không chọn period chỉ vì đẹp
Không bỏ regime xấu mà không có lý do trước
Không thay definition sau khi nhìn outcome
```

## 48.2 Baselines

Mọi module mới phải so với baseline đơn giản:

```text
Core Auction State only
Core + Orderflow
Core + optional feature
```

Chỉ giữ feature nếu có incremental value.

---

# 49. Kiến trúc Solution C# và nguyên tắc một DLL

## 49.1 Source solution

```text
GC.AuctionFlow.sln
│
├── GC.AuctionFlow
│   ├── Indicator
│   ├── Core
│   ├── Data
│   ├── Profile
│   ├── Auction
│   ├── Orderflow
│   ├── Liquidity
│   ├── Thesis
│   ├── Execution
│   ├── Risk
│   ├── CfdMap
│   ├── Notifications
│   ├── Logging
│   ├── Research
│   └── UI
│
├── GC.AuctionFlow.Tests
│   ├── Unit
│   ├── StateMachine
│   ├── Replay
│   └── Regression
│
└── GC.AuctionFlow.ResearchTools
    ├── HistoricalScanner
    ├── CsvJsonReader
    ├── EpisodeAnalyzer
    └── CalibrationReports
```

## 49.2 Artifact cài ATAS

```text
GC.AuctionFlow.dll
```

Tests và ResearchTools không cài vào ATAS.

## 49.3 Namespace đề xuất

```text
GC.AuctionFlow.Indicator
GC.AuctionFlow.Core
GC.AuctionFlow.Data
GC.AuctionFlow.Profile
GC.AuctionFlow.Auction
GC.AuctionFlow.Orderflow
GC.AuctionFlow.Liquidity
GC.AuctionFlow.Thesis
GC.AuctionFlow.Execution
GC.AuctionFlow.Risk
GC.AuctionFlow.CfdMap
GC.AuctionFlow.Notifications
GC.AuctionFlow.Logging
GC.AuctionFlow.Research
GC.AuctionFlow.UI
```

## 49.4 Không viết God Class

Không có file:

```text
GCAuctionFlow.cs 30.000 dòng
```

Indicator entry point chỉ orchestration, lifecycle, settings và rendering.

---

# 50. Feature Flags và Operating Modes

```csharp
public sealed class FeatureConfiguration
{
    public bool EnableProductionAnalysis { get; set; } = true;

    public bool EnableOneTimeFramingContext { get; set; } = true;
    public bool EnableThinParticipationTagging { get; set; } = true;
    public bool EnableSettlementWindowTags { get; set; } = true;

    public bool EnableFastCandidateDetection { get; set; } = true;
    public bool EnableFastShadowLogging { get; set; } = true;
    public bool EnableFastActionAlerts { get; set; } = false;
    public bool EnableFastExecutablePlans { get; set; } = false;
    public bool EnableFastRiskSizing { get; set; } = false;

    public bool EnableAdaptiveTpoResearch { get; set; }
    public bool EnableSinglePrintFormationResearch { get; set; }
    public bool EnableDayStructureResearch { get; set; }
    public bool EnableIbExtremeRevisitResearch { get; set; }
    public bool EnableAdjacentBuildResearch { get; set; }
    public bool EnableThinSessionWeightingResearch { get; set; }
    public bool EnableAuctionGenomeResearch { get; set; }
    public bool EnableBigTradeResearch { get; set; }
    public bool EnableTapeResearch { get; set; }
    public bool EnableDomResearch { get; set; }
    public bool EnableMboResearch { get; set; }
    public bool EnableIcebergResearch { get; set; }
    public bool EnableStopsResearch { get; set; }
    public bool EnableMboSweepResearch { get; set; }
    public bool EnableLimitTracingResearch { get; set; }
    public bool EnableOpenInterestProbe { get; set; }

    public bool EnableAtasAlerts { get; set; } = true;
    public bool EnableTelegramDirect { get; set; }
    public bool EnableWebhookRelay { get; set; }
}
```

Flag `true` không vượt Capability Gate.

Modes:

```text
PRODUCTION
RESEARCH
HISTORICAL_SCAN
REPLAY_AUDIT
LIVE_SHADOW
DIAGNOSTIC
```

---

# 51. Core Domain Objects

## 51.1 Analysis Snapshot

```csharp
public sealed record AnalysisSnapshot(
    DateTimeOffset Timestamp,
    DataState DataState,
    ParticipationRegime ParticipationRegime,
    MultiHorizonContext Context,
    AuctionMapSnapshot AuctionMap,
    IReadOnlyList<StructuralReference> References,
    IReadOnlyList<AuctionEpisodeSnapshot> Episodes,
    IReadOnlyList<ThesisContract> Theses,
    RiskState RiskState,
    IReadOnlyList<VetoReason> GlobalVetos);
```

## 51.2 Thesis Candidate

```csharp
public sealed class ThesisCandidate
{
    public string ThesisId { get; init; }
    public ThesisType Type { get; init; }
    public AuctionDirection Direction { get; init; }
    public SignalMaturity Maturity { get; set; }

    public string EpisodeId { get; init; }
    public string ReferenceId { get; init; }

    public PriceZone EntryZone { get; set; }
    public ExecutionPolicy ExecutionPolicy { get; set; }
    public InvalidationPlan Invalidation { get; set; }
    public IReadOnlyList<TargetPlan> Targets { get; set; }

    public OrderflowEvidence Orderflow { get; init; }
    public AuctionResolution Resolution { get; init; }
    public TimeframePlan Timeframes { get; init; }
    public RiskPlan Risk { get; set; }
    public CfdExecutionMap CfdMap { get; set; }

    public IReadOnlyList<VetoReason> Vetos { get; set; }
    public ExpiryPlan Expiry { get; init; }
}
```

## 51.3 Feature Provenance

```csharp
public sealed record FeatureProvenance(
    string Name,
    string Source,
    EvidenceTier Tier,
    string CalculationMethod,
    string NormalizationMethod,
    IReadOnlyList<string> KnownFailureModes,
    bool HistoricalCompatible,
    bool LiveCompatible,
    FeatureStatus Status,
    string Version);
```

---

# 52. Orchestration Pseudocode

```csharp
if (!dataIntegrity.CanAnalyzeCore)
{
    PublishDataStatus();
    return;
}

var context = multiHorizonEngine.BuildContext();
var auctionMap = profileEngine.BuildAuctionMap(context);
var directional = directionalAuctionEngine.Classify(auctionMap);
var references = referenceEngine.Update(auctionMap, directional);

foreach (var reference in references.Active)
{
    var episode = episodeEngine.Update(reference, marketEvents);

    if (episode.State is EpisodeState.Idle)
        continue;

    var acceptance = acceptanceEngine.Evaluate(episode, auctionMap);
    var reentry = reentryEngine.Evaluate(episode, auctionMap);
    var resolution = resolutionEngine.Resolve(episode, acceptance, reentry);

    var orderflow = orderflowEngine.Evaluate(episode);
    var facilitation = tradeFacilitationEngine.Compare(
        orderflow.Effort,
        orderflow.Result,
        directional,
        participationRegime);

    var thesisCandidates = thesisEngine.Build(
        context,
        reference,
        episode,
        resolution,
        facilitation);

    foreach (var thesis in thesisCandidates)
    {
        thesis.ExecutionPolicy = executionPolicyEngine.Select(thesis);
        thesis.Invalidation = invalidationEngine.Build(thesis);
        thesis.Targets = targetEngine.Build(thesis, auctionMap);
        thesis.CfdMap = cfdMapEngine.Build(thesis);
        thesis.Risk = riskEngine.Build(thesis);
        thesis.Vetos = vetoEngine.Evaluate(thesis);

        thesisStateMachine.Advance(thesis);
        analysisPublisher.Publish(thesis);
        notificationGateway.PublishStateChanges(thesis);
        logger.Write(thesis);
    }
}
```

Không có `Buy()` hoặc `Sell()` trong baseline.

---

# 53. Signal Scenario Matrix

Có hai thesis families, hai directions và ba maturity modes:

```text
FAR Long
FAR Short
AAC Long
AAC Short
```

mỗi loại có:

```text
FAST
STANDARD
CONFIRMED
```

Tổng cộng 12 signal variants, nhưng chỉ hai setup engines.

## 53.1 FAR Long Fast

```text
High-quality lower reference
Episode below
No established acceptance below
Fast stable re-entry
Sell facilitation failure
Micro pullback hold
Executable corridor
```

## 53.2 FAR Long Standard

```text
Reaccepted inside
Structural retest
Sell aggression ineffective on retest
Micro trigger
```

## 53.3 FAR Long Confirmed

```text
Reaccepted inside
Retest / second attempt
Value maintenance inside
Opposite flow confirmation
```

## 53.4 AAC Long Fast

```text
Acceptance developing strongly
POC/value migration
Old-value reclaim fails quickly
Micro continuation trigger
```

## 53.5 AAC Long Standard

```text
Accepted outside
First pullback holds
Counter-selling ineffective
```

## 53.6 AAC Long Confirmed

```text
Established outside value
Mature pullback
Follow-through milestone
```

Short variants đối xứng.

---

# 54. Ví dụ phân tích hoàn chỉnh: FAR Long

```text
DATA
READY

PARTICIPATION
LONDON ACTIVE

STRUCTURAL CONTEXT
MULTI-DAY BALANCE

TACTICAL CONTEXT
DOWN ROTATION TO COMPOSITE VAL

REFERENCE
COMPOSITE VAL + PRIOR DAY LOW
STATUS: ACTIVE

AUCTION EPISODE
ATTEMPTS: 2
MAX OUTSIDE DISTANCE: 7 TICKS
OUTSIDE DURATION: 82 SECONDS
LOCAL POC: DID NOT ESTABLISH BELOW
RESOLUTION: REACCEPTED INSIDE

ORDERFLOW
SELL AGGRESSION: 93RD REGIME PERCENTILE
DOWNSIDE PROGRESS: 19TH PERCENTILE
TRADE FACILITATION: SELLING INEFFICIENT
MBO SWEEP: PRESENT, RESEARCH TAG ONLY

THESIS
FAR LONG
MATURITY: STANDARD EXECUTABLE

ENTRY POLICY
MARKETABLE LIMIT ON MICRO RECLAIM
ENTRY ZONE: xxxx.x–xxxx.x

INVALIDATION
PRICE: BELOW EPISODE EXTREME + BUFFER
AUCTION: LOCAL VALUE ESTABLISHED BELOW VAL
TIME: NO UPSIDE PROGRESS WITHIN CALIBRATED WINDOW

TARGET CORRIDOR
TP1: LOCAL POC
BARRIER 1: VWAP
TP2: SESSION POC
TP3: COMPOSITE POC

CFD MAP
VALID
EXECUTABLE RR: 2.2R

VETO
NONE
```

---

# 55. Ví dụ phân tích hoàn chỉnh: AAC Short

```text
DATA
READY

STRUCTURAL CONTEXT
MULTI-DAY DOWN DISCOVERY

REFERENCE
COMPOSITE VAL

EPISODE
OUTSIDE ATTEMPT BELOW
OUTSIDE TIME AND VOLUME INCREASING
LOCAL POC MIGRATING LOWER
OLD VALUE RECLAIM FAILED
RESOLUTION: ACCEPTED OUTSIDE

ORDERFLOW
SELLING EFFECTIVE
PULLBACK BUYING INEFFICIENT

THESIS
AAC SHORT
MATURITY: STANDARD EXECUTABLE

ENTRY POLICY
STOP-MARKET BELOW MICRO PULLBACK LOW

INVALIDATION
REACCEPTANCE INTO OLD VALUE

TARGETS
TP1: NEW LOCAL POC EXTENSION
TP2: NEXT HVN
TP3: WEEKLY REFERENCE
```

---

# 56. Ví dụ No Trade: Unresolved Rotation

```text
REFERENCE
PRIOR DAY LOW

EPISODE
ATTEMPT 1 BELOW → BACK INSIDE
ATTEMPT 2 BELOW → LOCAL POC LOWER
ATTEMPT 3 INSIDE → NO MAINTENANCE

ACCEPTANCE
CONFLICTED

ORDERFLOW
SELLING AND BUYING BOTH CREATE LIMITED PROGRESS

RESOLUTION
UNRESOLVED ROTATION

OUTPUT
NO TRADE

REASON
REFERENCE LOST DISCRIMINATING POWER
EPISODE NOT RESOLVED
```

---

# 57. Workspace ATAS tối thiểu

## Context chart

```text
Volume Profile & TPO
Composite references
VWAP
Prior day/week references
GCAE overlay
```

## Execution chart

```text
Footprint Bid × Ask
Cluster Statistic
GCAE episode/entry zones
```

## Auxiliary windows khi cần

```text
Smart Tape
MBO DOM
DOM Heatmap
```

Không bật 283 indicators cùng lúc.

Audit từng công cụ theo thí nghiệm riêng.

---

# 58. User Settings đề xuất

## General

```text
Instrument
Contract mode
Timezone
Production / Research mode
```

## Profiles

```text
Classic TPO period
Value Area method
Composite mode
Profile anchors
```

## Auction Episodes

```text
Approach distance
Episode reset policy
Maximum episode duration
Reference lifecycle settings
```

## Signal Maturity

```text
Enable FAST
Enable STANDARD
Enable CONFIRMED
```

## Risk & CFD

```text
Account equity
Risk limits
CFD value per point
Spread
Slippage
Basis source
```

## Notifications

```text
ATAS alerts
Telegram routing
Event subscriptions
Cooldown
Quiet hours
```

## Research

```text
Adaptive TPO
Auction Genome
Tape
DOM
MBO
Iceberg
Stops
Sweeps
OI Probe
```

---

# 59. Unified Research Ledger và Source Provenance

## 59.1 Legacy H-Registry, được bảo toàn làm aliases

```text
H1: Reaccepted-inside episodes có expectancy khác accepted-outside rõ ràng không?
H2: Episode-level aggregation tốt hơn candle-pattern detection bao nhiêu?
H3: Effort vs Result có bổ sung edge ngoài Delta divergence không?
H4: FAST, STANDARD, CONFIRMED khác nhau thế nào về fill, MAE, MFE và expectancy?
H5: First structural retest tốt hơn later retests theo regime nào?
H6: No-retest micro confirmation có edge ở context nào?
H7: POC migration velocity bổ sung gì ngoài POC displacement?
H8: TPO–VP divergence dự báo transition hay chỉ mô tả transition?
H9: Double Distribution follow-through phụ thuộc compression và second-value quality thế nào?
H10: Failed Double Distribution có tạo FAR tốt hơn baseline không?
H11: Smart Tape burst efficiency bổ sung gì ngoài trade count/volume?
H12: Big Trades percentile có incremental value không?
H13: DOM persistence cải thiện execution policy hay direction?
H14: MBO reload behavior cải thiện absorption classification không?
H15: MBO Sweep/Stops/Iceberg chỉ là tags hay thực sự tăng expectancy?
H16: Auction Potential Index dự báo expansion tốt hơn ATR/compression baseline không?
H17: Friction/Vacuum indexes cải thiện target timing không?
H18: Adaptive TPO tốt hơn Classic 30m TPO ở regime nào?
H19: CFD spread/basis làm giảm theoretical expectancy bao nhiêu?
H20: Pre-event liquidity withdrawal có cải thiện veto timing không?
H21: Limit Tracing Candidate có thể phân biệt khỏi ordinary book thinning không?
H22: One-Time Framing persistence có incremental value ngoài value/POC migration không?
H23: Single Print formation context có phân biệt fill/acceptance outcome không?
H24: IB extreme là day extreme có ảnh hưởng next-auction revisit/reaction trên GC không?
H25: Thin participation làm sai lệch composite contribution bao nhiêu?
H26: Adjacent Build Ratio có cải thiện barrier permeability estimate không?
H27: Pre-settlement episodes khác baseline về reversal/continuation outcome không?
H28: Day Structure classification/revision có cải thiện expectation, target và expiry không?
H29: FAST shadow candidates có phân phối MAE/MFE đủ tốt để mở production không?
```

## 59.2 Canonical Research Ledger ID

Từ v1.2, mọi hypothesis/card được hợp nhất vào:

```text
RL-0001
RL-0002
RL-0003
...
```

Không xóa hoặc tái sử dụng ID cũ. `H2`, `RC-05` và các mã cũ được giữ trong `LegacyAliases`.

Schema:

```text
LedgerId
Title
CanonicalQuestion
SourceTags
LegacyAliases
0DStatus
EvidenceTier
RequiredCapabilities
MeasurementMethod
Instrument
Horizon
NullHypothesis
ExpectedDirection
FeatureDefinition
OutcomeDefinition
ScannerEligibility
ReplayRequired
LiveShadowRequired
Priority
Dependencies
DatasetVersion
CodeVersion
Owner
CurrentStage
PromotionCriteria
RejectionCriteria
DecisionHistory
```

Research stages:

```text
PROPOSED
DEFINED
DATA_READY
SCANNED
REPLAY_VALIDATED
LIVE_SHADOW
PILOT
PROMOTED
REJECTED
DORMANT
```

## 59.3 Source Provenance Map

Không dùng nhãn mơ hồ `Bài 3` nếu không có corpus ID.

Canonical source IDs:

```text
COURSE-K2-B01 ... COURSE-K2-B08
COURSE-K3-B01 ... COURSE-K3-B09
DISTILL-K2-B03-v1
CHAT-GCAE-2026-07-21-OTF
IMG-AMT-001
SPEC-v1.2-SEC29
REVIEW-CROSS-001
```

Source record:

```text
SourceId
CanonicalTitle
CoursePeriod
LessonNumber
Speaker
Date if known
FileRef
TranscriptQuality
DistillationVersion
RelatedSpecSections
Notes
```

Source tags:

```text
[GV]   Giảng viên / course statement
[NC]   Nhánh chưng cất
[SPEC] GCAE architecture inference
[IMG]  External educational image
[DATA] Empirical observation
```

## 59.4 Initial Research Ledger Workload

```text
IB width percentile
Day Structure distribution
IBExtreme revisit/reaction
Composite merge tolerance
LVN/HVN base rates
Thin-participation effect
One-Time Framing persistence
Pre-settlement episode outcome
```


---

# 60. Các quyết định đã khóa

```text
GCAE dùng để phân tích, không auto trade.
Một DLL, một indicator.
GC/Rithmic là nguồn phân tích, CFD là nơi thực thi.
AMT Context trước Orderflow.
Auction Episode thay cho SweepDetector.
Reference crossing không phải sweep.
FAR và AAC là hai thesis families lõi.
MBO Sweep chỉ là microstructure tag.
Acceptance là quá trình Time + Volume + Orderflow + Maintenance.
Directional context đa horizon thay cho trend filter đơn giản.
Entry type không cố định Limit hay Market.
Invalidation trước position size.
Target đến từ Auction corridor, không từ R tùy ý.
Hard Veto không bị score override.
OI không thuộc intraday core nếu capability chưa xác nhận.
ATAS Ultra modules phải capability-gated và provenance-aware.
Telegram chỉ gửi state transitions có ý nghĩa.
Historical Scanner được xây trước để không chờ dữ liệu nhiều tháng.
Raw features phải được log, không chỉ signals.
One-Time Framing là descriptive context, không tự phát signal.
Primary Intraday TPO mặc định 08:20 America/New_York và configurable.
Thin participation detection/tagging là Core; weighting/exclusion là Research.
Settlement proximity chỉ là logging/regime tag.
Day Structure được phục hồi ở Research-only.
FAST mặc định Shadow-only.
```

---

# 61. Những điều chưa khóa

```text
Acceptance thresholds
Episode reset thresholds
Composite merge tolerances
FAST eligibility thresholds
Retest depth / duration
POC migration velocity thresholds
Auction Potential formula
Tempo classification thresholds
Pressure index design
Friction/Vacuum formulas
Minimum executable RR
Adaptive stop buffer
Expiry windows
Big Trade percentiles
DOM persistence windows
MBO feature definitions
Limit Tracing classification
Adaptive TPO production eligibility
One-Time Framing confirmation policy theo horizon
Thin participation thresholds và weighting policy
Day Structure classification thresholds
IBExtreme revisit outcome windows
Adjacent Build band width và normalization
Settlement window offsets
FAST production unlock criteria
Single Print corresponding-activity classification
```

Các con số phải sinh ra từ dữ liệu GC và walk-forward.

---

# 62. Các khái niệm bị retire hoặc superseded

## Retired

```text
SweepDetector as price crossing detector
Sweep-Reclaim as core engine name
Retest hard gate cho mọi production signal
OI Positioning Engine intraday mặc định
One-score signal decision
```

## Superseded by

```text
AuctionEpisodeEngine
FailedAuctionReentryEngine
Signal Maturity FAST/STANDARD/CONFIRMED
Optional OI Context Adapter
Evidence Vector + State + Hard Veto
```

## Legacy aliases được phép

```text
Sweep-Reclaim
Break-Accept-Retest
```

chỉ để người dùng nhận diện, nhưng log/core phải lưu tên FAR/AAC.

---

# 63. Definition of Done cho Final Baseline Implementation

## Core

```text
[ ] Một DLL duy nhất được ATAS nhận diện
[ ] Load/unload không rò tài nguyên
[ ] Capability Matrix chính xác
[ ] Core chạy khi toàn bộ Research Lab tắt
[ ] Reference Lifecycle có tests
[ ] Auction Episode không báo lặp khi qua lại reference
[ ] Acceptance/Re-entry state machines có tests
[ ] FAR/AAC state machines có tests
[ ] FAST/STANDARD/CONFIRMED có definition rõ
[ ] Hard veto không bị index override
[ ] Multi-horizon alignment hoạt động
[ ] One-Time Framing chỉ dùng completed TPO periods
[ ] Primary Intraday TPO anchor timezone-aware ở 08:20 ET mặc định
[ ] FAST Action/Executable/Risk flags tắt mặc định
```

## Data

```text
[ ] Historical loading không tạo lookahead
[ ] Recalculation không phát notification trùng
[ ] Contract roll được xử lý
[ ] DOM/MBO modules tự tắt khi capability thiếu
[ ] OI module tự tắt khi dữ liệu không hợp lệ
```

## Execution & Risk

```text
[ ] Entry Policy trả order type recommendation
[ ] Structural/Auction/Time/Context invalidation đầy đủ
[ ] CFD mapping VALID/DEGRADED/INVALID
[ ] Position size tính sau invalidation
[ ] Risk Lock hoạt động
```

## Notifications

```text
[ ] ATAS AddAlert hoạt động
[ ] Telegram native forwarding kiểm tra
[ ] Direct adapter chạy nền nếu bật
[ ] Deduplication theo Episode/StateVersion
[ ] Disconnect/reconnect health alerts
```

## Research

```text
[ ] Historical Scanner xuất episode dataset
[ ] Logger ghi MFE/MAE/outcome
[ ] Research indexes có provenance
[ ] Unified Research Ledger bảo toàn legacy aliases
[ ] Source Provenance Map phân biệt đúng corpus/bài học
[ ] Day Structure chạy Research-only
[ ] Thin-session weighting/exclusion tắt mặc định
[ ] Walk-forward report tái tạo được
[ ] Live Shadow chạy ổn định
```

---

# 64. Lộ trình triển khai

## Phase 0: Capability & Recorder

```text
DataCapabilityProbe
Raw Event Recorder
Contract/Roll
Logging foundation
```

## Phase 1: Auction Core

```text
Classic TPO / VP
Composite
Reference Lifecycle
Directional Auction Context
One-Time Framing Tracker
Thin Participation and Settlement Tags
Auction Episode
Acceptance / Re-entry Resolution
```

## Phase 2: Executed Orderflow

```text
Bid/Ask
Delta/CVD
Cluster features
Effort vs Result
Trade Facilitation
```

## Phase 3: Thesis & Planning

```text
FAR/AAC
Signal Maturity
Entry Policy
Invalidation
PLAR Targets
CFD Mapping
Risk
```

## Phase 4: UI & Telegram

```text
Auction GPS Card
Overlay
Alerts
Deduplication
Health monitoring
```

## Phase 5: Historical Scanner & Calibration

```text
Episode dataset
MAE/MFE
Day Structure / IBExtreme studies
Thin participation / OTF / settlement studies
Adjacent Build study
Walk-forward
Threshold calibration
```

## Phase 6: ATAS Ultra Microstructure

```text
Smart Tape
Big Trades
DOM/MBO
Pulling/Stacking
Iceberg/Stops/Sweeps
```

## Phase 7: Advanced Research

```text
Adaptive TPO
Auction Potential
Tempo/Pressure/Quality
Genome/GPS indexes
Limit Tracing Candidate
External context
```

---

# 65. Glossary

```text
AMT: Auction Market Theory
AAC: Accepted Auction Continuation
FAR: Failed Auction Re-entry
Episode: Toàn bộ vòng đời tương tác giữa giá và một reference
Reference Excursion: Giá giao dịch ngoài reference, không hàm ý sweep
MBO Sweep: Aggressive liquidity removal qua nhiều levels
Acceptance: Duy trì và xây activity/value ngoài vùng cũ
Re-entry: Giá quay vào vùng cũ
Reacceptance: Re-entry được duy trì và xây activity bên trong
Trade Facilitation: Market làm tốt đến đâu theo hướng Auction đang thử
Liquidity Agreement Zone: Vùng có time/volume agreement, không phải fair value tuyệt đối
PLAR: Path of Least Auction Resistance
Signal Maturity: FAST / STANDARD / CONFIRMED
Thesis Contract: Hợp đồng định nghĩa expected behavior, invalidation, target và expiry
OTF: One-Time Framing trên completed TPO periods
Day Structure: Classification động của cấu trúc ngày, không phải prediction
Thin Participation: Participation quality thấp theo regime-normalized features
Settlement Tag: Nhãn proximity quanh settlement anchor, không phải signal
Research Ledger: Sổ duy nhất quản trị hypothesis, provenance và promotion
```

---

# 66. Kết luận chính thức

GC AuctionFlow Engine là:

```text
Một hệ thống quản lý và giám sát giả thuyết phân tích GC,
được xây trên Auction Market Theory,
tổ chức bằng Multi-Horizon Auction Map,
theo dõi toàn bộ Auction Episode thay vì mô hình nến,
đánh giá bằng executed orderflow và Trade Facilitation,
mở rộng bằng ATAS Ultra microstructure,
bảo vệ bằng Hard Veto và multi-dimensional invalidation,
dịch sang CFD bằng execution mapping,
gửi trạng thái qua ATAS/Telegram,
và tự cải thiện bằng historical scanning, logging và walk-forward.
```

Công thức cô đọng:

```text
WHERE?
→ Auction Map, Value, Reference

WHAT STATE?
→ Balance, Discovery, Transition

WHAT IS BEING ATTEMPTED?
→ Auction Episode

IS IT BEING ACCEPTED?
→ Time + Volume + Orderflow + Maintenance

WHO IS PRESSING AND DID IT WORK?
→ Effort vs Result / Trade Facilitation

WHAT THESIS IS VALID?
→ FAR / AAC / No Trade

HOW MATURE IS IT?
→ Fast / Standard / Confirmed

HOW SHOULD IT BE ENTERED?
→ Limit / Marketable Limit / Stop / Market / Wait

WHERE IS IT WRONG?
→ Price / Auction / Time / Context Invalidation

WHERE CAN AUCTION GO?
→ PLAR + Intermediate Barriers + Targets

CAN CFD EXECUTE IT SAFELY?
→ Basis + Spread + Slippage + Position Size

IS THE THESIS STILL ALIVE?
→ State Machine + Expiry + Management Ledger
```

> **Tình trạng kiểm toán:** Các cụm trao đổi của anh Fen được ánh xạ vào 0A; thay đổi so với v0.2/v1.1 được ghi ở 0B; Audit Patch 01 nằm ở 0A.16 và 0E; mọi ý tưởng chưa đủ production được giữ trong Unified Research Ledger thay vì bị bỏ quên.**

> **GCAE không cố cho trader đôi mắt nhìn thấy tương lai. Nó xây một hệ thống thị giác có bản đồ, kính hiển vi, thước đo, hộp đen ghi dữ liệu và chuông cảnh báo, để trader ít nhầm một cú chọc level thành “sweep”, ít nhầm volume lớn thành “absorption”, và ít giữ một câu chuyện sau khi thị trường đã chứng minh câu chuyện đó sai.**

---

# Phụ lục A. Idea Register, đảm bảo không bỏ sót các ý tưởng đã trao đổi

```text
[CORE] AMT là framework phân tích, không phải strategy hoàn chỉnh.
[CORE] Context → Location → Episode → Orderflow → Resolution → Execution.
[CORE] GC là continuous auction; sessions là participation regimes.
[CORE] Multi-horizon directional context.
[CORE] Time/Volume/Orderflow Acceptance.
[CORE] Composite auction và POC/Value migration.
[CORE] Reference lifecycle.
[CORE] Auction Episode thay SweepDetector.
[CORE] FAR / AAC.
[CORE] Effort vs Result / Trade Facilitation.
[CORE] Thesis Contract.
[CORE] Multi-dimensional invalidation.
[CORE] PLAR / intermediate barriers.
[CORE] CFD mapping.
[CORE] Hard Veto.
[CORE] Logging và Historical Scanner.
[CORE] One-Time Framing descriptive context.
[PROFILE] Primary Intraday TPO anchor 08:20 America/New_York, configurable.
[CORE] Thin Participation detection/tagging.
[CORE] Pre-Settlement / Settlement / Post-Settlement logging tags.
[RESEARCH] Single Print Formation Context và corresponding activity.
[RESEARCH] IB Extreme end-of-day labels và next-auction revisit study.
[RESEARCH] Thin-session profile weighting/exclusion.
[RESEARCH] Adjacent Build Ratio quanh HVN/intermediate barriers.
[RESEARCH] Day Structure Classifier, restored from v0.2.
[SCALP] FAST shadow-only mặc định; action/executable/risk flags tắt.
[GOVERNANCE] Unified Research Ledger với legacy aliases.
[GOVERNANCE] Source Provenance Map với canonical source IDs.

[SCALP] Fast / Standard / Confirmed.
[SCALP] Retest không tuyệt đối.
[SCALP] Entry Zone thay single price.
[SCALP] Limit / Market / Hybrid theo context.
[SCALP] Expiry theo tempo/horizon.

[PROFILE] Classic 30m TPO.
[RESEARCH] Adaptive TPO.
[PROFILE] TPOC/VPOC/VWAP/Composite POC alignment.
[PROFILE] POC Migration Velocity.
[PROFILE] POC Strength.
[PROFILE] nPOC lifecycle.
[PROFILE] HVN Friction.
[PROFILE] LVN Vacuum.
[PROFILE] Double Distribution follow-through/failure.

[ATAS] Cluster Search.
[ATAS] Cluster Statistic.
[ATAS] Smart Tape.
[ATAS] Speed of Tape.
[ATAS] Big Trades / Adaptive Big Trades.
[ATAS] Smart DOM / DOM Heatmap / DOM Levels.
[ATAS] MBO DOM.
[ATAS] Pulling / Stacking.
[ATAS] Iceberg.
[ATAS] Stops Tracker.
[ATAS] Sweeps Tracker.
[ATAS] Market Power / DOM Power / DOM Strength audit.
[ATAS] Dynamic Levels provenance.
[ATAS] Volume Statistic normalization.

[RESEARCH] Auction Potential / Energy.
[RESEARCH] Auction Tempo.
[RESEARCH] Auction Pressure.
[RESEARCH] Auction Quality / Breakout Quality.
[RESEARCH] Auction Genome.
[RESEARCH] Auction GPS.
[RESEARCH] Limit Tracing Candidate.
[RESEARCH] Displayed vs Executed Liquidity.

[RISK] Position size after invalidation.
[RISK] Trade affordability.
[RISK] Drawdown state.
[RISK] Frequency/cooldown.
[RISK] Protective hard stop.

[PSYCHOLOGY] Thesis consistency.
[PSYCHOLOGY] FOMO as premature state transition.
[PSYCHOLOGY] Revenge as cooldown violation.
[PSYCHOLOGY] Overtrade as risk/frequency violation.
[PSYCHOLOGY] Thesis horizon mutation.

[NOTIFICATION] One DLL.
[NOTIFICATION] ATAS AddAlert → Telegram preferred.
[NOTIFICATION] Direct Bot/Webhook optional.
[NOTIFICATION] Deduplication by Episode and StateVersion.

[EXTERNAL] OI capability-gated.
[EXTERNAL] COT low-frequency context.
[EXTERNAL] Macro/correlation as context/veto only.
```



## Phụ lục A.1 Conversation-Audit Tags

```text
[AUDITED] Chapter II reread baseline preserved.
[AUDITED] AMT is analysis framework, not signal generator.
[AUDITED] Signal strictness corrected with maturity modes.
[AUDITED] Sweep crossing logic retired.
[AUDITED] Auction Episode and repeat-attempt handling included.
[AUDITED] Historical Scanner avoids passive multi-month waiting.
[AUDITED] Directional Auction Context included.
[AUDITED] Classic and Adaptive TPO policy included.
[AUDITED] TikTok-image ideas retained with corrections and provenance.
[AUDITED] ATAS Ultra inventory mapped to modules and evidence tiers.
[AUDITED] Chapter III lessons 1–9 integrated.
[AUDITED] Entry order type remains context-dependent.
[AUDITED] OI capability gate, one-DLL architecture and Telegram included.
[AUDITED] Cross-review Audit Patch 01 entries 1–9 integrated.
[AUDITED] Day-Type omission confirmed accidental and restored as Research-only.
[AUDITED] FAST deployment guardrail defaults to Shadow-only.
[AUDITED] Unified Research Ledger and Source Provenance Map included.
```

# Phụ lục B. Implementation Notes đã xác minh ở thời điểm khóa baseline

```text
ATAS custom indicator SDK hỗ trợ AddAlert overloads.
ATAS Alerts có cơ chế cấu hình gửi Telegram.
ATAS có Webhooks để gửi signal ra dịch vụ ngoài.
MBO DOM của ATAS dùng dữ liệu order-level và hiện phụ thuộc connector capability.
MBO Icebergs/Stops/Sweeps có yêu cầu dữ liệu riêng và không phải standalone signals.
```

Các chi tiết API phải được kiểm tra lại trên tài liệu ATAS chính thức tại thời điểm code, vì SDK và nền tảng có thể thay đổi.

# Phụ lục C. Audit Patch 01 Decision Log

```text
Patch ID: GCAE_SPEC_v1.1_AUDIT_PATCH_01
Review ID: REVIEW-CROSS-001
Baseline affected: v1.1
Integrated into: v1.2

Accepted entries: 1–9
Day-Type omission: accidental
Implementation v0.1 scope: unchanged
FAST default: shadow-only
Research governance: Unified Research Ledger
Source governance: Source Provenance Map
```

## Phụ lục C.1 Status Matrix

| Entry | Status |
|---|---|
| OneTimeFramingTracker | PRODUCTION_CORE, descriptive only |
| SinglePrintFormationContext | RESEARCH_ONLY + CAPABILITY_GATED |
| TPO anchor 08:20 ET | PRODUCTION_CORE configurable default |
| IBExtreme revisit study | RESEARCH_ONLY, high priority |
| ThinParticipationClassifier | CORE detection; Research weighting |
| AdjacentBuildRatio | RESEARCH_ONLY |
| Settlement proximity tags | PRODUCTION_CORE logging tag |
| DayStructureClassifier | RESEARCH_ONLY |
| FAST guardrail | SHADOW_ONLY by default |


---

# PHỤ LỤC C — INTEGRITY CHECK

File này hợp lệ khi:

- Có đúng một heading `# GCAE IMPLEMENTATION BIBLE`.
- Có Phụ lục A và B.
- Hash nguồn nhúng khớp file tại thời điểm tạo hoặc được cập nhật có Decision Log.
- Current phase trong front matter khớp Phần 1 và Phụ lục A.
- Không có phase được ghi `LOCKED` trong Phần 1 nếu Phụ lục A chưa closeout.
- Không có state `Executable` được cấp quyền khi module vẫn `NOT CALIBRATED`.
- Không có MBO runtime semantics khi capability vẫn `BLOCKED`.
- Không có auto-trading call trong baseline hiện tại.

## Lệnh kiểm tra gợi ý

```bash
python tools/check_implementation_bible.py GCAE_IMPLEMENTATION_BIBLE_SINGLE_SOURCE_FINAL.md
```

Checker nên kiểm:

```text
front matter fields
required section IDs
current phase consistency
phase status contradictions
forbidden production claims
embedded source hashes
duplicate top-level headings
unclosed code fences
UTF-8 validity
```

---

# END OF SINGLE SOURCE OF TRUTH
