# GC AuctionFlow Engine — v1.3

## Đặc tả tri thức chuẩn tắc (Knowledge-Grounded Normative Spec)

> **Tài liệu này BỔ SUNG, KHÔNG THAY THẾ** `GC_AuctionFlow_Engine_v1.2_Post_Cross_Review_Final_Spec_VI.md`.
>
> v1.2 sở hữu **kiến trúc, roadmap, hợp đồng module, governance**.
> v1.3 sở hữu **ngữ nghĩa khái niệm, tiêu chí phân biệt, hợp đồng đo lường, hợp đồng hiệu chỉnh, guard chống suy luận sai**.
>
> Nguồn tri thức nền: **KIM ĐẤU KINH — Phương pháp Tam Trụ (AMT + Order Flow + GEX)**, ký hiệu `KDK`.

---

# 0. Vị thế tài liệu, quy tắc ưu tiên và phạm vi

## 0.1 Vì sao cần v1.3

v1.2 là bản đặc tả **kỹ thuật đầy đủ** nhưng **mỏng về tri thức miền**. Cụ thể, v1.2 liệt kê tên của các vector, enum và state nhưng thường không định nghĩa:

- **Điều gì phân biệt** khái niệm A với khái niệm B gần giống (ví dụ `Rejection` vs `Failed Auction`, `Re-entry` vs `Reacceptance`, `Absorption` vs `Exhaustion`).
- **Đo bằng gì, mẫu số là gì**, và trường nào được phép `null` thay vì `0`.
- **Điều kiện nào là bắt buộc** để một state được phép phát ra, và điều gì tuyệt đối không được kết luận.
- **Vì sao** một quy tắc tồn tại — nên khi implement dễ suy diễn sai thành một công thức tiện tay.

KDK có đủ chiều sâu đó. v1.3 chuyển tri thức KDK thành **hợp đồng chuẩn tắc có thể kiểm thử** cho `GC.AuctionFlow.dll`.

## 0.2 Quy tắc ưu tiên khi xung đột

| Chủ đề | Nguồn quyết định |
|---|---|
| Kiến trúc, một-DLL, namespace, phase roadmap, governance, feature flag | **v1.2** |
| Ngữ nghĩa khái niệm đấu giá / order flow | **v1.3 (nguồn KDK)** |
| Tiêu chí phân biệt hai khái niệm gần nhau | **v1.3** |
| Trường nào được phép suy luận, trường nào cấm suy luận | **v1.3** |
| Ngưỡng số cụ thể | **KHÔNG tài liệu nào** — xem §14 Calibration Ledger |
| Tên enum / tên policy version đã build | **Mã nguồn hiện hữu** (v1.3 phải ánh xạ, không đổi tên) |

**Bất biến tuyệt đối:** v1.3 **không được** mở khóa bất kỳ trạng thái nào đang bị `NOT_CALIBRATED`. v1.3 chỉ mô tả **điều kiện cần để một ngày nào đó được phép hiệu chỉnh**.

## 0.3 Phạm vi loại trừ ở v1.3

| Nội dung KDK | Trạng thái trong v1.3 |
|---|---|
| **PHẦN V — Bản đồ GEX theo ngày** (Ch 44–50) | **OUT OF SCOPE** — theo chỉ định của operator. Không tạo module, không tạo enum, không tạo trường snapshot, không tạo GPS row. |
| GEX Flip / Call Resistance / Put Support / HVL | **OUT OF SCOPE** |
| DEX, Options Flow, Gamma | **OUT OF SCOPE** |
| Ch 76 (ghim giá quanh GEX), Ch 77 (khuếch đại GEX âm), Ch 79 (AMT vs GEX xung đột) | **OUT OF SCOPE** — không triển khai họ chiến lược này |
| Mọi field `GexContext` trong thesis contract | **RESERVED = null**, không tính vào bất kỳ kết luận nào |

> Khi một chương KDK nói "vai trò của GEX", v1.3 diễn giải là: **trường bối cảnh tùy chọn, hiện đang không khả dụng**. Mọi logic phải hoạt động đầy đủ khi GEX vắng mặt. KDK Ch 51 đã quy định rõ: khi thiếu GEX, AMT + Order Flow vẫn đủ để ra kết luận; GEX không bao giờ là điều kiện cần.

## 0.4 Ký hiệu chuẩn tắc

| Ký hiệu | Nghĩa |
|---|---|
| `[N]` | **Normative** — bắt buộc implement đúng như mô tả |
| `[M]` | **Measurable** — phải trở thành trường số/enum trong snapshot, không được là văn bản mô tả |
| `[C]` | **Calibration-gated** — chỉ được phát ra sau khi vượt cổng hiệu chỉnh §14 |
| `[G]` | **Guard** — bất biến phải có test tự động chứng minh |
| `[*]` | **Heuristic** — quy tắc kinh nghiệm AMT, không phải định luật; không được tự sinh tín hiệu |
| `[R]` | **Research-only** — chỉ ghi log, không vào Production Core |

---

# 1. Bản đồ truy vết: KDK → v1.2 → Module đã build

| KDK | v1.2 | Namespace / Phase | Trạng thái |
|---|---|---|---|
| Ch 1–5 Nền tảng tư duy | §2 Hiến pháp, §8 Phân cấp bằng chứng | `Core` | LOCKED |
| Ch 7 Market Profile / TPO | §13, §14 | `Profile` (Phase 1A) | LOCKED |
| Ch 8 Volume Profile | §13, §16 | `Profile` (Phase 1A) | LOCKED |
| Ch 9 Value Area & POC | §16 | `Profile` | LOCKED |
| Ch 10 Initial Balance | §18.4 | `Profile` | Một phần |
| Ch 11 Hình dạng Profile / cấu trúc ngày | §18.4 Day Structure | — | **NOT STARTED** |
| Ch 12 Hệ thống mốc tham chiếu | §17 Structural Reference | `Reference` (Phase 1C) | LOCKED |
| Ch 13 Composite Profile | §15 | `Composite` (Phase 1B) | LOCKED |
| Ch 14 Dịch chuyển giá trị & bối cảnh định hướng | §12 | `Directional` (Phase 1D) | LOCKED |
| Ch 15 One-Time Framing | §12.4 | `Directional` (Phase 1D) | LOCKED |
| Ch 16 Auction Episode | §20 | `Episode` (Phase 1E) | LOCKED |
| Ch 17 Chấp nhận / từ chối / tái nhập | §21.1–21.3 | `Evidence` (Phase 1F) | LOCKED |
| Ch 18 Đấu giá thất bại / tiếp diễn | §21.4, §26, §27 | `Resolution` (Phase 2D) | CODE/TEST |
| Ch 19 Năm quy tắc vùng giá trị `[*]` | — **THIẾU TRONG v1.2** | — | **v1.3 §Phụ lục A** |
| Ch 20–21 Cơ chế khớp lệnh, phân loại phía chủ động | §22.1 | `Orderflow` (Phase 2A) | LOCKED |
| Ch 22 Delta | §22.2 | `Orderflow` (Phase 2A) | LOCKED |
| Ch 23 CVD | §22.3 | `Orderflow` (Phase 2A) | LOCKED |
| Ch 24 Footprint | §24.1–24.2 | `Cluster` (Phase 2B) | LOCKED |
| Ch 25 Imbalance | §24.1 | `Cluster` — raw only | **Phân loại NOT STARTED** |
| Ch 26 Khối lượng / số giao dịch / kích thước TB | §22.1 | `Orderflow` (Phase 2A) | LOCKED |
| Ch 27 Kiểm tra, tái kiểm tra, ký ức mức giá | §17.3 Metadata | `Reference` — một phần | **Ký ức mức giá NOT STARTED** |
| Ch 28 Ứng viên hấp thụ | §23.3, §23.5 | `EffortResult` (Phase 2E) | CODE/TEST — `[C]` |
| Ch 29 Ứng viên cạn kiệt | §23.3 | `EffortResult` (Phase 2E) | CODE/TEST — `[C]` |
| Ch 30 Nỗ lực và Kết quả | §23.1–23.3 | `Efficiency` (2C) + `EffortResult` (2E) | CODE/TEST |
| Ch 31 Khả năng tạo thuận lợi | §23.4 | `Facilitation` (Phase 2F) | CODE/TEST — `[C]` |
| Ch 32 Quét thanh khoản / dừng lỗ / mắc kẹt | §19 Sửa sai "Sweep" | `Probe` — MBO BLOCKED | **NOT STARTED** |
| Ch 33–36 DOM / Pulling / Iceberg / Stops | §24.5–24.10 | `Probe` — MBO BLOCKED | **NOT STARTED** |
| Ch 37 Chỉ báo tổng hợp áp lực | §24.11 | — | **NOT STARTED** `[R]` |
| Ch 38 Limit Tracing | §41.3 | — | **NOT STARTED** `[R]` |
| Ch 40 Hợp đồng, roll, chế độ tham gia | §10 | `Contract`, `Participation` (Phase 1G) | CODE/TEST |
| Ch 41 Sự kiện & vĩ mô | §40.1–40.3 | — | **NOT STARTED** |
| Ch 42–43 OI / COT | §40.4–40.5 | — | **NOT STARTED** |
| **Ch 44–50 GEX** | §— | — | **OUT OF SCOPE v1.3** |
| Ch 51–52 Vai trò ba trụ / đọc từ trên xuống | §11, §52 Orchestration | `Runtime` | Một phần |
| Ch 53–54 Vị trí trước tín hiệu / bối cảnh trước thực thi | §29, §31 | — | **Phase 3B** |
| Ch 55 Mốc → Episode | §20 | `Episode` | LOCKED |
| Ch 56 Episode → bằng chứng chấp nhận | §21.2 | `Evidence` (1F) + `Resolution` (2D) | CODE/TEST |
| Ch 57 Bằng chứng → Order Flow | §23 | `Efficiency` (2C) | CODE/TEST |
| Ch 59 Xây dựng luận điểm | §32.1 | `Thesis` (Phase 3A) | CODE/TEST |
| Ch 60 Vô hiệu đa chiều | §32.2 | `Thesis` — **chưa đủ 5 chiều** | **Phase 3C** |
| Ch 61 Mục tiêu & đường ít cản trở | §33 PLAR | — | **NOT STARTED** |
| Ch 62 Quản trị rủi ro | §35, §36 | — | **NOT STARTED** |
| Ch 63 Ba kết quả & ba mức trưởng thành | §29 Signal Maturity | — | **← PHASE 3B** |
| Ch 64 FAR | §26 | `Thesis.FarThesisHost` (3A) | CODE/TEST |
| Ch 65 AAC | §27 | `Thesis.AacThesisHost` (3A) | CODE/TEST |
| Ch 66–81 Họ chiến lược | §53 Scenario Matrix | — | **NOT STARTED** |
| Ch 82 Phân tách thị trường phân tích / thực thi | §2.7, §34 | — | **NOT STARTED** |
| Ch 83–88 Quy trình giao dịch, nhật ký, đánh giá | §45 Logging, §47 Validation | `Logging` — một phần | Một phần |

---

# 2. Tháp quyền hạn bằng chứng — chuẩn hóa

KDK quy định thứ tự quyền hạn. v1.2 §8 có 3 tier. v1.3 chuẩn hóa thành **4 tầng có thể kiểm tra**.

| Tầng | Tên | Nội dung | Được phép kết luận gì |
|---|---|---|---|
| **T1** | Executed Evidence | Giá đã khớp, khối lượng đã khớp, số giao dịch, dấu thời gian, mức giá được giao dịch | Mọi kết luận về **kết quả đấu giá** |
| **T2** | Classified Executed | Phân loại phía chủ động (Ask/Bid), Delta, CVD, cluster ratio | Kết luận về **nỗ lực có hướng** — chỉ khi tỷ lệ phân loại đủ cao |
| **T3** | Advertised Liquidity | DOM, MBO, iceberg, pulling/stacking | **Cơ chế thực thi** — không bao giờ là kết luận đấu giá |
| **T4** | Model Inference | Mắc kẹt, quét dừng lỗ, danh tính người tham gia, ý định | **KHÔNG BAO GIỜ** là bằng chứng chính; chỉ là nhãn nghiên cứu `[R]` |

## 2.1 Bất biến `[N][G]`

```text
G-EVID-001  Một kết luận ở tầng N không được dùng dữ liệu duy nhất từ tầng N+1 trở lên.
G-EVID-002  Nếu T2 không khả dụng (Unknown-aggressor only), hệ thống PHẢI
            xuống cấp sang T1 (tổng khối lượng, số giao dịch, tốc độ, tiến triển giá,
            quá trình xây profile) — KHÔNG được bịa Ask/Bid.
G-EVID-003  T3 vắng mặt (MBO BLOCKED) KHÔNG được làm bất kỳ kết luận T1/T2 nào
            bị chặn hoặc bị hạ trạng thái.
G-EVID-004  T4 KHÔNG được xuất hiện trong bất kỳ trường nào của Production snapshot.
```

> `G-EVID-002` chính là hành vi đã được chứng minh live ở Phase 2A/2B (Unknown-only Partial, Ask/Bid **unavailable** chứ không phải `0`). v1.3 nâng nó thành bất biến toàn hệ thống.

## 2.2 Ánh xạ sang mã nguồn hiện hữu

| Tầng | Enum hiện có |
|---|---|
| T1 | `EvidenceProvenance`, `OrderflowCoverageMode`, `ReferenceEvidenceTier` |
| T2 | `AggressorClassificationStatus`, `AggressorEvidenceAvailability` |
| T3 | `MboSubscriptionState`, `DepthLifecycleMarker`, `MboIsolationRequirement` |
| T4 | *(không có, và phải giữ nguyên như vậy)* |

---

# 3. Từ điển phân biệt (Discriminator Dictionary) `[N]`

**Đây là phần v1.2 thiếu nghiêm trọng nhất.** Mỗi mục dưới đây định nghĩa hai khái niệm dễ nhầm và **trường đo lường phân biệt chúng**.

## 3.1 Từ chối ≠ Cuộc đấu giá thất bại

| | Sự từ chối (Rejection) | Cuộc đấu giá thất bại (Failed Auction) |
|---|---|---|
| Định nghĩa | Phản ứng đẩy giá ra khỏi vùng vừa thử | Thử vùng mới, **không xây được chấp nhận bền vững**, **tái nhập VÀ duy trì** trong vùng cũ |
| Cần dữ liệu hậu sự kiện | Không | **Có — bắt buộc** |
| Có đủ cho FAR? | **KHÔNG** | Có |
| Trường phân biệt `[M]` | phản ứng tức thời | `ReentryResolutionState`, `TimeMaintainedInside`, `LocalPocResponse`, `OldValueReclaimSuccess` |

```text
G-DISC-001  Rejection candidate KHÔNG được nâng cấp thành Failed Auction
            khi chưa có bằng chứng duy trì sau tái nhập.
```

## 3.2 Tái nhập ≠ Tái chấp nhận

| | Tái nhập (Re-entry) | Tái chấp nhận (Reacceptance) |
|---|---|---|
| Bản chất | **Hình học** — giá quay vào vùng cũ | **Khả năng duy trì** — thị trường tổ chức lại giao dịch trong vùng cũ |
| Một râu nến quay vào | ĐỦ để gọi Re-entry | **KHÔNG BAO GIỜ** đủ |
| Bằng chứng cần | vị trí giá | POC cục bộ quay vào + vùng giá trị cục bộ hình thành trong + kiểm tra lại từ bên trong thất bại |
| Enum hiện có | `ReentryObservationState.GeometricReentry` | `ReentryResolutionState.StableReacceptance` `[C]` |

```text
G-DISC-002  GeometricReentry KHÔNG được tự động chuyển thành StableReacceptance.
G-DISC-003  Không dùng một cây nến / một râu nến làm bằng chứng bất kỳ
            state resolution nào. (v1.2 §21.5 — nâng thành guard)
```

**Bốn dạng quay vào** (v1.2 §21.4) — v1.3 bổ sung tiêu chí đo:

| Dạng | Đặc trưng đo được `[M]` | Đủ nền FAR? |
|---|---|---|
| `TemporaryCrossBack` | `TimeMaintainedInside` rất ngắn; không có POC cục bộ trong | Không |
| `MechanicalBounce` | `DistanceReturnedInside` nông; `OppositeAggression` không tạo tiến triển | Không |
| `PartialReentry` | Vào một phần vùng, `LocalValueRebuildInside` chưa hình thành | Không |
| `StableReacceptance` `[C]` | POC cục bộ quay vào + vùng giá trị cục bộ trong + kiểm tra lại từ trong thất bại + duy trì | **Có — duy nhất** |

## 3.3 Độ lệch ≠ Quét thanh khoản ≠ Kích hoạt dừng lỗ

| | Độ lệch (Reference Excursion) | Quét thanh khoản (Liquidity Sweep) | Kích hoạt dừng lỗ (Stop Run) |
|---|---|---|---|
| Tầng bằng chứng | **T1** | **T3** | **T4** |
| Định nghĩa | Giá giao dịch ngoài mốc tham chiếu | Dòng lệnh chủ động lấy thanh khoản **qua nhiều mức giá liên tiếp** với tốc độ phù hợp, có DOM/Tape hỗ trợ | Suy luận từ tốc độ + vị trí + chuỗi khớp |
| Được phép dùng khi MBO BLOCKED? | **Có** | **KHÔNG** | **KHÔNG** |
| Vai trò trong FAR/AAC | Là **câu hỏi** mở Episode | Chỉ là cơ chế thực thi | Chỉ là nhãn nghiên cứu `[R]` |

```text
G-DISC-004  Khi MboSubscriptionState != Active, hệ thống KHÔNG được phát ra
            bất kỳ nhãn Sweep / StopRun / TrappedTrader nào.
G-DISC-005  "Giá vượt đỉnh/đáy" chỉ được gọi là Excursion. Không nhãn nào khác.
```

> KDK Ch 32: *điều quan trọng không phải nhãn quét thanh khoản mà là khả năng duy trì của giá sau đó.* Đây là lý do FAR/AAC luôn ưu tiên hơn câu chuyện săn dừng lỗ.

## 3.4 Hấp thụ ≠ Cạn kiệt ≠ Đảo chiều

| | Ứng viên hấp thụ (Absorption) | Ứng viên cạn kiệt (Exhaustion) | Đảo chiều (Reversal) |
|---|---|---|---|
| Nỗ lực | **Lớn / lặp lại** | **Giảm dần** | không xác định |
| Kết quả | **Nhỏ** | **Yếu** | thay đổi cấu trúc |
| Cơ chế | Lực cản thụ động phía đối diện | Bên chủ động hụt hơi | Cấu trúc mới hình thành |
| Đủ để vào lệnh ngược? | **KHÔNG** | **KHÔNG** | Cần bằng chứng riêng |
| Điều kiện nâng cấp | Cần điều kiện kích hoạt **theo cấu trúc** + tái nhập | Cần hoạt động khởi xướng phía đối diện / phá OTF / lấy lại mốc | — |
| Enum `[C]` | `PotentialPassiveAbsorption` | `PotentialExhaustion` | *(không có enum — cố ý)* |

```text
G-DISC-006  Hai cơ chế có thể cùng xuất hiện ở các giai đoạn khác nhau của một Episode.
            Classifier KHÔNG được giả định loại trừ lẫn nhau.
G-DISC-007  Không gọi mọi phân kỳ Delta là hấp thụ.
G-DISC-008  Không gọi khối lượng thấp là cạn kiệt trong mọi chế độ —
            phải phân tầng theo ParticipationRegime + VolatilityRegime.
```

> KDK Ch 28: từ *"ứng viên"* là bắt buộc vì **ý định phía thụ động không quan sát được**. Tên enum hiện tại đã đúng (`Potential*`) — v1.3 khóa cách đặt tên này.

## 3.5 Sự chấp nhận ≠ Khối lượng lớn

Khối lượng lớn tại một mức giá **không phải** bằng chứng chấp nhận. Chấp nhận là **quá trình xây hoạt động**: thời gian ngoài + khối lượng ngoài + số giao dịch ngoài + POC cục bộ + khả năng giữ sau nhịp hồi + tiến triển vùng giá trị.

```text
G-DISC-009  AcceptanceResolutionState KHÔNG được phụ thuộc vào một trường đơn lẻ.
            Phải có ≥ 3 nhóm bằng chứng độc lập hội tụ. (KDK Ch 56)
```

## 3.6 Hoạt động đáp ứng ≠ Hoạt động khởi xướng

| | Đáp ứng (Responsive) | Khởi xướng (Initiative) |
|---|---|---|
| Vị trí | Từ **ngoài** vùng giá trị quay về | Từ **trong** vùng giá trị đẩy ra |
| Ý nghĩa | Ủng hộ cân bằng / luân phiên | Ủng hộ khám phá giá / AAC |
| Cần biết trước | **Vị trí so với vùng giá trị** | như trên |

```text
G-DISC-010  KHÔNG đọc Delta trước khi biết hướng của lần thử đấu giá
            và vị trí so với vùng giá trị. (KDK Ch 57)
```

## 3.7 Vô hiệu ≠ Dừng lỗ

| | Vô hiệu (Invalidation) | Dừng lỗ (Stop Loss) |
|---|---|---|
| Bản chất | **Lý do luận điểm không còn đúng** | Lệnh bảo vệ tài khoản |
| Chiều | 5 chiều (§11.3) | 1 chiều (giá) |
| Nếu khoảng cách quá xa | **Bỏ giao dịch hoặc giảm khối lượng** | Không được ép sát để tăng khối lượng |

## 3.8 Setup ≠ Trigger

Setup là **cấu trúc đủ điều kiện quan sát**. Trigger là **sự kiện cho phép thực thi**. Một Setup không có Trigger **không phải** tín hiệu.

```text
G-DISC-011  Candidate (setup) và Executable (trigger) PHẢI là hai state riêng biệt
            trong mọi thesis state machine. Không được gộp.
```

## 3.9 Chưa được giải quyết là kết quả HỢP LỆ

```text
G-DISC-012  Unresolved KHÔNG được ép thành FAR hoặc AAC.
G-DISC-013  "Không giao dịch" là một quyết định có cấu trúc, phải được
            log như một outcome, không phải sự vắng mặt của outcome.
```

---

# 4. Hợp đồng Nỗ lực–Kết quả — bổ sung cho v1.2 §23

## 4.1 Vectơ Nỗ lực chuẩn tắc `[M]`

KDK Ch 30 định nghĩa nỗ lực rộng hơn v1.2 §23.1.

| Thành phần | Tầng | Đã build (Phase 2C) | Ghi chú |
|---|---|---|---|
| Khối lượng đã khớp | T1 | ✅ `totalExecutedVolume` | |
| Số giao dịch | T1 | ✅ `tradeCount` | |
| **Số mức giá được giao dịch** | T1 | ✅ `priceLevelCount` | KDK nhấn mạnh — v1.2 §23.1 thiếu |
| **Thời gian** | T1 | ✅ `observationDuration` | v1.2 §23.1 thiếu |
| **Tốc độ** | T1 | ✅ `tradesPerSecondRaw`, `contractsPerSecondRaw` | |
| **Các lần kiểm tra lặp lại** | T1 | ✅ `revisitedLevelCount`, `maximumVisitCount` | v1.2 §23.1 thiếu |
| Áp lực chủ động đã phân loại | T2 | ✅ `askVolume` / `bidVolume` / `classifiedVolume` | nullable khi không phân loại được |
| Delta tuyệt đối | T2 | ✅ `absoluteClassifiedDelta` | |
| Imbalance count / Stacked | T2 | ⚠️ raw dominance đã có; **phân loại NOT STARTED** | Ch 25 |
| Big Trade activity | T2 | ❌ NOT STARTED | Ch 26 |
| MBO Sweep / Stop activity | T3/T4 | ❌ BLOCKED — theo `G-DISC-004` | |

## 4.2 Vectơ Kết quả chuẩn tắc `[M]`

| Thành phần | Đã build (Phase 2C) |
|---|---|
| Tiến triển ròng (net ticks) | ✅ |
| Mức mở rộng phạm vi | ✅ |
| Độ lệch thuận lợi tối đa (MFE) | ✅ `maximumFavorableProgressTicks` |
| **Độ lệch bất lợi tối đa (MAE)** | ✅ — KDK Ch 30 yêu cầu **cả hai**; v1.2 §23.2 chỉ nêu favorable |
| Dịch chuyển vùng giá trị & POC | ✅ |
| **Khả năng duy trì ngoài mốc** | ✅ | 
| **Khả năng giữ sau nhịp hồi** | ⚠️ một phần — thuộc `Evidence` 1F |
| Vị trí đóng cửa | ⚠️ `OutsideCloseRatio` — **NOT STARTED** (đã ghi deferred ở 1F) |

## 4.3 Ma trận bốn góc phần tư `[N]` — **THIẾU HOÀN TOÀN TRONG v1.2**

Đây là đóng góp tri thức lớn nhất của KDK Ch 30. v1.2 §23.3 chỉ liệt kê 8 state phẳng, không có cấu trúc.

| | **Kết quả LỚN** | **Kết quả NHỎ** |
|---|---|---|
| **Nỗ lực LỚN** | **Lớn–Lớn**: khả năng tạo thuận lợi tốt. ⚠️ Cảnh báo: nếu xảy ra **ngay trước rào cản lớn**, dư địa lợi nhuận còn lại có thể thấp → không tự động là tín hiệu tốt. | **Lớn–Nhỏ**: ứng viên phân kỳ. Nguyên nhân khả dĩ: kháng cự thụ động, dòng lệnh đối diện, thanh khoản được bổ sung lại, **hấp thụ**. |
| **Nỗ lực NHỎ** | **Nhỏ–Lớn**: thanh khoản mỏng / thiếu lực đối ứng / khoảng trống thanh khoản. Có thể tiếp diễn nhanh nhưng **rủi ro trượt giá và hồi mạnh cao**. | **Nhỏ–Nhỏ**: cân bằng hoặc chờ sự kiện. **Môi trường nên tránh giao dịch quá mức.** |

### 4.3.1 Ánh xạ chuẩn tắc sang enum hiện có

| Góc phần tư | `EffortResultClassificationState` `[C]` | Điều kiện bổ sung bắt buộc |
|---|---|---|
| Lớn–Lớn | `AggressionEffective` (101) | + kiểm tra `RemainingTargetSpace` trước khi coi là thuận lợi |
| Lớn–Nhỏ | `AggressionIneffective` (102) → có thể nâng cấp `PotentialPassiveAbsorption` (103) | Nâng cấp cần: nỗ lực **lặp lại** + POC cục bộ không đi theo + tái nhập |
| Nhỏ–Lớn | *(chưa có enum)* — **v1.3 đề xuất `ThinLiquidityProgress`** `[C]` | Bắt buộc gắn cờ `ThinParticipationLabel` |
| Nhỏ–Nhỏ | `EffortResultBalanced` (100) hoặc `PotentialExhaustion` (104) | `PotentialExhaustion` chỉ khi nỗ lực **giảm dần qua ≥3 nhịp liên tiếp** tại **cực trị** |

```text
G-ER-001  Phân loại góc phần tư PHẢI dựa trên so sánh với đường cơ sở
          cùng chế độ (regime), KHÔNG dùng một tỷ lệ tuyệt đối cho mọi chế độ.
G-ER-002  Lớn–Lớn KHÔNG tự động là tín hiệu tốt. Phải kiểm tra dư địa mục tiêu.
G-ER-003  Nhỏ–Lớn PHẢI mang cờ cảnh báo trượt giá; không được coi ngang
          với Lớn–Lớn về chất lượng.
G-ER-004  PotentialExhaustion yêu cầu so sánh ≥3 nhịp liên tiếp (KDK Ch 29).
          Một nhịp đơn lẻ KHÔNG đủ.
```

## 4.4 Hợp đồng đo lường bắt buộc `[N]`

KDK Ch 30 quy trình: *Định nghĩa cửa sổ đo → Ghi nỗ lực thô → Ghi kết quả thô → So với lịch sử cùng chế độ → Đặt trong Vị trí và Episode → Không gắn nhãn khi chưa có đường cơ sở.*

```text
G-ER-005  Mọi cặp (Effort, Result) PHẢI ghi kèm:
            - MeasurementWindow (định nghĩa rõ ràng, bất biến trong một snapshot)
            - EpisodeId / ScopeType
            - PriceValueLocation tại thời điểm đo
            - ParticipationRegime + SettlementProximityTag
          Thiếu bất kỳ trường nào → DataQuality = Partial, KHÔNG phát classification.
```

---

# 5. Khả năng tạo thuận lợi cho giao dịch — bổ sung v1.2 §23.4

v1.2 §23.4 chỉ có hai câu hỏi và một dòng công thức khái niệm. KDK Ch 31 cho đủ ngữ nghĩa.

## 5.1 Định nghĩa chuẩn tắc `[N]`

> Khả năng tạo thuận lợi = **thị trường làm tốt đến đâu theo hướng cuộc đấu giá đang thử**.
> Đây là **kết luận tổng hợp từ hoạt động, tiến triển và sự duy trì** — **không phải một con số Delta**.

## 5.2 Bốn thành phần bắt buộc `[M]`

| Thành phần | Khỏe | Suy yếu |
|---|---|---|
| **Hoạt động** | áp lực chủ động + khối lượng có mặt | nỗ lực **tăng** |
| **Tiến triển** | phạm vi mở rộng | tiến triển **giảm** |
| **Cấu trúc** | POC / vùng giá trị **dịch theo hướng** | POC **không đi theo** |
| **Sự duy trì** | nhịp hồi giữ; giao dịch tiếp tục ở vùng mới | giá **liên tục tái nhập**; vùng giá trị cũ được lấy lại |

```text
G-TF-001  TradeFacilitation KHÔNG được kết luận từ Delta cùng dấu.
G-TF-002  Cả 4 thành phần phải có mặt (hoặc được đánh dấu unavailable)
          trước khi phát Healthy/Failing. Thiếu → NotCalibrated.
G-TF-003  Nhãn Facilitation PHẢI kèm khung thời gian đo. Không có khung → không nhãn.
```

## 5.3 Facilitation trong phá vỡ và trong đảo chiều `[N]`

| Bối cảnh | Điều kiện để nói "có facilitation" |
|---|---|
| **Phá vỡ** | Không chỉ xuyên biên mà còn **tổ chức hoạt động ngoài biên**. Chỉ có nhịp đột biến → **vẫn Unresolved**. |
| **Đảo chiều** | Dòng lệnh đối diện phải làm giá **tái nhập + duy trì + thay đổi cấu trúc**. Một lần Delta đổi dấu ngắn **không đủ**. |

## 5.4 Tính tương đối bắt buộc `[N]`

> *Một đoạn 10 tick có thể là tiến triển tốt trong vùng cân bằng nhưng lại yếu trong một sự kiện mở rộng.*

```text
G-TF-004  Mọi ngưỡng facilitation PHẢI được phân tầng theo:
          VolatilityRegime × ParticipationRegime × DirectionalHorizon.
          Một ngưỡng phẳng toàn cục là vi phạm spec.
```

**Trạng thái Phase 2F hiện tại:** `TradeFacilitationHost` đã lưu đúng các thành phần thô (`DirectionConsistentEffortRatio`, `FavorableProgressPerDirectionUnit`) và luôn phát `NotCalibrated`. **Điều này đúng với v1.3.** Việc còn thiếu là: thành phần **Cấu trúc** (POC migration alignment) và **Sự duy trì** (post-pullback hold) chưa vào vector — xem §15 lộ trình.

---

# 6. Chấp nhận / Tái nhập / Resolution — bổ sung v1.2 §21

## 6.1 Chấp nhận là quá trình, không phải sự kiện `[N]`

Sáu nhóm bằng chứng của chấp nhận (KDK Ch 17 + Ch 56):

1. Thời gian bên ngoài
2. Khối lượng bên ngoài
3. Số giao dịch bên ngoài
4. POC cục bộ
5. Khả năng giữ sau nhịp hồi
6. Tiến triển của vùng giá trị

```text
G-ACC-001  Mỗi tỷ lệ ngoài vùng PHẢI có mẫu số được ghi rõ trong snapshot.
G-ACC-002  KHÔNG có ngưỡng phổ quát cho bất kỳ tỷ lệ nào.
G-ACC-003  KHÔNG gán 0 cho dữ liệu không khả dụng. Dùng null + limitation string.
G-ACC-004  Vùng giá trị cục bộ TÁCH KHỎI vùng giá trị cũ là bằng chứng
           mạnh hơn một nhịp đột biến — phải là trường riêng, không gộp.
```

## 6.2 Phép thử quyết định: Khả năng lấy lại vùng giá trị cũ `[N]`

**Đây là trục phân biệt FAR/AAC quan trọng nhất trong KDK Ch 18, và v1.2 chỉ nhắc thoáng qua (`OldValueReclaimFailure` trong danh sách vector).**

| Kết quả phép thử | Kết luận được hỗ trợ |
|---|---|
| Lấy lại **và duy trì được** | **FAR** |
| Lấy lại **thất bại** hoặc **chỉ thoáng qua** | **AAC** |
| Chưa có nỗ lực lấy lại | **Unresolved** — chưa đủ dữ liệu |

```text
G-ACC-005  OldValueReclaim PHẢI là một cấu trúc 3 trạng thái có thời hạn:
           NotAttempted / AttemptedAndHeld / AttemptedAndFailed.
           Một boolean nullable là KHÔNG đủ.
```

## 6.3 Ma trận Resolution `[C]`

| Chấp nhận ngoài | Tái nhập & duy trì | Lấy lại vùng cũ | Kết luận |
|---|---|---|---|
| Không hình thành | Có, ổn định | Thành công | **FAR candidate** `[C]` |
| Đang phát triển / đã thiết lập | Không | Thất bại | **AAC candidate** `[C]` |
| Bằng chứng cân bằng hoặc thay đổi liên tục | — | — | **Unresolved** |
| Chấp nhận ngoài **và** tái chấp nhận trong cùng lúc | — | — | **Unresolved** — xung đột, không ép |

```text
G-ACC-006  Xung đột bằng chứng PHẢI ra Unresolved, KHÔNG được chọn bên mạnh hơn.
```

---

# 7. FAR — bổ sung v1.2 §26

## 7.1 Bảy điều kiện cần `[N]` (KDK Ch 64)

v1.2 §26.5 liệt kê điều kiện nhưng không đầy đủ. Danh sách chuẩn tắc:

```text
1. Vùng mốc rõ ràng                        → ReferenceStatus, ReferenceMaturity
2. Episode hợp lệ                          → EpisodeState ∈ {OutsideAttempt, ReentryDeveloping}
3. KHÔNG có chấp nhận đã hình thành ngược  → AcceptanceResolutionState != Established
4. Tái nhập VÀ duy trì                     → ReentryResolutionState = StableReacceptance [C]
5. Hình học điểm vào → vô hiệu rõ           → InvalidationGeometry != null
6. Đường tới mục tiêu còn khoảng trống      → RemainingTargetSpace > 0
7. Dữ liệu và thị trường thực thi hợp lệ    → DataState = Ready, ExecutionContextAvailability
```

```text
G-FAR-001  Cả 7 điều kiện PHẢI đồng thời thỏa. Thiếu 1 → không Armed.
G-FAR-002  FAR KHÔNG giao dịch cú vượt biên. FAR giao dịch sự THẤT BẠI
           trong việc xây cuộc đấu giá mới VÀ sự TÁI CHẤP NHẬN cuộc đấu giá cũ.
```

## 7.2 Vô hiệu cốt lõi `[N]`

> FAR sai khi thị trường **thiết lập sự chấp nhận bền vững ngoài mốc**: hoạt động / POC / vùng giá trị xây ngoài; nhịp hồi giữ được; nỗ lực lấy lại vùng cũ thất bại.

Ánh xạ: `FarState.Invalidated` (200) khi `AcceptanceResolutionState == Established` theo hướng ngược `[C]`.

## 7.3 FAR hai lần thử `[N]`

> Lần thử thứ hai có **nỗ lực tương đương hoặc lớn hơn** nhưng **kết quả kém hơn**, sau đó tái nhập ổn định.

```text
G-FAR-003  Two-Attempt chỉ mạnh khi SO SÁNH ĐƯỢC ĐO LƯỜNG.
           "Số hai" KHÔNG có quyền lực đặc biệt. (KDK Ch 64)
G-FAR-004  Lần thử 2 KHÔNG tự động là điểm vào. (KDK Ch 18)
```

Trường bắt buộc `[M]`: `Attempt1EffortVector`, `Attempt1ResultVector`, `Attempt2EffortVector`, `Attempt2ResultVector`, `EffortDelta`, `ResultDelta`.

## 7.4 Mục tiêu FAR `[N]`

POC, trung tâm vùng giá trị, biên đối diện, Composite POC, HVN trước đó.

```text
G-FAR-005  Biên đối diện là mục tiêu TIỀM NĂNG, không phải mục tiêu bắt buộc.
G-FAR-006  POC là rào cản trung gian và CÓ THỂ NGẮT đường luân phiên.
           Target engine PHẢI mô hình hóa POC như barrier, không chỉ như target.
```

---

# 8. AAC — bổ sung v1.2 §27

## 8.1 Sáu điều kiện cần `[N]` (KDK Ch 65)

```text
1. Lần thử đấu giá ngoài vùng                → EpisodeState = OutsideAttempt
2. Chấp nhận đang phát triển hoặc đã thiết lập → AcceptanceResolutionState ∈ {Developing, Established[C]}
3. Tiến triển tương xứng với nỗ lực          → TradeFacilitation = Healthy [C]
4. Nỗ lực lấy lại vùng giá trị cũ THẤT BẠI   → OldValueReclaim = AttemptedAndFailed
5. Đường tới mục tiêu còn khoảng trống       → RemainingTargetSpace > 0
6. Vô hiệu rõ ràng                          → InvalidationGeometry != null
```

## 8.2 Vô hiệu cốt lõi `[N]`

> AAC sai khi giá **tái chấp nhận bền vững** vào vùng giá trị cũ: thời gian, hoạt động và POC quay vào; nỗ lực khôi phục vùng ngoài thất bại. **Một bóng nến quay vào chưa đủ.**

Ánh xạ: `AacState.ReacceptedOldValue` (200) — đã đúng ở Phase 3A.

```text
G-AAC-001  AacState.ReacceptedOldValue CHỈ được phát khi
           ReentryResolutionState = StableReacceptance [C],
           KHÔNG phải khi ReentryObservationState = GeometricReentry.
```

> **Lưu ý sai lệch hiện tại `[G]`:** `src/GC.AuctionFlow/Thesis/AacThesisHost.cs:227-230` map `EpisodeState.ReentryDeveloping → AacState.Invalidated` với `notCalibrated = false` và chú thích *"we can definitively observe this without calibrated thresholds"*.
>
> Theo KDK Ch 65 (*"Một bóng nến quay vào chưa đủ"*) và `G-DISC-002`, điều này **quá sớm**: `ReentryDeveloping` mới chỉ là **hình học**. Vô hiệu AAC yêu cầu **tái chấp nhận bền vững** — tức `ReentryResolutionState.StableReacceptance` `[C]`, hiện đang bị chặn.
>
> **Hành vi đúng theo v1.3:** `ReentryDeveloping → AacState.Pullback` với `notCalibrated = true`. Chuyển sang `ReacceptedOldValue` (200) chỉ khi resolution được hiệu chỉnh. Xem §15.1 mục 3.

## 8.3 Mục tiêu AAC `[N]`

Mốc cấu trúc có ý nghĩa tiếp theo, **hành lang LVN**, biên Composite, hoặc vùng từng được chấp nhận trên đường đi.

---

# 9. Ba mức trưởng thành của điểm vào — bổ sung v1.2 §29 ← **PHASE 3B**

v1.2 §29.2 mô tả Fast/Standard/Confirmed bằng văn bản. KDK Ch 63 + 64 + 65 cho **tiêu chí đo được**.

## 9.1 Bảng tiêu chí chuẩn tắc `[N][M]`

| Mức | Điều kiện đấu giá bắt buộc | Điều kiện xác nhận | Rủi ro đặc trưng |
|---|---|---|---|
| **Sớm (Fast)** | Bằng chứng **mới phát triển** | Tái nhập hình học + phản ứng đối diện ban đầu | **Nguy cơ phân loại sai cao** — giá tốt hơn |
| **Tiêu chuẩn (Standard)** | Tái nhập **hoặc** chấp nhận **rõ ràng** | **+ một lần kiểm tra hành vi** (micro test): nỗ lực quay ra ngoài thất bại **và** dòng lệnh đối diện tạo tiến triển | cân bằng |
| **Xác nhận (Confirmed)** | Vùng giá trị / POC **duy trì** | **+** kiểm tra lại từ bên trong **hoặc** lần thử thứ hai **+** bằng chứng tiếp diễn | Giá vào kém hơn, luận điểm trưởng thành, vô hiệu rõ hơn |

### 9.1.1 Chi tiết theo họ chiến lược

| | FAR | AAC |
|---|---|---|
| **Sớm** | Vào sau tái nhập hình học + phản ứng đối diện đầu tiên | Giá duy trì ngoài + dòng lệnh tạo tiến triển, **trước** một lần kiểm tra sâu |
| **Tiêu chuẩn** | Chờ giá quay lại kiểm tra mốc / vùng tái nhập; vào khi **nỗ lực quay ra ngoài thất bại** | Chờ nhịp hồi về mốc / vùng vừa chấp nhận; vào khi **nỗ lực lấy lại vùng cũ thất bại** |
| **Xác nhận** | Chờ POC / vùng giá trị cục bộ **quay vào**, sau đó kiểm tra mốc **từ bên trong** | Chờ **vùng cân bằng nhỏ / vùng giá trị mới hình thành ngoài**, sau đó vào lần mở rộng tiếp theo hoặc nhịp hồi về biên vùng giá trị mới |

## 9.2 Chính sách retest `[N]`

```text
Không có retest      → KHÔNG tự động loại; chỉ được Sớm khi micro-confirmation đủ mạnh
Structural retest    → Tiêu chuẩn
Lần thử 2 / mature   → Xác nhận
```

```text
G-MAT-001  KHÔNG có luật cứng "mọi Production signal bắt buộc structural retest".
G-MAT-002  Retest KHÔNG phải định luật tuyệt đối — nó là công cụ tạo hình học
           vào lệnh và vô hiệu rõ hơn, và KHÔNG phải lúc nào cũng xảy ra. (KDK Ch 17)
G-MAT-003  Giá vào đẹp KHÔNG đồng nghĩa luận điểm trưởng thành. (KDK Ch 63)
```

## 9.3 Bảng hành vi kỳ vọng `[N][M]` — **THIẾU HOÀN TOÀN TRONG v1.2**

KDK Ch 63 quy định: mỗi kịch bản phải khai báo trước hành vi **phải xuất hiện nếu luận điểm đúng**. Đây là nền tảng của Time Invalidation.

| Kịch bản | Hành vi phải xuất hiện nếu luận điểm đúng |
|---|---|
| FAR tái nhập | Giá tiến vào vùng giá trị cũ và **không nhanh chóng khôi phục vùng ngoài** |
| FAR kiểm tra lại | Lần kiểm tra **không xây được chấp nhận ngoài**; dòng lệnh đối diện tạo kết quả |
| AAC sớm | Giá **giữ ngoài mốc**; POC / vùng giá trị cục bộ **không quay vào** |
| AAC kiểm tra lại | Nỗ lực lấy lại vùng giá trị cũ **thất bại**; giá **khôi phục vùng đã được chấp nhận** |
| Tiếp diễn vùng giá trị mới | Vùng giá trị mới **giữ được** và POC **tiếp tục dịch** |
| Luân phiên | Giá **không xây vùng giá trị ngoài biên** và **quay lại trung tâm** |

```text
G-MAT-004  Mỗi ThesisCandidate PHẢI mang một ExpectedBehaviorContract
           được chọn từ bảng trên tại thời điểm tạo. Không có → không Armed.
G-MAT-005  ExpectedBehaviorContract PHẢI có deadline. Hết deadline mà hành vi
           không xuất hiện → Time Invalidation (§11.3), KHÔNG phải chờ chạm stop.
```

## 9.4 Hàng rào kiểm chứng cho phong cách Sớm `[N]`

KDK Ch 63 quy định phong cách Sớm **mặc định là phong cách nghiên cứu**:

```text
Phát lại dữ liệu
→ quan sát và ghi nhật ký KHÔNG thực thi
→ giao dịch mô phỏng hoặc quy mô rất nhỏ
→ đánh giá ngoài mẫu
→ chỉ sau đó mới dùng mức rủi ro chuẩn
```

Yêu cầu bắt buộc:

```text
G-FAST-001  Tiêu chuẩn mẫu PHẢI được xác định TRƯỚC, không chọn lại sau khi xem kết quả.
G-FAST-002  Kết quả PHẢI tính chênh lệch mua bán, hoa hồng, trượt giá và lệnh không khớp.
G-FAST-003  Phân tầng theo phiên, chế độ biến động và loại mốc.
G-FAST-004  Chưa vượt cổng bằng chứng → chỉ dùng Tiêu chuẩn hoặc Xác nhận.
G-FAST-005  KHÔNG tăng khối lượng chỉ vì vài giao dịch Sớm thắng đẹp.
```

Điều này **khớp hoàn toàn** với v1.2 §29.5 FAST Deployment Guardrail. v1.3 giữ nguyên cấu hình mặc định:

```csharp
EnableFastCandidateDetection = true;
EnableFastShadowLogging      = true;
EnableFastActionAlerts       = false;
EnableFastExecutablePlans    = false;
EnableFastRiskSizing         = false;
```

## 9.5 Hợp đồng Phase 3B `[N]`

| Hạng mục | Quy định |
|---|---|
| Enum đề xuất | `SignalMaturityLevel { Unknown=0, NotCalibrated=1, Fast=100, Standard=101, Confirmed=102 }` |
| Trạng thái phát ra ở 3B | **Luôn `NotCalibrated`** — tiêu chí trưởng thành chưa hiệu chỉnh |
| Lifecycle enum | `AnalysisLifecycleState { Observation=0, Approaching=1, EpisodeActive=2, Candidate=3, NotCalibrated=4, Armed=100, Executable=101, Managing=102, Completed=200, Invalidated=201, Expired=202 }` |
| Trường bắt buộc | `ExpectedBehaviorContract` (enum + deadline), `RetestObservation`, `MicroConfirmationEvidence`, `MaturityBlockingReasons[]` |
| Limitation strings | `SIGNAL_MATURITY_THRESHOLDS_NOT_CALIBRATED`, `FAST_MATURITY_SHADOW_ONLY`, `EXPECTED_BEHAVIOR_DEADLINE_NOT_CALIBRATED` |
| GPS row | `MATURITY:` — row thứ 13 |
| Schema | `0.15.0` → `0.16.0` |

---

# 10. Vị trí trước tín hiệu — bổ sung (KDK Ch 53)

**v1.2 không có mục riêng cho Location, dù §2.3 tuyên bố "Orderflow chỉ có ý nghĩa trong Context và Location".** v1.3 chuẩn hóa.

| Vị trí | Ý nghĩa cho tín hiệu | `PriceValueLocation` (đã build, `Directional`) |
|---|---|---|
| **Giữa vùng giá trị** | Vị trí **kém nhất** cho FAR/AAC. Ưu tiên không giao dịch. | `InsideValue` (2), `AtPoc` (3) |
| **Tại biên vùng giá trị** | Vị trí mở Episode. Luân phiên hoặc phá vỡ đều khả dĩ. | `AtValueHigh` (1), `AtValueLow` (4) |
| **Ngoài vùng giá trị** | Vị trí của FAR (thất bại) và AAC (chấp nhận). | `AboveValue` (0), `BelowValue` (5) |
| **Không xác định** | Không được sinh thesis. | `Unavailable` (6) |
| **Không gian mục tiêu** | Nếu không còn khoảng trống → **không giao dịch bất kể tín hiệu**. | `RemainingTargetSpace` — **NOT STARTED** |

```text
G-LOC-001  Bất kỳ thesis nào phát sinh tại InsideValue/AtPoc PHẢI mang
           cờ LowQualityLocation và KHÔNG được đạt maturity Confirmed.
G-LOC-002  RemainingTargetSpace = 0 → Hard Veto, không phụ thuộc score.
G-LOC-003  PriceValueLocation = Unavailable → không sinh ThesisCandidate.
```

---

# 11. Hợp đồng luận điểm — bổ sung v1.2 §32

## 11.1 Bảy trường bắt buộc `[N]` (KDK Ch 59)

```text
1. Bối cảnh          — trạng thái, vị trí, hướng cấu trúc & ngắn hạn, sự kiện
2. Lần thử           — mốc nào, hướng nào, Episode đang ở đâu
3. Bằng chứng        — chấp nhận/tái nhập, Nỗ lực–Kết quả, duy trì, VÀ BẰNG CHỨNG CÒN THIẾU
4. Hành vi kỳ vọng   — §9.3
5. Vô hiệu           — §11.3, 5 chiều
6. Đường mục tiêu    — rào cản trung gian + hành lang mục tiêu
7. Thời hạn luận điểm — luận điểm sống bao lâu
```

```text
G-THE-001  Trường "bằng chứng còn thiếu" là BẮT BUỘC và phải là danh sách
           có cấu trúc, không phải chuỗi tự do. Một luận điểm không khai báo
           được cái nó thiếu là một luận điểm không hợp lệ.
```

## 11.2 Cam kết năm khung thời gian `[N]` — **THIẾU TRONG v1.2**

KDK Ch 59 bắt buộc mỗi luận điểm ghi **năm khung**:

| Khung | Vai trò |
|---|---|
| **Khung bối cảnh** | Xác định trạng thái đấu giá bao trùm |
| **Khung luận điểm** | Khung mà FAR/AAC được phát biểu |
| **Khung điều kiện kích hoạt** | Khung của trigger |
| **Khung quản lý** | Khung để đánh giá tiến triển sau khi vào |
| **Khung mục tiêu** | Khung mà mục tiêu có ý nghĩa |

```text
G-THE-002  KHÔNG vào bằng điều kiện vài phút rồi biến lệnh thua thành
           vị thế nhiều ngày. → ThesisHorizonMutation guard (v1.2 §42.7)
G-THE-003  KHÔNG dùng một lần Delta đổi dấu để vô hiệu luận điểm nhiều phiên.
G-THE-004  Mục tiêu và thời hạn vô hiệu PHẢI phù hợp với khung của luận điểm.
```

## 11.3 Vô hiệu năm chiều `[N]`

v1.2 §32.2 có **bốn** loại. KDK Ch 60 có **năm**. v1.3 bổ sung chiều thứ năm.

| Chiều | Định nghĩa | Có trong v1.2? |
|---|---|---|
| **Theo giá** | Giá vượt mức cấu trúc khiến hình học sai. Cần khoảng đệm theo tick và spread. | ✅ |
| **Theo cuộc đấu giá** | FAR sai khi chấp nhận ngoài được thiết lập; AAC sai khi vùng cũ tái chấp nhận bền vững. | ✅ |
| **Theo thời gian** | Hành vi kỳ vọng không xảy ra trong thời hạn. | ✅ |
| **Theo bối cảnh** | Sự kiện, thanh khoản, ánh xạ hợp đồng hoặc **chất lượng dữ liệu** thay đổi. | ✅ |
| **Theo bằng chứng** | Dòng lệnh đối diện **tạo kết quả và sự duy trì**; POC / vùng giá trị cục bộ **dịch ngược**. | ❌ **BỔ SUNG v1.3** |

```text
G-INV-001  Vô hiệu theo bằng chứng PHẢI là một chiều độc lập, không gộp
           vào vô hiệu theo cuộc đấu giá. Nó kích hoạt SỚM HƠN và dựa trên
           Effort/Result + POC migration, không cần chờ acceptance hình thành.
G-INV-002  Dừng lỗ PHẢI nằm tại nơi luận điểm sai. Nếu khoảng dừng quá xa
           → bỏ lệnh hoặc giảm khối lượng. KHÔNG ép dừng lỗ gần để tăng khối lượng.
G-INV-003  Giữ lệnh sau khi luận điểm đã sai vì "stop chưa chạm" là vi phạm.
           Engine PHẢI phát InvalidationSignal độc lập với StopHit.
```

## 11.4 Luật nhất quán luận điểm `[N]`

> Mọi hành động sau điểm vào phải liên hệ được với **hành vi được kỳ vọng, vô hiệu, mục tiêu hoặc chính sách rủi ro**.
>
> Một thông tin mới chỉ được thay đổi giao dịch khi nó: **(a)** liên quan trực tiếp đến luận điểm, **(b)** tạo kết quả trên giá, **(c)** được duy trì, **(d)** xuất hiện trong khung thời gian phù hợp.

```text
G-THE-005  ManagementReasonLedger (v1.2 §37.3) PHẢI ép mỗi entry tham chiếu
           tới một trong 4 nguồn hợp lệ trên. Entry không tham chiếu được → reject.
G-THE-006  Bốn điều kiện (a)(b)(c)(d) PHẢI là 4 boolean riêng biệt trong ledger,
           không phải một cờ tổng hợp.
```

---

# 12. Sổ đăng ký chống suy luận sai (Anti-Pattern Guard Registry) `[G]`

Trích xuất từ mục **"Sai lầm thường gặp"** của KDK, chuyển thành bất biến có thể test.

| ID | Bất biến | Nguồn |
|---|---|---|
| `AP-001` | Không dùng một cây nến đóng ngoài làm bằng chứng chấp nhận | Ch 17 |
| `AP-002` | Không dùng một râu nến quay vào để kết luận tái chấp nhận | Ch 17 |
| `AP-003` | Không đặt ngưỡng cứng mà chưa kiểm chứng theo chế độ thị trường | Ch 17 |
| `AP-004` | Không gọi thất bại ngay khi giá quay đầu | Ch 18 |
| `AP-005` | Không gọi tiếp diễn ngay khi vừa phá vỡ với Delta lớn | Ch 18 |
| `AP-006` | Không bỏ qua trạng thái Unresolved vì muốn có giao dịch | Ch 18 |
| `AP-007` | Không gọi mọi phân kỳ Delta là hấp thụ | Ch 28 |
| `AP-008` | Không vào ngược ngay khi thấy khối lượng lớn | Ch 28 |
| `AP-009` | Không gọi khối lượng thấp là cạn kiệt trong mọi chế độ | Ch 29 |
| `AP-010` | Không bắt đáy chỉ vì Delta giảm | Ch 29 |
| `AP-011` | Không dùng một tỷ lệ Effort/Result duy nhất cho mọi chế độ | Ch 30 |
| `AP-012` | Không gọi Lớn–Nhỏ là đảo chiều chắc chắn | Ch 30 |
| `AP-013` | Không đồng nhất facilitation với Delta cùng dấu | Ch 31 |
| `AP-014` | Không dùng nhãn facilitation mà không xác định khung thời gian | Ch 31 |
| `AP-015` | Không gọi mọi cú phá đỉnh là quét thanh khoản | Ch 32 |
| `AP-016` | Không dùng câu chuyện "mắc kẹt" như nguyên nhân chắc chắn | Ch 32 |
| `AP-017` | Không đọc Delta trước khi biết hướng lần thử đấu giá | Ch 57 |
| `AP-018` | Không dùng Imbalance thay cho sự chấp nhận | Ch 57 |
| `AP-019` | Không bịa phía chủ động khi không phân loại được | Ch 57 |
| `AP-020` | Không dùng một tỷ lệ làm phán quyết | Ch 56 |
| `AP-021` | Không gán giá trị 0 cho dữ liệu không khả dụng | Ch 56 |
| `AP-022` | Không gọi tái nhập hình học là tái chấp nhận ổn định | Ch 56 |
| `AP-023` | Không dời vô hiệu khi giá đi ngược | Ch 59 |
| `AP-024` | Không dừng lỗ theo số tiền ngẫu nhiên | Ch 60 |
| `AP-025` | Không giữ lệnh sau khi luận điểm sai vì stop chưa chạm | Ch 60 |
| `AP-026` | Không gọi POC là "fair value" tuyệt đối | Ch 8, v1.2 §13.3 |
| `AP-027` | Không coi chạm VAH = bán, chạm VAL = mua | Ch 19 |
| `AP-028` | Không tăng khối lượng vì vài giao dịch Sớm thắng đẹp | Ch 63 |

## 12.1 Yêu cầu kiểm thử `[N]`

```text
G-AP-001  Mỗi AP-xxx PHẢI có ít nhất một test tự động trong
          tests/GC.AuctionFlow.Tests/Unit/Guards/AntiPatternGuardTests.cs
          chứng minh engine KHÔNG thể rơi vào mẫu sai đó.
G-AP-002  Test PHẢI kiểm tra hành vi (snapshot output), không chỉ
          kiểm tra sự tồn tại của chuỗi trong mã nguồn.
```

---

# 13. Ký ức mức giá và tái kiểm tra — bổ sung (KDK Ch 27)

**v1.2 §17.3 có `TestCount` nhưng không có ngữ nghĩa.** KDK Ch 27 cho đủ.

| Khái niệm | Định nghĩa | Trường `[M]` |
|---|---|---|
| Lần kiểm tra đầu tiên | Lần đầu giá tương tác với mốc kể từ khi mốc được xác lập | `FirstTestTimestamp`, `FirstTestOutcome` |
| Tái kiểm tra | Mọi lần tương tác sau đó | `RetestCount`, `RetestOutcomes[]` |
| Sự suy yếu của mốc | Mốc bị kiểm tra nhiều lần **không** tự động yếu đi | `ReferenceMaturity` |
| Tái bổ sung thanh khoản | Thanh khoản có thể được thêm lại giữa các lần kiểm tra | T3 — hiện BLOCKED |

```text
G-REF-001  KHÔNG có quy luật một chiều "mốc bị test nhiều lần thì yếu đi"
           hoặc "mạnh lên". (KDK Ch 27) Engine PHẢI ghi thô số lần test
           và kết quả từng lần, KHÔNG suy ra độ mạnh.
G-REF-002  Mọi kết luận về độ mạnh mốc là [C] — cần hiệu chỉnh thực nghiệm.
```

---

# 14. Sổ hiệu chỉnh (Calibration Ledger) `[C]`

**Đây là hợp đồng trung tâm của dự án.** Mọi trạng thái bị chặn phải xuất hiện ở đây với điều kiện mở khóa rõ ràng.

| Trạng thái bị chặn | Limitation string | Điều kiện mở khóa |
|---|---|---|
| `AcceptanceResolutionState.Established` | `ACCEPTANCE_ESTABLISHED_NOT_CALIBRATED` | Phân phối thực nghiệm của 6 nhóm bằng chứng §6.1, phân tầng theo regime, ≥ N mẫu định trước |
| `AcceptanceResolutionState.Failed` | `ACCEPTANCE_FAILED_NOT_CALIBRATED` | như trên |
| `ReentryResolutionState.StableReacceptance` | `STABLE_REACCEPTANCE_NOT_CALIBRATED` | Phân phối `TimeMaintainedInside`, `LocalPocResponse`, `LocalValueRebuildInside` |
| `ReentryResolutionState.ReentryFailed` | `REENTRY_FAILED_NOT_CALIBRATED` | như trên |
| `AuctionResolutionConclusion.FarCandidate` | `FAR_CONCLUSION_NOT_CALIBRATED` | Cần cả Acceptance + Reentry đã hiệu chỉnh |
| `AuctionResolutionConclusion.AacCandidate` | `AAC_CONCLUSION_NOT_CALIBRATED` | như trên |
| `EffortResultClassificationState.*` (100–106) | `EFFORT_RESULT_*_NOT_CALIBRATED` | Đường cơ sở Effort/Result theo regime §4.4 |
| `TradeFacilitationClassificationState.Healthy` | `TRADE_FACILITATION_HEALTHY_NOT_CALIBRATED` | Cả 4 thành phần §5.2 + phân tầng §5.4 |
| `TradeFacilitationClassificationState.Failing` | `TRADE_FACILITATION_FAILING_NOT_CALIBRATED` | như trên |
| `FarState` 100–105 | `FAR_CALIBRATED_STATES_NOT_AUTHORIZED` | 7 điều kiện §7.1 đo được + Signal Maturity hiệu chỉnh |
| `AacState` 100–102 | `AAC_CALIBRATED_STATES_NOT_AUTHORIZED` | 6 điều kiện §8.1 đo được + Signal Maturity hiệu chỉnh |
| `SignalMaturityLevel.*` | `SIGNAL_MATURITY_THRESHOLDS_NOT_CALIBRATED` | **Phase 3B — chưa tồn tại** |
| `OneTimeFramingState` ConfirmedUp/Down | `OTF_CONFIRMATION_NOT_CALIBRATED` | Đã ghi ở Phase 1D |
| `ThinParticipationLabel` production | `THIN_PARTICIPATION_NOT_CALIBRATED` | Phase 1G |

## 14.1 Giao thức hiệu chỉnh `[N]`

```text
G-CAL-001  Không trạng thái [C] nào được mở khóa bằng ngưỡng do người viết mã chọn.
G-CAL-002  Quy trình mở khóa bắt buộc:
             (1) Historical Scanner thu raw features
             (2) Phân phối thực nghiệm phân tầng theo
                 ReferenceType × ParticipationRegime × VolatilityRegime
             (3) Tiêu chuẩn mẫu định TRƯỚC (G-FAST-001)
             (4) Out-of-sample validation
             (5) Live Shadow
             (6) Ghi vào DECISION_LOG với hash dữ liệu nguồn
G-CAL-003  Mở khóa một trạng thái PHẢI bump schema version và ghi
           limitation string cũ vào danh sách "retired limitations".
```

---

# 15. Lộ trình v1.3 — việc cần làm tiếp

## 15.1 Đã hoàn thành (cập nhật 2026-07-27)

| # | Việc | Kết quả | Commit |
|---|---|---|---|
| 1 | Phase 3B — Signal Maturity (§9) | ✅ CODE/TEST PASS | `7dd6b7c` |
| 2 | Expected Behavior Contract (§9.3) | ✅ gộp vào 3B | `7dd6b7c` |
| 3 | Sửa `AacState` mapping (§8.2) | ✅ `ReentryDeveloping → Pullback` + NotCalibrated | `352bbdc` |
| 4 | Anti-Pattern Guard Tests (§12) | ✅ 22 test phủ AP-001..AP-028 | `352bbdc` |
| 5 | Vô hiệu năm chiều (§11.3) | ✅ Phase 3C | `b554adf` |
| 6 | Thesis Contract 7 trường + 5 khung (§11.1–11.2) | ✅ Phase 3C | `b554adf` |

> **Lưu ý:** mục 9 (Location gate) từng được xếp vào 3C nhưng **không** triển khai ở 3C —
> 3C chỉ nhận maturity làm đầu vào, chưa đọc `PriceValueLocation`. Vẫn còn nợ.

## 15.2 Việc còn lại

### Nhóm A — Bổ sung module đã có (nhỏ, độc lập, không đổi schema lớn)

| # | Việc | Phase | Vì sao cần |
|---|---|---|---|
| 7 | `OldValueReclaim` 3 trạng thái (§6.2 `G-ACC-005`) | **2G** | Đây là **trục quyết định FAR vs AAC** (KDK Ch 18). Hiện Resolution chưa có trường này ⇒ không phân biệt được "lấy lại và duy trì" với "lấy lại thất bại" |
| 8 | Facilitation thêm **Cấu trúc** + **Sự duy trì** (§5.2) | **2F-b** | 2F mới có 2/4 thành phần (Hoạt động, Tiến triển). `G-TF-002` đòi đủ 4 mới được phát Healthy/Failing |
| 9 | Location gate (§10, `G-LOC-001..003`) | **3D** | v1.2 §2.3 tuyên bố "Orderflow chỉ có nghĩa trong Context và Location" nhưng chưa module nào ép điều đó |

### Nhóm B — Module mới

| # | Việc | Phase | Phụ thuộc |
|---|---|---|---|
| 10 | PLAR / Target Engine — POC là **barrier** (§7.4 `G-FAR-006`) | **3E** | cần Location gate (9) |
| 11 | Imbalance classification (KDK Ch 25) | **2H** | Cluster raw đã có (2B) |
| 12 | Day Structure Classifier (KDK Ch 11) | **1H** | Profile đã LOCKED |
| 13 | Ký ức mức giá / retest ledger (§13) | **1I** | Reference đã LOCKED |
| 14 | Risk / Position Sizing (KDK Ch 62) | **4A** | cần Target Engine (10) |

### Nhóm C — Mở khóa

| # | Việc | Phase | Tác động |
|---|---|---|---|
| 15 | Historical Scanner + Calibration (§14.1) | **5A** | **Mở khóa toàn bộ trạng thái `[C]`** — đây là nút thắt duy nhất của cả dự án |

### Thứ tự đề xuất

```text
7 (2G)  →  8 (2F-b)  →  9 (3D)  →  10 (3E)  →  14 (4A)
                          ↘ 11 (2H), 12 (1H), 13 (1I) chạy song song được
                                    ↘ 15 (5A) mở khóa [C]
```

**Lý do xếp 7 trước:** `OldValueReclaim` là trường mà FAR (§7.1 điều kiện 4) và AAC
(§8.1 điều kiện 4) **đều** cần. Không có nó, cả hai họ chiến lược vĩnh viễn không thể
rời `NotCalibrated` dù Historical Scanner có chạy xong.

**Lưu ý về nợ kỹ thuật hiện tại:**

- Live acceptance đang pending cho **8 phase**: 1G, 2C, 2D, 2E, 2F, 3A, 3B, 3C.
  Không phase nào trong số này được gọi là FINAL PASS.
- `src/Oac.Core`, `src/Oac.Atas`, `tests/Oac.Core.Tests` vẫn untracked, ngoài phạm vi.
- Bản dual-install cũ tại `Documents\ATAS\Indicators\GC.AuctionFlow.dll`
  (2026-07-23) chưa được xử lý — vi phạm `D-P0-02A-003`.

## 15.3 Bất biến vận hành không đổi

```text
D-P0-02-002   Chỉ một artifact: GC.AuctionFlow.dll. KHÔNG BAO GIỜ build Oac.Core/Oac.Atas.
D-P0-02A-003  Deploy đúng một nơi: %APPDATA%\ATAS\Indicators\GC.AuctionFlow.dll
              KHÔNG dual-install sang Documents.
SHA-256       Xác minh source == deployed tại mỗi closeout.
LIVE_ONLY     Không tái dựng lịch sử từ candle / ATAS visual.
NOT_CALIBRATED Mọi trạng thái [C] luôn phát NotCalibrated + limitation string khớp.
```

---

# Phụ lục A. Năm quy tắc kinh nghiệm quanh vùng giá trị `[*]`

**Không phải định luật. Không tự tạo điểm vào.** Chúng tạo **câu hỏi và kỳ vọng có điều kiện**.

## A.1 Quy tắc 1 `[*]` — Tái chấp nhận vào vùng cân bằng

Khi giá được tái chấp nhận vào vùng cân bằng, **biên đối diện có thể** trở thành mục tiêu luân phiên.

Điều kiện cần xem:
- Tái chấp nhận thực sự, **không chỉ một bóng nến**
- Không có vùng giá trị mới phát triển bên ngoài
- POC và rào cản trung gian không tạo phản ứng phủ định
- Dòng lệnh sau tái nhập tạo được tiến triển
- Đường tới biên đối diện còn đủ khoảng trống

> Biên đối diện là mục tiêu **tiềm năng**, không phải bắt buộc.

## A.2 Quy tắc 2 `[*]` — Luân phiên bên trong cân bằng

Trong vùng cân bằng còn nguyên vẹn, giá có xu hướng luân phiên giữa các biên và quay lại trung tâm.

```text
AP-027  Chạm VAH ≠ bán. Chạm VAL ≠ mua.
G-ROT-001  Mỗi biên chỉ mở MỘT Episode để quan sát phản ứng.
G-ROT-002  Khi thời gian, khối lượng VÀ POC cục bộ bắt đầu xây ngoài biên
           → giả thuyết luân phiên SUY YẾU.
```

## A.3 Quy tắc 3 `[*]` — Chấp nhận ngoài cân bằng

Khi giá được chấp nhận ngoài vùng cân bằng → ưu tiên giả thuyết **khám phá giá** và tìm vùng giá trị mới.

Mục tiêu: mốc cấu trúc có ý nghĩa tiếp theo hoặc vùng từng được chấp nhận trên đường đi.

> POC cũ chỉ trở thành mục tiêu hợp lý **khi cuộc đấu giá quay lại vùng giá trị cũ**.

## A.4 Quy tắc 4 `[*]` — POC có thể ngắt đường luân phiên

Trong luận điểm luân phiên, POC là **rào cản trung gian**. Phản ứng mạnh tại POC, dòng lệnh đối diện tạo kết quả, hoặc vùng giá trị bắt đầu xây quanh POC → giảm khả năng đi hết tới biên đối diện.

→ Ánh xạ code: `G-FAR-006` — Target Engine phải mô hình POC như **barrier**, không chỉ như target.

## A.5 Quy tắc 5 `[*]` — Xây hoạt động tại biên

```text
Hoạt động lớn tại biên nhưng KHÔNG có tiến triển
  → có thể là hấp thụ HOẶC trạng thái chưa giải quyết

Hoạt động xây ngoài biên + POC/vùng giá trị dịch + duy trì ngoài
  → ứng viên chấp nhận
```

---

# Phụ lục B. Bảng thuật ngữ VI ↔ EN ↔ Code

| Tiếng Việt (KDK) | English (v1.2) | Định danh trong mã |
|---|---|---|
| Cuộc đấu giá | Auction | `AuctionProfileState` |
| Cân bằng | Balance / Equilibrium | `DirectionalAuctionState` |
| Khám phá giá | Price Discovery | `DirectionalAuctionState` |
| Vùng giá trị | Value Area | `ValueRelationship`, `PriceValueLocation` |
| Mốc tham chiếu | Structural Reference | `ReferenceType`, `ReferenceStatus` |
| Lần thử đấu giá ngoài vùng | Outside Auction Attempt | `EpisodeState.OutsideAttempt` |
| Sự chấp nhận | Acceptance | `AcceptanceObservationState`, `AcceptanceResolutionState` |
| Sự từ chối | Rejection | *(không có enum riêng — cố ý, xem §3.1)* |
| Tái nhập | Re-entry | `ReentryObservationState.GeometricReentry` |
| Tái chấp nhận | Reacceptance | `ReentryResolutionState.StableReacceptance` `[C]` |
| Kiểm tra lại | Retest | `RetestObservation` — **Phase 3B** |
| Cuộc đấu giá thất bại | Failed Auction | `FarState` |
| Tiếp diễn được chấp nhận | Accepted Continuation | `AacState` |
| Chưa được giải quyết | Unresolved | `AuctionResolutionConclusion.Unknown` |
| Nỗ lực | Effort | `AuctionEffortEvidenceVector` |
| Kết quả | Result | `AuctionResultEvidenceVector` |
| Khả năng tạo thuận lợi | Trade Facilitation | `TradeFacilitationClassificationState` |
| Ứng viên hấp thụ | Absorption candidate | `PotentialPassiveAbsorption` `[C]` |
| Ứng viên cạn kiệt | Exhaustion candidate | `PotentialExhaustion` `[C]` |
| Độ lệch | Reference Excursion | `EpisodeInteractionDirection` |
| Quét thanh khoản | Liquidity Sweep | *(BLOCKED — MBO)* |
| Ba mức trưởng thành | Signal Maturity | `SignalMaturityLevel` — **Phase 3B** |
| Sớm / Tiêu chuẩn / Xác nhận | Fast / Standard / Confirmed | như trên |
| Hành vi kỳ vọng | Expected Behavior | `ExpectedBehaviorContract` — **Phase 3B** |
| Vô hiệu | Invalidation | `ThesisInvalidationReason` — **Phase 3C** |
| Luận điểm | Thesis | `ThesisModuleState`, `ThesisDirection` |
| Chế độ tham gia | Participation Regime | `ThinParticipationLabel` |
| Ký ức mức giá | Price Memory | — **NOT STARTED** |

---

# Phụ lục C. Những gì v1.3 CỐ Ý không đưa vào

| Nội dung | Lý do |
|---|---|
| **Toàn bộ PHẦN V KDK (GEX, Ch 44–50)** | Operator chỉ định loại trừ ở giai đoạn này |
| Ch 76, 77, 79 (họ chiến lược dựa GEX) | Phụ thuộc GEX |
| DEX / Options Flow / Gamma | Phụ thuộc GEX |
| Bất kỳ ngưỡng số cụ thể nào | Vi phạm `G-CAL-001` |
| Tự động đặt lệnh | v1.2 §2.8 — baseline không tự động thực thi |
| Score / xác suất | v1.2 §2.5 — score không được giả danh xác suất |
| Nhãn tầng T4 (mắc kẹt, săn stop) trong Production | `G-EVID-004` |
| Đổi tên enum đã build | Sẽ phá vỡ 758 test hiện hữu |

---

# Phụ lục D. Nhật ký quyết định v1.3

| ID | Quyết định |
|---|---|
| `D-V13-001` | v1.3 là **companion spec**, không thay thế v1.2. Ưu tiên theo §0.2. |
| `D-V13-002` | GEX **OUT OF SCOPE**. Mọi trường GEX = `null`, mọi logic phải hoạt động đầy đủ khi vắng GEX. |
| `D-V13-003` | Ma trận 4 góc phần tư Effort/Result (§4.3) là **chuẩn tắc mới**, bổ sung cho danh sách 8 state phẳng của v1.2 §23.3. |
| `D-V13-004` | Vô hiệu có **năm** chiều, không phải bốn. Chiều "theo bằng chứng" là bổ sung v1.3. |
| `D-V13-005` | `ExpectedBehaviorContract` là **bắt buộc** cho mọi ThesisCandidate. |
| `D-V13-006` | 28 anti-pattern guard (§12) phải có test hành vi, không chỉ test chuỗi nguồn. |
| `D-V13-007` | `OldValueReclaim` phải là 3 trạng thái, không phải boolean. |
| `D-V13-008` | Phase 3A mapping `ReentryDeveloping → AacState.Invalidated` được đánh dấu **cần sửa** theo `G-DISC-002`. |
| `D-V13-009` | Không mở khóa bất kỳ trạng thái `[C]` nào trong v1.3. |
| `D-V13-010` | Phase kế tiếp: **3B Signal Maturity** với `SignalMaturityLevel` luôn phát `NotCalibrated`. |
