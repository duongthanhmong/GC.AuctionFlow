# KIM ĐẤU KINH

## Phương pháp Tam Trụ đọc và giao dịch thị trường vàng

### AMT + Order Flow + OPTIONS

**Phiên bản tái thiết trụ Options**  
GEX được giữ lại như một mô-đun exposure, không còn đại diện cho toàn bộ trụ quyền chọn.

> Một thị trường không chỉ có hướng. Nó có vị trí, trạng thái, nỗ lực, kết quả và môi trường. Phương pháp Tam Trụ ghép các mảnh ấy thành một câu chuyện có thể quan sát, kiểm tra, vô hiệu và quản trị.

## Mục tiêu

Sau khi học xong, người đọc phải làm được năm việc:

1. **Đọc cuộc đấu giá:** xác định thị trường đang cân bằng, khám phá giá hay chuyển tiếp.
2. **Đọc vị trí:** xác định vùng giá trị, mốc cấu trúc, biên, trung tâm và đường đi có ý nghĩa.
3. **Đọc hành vi thực thi:** phân biệt áp lực mua bán chủ động với kết quả giá thực tế.
4. **Đọc trụ Options:** đọc cấu trúc kỳ hạn, biến động hàm ý, dòng giao dịch, vị thế công khai và các exposure theo kịch bản; đồng thời biết rõ dữ liệu cho phép và không cho phép kết luận điều gì.
5. **Xây dựng kế hoạch:** hình thành luận điểm, điều kiện vào lệnh, vô hiệu, mục tiêu, quản trị và quyết định không giao dịch.

## Ba trụ của phương pháp

&#x20;   AMT
    → Bản đồ và phán quyết cuộc đấu giá:
      thị trường đang ở đâu, đang thử làm gì và mức giá mới có được chấp nhận hay không?

    ORDER FLOW
    → Lớp kiểm tra thực thi:
      bên nào đang chủ động và nỗ lực đó có tạo ra kết quả tương xứng hay không?

    OPTIONS
    → Lớp định giá rủi ro và áp lực tiềm tàng:
      thị trường quyền chọn đang định giá bao nhiêu biến động, rủi ro tập trung ở kỳ hạn/giá thực hiện nào,
      dòng giao dịch và vị thế công khai đang thay đổi ra sao, và cơ chế phòng hộ nào là ứng viên cần kiểm chứng?


Ba trụ không phải ba bộ chỉ báo cộng điểm và cũng không tranh cùng một quyền phán quyết. **AMT có quyền cao nhất đối với vị trí, trạng thái và sự chấp nhận đã hiện thực hóa trên giá.** **Order Flow** có quyền đối với nỗ lực thực thi đã khớp và kết quả giá. **Options** có quyền đối với điều thị trường quyền chọn đang định giá và những vùng nhạy cảm được suy ra có điều kiện. Options không còn là lớp trang trí cuối cùng, nhưng cũng không được dùng để phủ quyết sự chấp nhận rõ ràng của giá hoặc gán chắc chắn vị thế cho nhà tạo lập.



## Các khái niệm nền tảng cần biết

Phần này giải thích những khái niệm cốt lõi được sử dụng xuyên suốt tài liệu. Mục đích không phải ghi nhớ thật nhiều thuật ngữ, mà là hiểu mỗi khái niệm trả lời câu hỏi nào và không được phép kết luận điều gì từ nó.

### 1\. AMT là gì?

**AMT**, viết tắt của *Auction Market Theory*, là Lý thuyết thị trường đấu giá. AMT xem thị trường như một quá trình đấu giá hai chiều liên tục, trong đó giá di chuyển để tìm nơi người mua và người bán sẵn sàng giao dịch.

Khi hai phía cùng giao dịch thuận lợi trong một vùng, thị trường tạo **cân bằng**. Khi vùng cũ không còn thu hút đủ giao dịch hai chiều, giá rời đi để **khám phá giá**. Khi đến vùng mới, thị trường phải chứng minh vùng đó được chấp nhận, bị từ chối hay vẫn chưa được giải quyết.

AMT giúp trả lời:

&#x20;   Thị trường đang cân bằng, khám phá giá hay chuyển tiếp?
    Giá đang ở đâu trong cấu trúc?
    Mốc nào đang được kiểm tra?
    Giá mới có được duy trì hay không?
    Điều gì chứng minh luận điểm sai?


AMT không phải chỉ báo mua bán. Nó tạo bản đồ, bối cảnh và điều kiện để đánh giá hành vi giá.

### 2\. Cuộc đấu giá là gì?

**Cuộc đấu giá** là quá trình giá được chào, giao dịch được thực hiện và thị trường kiểm tra xem một vùng giá có thu hút đủ hoạt động hai chiều hay không.

Một cuộc đấu giá không được đánh giá bằng một cây nến đơn lẻ. Nó được đọc qua cả quá trình:

&#x20;   Tiếp cận mốc
    → tương tác
    → thử đi ra ngoài
    → được chấp nhận, tái nhập hoặc chưa được giải quyết





### 3\. Cân bằng, khám phá giá và chuyển tiếp

* **Cân bằng:** thị trường giao dịch hai chiều trong một vùng được chấp nhận. Giá thường luân phiên quanh trung tâm và kiểm tra hai biên.
* **Khám phá giá:** thị trường rời vùng thỏa thuận cũ để tìm vùng giao dịch mới. Dấu hiệu quan trọng là giá, POC và vùng giá trị có cùng dịch chuyển hay không.
* **Chuyển tiếp:** vùng trung gian khi cân bằng cũ đã suy yếu nhưng cân bằng mới chưa hình thành. Đây là trạng thái dễ xuất hiện bằng chứng xung đột và thường cần kiên nhẫn hơn.

### 4\. Giá và giá trị khác nhau thế nào?

**Giá** là mức đang được chào hoặc giao dịch tại một thời điểm. **Giá trị** là vùng mà thị trường đã tổ chức hoạt động và duy trì giao dịch trong một phạm vi thời gian xác định.

Giá có thể đi rất xa trong thời gian ngắn nhưng chưa chắc đã tạo giá trị mới. Chỉ khi thời gian, khối lượng, POC và hoạt động tiếp tục phát triển ở vùng mới, ta mới có thêm bằng chứng về sự chấp nhận.

Vùng giá trị không phải “giá thật” tuyệt đối của vàng. Nó chỉ mô tả vùng được chấp nhận tương đối trong hồ sơ và khoảng thời gian đang xét.



### 5\. Sự chấp nhận, từ chối, tái nhập và tái chấp nhận

* **Sự chấp nhận:** thị trường duy trì giao dịch và bắt đầu tổ chức hoạt động hoặc vùng giá trị tại vùng mới.
* **Sự từ chối:** giá thử một vùng rồi bị đẩy ra. Một phản ứng nhanh mới chỉ là dấu hiệu ban đầu, chưa đủ kết luận cuộc đấu giá thất bại.
* **Tái nhập:** giá quay về vùng cũ về mặt hình học.
* **Tái chấp nhận:** sau khi tái nhập, thị trường tiếp tục duy trì giao dịch và tổ chức lại hoạt động trong vùng cũ.

Một cây nến đóng ngoài không tự chứng minh sự chấp nhận. Một râu nến quay vào cũng không tự chứng minh tái chấp nhận.



### 6\. Episode là gì?

**Episode** là toàn bộ vòng đời tương tác giữa giá và một mốc tham chiếu. Episode bắt đầu khi giá thực sự tương tác với mốc và kết thúc khi cuộc đấu giá được giải quyết, mốc mất vai trò hoặc luận điểm hết thời hạn.

Episode giúp người giao dịch theo dõi một quá trình thay vì phán quyết từ một khoảnh khắc. Các thông tin chính gồm:

* thời điểm tương tác đầu tiên;
* số lần thử;
* độ lệch tối đa khỏi mốc;
* thời gian, khối lượng và số giao dịch ngoài vùng;
* tốc độ tái nhập;
* vị trí POC và vùng giá trị cục bộ;
* khả năng duy trì trong hoặc ngoài vùng.



### 7\. Market Profile và TPO là gì?

**Market Profile** tổ chức giá theo thời gian. **TPO**, viết tắt của *Time Price Opportunity*, ghi nhận việc giá đã xuất hiện trong từng khoảng thời gian.

Market Profile giúp nhìn:

* nơi thị trường dành nhiều hoặc ít thời gian;
* hình dạng phân phối;
* vùng giá trị;
* POC theo thời gian;
* Initial Balance;
* cấu trúc ngày và các vùng excess, single prints hoặc poor extreme.

TPO không đo chính xác số hợp đồng đã giao dịch và không cho biết chắc ai đang mua hoặc bán.

### 8\. Volume Profile là gì?

**Volume Profile** phân bổ khối lượng đã khớp theo từng mức giá. Nó cho thấy nơi hoạt động thực thi tập trung hoặc thưa thớt trong phạm vi được chọn.

Các mốc chính:

* **Volume POC:** mức có khối lượng lớn nhất.
* **VAH/VAL:** biên trên và biên dưới của vùng giá trị.
* **HVN:** vùng có khối lượng cao, thường từng có hoạt động hai chiều dày.
* **LVN:** vùng có khối lượng thấp, thường từng được đi qua nhanh hoặc ít tạo thỏa thuận.
* **nPOC:** POC chưa được kiểm tra lại.

Các mốc này là nơi quan sát, không phải nam châm hoặc hỗ trợ kháng cự bắt buộc.

### 9\. Order Flow là gì?

**Order Flow** là dữ liệu về các giao dịch đã thực sự khớp. Nó giúp đánh giá phía nào đang chủ động lấy thanh khoản, mức độ tham gia ra sao và nỗ lực đó có tạo tiến triển giá hay không.

Các công cụ thường dùng gồm:

&#x20;   Footprint
    Bid và Ask
    Delta
    CVD
    Tape hoặc Time \& Sales
    Khối lượng và số giao dịch


Order Flow không cho biết chắc danh tính, mục đích hoặc chất lượng thông tin của người tham gia. Nó chỉ có giá trị khi được đặt đúng vị trí và so sánh với kết quả giá.

### 10\. Lệnh thị trường, lệnh giới hạn, Bid và Ask

* **Lệnh giới hạn** cung cấp thanh khoản bằng cách chờ khớp tại một mức giá xác định.
* **Lệnh thị trường**, hoặc lệnh có khả năng khớp ngay, lấy thanh khoản đang có.
* **Bid** là giá mua chờ cao nhất.
* **Ask** là giá bán chờ thấp nhất.

Giao dịch khớp tại Ask thường được phân loại là mua chủ động. Giao dịch khớp tại Bid thường được phân loại là bán chủ động. Mỗi giao dịch luôn có cả người mua và người bán; khái niệm chủ động chỉ nói bên nào chấp nhận giá đang có để khớp ngay.

### 11\. Footprint là gì?

**Footprint** hiển thị khối lượng khớp tại Bid và Ask ở từng mức giá trong một thanh. Nó giúp quan sát chi tiết nơi dòng chủ động xuất hiện, nơi có mất cân bằng và mối quan hệ giữa khối lượng với tiến triển giá.

Footprint không hiển thị toàn bộ lệnh đang treo và không tự chứng minh hấp thụ, người giao dịch mắc kẹt hay đảo chiều.

### 12\. Delta và CVD là gì?

**Delta** là chênh lệch giữa khối lượng mua chủ động và khối lượng bán chủ động trong phạm vi đang xét.

&#x20;   Delta = khối lượng khớp tại Ask − khối lượng khớp tại Bid


**CVD** là Delta được cộng dồn theo thời gian. Delta và CVD đo áp lực chủ động, không đo vị thế ròng và không cho biết chắc người tham gia đang mở hay đóng vị thế.

Delta lớn nhưng giá không tiến triển có ý nghĩa khác với Delta lớn đi cùng sự mở rộng và duy trì của giá.

### 13\. Nỗ lực và Kết quả

**Nỗ lực** là mức độ hoạt động giao dịch, có thể quan sát qua khối lượng, số giao dịch, Delta, tốc độ hoặc dòng quét. **Kết quả** là mức tiến triển thực tế của giá và khả năng duy trì tiến triển đó.

Các trường hợp cơ bản:

* nỗ lực lớn, kết quả lớn: dòng chủ động đang tạo thuận lợi cho chuyển động;
* nỗ lực lớn, kết quả nhỏ: có thể xuất hiện đối ứng, hấp thụ hoặc trạng thái chưa được giải quyết;
* nỗ lực tương đối nhỏ, kết quả lớn: có thể phản ánh thanh khoản mỏng hoặc ít lực cản.

Nỗ lực–Kết quả phải được đánh giá tại đúng vị trí, không dùng như tín hiệu độc lập.

### 14\. Hấp thụ và cạn kiệt

* **Hấp thụ:** giả thuyết rằng dòng chủ động lớn gặp lực đối ứng đủ mạnh nên không tạo được tiến triển tương xứng.
* **Cạn kiệt:** giả thuyết rằng một phía giảm khả năng tiếp tục đẩy giá vì hoạt động chủ động suy yếu tại cực trị.

Cả hai đều là kết luận suy ra. Cần có phản ứng giá, tiến triển theo phía đối diện và khả năng duy trì trước khi dùng trong luận điểm.

### 15\. Imbalance là gì?

**Imbalance** là sự chênh lệch đáng kể giữa khối lượng chủ động ở hai phía tại các mức giá được so sánh. Nó giúp làm nổi bật nơi áp lực mua hoặc bán chủ động tập trung.

Một Imbalance không tự tạo tín hiệu. Chuỗi Imbalance chỉ có ý nghĩa khi tạo kết quả giá và phù hợp với vị trí cấu trúc.

### 16\. DOM, MBO và thanh khoản hiển thị

**DOM** là sổ lệnh tổng hợp theo mức giá. **MBO** là dữ liệu từng lệnh trong hàng đợi khi nguồn dữ liệu hỗ trợ.

Chúng giúp quan sát:

* thanh khoản đang treo gần giá;
* lệnh được thêm, rút hoặc di chuyển;
* vị trí và vòng đời của lệnh;
* ứng viên iceberg;
* dòng quét hoặc cụm dừng lỗ được kích hoạt.

Thanh khoản hiển thị có thể bị hủy. Vì vậy, sổ lệnh thể hiện ý định tạm thời, không phải cam kết chắc chắn.

### 17\. Open Interest và COT

**Open Interest**, viết tắt là OI, là số hợp đồng còn mở. OI khác với khối lượng giao dịch. OI tăng cho thấy tổng số hợp đồng mở tăng, nhưng không tự nói bên mua hay bên bán đang chiếm ưu thế.

**COT** là báo cáo vị thế theo nhóm người tham gia, thường có tần suất thấp. COT phù hợp với bối cảnh nhiều ngày hoặc nhiều tuần, không phù hợp để tạo điểm vào trong ngày.

Delta kết hợp với OI chỉ tạo bối cảnh về hoạt động và sự thay đổi số hợp đồng mở; nó không xác định chắc “người mua mới” hay “người bán mới”.

### 18\. Trụ Options là gì?

**Trụ Options** là lớp phân tích toàn bộ dữ liệu quyền chọn có thể quan sát và tính toán được, gồm hợp đồng, kỳ hạn, giá thực hiện, báo giá, giao dịch, khối lượng, Open Interest, biến động hàm ý, Greeks và các exposure theo kịch bản.

Trụ này trả lời năm nhóm câu hỏi:

1. **Thị trường đang định giá bao nhiêu biến động?**

   * IV tại ATM;
   * cấu trúc kỳ hạn;
   * skew và convexity;
   * expected move theo từng horizon.
2. **Rủi ro đang tập trung ở đâu?**

   * theo giá thực hiện;
   * theo ngày đáo hạn;
   * theo Call/Put;
   * theo Gamma, Delta, Vega và các độ nhạy khác.
3. **Hoạt động quyền chọn đang thay đổi thế nào?**

   * khối lượng;
   * Open Interest và thay đổi Open Interest;
   * premium/notional;
   * dòng giao dịch chủ động nếu nguồn cho phép phân loại.
4. **Cơ chế nào có thể tác động lên đường đi của giá?**

   * ứng viên ổn định hoặc luân phiên;
   * ứng viên khuếch đại;
   * áp lực gần đáo hạn;
   * rủi ro sự kiện và thay đổi biến động.
5. **Dữ liệu nào chỉ là mô hình?**

   * GEX, DEX và các exposure tổng hợp;
   * dealer positioning;
   * pinning, vanna flow, charm flow hoặc hedging pressure.

**GEX** vẫn được sử dụng, nhưng chỉ là một mô-đun bên trong trụ Options. Nó không còn đại diện cho toàn bộ quyền chọn. Một mức GEX, nhãn vendor như Call Resistance/Put Support hoặc Gamma Flip chỉ được dùng khi công thức, quy ước dấu, kỳ hạn và giả định vị thế được ghi rõ.

Trụ Options không tự xác nhận điểm mua, điểm bán hoặc hướng đi. Nó xây **kịch bản có điều kiện**, điều chỉnh kỳ vọng về biến động, đường đi, yêu cầu xác nhận và quản trị. Giá, Episode, sự chấp nhận và Order Flow cho biết kịch bản nào đang thực sự được kích hoạt.

### 19\. FAR và AAC

**FAR**, cuộc đấu giá thất bại và tái nhập, xuất hiện khi giá thử đi ra ngoài vùng cũ nhưng không xây được sự chấp nhận bền vững, sau đó tái nhập và duy trì trong vùng cũ.

**AAC**, cuộc đấu giá được chấp nhận và tiếp diễn, xuất hiện khi giá đi ra ngoài vùng cũ, xây hoạt động hoặc vùng giá trị mới và không thể tái chấp nhận bền vững vùng cũ.

&#x20;   Thử đi ra ngoài
    ├─ thất bại và tái nhập → FAR
    ├─ được chấp nhận và tiếp diễn → AAC
    └─ bằng chứng chưa đủ → chưa được giải quyết


FAR và AAC là hai họ luận điểm, không phải hai mẫu nến.

### 20\. Luận điểm, hành vi kỳ vọng và vô hiệu

**Luận điểm** là cách diễn đạt có điều kiện về điều thị trường cần làm tại một vị trí cụ thể. Một luận điểm hoàn chỉnh phải có:

* trạng thái và vị trí;
* mốc đang được kiểm tra;
* điều thị trường đang thử làm;
* bằng chứng đang có và còn thiếu;
* hành vi phải xuất hiện nếu luận điểm đúng;
* điều kiện làm luận điểm sai;
* mục tiêu và các rào cản trên đường đi.

**Vô hiệu** không chỉ là một mức dừng lỗ. Luận điểm có thể sai do giá, do sự chấp nhận của cuộc đấu giá, do hết thời gian, do bối cảnh thay đổi hoặc do dữ liệu không còn đủ tin cậy.

### 21\. Rủi ro và khối lượng vị thế

Rủi ro được xác định từ nơi luận điểm sai, không phải từ số tiền người giao dịch muốn kiếm. Trình tự đúng là:

&#x20;   Luận điểm
    → điểm vô hiệu
    → khoảng cách dừng lỗ
    → ngân sách rủi ro
    → khối lượng vị thế


Không chọn khối lượng vị thế trước rồi kéo dừng lỗ cho vừa. Chi phí, trượt giá, chênh lệch giá và rủi ro sự kiện phải được tính vào khả năng chịu lỗ thực tế.

### Mối quan hệ giữa các khái niệm

&#x20;   AMT
    → cho biết thị trường đang ở đâu và đang thử làm gì

    Market Profile và Volume Profile
    → mô tả nơi thị trường dành thời gian và khối lượng

    Episode và sự chấp nhận
    → cho biết cuộc thử giá thành công, thất bại hay chưa được giải quyết

    Order Flow
    → kiểm tra nỗ lực thực thi và kết quả giá

    Options, OI, COT và bối cảnh ngoài
    → mô tả định giá rủi ro, cấu trúc kỳ hạn và áp lực tiềm tàng; không thay quyền phán quyết của giá

    Luận điểm, vô hiệu và rủi ro
    → chuyển phân tích thành một quyết định có thể quản trị





## Tháp quyền hạn của bằng chứng

&#x20;   1. Tính toàn vẹn dữ liệu
    2. Vị trí cấu trúc
    3. Diễn biến cuộc đấu giá
    4. Sự chấp nhận hoặc tái chấp nhận của giá
    5. Dòng lệnh đã khớp
    6. Vi cấu trúc và thanh khoản hiển thị
    7. Cấu trúc Options, OI, COT và vĩ mô
    8. Kỹ thuật vào lệnh
    9. Câu chuyện, ẩn dụ và mẫu hình


Quy tắc quyền hạn:

* Tầng dưới không được phủ quyết tầng trên.
* Dòng lệnh mạnh không phủ quyết sự chấp nhận giá theo hướng ngược lại.
* Options không phủ quyết bằng chứng chấp nhận rõ ràng từ giá; xung đột làm giảm độ chắc chắn hoặc thay đổi quản trị theo quy tắc đã kiểm chứng.
* Một mẫu hình đẹp không cứu được vị trí sai.
* Một câu chuyện về nhà tạo lập không phủ quyết dữ liệu đã khớp.

## Quy ước dấu sao `\*`

> \*\*Dấu\*\* `\*` đánh dấu nội dung có giá trị sư phạm, kinh nghiệm hoặc nghiên cứu nhưng chưa được xem là quy luật phổ quát hay lợi thế đã xác nhận. Nội dung có dấu `\*` không được tự mình tạo điểm vào, tăng khối lượng vị thế, xác định hướng hoặc phủ quyết sự chấp nhận của giá.

Các nhãn có thể đi kèm:

* `\* \[Kinh nghiệm]`: quy tắc kinh nghiệm cần kiểm chứng.
* `\* \[Số liệu nguồn]`: con số từ nguồn nhưng chưa có bộ dữ liệu đầy đủ.
* `\* \[Phụ thuộc dữ liệu]`: kết quả phụ thuộc cách gom dữ liệu hoặc cấu hình.
* `\* \[Nhận định thời điểm]`: bình luận chỉ có giá trị trong bối cảnh lịch sử cụ thể.

Lộ trình nâng một giả thuyết thành quy tắc sử dụng:

&#x20;   Nguồn hoặc quan sát
    → Giả thuyết\*
    → Phát lại dữ liệu
    → Quan sát không thực thi
    → Giao dịch mô phỏng hoặc quy mô rất nhỏ
    → Đánh giá ngoài mẫu
    → Chấp nhận, sửa đổi hoặc loại bỏ


## Nguyên tắc xuyên suốt

> Không giao dịch một tín hiệu. Giao dịch một câu chuyện đấu giá có bằng chứng, có điều kiện sai, có không gian mục tiêu và có dữ liệu đủ tin cậy.

## Bảng tra nhanh thuật ngữ

* **AMT:** Auction Market Theory, Lý thuyết thị trường đấu giá.
* **Order Flow:** dòng lệnh đã khớp.
* **Options:** trụ dữ liệu quyền chọn gồm cấu trúc biến động, hoạt động, vị thế công khai và exposure theo kịch bản.
* **GEX:** Gamma Exposure, một mô-đun exposure bên trong trụ Options.
* **TPO:** Time Price Opportunity, cơ hội giá theo thời gian.
* **POC:** Point of Control, mức hoạt động lớn nhất theo thước đo đang dùng.
* **VAH/VAL:** biên trên và biên dưới vùng giá trị.
* **CVD:** Cumulative Volume Delta, chênh lệch khối lượng chủ động tích lũy.
* **DOM:** Depth of Market, sổ lệnh theo mức giá.
* **MBO:** Market By Order, dữ liệu từng lệnh trong hàng đợi.
* **OI:** Open Interest, số hợp đồng còn mở.
* **COT:** Commitments of Traders, báo cáo vị thế theo nhóm.
* **FAR:** Failed Auction Re-entry, cuộc đấu giá thất bại và tái nhập vùng cũ.
* **AAC:** Accepted Auction Continuation, cuộc đấu giá được chấp nhận và tiếp diễn.
* **MFE/MAE:** mức thuận lợi tối đa và mức bất lợi tối đa sau khi vào lệnh.

# MỤC LỤC

## PHẦN I — NỀN TẢNG TƯ DUY THỊ TRƯỜNG

* Chương 1. Thị trường là một cuộc đấu giá
* Chương 2. Hai trạng thái cốt lõi: Cân bằng và Khám phá giá
* Chương 3. Giá trị, thời gian và khối lượng
* Chương 4. Người tham gia và khung thời gian
* Chương 5. Dữ liệu, bằng chứng và kết luận
* Chương 6. Cấu trúc thị trường, vùng mốc và kế hoạch điều kiện

## PHẦN II — LÝ THUYẾT THỊ TRƯỜNG ĐẤU GIÁ

* Chương 7. Market Profile và TPO
* Chương 8. Volume Profile
* Chương 9. Vùng giá trị và POC
* Chương 10. Initial Balance
* Chương 11. Hình dạng Profile và cấu trúc ngày
* Chương 12. Hệ thống mốc tham chiếu
* Chương 13. Composite Profile và cấu trúc nhiều phiên
* Chương 14. Dịch chuyển giá trị và bối cảnh định hướng
* Chương 15. One-Time Framing: nhịp cực trị một chiều
* Chương 16. Auction Episode: vòng đời tương tác với một mốc
* Chương 17. Sự chấp nhận, từ chối và tái nhập
* Chương 18. Cuộc đấu giá thất bại và cuộc đấu giá tiếp diễn
* Chương 19. Năm quy tắc kinh nghiệm quanh vùng giá trị\*

## PHẦN III — DÒNG LỆNH ĐÃ KHỚP VÀ VI CẤU TRÚC THỊ TRƯỜNG

* Chương 20. Cơ chế thực thi lệnh
* Chương 21. Bid, Ask và phân loại phía chủ động
* Chương 22. Delta
* Chương 23. Cumulative Volume Delta
* Chương 24. Biểu đồ Footprint
* Chương 25. Imbalance
* Chương 26. Khối lượng, số giao dịch và kích thước giao dịch trung bình
* Chương 27. Lần kiểm tra, tái kiểm tra và ký ức mức giá
* Chương 28. Ứng viên hấp thụ
* Chương 29. Ứng viên cạn kiệt
* Chương 30. Nỗ lực và Kết quả
* Chương 31. Khả năng tạo thuận lợi cho giao dịch
* Chương 32. Quét thanh khoản, kích hoạt dừng lỗ và người giao dịch mắc kẹt
* Chương 33. DOM và vòng đời thanh khoản hiển thị
* Chương 34. Pulling và Stacking: thanh khoản bị rút và được thêm
* Chương 35. Iceberg và các công cụ phát hiện lệnh ẩn
* Chương 36. Nhận diện dừng lỗ và lệnh quét
* Chương 37. Các chỉ báo tổng hợp về áp lực và mức động
* Chương 38. Limit Tracing: giả thuyết nghiên cứu về chuỗi thanh khoản
* Chương 39. Cực trị thanh chưa hoàn tất và cực trị kém hoàn thiện\*

## PHẦN IV — HỢP ĐỒNG, PHIÊN, SỰ KIỆN VÀ VỊ THẾ

* Chương 40. Hợp đồng, chuyển tháng và chế độ tham gia
* Chương 41. Sự kiện, vĩ mô và thị trường bên ngoài
* Chương 42. Open Interest và giới hạn suy luận vị thế
* Chương 43. COT, nhà tạo lập và rủi ro tồn kho

## PHẦN V — TRỤ OPTIONS: ĐỊNH GIÁ RỦI RO, DÒNG GIAO DỊCH VÀ EXPOSURE

* Chương 44. Vai trò, quyền hạn và kiến trúc của trụ Options
* Chương 45. Dữ liệu Options, ánh xạ hợp đồng và kiểm soát chất lượng
* Chương 46. Kỳ hạn, moneyness và cấu trúc biến động hàm ý
* Chương 47. Volume, Open Interest và Options Flow
* Chương 48. Greeks, GEX và exposure theo kịch bản
* Chương 49. Expected move, vùng nhạy cảm và Options Regime
* Chương 50. Tích hợp Options với AMT, Order Flow và quản trị

## PHẦN VI — HỢP NHẤT TAM TRỤ

* Chương 51. Vai trò của ba trụ
* Chương 52. Quy trình đọc thị trường từ trên xuống
* Chương 53. Vị trí trước tín hiệu
* Chương 54. Bối cảnh trước thực thi
* Chương 55. Từ mốc tham chiếu đến Episode
* Chương 56. Từ Episode đến bằng chứng chấp nhận
* Chương 57. Từ bằng chứng đến Order Flow
* Chương 58. Tích hợp Options Intelligence vào câu chuyện đấu giá
* Chương 59. Xây dựng luận điểm thị trường
* Chương 60. Vô hiệu đa chiều
* Chương 61. Mục tiêu và đường đi ít cản trở
* Chương 62. Quản trị rủi ro

## PHẦN VII — CÁC HỌ CHIẾN LƯỢC VÀ LỐI VÀO LỆNH

* Chương 63. Ba kết quả của một lần thử đấu giá ngoài vùng
* Chương 64. FAR: Failed Auction Re-entry
* Chương 65. AAC: Accepted Auction Continuation
* Chương 66. Luân phiên trong vùng giá trị
* Chương 67. Luân phiên quanh đường trung tâm
* Chương 68. Giao dịch đáp ứng tại biên
* Chương 69. Phá vỡ chủ động
* Chương 70. Hồi về vùng giá trị mới
* Chương 71. FAR hai lần thử
* Chương 72. Nỗ lực lớn nhưng tiến triển hạn chế
* Chương 73. Kết quả lớn với nỗ lực tương đối thấp
* Chương 74. Từ chối tại biên Composite
* Chương 75. Phá Composite và xây vùng giá trị mới
* Chương 76. Ứng viên luân phiên trong chế độ Options ổn định
* Chương 77. Ứng viên khuếch đại trong chế độ Options bất ổn
* Chương 78. AMT và Order Flow đồng thuận, Options hỗ trợ hoặc trung tính
* Chương 79. AMT và Order Flow đồng thuận nhưng Options xung đột
* Chương 80. Order Flow mạnh tại vị trí sai
* Chương 81. Không giao dịch

## PHẦN VIII — QUY TRÌNH GIAO DỊCH

* Chương 82. Phân tách thị trường phân tích và thị trường thực thi
* Chương 83. Chuẩn bị trước phiên
* Chương 84. Quy trình đọc thị trường trong 90 giây
* Chương 85. Quy trình ra quyết định
* Chương 86. Quản lý giao dịch
* Chương 87. Nhật ký giao dịch
* Chương 88. Đánh giá lại và phát triển phương pháp

## PHẦN IX — NGHIÊN CỨU, KIỂM CHỨNG VÀ THƯ VIỆN TÌNH HUỐNG

* Chương 89. Chuẩn hóa dữ liệu và cấu hình quan sát
* Chương 90. Đo lường lợi thế và độ bất định
* Chương 91. Kiểm định độ bền và rủi ro chuỗi thua
* Chương 92. Xây dựng thư viện tình huống

## PHẦN X — GIẢNG DẠY PHƯƠNG PHÁP

* Chương 93. Lộ trình đào tạo bốn cấp
* Chương 94. Phương pháp giảng một bài
* Phụ lục A–O
* Kết luận

# PHẦN I — NỀN TẢNG TƯ DUY THỊ TRƯỜNG

## Chương 1. Thị trường là một cuộc đấu giá

Chương này đặt lại nền móng tư duy. Thị trường không phải chiếc máy tạo nến, mà là một quá trình liên tục chào giá, khớp giao dịch, kiểm tra mức giá mới và quay lại những nơi hoạt động hai chiều thuận lợi.

### Nội dung cốt lõi

#### Giá là lời mời, giao dịch là sự đồng ý

Một mức giá xuất hiện trên màn hình chỉ là lời mời. Giá trị thông tin tăng lên khi tại đó có giao dịch, có thời gian lưu lại, có khối lượng và có khả năng duy trì. Vì vậy, một cú chạm thoáng qua không có cùng ý nghĩa với một vùng được giao dịch liên tục.

#### Cuộc đấu giá hai chiều

Người mua cạnh tranh để nâng giá khi họ chấp nhận trả cao hơn; người bán cạnh tranh để hạ giá khi họ chấp nhận bán thấp hơn. Khi cả hai phía giao dịch tích cực trong một vùng, thị trường đang tạo thỏa thuận tạm thời. Khi một phía buộc phải đuổi giá, thị trường chuyển sang khám phá.

#### Giá không phải giá trị

Giá là một điểm; giá trị là vùng hoạt động được duy trì. Giá có thể rời vùng giá trị do tin tức, thanh khoản mỏng hoặc một đợt lệnh lớn, nhưng chỉ khi thị trường tiếp tục giao dịch và tổ chức hoạt động ở vùng mới thì ta mới có bằng chứng chấp nhận.

#### Nhiệm vụ của người giao dịch

Người giao dịch không cần đoán mọi bước đi. Nhiệm vụ là nhận ra thị trường đang tìm kiếm gì, mức nào đang được thử, cuộc thử đó thành công hay thất bại, và rủi ro nào xuất hiện nếu giả thuyết sai.

### Quy trình áp dụng

1. Xác định vùng đang có hoạt động hai chiều.
2. Đánh dấu điểm giá bắt đầu rời vùng đó.
3. Quan sát xem thị trường có duy trì hoạt động ở vùng mới hay nhanh chóng quay lại.
4. Chỉ sau đó mới xem xét hướng giao dịch.

### Sai lầm thường gặp

* Gọi mọi cú phá đỉnh là mua chủ động thành công.
* Đồng nhất một cây nến lớn với sự chấp nhận.
* Kể câu chuyện về “cá mập” khi chỉ có dữ liệu giá.

### Ghi nhớ

> Giá trả lời câu hỏi “đang ở đâu”; cuộc đấu giá trả lời câu hỏi “mức giá đó có được duy trì hay không”.

## Chương 2. Hai trạng thái cốt lõi: Cân bằng và Khám phá giá

Phần lớn diễn biến thị trường có thể được hiểu như sự luân phiên giữa cân bằng, khám phá giá và giai đoạn chuyển tiếp. Đây là ngữ pháp cơ bản của AMT.



### Nội dung cốt lõi

#### Cân bằng

Cân bằng xuất hiện khi thị trường tìm được vùng mà người mua và người bán đều sẵn sàng giao dịch. Giá thường quay lại trung tâm, biên độ mở rộng chậm và các lần phá biên dễ bị kiểm tra lại.

#### Khám phá giá

Khám phá giá xuất hiện khi vùng cũ không còn thu hút đủ giao dịch hai chiều. Giá phải di chuyển để tìm đối tác mới. Dấu hiệu quan trọng không chỉ là tốc độ, mà còn là khả năng xây hoạt động và giá trị theo hướng di chuyển.

#### Chuyển tiếp

Chuyển tiếp là đoạn thị trường chưa còn cân bằng cũ nhưng cũng chưa xây cân bằng mới. Đây là vùng dễ xuất hiện tín hiệu mâu thuẫn: giá đi xa nhưng POC chưa dịch, Delta lớn nhưng tiến triển yếu, hoặc nhiều lần tái nhập vùng cũ.

#### Hoạt động chủ động và đáp ứng

Hoạt động chủ động chấp nhận giao dịch ngoài vùng quen thuộc để tìm giá mới. Hoạt động đáp ứng xuất hiện khi người tham gia cho rằng giá đã đi quá xa so với vùng thỏa thuận hiện tại và giao dịch hướng về bên trong.

### Hoạt động đáp ứng và hoạt động khởi xướng

AMT truyền thống phân biệt hai chức năng:

|Thuật ngữ|Cách hiểu trong phương pháp|
|-|-|
|Responsive Buying|Mua đáp ứng dưới vùng giá trị đã được chấp nhận; có thể hỗ trợ ứng viên FAR mua hoặc nhịp luân phiên|
|Responsive Selling|Bán đáp ứng trên vùng giá trị đã được chấp nhận; có thể hỗ trợ ứng viên FAR bán hoặc nhịp luân phiên|
|Initiating Buying|Mua khởi xướng tạo và duy trì sự chấp nhận phía trên vùng giá trị; có thể hỗ trợ ứng viên AAC mua|
|Initiating Selling|Bán khởi xướng tạo và duy trì sự chấp nhận phía dưới vùng giá trị; có thể hỗ trợ ứng viên AAC bán|

Đây là **mô hình chức năng**, không phải danh tính người tham gia. Không gọi hoạt động đáp ứng chỉ vì giá trông “đắt” hoặc “rẻ”. Không gọi hoạt động khởi xướng chỉ vì có phá biên hoặc Delta lớn. Phải xem Episode, tiến triển giá, sự chấp nhận và khả năng duy trì.

### Quy trình áp dụng

1. Xác định vùng giá trị gần nhất.
2. Quan sát giá đang ở trong, tại biên hay ngoài vùng.
3. Kiểm tra POC và giá trị có dịch theo giá hay không.
4. Phân loại: cân bằng, khám phá hoặc chuyển tiếp.
5. Chỉ chọn lối giao dịch phù hợp với trạng thái.

### Sai lầm thường gặp

* Giao dịch ngược mọi cú phá biên chỉ vì thị trường trước đó cân bằng.
* Mua đuổi mọi cú phá biên mà chưa có giá trị mới.
* Ép thị trường vào hai nhãn tăng hoặc giảm khi thực tế đang chuyển tiếp.

### Ghi nhớ

> Cân bằng ưu tiên hồi quy và luân phiên; khám phá ưu tiên tiếp diễn; chuyển tiếp ưu tiên chờ bằng chứng.

## Chương 3. Giá trị, thời gian và khối lượng

AMT dùng thời gian và khối lượng để mô tả nơi thị trường đã dành sự chú ý. Chúng không phải hai phiên bản của cùng một dữ liệu; chúng trả lời hai câu hỏi khác nhau.

### Nội dung cốt lõi

#### Thời gian tại giá

TPO ghi nhận cơ hội thời gian mà thị trường đã có tại từng mức giá. Nhiều TPO cho thấy giá được lặp lại qua nhiều khoảng thời gian, nhưng không cho biết chính xác bao nhiêu hợp đồng đã khớp.

#### Khối lượng tại giá

Hồ sơ khối lượng cộng số hợp đồng đã khớp tại từng giá. Nó mô tả hoạt động thực thi, nhưng không tự cho biết hoạt động đó thuộc một người hay nhiều người, là mở vị thế hay đóng vị thế.

#### Đồng thuận

Khi TPO và khối lượng cùng tập trung ở một vùng, ta có bằng chứng mạnh hơn rằng thị trường đã tổ chức hoạt động tại đó. Đây là vùng thỏa thuận thanh khoản, không phải “giá trị thật” tuyệt đối.

#### Bất đồng

Giá có thể dành nhiều thời gian nhưng khối lượng thấp, hoặc khối lượng bùng nổ trong thời gian rất ngắn. Bất đồng này thường tiết lộ chất lượng tham gia: giao dịch chậm và đều khác với một cú xả lệnh dồn dập.

### Quy trình áp dụng

1. Đặt TPO Profile và Volume Profile cạnh nhau.
2. So sánh POC, vùng giá trị và các nút lớn.
3. Ghi nhận vùng đồng thuận và vùng bất đồng.
4. Quan sát kết quả khi giá quay lại các vùng đó.

### Sai lầm thường gặp

* Gọi mọi vùng khối lượng cao là hỗ trợ.
* Dùng TPO như thước đo tiền.
* Dùng khối lượng như bằng chứng chắc chắn về ý định.

### Ghi nhớ

> Thời gian mô tả sự lặp lại; khối lượng mô tả mức độ thực thi; giá trị là kết luận bối cảnh, không phải một đường kẻ thần kỳ.

## Chương 4. Người tham gia và khung thời gian

Cùng một giá có thể là điểm chốt lời của người giao dịch trong ngày, điểm vào của người giữ nhiều ngày và nơi phòng hộ của doanh nghiệp. Phương pháp không cố đọc danh tính; nó đọc dấu vết mà các khung thời gian để lại.

### Nội dung cốt lõi

#### Khung ngắn hạn

Người giao dịch ngắn hạn thường phản ứng với thanh khoản gần, tốc độ và cấu trúc vi mô. Họ có thể tạo nhiều giao dịch nhưng không nhất thiết tạo giá trị mới.

#### Khung dài hơn

Người giữ vị thế nhiều ngày có khả năng chấp nhận giá xa vùng hiện tại lâu hơn. Dấu hiệu phù hợp có thể là giá trị dịch chuyển, nhiều phiên không quay lại vùng cũ và các đợt hồi nông.

#### Hoạt động phòng hộ và nhà tạo lập

Hoạt động phòng hộ giảm rủi ro kinh tế; nhà tạo lập quản trị sổ vị thế và thanh khoản. Dòng giao dịch của họ có thể không xuất phát từ quan điểm tăng giảm đơn giản.

#### Xung đột khung thời gian

Một cấu trúc ngày có thể đang tăng trong khi cấu trúc nhiều ngày còn cân bằng hoặc giảm. Trạng thái xung đột không phải lỗi dữ liệu; nó là thông tin cho biết kỳ vọng và mục tiêu cần thận trọng.

### Quy trình áp dụng

1. Tách bối cảnh nhiều phiên khỏi bối cảnh trong ngày.
2. Ghi rõ khung nào đang khám phá và khung nào đang cân bằng.
3. Không gán danh tính cho dòng lệnh.
4. Điều chỉnh thời gian giữ lệnh theo khung tạo luận điểm.

### Sai lầm thường gặp

* Dùng một lệnh lớn để kết luận “tổ chức vào hàng”.
* Dùng xu hướng khung nhỏ để phủ nhận biên lớn.
* Trộn luận điểm nhiều ngày với điểm vô hiệu vài nhịp giá.

### Ghi nhớ

> Khung thời gian quyết định ý nghĩa của dữ liệu và thời hạn sống của luận điểm.

## Chương 5. Dữ liệu, bằng chứng và kết luận

Một phương pháp có thể đúng về ý tưởng nhưng thất bại vì ngôn ngữ quá chắc chắn. Chương này xây hàng rào giữa điều quan sát được, điều tính được và điều chỉ là giả thuyết.



### Nội dung cốt lõi

#### Ba tầng thông tin

**Quan sát trực tiếp** gồm giá, thời gian, khối lượng, báo giá và giao dịch. **Dữ liệu suy ra** gồm Delta, POC, vùng giá trị, IV, Greeks, expected move và các exposure do mô hình tính. **Mô hình suy luận** gồm hấp thụ, người mua mắc kẹt, dealer hedging, ghim giá hoặc khuếch đại. Mỗi tầng cần cách nói khác nhau; đặc biệt dữ liệu Options phải tách rõ trường quan sát, trường do sàn công bố, trường do mô hình tính và giả định vị thế.

#### Bằng chứng không phải dự đoán

Một tập bằng chứng chỉ làm một kịch bản hợp lý hơn hoặc yếu đi. Nó không biến thị trường thành chắc chắn. Người giao dịch giỏi luôn giữ một điều kiện vô hiệu rõ ràng.

#### Nhìn lại sau khi biết kết quả

Sau khi biết kết quả, não có xu hướng nhìn lại và cho rằng tín hiệu trước đó rất rõ. Cách chống lại là lưu ảnh và nhận định tại đúng thời điểm, không sửa câu chuyện sau khi giá đã chạy.

#### Ngôn ngữ kỷ luật

Dùng “phù hợp với giả thuyết”, “bằng chứng đang phát triển”, “chưa đủ xác nhận” thay cho “chắc chắn”, “cá mập”, “bị thao túng”. Ngôn ngữ chính xác giúp hành vi giao dịch chính xác.

### Quy trình áp dụng

1. Gắn nhãn cho từng thông tin: quan sát, tính toán hoặc suy luận.
2. Viết điều đang biết, chưa biết, đang chờ và điều làm giả thuyết sai.
3. Chỉ chuyển từ quan sát sang luận điểm khi có chuỗi bằng chứng.
4. Lưu lại nhận định trước khi biết kết quả.

### Sai lầm thường gặp

* Biến tương quan thành nhân quả.
* Dùng từ tuyệt đối để che sự thiếu chắc chắn.
* Chọn lọc dữ liệu chỉ vì nó ủng hộ lệnh đang muốn vào.

### Ghi nhớ

> Người giao dịch chuyên nghiệp không chỉ biết điều gì đang xảy ra; họ còn biết mình chưa có quyền kết luận điều gì.

## Chương 6. Cấu trúc thị trường, vùng mốc và kế hoạch điều kiện

Các công cụ chỉ có giá trị khi chúng được đặt lên một cấu trúc đã xác định. Hồ sơ, Delta, iceberg, IV hay bất kỳ exposure Options nào cũng không thể cứu một luận điểm sinh ra từ vị trí sai.



### Cấu trúc đứng trước chỉ báo

Thứ tự làm việc:

&#x20;   Trạng thái cuộc đấu giá
    → Cấu trúc khung thời gian lớn
    → Vùng mốc cấu trúc
    → Luận điểm
    → Hành vi được kỳ vọng
    → Dòng lệnh kiểm chứng
    → Options và bối cảnh ngoài
    → Order Flow kiểm chứng tại điểm tương tác
    → Thực thi và rủi ro


Luận điểm không bắt đầu từ phân kỳ Delta hoặc một dấu iceberg. Luận điểm bắt đầu từ câu hỏi: **giá đang ở đâu và bên nào cần chứng minh điều gì tại vùng này?**

### Mốc là vùng, không phải một con số thần chú

Một mốc cấu trúc nên được hiểu như một khoảng giá có ý nghĩa. Độ rộng của vùng phụ thuộc kích thước tick, biến động, phiên giao dịch, loại mốc và độ lệch giữa hợp đồng tương lai vàng (GC) với sản phẩm CFD dùng để thực thi.

Không gượng vẽ mốc vì đang nóng lòng tìm lệnh. Một vùng chỉ đáng giữ khi nguồn gốc của nó rõ ràng và vai trò của nó có thể mô tả được.

### Hành vi được kỳ vọng

Một mốc chỉ có giá trị giao dịch khi thị trường thể hiện hành vi phù hợp với vai trò được gán:

* Vùng hỗ trợ không bắt buộc bật hình chữ V, nhưng phải làm giảm khả năng tiếp diễn xuống hoặc tạo phản ứng có kết quả.
* Nếu giá xây vùng giá trị và duy trì bên dưới vùng hỗ trợ, luận điểm mua suy yếu.
* Nếu giá chỉ giằng co và chưa xây được sự chấp nhận theo phía nào, trạng thái đúng là chưa được giải quyết.

### Kế hoạch điều kiện

Không cố đoán người tham gia lớn đang nghĩ gì. Chuẩn bị cây quyết định:

&#x20;   Giá tiếp cận vùng mốc
    ├─ Tái nhập và duy trì vùng cũ
    │  └─ Xem xét FAR
    ├─ Xây hoạt động và duy trì ngoài vùng
    │  └─ Xem xét AAC
    └─ Bằng chứng xung đột hoặc thiếu
       └─ Chưa giải quyết / Không giao dịch


### Mẫu hình chỉ là hình thức biểu hiện

SFP, Quasimodo, tam giác, hình chữ nhật hoặc phá vỡ rồi kiểm tra lại chỉ là hình học. Chúng chỉ được đưa vào luận điểm khi có vị trí cấu trúc, cuộc thử giá, bằng chứng thất bại hoặc chấp nhận, điều kiện vô hiệu và không gian mục tiêu.

### Ghi nhớ

> Cấu trúc cho biết đứng ở đâu. Luận điểm cho biết điều gì phải xảy ra. Dòng lệnh cho biết điều đó có đang xảy ra hay không.

# PHẦN II — LÝ THUYẾT THỊ TRƯỜNG ĐẤU GIÁ

## Chương 7. Market Profile và TPO

Market Profile tổ chức diễn biến giá theo thời gian để làm lộ cấu trúc của cuộc đấu giá. Nó không dự báo; nó giúp nhìn thấy nơi thị trường dành thời gian và cách phạm vi phát triển.



### Nội dung cốt lõi

#### Cấu tạo TPO

Mỗi ký tự hoặc khối TPO đại diện cho một khoảng thời gian mà giá đã giao dịch. Các TPO xếp theo mức giá tạo thành hình phân phối. Khu vực rộng thể hiện sự lặp lại; khu vực mỏng thể hiện sự đi qua nhanh.

#### POC và vùng giá trị

TPO POC là giá có nhiều TPO nhất theo quy tắc lựa chọn đã dùng. Vùng giá trị thường bao phủ khoảng 70% TPO quanh POC, nhưng tỷ lệ này là quy ước mô tả, không phải ngưỡng có lợi thế tự nhiên.

#### Single Prints và Excess

Single Prints là vùng chỉ có một TPO trong cấu trúc, thường phản ánh di chuyển nhanh. Excess là phần đuôi cho thấy cuộc đấu giá đã thử giá và phản ứng mạnh. Cả hai cần được đánh giá trong bối cảnh, không phải tự động là hỗ trợ kháng cự.

#### Đỉnh và đáy kém hoàn thiện

Đỉnh hoặc đáy phẳng với nhiều TPO tương tự có thể cho thấy cuộc đấu giá chưa tạo Excess rõ. Đây là dấu hiệu cấu trúc chưa hoàn chỉnh, nhưng không bảo đảm thị trường sẽ quay lại ngay.

### Quy trình áp dụng

1. Xác định TPO POC, VAH, VAL.
2. Đánh dấu Single Prints, Excess, đỉnh kém hoàn thiện và đáy kém hoàn thiện.
3. Quan sát vị trí mở cửa và cách phạm vi mở rộng.
4. So sánh cấu trúc đang phát triển với phiên hoàn tất.

### Sai lầm thường gặp

* Giao dịch mọi Single Print như khoảng trống phải lấp.
* Coi đỉnh kém hoàn thiện là mục tiêu bắt buộc.
* Quên rằng hình dạng đang phát triển có thể thay đổi trước khi phiên kết thúc.

### Ghi nhớ

> TPO kể câu chuyện về thời gian và cấu trúc, không kể chắc chắn ai đang mua hay bán.

## Chương 8. Volume Profile

Hồ sơ khối lượng phân bổ số hợp đồng đã khớp theo mức giá. Nó là bản đồ hoạt động thực thi và thường bổ sung cho TPO.



### Nội dung cốt lõi

#### Volume POC

Volume POC là mức giá có khối lượng lớn nhất trong phạm vi phân tích. Nó cho thấy nơi hoạt động đã tập trung, không chứng minh đó là giá công bằng hay nam châm bắt buộc.

#### HVN và LVN

Nút khối lượng cao (HVN) thường phản ánh khu vực giao dịch hai chiều dày. Nút khối lượng thấp (LVN) thường phản ánh vùng thị trường đi qua nhanh hoặc ít thỏa thuận. Giá có thể luân phiên trong HVN và di chuyển nhanh qua LVN, nhưng kết quả phụ thuộc trạng thái hiện tại.

#### Hình phân phối

Phân phối đối xứng gợi ý cân bằng; phân phối kéo dài hoặc nhiều đỉnh có thể phản ánh khám phá và các vùng thỏa thuận khác nhau. Hình dạng là mô tả hậu quả, không phải nguyên nhân.

#### Phạm vi chọn dữ liệu

Hồ sơ theo phiên, theo vùng cố định và composite trả lời các câu hỏi khác nhau. Nếu chọn phạm vi tùy tiện để khớp ý tưởng, người giao dịch sẽ tạo ra mốc giả.

### Ngữ nghĩa và giới hạn của POC, HVN, LVN và nPOC

* **POC** là mức hoạt động lớn nhất trong hồ sơ đã chọn. Nó không phải giá trị nội tại, không mặc định là hỗ trợ/kháng cự và không có lực hút vật lý. Trong cân bằng, POC có thể là mục tiêu trung gian của nhịp luân phiên; trong khám phá giá mạnh, nó có thể bị bỏ lại lâu.
* **nPOC** nên được hiểu là POC chưa được kiểm tra lại. Nó là mốc tham chiếu lịch sử, không tạo nghĩa vụ cho giá phải quay về.
* **HVN** cho thấy vùng từng có hoạt động và sự chấp nhận cao. Nó có thể là vùng luân phiên, rào cản hoặc mục tiêu, nhưng vai trò hiện tại phải được Episode xác nhận.
* **LVN** cho thấy vùng ít hoạt động trong hồ sơ lịch sử. Sau khi được chấp nhận xuyên qua, nó có thể trở thành hành lang di chuyển nhanh; mép LVN cũng có thể tạo từ chối. Không được mặc định “vào LVN là giá sẽ bay”.

Không dùng các câu tuyệt đối: “POC là nam châm”, “HVN chắc chắn giữ”, “LVN là chân không”, “nPOC bắt buộc được lấp”.

### Quy trình áp dụng

1. Chọn phạm vi theo một cuộc đấu giá có lý do rõ.
2. Đánh dấu POC, VAH, VAL, HVN và LVN.
3. So sánh với TPO và cấu trúc giá.
4. Kiểm tra phản ứng khi giá quay lại, thay vì mặc định phản ứng.

### Sai lầm thường gặp

* Kéo phạm vi cho đến khi POC nằm đúng nơi mong muốn.
* Coi HVN luôn là vùng đảo chiều.
* Dùng LVN như bức tường không thể xuyên.

### Ghi nhớ

> Hồ sơ chỉ có ý nghĩa khi phạm vi của nó tương ứng với một cuộc đấu giá hợp lý.

## Chương 9. Vùng giá trị và POC

VAH, VAL và POC là những điểm neo quan trọng, nhưng giá trị của chúng đến từ câu chuyện đấu giá chứ không phải từ việc biến chúng thành đường hỗ trợ kháng cự máy móc.



### Nội dung cốt lõi

#### Bên trong vùng giá trị

Khi giá ở trong vùng giá trị và cấu trúc cân bằng, kỳ vọng cơ bản là luân phiên quanh POC. Tuy vậy, độ rộng, vị trí mở cửa, tin tức và hướng của giá trị nhiều phiên có thể làm thay đổi kỳ vọng.

#### Ngoài vùng giá trị

Giá ngoài VAH/VAL tạo câu hỏi, không tạo câu trả lời: thị trường sẽ được chấp nhận ngoài vùng cũ hay sẽ tái nhập? Câu trả lời cần thời gian, khối lượng, vị trí POC, duy trì giá và Order Flow.

#### POC đang phát triển

POC đang phát triển có thể di chuyển mạnh khi khối lượng mới xuất hiện. Không nên đối xử với nó như mốc bất biến. POC hoàn tất đáng tin cậy hơn về mặt lịch sử, nhưng vẫn có thể mất ý nghĩa khi cuộc đấu giá chuyển sang vùng mới.

#### Dịch chuyển giá trị

sự dịch chuyển vùng giá trị và POC dịch chuyển cho thấy vùng hoạt động được duy trì đang dịch. Đây là bằng chứng cấu trúc quan trọng hơn một lần chạm đường đơn lẻ.

### Vùng giá trị là quy ước mô tả

Tỷ lệ 68% hoặc 70% là tham số xây vùng giá trị theo phương pháp của nguồn hoặc nền tảng. Hồ sơ thực tế có thể lệch, nhiều đỉnh hoặc có hai vùng phân phối. POC không nhất thiết là trung bình thống kê, và VAH/VAL không mặc nhiên tương ứng với cộng/trừ một độ lệch chuẩn.

Vùng giá trị là nơi hoạt động được chấp nhận tương đối trong phạm vi và thời gian đã chọn, không phải “giá thật” tuyệt đối của vàng.

### Quy trình áp dụng

1. Xác định giá đang trong, trên hay dưới vùng giá trị.
2. So sánh vùng giá trị hiện tại với phiên trước.
3. Quan sát POC đang đứng yên hay dịch.
4. Chờ bằng chứng tái nhập hoặc chấp nhận trước khi chọn FAR hay AAC.

### Sai lầm thường gặp

* Bán ngay tại VAH và mua ngay tại VAL.
* Gọi POC là nam châm bất kể trạng thái.
* Không phân biệt POC đang phát triển với POC hoàn tất.

### Ghi nhớ

> VAH và VAL là biên của một vùng thỏa thuận quá khứ; điều cần biết là thị trường hiện tại còn tôn trọng thỏa thuận ấy hay không.

## Chương 10. Initial Balance

Initial Balance (IB) là phạm vi hình thành trong giai đoạn đầu của phiên theo quy ước của thị trường. Nó cung cấp khung tham chiếu cho cách phạm vi còn lại phát triển.



### Nội dung cốt lõi

#### Đỉnh và đáy IB

Hai biên của IB là nơi cuộc đấu giá đầu phiên dừng lại. Chúng có thể trở thành mốc tham chiếu trong ngày, đặc biệt khi giá quay lại sau khi mở rộng phạm vi.

#### Độ rộng IB

IB hẹp tạo điều kiện về không gian cho mở rộng phạm vi nhưng không bảo đảm Trend Day. IB rộng có thể hấp thụ phần lớn phạm vi kỳ vọng, nhưng tin tức hoặc chuyển động lớn vẫn có thể mở rộng thêm.

#### Mở rộng phạm vi

Mở rộng một phía cho thấy cuộc đấu giá tìm giá ngoài IB. Mở rộng hai phía cho thấy xung đột hoặc ứng viên Neutral Day. Quan trọng nhất vẫn là việc giá có xây hoạt động ngoài IB hay không.

#### IB và cấu trúc ngày

IB là một đầu vào cho phân loại cấu trúc ngày, không phải toàn bộ câu trả lời. Vị trí mở cửa, vùng giá trị, nhịp độ và kết quả cuối phiên cũng cần được xem xét.

### Vị trí mở cửa so với phiên trước

Vị trí mở cửa cung cấp câu hỏi đầu tiên của phiên, không phải tín hiệu giao dịch:

* **Mở trong vùng giá trị và trong phạm vi trước:** thị trường bắt đầu gần vùng thỏa thuận cũ; ưu tiên quan sát luân phiên cho đến khi một phía chứng minh được khả năng rời vùng.
* **Mở ngoài vùng giá trị nhưng vẫn trong phạm vi trước:** giá đang thử phần ít được chấp nhận hơn của phạm vi; cần phân biệt phản ứng quay về với hoạt động khởi xướng tiếp tục ra ngoài.
* **Mở ngoài phạm vi trước:** thị trường bắt đầu bằng một khoảng cách khỏi vùng thỏa thuận cũ; cần đánh giá việc khoảng cách được duy trì hay bị lấp lại.

### Các kiểu mở cửa thường gặp

Các kiểu mở cửa chỉ là mô hình mô tả ban đầu và có thể thay đổi khi dữ liệu phát triển:

* **Mở cửa dẫn động:** giá rời vùng mở cửa nhanh, ít quay lại và liên tục tạo tiến triển.
* **Mở cửa thử rồi đi:** giá kiểm tra một phía trước, thất bại, sau đó phát triển theo phía còn lại.
* **Mở cửa từ chối rồi đảo:** giá đi ra một phía, bị từ chối rõ và quay xuyên vùng mở cửa theo hướng ngược lại.
* **Mở cửa đấu giá:** giá qua lại quanh vùng mở cửa, chưa có phía nào tạo được sự duy trì.

Không gắn nhãn quá sớm. Kiểu mở cửa phải được kiểm tra bằng phạm vi, khối lượng, POC đang phát triển, khả năng duy trì và diễn biến sau lần thử đầu tiên.



### Tồn kho qua đêm

Tồn kho qua đêm mô tả việc vị thế ngắn hạn trong phiên điện tử nghiêng mạnh về một phía trước khi phiên chính bắt đầu. Nó không cho biết chính xác ai đang nắm vị thế và không buộc thị trường phải điều chỉnh.

Dấu hiệu cần theo dõi:

* Giá qua đêm di chuyển một chiều và đóng gần cực trị.
* Phần lớn phạm vi qua đêm nằm trên hoặc dưới vùng giá trị trước.
* Khi phiên chính mở cửa, thị trường giữ được vùng qua đêm hay nhanh chóng quay lại vùng thỏa thuận cũ.

Sự điều chỉnh tồn kho chỉ là giả thuyết\*. Nếu giá tiếp tục được chấp nhận theo hướng qua đêm, việc cố giao dịch ngược chỉ vì “tồn kho lệch” là sai logic.

### Quy trình áp dụng

1. Xác định IB theo đúng mốc thời gian đã chọn.
2. Đo độ rộng tương đối với lịch sử.
3. Ghi lần phá IB đầu tiên và phản ứng sau phá.
4. Kiểm tra vùng giá trị/POC có đi theo mở rộng phạm vi.

### Sai lầm thường gặp

* Mua mọi cú phá đỉnh IB.
* Gán Trend Day quá sớm.
* Thay đổi thời lượng IB tùy ý sau khi nhìn kết quả.

### Ghi nhớ

> IB là khung mở đầu; chất lượng của mở rộng phạm vi mới cho biết câu chuyện tiếp theo.

## Chương 11. Hình dạng Profile và cấu trúc ngày

Loại ngày là cách mô tả cấu trúc sau khi dữ liệu phát triển đủ. Trong phiên, mọi nhãn chỉ là ứng viên có thể thay đổi.



### Nội dung cốt lõi

#### Normal và Normal Variation

Normal Day có phần lớn phạm vi trong IB; Normal Variation có mở rộng phạm vi rõ một phía nhưng vẫn giữ cấu trúc tương đối cân bằng.

#### Trend Day

Trend Day thường có mở rộng phạm vi mạnh, hồi nông, POC/vùng giá trị dịch theo hướng và cấu trúc kéo dài. Một cây nến lớn không đủ; cần sự duy trì.

#### Neutral và Non-Trend

Neutral Day mở rộng hai phía IB. Non-Trend có phạm vi hẹp và ít khám phá. Hai loại này đòi hỏi cách đặt mục tiêu khác với Trend Day.

#### Phân phối kép và hình P/b/D

Phân phối kép (Double Distribution) phản ánh hai vùng thỏa thuận được nối bởi một vùng đi nhanh. P-shape, b-shape và D-shape chỉ mô tả hình dạng; chúng không tự chứng minh đóng vị thế bán, thanh lý vị thế mua hay cân bằng.

### Quy trình áp dụng

1. Gắn nhãn “ứng viên” trong phiên.
2. Cập nhật khi vùng giá trị, mở rộng phạm vi và hình phân phối thay đổi.
3. Chỉ xác nhận sau khi phiên đủ hoàn chỉnh.
4. Dùng loại ngày để điều chỉnh kỳ vọng, không dùng như tín hiệu vào.

### Sai lầm thường gặp

* Gọi P-shape là đóng vị thế bán chắc chắn.
* Cố giữ nhãn Trend Day dù giá đã tái nhập vùng giá trị.
* Dự đoán loại ngày quá sớm để biện minh cho lệnh.

### Ghi nhớ

> Cấu trúc ngày là kết quả của cuộc đấu giá, không phải chiếc khuôn ép thị trường phải diễn tiếp.

## Chương 12. Hệ thống mốc tham chiếu

Mốc tham chiếu là mức hoặc vùng có ý nghĩa vì nó tóm tắt một cuộc đấu giá trước, một cực trị, một vùng thỏa thuận hoặc một điểm chuyển trạng thái.

### Nội dung cốt lõi

#### Nhóm hồ sơ

VAH/VAL/POC phiên trước, VAH/VAL/POC Composite, HVN, LVN và nPOC là các mốc tham chiếu xuất phát từ phân phối hoạt động.

#### Nhóm phạm vi

Đỉnh/đáy IB, đỉnh/đáy phiên qua đêm, đỉnh/đáy tuần, tháng và các điểm xoay là mốc tham chiếu xuất phát từ cực trị hoặc cấu trúc thời gian.

#### Nhóm tiến trình

VWAP, Anchored VWAP, điểm khởi phát chuyển động, đỉnh/đáy sự kiện và vùng Single Prints mô tả tiến trình hoặc nguồn khởi phát.

#### Sự hội tụ của các mốc

Nhiều mốc tham chiếu gần nhau có thể làm vùng trở nên đáng chú ý hơn, nhưng sự hội tụ không tự tạo chiều giao dịch. Phải biết vai trò của từng mốc và trạng thái cuộc đấu giá khi giá đến.

#### Tuổi và số lần kiểm tra

Mốc cũ hoặc bị giao dịch xuyên qua nhiều lần có thể mất tính phân biệt. Tuy nhiên không có quy luật “kiểm tra càng nhiều càng yếu” áp dụng cho mọi bối cảnh; cần xem phản ứng và khả năng phục hồi thanh khoản.

### Vùng mốc cấu trúc

Mốc nên được trình bày như một **vùng giá**, không phải đúng một tick. Độ rộng phải có lý do: kích thước tick, độ biến động, chất lượng dữ liệu, phạm vi hồ sơ, thời điểm trong phiên và chênh lệch giữa hợp đồng tương lai với CFD.

Mỗi vùng mốc phải có:

&#x20;   Nguồn hình thành
    Khung thời gian
    Vai trò: biên / trung tâm / cực trị / hành lang
    Tuổi của mốc
    Số lần kiểm tra
    Hành vi được kỳ vọng
    Điều kiện làm mốc mất vai trò


### Quy trình áp dụng

1. Chỉ giữ các mốc có nguồn gốc rõ.
2. Gắn vai trò: biên trên, biên dưới, trung tâm, cực trị hoặc hành lang.
3. Xếp theo khung thời gian.
4. Loại bớt mốc trùng lặp gây nhiễu.
5. Khi giá đến, mở một Episode thay vì vào lệnh ngay.

### Sai lầm thường gặp

* Vẽ quá nhiều đường.
* Gọi mọi mốc là hỗ trợ/kháng cự.
* Đánh đồng sự hội tụ của nhiều mốc với xác suất cao khi chưa có dữ liệu.

### Ghi nhớ

> Mốc tham chiếu chỉ là nơi đặt câu hỏi; Episode và bằng chứng mới trả lời.

## Chương 13. Composite Profile và cấu trúc nhiều phiên

Composite Profile gom các phiên thuộc cùng một cuộc đấu giá để nhìn vùng thỏa thuận lớn hơn. Chất lượng của composite phụ thuộc vào cách chọn phạm vi, không chỉ vào số ngày.



### Nội dung cốt lõi

#### Khi nên gộp

Các phiên có vùng giá trị chồng lấn, POC gần nhau, phạm vi luân phiên và chưa có sự tách biệt bền vững thường có thể thuộc cùng vùng cân bằng nhiều ngày.

#### Khi không nên gộp

Nếu thị trường đã tạo giá trị tách biệt, POC dịch rõ và không tái nhập vùng cũ, việc gộp có thể che mất khám phá giá mới.

#### Biên Composite và Trung tâm

VAH/VAL composite mô tả biên thỏa thuận nhiều phiên; Composite POC mô tả trung tâm hoạt động. Biên thích hợp để đặt câu hỏi FAR/AAC, Trung tâm thích hợp để đánh giá luân phiên.

#### Đang phát triển và được xác nhận

Composite đang phát triển có thể thay đổi theo phiên mới. Composite được xác nhận nên dựa trên quy tắc kết thúc vùng cân bằng rõ ràng, không phải chọn sau khi thấy giá chạy.

### Quy trình áp dụng

1. Xác định phiên bắt đầu của vùng cân bằng.
2. Kiểm tra chồng lấn và dịch chuyển.
3. Xây Composite từ các phiên có cùng cách tổ chức.
4. Đánh dấu biên, trung tâm và LVN.
5. Khi giá rời composite, theo dõi chấp nhận hoặc tái nhập.

### Sai lầm thường gặp

* Gộp cố định N ngày bất kể cấu trúc.
* Loại phiên không thuận ý tưởng.
* Giữ composite cũ quá lâu sau khi vùng giá trị mới đã hình thành.

### Ghi nhớ

> Composite là bản đồ của một cuộc đấu giá nhiều phiên, không phải phép cộng máy móc của lịch.

## Chương 14. Dịch chuyển giá trị và bối cảnh định hướng

Hướng giá ngắn hạn có thể nhiễu. Dịch chuyển của vùng giá trị, POC và phạm vi cho thấy nơi thị trường thực sự tổ chức hoạt động qua thời gian.



### Nội dung cốt lõi

#### Vùng giá trị cao dần và thấp dần

Vùng giá trị cao dần cho thấy vùng hoạt động được duy trì đang dịch lên; vùng giá trị thấp dần cho thấy vùng hoạt động đang dịch xuống. Mức độ chồng lấn quyết định sức mạnh của sự dịch chuyển.

#### POC dịch chuyển

POC dịch cùng vùng giá trị củng cố câu chuyện. Giá tăng nhưng POC đứng yên có thể là khám phá giá chưa hoàn thiện hoặc chỉ là độ lệch.

#### Cấu trúc và ngắn hạn

Bối cảnh cấu trúc dùng nhiều phiên hoàn tất; bối cảnh ngắn hạn dùng phiên đang phát triển. Hai lớp có thể đồng thuận hoặc xung đột.

#### Xung đột

Xung đột là trạng thái hữu ích khi bằng chứng không cùng hướng. Nó yêu cầu giảm độ chắc chắn, rút mục tiêu hoặc chờ vị trí rõ hơn, không buộc phải đoán bên thắng.

### Quy trình áp dụng

1. So sánh ba đến năm phiên hoàn tất.
2. Xác định hướng vùng giá trị và POC.
3. Đánh giá chồng lấn.
4. Tách bối cảnh cấu trúc và ngắn hạn.
5. Ghi rõ trạng thái đồng thuận, chuyển tiếp hoặc xung đột.

### Sai lầm thường gặp

* Dùng giá đóng cửa thay cho sự dịch chuyển vùng giá trị.
* Gọi mọi phiên cao hơn là xu hướng tăng.
* Ép xung đột thành Long hoặc Short.

### Ghi nhớ

> Hướng bền vững được thể hiện qua nơi thị trường xây hoạt động, không chỉ qua điểm giá cuối cùng.

## Chương 15. One-Time Framing: nhịp cực trị một chiều

One-Time Framing (OTF) mô tả việc các cực trị của những khoảng thời gian **đã hoàn tất** tiếp tục giữ một nhịp một chiều. OTF là công cụ mô tả tiến triển, không phải tín hiệu đảo chiều hoặc xu hướng dài hạn.



### OTF tăng

OTF tăng được duy trì khi **đáy của khoảng hoàn tất hiện tại không thấp hơn đáy của khoảng trước**. Điều này bao gồm cả đáy cao hơn và đáy bằng nhau.

&#x20;   Đáy hiện tại ≥ Đáy trước
    → OTF tăng còn duy trì


OTF tăng chỉ bị phá khi xuất hiện một đáy thấp hơn.

### OTF giảm

OTF giảm được duy trì khi **đỉnh của khoảng hoàn tất hiện tại không cao hơn đỉnh của khoảng trước**. Điều này bao gồm cả đỉnh thấp hơn và đỉnh bằng nhau.

&#x20;   Đỉnh hiện tại ≤ Đỉnh trước
    → OTF giảm còn duy trì


OTF giảm chỉ bị phá khi xuất hiện một đỉnh cao hơn.

### Cực trị bằng nhau

Đỉnh bằng nhau hoặc đáy bằng nhau vẫn duy trì OTF theo định nghĩa trên, nhưng cho thấy tiến triển có thể đang yếu đi. Cần kiểm tra thêm phạm vi, vùng giá trị, POC, khối lượng và phản ứng sau đó.

### OTF bị phá

Một lần phá OTF là cảnh báo nhịp ngắn hạn đã thay đổi. Nó không tự động đảo hướng cấu trúc nhiều phiên. Trong cân bằng, OTF thường chỉ mô tả một nhịp luân phiên nội vùng.

### Quy trình áp dụng

1. Chỉ dùng các khoảng đã hoàn tất.
2. Ghi rõ quy tắc xử lý cực trị bằng nhau.
3. Đặt OTF trong bối cảnh vùng giá trị, Composite và Episode.
4. Kiểm tra POC/vùng giá trị có dịch cùng hướng hay không.
5. Không vào lệnh chỉ vì OTF được duy trì hoặc bị phá.

### Sai lầm thường gặp

* Dùng khoảng đang chạy để xác nhận.
* Đòi hỏi đáy luôn phải cao hơn hoặc đỉnh luôn phải thấp hơn, làm mất trường hợp bằng nhau.
* Gọi một lần phá OTF là đảo xu hướng.
* Bỏ qua việc OTF nằm giữa vùng cân bằng.

### Ghi nhớ

> OTF đo nhịp cực trị của các khoảng hoàn tất; vùng giá trị và Episode quyết định ý nghĩa của nhịp đó.

## Chương 16. Auction Episode: vòng đời tương tác với một mốc

Episode là toàn bộ câu chuyện từ lúc giá tiếp cận một mốc tham chiếu, tương tác, đi ra ngoài, tái nhập hoặc được chấp nhận. Tư duy Episode thay thế thói quen phán quyết bằng một cây nến.

### Nội dung cốt lõi

#### Bắt đầu Episode

Episode bắt đầu khi giá thực sự tương tác với vùng mốc đã chọn. “Tiếp cận” chỉ có ý nghĩa khi khoảng cách được định nghĩa trước; nếu không, nên coi là quan sát chuẩn bị.

#### Lần thử đấu giá ngoài vùng

Khi giá giao dịch vượt ra ngoài phía chuẩn của một biên, ta có một lần thử đấu giá ngoài. Điều này chưa chứng minh có quét thanh khoản, kích hoạt dừng lỗ, thất bại hoặc tiếp diễn.

#### Độ lệch và số lần thử

Độ lệch tối đa đo khoảng cách xa nhất mà giá đi khỏi mốc. Số lần thử đếm các lần thử độc lập theo quy tắc rõ ràng. Nhiều lần thử cung cấp dữ liệu để so sánh Nỗ lực–Kết quả, nhưng không tự chứng minh mốc đang yếu đi.

#### Episode quanh đường trung tâm

Với POC hoặc đường trung tâm, khái niệm trong/ngoài không đối xứng như biên. Ta quan sát chạm, xuyên, số lần qua lại và độ lệch hai phía.

#### Kết thúc

Episode kết thúc khi cuộc đấu giá được giải quyết, mốc mất vai trò, phiên thay đổi hoặc luận điểm hết thời hạn. Episode chưa giải quyết là một trạng thái hợp lệ và thường dẫn đến không giao dịch.

### Quy trình áp dụng

1. Chọn mốc tham chiếu trước khi giá đến.
2. Ghi thời điểm tương tác đầu tiên.
3. Theo dõi độ lệch, thời gian, khối lượng, số giao dịch và POC cục bộ.
4. Đánh dấu tái nhập hoặc sự duy trì ngoài vùng.
5. Chưa gán FAR/AAC cho đến khi bằng chứng đủ.

### Sai lầm thường gặp

* Bắt đầu Episode sau khi nhìn thấy kết quả.
* Gọi mọi lần xuyên là quét thanh khoản.
* Tạo Episode mới cho từng cây nến và mất lịch sử thử giá.

### Ghi nhớ

> Episode biến một khoảnh khắc thành một quá trình có thể đo lường.

## Chương 17. Sự chấp nhận, từ chối và tái nhập

Đây là trung tâm của phương pháp. Giá đi qua mốc chỉ là hình học; Sự chấp nhận hoặc Tái chấp nhận cần bằng chứng duy trì và tổ chức hoạt động.



### Nội dung cốt lõi

#### Sự chấp nhận

Sự chấp nhận là quá trình thị trường duy trì giao dịch và bắt đầu xây hoạt động/vùng giá trị ở vùng mới. Bằng chứng gồm thời gian bên ngoài, khối lượng, số giao dịch, POC cục bộ, khả năng giữ sau nhịp hồi và tiến triển của vùng giá trị.

#### Sự từ chối

Sự từ chối là phản ứng đẩy giá ra khỏi vùng thử. Một phản ứng nhanh mới chỉ là ứng viên từ chối; để kết luận cuộc đấu giá thất bại, cần biết thị trường có tái nhập và duy trì trong vùng cũ hay không.

#### Tái nhập

Tái nhập chỉ mô tả việc giá quay vào vùng cũ. Một râu nến quay vào rồi lập tức đi ra chưa phải tái chấp nhận. Sau tái nhập, cần theo dõi thời gian, hoạt động và khả năng duy trì trong vùng.

#### Tái chấp nhận

Tái chấp nhận xuất hiện khi thị trường tổ chức lại giao dịch trong vùng cũ sau một lần thử đấu giá ngoài vùng. POC cục bộ quay vào, vùng giá trị cục bộ hình thành trong và kiểm tra lại từ bên trong thất bại là các bằng chứng hữu ích.

#### Kiểm tra lại

Kiểm tra lại là lần quay lại kiểm tra vùng chuyển trạng thái. Kiểm tra lại giúp tạo hình học vào lệnh và vô hiệu rõ hơn, nhưng không phải lúc nào cũng xảy ra.

### Quy trình áp dụng

1. Đo tỷ lệ thời gian, khối lượng và số giao dịch ngoài vùng.
2. Theo dõi POC cục bộ và vùng giá trị.
3. Xác định tái nhập về mặt hình học.
4. Chờ sự duy trì trong hoặc ngoài.
5. Dùng kiểm tra lại để kiểm tra trạng thái.
6. Phân loại FAR, AAC hoặc chưa được giải quyết.

### Sai lầm thường gặp

* Dùng một cây nến đóng ngoài làm Sự chấp nhận.
* Dùng một râu nến quay vào để kết luận tái chấp nhận.
* Đặt ngưỡng cứng mà không kiểm chứng theo chế độ thị trường.

### Ghi nhớ

> Sự chấp nhận là quá trình xây hoạt động; tái nhập là hình học; tái chấp nhận là khả năng duy trì sau khi quay vào.

## Chương 18. Cuộc đấu giá thất bại và cuộc đấu giá tiếp diễn

Một lần thử đấu giá ngoài vùng thường có ba kết quả: thất bại và quay lại, được chấp nhận và tiếp diễn, hoặc chưa được giải quyết. Hai kết quả đầu tạo nền cho FAR và AAC.



### Nội dung cốt lõi

#### Cuộc đấu giá thất bại

Cuộc đấu giá thất bại khi giá thử vùng mới nhưng không xây sự chấp nhận bền vững, sau đó tái nhập và duy trì trong vùng cũ. Không phải mọi cú phá vỡ giả đều đạt chuẩn này.

#### Tiếp diễn được chấp nhận

Cuộc đấu giá tiếp diễn khi hoạt động, POC hoặc vùng giá trị phát triển ngoài vùng cũ, và các nhịp hồi không tạo tái chấp nhận bền vững vào vùng cũ.

#### Khả năng lấy lại vùng giá trị cũ

Khả năng lấy lại vùng giá trị cũ là phép thử quan trọng. Với FAR, lấy lại được duy trì; với AAC, lấy lại thất bại hoặc chỉ xảy ra thoáng qua.

#### Hai lần thử

Lần thử thứ hai cho phép so sánh Nỗ lực–Kết quả với lần đầu. Nếu nỗ lực tương đương hoặc lớn hơn nhưng độ lệch và sự duy trì kém hơn, giả thuyết thất bại có thể mạnh hơn. Tuy nhiên lần thử 2 không tự động là điểm vào.

#### Chưa được giải quyết

Nếu hoạt động phân bổ hai phía, POC không rõ, giá liên tục qua lại và không có sự duy trì, trạng thái đúng là chưa giải quyết. Không giao dịch cũng là quyết định có cấu trúc.

### Quy trình áp dụng

1. Xác định lần thử đấu giá ngoài vùng.
2. Đo sự chấp nhận ngoài vùng.
3. Đánh giá tái nhập và vùng giá trị cũ lấy lại.
4. So sánh Nỗ lực–Kết quả giữa các lần thử.
5. Chọn FAR, AAC hoặc chưa được giải quyết.
6. Chỉ sau đó mới chọn phong cách vào.

### Sai lầm thường gặp

* Gọi thất bại ngay khi giá quay đầu.
* Gọi tiếp diễn ngay khi giá vừa phá vỡ với Delta lớn.
* Bỏ qua trạng thái chưa được giải quyết vì muốn có giao dịch.

### Ghi nhớ

> Lần thử đấu giá ngoài vùng là câu hỏi; FAR, AAC và chưa được giải quyết là ba câu trả lời có thể có.

## Chương 19. Năm quy tắc kinh nghiệm quanh vùng giá trị\*

Năm nội dung dưới đây được giữ như **quy tắc kinh nghiệm thuộc AMT**, không phải định luật. Chúng giúp tạo kịch bản quan sát nhưng không tự tạo điểm vào.

### Quy tắc 1\*: Tái chấp nhận vào vùng cân bằng

Khi giá được tái chấp nhận vào vùng cân bằng, biên đối diện có thể trở thành mục tiêu của nhịp luân phiên. Giá cũng có thể kiểm tra lại biên vừa đi qua trước khi tiếp tục vào sâu hơn.

Điều kiện cần xem:

* Tái chấp nhận thực sự, không chỉ một bóng nến.
* Không có vùng giá trị mới phát triển bên ngoài.
* POC và các rào cản trung gian không tạo phản ứng phủ định.
* Dòng lệnh sau tái nhập tạo được tiến triển.
* Đường tới biên đối diện còn đủ khoảng trống.

Biên đối diện là mục tiêu tiềm năng, không phải mục tiêu bắt buộc.

### Quy tắc 2\*: Luân phiên bên trong cân bằng

Trong một vùng cân bằng còn nguyên vẹn, giá có xu hướng luân phiên giữa các biên và quay lại trung tâm. Tuy nhiên, chạm VAH không đồng nghĩa bán và chạm VAL không đồng nghĩa mua. Mỗi biên chỉ mở một Episode để quan sát phản ứng.

Khi thời gian, khối lượng và POC cục bộ bắt đầu xây ngoài biên, giả thuyết luân phiên suy yếu.

### Quy tắc 3\*: Chấp nhận ngoài cân bằng

Khi giá được chấp nhận ngoài vùng cân bằng, ưu tiên giả thuyết khám phá giá và tìm vùng giá trị mới. Mục tiêu là mốc cấu trúc có ý nghĩa tiếp theo hoặc vùng từng được chấp nhận nằm trên đường đi.

POC cũ chỉ trở thành mục tiêu hợp lý khi cuộc đấu giá quay lại vùng giá trị cũ.

### Quy tắc 4\*: POC có thể ngắt đường luân phiên

Trong một luận điểm luân phiên, POC là rào cản trung gian. Phản ứng mạnh tại POC, dòng lệnh đối diện tạo kết quả hoặc vùng giá trị bắt đầu xây quanh POC có thể làm giảm khả năng giá đi hết tới biên đối diện.

### Quy tắc 5\*: Xây hoạt động tại biên

Thời gian, khối lượng và POC cục bộ xây tại hoặc ngoài biên làm tăng tính hợp lý của giả thuyết chấp nhận và AAC, nhưng chỉ khi giá duy trì ngoài và không lấy lại bền vững vùng giá trị cũ.

&#x20;   Hoạt động lớn tại biên nhưng không có tiến triển
    → có thể là hấp thụ hoặc trạng thái chưa giải quyết

    Hoạt động xây ngoài biên + POC/vùng giá trị dịch + duy trì ngoài
    → ứng viên chấp nhận


### Ghi nhớ

> Năm quy tắc này tạo câu hỏi và kỳ vọng có điều kiện. Episode, sự chấp nhận và kết quả giá mới quyết định.

# PHẦN III — DÒNG LỆNH ĐÃ KHỚP VÀ VI CẤU TRÚC THỊ TRƯỜNG

## Chương 20. Cơ chế thực thi lệnh

Order Flow bắt đầu từ một sự thật đơn giản: chỉ lệnh đã khớp mới trở thành giao dịch. Sổ lệnh cho thấy ý định đang treo; Tape và Footprint cho thấy phần đã thực sự khớp.



### Nội dung cốt lõi

#### Lệnh giới hạn và lệnh thị trường

Lệnh giới hạn chờ tại một mức giá và cung cấp thanh khoản. Lệnh thị trường hoặc lệnh có khả năng khớp ngay lấy thanh khoản đang có. Một giao dịch luôn cần hai phía, nhưng phía chủ động là phía chấp nhận giá hiện có để khớp ngay.

#### Bid và Ask

Bid là giá mua chờ cao nhất; Ask là giá bán chờ thấp nhất. Giao dịch khớp tại Ask thường được phân loại là mua chủ động; khớp tại Bid thường là bán chủ động.

#### Hàng đợi và khớp một phần

Nhiều lệnh có thể đứng cùng giá. Thứ tự ưu tiên và khớp một phần khiến một mức giá có thể hấp thụ nhiều giao dịch trước khi di chuyển.

#### Hủy lệnh

Lệnh treo có thể bị hủy. Vì vậy độ sâu sổ lệnh không phải cam kết. Phương pháp ưu tiên giao dịch đã khớp, phản ứng giá và bối cảnh thay vì tin tuyệt đối vào tường lệnh.

#### Giới hạn quan sát

Dữ liệu thường không cho biết danh tính, mục đích, vị thế trước đó hoặc chiến lược của người giao dịch. Mọi câu chuyện về tổ chức phải được xem là giả thuyết.

### Quy trình áp dụng

1. Xác định dữ liệu nào là lệnh treo và dữ liệu nào là giao dịch đã khớp.
2. Đọc hành vi chủ động tại đúng vị trí AMT.
3. Quan sát kết quả giá sau nỗ lực thực thi.
4. Không suy luận ý định vượt quá dữ liệu.

### Sai lầm thường gặp

* Gọi lệnh giới hạn lớn là hỗ trợ chắc chắn.
* Coi mọi lực mua chủ động là tín hiệu tăng.
* Nhầm số hợp đồng khớp với số người tham gia.

### Ghi nhớ

> Order Flow đo hành vi thực thi; giá cho biết hành vi đó có tác dụng hay không.

## Chương 21. Bid, Ask và phân loại phía chủ động

Phân loại phía chủ động giúp chia khối lượng đã khớp thành mua chủ động, bán chủ động và không xác định. Chất lượng dữ liệu quan trọng hơn vẻ chính xác của con số.

### Nội dung cốt lõi

#### Mua chủ động

Khi giao dịch khớp tại Ask hiện hành, người mua đã chấp nhận giá bán để khớp ngay. Điều này cho thấy mức độ khẩn trương, không chứng minh người mua sẽ thắng.

#### Bán chủ động

Khi giao dịch khớp tại Bid hiện hành, người bán đã chấp nhận giá mua để khớp ngay. Nó đo áp lực thực thi tại thời điểm đó.

#### Không xác định

Nếu dấu thời gian, báo giá hoặc nguồn dữ liệu không cho phép phân loại tin cậy, giao dịch nên được giữ ở trạng thái không xác định. Gán phía chủ động bằng quy tắc tick khi dữ liệu không đủ sẽ tạo sự tự tin giả.

#### Tỷ lệ phân loại phía chủ động

Tỷ lệ khối lượng được phân loại cho biết độ tin cậy của Delta. Nếu tỷ lệ này thấp, phải giảm quyền kết luận của Delta và tập trung vào tổng khối lượng, số giao dịch và kết quả giá.

#### Hai phía cùng tồn tại

Một lệnh mua chủ động khớp với một người bán thụ động. Order Flow không nói bên nào “thông minh”; nó chỉ nói bên nào đang đòi khớp ngay.

### Quy trình áp dụng

1. Kiểm tra nguồn và quy tắc phân loại.
2. Theo dõi tỷ lệ Không xác định.
3. Dùng Delta chỉ trong phần dữ liệu đã phân loại.
4. So sánh áp lực chủ động với tiến triển.

### Sai lầm thường gặp

* Ép toàn bộ giao dịch thành Bid hoặc Ask.
* Dùng Delta bằng 0 như bằng chứng cân bằng khi tỷ lệ phân loại thấp.
* Gọi thụ động phía là tổ chức mà không có bằng chứng.

### Ghi nhớ

> Không xác định là trạng thái trung thực, không phải lỗi cần che giấu.

## Chương 22. Delta

Delta là chênh lệch giữa khối lượng mua chủ động và bán chủ động trong phạm vi đã chọn. Nó đo áp lực chủ động đã khớp, không đo vị thế ròng hay niềm tin của thị trường.



### Nội dung cốt lõi

#### Công thức

`Delta = Ask Volume - Bid Volume` trong phần giao dịch được phân loại. Delta dương nghĩa mua chủ động nhiều hơn; Delta âm nghĩa bán chủ động nhiều hơn.

#### Phạm vi

Delta có thể được tính theo mức giá, thanh, phiên, Episode hoặc đoạn tùy chọn. Kết luận chỉ hợp lệ trong phạm vi tính.

#### Delta cùng hướng giá

Giá tăng với Delta dương và tiến triển ổn định có thể phù hợp với khả năng tạo thuận lợi theo hướng lên. Tuy nhiên, cần xem vị trí, vùng giá trị và mức nỗ lực cần thiết.

#### Delta bất đồng giá

Giá tăng trong khi Delta âm cho thấy áp lực bán chủ động không tạo được tiến triển giảm tương xứng. Điều này phù hợp với giả thuyết bên mua thụ động đang chống đỡ, nhưng chưa đủ để kết luận có hấp thụ hoặc đảo chiều.

#### Cực trị Delta

“Cực đoan” phải được định nghĩa theo phân phối và chế độ. Một con số lớn tuyệt đối trong phiên sôi động có thể bình thường.

### Quy trình áp dụng

1. Chọn phạm vi tính Delta.
2. Kiểm tra tỷ lệ phân loại.
3. So sánh dấu Delta với hướng và độ lớn tiến triển giá.
4. Đặt kết quả vào đúng vị trí cấu trúc.
5. Chỉ gọi ứng viên nếu thiếu xác nhận.

### Sai lầm thường gặp

* Dùng Delta dương để mua bất kể vị trí.
* Coi Delta âm là bằng chứng chắc chắn về vị thế bán.
* So sánh con số giữa phiên khác nhau mà không chuẩn hóa.

### Ghi nhớ

> Delta nói bên nào chủ động hơn; Nỗ lực–Kết quả nói sự chủ động ấy có hiệu quả hay không.

## Chương 23. Cumulative Volume Delta

CVD cộng dồn Delta theo thời gian. Nó giúp nhìn tiến trình áp lực chủ động nhưng rất nhạy với điểm bắt đầu, dữ liệu thiếu và chuyển tháng.

### Nội dung cốt lõi

#### CVD theo phiên

Đặt lại tại đầu phiên cho phép theo dõi áp lực chủ động trong một cuộc đấu giá trong ngày. Nó không đại diện cho toàn bộ vị thế của người tham gia.

#### CVD có neo

Có thể neo CVD tại một sự kiện, mốc hoặc đầu Episode. Điều này hữu ích để so sánh nỗ lực từ thời điểm cuộc thử giá bắt đầu.

#### Phân kỳ

Giá tạo đỉnh mới trong khi CVD không tạo đỉnh có thể cho thấy giá đang tiến xa hơn mức áp lực chủ động đã phân loại, hoặc lực mua chủ động đang giảm. Đây là câu hỏi cần kiểm tra, không phải lệnh bán.

#### CVD và dữ liệu

Một nguồn mất giao dịch, sai phía hoặc ghép hợp đồng không đúng có thể làm đường CVD méo lâu dài. Không nên xem đường mượt là bằng chứng dữ liệu tốt.

#### Đặt lại dữ liệu và chuyển tháng

CVD phải có chính sách đặt lại rõ ràng. Không cộng dồn xuyên hợp đồng nếu không có phương pháp nối dữ liệu hợp lý.

### Quy trình áp dụng

1. Chọn mốc neo có ý nghĩa.
2. Kiểm tra tỷ lệ phân loại và tính liên tục của dữ liệu.
3. So sánh các điểm xoay của CVD với các điểm xoay của giá.
4. Kết hợp vùng giá trị, Episode và kiểm tra lại.

### Sai lầm thường gặp

* Dùng CVD như RSI.
* Bán chỉ vì phân kỳ.
* So sánh CVD của hai nguồn dữ liệu mà không kiểm tra cách phân loại.

### Ghi nhớ

> CVD là nhật ký áp lực chủ động tích lũy, không phải máy phát tín hiệu.

## Chương 24. Biểu đồ Footprint

Footprint phân rã mỗi thanh thành hoạt động tại từng mức giá. Nó là kính hiển vi, nhưng kính hiển vi chỉ hữu ích khi biết mẫu vật nằm ở đâu trên bản đồ.



### Nội dung cốt lõi

#### Bid x Ask

Mỗi mức giá hiển thị khối lượng khớp tại Bid và Ask. Người giao dịch nhìn phân bố, không chỉ nhìn tổng Delta của thanh.

#### Delta Footprint

Màu hóa Delta làm nổi bật áp lực chủ động, nhưng màu có thể phóng đại cảm xúc. Luôn đọc số, tỷ lệ và kết quả giá.

#### Volume Footprint

Tổng khối lượng cho biết mức độ tập trung hoạt động. Khối lượng cao tại cực trị có thể phản ánh khả năng tạo thuận lợi, ứng viên hấp thụ hoặc chỉ là một sự kiện dòng lệnh; ý nghĩa phụ thuộc vào kết quả giá.

#### POC của thanh và vị trí đóng cửa

POC của thanh cho biết nơi hoạt động tập trung trong thanh. Giá đóng cửa gần đỉnh/đáy chỉ là hình học; cần kết hợp với hoạt động tại cực trị và thanh sau.

#### Không phải sổ lệnh

Footprint chỉ cho thấy giao dịch đã khớp. Nó không cho thấy toàn bộ lệnh bị hủy, vị trí hàng đợi, lệnh ẩn hay ý định trước khi khớp.

### Quy trình áp dụng

1. Bắt đầu từ Vị trí và Episode.
2. Đọc phân bố từ dưới lên hoặc trên xuống.
3. Ghi hoạt động tại cực trị, POC, Imbalance và vị trí đóng cửa.
4. Quan sát thanh sau hoặc kiểm tra lại.
5. Tránh quyết định từ màu đơn lẻ.

### Sai lầm thường gặp

* Phóng biểu đồ quá gần và làm mất bối cảnh.
* Sưu tầm mẫu nến Footprint mà không xét vị trí.
* Gọi vùng đỏ/xanh là tổ chức.

### Ghi nhớ

> Footprint giúp trả lời “điều gì diễn ra tại đây”, nhưng AMT mới trả lời “tại đây là đâu”.

## Chương 25. Imbalance

Imbalance so sánh khối lượng mua và bán chủ động theo quy tắc xác định. Nó mô tả sự mất cân xứng trong thực thi, không tự chứng minh tiếp diễn.



### Nội dung cốt lõi

#### So sánh cùng mức giá

So sánh Bid và Ask tại cùng mức giá. Cách này dễ hiểu nhưng không phản ánh trực tiếp cách lệnh thị trường nâng hoặc hạ giá qua các bậc.

#### So sánh chéo

So sánh Ask ở một giá với Bid ở giá thấp hơn một tick, hoặc ngược lại. Đây là cách phổ biến để mô tả sự chênh lệch đối diện theo cấu trúc khớp lệnh.

#### Tỷ lệ và ngưỡng tối thiểu

Tỷ lệ rất lớn trên khối lượng cực nhỏ có thể vô nghĩa. Cần có tối thiểu khối lượng và quy tắc phân phối phù hợp với sản phẩm, thời điểm và loại thanh.

#### Stacked Imbalance

Nhiều mức liền nhau cùng phía được gọi stacked Imbalance theo quy tắc cụ thể. Nó có thể phản ánh áp lực chủ động khởi xướng, nhưng vẫn cần tiến triển giá và sự duy trì.

#### Vị trí

Mất cân bằng giữa vùng giá trị có thể chỉ là một phần của nhịp luân phiên. Mất cân bằng tại biên trong một Episode đáng chú ý hơn, đặc biệt khi đi cùng sự chấp nhận hoặc tái nhập.

### Quy trình áp dụng

1. Xác định quy tắc tỷ lệ và tối thiểu khối lượng trước phiên.
2. Đánh dấu Imbalance nhưng không vào ngay.
3. Kiểm tra tiến triển, vị trí đóng cửa và thanh sau.
4. Đặt trong Episode và vùng giá trị Bối cảnh.

### Sai lầm thường gặp

* Thay đổi tỷ lệ chỉ để biểu đồ trông đẹp.
* Coi mất cân bằng xếp chồng là hỗ trợ hoặc kháng cự vĩnh viễn.
* Bỏ qua Không xác định khối lượng và dữ liệu chất lượng.

### Ghi nhớ

> Imbalance đo bất đối xứng thực thi; hiệu quả của bất đối xứng mới quyết định giá trị giao dịch.

## Chương 26. Khối lượng, số giao dịch và kích thước giao dịch trung bình

Ba thước đo này tách “bao nhiêu hợp đồng” khỏi “bao nhiêu lần khớp” và “mỗi lần khớp trung bình bao nhiêu”. Chúng giúp mô tả đặc tính của mức tham gia.

### Nội dung cốt lõi

#### Khối lượng đã khớp

Tổng số hợp đồng đã khớp trong phạm vi. Volume lớn là nỗ lực lớn theo một nghĩa, nhưng chưa nói phía hoặc hiệu quả.

#### Số giao dịch

Số lần khớp cho biết mức độ phân mảnh. Nhiều giao dịch với khối lượng vừa có thể phản ánh hoạt động dày; ít giao dịch với khối lượng lớn phản ánh các lệnh khớp có kích thước lớn hơn.

#### Kích thước giao dịch trung bình

Khối lượng chia cho số giao dịch tạo ra kích thước giao dịch trung bình. Chỉ số này không nhận diện được tổ chức, vì một lệnh có thể bị chia nhỏ và nhiều người có thể tạo ra giao dịch có kích thước tương tự.

#### Giao dịch lớn

Giao dịch lớn cần ngưỡng xác định theo phân phối dữ liệu. Một giao dịch lớn có thể là mở vị thế, đóng vị thế, phòng hộ, giao dịch chênh lệch hoặc thanh lý; không được tự gán ý định.

#### Cumulative Số giao dịch

Gộp các giao dịch gần nhau có thể hữu ích để nhìn cách lệnh được chia nhỏ, nhưng quy tắc thời gian và giá phải cố định, không được tạo câu chuyện tùy ý.

### Quy trình áp dụng

1. Đọc khối lượng và số giao dịch song song.
2. So sánh với phân phối của cùng chế độ.
3. Xem tiến triển giá trên mỗi hợp đồng hoặc mỗi giao dịch.
4. Đặt giao dịch lớn trong vị trí và chuỗi phản ứng.

### Sai lầm thường gặp

* Gọi một giao dịch lớn là “dòng tiền thông minh”.
* So sánh khối lượng thô giữa các thời điểm có thanh khoản khác nhau.
* Bỏ qua cách lệnh được chia nhỏ khi thực thi.

### Ghi nhớ

> Khối lượng nói về quy mô; số giao dịch nói về độ phân mảnh; giá nói về hiệu quả.

## Chương 27. Lần kiểm tra, tái kiểm tra và ký ức mức giá

Một mức giá được kiểm tra nhiều lần tạo ra lịch sử phản ứng. Lịch sử ấy có thể cho thấy sự mệt mỏi, tái bổ sung thanh khoản hoặc quá trình chuyển từ từ chối sang chấp nhận.

### Nội dung cốt lõi

#### Lần kiểm tra đầu tiên

Lần đầu giá đến một mốc tham chiếu thường có độ bất định cao và thanh khoản chưa bị kiểm tra. Phản ứng đầu không bảo đảm sẽ lặp lại.

#### Tái kiểm tra

Lần quay lại cho phép so sánh nỗ lực, độ lệch, thời gian và khả năng phục hồi. Đây là nền tảng của FAR hai lần thử.

#### Sự suy yếu của mốc tham chiếu

Nếu mỗi lần kiểm tra tạo phản ứng nhỏ hơn và giá ở lại gần mốc lâu hơn, giả thuyết mốc đang mất khả năng phân tách trở nên hợp lý hơn.

#### Tái bổ sung thanh khoản

Nếu khối lượng chủ động lặp lại nhưng tiến triển tiếp tục bị giới hạn và giá phục hồi, có thể tồn tại thụ động kháng cự hoặc tái bổ sung. Đây vẫn là giả thuyết.

#### Không có quy luật một chiều

Nhiều lần kiểm tra có thể làm mốc yếu, nhưng cũng có thể chứng minh mốc được bảo vệ. Chỉ so sánh kết quả mới có giá trị.

### Quy trình áp dụng

1. Đánh số lần kiểm tra.
2. Ghi nỗ lực và kết quả từng lần.
3. Đo thời gian giữa các lần.
4. So sánh POC cục bộ và sự duy trì.
5. Không dùng riêng số lần kiểm tra để kết luận.

### Sai lầm thường gặp

* Mặc định kiểm tra thứ ba sẽ phá.
* Mặc định kiểm tra nhiều là mốc mạnh.
* Không phân biệt kiểm tra trong cùng Episode và kiểm tra sau khi cấu trúc đã đổi.

### Ghi nhớ

> Ký ức mức giá nằm trong chuỗi phản ứng, không nằm trong con số lần kiểm tra đơn độc.

## Chương 28. Ứng viên hấp thụ

Hấp thụ mô tả trường hợp nỗ lực chủ động gặp lực cản thụ động và không tạo tiến triển tương xứng. Vì ý định của phía thụ động không thể quan sát trực tiếp, cách nói an toàn là ứng viên hấp thụ.

### Nội dung cốt lõi

#### Ứng viên hấp thụ phía mua

Áp lực bán chủ động lớn hoặc lặp lại tại đáy nhưng giá giảm rất ít, sau đó phục hồi và không xây được sự chấp nhận thấp hơn. Điều này phù hợp với giả thuyết bên mua thụ động đang chống đỡ.

#### Ứng viên hấp thụ phía bán

Áp lực mua chủ động lớn tại đỉnh nhưng giá tăng rất ít, không duy trì được trên mốc rồi quay lại. Điều này phù hợp với giả thuyết bên bán thụ động đang chống đỡ.

#### Điều kiện hỗ trợ

Áp lực chủ động lặp lại, khối lượng cao tại cực trị, tiến triển trên mỗi hợp đồng giảm, tái nhập, POC cục bộ không đi theo và phản ứng đối diện làm giả thuyết mạnh hơn.

#### Hấp thụ khác đảo chiều

Một phía có thể bị chặn tạm thời, nhưng sau đó thanh khoản thụ động cạn và giá tiếp tục. Ứng viên hấp thụ cần điều kiện kích hoạt theo cấu trúc, không tự tạo lệnh đảo chiều.

#### Hấp thụ khác cạn kiệt

Hấp thụ có nỗ lực lớn nhưng kết quả nhỏ. Cạn kiệt có nỗ lực suy giảm và không còn đủ lực tiếp tục.

### Quy trình áp dụng

1. Bắt đầu tại một vị trí có ý nghĩa.
2. Đo áp lực chủ động và tiến triển.
3. Tìm các lần kiểm tra lặp lại hoặc dấu hiệu không còn tạo thuận lợi cho chuyển động.
4. Chờ tái nhập hoặc điều kiện kích hoạt cấu trúc.
5. Đặt mức vô hiệu ngoài vùng thanh khoản thụ động đang cản giá.

### Sai lầm thường gặp

* Gọi mọi phân kỳ Delta là hấp thụ.
* Vào ngược ngay khi thấy khối lượng lớn.
* Không xem liệu vùng giá trị có đang xây theo hướng áp lực chủ động.

### Ghi nhớ

> Ứng viên hấp thụ là bằng chứng về sự bất tương xứng Nỗ lực–Kết quả, không phải bằng chứng chắc chắn về danh tính hay đảo chiều.

## Chương 29. Ứng viên cạn kiệt

Cạn kiệt mô tả việc bên đang thúc đẩy giá giảm dần nỗ lực và không còn tạo tiến triển như trước. Nó khác với việc bị một phía thụ động hấp thụ mạnh.



### Nội dung cốt lõi

#### Dấu hiệu

Khối lượng, số giao dịch hoặc áp lực chủ động giảm dần trong khi giá cố tạo cực trị mới; nhịp độ chậm; độ lệch ngắn; giá dễ quay lại vùng trước.

#### Tại cực trị

Cạn kiệt tại biên có giá trị hơn ở giữa phạm vi. Tuy nhiên, thị trường thanh khoản mỏng có thể di chuyển xa với khối lượng thấp, nên phải xét chế độ thanh khoản.

#### Không đồng nghĩa đảo chiều

Bên chủ động có thể nghỉ rồi tiếp tục. Cần hoạt động khởi xướng phía đối diện, tái nhập, phá OTF hoặc lấy lại mốc tham chiếu để xác nhận thay đổi.

#### So sánh với hấp thụ

Hấp thụ: nỗ lực lớn, kết quả nhỏ. Cạn kiệt: nỗ lực nhỏ dần, kết quả cũng yếu. Hai cơ chế có thể cùng xuất hiện ở các giai đoạn khác nhau của Episode.

### Quy trình áp dụng

1. So sánh ba nhịp liên tiếp.
2. Đo khối lượng, số giao dịch, delta và tiến triển.
3. Kiểm tra vị trí cấu trúc.
4. Chờ phản ứng phía đối diện hoặc tái nhập.
5. Tránh vào lệnh khi thanh khoản mỏng nếu thiếu xác nhận.

### Sai lầm thường gặp

* Gọi khối lượng thấp là cạn kiệt trong mọi chế độ.
* Bắt đáy chỉ vì Delta giảm.
* Không xem thời điểm tin tức hoặc thay đổi thanh khoản.

### Ghi nhớ

> Cạn kiệt nói động cơ đang hụt hơi; cấu trúc mới cho biết xe có thực sự quay đầu hay không.

## Chương 30. Nỗ lực và Kết quả

Nỗ lực–Kết quả là cầu nối giữa Order Flow và AMT. Không hỏi chỉ “bên nào bấm lệnh nhiều hơn”, mà hỏi “nỗ lực đó đã thay đổi cuộc đấu giá đến mức nào”.



### Nội dung cốt lõi

#### Vectơ nỗ lực

Nỗ lực có thể gồm khối lượng đã khớp, số giao dịch, áp lực chủ động đã phân loại, thời gian, tốc độ, số mức giá được giao dịch và các lần kiểm tra lặp lại.

#### Vectơ kết quả

Kết quả có thể gồm tiến triển ròng, mức mở rộng phạm vi, độ lệch thuận lợi hoặc bất lợi, sự dịch chuyển của vùng giá trị và POC, khả năng duy trì ngoài mốc và khả năng giữ sau nhịp hồi.

#### Lớn–Lớn

Nỗ lực lớn và kết quả lớn thường phù hợp với khả năng tạo thuận lợi tốt. Nhưng nếu diễn ra ngay trước rào cản lớn, dư địa lợi nhuận còn lại có thể thấp.

#### Lớn–Nhỏ

Nỗ lực lớn nhưng kết quả nhỏ là ứng viên phân kỳ. Nguyên nhân có thể là kháng cự thụ động, dòng lệnh đối diện, thanh khoản được bổ sung lại hoặc hấp thụ.

#### Nhỏ–Lớn

Kết quả lớn với nỗ lực nhỏ có thể xuất hiện khi thanh khoản mỏng, thiếu lực đối ứng hoặc có khoảng trống thanh khoản. Giá có thể tiếp diễn nhanh, nhưng rủi ro trượt giá và hồi mạnh cũng cao.

#### Nhỏ–Nhỏ

Hoạt động thấp và tiến triển thấp thường phản ánh vùng cân bằng hoặc giai đoạn chờ sự kiện. Đây là môi trường nên tránh giao dịch quá mức.

### Quy trình áp dụng

1. Định nghĩa cửa sổ đo.
2. Ghi nỗ lực thô.
3. Ghi kết quả thô.
4. So sánh với dữ liệu lịch sử trong cùng chế độ.
5. Đặt trong Vị trí và Episode.
6. Không gắn nhãn tốt hoặc xấu khi chưa có đường cơ sở so sánh.

### Sai lầm thường gặp

* Dùng một tỷ lệ duy nhất cho mọi chế độ.
* Gọi nỗ lực lớn–kết quả nhỏ là đảo chiều chắc chắn.
* Quên chi phí và mục tiêu khoảng trống.

### Ghi nhớ

> Nỗ lực chỉ có ý nghĩa khi được so với kết quả mà nó tạo ra.

## Chương 31. Khả năng tạo thuận lợi cho giao dịch

khả năng tạo thuận lợi cho giao dịch mô tả thị trường làm tốt đến đâu theo hướng cuộc đấu giá đang thử. Nó là kết luận tổng hợp từ hoạt động, tiến triển và sự duy trì, không phải một con số Delta.

### Nội dung cốt lõi

#### Khả năng tạo thuận lợi khỏe

Áp lực chủ động và khối lượng tạo tiến triển, phạm vi mở rộng, nhịp hồi giữ, POC/vùng giá trị dịch và giao dịch tiếp tục ở vùng mới.

#### Khả năng tạo thuận lợi suy yếu

Nỗ lực tăng nhưng tiến triển giảm, giá liên tục tái nhập, POC không đi theo hoặc vùng giá trị cũ được lấy lại.

#### Trong phá vỡ

Phá vỡ được khả năng tạo thuận lợi khi thị trường không chỉ xuyên biên mà còn tổ chức hoạt động ngoài biên. Nếu chỉ có nhịp đột biến, trạng thái vẫn chưa giải quyết.

#### Trong đảo chiều

Dòng lệnh đối diện chỉ có ý nghĩa khi làm giá tái nhập, duy trì và thay đổi cấu trúc. Một lần Delta đổi dấu ngắn không đủ.

#### Tính tương đối

Khả năng tạo thuận lợi phải được đánh giá theo chế độ và khung thời gian. Một đoạn 10 tick có thể là tiến triển tốt trong vùng cân bằng nhưng lại yếu trong một sự kiện mở rộng.

### Quy trình áp dụng

1. Xác định cuộc đấu giá đang được thử.
2. Đo nỗ lực theo hướng đó.
3. Đo kết quả và sự duy trì.
4. Kiểm tra vùng giá trị/POC.
5. Phân loại khỏe, suy yếu hoặc chưa rõ theo chính sách đã kiểm chứng.

### Sai lầm thường gặp

* Đồng nhất khả năng tạo thuận lợi với Delta cùng dấu.
* Dùng nhãn mà không xác định khung thời gian.
* Bỏ qua vùng cản và dư địa lợi nhuận còn lại.

### Ghi nhớ

> khả năng tạo thuận lợi cho giao dịch là chất lượng của tiến trình đấu giá, không phải độ lớn của một chỉ báo.

## Chương 32. Quét thanh khoản, kích hoạt dừng lỗ và người giao dịch mắc kẹt

Các câu chuyện về quét thanh khoản, kích hoạt dừng lỗ và người giao dịch mắc kẹt rất hấp dẫn vì dễ hình dung. Chúng cũng rất dễ bị lạm dụng. Chỉ sử dụng các nhãn này khi có dữ liệu phù hợp và chuỗi hậu quả quan sát được.

### Nội dung cốt lõi

#### Độ lệch ngoài mốc tham chiếu

Giá giao dịch ngoài đỉnh/đáy hoặc mốc tham chiếu chỉ là độ lệch. Nó không chứng minh dừng lỗ bị quét, thanh khoản bị săn hay người giao dịch bị mắc kẹt.

#### Quét thanh khoản

Một cú quét vi mô cần bằng chứng dòng lệnh chủ động lấy thanh khoản qua nhiều mức giá, tốc độ phù hợp và dữ liệu sổ lệnh hoặc Tape hỗ trợ. Quét thanh khoản có thể xuất hiện cả trong tiếp diễn lẫn thất bại.

#### Kích hoạt dừng lỗ

Lệnh dừng lỗ không phải lúc nào cũng được gắn nhãn công khai. Vì vậy, một đợt kích hoạt dừng lỗ thường chỉ là suy luận từ tốc độ, vị trí và chuỗi khớp; cần dùng thuật ngữ này thận trọng.

#### Người mua hoặc người bán mắc kẹt

Một nhóm chỉ có thể được gọi là ứng viên mắc kẹt khi họ đã chủ động tham gia, giá không tiến triển, thị trường đi ngược và vùng vào của họ không được lấy lại. Ta vẫn không biết danh tính hay trạng thái vị thế thực.

#### Hành vi sau sự kiện

Điều quan trọng không phải nhãn quét thanh khoản mà là khả năng duy trì của giá: giá có quay lại, giữ trong vùng cũ hay tiếp tục xây hoạt động ngoài?

### Quy trình áp dụng

1. Gọi sự kiện trung tính là độ lệch trước.
2. Kiểm tra dữ liệu tốc độ và sổ lệnh trước khi gọi là quét thanh khoản.
3. Theo dõi tái nhập và sự duy trì.
4. Chỉ gọi ứng viên mắc kẹt khi có thất bại và không lấy lại được vùng vào.
5. Ưu tiên FAR/AAC hơn câu chuyện săn dừng lỗ.

### Sai lầm thường gặp

* Gọi mọi cú phá đỉnh là quét thanh khoản.
* Vào ngược vì tin rằng “dừng lỗ đã quét xong”.
* Dùng câu chuyện người giao dịch mắc kẹt như một nguyên nhân chắc chắn.

### Ghi nhớ

> Đừng giao dịch câu chuyện săn dừng lỗ; hãy giao dịch kết quả của cuộc đấu giá sau khi giá đi qua mốc tham chiếu.

## Chương 33. DOM và vòng đời thanh khoản hiển thị

**DOM** (*Depth of Market*) là sổ lệnh theo mức giá. Nó cho biết lượng mua và bán giới hạn đang hiển thị quanh giá hiện tại.



### DOM cho biết gì?

* Khối lượng Bid và Ask đang hiển thị.
* Khoảng cách giữa giá mua tốt nhất và giá bán tốt nhất.
* Mức tập trung thanh khoản gần hoặc xa giá.
* Thanh khoản được thêm, bị rút hoặc bị khớp theo thời gian nếu công cụ lưu chuỗi cập nhật.

### DOM không cho biết gì?

Lệnh giới hạn có thể bị hủy, di chuyển, chia nhỏ hoặc che giấu. Một ảnh chụp DOM không chứng minh ý định thật và không xác nhận hướng.

### MBO DOM

**MBO** (*Market By Order*) hiển thị từng lệnh, kích thước, thời điểm, mã lệnh và vị trí hàng đợi khi nguồn dữ liệu hỗ trợ. Với nguồn dữ liệu, cần kiểm tra cấu hình dữ liệu và tắt chế độ gộp báo giá khi công cụ yêu cầu.

Các trường đáng quan sát:

&#x20;   Thời gian sống của lệnh
    Vị trí trong hàng đợi
    Số lần sửa đổi
    Phần đã khớp
    Phần bị hủy
    Hành vi tái nạp
    Khoảng cách tới giá
    Kết quả khi giá tiếp cận


MBO là kính hiển vi của vòng đời lệnh, không phải máy đọc ý định.

### Cách dùng trong phương pháp

DOM/MBO chỉ được đọc tại vùng mốc đã xác định. Điều quan trọng không phải “có lệnh lớn”, mà là lệnh đó tồn tại bao lâu, bị khớp hay rút, và giá phản ứng thế nào.

## Chương 34. Pulling và Stacking: thanh khoản bị rút và được thêm

**Stacking** là hiện tượng thanh khoản hiển thị được thêm vào một phía của sổ lệnh. **Pulling** là hiện tượng thanh khoản hiển thị bị rút khỏi một phía.



### Câu hỏi đúng

&#x20;   Thanh khoản được thêm có tồn tại hay biến mất nhanh?
    Giá có tiến tới phía đó không?
    Lệnh được khớp hay bị rút trước khi giá tới?
    Pulling xảy ra trước, cùng lúc hay sau chuyển động giá?
    Hiện tượng có lặp lại qua nhiều cập nhật không?


### Diễn giải có điều kiện

* Stacking bền ở Bid và được khớp mà giá không giảm có thể phù hợp với giả thuyết hỗ trợ thụ động.
* Pulling ở Ask trước một nhịp tăng có thể cho thấy cản trở hiển thị giảm, nhưng cũng có thể chỉ là điều chỉnh báo giá.
* Pulling/Stacking xảy ra sau khi giá đã chạy có thể là phản ứng, không phải nguyên nhân.

### Hàng rào

Không dùng một ảnh chụp. Cần chuỗi cập nhật, độ bền, khoảng cách tới giá, phần khớp và kết quả giá. Thanh khoản hiển thị không phải cam kết.

## Chương 35. Iceberg và các công cụ phát hiện lệnh ẩn

**Iceberg order** là lệnh giới hạn có phần hiển thị nhỏ hơn khối lượng thực, phần ẩn tiếp tục được tái nạp khi phần hiển thị được khớp.

### Công cụ phát hiện lệnh ẩn

Công cụ có thể hiển thị phía Bid/Ask, thời gian sống, khối lượng đã khớp, nhãn khối lượng và mức độ xác nhận. Mức xác nhận mạnh hơn xuất hiện khi việc tái nạp và ưu tiên hàng đợi được dữ liệu MBO thể hiện rõ; xác nhận gián tiếp phải được đọc thận trọng hơn.

### Công cụ phát hiện lệnh ẩn bổ sung

Mỗi công cụ phát hiện lệnh ẩn có thuật toán và yêu cầu dữ liệu riêng. Chỉ sử dụng khi hiểu dữ liệu đầu vào, cách xác nhận và giới hạn của mô hình; kết quả không phải sự thật tuyệt đối.

### Dùng để làm gì?

* Xác định nơi khối lượng chủ động liên tục gặp thanh khoản ẩn.
* Đo lượng đã khớp và khả năng giữ giá.
* Bổ sung bằng chứng cho Nỗ lực–Kết quả tại mốc.
* Theo dõi iceberg còn tồn tại, bị tiêu thụ hay bị hủy.

### Không được kết luận

* Iceberg Bid không tự tạo lệnh mua.
* Iceberg Ask không tự tạo lệnh bán.
* Iceberg có thể bị tiêu thụ hoàn toàn và giá vẫn xuyên qua.
* Không gán danh tính “tổ chức” hoặc mục đích mở/đóng vị thế.

## Chương 36. Nhận diện dừng lỗ và lệnh quét



### Nhận diện dừng lỗ

Công cụ nhận diện dừng lỗ gom các cụm giao dịch có đặc điểm phù hợp với việc lệnh dừng lỗ bị kích hoạt.

Nó cho biết cơ chế kích hoạt dừng lỗ có thể đã xảy ra, nhưng không cho biết lệnh đó dùng để thoát hay mở vị thế. Hiện tượng này cũng không tự chứng minh đảo chiều.

### Nhận diện lệnh quét

Công cụ nhận diện lệnh quét tìm dòng lệnh chủ động lấy thanh khoản qua nhiều mức giá trong thời gian ngắn. Một cú quét có thể đến từ lệnh thị trường, lệnh IOC/FOK hoặc lệnh giới hạn có khả năng khớp ngay.

Cú quét cho thấy xung lực lấy thanh khoản. Nó không đồng nghĩa:

* Cuộc đấu giá thất bại.
* Khẳng định chắc chắn có săn dừng lỗ.
* Giá phải tiếp tục.
* Giá phải đảo chiều.

### Cách đọc trong FAR và AAC

&#x20;   Quét ra ngoài biên
    + không xây được tiến triển
    + tái nhập và duy trì vùng cũ
    → hỗ trợ câu chuyện FAR

    Quét ra ngoài biên
    + tiến triển tốt
    + POC/vùng giá trị xây ngoài
    + không lấy lại vùng cũ
    → hỗ trợ câu chuyện AAC


Cùng một cú quét có ý nghĩa khác nhau tùy kết quả giá.

## Chương 37. Các chỉ báo tổng hợp về áp lực và mức động

### Áp lực thị trường

Chỉ báo áp lực thị trường tổng hợp áp lực mua và bán từ các giao dịch gần đây, thường đọc theo khối lượng hoặc nhịp độ Tape. Dữ liệu mới thường được cho trọng số lớn hơn dữ liệu cũ.

Dùng để:

* Nhìn xung lực ngắn hạn.
* So sánh áp lực mua và bán.
* Phát hiện khi nhịp khớp tăng đột ngột.

Không dùng để thay thế Episode hoặc sự chấp nhận.

### Sức mạnh thị trường và CVD

Các chỉ báo sức mạnh thị trường thường tổng hợp một hoặc nhiều thành phần như Delta, CVD, khối lượng hoặc thị trường liên quan. Trước khi dùng phải hiểu thuật toán, cách đặt lại, dữ liệu lịch sử, khả năng vẽ lại và độ trễ. Nếu không hiểu, chỉ xem đây là hộp đen tham khảo.

### Áp lực sổ lệnh

Chỉ báo áp lực sổ lệnh tổng hợp thanh khoản Bid/Ask đang hiển thị và thường giảm trọng số cho các mức xa giá. Nó mô tả cấu trúc sổ lệnh hiện tại, không phải giao dịch đã khớp.

### Mức động

Các công cụ mức động tạo mốc theo thuật toán riêng. Một mức chỉ được đưa vào bản đồ khi biết:

&#x20;   Nguồn dữ liệu dùng để tính
    Mức dựa trên giao dịch đã khớp hay thanh khoản hiển thị
    Tuổi của mức
    Số lần kiểm tra
    Phản ứng khi giá xuyên qua
    Mức có vẽ lại hay không


Các mức động chỉ là nguồn tạo mốc tham chiếu ứng viên, không phải hệ thống mua bán.

## Chương 38. Limit Tracing: giả thuyết nghiên cứu về chuỗi thanh khoản

Limit Tracing là giả thuyết rằng thanh khoản giới hạn bị rút, thêm hoặc di chuyển theo chuỗi khi giá tiến qua các mức. Mẫu ứng viên có thể gồm:

&#x20;   Rút báo giá tuần tự
    + dòng chủ động bám theo
    + giá tiến nhanh qua từng mức
    + sổ lệnh đối diện mỏng


### Vì sao khó xác nhận?

Ảnh DOM hoặc quan sát bằng mắt không đủ để phân biệt:

* Rút lệnh có chủ ý.
* Nhà tạo lập điều chỉnh báo giá bình thường.
* Thay đổi do giá cơ sở khác.
* Độ trễ dữ liệu.
* Gộp báo giá.
* Thị trường đơn giản mất thanh khoản.

Limit Tracing chỉ là biến nghiên cứu khi có MBO, mã lệnh, dấu thời gian và phương án giải thích thay thế. Không dùng như thiết lập vào lệnh.

## Chương 39. Cực trị thanh chưa hoàn tất và cực trị kém hoàn thiện\*

Khái niệm **cực trị thanh chưa hoàn tất**\* được quan sát trên Footprint Bid × Ask:

&#x20;   Tại đỉnh thanh vẫn có Bid khối lượng > 0
    → ứng viên đỉnh thanh chưa hoàn tất\*

    Tại đáy thanh vẫn có Ask khối lượng > 0
    → ứng viên đáy thanh chưa hoàn tất\*


Khái niệm này phụ thuộc cách phân loại Bid/Ask, loại thanh, cách gom tick, luồng dữ liệu, giao dịch bị thiếu, mẫu phiên và cấu hình nền tảng.



### Quyền sử dụng

Có thể dùng như:

* Mốc vi mô phụ.
* Mức cần quan sát khi giá quay lại.
* Bằng chứng phụ cho cực trị thiếu kết thúc gọn.
* Biến nghiên cứu trong nhật ký.
* Rào cản hoặc mục tiêu phụ khi có hội tụ.

Không dùng như:

* Nam châm bắt buộc.
* Điểm vào độc lập.
* Bằng chứng chắc chắn đảo chiều.
* Bằng chứng chắc chắn có người mắc kẹt.
* Lý do giữ một lệnh sai vì “giá phải quay lại”.

Đỉnh/đáy kém hoàn thiện của Market Profile và cực trị thanh chưa hoàn tất của Footprint là hai khái niệm khác nhau, dù cả hai đều gợi ý một dạng kết thúc chưa rõ.

# PHẦN IV — HỢP ĐỒNG, PHIÊN, SỰ KIỆN VÀ VỊ THẾ

## Chương 40. Hợp đồng, chuyển tháng và chế độ tham gia

GC là một cuộc đấu giá gần như liên tục, nhưng chất lượng tham gia thay đổi theo thời điểm. Phiên châu Á, phiên châu Âu, giai đoạn trước mở cửa Mỹ, thời điểm mở cửa Mỹ và sau cao điểm nên được xem như các **chế độ tham gia**, không phải những thị trường hoàn toàn tách biệt.



### Kiểm tra hợp đồng

Trước phiên cần ghi:

&#x20;   Hợp đồng hiện tại
    Hợp đồng kế tiếp
    Ngày đáo hạn
    Khối lượng của hai hợp đồng
    Trạng thái chuyển tháng
    Mẫu phiên
    Chính sách nối hoặc đặt lại Composite/CVD


Trong giai đoạn chuyển tháng:

* Không so sánh khối lượng thô giữa hai hợp đồng như thể cùng một phân phối.
* Ngưỡng giao dịch lớn và Tape phải được xác định riêng theo từng hợp đồng.
* Composite nhiều ngày cần quy tắc nối hoặc đặt lại.
* OI và basis với CFD phải được xem lại.

### Chế độ tham gia

&#x20;   Qua đêm mỏng
    Phiên Á hoạt động
    Chuyển tiếp châu Âu
    Phiên châu Âu hoạt động
    Trước phiên Mỹ
    Mở cửa phiên Mỹ
    Phiên Mỹ hoạt động
    Sau phiên Mỹ
    Chế độ sự kiện


Mỗi chế độ có phân phối riêng về khối lượng, số giao dịch, Delta, tốc độ Tape, chênh lệch mua bán, độ sâu và kích thước giao dịch lớn. Không dùng cùng một ngưỡng cho mọi thời điểm.

## Chương 41. Sự kiện, vĩ mô và thị trường bên ngoài

Bối cảnh vĩ mô không tạo điểm vào trong ngày. Nó thay đổi kỳ vọng, rủi ro và cách đọc thanh khoản.



### Các nhóm sự kiện quan trọng với vàng

* Lạm phát: CPI, PCE và các thước đo liên quan.
* Việc làm: NFP, thất nghiệp, tiền lương và đơn xin trợ cấp.
* Chính sách tiền tệ: quyết định lãi suất, biên bản và phát biểu ngân hàng trung ương.
* Tăng trưởng và thanh khoản: GDP, PMI, điều kiện tài chính.
* Địa chính trị và rủi ro hệ thống.

### Chế độ sự kiện

&#x20;   Bình thường
    Trước sự kiện
    Xung lực sự kiện
    Khám phá giá sau sự kiện
    Ổn định sau sự kiện


Trước sự kiện, chênh lệch mua bán có thể mở rộng, độ sâu bị rút và ảnh chụp DOM kém đáng tin. Không đuổi theo xung lực đầu chỉ vì Tape mạnh. Sau xung lực, chờ nhịp hồi, Episode, sự chấp nhận hoặc tái nhập và sự hình thành vùng giá trị.

### Thị trường bên ngoài

Lợi suất, đồng USD, bạc, cổ phiếu hoặc năng lượng chỉ là bối cảnh. Tương quan phải dùng thay đổi hoặc lợi suất trong cửa sổ rõ ràng; không suy luận nhân quả từ một lần đồng hướng.

## Chương 42. Open Interest và giới hạn suy luận vị thế

**Open Interest (OI)** là số hợp đồng còn mở. Nó khác với khối lượng giao dịch:

&#x20;   Khối lượng
    → đếm hoạt động đã giao dịch

    OI
    → đếm số hợp đồng còn mở sau quá trình bù trừ


Mỗi hợp đồng tương lai luôn có một phía mua và một phía bán. OI tăng không trực tiếp cho biết bên nào “thông minh hơn”. Delta cho biết phía chủ động khớp lệnh, không cho biết chắc giao dịch đó mở hay đóng vị thế.



### Ma trận diễn giải thận trọng

&#x20;   OI tăng + Delta dương
    → người mua chủ động hoạt động trong lúc số hợp đồng mở tăng
    → phù hợp với ứng viên mua khởi xướng
    ≠ chứng minh Long mới đang kiểm soát

    OI tăng + Delta âm
    → người bán chủ động hoạt động trong lúc số hợp đồng mở tăng
    → phù hợp với ứng viên bán khởi xướng
    ≠ chứng minh Short mới đang kiểm soát


Với GC, dữ liệu OI chính thức cuối ngày thường phù hợp hơn cho bối cảnh nhiều ngày. OI trong ngày chỉ được dùng khi nguồn, nhịp cập nhật và độ chính xác đã được xác minh.

OI không phải điểm vào và không phải bằng chứng về sự chấp nhận.

## Chương 43. COT, nhà tạo lập và rủi ro tồn kho

### COT

**COT** (*Commitments of Traders*) là báo cáo vị thế tần suất thấp theo nhóm tham gia. Nó phù hợp để nghiên cứu:

* Bối cảnh tuần và nhiều tuần.
* Vị thế cực đoan theo lịch sử.
* Thay đổi chế độ dài hơn.

COT không dùng cho điểm vào trong ngày và không có quyền phủ quyết hành vi giá hiện tại.

### Nhà tạo lập và rủi ro tồn kho

Nhà tạo lập cung cấp thanh khoản, thu chênh lệch Bid/Ask và quản trị tồn kho cùng rủi ro biến động. Trước sự kiện, họ có thể giảm độ sâu, mở rộng chênh lệch mua bán hoặc thay đổi độ bền báo giá.

Không kể câu chuyện “nhà tạo lập luôn lái giá”. Các dấu hiệu hợp lý để quan sát gồm:

&#x20;   Độ sâu giảm
    Chênh lệch mua bán mở rộng
    Độ bền báo giá giảm
    Tỷ lệ hủy lệnh tăng
    Tape và DOM mất đồng bộ


Đây là bối cảnh rủi ro thanh khoản, không phải tín hiệu hướng.

Trong trụ Options, không được suy dealer Long/Short Gamma chỉ từ Call/Put OI hoặc từ một công thức GEX không công bố giả định. Khi phía vị thế không quan sát được, hệ thống phải chạy nhiều kịch bản, hoặc chỉ dùng độ tập trung tuyệt đối và độ nhạy không dấu. Một câu chuyện phòng hộ chỉ được nâng quyền khi có dữ liệu, phản ứng giá và Order Flow phù hợp.

# PHẦN V — TRỤ OPTIONS: ĐỊNH GIÁ RỦI RO, DÒNG GIAO DỊCH VÀ EXPOSURE

## Chương 44. Vai trò, quyền hạn và kiến trúc của trụ Options

Trụ Options không phải một nhóm đường hỗ trợ/kháng cự và cũng không phải công cụ dự đoán hướng. Nó là hệ thống đọc cách thị trường quyền chọn **định giá biến động**, **phân bổ rủi ro theo kỳ hạn và giá thực hiện**, **tạo giao dịch mới**, và **thay đổi độ nhạy khi giá, thời gian hoặc IV chuyển động**.

### Câu hỏi mà trụ Options sở hữu

Trụ Options có quyền trả lời:

* IV đang cao, thấp hay thay đổi so với chính phân phối lịch sử phù hợp?
* Kỳ hạn nào đang mang premium biến động lớn nhất?
* Skew đang nghiêng về rủi ro tăng, giảm hay cân bằng?
* Volume, Open Interest và thay đổi OI đang tập trung ở đâu?
* Dòng giao dịch nào nổi bật theo premium, size, strike, expiry và phía chủ động?
* Delta, Gamma, Vega, Theta và các độ nhạy khác tập trung ở vùng nào?
* Nếu áp dụng một giả định vị thế cụ thể, các kịch bản hedging có thể thay đổi ra sao?
* Expected move của từng horizon là bao nhiêu theo phương pháp đã chọn?

Trụ Options không có quyền tự trả lời:

* Giá chắc chắn sẽ tăng hay giảm.
* Nhà tạo lập đang chắc chắn Long hay Short Gamma.
* Call volume lớn đồng nghĩa mua tăng giá.
* Put volume lớn đồng nghĩa phòng hộ giảm giá.
* Một strike OI lớn bắt buộc phải ghim giá.
* Một vùng exposure Options bắt buộc phải chặn hoặc hút giá.

### Bốn lớp của trụ Options

#### Lớp 1: Dữ liệu hợp đồng và thị trường

Gồm underlying futures, mã quyền chọn, Call/Put, strike, expiry, DTE, hệ số hợp đồng, kiểu thực hiện/đáo hạn, báo giá Bid/Ask, last, volume, OI và timestamp.

#### Lớp 2: Dữ liệu phân tích

Gồm IV, moneyness, Delta, Gamma, Vega, Theta, Rho và các giá trị do nguồn hoặc mô hình tính.

#### Lớp 3: Cấu trúc tổng hợp

Gồm volatility surface, term structure, skew, risk reversal, convexity, expected move, concentration map, volume/OI heatmap và exposure map.

#### Lớp 4: Suy luận có điều kiện

Gồm dealer hedging, pinning, acceleration, vanna flow, charm flow, event premium hoặc inventory pressure. Đây là lớp có quyền thấp nhất và phải đi kèm giả định, kịch bản thay thế và độ tin cậy.

### Quan hệ với AMT và Order Flow

&#x20;   AMT
    → cho biết giá đang ở đâu, cuộc đấu giá đang làm gì và mức giá mới có được chấp nhận hay không

    OPTIONS
    → cho biết rủi ro được định giá thế nào, kỳ hạn/strike nào nhạy cảm và cơ chế nào có thể được kích hoạt

    ORDER FLOW
    → cho biết áp lực thực thi trên futures có đang tạo kết quả hay không

    QUẢN TRỊ
    → quyết định liệu lợi thế, thanh khoản và rủi ro có đủ để tham gia


Options được đọc trước phiên để xây kịch bản, được cập nhật trong phiên để phát hiện thay đổi chế độ, và được kiểm chứng bằng AMT cùng Order Flow khi giá đến vùng có ý nghĩa.

Trong giai đoạn chuẩn bị, AMT và Options có thể được đọc song song vì chúng trả lời hai nhóm câu hỏi khác nhau. Sự song song trong vận hành không có nghĩa hai trụ cùng sở hữu quyền phán quyết acceptance. Khi giá đã tương tác, trạng thái hiện tại phải được xác định từ Episode, kết quả giá và bằng chứng chấp nhận; Options chuyển sang vai trò hỗ trợ kịch bản và quản trị.

### Ma trận quyền hạn theo câu hỏi

|Câu hỏi|Trụ có quyền chính|Trụ hỗ trợ|Điều không được làm|
|-|-|-|-|
|Giá đang ở đâu và có được chấp nhận không?|AMT|Order Flow|Options phủ quyết sự chấp nhận rõ|
|Áp lực chủ động có hiệu quả không?|Order Flow|AMT|Dùng flow tách khỏi vị trí|
|Thị trường định giá biến động ra sao?|Options|Sự kiện, realized volatility|Dùng IV như dự báo chắc chắn|
|Rủi ro tập trung ở strike/expiry nào?|Options|AMT|Gọi concentration là tường giá|
|Có nên thực thi không?|Quản trị rủi ro|Cả ba trụ|Cộng điểm tùy ý rồi vào lệnh|

### Quy trình áp dụng

1. Xác minh dữ liệu Options có thể dùng.
2. Tách dữ liệu quan sát, dữ liệu tính và suy luận.
3. Xây trạng thái Options theo kỳ hạn và theo vùng giá.
4. Đặt các vùng nhạy cảm lên bản đồ AMT.
5. Viết hai phía của kịch bản, không viết một kết luận bắt buộc.
6. Khi giá tương tác, dùng Episode và Order Flow để kiểm chứng.
7. Chỉ điều chỉnh risk, target hoặc kiểu vào bằng quy tắc đã kiểm chứng trước.

### Sai lầm thường gặp

* Đổi tên GEX thành Options nhưng vẫn chỉ dùng vài đường ngang.
* Coi toàn bộ chain là một kỳ hạn duy nhất.
* Gộp volume, OI và OI change thành cùng một khái niệm.
* Gán Call là bullish và Put là bearish.
* Gọi mô hình dealer exposure là vị thế quan sát được.
* Không lưu snapshot theo thời điểm nên backtest có look-ahead.

### Ghi nhớ

> Trụ Options không kể tương lai. Nó mô tả giá của sự bất định, nơi rủi ro tập trung và những cơ chế có thể trở nên quan trọng nếu giá kích hoạt chúng.

## Chương 45. Dữ liệu Options, ánh xạ hợp đồng và kiểm soát chất lượng

Một hệ thống Options mạnh bắt đầu bằng dữ liệu đúng. Chain đầy đủ nhưng ánh xạ sai underlying, sai expiry hoặc dùng báo giá cũ vẫn tạo ra một bản đồ bóng bẩy nhưng vô nghĩa.

### Bộ trường dữ liệu tối thiểu

#### Định danh hợp đồng

* product và exchange;
* option symbol;
* underlying futures symbol và contract month;
* Call/Put;
* strike;
* expiration date và expiration time;
* DTE theo quy ước nhất quán;
* contract multiplier;
* exercise/settlement style nếu có ảnh hưởng tới mô hình.

#### Báo giá

* Bid, Ask, Bid size, Ask size;
* last trade và timestamp;
* quote timestamp;
* spread tuyệt đối và spread theo phần trăm premium;
* trạng thái two-sided, one-sided, crossed hoặc locked.

#### Hoạt động

* volume hiện tại;
* trade size;
* premium hoặc notional;
* Open Interest;
* thay đổi Open Interest;
* nguồn và thời điểm công bố OI.

#### Phân tích

* IV;
* Delta, Gamma, Vega, Theta, Rho;
* moneyness;
* forward/underlying dùng trong mô hình;
* lãi suất và giả định mô hình nếu tự tính.

### Ánh xạ quyền chọn vào futures vàng

Options vàng là quyền trên một hợp đồng futures cụ thể. Vì vậy phải ghi rõ:

&#x20;   option expiry
    → underlying futures contract
    → giá futures dùng để tính moneyness và Greeks
    → hợp đồng GC đang dùng cho AMT/Order Flow
    → basis nếu thực thi trên CFD


Không được dùng Spot XAUUSD thay cho futures underlying trong mô hình mà không có quy tắc basis. Không được gộp options của nhiều underlying month như thể chúng cùng một tài sản tức thời.

### Kỳ hạn và thời điểm đáo hạn

Ngày đáo hạn không đủ. Hệ thống phải biết thời điểm đáo hạn, phiên giao dịch liên quan và thời gian còn lại chính xác. DTE có thể thay đổi mạnh ý nghĩa của Gamma, Theta và expected move, đặc biệt ở các kỳ hạn rất ngắn.

Các bucket nghiên cứu gợi ý:

* 0DTE;
* 1–3 DTE;
* 4–10 DTE;
* 11–30 DTE;
* 31–90 DTE;
* trên 90 DTE.

Bucket chỉ là cấu hình nghiên cứu, không phải quy luật phổ quát.

### Cổng chất lượng báo giá

Một option chỉ được dùng để tính IV/Greeks khi đáp ứng chính sách dữ liệu, ví dụ:

* có Bid và Ask hợp lệ;
* spread không vượt giới hạn theo moneyness và DTE;
* quote age không quá cũ;
* premium không âm hoặc phi lý;
* không crossed market chưa xử lý;
* underlying timestamp đủ gần option timestamp;
* không dùng last trade cũ thay cho mid hiện tại.

Khi không có two-sided market, hệ thống phải hạ độ tin cậy hoặc loại khỏi surface. Không điền IV bằng 0 cho dữ liệu thiếu.

### OI là dữ liệu có nhịp cập nhật riêng

Open Interest thường không phải dữ liệu thời gian thực. Khi nghiên cứu intraday, phải sử dụng OI đã biết tại đúng thời điểm đó. OI cuối ngày không được quay ngược lại dùng cho các quyết định trước khi nó được công bố.

### Phiên bản hóa dữ liệu

Mỗi snapshot Options phải lưu:

&#x20;   observed\_at
    trade\_date
    source
    source\_version
    underlying\_contract
    underlying\_price
    chain\_hash hoặc snapshot\_id
    quote\_quality\_policy
    model\_version
    rate/input assumptions


Không có point-in-time snapshot thì không thể replay trung thực.

### Trạng thái dữ liệu

|Trạng thái|Ý nghĩa|Quyền sử dụng|
|-|-|-|
|Tốt|Đủ trường, timestamp đồng bộ, quote hợp lệ|Dùng toàn bộ module phù hợp|
|Hạn chế|Thiếu flow hoặc OI change, surface còn đủ|Dùng cấu trúc biến động, hạ quyền suy luận|
|Thận trọng|Spread rộng, quote cũ, underlying lệch|Chỉ dùng concentration lớn và bối cảnh|
|Không dùng|Sai mapping, dữ liệu hỏng hoặc look-ahead|Loại trụ Options khỏi quyết định|

### Quy trình áp dụng

1. Xác minh product, underlying và contract month.
2. Chuẩn hóa timezone và timestamp.
3. Áp dụng cổng chất lượng báo giá.
4. Gắn trạng thái availability cho từng trường.
5. Tách OI theo ngày công bố và flow intraday.
6. Lưu snapshot bất biến.
7. Chỉ chạy module mà dữ liệu cho phép.

### Sai lầm thường gặp

* Tính IV từ last trade cũ.
* Dùng OI cuối ngày cho backtest trong ngày.
* Gộp các expiry hoặc underlying month không tương thích.
* Điền 0 cho dữ liệu thiếu.
* Không lưu version của mô hình Greeks.

### Ghi nhớ

> Options không mạnh hơn chất lượng chain. Dữ liệu sai biến một mô hình tinh vi thành máy sản xuất tự tin giả.

## Chương 46. Kỳ hạn, moneyness và cấu trúc biến động hàm ý

Biến động hàm ý là mức biến động làm giá mô hình khớp với premium quan sát theo các giả định đã chọn. Nó là **giá của rủi ro trong quyền chọn**, không phải lời hứa về biên độ tương lai.

### Moneyness

Moneyness mô tả vị trí strike so với underlying hoặc forward:

* ITM;
* ATM;
* OTM;
* hoặc theo Delta/moneyness chuẩn hóa.

Mọi so sánh skew phải dùng cùng quy ước. So sánh strike tuyệt đối giữa các ngày khi giá vàng đã dịch xa có thể gây sai.

### ATM IV

ATM IV là mốc nền để so sánh biến động giữa expiry. Phải ghi rõ cách chọn ATM:

* strike gần futures nhất;
* delta gần 50;
* nội suy tại forward;
* hoặc phương pháp của nguồn.

Không trộn các phương pháp trong cùng nghiên cứu.

### Cấu trúc kỳ hạn

**Term structure** so sánh IV giữa các expiry. Nó giúp nhận diện:

* premium sự kiện ở kỳ hạn gần;
* kỳ vọng bất định kéo dài;
* front-end căng hơn back-end;
* hoặc trạng thái kỳ hạn gần rẻ tương đối.

Không suy rằng front IV cao chắc chắn giá sẽ chạy mạnh. IV có thể cao vì bảo hiểm đắt, sự kiện đã được định giá hoặc thanh khoản kém.

### Skew

Skew mô tả IV khác nhau theo moneyness. Các cách đo có thể gồm:

* IV của put/call cùng Delta;
* risk reversal;
* chênh IV giữa wing và ATM;
* slope nội suy của surface.

Skew cho biết phía nào của phân phối được trả premium tương đối cao hơn. Nó không tự nói người mua hay người bán đang đúng.

### Convexity và butterfly

Butterfly/convexity mô tả mức đắt tương đối của hai cánh so với vùng ATM. Nó có thể phản ánh tail demand, cấu trúc cung/cầu hoặc đặc điểm mô hình. Không dùng một giá trị cực đoan mà không kiểm tra thanh khoản của wing.

### Volatility surface

Surface là hàm của:

&#x20;   IV = f(strike hoặc moneyness, expiry, thời điểm)


Một surface hữu ích cần:

* quote filtering;
* nội suy có kiểm soát;
* cờ ngoại suy;
* kiểm tra tính hợp lý;
* lưu model version;
* không tạo độ chính xác giả ở vùng không có thanh khoản.

### Thay đổi surface

Không chỉ đọc mức tuyệt đối. Theo dõi:

* ΔATM IV;
* Δterm slope;
* Δskew;
* Δwing convexity;
* dịch chuyển theo sự kiện;
* tốc độ thay đổi trong phiên.

Thay đổi đồng thời của price, IV và skew có giá trị hơn một snapshot đơn lẻ.

### Regime biến động gợi ý

|Regime|Dấu hiệu ứng viên|Cách sử dụng|
|-|-|-|
|IV thấp và ổn định|ATM IV thấp tương đối, surface ít đổi|Không tự bán biến động; chỉ kỳ vọng biên hẹp khi AMT đồng thuận|
|IV tăng front-end|Kỳ hạn gần tăng nhanh|Tăng cảnh giác sự kiện/đột biến và trượt giá|
|Upside skew bid|Call wing đắt tương đối|Ghi rủi ro phía tăng được trả premium, không tự Long|
|Downside skew bid|Put wing đắt tương đối|Ghi rủi ro phía giảm được trả premium, không tự Short|
|Surface hỗn hợp|expiry/skew không đồng thuận|Hạ độ chắc chắn, phân tách horizon|

### Quy trình áp dụng

1. Chọn underlying và expiry hợp lệ.
2. Chuẩn hóa moneyness.
3. Tính hoặc lấy ATM IV theo một phương pháp cố định.
4. Xây term structure và skew.
5. So với lịch sử theo cùng regime sản phẩm.
6. Ghi mức, thay đổi và độ tin cậy.
7. Kết nối với lịch sự kiện và AMT, không tạo lệnh độc lập.

### Sai lầm thường gặp

* Gọi IV là dự báo chính xác.
* So skew bằng strike tuyệt đối khi underlying đã đổi lớn.
* Dùng wing không thanh khoản để kết luận tail demand.
* Gộp mọi expiry thành một IV duy nhất.
* Chọn lookback sau khi nhìn kết quả.

### Ghi nhớ

> Surface cho biết thị trường đang định giá hình dạng của sự bất định. Giá và cuộc đấu giá cho biết hình dạng ấy có đang được hiện thực hóa hay không.

## Chương 47. Volume, Open Interest và Options Flow

Volume đo hoạt động trong khoảng thời gian. Open Interest đo số hợp đồng còn mở sau quá trình bù trừ. Options Flow mô tả các giao dịch quyền chọn đã xảy ra. Ba khái niệm liên quan nhưng không thay thế nhau.

### Volume

Volume cho biết mức hoạt động, không cho biết vị thế còn tồn tại sau phiên. Volume lớn có thể là:

* mở vị thế mới;
* đóng vị thế cũ;
* chuyển kỳ hạn;
* một chân của spread;
* hedge của vị thế khác;
* giao dịch hai chiều lặp lại.

### Open Interest

OI là số hợp đồng còn mở của một series. OI lớn cho biết concentration lịch sử, không cho biết chắc ai Long/Short hoặc ai là dealer.

### Thay đổi Open Interest

ΔOI giúp phân biệt vùng hoạt động mới xây hoặc đang tháo dỡ, nhưng chỉ khi nhịp công bố đúng và việc so sánh cùng series hợp lệ.

Ma trận thận trọng:

|Volume|ΔOI sau bù trừ|Diễn giải hợp lệ|
|-|-:|-|
|Cao|Tăng|Hoạt động mới có thể đã làm tăng số hợp đồng mở|
|Cao|Gần 0|Có thể chủ yếu chuyển tay, mở/đóng bù nhau hoặc spread|
|Cao|Giảm|Hoạt động có thể liên quan đóng vị thế|
|Thấp|OI lớn|Tồn kho lịch sử còn tập trung, flow mới hạn chế|

Không suy hướng chỉ từ bảng này.

### Options Flow

Nếu có dữ liệu giao dịch chi tiết, mỗi trade nên gồm:

* timestamp;
* Call/Put;
* strike;
* expiry/DTE;
* price, Bid, Ask và mid tại thời điểm khớp;
* size;
* premium/notional;
* underlying price;
* IV và Greeks tại thời điểm trade;
* aggressor classification;
* block/spread/strategy flags nếu có.

### Phân loại phía chủ động

Trade gần Ask có thể phù hợp mua chủ động; gần Bid có thể phù hợp bán chủ động. Nhưng spread rộng, quote stale, complex order và price improvement có thể làm phân loại sai. Luôn có trạng thái **không xác định**.

### Không đồng nhất Call/Put với hướng

* Mua Call có thể là bullish, hedge short, một chân spread hoặc volatility trade.
* Bán Call có thể là covered, spread, closing hoặc short-vol.
* Mua Put có thể là bearish, hedge long hoặc tail insurance.
* Bán Put có thể là bullish, spread hoặc đóng hedge.

Vì vậy hệ thống phải mô tả **giao dịch quan sát được**, không nhảy thẳng sang ý định.

### Nhận diện giao dịch nhiều chân

Nếu nguồn có strategy/complex-order identifiers, phải nhóm các chân. Nếu không có, chỉ được gắn nhãn **ứng viên spread** dựa trên timestamp, size, expiry, strike và premium tương quan. Không loại bỏ chân tùy ý để kể câu chuyện một chiều.

### Thước đo Options Flow gợi ý

* premium mua/bán chủ động theo expiry;
* delta-adjusted notional;
* gamma hoặc vega traded theo strike/expiry;
* call/put flow theo moneyness;
* flow anomaly so với phân phối cùng giờ và DTE;
* repeat flow;
* concentration migration;
* response của IV và underlying sau trade.

Mọi thước đo phải ghi công thức và giới hạn.

### Flow Episode

Tương tự Auction Episode, một **Options Flow Episode** theo dõi:

&#x20;   cụm giao dịch bắt đầu
    → lặp lại theo strike/expiry
    → IV/OI/underlying phản ứng
    → flow tiếp diễn, đảo chiều hoặc hết hiệu lực


Một trade lớn đơn lẻ không đủ để gọi thay đổi chế độ.

### Quy trình áp dụng

1. Tách volume, OI và ΔOI.
2. Phân loại trade side với trạng thái không xác định.
3. Nhóm complex trades khi có dữ liệu.
4. Chuẩn hóa theo premium, Delta/Gamma/Vega và lịch sử.
5. Theo dõi phản ứng IV cùng underlying.
6. Xây Flow Episode thay vì kể chuyện từ một print.
7. Chỉ đưa flow vào luận điểm khi horizon phù hợp với giao dịch futures.

### Sai lầm thường gặp

* Call volume cao đồng nghĩa bullish.
* Volume/OI cao đồng nghĩa vị thế mới.
* Gọi mọi trade tại Ask là mua sạch.
* Bỏ qua spread và roll.
* Dùng OI công bố sau phiên cho quyết định trước đó.

### Ghi nhớ

> Options Flow cho biết hoạt động đã xảy ra. Ý định, vị thế ròng và cơ chế phòng hộ vẫn là kết luận cần thêm bằng chứng.

## Chương 48. Greeks, GEX và exposure theo kịch bản

Greeks mô tả độ nhạy của giá quyền chọn với các biến đầu vào. Exposure tổng hợp nhân Greeks với số hợp đồng, hệ số hợp đồng và một quy ước vị thế. Chính quy ước vị thế quyết định việc bản đồ có thể được đọc đến đâu.

### Delta

Delta mô tả độ nhạy của premium quyền chọn với thay đổi underlying theo mô hình. Delta cũng thường được dùng để chuẩn hóa moneyness hoặc quy đổi notional tương đương. Nó không phải xác suất chắc chắn và không cho biết ai đang nắm vị thế.

### Gamma

Gamma mô tả tốc độ thay đổi Delta khi underlying thay đổi. Gamma thường cao hơn gần ATM và gần expiry, nhưng kết quả cụ thể phụ thuộc mô hình, IV và thời gian.

### Vega

Vega mô tả độ nhạy của premium với thay đổi IV. Vega thường quan trọng hơn ở kỳ hạn dài hơn so với Gamma ngắn hạn, nhưng phải đánh giá theo đơn vị và multiplier của nguồn.

### Theta

Theta mô tả độ nhạy theo thời gian khi các yếu tố khác được giữ theo giả định mô hình. Theta không phải dòng tiền chắc chắn và có thể bị lấn át bởi price/IV move.

### Vanna và Charm

* **Vanna**: thay đổi Delta khi IV thay đổi, hoặc thay đổi Vega khi underlying thay đổi tùy quy ước.
* **Charm**: thay đổi Delta khi thời gian trôi qua.

Đây là công cụ nâng cao. Chỉ dùng khi công thức, dấu, đơn vị và horizon được xác minh.

### Exposure không dấu

Khi không biết phía vị thế, hệ thống vẫn có thể tính:

* absolute Gamma concentration;
* absolute Delta/Vega concentration;
* OI-weighted sensitivity;
* strike/expiry sensitivity density.

Các thước đo này cho biết **nơi nhạy cảm**, không cho biết hướng hedging.

### Exposure có dấu

Signed GEX/DEX/VEX chỉ được sử dụng khi có một trong các điều kiện:

* nguồn cung cấp vị thế theo phía đáng tin cậy;
* dữ liệu giao dịch và open/close đủ để xây inventory có kiểm chứng;
* hoặc hệ thống công bố rõ giả định dealer/customer và chạy nhiều scenario.

Mỗi kết quả phải chứa:

&#x20;   exposure\_name
    formula\_version
    position\_side\_assumption
    included\_expiries
    underlying\_price
    contract\_multiplier
    timestamp
    confidence


### GEX là mô-đun, không phải trụ

GEX có thể được tính theo strike và expiry, rồi tổng hợp thành profile hoặc zero-gamma estimate. Nhưng:

* không mặc định Call GEX dương và Put GEX âm mà không nêu quy ước;
* không mặc định dealer đối diện toàn bộ OI;
* không gọi GEX Flip là ranh giới vật lý;
* không gọi Call Resistance/Put Support là khái niệm chuẩn nếu đó chỉ là nhãn vendor;
* không gộp mọi expiry mà không xem contribution.

### Ba scenario vị thế tối thiểu

|Scenario|Giả định|Cách dùng|
|-|-|-|
|Dealer đối diện customer flow|Dealer giữ phía ngược flow/inventory ước tính|Nghiên cứu hedging candidate|
|Dealer cùng dấu với exposure quan sát|Kịch bản thay thế|Kiểm tra độ nhạy của kết luận|
|Không biết phía|Chỉ dùng absolute concentration|Mặc định an toàn khi dữ liệu thiếu|

Nếu kết luận đảo hoàn toàn khi đổi scenario, trạng thái đúng là **model-sensitive**, không phải bullish/bearish.

### Dynamic exposure

Exposure phải được tính lại khi:

* underlying thay đổi đáng kể;
* IV surface thay đổi;
* thời gian trôi qua, đặc biệt gần expiry;
* OI/flow snapshot cập nhật;
* contract mapping đổi.

Một file GEX tĩnh không đại diện cho dynamic exposure nếu các đầu vào đã thay đổi.

### Quy trình áp dụng

1. Xác minh Greeks và đơn vị.
2. Tính concentration không dấu trước.
3. Chỉ thêm dấu khi giả định vị thế rõ.
4. Phân tách contribution theo expiry.
5. Recalculate theo price/IV/time.
6. So sánh nhiều scenario.
7. Ghi độ ổn định của kết luận.
8. Đưa exposure lên bản đồ AMT như vùng nhạy cảm, không như lệnh.

### Sai lầm thường gặp

* Coi GEX là dữ liệu trực tiếp của sàn.
* Dùng một công thức bí mật như sự thật.
* Không ghi multiplier hoặc đơn vị.
* Bỏ qua expiry contribution.
* Gọi dealer Short Gamma chắc chắn từ Put/Call OI.
* Dùng zero gamma như điểm đảo chiều bắt buộc.

### Ghi nhớ

> Exposure cho biết mô hình nhạy với điều gì. Chỉ giá, thời gian và giao dịch thực tế mới cho biết kịch bản nào đang trở thành hành vi thị trường.

## Chương 49. Expected move, vùng nhạy cảm và Options Regime

Trụ Options cần một đầu ra có cấu trúc, không phải một rừng chỉ số. Đầu ra trung tâm là **Options Regime**, đi kèm expected move, vùng nhạy cảm và độ tin cậy.

### Expected move

Expected move phải ghi phương pháp, horizon và timestamp. Các phương pháp có thể gồm:

* ATM straddle quy đổi;
* IV × căn bậc hai thời gian × underlying;
* phân phối nội suy từ surface;
* mô hình riêng đã kiểm chứng.

Không trộn các phương pháp và không gọi expected move là biên bắt buộc. Giá có thể vượt ngoài; xác suất thực tế phụ thuộc phân phối, jumps, skew và regime.

### Nhiều horizon

Tối thiểu nên có:

* đến cuối phiên;
* đến expiry gần nhất;
* đến sự kiện kế tiếp;
* 1 tuần;
* 30 ngày hoặc horizon phù hợp.

Mỗi horizon phải dùng expiry hoặc nội suy hợp lý.

### Vùng nhạy cảm

Vùng nhạy cảm có thể sinh từ:

* concentration OI/volume;
* absolute Gamma/Vega/Delta;
* signed exposure scenario;
* strike gần expected move boundary;
* nơi skew hoặc surface thay đổi nhanh;
* expiry magnet candidate gần thời điểm đáo hạn;
* flow concentration mới.

Mỗi vùng phải có `source`, `horizon`, `strength`, `age`, `assumption` và `invalid\_when`.

### Options Regime

Bộ trạng thái gợi ý:

1. **DATA\_UNUSABLE**: dữ liệu không đủ hoặc sai mapping.
2. **NEUTRAL**: không có cấu trúc nổi bật hoặc tín hiệu hỗn hợp.
3. **STABILITY\_CANDIDATE**: surface ổn định, concentration gần cân bằng và scenario hedging có thể chống chuyển động.
4. **EXPANSION\_CANDIDATE**: front IV tăng, concentration nhạy, scenario hedging có thể đi cùng chuyển động hoặc liquidity risk tăng.
5. **EVENT\_PREMIUM**: kỳ hạn chứa sự kiện được định giá nổi bật.
6. **EXPIRY\_DOMINANT**: Gamma/Theta và concentration kỳ hạn gần chi phối.
7. **FLOW\_SHIFT**: flow mới làm thay đổi IV, skew hoặc concentration.
8. **MIXED**: các expiry hoặc thước đo xung đột.

Tên trạng thái mô tả **ứng viên môi trường**, không mô tả hướng giá.

### Confluence với AMT

|AMT|Options Regime|Kỳ vọng hợp lệ|
|-|-|-|
|Cân bằng|Stability candidate|Luân phiên có thể được ưu tiên nếu Episode xác nhận|
|Cân bằng|Expansion candidate|Cảnh giác phá biên; không fade tự động|
|Khám phá giá|Expansion candidate|AAC có thể có đường đi rộng hơn nếu Order Flow xác nhận|
|Khám phá giá|Stability candidate|Có rào cản/giảm tốc ứng viên, nhưng không phủ quyết acceptance|
|Chuyển tiếp|Mixed/Event|Hạ độ chắc chắn, chờ bằng chứng|

### Thay đổi regime

Regime chỉ thay đổi khi có tiêu chí định trước, ví dụ:

* ATM IV hoặc term slope vượt ngưỡng phân phối;
* skew đổi đáng kể;
* flow anomaly lặp lại;
* expected move reset;
* expiry contribution đổi;
* exposure scenario đổi dấu ổn định;
* dữ liệu mất chất lượng.

Không đổi regime chỉ vì một trade hoặc một tick.

### Output chuẩn

```yaml
options\_regime: EXPANSION\_CANDIDATE
as\_of: 2026-01-01T12:00:00Z
data\_quality: GOOD
horizon: INTRADAY
atm\_iv\_state: ELEVATED
term\_structure: FRONT\_PREMIUM
skew\_state: UPSIDE\_BID
expected\_move:
  session: null
  nearest\_expiry: null
sensitive\_zones: \[]
flow\_state: DEVELOPING
exposure\_scenarios: \[]
known\_unknowns: \[]
prohibited\_conclusions: \[]
```

### Quy trình áp dụng

1. Tính expected move theo nhiều horizon.
2. Xây vùng nhạy cảm có metadata.
3. Phân loại regime.
4. Ghi các thành phần đồng thuận và xung đột.
5. So regime với AMT.
6. Chờ Order Flow tại vùng tương tác.
7. Cập nhật khi dữ liệu hoặc horizon thực sự đổi.

### Sai lầm thường gặp

* Gọi expected move là support/resistance.
* Gọi stability candidate là chắc chắn đi ngang.
* Gọi expansion candidate là tín hiệu breakout.
* Trộn horizon 0DTE với 30D.
* Không lưu regime as-of-time.

### Ghi nhớ

> Regime là bản tóm tắt có điều kiện của trụ Options. Nó phải giúp ra quyết định rõ hơn, không che mất dữ liệu gốc.

## Chương 50. Tích hợp Options với AMT, Order Flow và quản trị

Options không được đặt cuối quy trình như con dấu xác nhận. Nó tham gia hai thời điểm khác nhau: **trước phiên để xây kịch bản** và **trong phiên để cập nhật áp lực tiềm tàng**. Phán quyết giao dịch vẫn đến từ vị trí, Episode, sự chấp nhận, Order Flow và quản trị.

### Trình tự đúng

&#x20;   1. Tính toàn vẹn dữ liệu
    2. AMT: bản đồ, trạng thái và vị trí
    3. Options: regime, expected move, kỳ hạn và vùng nhạy cảm
    4. Episode tại mốc AMT hoặc vùng hội tụ
    5. Order Flow: nỗ lực và kết quả
    6. Sự chấp nhận/tái chấp nhận
    7. Luận điểm, vô hiệu, mục tiêu và risk


### Options trong FAR

Options có thể hỗ trợ FAR khi:

* AMT đang kiểm tra biên cân bằng;
* Options regime là stability candidate hoặc expiry-dominant quanh vùng hội tụ;
* flow mới không làm IV/skew mở rộng theo hướng phá;
* giá thử ngoài expected/concentration zone nhưng không xây acceptance;
* Order Flow cho nỗ lực lớn nhưng kết quả hạn chế;
* giá tái nhập và tái chấp nhận vùng cũ.

Options không đủ để gọi FAR nếu tái chấp nhận chưa xuất hiện.

### Options trong AAC

Options có thể hỗ trợ AAC khi:

* AMT có acceptance ngoài vùng;
* Options regime là expansion candidate, event repricing hoặc flow shift phù hợp horizon;
* IV/term/skew thay đổi đồng thời với price discovery;
* vùng phía trước ít concentration hoặc concentration đang dịch theo giá;
* Order Flow tạo tiến triển và nhịp hồi giữ phía mới.

Options không đủ để gọi AAC chỉ vì GEX scenario đổi dấu hoặc giá vượt expected move.

### Options trong luân phiên

Khi AMT cân bằng, surface ổn định, expected move chưa bị tái định giá và concentration nằm gần trung tâm, Options có thể tăng kỳ vọng luân phiên. Điểm vào vẫn phải đến từ biên AMT, Episode và hành vi đáp ứng.

### Options trong sự kiện

Trước sự kiện:

* front IV có thể tăng;
* spread Options và futures có thể mở rộng;
* expected move có thể thay đổi;
* flow có thể là hedge chứ không phải hướng.

Sau sự kiện:

* theo dõi IV crush hoặc repricing;
* kiểm tra surface và skew có tái cấu trúc;
* không dùng snapshot trước tin như bản đồ tĩnh nếu underlying đã dịch xa;
* chờ AMT hình thành vùng giá trị và Order Flow ổn định.

### Giao dịch phản ứng tại vùng Options

Nhiều người giao dịch sử dụng strike tập trung, expected move, gamma
concentration, vùng exposure hoặc các mốc Options khác để chuẩn bị giao dịch
phản ứng. Cách làm này phù hợp với Tam Trụ nếu "phản ứng" được hiểu là hành vi
thực sự xuất hiện trên futures, không phải giả định rằng một đường Options bắt
buộc phải giữ giá.

Chuỗi đúng là:

    Options xác định vùng nhạy cảm
    → Đặt vùng đó lên bản đồ AMT
    → Chờ giá tương tác
    → Mở Episode
    → Quan sát Order Flow và kết quả giá
    → Đánh giá acceptance, tái nhập hoặc thất bại
    → Chỉ sau đó mới chọn thực thi

Một vùng Options có thể trở thành:

- vùng chuẩn bị quan sát phản ứng;
- rào cản tiềm năng trên đường đi;
- nơi cần tăng yêu cầu xác nhận;
- ứng viên cho luân phiên hoặc khuếch đại;
- vùng điều chỉnh target và quản trị.

Nó không tự trở thành hỗ trợ, kháng cự hoặc điểm đảo chiều.

#### Phản ứng hợp lệ

Một ứng viên phản ứng có thêm giá trị khi:

- vùng Options được ánh xạ đúng underlying, expiry và horizon;
- vùng nằm tại hoặc gần một vị trí AMT có ý nghĩa;
- giá tương tác nhưng nỗ lực theo hướng xuyên vùng không tạo kết quả tương xứng;
- xuất hiện tái nhập, từ chối hoặc bảo vệ vùng có khả năng duy trì;
- Order Flow phù hợp với kết quả giá;
- điều kiện vô hiệu và không gian mục tiêu được xác định trước.

#### Phản ứng chưa đủ bằng chứng

Trạng thái vẫn là chưa được giải quyết khi:

- giá chỉ chạm vùng rồi bật trong thời gian rất ngắn;
- Order Flow mạnh nhưng kết quả giá chưa rõ;
- giá giằng co quanh vùng mà chưa xây acceptance theo phía nào;
- snapshot Options đã cũ hoặc regime đang thay đổi;
- vùng Options không có vị trí AMT hỗ trợ;
- basis hoặc dữ liệu giữa các thị trường không đáng tin cậy.

#### Khi vùng Options thất bại

Nếu giá xuyên qua vùng Options, duy trì giao dịch phía bên kia, POC hoặc vùng giá
trị bắt đầu dịch theo và Order Flow tiếp tục tạo tiến triển, không được tiếp tục
giao dịch ngược chỉ vì vùng đó từng được gọi là wall, flip hoặc concentration.

Sự thất bại của một vùng Options là thông tin. Nó có thể cho thấy:

- kịch bản exposure đã sai;
- giả định dealer positioning không phù hợp;
- regime đang chuyển;
- horizon đang dùng không đúng;
- dòng thực thi hiện tại mạnh hơn cơ chế ổn định được giả định.

Không được di chuyển vùng hoặc đổi câu chuyện sau khi giá đã xuyên qua chỉ để bảo
vệ mô hình.

### Ghi nhớ

> Giao dịch phản ứng của Options không phải giao dịch một đường Options. Đó là
> giao dịch phản ứng đã được giá, Episode và Order Flow chứng minh tại một vùng
> mà Options giúp ta chuẩn bị trước.

### Khi Options xung đột với giá

Nếu Options gợi ý ổn định nhưng giá xây acceptance và Order Flow tạo tiến triển, **giá thắng đối với trạng thái hiện tại**. Options được dùng để:

* kiểm tra rào cản;
* hạ kỳ vọng follow-through nếu quy tắc thống kê ủng hộ;
* điều chỉnh kiểu vào hoặc mục tiêu theo policy;
* không đổi hướng tùy ý.

Nếu Options gợi ý khuếch đại nhưng giá thất bại và tái chấp nhận vùng cũ, không được đuổi breakout.

### Quyền điều chỉnh quản trị

Trụ Options được phép điều chỉnh:

* yêu cầu xác nhận;
* loại setup được ưu tiên;
* thời gian giữ;
* target corridor;
* risk budget;
* điều kiện không giao dịch;

chỉ khi quy tắc đã được kiểm chứng, ghi trước và cùng horizon. Nó không được tự nới dừng lỗ hoặc tăng rủi ro vì một narrative dealer hedging.

### Ma trận xử lý

|AMT|Order Flow|Options|Cách xử lý|
|-|-|-|-|
|FAR rõ|Tái nhập được bảo vệ|Stability/neutral|Triển khai FAR theo policy|
|FAR rõ|Tái nhập được bảo vệ|Expansion conflict|Giữ phán quyết giá, giảm kỳ vọng hoặc chờ xác nhận thêm theo policy|
|AAC rõ|Tiến triển tốt|Expansion/flow shift|Bối cảnh hỗ trợ follow-through|
|AAC rõ|Tiến triển tốt|Stability/concentration trước mặt|Không hủy AAC; đánh giá đường đi, target và kiểu vào|
|AMT chưa rõ|Order Flow mâu thuẫn|Bất kỳ|Không giao dịch|
|AMT và Order Flow rõ|Options không dùng được|Không bịa; vận hành bằng hai trụ còn lại và hạ confidence nếu policy yêu cầu||

### Quy trình áp dụng

1. Lưu Options snapshot trước phiên.
2. Xây regime và vùng nhạy cảm theo horizon.
3. Đặt lên bản đồ AMT.
4. Khi giá đến vùng, mở Episode.
5. Theo dõi Order Flow và acceptance.
6. Phân loại Options là hỗ trợ, xung đột, trung tính hoặc không dùng.
7. Áp dụng risk/target policy đã kiểm chứng.
8. Lưu snapshot và quyết định trước khi biết kết quả.

### Sai lầm thường gặp

* Bắt Options “xác nhận hướng”.
* Dùng dealer story để phủ nhận giá.
* Dùng dữ liệu Options cuối ngày cho quyết định intraday trước đó.
* Đổi risk tùy cảm xúc dưới danh nghĩa regime.
* Tối ưu quá nhiều feature Options trên mẫu nhỏ.

### Ghi nhớ

> AMT cho biết điều thị trường đã chấp nhận. Options cho biết sự bất định và áp lực tiềm tàng được định giá thế nào. Order Flow cho biết áp lực nào đang thực sự đi qua thị trường. Giao dịch chỉ tồn tại khi ba câu trả lời ghép thành một luận điểm có thể vô hiệu và quản trị.

# PHẦN VI — HỢP NHẤT TAM TRỤ

## Chương 51. Vai trò của ba trụ

Tam Trụ không phải ba bộ chỉ báo cộng điểm. Đây là ba miền bằng chứng trả lời ba câu hỏi khác nhau. Một trụ chỉ có quyền trong phạm vi dữ liệu mà nó thực sự quan sát hoặc tính toán được.

### Nội dung cốt lõi

#### AMT sở hữu vị trí và trạng thái đấu giá

AMT xác định vùng giá trị, cân bằng hoặc khám phá giá, mốc tham chiếu, bối cảnh nhiều phiên và sự chấp nhận đã hiện thực hóa. Không có vị trí, tín hiệu vi mô và Options đều mất phần lớn ý nghĩa giao dịch.

#### Options sở hữu định giá rủi ro và cấu trúc tiềm tàng

Options mô tả IV, term structure, skew, expected move, volume/OI, flow và exposure theo kịch bản. Nó cho biết thị trường quyền chọn đang trả giá cho loại bất định nào và vùng nào có thể nhạy cảm. Nó không sở hữu phán quyết acceptance.

#### Order Flow sở hữu nỗ lực thực thi

Order Flow đo giao dịch đã khớp, phía chủ động, nhịp, cụm và kết quả giá. Nó kiểm tra xem cơ chế được AMT/Options đặt ra có đang biểu hiện trên futures hay không.

#### Quản trị sở hữu quyền tham gia

Một luận điểm đúng về thị trường vẫn có thể không được giao dịch nếu dữ liệu, spread, basis, sự kiện hoặc risk không đạt chuẩn.
### Quyền hạn thay đổi theo giai đoạn, không phải thứ bậc cố định



Không nên hiểu Tam Trụ bằng một công thức cố định như:



&#x20;   AMT > Options > Order Flow



trong mọi thời điểm và đối với mọi câu hỏi.



Ba trụ không tranh cùng một quyền phán quyết. Quyền chính thay đổi theo giai đoạn của quá trình phân tích, nhưng mỗi trụ vẫn chỉ được kết luận trong phạm vi dữ liệu mà nó sở hữu.



#### Trước phiên: AMT và Options được đọc song song nhưng khác quyền



Trong giai đoạn chuẩn bị, AMT và Options có thể được xem là hai lớp bối cảnh song song:



- **AMT** xây bản đồ cuộc đấu giá, xác định vùng giá trị, cấu trúc, trạng thái và các mốc cần quan sát.

- **Options** xác định horizon, định giá biến động, expected move, term structure, skew, concentration, flow state và các vùng nhạy cảm theo kịch bản.



AMT không được dùng để suy ra cấu trúc biến động hàm ý hoặc exposure Options. Options cũng không được tự tạo vị trí giao dịch khi chưa đặt lên bản đồ AMT.



Một vùng Options có thể là nơi đáng chuẩn bị quan sát, nhưng chưa phải điểm vào và chưa phải bằng chứng rằng giá sẽ bị chặn, bị hút hoặc đảo chiều.



#### Khi giá tương tác: Episode trở thành cầu nối



Khi giá đến một mốc AMT, một vùng Options nhạy cảm hoặc một vùng hội tụ giữa hai lớp, phải mở Episode để theo dõi toàn bộ lần tương tác.



Chuỗi kiểm chứng đúng là:



&#x20;   Vùng nhạy cảm từ Options hoặc mốc AMT

&#x20;   → Giá tiếp cận

&#x20;   → Episode bắt đầu

&#x20;   → Order Flow cho biết nỗ lực thực thi

&#x20;   → Kết quả giá cho biết nỗ lực có hiệu quả hay không

&#x20;   → Acceptance hoặc tái chấp nhận quyết định trạng thái cuộc đấu giá



Nguồn gốc của vùng có thể đến từ Options, nhưng phản ứng chỉ có giá trị giao dịch khi nó được biểu hiện trên futures qua giá, thời gian, khối lượng, Order Flow và khả năng duy trì.



#### Tại vị trí: Episode, Order Flow và Acceptance giữ quyền phán quyết



Khi giá đã đến vùng cần quan sát:



- Episode cho biết lần thử đang phát triển như thế nào;

- Order Flow cho biết bên nào đang chủ động và nỗ lực có tạo tiến triển hay không;

- Acceptance cho biết giá mới đã được duy trì, bị từ chối hay vẫn chưa được giải quyết.



Một phản ứng nhanh tại strike hoặc vùng exposure mới chỉ là quan sát ban đầu. Nó chưa đủ để gọi FAR, AAC hoặc đảo chiều nếu thị trường chưa chứng minh khả năng duy trì.



#### Khi ra quyết định: Options có thể chặn hoặc điều chỉnh giao dịch



Một luận điểm AMT có thể hợp lệ nhưng vẫn không được phép giao dịch nếu trụ Options hoặc quản trị cho thấy:



- rủi ro sự kiện quá gần;

- biến động đang được tái định giá mạnh;

- expected move hoặc target corridor không còn đủ không gian;

- concentration lớn nằm ngay trên đường đi;

- spread, thanh khoản hoặc basis không phù hợp;

- horizon của luận điểm không khớp horizon Options.



Trong trường hợp này cần phân biệt hai kết luận:



&#x20;   Luận điểm thị trường vẫn hợp lệ

&#x20;   ≠

&#x20;   Giao dịch hiện tại được phép thực hiện



Options có thể làm tăng yêu cầu xác nhận, giảm khối lượng, rút ngắn thời gian giữ, thay đổi target corridor hoặc chuyển quyết định thành Chờ/Không giao dịch. Điều đó không có nghĩa Options đã phủ nhận trạng thái AMT.



#### Phán quyết cuối về trạng thái đã hiện thực hóa



Nếu Options gợi ý ổn định nhưng giá xây acceptance ngoài vùng và Order Flow tạo tiến triển, acceptance của giá giữ quyền phán quyết đối với trạng thái hiện tại.



Nếu Options gợi ý khuếch đại nhưng giá thất bại, tái nhập và tái chấp nhận vùng cũ, không được tiếp tục đuổi theo kịch bản phá vỡ.



Options có thể thay đổi kỳ vọng về đường đi và quản trị, nhưng không được viết lại điều giá đã chứng minh.



### Ma trận quyền hạn theo giai đoạn



| Giai đoạn | Quyền chính | Vai trò của các lớp còn lại |

|---|---|---|

| Chuẩn bị trước phiên | AMT và Options song song, khác miền | AMT xây bản đồ; Options xây regime, horizon và vùng nhạy cảm |

| Giá tiếp cận vùng | Episode | Ghi nhận lần thử, độ lệch, thời gian và hoạt động |

| Kiểm tra phản ứng | Order Flow và kết quả giá | Đánh giá nỗ lực thực thi có tạo tiến triển hay không |

| Phán quyết trạng thái | Acceptance thuộc miền AMT | Chọn FAR, AAC, Luân phiên hoặc Chưa giải quyết |

| Quyết định tham gia | Quản trị rủi ro | Options có thể hỗ trợ, điều chỉnh hoặc chặn giao dịch |

| Thực thi | Kỹ thuật vào lệnh | Không được đảo ngược phán quyết của các lớp cao hơn |



### Ghi nhớ



> Options có thể dẫn đường đến nơi cần quan sát. AMT xác nhận thị trường đã thực sự đi đến đâu. Order Flow cho biết bước đi đó được thực thi bằng nỗ lực nào. Quản trị quyết định liệu câu chuyện ấy có đáng để tham gia hay không.

### Thứ tự vận hành

&#x20;   AMT tạo bản đồ và trạng thái
    → Options tạo regime, horizon và vùng nhạy cảm
    → Episode mở khi giá tương tác
    → Order Flow kiểm tra nỗ lực–kết quả
    → Acceptance quyết định câu chuyện
    → Quản trị quyết định tham gia


### Khi thiếu một trụ

* Thiếu Options: vẫn có thể vận hành AMT + Order Flow, nhưng không được bịa expected move, dealer exposure hoặc Options regime.
* Thiếu Order Flow: có thể xây bối cảnh AMT + Options, nhưng điểm vào cần hạ quyền hoặc chờ bằng chứng giá khác theo policy.
* Thiếu AMT: không được dùng Options/Order Flow để tự tạo vị trí.

### Quy trình áp dụng

1. Ghi trạng thái dữ liệu của từng trụ.
2. Đọc AMT.
3. Đọc Options theo horizon.
4. Mở Episode tại vùng có ý nghĩa.
5. Đọc Order Flow.
6. Ghi đồng thuận, xung đột và dữ liệu còn thiếu.
7. Chỉ quyết định khi risk gate cho phép.

### Sai lầm thường gặp

* Cộng điểm ba trụ rồi gọi là xác suất.
* Đòi ba trụ cùng một “hướng”.
* Dùng Options để đo acceptance.
* Dùng Order Flow để suy volatility term structure.
* Từ chối mọi giao dịch khi một trụ không khả dụng dù policy cho phép.

### Ghi nhớ

> AMT là địa hình đã được giao dịch, Options là bản đồ giá của sự bất định, Order Flow là dấu chân đang xuất hiện. Không bắt một bản đồ làm công việc của bản đồ khác.

## Chương 52. Quy trình đọc thị trường từ trên xuống

Một quy trình cố định giảm sự chú ý chọn lọc. Người giao dịch không được mở Footprint hoặc Options dashboard rồi tìm tín hiệu trước khi biết dữ liệu và thị trường đang ở đâu.

### Nội dung cốt lõi

#### Lớp 1: Dữ liệu và sản phẩm

Xác minh hợp đồng GC, underlying của options, expiry, timestamp, phiên, sự kiện, basis GC–CFD và trạng thái chất lượng từng nguồn.

#### Lớp 2: Bản đồ đấu giá

Xác định Composite, vùng giá trị, POC, Initial Balance, phiên qua đêm và các mốc quan trọng.

#### Lớp 3: Trạng thái AMT

Phân loại Cân bằng, Khám phá giá hoặc Chuyển tiếp; tách nhiều phiên và trong ngày.

#### Lớp 4: Options Intelligence

Ghi Options regime, horizon, expected move, term structure, skew, concentration, flow state và exposure scenario. Nếu dữ liệu không đủ, ghi module nào không dùng.

#### Lớp 5: Episode

Khi giá đến mốc AMT hoặc vùng hội tụ, ghi lần thử, độ lệch, thời gian, hoạt động và cấu trúc cục bộ.

#### Lớp 6: Order Flow

Đánh giá phía chủ động, cụm giao dịch, các lần kiểm tra lặp lại và Nỗ lực–Kết quả.

#### Lớp 7: Acceptance và quyết định

Chọn FAR, AAC, Luân phiên, Chờ hoặc không giao dịch; xác định vô hiệu, target corridor, risk và thời hạn.

### Quy trình áp dụng

1. Kiểm tra dữ liệu.
2. Xây bản đồ AMT.
3. Phân loại trạng thái.
4. Xây Options regime theo horizon.
5. Chờ giá đến vị trí.
6. Theo dõi Episode.
7. Đọc Order Flow và acceptance.
8. Áp dụng policy Options–risk nếu có.
9. Quyết định Giao dịch, Chờ hoặc Không giao dịch.

### Sai lầm thường gặp

* Bắt đầu từ một trade Options lớn.
* Nhìn GEX/expected move trước vùng giá trị.
* Trộn horizon 0DTE và nhiều tuần.
* Chuyển từ dashboard sang lệnh mà thiếu Episode.

### Ghi nhớ

> Quy trình từ trên xuống không làm thị trường đơn giản hơn. Nó làm rõ lớp nào đang nói và lớp nào chưa có quyền kết luận.

## Chương 53. Vị trí trước tín hiệu

Cùng một tín hiệu có giá trị khác nhau tùy vị trí và horizon. Vị trí quyết định dữ liệu đang xuất hiện giữa vùng thỏa thuận, tại biên hay trong khám phá giá; Options cho biết vùng đó có thêm độ nhạy nào nhưng không tự tạo vị trí.

### Nội dung cốt lõi

#### Giữa vùng giá trị

Delta, Imbalance hoặc một flow Options nổi bật giữa vùng giá trị thường dễ bị nhiễu bởi luân phiên. Nếu không có khoảng trống mục tiêu, không biến tín hiệu vi mô thành giao dịch.

#### Tại biên vùng giá trị

VAH, VAL, biên Composite, IB Đỉnh/Đáy và cực trị là nơi thích hợp mở Episode. Tại đây theo dõi tái nhập, acceptance phía ngoài hoặc chưa giải quyết.

#### Ngoài vùng giá trị

Order Flow ngoài vùng phải được đánh giá theo khả năng xây hoạt động. Options expansion candidate chỉ tăng chú ý tới follow-through; nó không thay bằng chứng acceptance.

#### Tại vùng Options nhạy cảm

Concentration, expected-move boundary, expiry cluster hoặc exposure zone chỉ đáng chú ý khi:

* horizon phù hợp;
* dữ liệu còn tươi;
* phương pháp tính rõ;
* vùng gần hoặc hội tụ với mốc AMT;
* có hành vi giá để mở Episode.

#### Không gian mục tiêu

Một điểm vào tốt vẫn có thể là giao dịch xấu nếu mốc AMT, concentration hoặc expected-move repricing nằm quá gần. Rào cản Options là biến của đường đi, không phải mục tiêu bắt buộc.

### Quy trình áp dụng

1. Xác định trong, tại biên hay ngoài vùng giá trị.
2. Ghi mốc AMT gần nhất.
3. Ghi vùng Options gần nhất và horizon.
4. Chỉ đọc Footprint chi tiết khi vị trí có ý nghĩa.
5. Loại giao dịch nếu đường đi không đủ không gian.

### Sai lầm thường gặp

* Mua bán Delta giữa vùng giá trị.
* Dùng strike OI lớn như lệnh chờ.
* Dùng expected move như tường giá.
* Gộp vùng Options xa horizon vào intraday.

### Ghi nhớ

> Vị trí biến dữ liệu thành thông tin. Options làm giàu vị trí, không phát minh vị trí sau khi tín hiệu đã xuất hiện.

## Chương 54. Bối cảnh trước thực thi

Bối cảnh trả lời thị trường đang ưu tiên luân phiên, khám phá giá, chuyển tiếp hay tái định giá biến động. Thực thi chỉ bắt đầu khi bối cảnh, vị trí và dữ liệu đủ rõ.

### Nội dung cốt lõi

#### Bối cảnh nhiều phiên

Composite, dịch chuyển vùng giá trị và POC cho biết cuộc đấu giá lớn đang cân bằng hay di chuyển.

#### Bối cảnh trong ngày

Vị trí mở cửa, Initial Balance, One-Time Framing, vùng giá trị đang phát triển và tốc độ cho biết câu chuyện hiện tại.

#### Bối cảnh Options

Options regime, term structure, skew, expected move, expiry concentration và flow state cho biết sự bất định đang được định giá thế nào. Phải ghi horizon và độ tin cậy.

#### Bối cảnh Episode

Một lần thử ngoài vùng, tái nhập hoặc acceptance đang phát triển tạo khuôn cho cách đọc Order Flow.

#### Bối cảnh sự kiện và thanh khoản

Tin, spread, depth và basis ảnh hưởng khả năng thực thi. Options front-end premium có thể báo sự kiện đã được định giá nhưng không bảo đảm chuyển động thực tế.

#### Xung đột

Khi nhiều phiên, trong ngày, Options và Order Flow không đồng thuận, trạng thái đúng có thể là Chờ. Xung đột phải được ghi theo câu hỏi, không ép tất cả thành Long/Short.

### Quy trình áp dụng

1. Viết bối cảnh nhiều phiên.
2. Viết bối cảnh trong ngày.
3. Ghi Options regime và horizon.
4. Ghi Episode đang sống.
5. Ghi Order Flow và dữ liệu còn thiếu.
6. Chỉ chọn kiểu thực thi phù hợp.

### Sai lầm thường gặp

* Vào lệnh trước rồi mới viết bối cảnh.
* Dùng Options để thay hướng AMT.
* Bỏ qua event premium và liquidity.
* Gọi xung đột là “xác nhận ngược”.

### Ghi nhớ

> Bối cảnh không cho điểm vào. Nó quyết định loại điểm vào, mức xác nhận và mức rủi ro nào được phép tồn tại.

## Chương 55. Từ mốc tham chiếu đến Episode

Mốc tham chiếu không phải điểm vào. Nó là nơi một cuộc kiểm tra bắt đầu. Episode biến mốc tham chiếu thành một dòng thời gian có thể so sánh.

### Nội dung cốt lõi

#### Vai trò

Biên trên, biên dưới, đường trung tâm và hành lang có hình học khác nhau. FAR/AAC phù hợp nhất với các biên; luân phiên phù hợp với đường trung tâm.

#### Tương tác

Ghi lần chạm đầu, tốc độ, mức độ hoạt động và phía chủ động. Không vào lệnh chỉ vì giá chạm mốc.

#### Lần thử đấu giá ngoài vùng

Đo độ lệch ngoài mốc, thời lượng và hoạt động. Ghi số lần thử.

#### Tái nhập

Xác định hình học quay vào và tốc độ. Sau đó kiểm tra khả năng duy trì.

#### Cấu trúc cục bộ

POC cục bộ/vùng giá trị giúp biết hoạt động đang xây phía nào.

#### Kết quả

FAR, AAC, luân phiên hoặc chưa được giải quyết. Kết quả không được chọn trước.

### Quy trình áp dụng

1. Chọn vai trò của mốc.
2. Ghi dấu thời gian tương tác.
3. Đo các thước đo ngoài vùng.
4. Đánh dấu tái nhập.
5. Theo dõi POC cục bộ.
6. Chờ kết quả.

### Sai lầm thường gặp

* Vẽ mốc tham chiếu sau khi giá phản ứng.
* Không phân biệt biên với đường trung tâm.
* Bỏ lịch sử lần thử.

### Ghi nhớ

> Một mốc tham chiếu chỉ trở thành cơ hội khi Episode tạo hình học và bằng chứng phù hợp.

## Chương 56. Từ Episode đến bằng chứng chấp nhận

Sự chấp nhận không được xác nhận bằng cảm giác. Ta thu thập nhiều nhóm bằng chứng và xem chúng cùng xây một câu chuyện hay mâu thuẫn.

### Nội dung cốt lõi

#### Các tỷ lệ ngoài vùng

Tỷ lệ thời gian, khối lượng và số giao dịch ngoài vùng mô tả phần hoạt động nằm ngoài mốc. Mỗi tỷ lệ phải có mẫu số rõ ràng và không có ngưỡng phổ quát.

#### Dịch chuyển POC cục bộ

POC cục bộ dịch ra ngoài ủng hộ sự chấp nhận; quay vào ủng hộ tái chấp nhận.

#### Quá trình xây vùng giá trị

Vùng giá trị cục bộ tách khỏi vùng giá trị cũ là bằng chứng mạnh hơn một nhịp đột biến.

#### Sự duy trì

Giá có ở lại sau nhịp hồi hay nhanh chóng khôi phục vùng cũ? Sự duy trì quyết định chất lượng.

#### Khả năng lấy lại vùng giá trị cũ

Lấy lại bền vững ủng hộ FAR; không lấy lại được ủng hộ AAC.

#### Chưa được giải quyết

Bằng chứng cân bằng hoặc thay đổi liên tục thì chưa giải quyết.

### Quy trình áp dụng

1. Thu các thước đo thô.
2. Không gắn đỉnh/đáy cảm tính.
3. So các nhóm bằng chứng.
4. Ghi rõ những trường dữ liệu còn thiếu.
5. Chỉ kết luận khi chính sách phân loại đã đủ điều kiện.

### Sai lầm thường gặp

* Dùng một tỷ lệ làm phán quyết.
* Gán giá trị 0 cho dữ liệu không khả dụng.
* Gọi tái nhập về mặt hình học là tái chấp nhận ổn định.

### Ghi nhớ

> Sự chấp nhận là kết quả hội tụ của khả năng duy trì, hoạt động và cấu trúc.

## Chương 57. Từ bằng chứng đến Order Flow

Sau khi biết cuộc đấu giá đang được thử, Order Flow được dùng để kiểm tra ai đang gây áp lực và việc gây áp lực có hiệu quả.

### Nội dung cốt lõi

#### Tại mốc tham chiếu

Áp lực chủ động đầu tiên cho biết mức độ khẩn trương của phía tham gia. Không kết luận trước khi thấy phản ứng của giá.

#### Ngoài mốc tham chiếu

Hoạt động ngoài vùng có tạo tiến triển và POC cục bộ không? Đây là câu hỏi AAC.

#### Trong quá trình tái nhập

Áp lực chủ động phía đối diện có duy trì giá trong vùng cũ hay không? Đây là câu hỏi FAR.

#### Các lần thử lặp lại

So nỗ lực/kết quả giữa lần thử 1 và 2. Kém tiến triển hơn có thể hỗ trợ FAR hai lần thử.

#### Không xác định được phía chủ động

Nếu không phân loại được phía chủ động, dùng tổng khối lượng, số giao dịch, tốc độ, tiến triển giá và quá trình xây hồ sơ.

#### Xung đột

Dòng lệnh mạnh nhưng sự chấp nhận phát triển theo hướng ngược lại là một xung đột. Không được ép kết luận.

### Quy trình áp dụng

1. Xác định hướng của lần thử đấu giá.
2. Đo nỗ lực theo hướng của lần thử.
3. Đo kết quả.
4. Kiểm tra phản ứng phía đối diện.
5. Kết hợp sự duy trì.
6. Ghi kết luận hoặc trạng thái chưa được giải quyết.

### Sai lầm thường gặp

* Đọc Delta trước khi biết hướng của lần thử đấu giá.
* Dùng Imbalance thay sự chấp nhận.
* Bịa phía khi không xác định.

### Ghi nhớ

> Order Flow không chọn câu chuyện; nó kiểm tra câu chuyện đang được thị trường thử.

## Chương 58. Tích hợp Options Intelligence vào câu chuyện đấu giá

Options Intelligence được thêm sau khi bản đồ AMT đã rõ nhưng trước khi thực thi. Mục tiêu là định nghĩa regime, horizon, vùng nhạy cảm và kịch bản kiểm chứng, không thay bằng chứng từ giá.

### Nội dung cốt lõi

#### Độ tươi và horizon

Ghi timestamp, underlying contract, expiry buckets và giá futures tại snapshot. Một surface có thể còn dùng cho horizon nhiều tuần nhưng flow intraday đã cũ, hoặc ngược lại.

#### Volatility structure

Ghi ATM IV, term structure, skew và thay đổi so với snapshot trước. Chỉ dùng so sánh đã chuẩn hóa.

#### Activity structure

Ghi volume, OI, ΔOI và flow anomalies theo strike/expiry. Không gộp tồn kho lịch sử với flow mới.

#### Exposure structure

Ghi absolute concentration trước, signed scenarios sau. Mọi scenario phải nêu giả định vị thế, công thức và độ nhạy.

#### Vùng hội tụ

Một vùng Options được nâng quyền quan sát khi trùng hoặc gần:

* biên vùng giá trị;
* Composite edge;
* LVN/HVN phù hợp;
* mốc sự kiện;
* expected-move boundary;
* strike/expiry concentration đáng kể.

Vùng hội tụ mở Episode, không tự tạo lệnh.

#### Đồng thuận

AAC có acceptance, Order Flow tạo tiến triển và Options expansion/flow shift phù hợp horizon: Options hỗ trợ kỳ vọng đường đi rộng hơn, nhưng risk vẫn theo policy.

#### Xung đột

FAR rõ nhưng Options gợi ý expansion: bằng chứng giá vẫn ưu tiên; Options tạo yêu cầu xem lại target, thời gian giữ, event risk hoặc xác nhận.

#### Trung tính hoặc không dùng

Options neutral, mixed hoặc data unusable không tự làm luận điểm AMT + Order Flow mất hiệu lực. Phải ghi rõ phần nào không khả dụng.

### Quy trình áp dụng

1. Hoàn thành bản đồ AMT.
2. Chọn horizon giao dịch.
3. Đọc volatility, activity và exposure structure.
4. Xây Options regime.
5. Đặt vùng nhạy cảm lên bản đồ.
6. Chờ Episode và Order Flow.
7. Phân loại hỗ trợ, xung đột, trung tính hoặc không dùng.
8. Không đổi hướng chỉ vì mô hình Options.

### Sai lầm thường gặp

* Chỉ nhìn GEX mà bỏ surface và expiry.
* Dùng snapshot không point-in-time.
* Gọi concentration là hỗ trợ/kháng cự.
* Không phân tách horizon.
* Dùng vendor label mà không biết công thức.

### Ghi nhớ

> Options Intelligence làm phong phú câu chuyện bằng giá của sự bất định và cấu trúc nhạy cảm. Cuộc đấu giá vẫn viết kết luận bằng giá và giao dịch.

## Chương 59. Xây dựng luận điểm thị trường

Luận điểm thị trường là hợp đồng giữa kỳ vọng và bằng chứng. Nó phải nêu hành vi mong đợi, điều kiện vào, điều kiện sai và thời hạn.



### Nội dung cốt lõi

#### Bối cảnh

Trạng thái, vị trí, hướng cấu trúc và ngắn hạn, sự kiện, Options regime, horizon và độ tin cậy dữ liệu.

#### Lần thử

Mốc tham chiếu nào, hướng lần thử đấu giá ngoài vùng nào, Episode đang ở đâu.

#### Bằng chứng

Sự chấp nhận hoặc tái nhập, Nỗ lực–Kết quả, khả năng duy trì và bằng chứng còn thiếu.

#### Hành vi kỳ vọng

Nếu luận điểm đúng, giá phải làm gì tiếp theo? Ví dụ giữ trên VAH, kiểm tra lại không lấy lại vùng giá trị cũ.

#### Vô hiệu

Điều gì chứng minh luận điểm sai về đấu giá, không chỉ chạm số giá.

#### Đường mục tiêu

Rào cản trung gian và hành lang mục tiêu.

#### Thời hạn luận điểm

Luận điểm sống bao lâu? Nếu hành vi kỳ vọng không xảy ra trong thời hạn đã định, luận điểm hết hiệu lực.

### Cam kết khung thời gian

Mỗi luận điểm phải ghi năm khung:

&#x20;   Khung bối cảnh
    Khung luận điểm
    Khung điều kiện kích hoạt
    Khung quản lý
    Khung mục tiêu


Không vào bằng điều kiện vài phút rồi biến lệnh thua thành vị thế nhiều ngày. Không dùng một lần Delta đổi dấu để vô hiệu luận điểm nhiều phiên. Mục tiêu và thời hạn vô hiệu phải phù hợp với khung của luận điểm.

### Luật nhất quán luận điểm

Mọi hành động sau điểm vào phải liên hệ được với hành vi được kỳ vọng, vô hiệu, mục tiêu hoặc chính sách rủi ro. Một thông tin mới chỉ được thay đổi giao dịch khi nó liên quan trực tiếp đến luận điểm, tạo kết quả trên giá, được duy trì và xuất hiện trong khung thời gian phù hợp.

### Quy trình áp dụng

1. Viết một câu luận điểm.
2. Liệt kê bằng chứng.
3. Liệt kê bằng chứng còn thiếu.
4. Viết hành vi kỳ vọng.
5. Viết vô hiệu.
6. Viết đường mục tiêu.
7. Viết thời hạn luận điểm.
8. Chọn phong cách vào.

### Sai lầm thường gặp

* Luận điểm chỉ là “tôi nghĩ tăng”.
* Không có hành vi kỳ vọng.
* Dời vô hiệu khi giá đi ngược.

### Ghi nhớ

> Luận điểm tốt không chỉ nói điều có thể xảy ra; nó nói thị trường phải làm gì để giữ luận điểm sống.

## Chương 60. Vô hiệu đa chiều

Dừng lỗ là lệnh bảo vệ; vô hiệu là lý do luận điểm không còn đúng. Hai thứ liên quan nhưng không đồng nhất.



### Nội dung cốt lõi

#### Vô hiệu theo giá

Giá vượt mức cấu trúc khiến hình học giao dịch sai. Cần khoảng đệm hợp lý theo tick và chênh lệch mua bán.

#### Vô hiệu theo cuộc đấu giá

FAR sai khi sự chấp nhận ngoài vùng được thiết lập; AAC sai khi vùng giá trị cũ tái chấp nhận bền vững.

#### Vô hiệu theo thời gian

Hành vi kỳ vọng không xảy ra trong thời hạn. Một lệnh đứng yên tạo chi phí cơ hội và làm tăng rủi ro sự kiện.

#### Vô hiệu theo bối cảnh

Sự kiện, thanh khoản, ánh xạ hợp đồng hoặc chất lượng dữ liệu thay đổi.

#### Vô hiệu theo bằng chứng

Dòng lệnh đối diện tạo kết quả và sự duy trì, POC/vùng giá trị cục bộ dịch ngược.

#### Vị trí dừng lỗ

Dừng lỗ phải nằm tại nơi luận điểm sai và phù hợp với ngân sách rủi ro. Nếu khoảng dừng quá xa, bỏ lệnh hoặc giảm khối lượng vị thế; không ép dừng lỗ gần chỉ để tăng khối lượng.

### Quy trình áp dụng

1. Xác định đấu giá vô hiệu.
2. Chuyển thành vùng giá.
3. Thêm khoảng đệm thực thi.
4. Tính rủi ro.
5. Nếu rủi ro không thể chịu được, bỏ giao dịch.
6. Ghi thời gian/bối cảnh vô hiệu.

### Sai lầm thường gặp

* Dừng lỗ theo số tiền ngẫu nhiên.
* Dời dừng lỗ xa chỉ để tránh bị chạm.
* Giữ lệnh sau khi luận điểm sai vì dừng lỗ chưa chạm.

### Ghi nhớ

> Dừng lỗ bảo vệ tài khoản; vô hiệu bảo vệ tính nhất quán của luận điểm.

## Chương 61. Mục tiêu và đường đi ít cản trở

Mục tiêu không phải con số mong muốn. Nó là điểm hoặc hành lang mà cuộc đấu giá có khả năng hướng tới nếu luận điểm tiếp tục sống.



### Nội dung cốt lõi

#### Mục tiêu cấu trúc

POC, biên đối diện của vùng giá trị, Composite POC, HVN, LVN và mốc nhiều phiên là những mục tiêu cấu trúc phổ biến.

#### Rào cản trung gian

Mỗi mốc giữa điểm vào và mục tiêu có thể làm giá chậm, luân phiên hoặc đảo trạng thái. Phải lập đường đi thay vì chỉ đánh dấu điểm cuối.

#### Đường đi ít cản trở

Đường đi ít cản trở là hướng có ít vùng thỏa thuận, ít rào cản và có sự chấp nhận của giá phù hợp. Nó không phải đường thẳng.

#### Bối cảnh Options

Expected-move boundary, strike/expiry concentration, volatility regime và exposure zone có thể là vùng cần xem xét trên đường tới mục tiêu. Chúng không tự trở thành mục tiêu và không tự buộc người giao dịch chốt lời.

#### Hành lang mục tiêu

Thay vì một giá duy nhất, có thể dùng vùng mục tiêu gồm mốc cấu trúc, độ rung bình thường và chi phí thực thi.

#### Không có không gian mục tiêu

Nếu rào cản gần hơn mức vô hiệu hoặc tỷ lệ lợi nhuận/rủi ro không đủ, quyết định đúng là không vào.

### Quy trình áp dụng

1. Đánh dấu mục tiêu cấu trúc.
2. Liệt kê rào cản trung gian.
3. Thêm các vùng Options đáng chú ý nếu dữ liệu, horizon và phương pháp hợp lệ.
4. Tính khoảng cách tới vô hiệu.
5. Chỉ vào khi hành lang mục tiêu còn đủ không gian.

### Sai lầm thường gặp

* Chọn mục tiêu theo số tiền muốn kiếm.
* Dùng expected move, strike OI hoặc vendor level như mục tiêu bắt buộc.
* Bỏ qua POC hoặc vùng giá trị nằm giữa.
* Vào lệnh không có không gian.

### Ghi nhớ

> Mục tiêu tốt nằm trên đường đi hợp lý của cuộc đấu giá, không nằm trong mong muốn của người giao dịch.

## Chương 62. Quản trị rủi ro

Một phương pháp tốt vẫn có chuỗi thua. Quản trị rủi ro biến sự bất định thành mức chi phí có thể chịu được và giúp người giao dịch tồn tại đủ lâu để lợi thế bộc lộ.



### Nội dung cốt lõi

#### R trên mỗi giao dịch

Xác định phần trăm hoặc số tiền rủi ro tối đa trước giao dịch. Khối lượng vị thế được tính sau khi xác định khoảng dừng lỗ, không làm ngược lại.

#### Công thức khối lượng vị thế

`Khối lượng vị thế = Ngân sách rủi ro / (Khoảng dừng lỗ × Giá trị mỗi đơn vị + Chi phí ước tính)`.

#### Giới hạn rủi ro ngày và tuần

Giới hạn chuỗi thua và trạng thái tâm lý. Khi đã chạm giới hạn, không giao dịch.

#### Tỷ lệ lợi nhuận/rủi ro thực thi

Dùng điểm vào thực tế, chênh lệch mua bán, trượt giá và mục tiêu rào cản. RR lý thuyết không đủ.

#### Tương quan

Nhiều vị thế cùng nhạy với USD, lãi suất hoặc kim loại có thể thực chất là một rủi ro tập trung.

#### Rủi ro sự kiện

Tin tức có thể khiến giá vượt dừng lỗ và thanh khoản biến mất. Giảm rủi ro hoặc đứng ngoài theo chính sách.

#### Rủi ro hành vi

Không tăng khối lượng vị thế để gỡ lỗ, không giao dịch trả thù và không đổi quy tắc giữa phiên.

### Sụt giảm tài khoản và rủi ro chuỗi thua

Một phương pháp có kỳ vọng dương vẫn có thể tạo chuỗi thua dài. Quy mô vị thế phải được thiết kế để tài khoản và tâm lý vẫn hoạt động được trong giai đoạn bất lợi.

Cần theo dõi:

* Sụt giảm lớn nhất theo đơn vị R và theo phần trăm tài khoản.
* Chuỗi thua dài nhất trong mẫu và trong mô phỏng.
* Mức tập trung rủi ro khi nhiều giao dịch có cùng yếu tố USD, lãi suất hoặc kim loại.
* Khả năng tiếp tục tuân thủ khi tài khoản đang sụt giảm.

Không tăng rủi ro để gỡ lại sụt giảm. Việc giảm rủi ro phải dựa trên chính sách viết trước, chẳng hạn sau khi vượt một mức sụt giảm hoặc khi chất lượng thực thi giảm rõ rệt.

### Rủi ro phá sản

Rủi ro phá sản không chỉ là tài khoản về không. Nó còn gồm việc sụt giảm đến mức người giao dịch không còn đủ vốn, khả năng chịu đựng hoặc niềm tin để tiếp tục thực hiện phương pháp. Rủi ro này tăng khi:

* Rủi ro mỗi lệnh quá lớn.
* Các giao dịch có tương quan cao.
* Chi phí và trượt giá bị đánh giá thấp.
* Kỳ vọng được ước lượng từ mẫu nhỏ hoặc đã chọn lọc.
* Người giao dịch thay đổi quy tắc giữa chuỗi thua.

### Quy trình áp dụng

1. Tính dừng lỗ theo vô hiệu.
2. Tính khối lượng vị thế.
3. Tính đầy đủ chi phí.
4. Kiểm tra mức phơi nhiễm trong ngày.
5. Kiểm tra sự kiện.
6. Chỉ giao dịch khi mức rủi ro có thể chịu được.

### Sai lầm thường gặp

* Khối lượng vị thế trước dừng lỗ.
* Bỏ qua trượt giá.
* Tăng rủi ro sau thắng/thua cảm tính.

### Ghi nhớ

> Không có quyền tồn tại thì không có cơ hội tận dụng lợi thế.

# PHẦN VII — CÁC HỌ CHIẾN LƯỢC VÀ LỐI VÀO LỆNH

## Chương 63. Ba kết quả của một lần thử đấu giá ngoài vùng

Một lần thử đấu giá ngoài vùng có ba kết quả hợp lệ: thất bại và tái nhập, được chấp nhận và tiếp diễn, hoặc chưa được giải quyết.



### FAR

Giá không xây được sự chấp nhận bền vững ngoài mốc, tái nhập và duy trì trong cuộc đấu giá cũ. Kỳ vọng hướng về vùng thỏa thuận cũ.

### AAC

Giá xây hoạt động và vùng giá trị ngoài mốc; nỗ lực lấy lại vùng giá trị cũ thất bại; cuộc đấu giá tiếp tục theo hướng khám phá giá.

### Chưa được giải quyết

Giá qua lại, hoạt động phân tán, POC chưa rõ và khả năng duy trì thiếu. Không được ép thành FAR hoặc AAC.

### Ba mức trưởng thành của điểm vào

|Mức|Đặc điểm|
|-|-|
|Sớm|Vào khi bằng chứng mới phát triển, giá tốt hơn nhưng nguy cơ phân loại sai cao|
|Tiêu chuẩn|Có tái nhập hoặc chấp nhận rõ và một lần kiểm tra hành vi|
|Xác nhận|Có vùng giá trị/POC duy trì, kiểm tra lại hoặc lần thử thứ hai và bằng chứng tiếp diễn|

### Hàng rào kiểm chứng cho phong cách Sớm

Phong cách Sớm mặc định là **phong cách nghiên cứu**, không phải lựa chọn mặc định cho người mới.

&#x20;   Phát lại dữ liệu
    → quan sát và ghi nhật ký không thực thi
    → giao dịch mô phỏng hoặc quy mô rất nhỏ
    → đánh giá ngoài mẫu
    → chỉ sau đó mới dùng mức rủi ro chuẩn


Yêu cầu:

* Tiêu chuẩn mẫu phải được xác định trước, không chọn lại sau khi xem kết quả.
* Kết quả phải tính chênh lệch mua bán, hoa hồng, trượt giá và lệnh không khớp.
* Phân tầng theo phiên, chế độ biến động và loại mốc.
* Chưa vượt qua cổng bằng chứng cá nhân thì chỉ dùng phong cách Tiêu chuẩn hoặc Xác nhận.
* Không tăng khối lượng chỉ vì vài giao dịch Sớm thắng đẹp.

### Hành vi được kỳ vọng

|Kịch bản|Hành vi phải xuất hiện nếu luận điểm đúng|
|-|-|
|FAR tái nhập|Giá tiến vào vùng giá trị cũ và không nhanh chóng khôi phục vùng ngoài|
|FAR kiểm tra lại|Lần kiểm tra không xây được sự chấp nhận ngoài; dòng lệnh đối diện tạo kết quả|
|AAC sớm|Giá giữ ngoài mốc; POC/vùng giá trị cục bộ không quay vào|
|AAC kiểm tra lại|Nỗ lực lấy lại vùng giá trị cũ thất bại; giá khôi phục vùng đã được chấp nhận|
|Tiếp diễn vùng giá trị mới|Vùng giá trị mới giữ được và POC tiếp tục dịch|
|Luân phiên|Giá không xây vùng giá trị ngoài biên và quay lại trung tâm|

### Ghi nhớ

> Giá vào đẹp không đồng nghĩa luận điểm trưởng thành. Phong cách Sớm phải kiếm được quyền sử dụng bằng dữ liệu riêng.

## Chương 64. FAR: Failed Auction Re-entry

FAR là họ chiến lược giao dịch theo hướng quay về cuộc đấu giá cũ sau khi một lần thử giá ngoài mốc không xây được sự chấp nhận và việc tái nhập được duy trì.



### FAR mua

&#x20;   Giá thử dưới biên dưới
    → không xây vùng giá trị bền vững bên dưới
    → áp lực bán chủ động không tạo tiến triển giảm tương xứng
    → giá tái nhập lên trên hoặc vào trong
    → duy trì trong vùng cũ
    → luận điểm mua FAR


### FAR bán

Cấu trúc đối xứng phía trên: giá thử trên biên trên, không xây vùng giá trị bền vững, áp lực mua không tạo tiến triển tăng tương xứng, sau đó tái nhập và duy trì trong vùng cũ.

### Điều kiện cần

* Vùng mốc rõ ràng.
* Episode hợp lệ.
* Không có sự chấp nhận đã hình thành theo hướng chống luận điểm.
* Tái nhập và duy trì.
* Hình học từ điểm vào đến vô hiệu rõ.
* Đường tới mục tiêu còn khoảng trống.
* Dữ liệu và thị trường thực thi hợp lệ.

### Phong cách Sớm: vào sau tái nhập

Tham gia sau tái nhập hình học và phản ứng đối diện ban đầu. Giá thường tốt hơn nhưng bằng chứng ít hơn. Phong cách này chỉ được giao dịch thật sau khi đã qua cổng kiểm chứng riêng; nếu chưa, chỉ ghi nhận hoặc giao dịch mô phỏng.

### Phong cách Tiêu chuẩn: kiểm tra vi mô

Chờ giá quay lại kiểm tra mốc hoặc vùng tái nhập. Tham gia khi nỗ lực quay ra ngoài thất bại và dòng lệnh đối diện tạo tiến triển.

### Phong cách Xác nhận: kiểm tra cấu trúc

Chờ POC/vùng giá trị cục bộ quay vào, sau đó kiểm tra mốc từ bên trong. Giá vào kém hơn nhưng luận điểm trưởng thành và vô hiệu rõ hơn.

### FAR hai lần thử

Lần thử thứ hai có nỗ lực tương đương hoặc lớn hơn nhưng kết quả kém hơn, sau đó tái nhập ổn định. Đây là biến thể mạnh khi so sánh được đo lường, không phải vì con số hai có quyền lực đặc biệt.

### Vô hiệu cốt lõi

FAR sai khi thị trường thiết lập sự chấp nhận bền vững ngoài mốc: hoạt động, POC hoặc vùng giá trị xây ngoài; nhịp hồi giữ được; nỗ lực lấy lại vùng cũ thất bại.

### Mục tiêu

POC, trung tâm vùng giá trị, biên đối diện, Composite POC hoặc HVN trước đó. Sau khi giá được tái chấp nhận vào cân bằng, biên đối diện là mục tiêu luân phiên tiềm năng, không phải mục tiêu bắt buộc. POC có thể ngắt đường đi.

### Vai trò của Options và vi cấu trúc

Options bổ sung regime, horizon và vùng nhạy cảm. DOM/MBO, iceberg, đợt kích hoạt dừng lỗ hoặc cú quét chỉ bổ sung cơ chế thực thi. Không công cụ nào thay thế tái chấp nhận vào vùng cũ.

### Ghi nhớ

> FAR không giao dịch cú vượt biên. FAR giao dịch sự thất bại trong việc xây cuộc đấu giá mới và sự tái chấp nhận cuộc đấu giá cũ.

## Chương 65. AAC: Accepted Auction Continuation

AAC là họ chiến lược tiếp diễn sau khi thị trường rời mốc, xây sự chấp nhận ở vùng mới và không tái chấp nhận bền vững vùng cũ.



### AAC mua

&#x20;   Giá thử trên biên trên
    → thời gian, khối lượng và số giao dịch phát triển ngoài
    → POC hoặc vùng giá trị cục bộ dịch lên
    → nhịp hồi không lấy lại bền vững vùng giá trị cũ
    → luận điểm tiếp diễn mua AAC


### AAC bán

Cấu trúc đối xứng phía dưới: hoạt động xây dưới, POC/vùng giá trị dịch xuống và nhịp hồi không tái chấp nhận vùng cũ.

### Điều kiện cần

* Lần thử đấu giá ngoài vùng.
* Sự chấp nhận đang phát triển hoặc đã được thiết lập.
* Tiến triển tương xứng với nỗ lực.
* Nỗ lực lấy lại vùng giá trị cũ thất bại.
* Đường tới mục tiêu còn khoảng trống.
* Vô hiệu rõ ràng.

### Phong cách Sớm: chấp nhận ban đầu

Tham gia khi giá duy trì ngoài và dòng lệnh tạo tiến triển nhưng trước một lần kiểm tra sâu. Phong cách này có nguy cơ nhầm chấp nhận tạm thời với sự chấp nhận bền vững, vì vậy phải qua cổng kiểm chứng riêng trước khi dùng tiền thật ở mức rủi ro chuẩn.

### Phong cách Tiêu chuẩn: kiểm tra vùng được chấp nhận

Chờ nhịp hồi về mốc hoặc vùng vừa được chấp nhận. Tham gia khi nỗ lực lấy lại vùng giá trị cũ thất bại và giá khôi phục phía ngoài.

### Phong cách Xác nhận: tiếp diễn vùng giá trị mới

Chờ vùng cân bằng nhỏ hoặc vùng giá trị mới hình thành ngoài, sau đó tham gia lần mở rộng tiếp theo hoặc nhịp hồi về biên vùng giá trị mới.

### Vô hiệu cốt lõi

AAC sai khi giá tái chấp nhận bền vững vào vùng giá trị cũ: thời gian, hoạt động và POC quay vào; nỗ lực khôi phục vùng ngoài thất bại. Một bóng nến quay vào chưa đủ.

### Mục tiêu

Mốc cấu trúc có ý nghĩa tiếp theo, hành lang LVN, biên Composite hoặc vùng từng được chấp nhận trên đường đi. Options concentration hoặc expected-move repricing có thể tạo rào cản cần xem xét nhưng không tự hủy AAC.

### Ghi nhớ

> AAC giao dịch sự hình thành của cuộc đấu giá mới, không giao dịch một cú phá đường đơn lẻ.

## Chương 66. Luân phiên trong vùng giá trị

Kịch bản giao dịch này chuyển lý thuyết Tam Trụ thành một kế hoạch có thể quan sát, kiểm tra và từ chối. Nó không phải mẫu hình bảo đảm; mọi lệnh phải đi qua Vị trí, Episode, Bằng chứng, Nỗ lực–Kết quả, vô hiệu và rủi ro.



### Nội dung cốt lõi

#### Bản chất

Luân phiên trong vùng giá trị khai thác sự luân phiên trong một đấu giá cân bằng: từ biên về trung tâm hoặc từ trung tâm về biên đối diện. Đây không phải FAR trừ khi trước đó có lần thử đấu giá ngoài vùng và tái chấp nhận.

#### Bối cảnh phù hợp

Vùng cân bằng rõ, vùng giá trị ổn định, POC không dịch chuyển mạnh, không sự kiện mở rộng và giá đang tại biên hoặc trung tâm có ý nghĩa.

#### Cấu trúc thiết lập

Giá kiểm tra VAH/VAL và không có sự chấp nhận ngoài; phản ứng đáp ứng xuất hiện; đường đi về POC/biên đối diện còn trống.

#### Điều kiện kích hoạt

* Tái nhập vào vùng giá trị hoặc không rời được vùng giá trị.
* Dòng lệnh đối diện tạo tiến triển.
* POC vẫn ở trong vùng giá trị.
* Không có sự chấp nhận khởi xướng chống lệnh.

#### Vô hiệu

Sự chấp nhận bền vững ngoài vùng giá trị hoặc vùng giá trị/POC dịch chuyển theo phá vỡ.

#### Mục tiêu và quản lý

Mục tiêu thứ nhất là POC; mục tiêu thứ hai là biên đối diện. Quản lý chặt nếu POC trở thành trung tâm bám giá.

#### Vai trò của Options

Trụ Options có thể cho biết volatility regime, horizon, vùng concentration và cơ chế exposure nào đáng quan sát. Nó điều chỉnh kỳ vọng về đường đi, yêu cầu xác nhận và quản trị theo policy, nhưng không thay Vị trí, Episode, Sự chấp nhận hoặc Nỗ lực–Kết quả.

### Quy trình áp dụng

1. Xác định bối cảnh và mốc tham chiếu trước khi giá đến.
2. Theo dõi Episode mà không dự đoán kết quả.
3. Chờ điều kiện kích hoạt thuộc đúng phong cách vào.
4. Tính điểm vô hiệu, khoảng trống tới mục tiêu và rủi ro.
5. Bỏ lệnh nếu thiếu điều kiện hoặc xuất hiện xung đột lớn.

### Sai lầm thường gặp

* Giao dịch ngược tại biên khi khám phá giá đã bắt đầu.
* Nhắm biên đối diện dù POC giữ giá.
* Gọi luân phiên là FAR không có ngoài vùng episode.

### Ghi nhớ

> Một kịch bản giao dịch chỉ có giá trị khi điều kiện không giao dịch rõ ngang với điều kiện vào.

## Chương 67. Luân phiên quanh đường trung tâm

Kịch bản giao dịch này chuyển lý thuyết Tam Trụ thành một kế hoạch có thể quan sát, kiểm tra và từ chối. Nó không phải mẫu hình bảo đảm; mọi lệnh phải đi qua Vị trí, Episode, Bằng chứng, Nỗ lực–Kết quả, vô hiệu và rủi ro.



### Nội dung cốt lõi

#### Bản chất

Luân phiên quanh đường trung tâm là giao dịch sự qua lại quanh POC hoặc Composite POC trong vùng cân bằng. Đường trung tâm không có hình học trong/ngoài giống một biên.

#### Bối cảnh phù hợp

Composite hoặc phiên đang cân bằng, đường trung tâm được giao dịch lặp lại, các cực trị còn xa và OTF không duy trì.

#### Cấu trúc thiết lập

Giá rời đường trung tâm nhưng thiếu tiến triển rồi quay lại; số lần cắt qua tăng; hoạt động tiếp tục tập trung quanh trung tâm.

#### Điều kiện kích hoạt

* Không duy trì được khoảng cách khỏi đường trung tâm.
* Giá cắt lại đường trung tâm kèm hoạt động.
* Mục tiêu phía rõ và không rào cản quá gần.

#### Vô hiệu

Vùng giá trị dịch chuyển hoặc giá được chấp nhận xa đường trung tâm; đường trung tâm không còn là vùng hoạt động chính.

#### Mục tiêu và quản lý

Mục tiêu là đối diện nội vùng mốc tham chiếu hoặc biên; thoát nếu giá quay lại trung tâm nhiều lần.

#### Vai trò của Options

Trụ Options có thể cho biết volatility regime, horizon, vùng concentration và cơ chế exposure nào đáng quan sát. Nó điều chỉnh kỳ vọng về đường đi, yêu cầu xác nhận và quản trị theo policy, nhưng không thay Vị trí, Episode, Sự chấp nhận hoặc Nỗ lực–Kết quả.

### Quy trình áp dụng

1. Xác định bối cảnh và mốc tham chiếu trước khi giá đến.
2. Theo dõi Episode mà không dự đoán kết quả.
3. Chờ điều kiện kích hoạt thuộc đúng phong cách vào.
4. Tính điểm vô hiệu, khoảng trống tới mục tiêu và rủi ro.
5. Bỏ lệnh nếu thiếu điều kiện hoặc xuất hiện xung đột lớn.

### Sai lầm thường gặp

* Dùng POC như hỗ trợ/kháng cự.
* Áp dụng cách lập luận FAR/AAC máy móc quanh đường trung tâm.
* Giao dịch quá mức mỗi lần giá cắt qua đường trung tâm.

### Ghi nhớ

> Một kịch bản giao dịch chỉ có giá trị khi điều kiện không giao dịch rõ ngang với điều kiện vào.

## Chương 68. Giao dịch đáp ứng tại biên

Kịch bản giao dịch này chuyển lý thuyết Tam Trụ thành một kế hoạch có thể quan sát, kiểm tra và từ chối. Nó không phải mẫu hình bảo đảm; mọi lệnh phải đi qua Vị trí, Episode, Bằng chứng, Nỗ lực–Kết quả, vô hiệu và rủi ro.

### Nội dung cốt lõi

#### Bản chất

Đáp ứng Biên là phản ứng hướng vào vùng giá trị tại một biên, nhưng lần thử đấu giá ngoài vùng chưa đủ phát triển để gọi FAR hoàn chỉnh. Đây là lối vào nhanh, xác nhận thấp.

#### Bối cảnh phù hợp

Vùng cân bằng có biên rõ, lần kiểm tra đầu hoặc độ lệch nhỏ, hoạt động đáp ứng xuất hiện và khoảng vô hiệu ngắn.

#### Cấu trúc thiết lập

Giá kiểm tra biên nhưng không duy trì ngoài; vi cấu trúc đối diện xuất hiện; mục tiêu đầu tiên là POC.

#### Điều kiện kích hoạt

* Đóng cửa hoặc tái nhập vào vùng nhỏ.
* Áp lực chủ động phía đối diện tạo tiến triển ngay.
* No sự chấp nhận bằng chứng ngoài.
* Rủi ro nhỏ và mục tiêu khoảng trống đủ.

#### Vô hiệu

Hoạt động/vùng giá trị bắt đầu xây ngoài hoặc kiểm tra lại biên giữ ngoài vùng.

#### Mục tiêu và quản lý

Quản lý nhanh với POC là mục tiêu chính. Không biến một giao dịch ngắn thất bại thành một lệnh FAR giữ lâu hơn.

#### Vai trò của Options

Trụ Options có thể cho biết volatility regime, horizon, vùng concentration và cơ chế exposure nào đáng quan sát. Nó điều chỉnh kỳ vọng về đường đi, yêu cầu xác nhận và quản trị theo policy, nhưng không thay Vị trí, Episode, Sự chấp nhận hoặc Nỗ lực–Kết quả.

### Quy trình áp dụng

1. Xác định bối cảnh và mốc tham chiếu trước khi giá đến.
2. Theo dõi Episode mà không dự đoán kết quả.
3. Chờ điều kiện kích hoạt thuộc đúng phong cách vào.
4. Tính điểm vô hiệu, khoảng trống tới mục tiêu và rủi ro.
5. Bỏ lệnh nếu thiếu điều kiện hoặc xuất hiện xung đột lớn.

### Sai lầm thường gặp

* Gọi một giao dịch đáp ứng là FAR đã xác nhận.
* Giữ khi AAC bằng chứng xuất hiện.
* Vào lệnh giữa vùng nhiễu.

### Ghi nhớ

> Một kịch bản giao dịch chỉ có giá trị khi điều kiện không giao dịch rõ ngang với điều kiện vào.

## Chương 69. Phá vỡ chủ động

Kịch bản giao dịch này chuyển lý thuyết Tam Trụ thành một kế hoạch có thể quan sát, kiểm tra và từ chối. Nó không phải mẫu hình bảo đảm; mọi lệnh phải đi qua Vị trí, Episode, Bằng chứng, Nỗ lực–Kết quả, vô hiệu và rủi ro.

### Nội dung cốt lõi

#### Bản chất

Khởi xướng Phá vỡ tham gia sớm khi thị trường rời vùng cân bằng với áp lực chủ động và tiến triển giá rõ, trước khi AAC trưởng thành. Đây là phong cách rủi ro cao hơn.

#### Bối cảnh phù hợp

Vùng cân bằng có sự nén rõ, mức tham gia mở rộng hoặc chất xúc tác, đồng thời đường đi ngoài biên còn ít cản trở.

#### Cấu trúc thiết lập

Giá phá biên với tốc độ, khối lượng và số giao dịch tăng; áp lực chủ động xếp chồng và khả năng duy trì xuất hiện ngay; nhịp hồi nông.

#### Điều kiện kích hoạt

* Cú phá vỡ tạo ra kết quả có ý nghĩa.
* Không tái nhập ngay.
* Áp lực chủ động tiếp tục tạo tiến triển.
* Hành lang mục tiêu đủ rộng.

#### Vô hiệu

Tái nhập được duy trì vào vùng giá trị hoặc tiến triển đình trệ dù nỗ lực lớn.

#### Mục tiêu và quản lý

Quản lý bằng quy tắc thất bại nhanh; chốt một phần tại rào cản đầu tiên; chuyển sang cách quản lý AAC nếu sự chấp nhận tiếp tục phát triển.

#### Vai trò của Options

Trụ Options có thể cho biết volatility regime, horizon, vùng concentration và cơ chế exposure nào đáng quan sát. Nó điều chỉnh kỳ vọng về đường đi, yêu cầu xác nhận và quản trị theo policy, nhưng không thay Vị trí, Episode, Sự chấp nhận hoặc Nỗ lực–Kết quả.

### Quy trình áp dụng

1. Xác định bối cảnh và mốc tham chiếu trước khi giá đến.
2. Theo dõi Episode mà không dự đoán kết quả.
3. Chờ điều kiện kích hoạt thuộc đúng phong cách vào.
4. Tính điểm vô hiệu, khoảng trống tới mục tiêu và rủi ro.
5. Bỏ lệnh nếu thiếu điều kiện hoặc xuất hiện xung đột lớn.

### Sai lầm thường gặp

* Mua phá vỡ trong một chuyển động giả với thanh khoản mỏng.
* Đặt dừng lỗ đúng tại biên mà không có khoảng đệm.
* Không thoát khi tái nhập xảy ra.

### Ghi nhớ

> Một kịch bản giao dịch chỉ có giá trị khi điều kiện không giao dịch rõ ngang với điều kiện vào.

## Chương 70. Hồi về vùng giá trị mới

Kịch bản giao dịch này chuyển lý thuyết Tam Trụ thành một kế hoạch có thể quan sát, kiểm tra và từ chối. Nó không phải mẫu hình bảo đảm; mọi lệnh phải đi qua Vị trí, Episode, Bằng chứng, Nỗ lực–Kết quả, vô hiệu và rủi ro.

### Nội dung cốt lõi

#### Bản chất

Hồi về vùng giá trị mới là kịch bản tiếp diễn sau khi quá trình khám phá giá đã xây được vùng giá trị mới. Điểm vào muộn hơn AAC ban đầu nhưng cấu trúc rõ hơn.

#### Bối cảnh phù hợp

vùng giá trị/POC đã dịch và tách khỏi vùng giá trị cũ; vùng cân bằng nhỏ ở vùng mới.

#### Cấu trúc thiết lập

Nhịp hồi về VAL, VAH, POC mới hoặc điểm khởi phát của đợt mở rộng gần nhất, sau đó giá duy trì theo hướng khám phá.

#### Điều kiện kích hoạt

* Vùng giá trị mới vẫn còn nguyên.
* Nhịp hồi không tái chấp nhận vùng giá trị cũ.
* Dòng lệnh khôi phục hướng đi ban đầu.
* Mục tiêu tiếp theo còn mở.

#### Vô hiệu

Vùng giá trị mới thất bại và vùng giá trị cũ được lấy lại bền vững.

#### Mục tiêu và quản lý

Mục tiêu là mốc tham chiếu tiếp theo; dừng lỗ nằm sau vùng làm vùng giá trị mới mất hiệu lực; dời điểm dừng theo sự dịch chuyển vùng giá trị.

#### Vai trò của Options

Trụ Options có thể cho biết volatility regime, horizon, vùng concentration và cơ chế exposure nào đáng quan sát. Nó điều chỉnh kỳ vọng về đường đi, yêu cầu xác nhận và quản trị theo policy, nhưng không thay Vị trí, Episode, Sự chấp nhận hoặc Nỗ lực–Kết quả.

### Quy trình áp dụng

1. Xác định bối cảnh và mốc tham chiếu trước khi giá đến.
2. Theo dõi Episode mà không dự đoán kết quả.
3. Chờ điều kiện kích hoạt thuộc đúng phong cách vào.
4. Tính điểm vô hiệu, khoảng trống tới mục tiêu và rủi ro.
5. Bỏ lệnh nếu thiếu điều kiện hoặc xuất hiện xung đột lớn.

### Sai lầm thường gặp

* Gọi mọi vùng tích lũy là vùng giá trị mới.
* Vào khi vùng cân bằng mới chưa đủ hoạt động.
* Bỏ qua vùng giá trị cũ lấy lại.

### Ghi nhớ

> Một kịch bản giao dịch chỉ có giá trị khi điều kiện không giao dịch rõ ngang với điều kiện vào.

## Chương 71. FAR hai lần thử

Kịch bản giao dịch này chuyển lý thuyết Tam Trụ thành một kế hoạch có thể quan sát, kiểm tra và từ chối. Nó không phải mẫu hình bảo đảm; mọi lệnh phải đi qua Vị trí, Episode, Bằng chứng, Nỗ lực–Kết quả, vô hiệu và rủi ro.



### Nội dung cốt lõi

#### Bản chất

FAR hai lần thử là biến thể dùng để so sánh hai lần thử tại cùng một biên. Cốt lõi là sự suy giảm của kết quả, không phải bản thân con số hai.

#### Bối cảnh phù hợp

Lần thử 1 bị tái nhập hoặc từ chối nhưng Episode còn sống; giá quay lại kiểm tra lần 2 trong bối cảnh không đổi lớn.

#### Cấu trúc thiết lập

Lần thử thứ hai có nỗ lực lặp lại hoặc lớn hơn nhưng độ lệch ngoài mốc, thời lượng hoặc sự duy trì kém hơn; sau đó xuất hiện tái nhập ổn định.

#### Điều kiện kích hoạt

* Đặc điểm của lần thử được xác định rõ.
* Nỗ lực và kết quả giữa hai lần thử có thể so sánh.
* Lần thử thứ hai không xây được sự chấp nhận.
* Tái nhập được duy trì.
* Mục tiêu khoảng trống còn lại.

#### Vô hiệu

Lần thử 2 xây vùng giá trị/POC ngoài hoặc cấu trúc bối cảnh đổi.

#### Mục tiêu và quản lý

Điểm vào tại tái nhập hoặc kiểm tra lại; dừng lỗ ngoài lần thử thứ hai; mục tiêu là POC hoặc vùng giá trị cũ.

#### Vai trò của Options

Trụ Options có thể cho biết volatility regime, horizon, vùng concentration và cơ chế exposure nào đáng quan sát. Nó điều chỉnh kỳ vọng về đường đi, yêu cầu xác nhận và quản trị theo policy, nhưng không thay Vị trí, Episode, Sự chấp nhận hoặc Nỗ lực–Kết quả.

### Quy trình áp dụng

1. Xác định bối cảnh và mốc tham chiếu trước khi giá đến.
2. Theo dõi Episode mà không dự đoán kết quả.
3. Chờ điều kiện kích hoạt thuộc đúng phong cách vào.
4. Tính điểm vô hiệu, khoảng trống tới mục tiêu và rủi ro.
5. Bỏ lệnh nếu thiếu điều kiện hoặc xuất hiện xung đột lớn.

### Sai lầm thường gặp

* Coi lần thử thứ hai là đảo chiều tự động.
* Không đo nỗ lực.
* Gộp hai lần thử thuộc hai đấu giá khác.

### Ghi nhớ

> Một kịch bản giao dịch chỉ có giá trị khi điều kiện không giao dịch rõ ngang với điều kiện vào.

## Chương 72. Nỗ lực lớn nhưng tiến triển hạn chế

Kịch bản giao dịch này chuyển lý thuyết Tam Trụ thành một kế hoạch có thể quan sát, kiểm tra và từ chối. Nó không phải mẫu hình bảo đảm; mọi lệnh phải đi qua Vị trí, Episode, Bằng chứng, Nỗ lực–Kết quả, vô hiệu và rủi ro.

### Nội dung cốt lõi

#### Bản chất

Đây là ứng viên thiết lập dựa trên phân kỳ Nỗ lực–Kết quả tại vị trí quan trọng. Nó chưa phải lệnh cho đến khi đấu giá tạo điều kiện kích hoạt.

#### Bối cảnh phù hợp

Biên hoặc mốc tham chiếu, áp lực chủ động lặp lại, khối lượng và số giao dịch tăng cao nhưng tiến triển ròng hoặc sự duy trì thấp.

#### Cấu trúc thiết lập

Phân kỳ thô xuất hiện; sau đó tái nhập, phá OTF hoặc hoạt động khởi xướng phía đối diện xác nhận.

#### Điều kiện kích hoạt

* Nỗ lực cao tương đối so với bối cảnh.
* Tiến triển bị hạn chế.
* Vị trí có ý nghĩa.
* Cấu trúc điều kiện kích hoạt xuất hiện.

#### Vô hiệu

Tiến triển được khôi phục và sự chấp nhận phát triển tương xứng với nỗ lực.

#### Mục tiêu và quản lý

Nếu trở thành FAR, mục tiêu là vùng giá trị cũ; nếu chỉ giao dịch lướt sóng, phải quản lý nhanh. Dừng lỗ tại nơi phân kỳ bị phủ nhận bởi sự chấp nhận.

#### Vai trò của Options

Trụ Options có thể cho biết volatility regime, horizon, vùng concentration và cơ chế exposure nào đáng quan sát. Nó điều chỉnh kỳ vọng về đường đi, yêu cầu xác nhận và quản trị theo policy, nhưng không thay Vị trí, Episode, Sự chấp nhận hoặc Nỗ lực–Kết quả.

### Quy trình áp dụng

1. Xác định bối cảnh và mốc tham chiếu trước khi giá đến.
2. Theo dõi Episode mà không dự đoán kết quả.
3. Chờ điều kiện kích hoạt thuộc đúng phong cách vào.
4. Tính điểm vô hiệu, khoảng trống tới mục tiêu và rủi ro.
5. Bỏ lệnh nếu thiếu điều kiện hoặc xuất hiện xung đột lớn.

### Sai lầm thường gặp

* Gọi hấp thụ là kết luận chắc chắn.
* Giao dịch ngược giữa xu hướng.
* Không chờ cấu trúc điều kiện kích hoạt.

### Ghi nhớ

> Một kịch bản giao dịch chỉ có giá trị khi điều kiện không giao dịch rõ ngang với điều kiện vào.

## Chương 73. Kết quả lớn với nỗ lực tương đối thấp

Kịch bản giao dịch này chuyển lý thuyết Tam Trụ thành một kế hoạch có thể quan sát, kiểm tra và từ chối. Nó không phải mẫu hình bảo đảm; mọi lệnh phải đi qua Vị trí, Episode, Bằng chứng, Nỗ lực–Kết quả, vô hiệu và rủi ro.

### Nội dung cốt lõi

#### Bản chất

Thiếu lực đối kháng hoặc thanh khoản mỏng có thể cho phép giá đi xa với nỗ lực thấp. Thiết lập có thể tiếp diễn nhưng rủi ro thực thi cao.

#### Bối cảnh phù hợp

Ngoài vùng cân bằng hoặc LVN, thanh khoản mỏng hoặc thị trường đang chuyển tiếp; đường đi còn ít cản trở.

#### Cấu trúc thiết lập

tiến triển giá lớn, khối lượng/giao dịch không tương xứng, nhịp hồi nông, no đối diện sự chấp nhận.

#### Điều kiện kích hoạt

* Tiến triển được duy trì.
* No vùng giá trị cũ lấy lại.
* Điều kiện thanh khoản đã được xác định.
* Điểm vào không đuổi theo đoạn mở rộng cuối.

#### Vô hiệu

Phản ứng mạnh phía đối diện, tái nhập hoặc chênh lệch mua bán/trượt giá bất thường.

#### Mục tiêu và quản lý

Mục tiêu là HVN hoặc mốc tham chiếu tiếp theo; khối lượng vị thế thận trọng; loại lệnh phải hạn chế trượt giá.

#### Vai trò của Options

Trụ Options có thể cho biết volatility regime, horizon, vùng concentration và cơ chế exposure nào đáng quan sát. Nó điều chỉnh kỳ vọng về đường đi, yêu cầu xác nhận và quản trị theo policy, nhưng không thay Vị trí, Episode, Sự chấp nhận hoặc Nỗ lực–Kết quả.

### Quy trình áp dụng

1. Xác định bối cảnh và mốc tham chiếu trước khi giá đến.
2. Theo dõi Episode mà không dự đoán kết quả.
3. Chờ điều kiện kích hoạt thuộc đúng phong cách vào.
4. Tính điểm vô hiệu, khoảng trống tới mục tiêu và rủi ro.
5. Bỏ lệnh nếu thiếu điều kiện hoặc xuất hiện xung đột lớn.

### Sai lầm thường gặp

* Đuổi theo phần cuối của chuyển động.
* Dùng khối lượng thấp gọi cạn kiệt.
* Không tính trượt giá.

### Ghi nhớ

> Một kịch bản giao dịch chỉ có giá trị khi điều kiện không giao dịch rõ ngang với điều kiện vào.

## Chương 74. Từ chối tại biên Composite

Kịch bản giao dịch này chuyển lý thuyết Tam Trụ thành một kế hoạch có thể quan sát, kiểm tra và từ chối. Nó không phải mẫu hình bảo đảm; mọi lệnh phải đi qua Vị trí, Episode, Bằng chứng, Nỗ lực–Kết quả, vô hiệu và rủi ro.



### Nội dung cốt lõi

#### Bản chất

Đây là thiết lập FAR hoặc đáp ứng tại biên vùng cân bằng nhiều phiên, nơi mục tiêu và cấu trúc thường lớn hơn vùng giá trị của một phiên.

#### Bối cảnh phù hợp

Composite hợp lý, biên chưa bị sự chấp nhận ngoài, giá tiếp cận từ trong vùng hoặc lần thử đấu giá ngoài vùng.

#### Cấu trúc thiết lập

Lần thử đấu giá ngoài vùng thất bại, giá tái nhập Composite, POC cục bộ quay vào và lần kiểm tra lại biên không khôi phục được vùng ngoài.

#### Điều kiện kích hoạt

* Biên Composite còn hiệu lực.
* Tái nhập được duy trì.
* Order Flow hỗ trợ tiến triển hướng vào trong vùng.
* Đường tới mục tiêu trung tâm còn khoảng trống.

#### Vô hiệu

Sự chấp nhận phát triển ngoài Composite và vùng giá trị mới hình thành.

#### Mục tiêu và quản lý

Mục tiêu là Composite POC rồi HVN hoặc biên đối diện; thời gian giữ lệnh có thể dài hơn FAR trong phiên.

#### Vai trò của Options

Trụ Options có thể cho biết volatility regime, horizon, vùng concentration và cơ chế exposure nào đáng quan sát. Nó điều chỉnh kỳ vọng về đường đi, yêu cầu xác nhận và quản trị theo policy, nhưng không thay Vị trí, Episode, Sự chấp nhận hoặc Nỗ lực–Kết quả.

### Quy trình áp dụng

1. Xác định bối cảnh và mốc tham chiếu trước khi giá đến.
2. Theo dõi Episode mà không dự đoán kết quả.
3. Chờ điều kiện kích hoạt thuộc đúng phong cách vào.
4. Tính điểm vô hiệu, khoảng trống tới mục tiêu và rủi ro.
5. Bỏ lệnh nếu thiếu điều kiện hoặc xuất hiện xung đột lớn.

### Sai lầm thường gặp

* Dùng composite chọn tùy ý.
* Giao dịch ngược tại biên sau khi vùng giá trị nhiều ngày đã tách rõ.
* Dừng lỗ quá gần.

### Ghi nhớ

> Một kịch bản giao dịch chỉ có giá trị khi điều kiện không giao dịch rõ ngang với điều kiện vào.

## Chương 75. Phá Composite và xây vùng giá trị mới

Kịch bản giao dịch này chuyển lý thuyết Tam Trụ thành một kế hoạch có thể quan sát, kiểm tra và từ chối. Nó không phải mẫu hình bảo đảm; mọi lệnh phải đi qua Vị trí, Episode, Bằng chứng, Nỗ lực–Kết quả, vô hiệu và rủi ro.

### Nội dung cốt lõi

#### Bản chất

Đây là AAC nhiều phiên: thị trường thoát vùng cân bằng lớn, tạo hoạt động tách biệt và chuyển cấu trúc bối cảnh.

#### Bối cảnh phù hợp

Composite đã trưởng thành, có sự nén, chất xúc tác hoặc mức tham gia mở rộng, đồng thời đường đi bên ngoài còn ít cản trở.

#### Cấu trúc thiết lập

Giá phá biên, duy trì ngoài vùng qua nhiều khoảng thời gian, POC và vùng giá trị dịch chuyển; khi kiểm tra lại, biên cũ giữ được vai trò từ phía ngoài.

#### Điều kiện kích hoạt

* Thời gian và khối lượng ngoài vùng tiếp tục phát triển.
* Composite cũ không được lấy lại.
* Vùng giá trị mới hình thành.
* Order Flow tiếp tục tạo thuận lợi cho chuyển động.

#### Vô hiệu

Tái chấp nhận bền vững vào composite.

#### Mục tiêu và quản lý

Mục tiêu là mốc tuần, tháng hoặc hồ sơ tiếp theo; dời điểm dừng theo sự dịch chuyển của vùng giá trị mới.

#### Vai trò của Options

Trụ Options có thể cho biết volatility regime, horizon, vùng concentration và cơ chế exposure nào đáng quan sát. Nó điều chỉnh kỳ vọng về đường đi, yêu cầu xác nhận và quản trị theo policy, nhưng không thay Vị trí, Episode, Sự chấp nhận hoặc Nỗ lực–Kết quả.

### Quy trình áp dụng

1. Xác định bối cảnh và mốc tham chiếu trước khi giá đến.
2. Theo dõi Episode mà không dự đoán kết quả.
3. Chờ điều kiện kích hoạt thuộc đúng phong cách vào.
4. Tính điểm vô hiệu, khoảng trống tới mục tiêu và rủi ro.
5. Bỏ lệnh nếu thiếu điều kiện hoặc xuất hiện xung đột lớn.

### Sai lầm thường gặp

* Gọi nhịp đột biến đầu tiên là cấu trúc phá vỡ.
* Giữ lệnh khi composite lấy lại.
* Mục tiêu quá gần.

### Ghi nhớ

> Một kịch bản giao dịch chỉ có giá trị khi điều kiện không giao dịch rõ ngang với điều kiện vào.

## Chương 76. Ứng viên luân phiên trong chế độ Options ổn định

Kịch bản này mô tả khả năng luân phiên khi AMT đang cân bằng và trụ Options tạo **stability candidate**. Đây là môi trường ứng viên, không phải lệnh fade mọi cú phá.

### Nội dung cốt lõi

#### Bản chất

Options ổn định có thể xuất hiện khi surface ít tái định giá, expected move không mở rộng, concentration gần vùng cân bằng và exposure scenario có thể phù hợp hedging chống chuyển động. Vì phía dealer thường không quan sát trực tiếp, ngôn ngữ bắt buộc là “ứng viên ổn định”.

#### Bối cảnh phù hợp

* AMT đang cân bằng.
* POC/vùng giá trị chưa dịch khỏi trung tâm.
* Options regime là STABILITY\_CANDIDATE hoặc EXPIRY\_DOMINANT.
* ATM IV, term structure và skew không mở rộng đáng kể theo hướng phá.
* Concentration phù hợp horizon nằm gần vùng thỏa thuận.
* Order Flow tạo nỗ lực nhưng tiến triển hạn chế.

#### Cấu trúc thiết lập

Giá thử khỏi biên AMT hoặc vùng hội tụ, không duy trì, tái nhập và quay về vùng hoạt động. Mốc AMT và Episode là cơ sở vào; Options chỉ tăng kỳ vọng rằng chuyển động có thể bị hấp thụ hoặc luân phiên.

#### Điều kiện kích hoạt

* FAR hoặc hoạt động đáp ứng tại biên.
* Không có value/POC migration theo hướng phá.
* Flow Options không tái định giá mạnh theo hướng mới.
* Mục tiêu POC hoặc biên đối diện còn đủ không gian.
* Không ở ngay trước sự kiện có thể reset regime.

#### Vô hiệu

* Acceptance ngoài vùng.
* POC/vùng giá trị tách khỏi cân bằng.
* IV/term/skew repricing bền theo hướng phá.
* Options regime đổi sang expansion candidate.
* Dữ liệu Options mất chất lượng.

#### Mục tiêu và quản lý

Mục tiêu theo POC, HVN hoặc biên đối diện. Không giữ luận điểm luân phiên sau khi khám phá giá được chấp nhận. Risk không được tăng chỉ vì expiry concentration lớn.

#### Vai trò của Options

Options cung cấp bằng chứng môi trường cho luân phiên và thời hạn của kỳ vọng. Nó không chứng minh dealer đang ghim giá và không cho phép bán/mua một strike tự động.

### Sai lầm thường gặp

* Strike OI lớn đồng nghĩa giá phải quay lại.
* Stability candidate đồng nghĩa chắc chắn đi ngang.
* Fade breakout không cần Episode.
* Dùng snapshot trước sự kiện sau khi regime đã reset.

### Ghi nhớ

> Luân phiên là kết quả của cuộc đấu giá không chấp nhận giá mới. Options chỉ giúp đánh giá môi trường nào làm kết quả ấy hợp lý hơn.

## Chương 77. Ứng viên khuếch đại trong chế độ Options bất ổn

Kịch bản này mô tả môi trường nơi chuyển động có thể mở rộng khi AMT rời cân bằng và Options chuyển sang **expansion candidate**, **event repricing** hoặc **flow shift**. Nó chỉ được giao dịch khi AAC hoặc bằng chứng khởi xướng đã xuất hiện.

### Nội dung cốt lõi

#### Bản chất

Khuếch đại có thể liên quan front IV tăng, term structure căng, skew tái định giá, concentration bị xuyên, expiry gần có Gamma cao hoặc signed exposure scenario phù hợp hedging đi cùng chuyển động. Không yếu tố nào tự tạo hướng.

#### Bối cảnh phù hợp

* Giá ở biên cân bằng hoặc Composite.
* AMT bắt đầu khám phá giá.
* Options regime là EXPANSION\_CANDIDATE, EVENT\_PREMIUM hoặc FLOW\_SHIFT.
* IV/skew/term thay đổi cùng horizon với chuyển động.
* Vùng phía trước ít thỏa thuận AMT hoặc concentration đang dịch.
* Order Flow tạo tiến triển tương xứng.

#### Cấu trúc thiết lập

Phá vỡ có acceptance, nhịp hồi giữ phía ngoài, vùng giá trị cũ lấy lại thất bại và nhịp tiếp theo mở rộng. Options phải cho thấy repricing hoặc sensitivity phù hợp, không chỉ một vùng exposure Options âm tĩnh.

#### Điều kiện kích hoạt

* AAC bằng chứng.
* Không có tái chấp nhận bền vững vào vùng cũ.
* Order Flow thuận lợi cho giao dịch.
* Options repricing còn sống và dữ liệu đủ tươi.
* Thanh khoản thực thi chấp nhận được.

#### Vô hiệu

Tái chấp nhận vào vùng cũ, value migration thất bại, IV/skew đảo repricing, flow shift hết hiệu lực hoặc dữ liệu Options không còn đại diện cho underlying hiện tại.

#### Mục tiêu và quản lý

Mục tiêu theo mốc cấu trúc tiếp theo và target corridor. Chú ý trượt giá, event risk và IV regime. Không tăng khối lượng chỉ vì mô hình cho signed negative Gamma.

#### Vai trò của Options

Options cho biết môi trường có thể dễ mở rộng hơn và horizon nào chi phối. AMT và Order Flow xác nhận sự mở rộng có thật hay không.

### Sai lầm thường gặp

* Mua vì price vượt Gamma Flip mà chưa có acceptance.
* Gọi front IV tăng là hướng tăng.
* Bỏ qua concentration/expiry khác đang xung đột.
* Tăng risk vì nghĩ “short gamma chắc chạy”.

### Ghi nhớ

> Options bất ổn không tạo breakout. Nó làm thay đổi hình dạng rủi ro sau khi thị trường đã chứng minh breakout.

## Chương 78. AMT và Order Flow đồng thuận, Options hỗ trợ hoặc trung tính

Đây là cấu hình hoàn toàn giao dịch được. AMT và Order Flow có thể tạo luận điểm đầy đủ trong khi Options hỗ trợ, trung tính, mixed hoặc không khả dụng.

### Nội dung cốt lõi

#### Bản chất

Options là trụ độc lập, không phải con dấu bắt buộc. Nếu vị trí, Episode, acceptance, Nỗ lực–Kết quả, vô hiệu và target rõ, Options neutral không làm giao dịch mất hiệu lực.

#### Bối cảnh phù hợp

* AMT rõ.
* Order Flow tạo kết quả phù hợp.
* Options không có xung đột material với horizon giao dịch, hoặc dữ liệu không dùng được.
* Risk và liquidity đạt chuẩn.

#### Cấu trúc thiết lập

FAR, AAC hoặc luân phiên theo các chương tương ứng. Options chỉ ghi support/neutral/mixed/data-unusable.

#### Điều kiện kích hoạt

Theo AMT + Order Flow đã viết. Không cần đợi một “Options confirmation” không tồn tại.

#### Vô hiệu

Theo price, acceptance, time, context và risk. Options chỉ tạo vô hiệu riêng khi policy đã định nghĩa regime change material.

#### Mục tiêu và quản lý

Quản lý theo cấu trúc đấu giá. Options có thể điều chỉnh target corridor/risk nếu rule đã kiểm chứng; nếu không, ghi trung tính.

#### Vai trò của Options

Trung tính cũng là thông tin. Nó cho biết không có bằng chứng Options đủ mạnh để nâng hoặc hạ kỳ vọng.

### Sai lầm thường gặp

* Thiếu Options đồng nghĩa không giao dịch mặc định.
* Tự tìm một strike để xác nhận thiên kiến.
* Gọi mixed là bullish hoặc bearish.
* Hạ quyền AMT vì dashboard không “đẹp”.

### Ghi nhớ

> Một trụ trung tính không phải lỗ hổng. Lỗ hổng là bịa tín hiệu để làm bộ ba trông hoàn chỉnh.

## Chương 79. AMT và Order Flow đồng thuận nhưng Options xung đột

Xung đột Options không có nghĩa Options phủ quyết giá. Nó yêu cầu xác định xung đột thuộc horizon, biến động, concentration, flow hay mô hình exposure, rồi đánh giá tác động tới đường đi và quản trị.

### Nội dung cốt lõi

#### Bản chất

Ví dụ, AMT và Order Flow cho AAC Long nhưng Options regime là stability candidate, front IV không mở rộng hoặc concentration lớn nằm ngay phía trước. Đây là xung đột về follow-through/đường đi, không phải tín hiệu Short.

#### Phân loại xung đột

* **Horizon conflict:** Options dài hạn khác intraday.
* **Volatility conflict:** giá khám phá nhưng IV không repricing.
* **Concentration conflict:** vùng nhạy cảm gần target.
* **Flow conflict:** Options flow nổi bật phía khác nhưng ý định chưa rõ.
* **Model conflict:** signed exposure khác nhau theo scenario.
* **Data conflict:** timestamp hoặc underlying mapping không phù hợp.

#### Cấu trúc thiết lập

Giữ luận điểm AMT + Order Flow, nhưng ghi câu hỏi Options cụ thể và điều kiện cần kiểm tra. Không đổi FAR thành AAC hoặc Long thành Short chỉ vì một dashboard.

#### Điều kiện kích hoạt

Có thể yêu cầu:

* kiểm tra lại sâu hơn;
* acceptance lâu hơn;
* target ngắn hơn;
* không vào nếu RR thực thi không đủ;
* hoặc risk thấp hơn theo policy đã kiểm chứng.

#### Vô hiệu

Vẫn đến từ price, acceptance, time, context hoặc risk. Options conflict chỉ là một phần bối cảnh trừ khi regime-change rule được viết trước.

#### Vai trò của Options

Options tạo cờ xem xét và giúp phân biệt xung đột material với xung đột giả do khác horizon. Nó không có quyền đổi hướng luận điểm đã được giá xác nhận.

### Sai lầm thường gặp

* Thấy concentration rồi đảo chiều luận điểm.
* Trộn 30D skew với 5 phút entry.
* Nới dừng lỗ để “đi qua vùng Options”.
* Gọi model-sensitive exposure là chắc chắn.

### Ghi nhớ

> Khi Options xung đột với giá, hãy phân loại xung đột, kiểm tra horizon và quản trị đường đi. Đừng viết lại điều thị trường đang thực sự làm.

## Chương 80. Order Flow mạnh tại vị trí sai

Kịch bản giao dịch này chuyển lý thuyết Tam Trụ thành một kế hoạch có thể quan sát, kiểm tra và từ chối. Nó không phải mẫu hình bảo đảm; mọi lệnh phải đi qua Vị trí, Episode, Bằng chứng, Nỗ lực–Kết quả, vô hiệu và rủi ro.



### Nội dung cốt lõi

#### Bản chất

Tín hiệu Order Flow có thể là thật nhưng không tạo lợi thế vì nằm giữa phạm vi, sát rào cản hoặc xuất hiện sau một đoạn mở rộng.

#### Bối cảnh phù hợp

Footprint/Delta mạnh nhưng AMT vị trí không phù hợp.

#### Cấu trúc thiết lập

Không thiết lập; chờ giá đến mốc tham chiếu hoặc xây vùng giá trị mới.

#### Điều kiện kích hoạt

* Xuất hiện vị trí có ý nghĩa.
* Khoảng trống tới mục tiêu được mở ra.
* Episode hình thành.

#### Vô hiệu

Không cần điều kiện vô hiệu khi chưa vào lệnh; nếu đã vào, thoát theo kế hoạch khi tiền đề không còn.

#### Mục tiêu và quản lý

Mục tiêu không rõ là lý do không giao dịch.

#### Vai trò của Options

Trụ Options có thể cho biết volatility regime, horizon, vùng concentration và cơ chế exposure nào đáng quan sát. Nó điều chỉnh kỳ vọng về đường đi, yêu cầu xác nhận và quản trị theo policy, nhưng không thay Vị trí, Episode, Sự chấp nhận hoặc Nỗ lực–Kết quả.

### Quy trình áp dụng

1. Xác định bối cảnh và mốc tham chiếu trước khi giá đến.
2. Theo dõi Episode mà không dự đoán kết quả.
3. Chờ điều kiện kích hoạt thuộc đúng phong cách vào.
4. Tính điểm vô hiệu, khoảng trống tới mục tiêu và rủi ro.
5. Bỏ lệnh nếu thiếu điều kiện hoặc xuất hiện xung đột lớn.

### Sai lầm thường gặp

* FOMO vì màu Footprint.
* Tự tạo mốc tham chiếu quanh một tín hiệu.
* Đuổi theo chuyển động cuối.

### Ghi nhớ

> Một kịch bản giao dịch chỉ có giá trị khi điều kiện không giao dịch rõ ngang với điều kiện vào.

## Chương 81. Không giao dịch

Kịch bản giao dịch này chuyển lý thuyết Tam Trụ thành một kế hoạch có thể quan sát, kiểm tra và từ chối. Nó không phải mẫu hình bảo đảm; mọi lệnh phải đi qua Vị trí, Episode, Bằng chứng, Nỗ lực–Kết quả, vô hiệu và rủi ro.



### Nội dung cốt lõi

#### Bản chất

Không giao dịch là một quyết định chủ động khi dữ liệu, vị trí, bằng chứng, hình học hoặc rủi ro không đủ. Nó bảo vệ vốn và sự rõ ràng.

#### Bối cảnh phù hợp

Episode chưa được giải quyết, dữ liệu không hợp lệ, hợp đồng hoặc ánh xạ sai, thiếu khoảng trống mục tiêu, sự kiện rủi ro, đã chạm giới hạn ngày hoặc luận điểm còn xung đột.

#### Cấu trúc thiết lập

Quyết định không vào được ghi cùng lý do và điều kiện sẽ làm mở lại quan sát.

#### Điều kiện kích hoạt

* Điều kiện bắt buộc không giao dịch: dữ liệu sai, hợp đồng sai hoặc đã chạm giới hạn rủi ro.
* Cần đánh giá lại khi trạng thái chưa được giải quyết, thanh khoản mỏng hoặc bằng chứng xung đột.
* Không có khoảng trống hợp lý tới mục tiêu.
* Không có điểm vô hiệu rõ ràng.
* Cảm xúc hoặc mệt mỏi.

#### Vô hiệu

Quyết định không giao dịch chỉ hết hiệu lực khi điều kiện còn thiếu được đáp ứng và thiết lập vẫn còn giá trị; không đuổi giá.

#### Mục tiêu và quản lý

Mục tiêu của kịch bản này là bảo toàn vốn và sự tập trung.

#### Vai trò của Options

Trụ Options có thể cho biết volatility regime, horizon, vùng concentration và cơ chế exposure nào đáng quan sát. Nó điều chỉnh kỳ vọng về đường đi, yêu cầu xác nhận và quản trị theo policy, nhưng không thay Vị trí, Episode, Sự chấp nhận hoặc Nỗ lực–Kết quả.

### Quy trình áp dụng

1. Xác định bối cảnh và mốc tham chiếu trước khi giá đến.
2. Theo dõi Episode mà không dự đoán kết quả.
3. Chờ điều kiện kích hoạt thuộc đúng phong cách vào.
4. Tính điểm vô hiệu, khoảng trống tới mục tiêu và rủi ro.
5. Bỏ lệnh nếu thiếu điều kiện hoặc xuất hiện xung đột lớn.

### Sai lầm thường gặp

* Coi quyết định không giao dịch là thất bại.
* Chờ quá lâu rồi lại đuổi giá.
* Không ghi lý do.

### Ghi nhớ

> Một kịch bản giao dịch chỉ có giá trị khi điều kiện không giao dịch rõ ngang với điều kiện vào.

# PHẦN VIII — QUY TRÌNH GIAO DỊCH

## Chương 82. Phân tách thị trường phân tích và thị trường thực thi

Trong quy trình này, **hợp đồng tương lai vàng (GC)** là thị trường AMT và Order Flow. **Options trên futures vàng** là thị trường định giá rủi ro. **XAUUSD CFD** hoặc sản phẩm CFD tương ứng là thị trường thực thi.

&#x20;   GC futures
    → AMT, Profile, Footprint, Tape, Delta, DOM và MBO

    Gold options on futures
    → IV surface, term structure, skew, flow, OI và exposure theo expiry/strike

    CFD
    → điểm vào, dừng lỗ, mục tiêu và quản lý vị thế


### Quy tắc bắt buộc

* Không dùng sổ lệnh CFD để đại diện cho GC.
* Mọi mốc, Episode, acceptance và Order Flow được quyết định trên GC.
* Options phải ánh xạ đúng underlying futures contract.
* Không dùng XAUUSD Spot làm underlying của Options model nếu chưa có quy tắc basis.
* Không sao chép số giá GC hoặc strike Options trực tiếp sang CFD.
* Mức thực thi phải chuyển theo basis đo tại thời điểm đối chiếu.

### Trước phiên phải ghi

&#x20;   Hợp đồng GC đang phân tích
    Underlying futures của từng expiry Options
    Giá futures tại snapshot Options
    Giá GC tại thời điểm đối chiếu
    Bid/Ask của CFD
    Basis hiện tại
    Chênh lệch mua bán CFD
    Độ trễ giữa các nguồn
    Trạng thái chuyển tháng


### Điều kiện chặn tuyệt đối

Không thực thi khi:

* spread CFD mở rộng bất thường;
* basis thay đổi nhanh hoặc mất ổn định;
* luồng GC/CFD bị trễ;
* Options mapping sai underlying hoặc expiry;
* Options snapshot có look-ahead hoặc timestamp không xác định;
* nền tảng mất kết nối;
* sai hợp đồng/chuyển tháng chưa hiệu chỉnh;
* CFD không phản ánh GC đủ ổn định.

### Nhật ký ba lớp giá

&#x20;   Giá kích hoạt trên GC
    Giá futures underlying dùng cho Options snapshot
    Strike/vùng Options liên quan
    Giá thực thi trên CFD
    Mức vô hiệu trên GC
    Dừng lỗ trên CFD
    Basis tại điểm vào/thoát
    Spread và trượt giá


### Ghi nhớ

> Ba thị trường có ba chức năng. Không có ánh xạ đo lường, một mốc đúng trên chain hoặc futures có thể biến thành lệnh sai trên CFD.

## Chương 83. Chuẩn bị trước phiên

Chuẩn bị trước phiên không nhằm dự đoán ngày tăng hay giảm. Mục tiêu là chuẩn bị bản đồ AMT, Options regime, kịch bản và điều kiện đứng ngoài.

### Nội dung cốt lõi

#### Dữ liệu và sản phẩm

Xác minh GC contract, tick, phiên, basis, nguồn dữ liệu, underlying mapping của options, expiry và trạng thái quote/OI.

#### Lịch sự kiện

Đánh dấu tin kinh tế, phát biểu ngân hàng trung ương, dữ liệu lạm phát, việc làm, địa chính trị và các expiry/event horizon liên quan.

#### Bản đồ đấu giá

Đánh dấu vùng giá trị, POC, Composite, phiên qua đêm, mốc tuần/tháng và vùng cấu trúc.

#### Bối cảnh

Phân loại Cân bằng, Khám phá giá hoặc Chuyển tiếp; ghi bối cảnh nhiều phiên, trong ngày và vị trí hiện tại.

#### Bản đồ Options

Ghi:

&#x20;   snapshot time và data quality
    underlying/expiry buckets
    ATM IV và term structure
    skew/convexity
    expected move theo horizon
    volume/OI/ΔOI concentration
    Options Flow state
    exposure scenarios và vùng nhạy cảm
    Options regime


#### Kịch bản

Viết FAR, AAC, Luân phiên và không giao dịch quanh các mốc chính. Mỗi kịch bản ghi Options hỗ trợ, xung đột, trung tính hoặc không dùng.

#### Rủi ro

Ghi giới hạn lỗ, số giao dịch, event policy, regime policy và trạng thái người giao dịch.

### Giao thức GC trước phiên

&#x20;   Hợp đồng GC đang hoạt động
    Trạng thái chuyển tháng
    Mẫu phiên và lịch tin
    Kết nối dữ liệu
    Chính sách đặt lại CVD/Composite
    Options snapshot point-in-time
    OI availability date
    Options regime và horizon
    Basis GC–CFD


### Quy trình áp dụng

1. Kiểm tra dữ liệu ba thị trường.
2. Xây bản đồ AMT.
3. Xây Options regime và vùng nhạy cảm.
4. Viết hai đến bốn kịch bản.
5. Ghi bằng chứng còn thiếu.
6. Ghi giới hạn risk.
7. Không viết dự đoán hướng bắt buộc.

### Sai lầm thường gặp

* Vẽ quá nhiều vùng Options.
* Dùng OI chưa được biết tại thời điểm chuẩn bị.
* Không kiểm tra underlying/expiry.
* Chỉ nhìn GEX mà bỏ term/skew/flow.
* Chuẩn bị khi giá đã chạy.

### Ghi nhớ

> Chuẩn bị tốt không làm ta biết trước kết quả. Nó làm giảm số câu chuyện phải sáng tác khi dữ liệu bắt đầu chuyển động.

## Chương 84. Quy trình đọc thị trường trong 90 giây

Khi giá đến vùng quan trọng, người giao dịch cần một lượt quét ngắn nhưng đủ lớp để tránh bị cuốn vào vi cấu trúc hoặc dashboard Options.

### Nội dung cốt lõi

#### 1\. Trạng thái AMT

Cân bằng, Khám phá giá hay Chuyển tiếp?

#### 2\. Vị trí

Trong vùng giá trị, tại biên, ngoài vùng hay tại trung tâm?

#### 3\. Mốc và Episode

Mốc nào, khung nào, lần thử thứ mấy, độ lệch và tái nhập ra sao?

#### 4\. Acceptance

Hoạt động đang xây trong hay ngoài vùng cũ?

#### 5\. Options

* Horizon nào liên quan?
* Data quality còn tốt không?
* Regime là stability, expansion, event, expiry, mixed hay neutral?
* Expected move/concentration nào ở gần?
* Flow/IV/skew có đang đổi không?

#### 6\. Order Flow

Nỗ lực bên nào, kết quả và khả năng duy trì thế nào?

#### 7\. Bằng chứng còn thiếu

Cần kiểm tra lại, thêm thời gian, acceptance hay repricing?

#### 8\. Quyết định

FAR, AAC, Luân phiên, Chờ hoặc không giao dịch?

#### 9\. Rủi ro

Vô hiệu, target corridor, RR, event và basis?

### Quy trình áp dụng

1. Trả lời một câu cho mỗi mục.
2. Nếu horizon/dữ liệu mâu thuẫn, trạng thái là Chờ.
3. Không bỏ qua risk.
4. Không vào chỉ vì Options regime trông hấp dẫn.

### Sai lầm thường gặp

* Bắt đầu từ Delta hoặc một trade Options lớn.
* Không kiểm tra horizon/timestamp.
* Vội giao dịch để kịp giá.
* Biến lượt quét thành danh sách không có quyết định.

### Ghi nhớ

> Quy trình 90 giây không nhằm quyết định thật nhanh. Nó nhằm nhận ra khi chưa đủ quyền quyết định.

## Chương 85. Quy trình ra quyết định

Một giao dịch đi qua chín bước: quan sát, đóng khung, giả thuyết, kiểm tra, chờ, thực thi, quản lý, thoát và đánh giá lại.



### Nội dung cốt lõi

#### Quan sát

Thu dữ liệu không phán xét.

#### Đóng khung

Trạng thái, vị trí, mốc tham chiếu và Episode.

#### Giả thuyết

FAR/AAC/Luân phiên hoặc không giao dịch ứng viên.

#### Kiểm tra

Sự chấp nhận, Nỗ lực–Kết quả, Options regime và các xung đột theo horizon.

#### Chờ

Bằng chứng còn thiếu và phong cách vào lệnh.

#### Thực thi

Loại lệnh, khối lượng vị thế, dừng lỗ và mục tiêu.

#### Quản lý

hành vi kỳ vọng và vô hiệu.

#### Thoát

Mục tiêu, vô hiệu luận điểm, dừng lỗ theo thời gian hoặc tình huống khẩn cấp.

#### Đánh giá lại

Quy trình trước P\&L.

### Quy trình áp dụng

1. Không bỏ bước.
2. Dùng bảng kiểm.
3. Ghi dấu thời gian.
4. Khi luận điểm đổi, đóng lệnh trước khi viết luận điểm mới.

### Sai lầm thường gặp

* Nhảy từ quan sát sang lệnh.
* Đổi giả thuyết chỉ để tiếp tục giữ vị thế.
* Đánh giá lại chỉ P\&L.

### Ghi nhớ

> Quy trình là lợi thế hành vi khi thị trường không chắc chắn.

## Chương 86. Quản lý giao dịch

Sau điểm vào, người giao dịch không cần giải thích mọi tick. Quản lý dựa trên hành vi kỳ vọng, cấu trúc và rủi ro đã viết trước.



### Nội dung cốt lõi

#### Sau điểm vào

Giá phải phản ứng trong khoảng thời gian hợp lý theo thiết lập. Tái nhập FAR thường cần tiến triển vào vùng giá trị cũ; kiểm tra lại AAC cần khôi phục vùng ngoài.

#### Không tiến triển

Thời gian dừng lỗ hoặc giảm rủi ro nếu nỗ lực tăng nhưng kết quả không xuất hiện.

#### Vùng giá trị thay đổi

POC/sự dịch chuyển vùng giá trị theo luận điểm hỗ trợ giữ; dịch chuyển ngược là cảnh báo.

#### Order Flow thay đổi

Áp lực chủ động phía đối diện chỉ quan trọng nếu tạo kết quả và sự duy trì.

#### Chốt một phần

Chốt tại rào cản/1R theo chính sách, không tùy cảm xúc.

#### Dời điểm dừng

Dời điểm dừng theo cấu trúc, vùng giá trị mới hoặc các điểm xoay; không siết dừng lỗ chỉ vì sợ mất lợi nhuận.

#### Tình huống khẩn cấp

Mất kết nối dữ liệu, chênh lệch mua bán bất thường hoặc sự kiện bất ngờ có thể yêu cầu thoát lệnh.

### Quy trình áp dụng

1. Theo dõi hành vi kỳ vọng.
2. Không đổi mục tiêu/dừng lỗ tùy tick.
3. Ghi lý do cho mỗi hành động.
4. Thoát khi luận điểm mất hiệu lực.
5. Không đảo chiều vị thế ngay trừ khi có luận điểm mới.

### Sai lầm thường gặp

* Quản lý vi mô quá mức.
* Dời dừng lỗ hòa vốn quá sớm.
* Giữ vì “chắc quay lại”.

### Ghi nhớ

> Quản lý tốt là thi hành hợp đồng, không phải thương lượng với thị trường.

## Chương 87. Nhật ký giao dịch

Nhật ký biến trải nghiệm thành dữ liệu. Với trụ Options, nhật ký phải lưu được snapshot point-in-time, không chỉ một ảnh dashboard sau phiên.

### Nội dung cốt lõi

#### Dữ liệu trước lệnh

Lưu trạng thái AMT, Footprint/Order Flow và Options snapshot id cùng timestamp.

#### Trường luận điểm

Trạng thái, vị trí, mốc, Episode, acceptance, bằng chứng, FAR/AAC/Luân phiên và phong cách vào.

#### Trường Options

Ghi:

* underlying contract;
* snapshot time và data quality;
* expiry/horizon;
* ATM IV, term structure, skew;
* expected move;
* concentration gần nhất;
* flow state;
* exposure scenario và giả định;
* Options regime;
* hỗ trợ/xung đột/trung tính/không dùng.

#### Trường rủi ro

Điểm vào, dừng lỗ, target, khối lượng, chi phí, event, basis và policy adjustment do Options nếu có.

#### Kết quả

Thoát, P\&L theo R, MFE, MAE, thời gian giữ, realized move, IV/skew change và regime transition.

#### Điểm quy trình

Chấm mức tuân thủ và tính point-in-time, không chấm việc đoán đúng hướng.

#### So sánh ngược

Ghi điều gì thay đổi nếu không dùng Options, dùng horizon khác hoặc dùng policy khác. Không viết lại snapshot sau khi biết kết quả.

### Trường bắt buộc khi phân tích GC và thực thi CFD

* Giá kích hoạt trên GC.
* Giá futures dùng trong Options snapshot.
* Giá thực thi trên CFD.
* Vô hiệu trên GC và dừng CFD.
* Basis vào/thoát.
* Spread và trượt giá.
* GC contract, Options underlying, expiry và độ trễ.

### Quy trình áp dụng

1. Ghi trước điểm vào.
2. Lưu snapshot id tại điều kiện kích hoạt.
3. Ghi hành động trong lệnh.
4. Đánh giá sau phiên.
5. Gắn nhãn setup, Options regime và data quality.

### Sai lầm thường gặp

* Chỉ lưu ảnh đã cập nhật sau kết quả.
* Không ghi OI availability date.
* Không lưu model version/assumption.
* Sửa regime sau giao dịch.
* Không ghi không giao dịch.

### Ghi nhớ

> Không tái tạo được dữ liệu Options tại thời điểm quyết định thì không thể biết hệ thống đã đúng, sai hay chỉ nhìn thấy tương lai.

## Chương 88. Đánh giá lại và phát triển phương pháp

Đánh giá lại tốt không hỏi “tại sao giao dịch này thua” một cách đơn lẻ. Nó hỏi một quy tắc hoạt động ra sao trên mẫu và chế độ khác nhau.



### Nội dung cốt lõi

#### Đánh giá giao dịch

Tách chất lượng thiết lập, thực thi, quản lý và yếu tố ngẫu nhiên.

#### Đánh giá theo tuần

Tổng hợp số thiết lập, số lần không giao dịch, kết quả theo R, lỗi và bối cảnh.

#### Đánh giá theo kịch bản giao dịch

FAR tái nhập, kiểm tra lại và xác nhận; các phong cách AAC; vị trí.

#### MFE/MAE

Dùng phân phối để cải thiện dừng lỗ/mục tiêu, không chọn một mẫu đẹp.

#### Kích thước mẫu

Không sửa quy tắc sau vài giao dịch. Cần đủ mẫu và phân tầng theo chế độ.

#### Kiểm tra cuốn chiếu

Quy tắc được phát triển trên giai đoạn trước và kiểm tra trên giai đoạn sau để giảm nguy cơ quá khớp.

#### Kiểm soát thay đổi

Mỗi thay đổi phải có lý do, giả thuyết, phạm vi áp dụng và kế hoạch đánh giá.

### Quy trình áp dụng

1. Đánh giá lại quy trình theo chu kỳ cố định.
2. Đề xuất một thay đổi có giả thuyết rõ.
3. Kiểm tra lại bằng dữ liệu lịch sử hoặc phát lại.
4. Giao dịch mô phỏng hoặc trực tiếp với quy mô rất nhỏ.
5. Đánh giá trên dữ liệu ngoài mẫu.
6. Chỉ sau đó mới khóa quy tắc.

### Sai lầm thường gặp

* Tối ưu theo P\&L vài ngày.
* Xóa giao dịch xấu.
* Thay nhiều biến cùng lúc.

### Ghi nhớ

> Một phương pháp sống bằng kiểm chứng có kỷ luật, không bằng liên tục thêm điều kiện.

# PHẦN IX — NGHIÊN CỨU, KIỂM CHỨNG VÀ THƯ VIỆN TÌNH HUỐNG

## Chương 89. Chuẩn hóa dữ liệu và cấu hình quan sát

Một kết luận chỉ đáng tin khi dữ liệu và cách tính có thể tái tạo. Thay đổi cách gom futures hoặc cách lọc chain có thể làm Delta, POC, IV surface, flow và exposure thay đổi dù thị trường gốc không đổi.

### Cấu hình futures phải cố định

* Hợp đồng và quy tắc chuyển tháng.
* Múi giờ và mẫu phiên.
* Loại thanh và bước gom Footprint.
* Phân loại Bid/Ask.
* Ngưỡng Imbalance.
* Cách đặt lại CVD.
* Phạm vi Profile/Composite.
* Bộ lọc trade lớn, iceberg, stops hoặc sweep.

### Cấu hình Options phải cố định

* mapping option → underlying futures;
* timezone, expiration time và DTE convention;
* quote-age/spread filter;
* mid/mark policy;
* ATM và moneyness definition;
* IV model và input assumptions;
* interpolation/extrapolation policy;
* expiry buckets;
* complex-trade grouping;
* aggressor classification;
* OI availability policy;
* exposure formula, sign convention và scenario;
* expected-move method;
* regime thresholds;
* snapshot cadence.

### Kiểm tra tính toàn vẹn

&#x20;   Đúng futures contract và option underlying?
    Đúng expiry và thời điểm đáo hạn?
    Có khoảng trống dữ liệu?
    Quote có two-sided và đủ tươi?
    Futures/Options timestamp có đồng bộ?
    OI đã thực sự khả dụng tại thời điểm đó?
    Model/config có giống mẫu nghiên cứu?
    CFD basis có ổn định?


### Point-in-time và look-ahead

Mọi backtest Options phải dùng snapshot đúng thời điểm. Cấm sử dụng:

* OI cuối ngày cho buổi sáng cùng ngày;
* IV/Greeks được tính lại bằng surface sau phiên;
* complex-order labels chỉ biết sau xử lý;
* final settlement thay cho quote lúc quyết định;
* regime được gắn sau khi nhìn kết quả.

### Thay đổi cấu hình

Mỗi thay đổi có lý do, version, ngày áp dụng và giai đoạn đánh giá riêng. Không thay filter hoặc scenario sau khi thấy giao dịch.

### Ghi nhớ

> Một tín hiệu không thể tái tạo thường là sản phẩm của dữ liệu, mô hình hoặc cấu hình, không phải lợi thế của thị trường.

## Chương 90. Đo lường lợi thế và độ bất định

Lợi thế không được chứng minh bằng một biểu đồ đẹp hoặc một chuỗi thắng. Nó phải được đo trên một tập giao dịch có quy tắc nhất quán và chi phí thực tế.



### Các chỉ số tối thiểu

* Số giao dịch và số lần không giao dịch.
* Tỷ lệ thắng.
* Lãi trung bình và lỗ trung bình theo R.
* Kỳ vọng trung bình mỗi giao dịch.
* Hệ số lợi nhuận.
* Sụt giảm lớn nhất.
* Chuỗi thắng và chuỗi thua dài nhất.
* MFE, MAE và thời gian giữ lệnh.
* Chi phí, trượt giá và lệnh không khớp.

### Kỳ vọng

&#x20;   Kỳ vọng
    = Tỷ lệ thắng × Lãi trung bình
    - Tỷ lệ thua × Lỗ trung bình
    - Chi phí trung bình


Kỳ vọng ước lượng dương không có nghĩa lợi nhuận được bảo đảm. Sai số tăng khi mẫu nhỏ, dữ liệu phụ thuộc nhau hoặc quy tắc được chọn sau khi xem kết quả.

### Phân tầng kết quả

Không gộp mọi giao dịch vào một con số. Cần phân tầng theo:

* FAR, AAC, luân phiên hoặc không gian khác.
* Phong cách Sớm, Tiêu chuẩn hoặc Xác nhận.
* Loại mốc tham chiếu.
* Phiên và thời điểm trong ngày.
* Chế độ biến động, thanh khoản và sự kiện.
* Options regime, horizon, term/skew state, flow state và data quality.
* Thuận hoặc ngược bối cảnh nhiều phiên.

### Độ bất định

Một tỷ lệ thắng hoặc kỳ vọng phải được trình bày cùng kích thước mẫu và phạm vi biến động. Không so sánh hai thiết lập chỉ dựa trên chênh lệch nhỏ khi số giao dịch còn ít.

### Ghi nhớ

> Lợi thế là một phân phối kết quả có độ bất định, không phải một tỷ lệ thắng được khắc trên đá.

## Chương 91. Kiểm định độ bền và rủi ro chuỗi thua

Một quy tắc tốt phải sống được ngoài giai đoạn đã dùng để xây nó.



### Tách dữ liệu

* Giai đoạn phát triển dùng để hình thành giả thuyết.
* Giai đoạn kiểm tra dùng để đánh giá mà không chỉnh quy tắc.
* Giai đoạn theo dõi trực tiếp dùng để xác nhận khả năng thực thi.

### Kiểm tra cuốn chiếu

Quy tắc được phát triển trên một cửa sổ quá khứ, kiểm tra trên giai đoạn tiếp theo, rồi cửa sổ được dịch chuyển. Cách này giúp phát hiện quy tắc chỉ hoạt động trong một chế độ ngắn.

### Phân tích độ nhạy

Một quy tắc đáng tin không nên sụp đổ chỉ vì thay đổi rất nhỏ của:

* Ngưỡng Delta/Imbalance và Options regime thresholds.
* Độ rộng vùng mốc.
* Thời gian chờ duy trì.
* Khoảng dừng lỗ hoặc mục tiêu.
* Chi phí và trượt giá giả định.

Nếu chỉ một giá trị rất cụ thể tạo kết quả đẹp, nguy cơ khớp quá mức rất cao.

### Mô phỏng thứ tự giao dịch

Có thể xáo trộn thứ tự các kết quả hoặc lấy mẫu lại từ lịch sử để quan sát nhiều đường vốn khả dĩ. Mục tiêu không phải dự báo chính xác, mà là ước lượng chuỗi thua và sụt giảm có thể lớn hơn lịch sử đã thấy.

### Nhiều phép thử

Khi thử rất nhiều ngưỡng, chỉ báo và biến thể, một số kết quả đẹp sẽ xuất hiện do ngẫu nhiên. Phải ghi lại toàn bộ số giả thuyết đã thử, không chỉ giữ biến thể thắng cuộc.

### Ghi nhớ

> Một phương pháp bền không cần mọi giai đoạn đều đẹp; nó cần giữ được logic và khả năng sống sót khi môi trường thay đổi.

## Chương 92. Xây dựng thư viện tình huống

Tài liệu chuyên sâu cần ví dụ theo trình tự thời gian, không chỉ hình sau khi kết quả đã rõ. Mỗi tình huống phải cho thấy điều người giao dịch biết tại từng thời điểm.



### Bốn nhóm bắt buộc

1. FAR thành công và FAR thất bại.
2. AAC thành công và AAC thất bại.
3. Trạng thái chưa được giải quyết dẫn đến đứng ngoài.
4. Tín hiệu Order Flow mạnh nhưng xuất hiện tại vị trí sai.

### Cấu trúc một tình huống

&#x20;   1. Bối cảnh trước phiên
    2. Vùng mốc và lý do chọn
    3. Ảnh trước khi giá đến
    4. Lần tương tác đầu tiên
    5. Lần thử đấu giá ngoài vùng
    6. Bằng chứng chấp nhận hoặc tái nhập
    7. Options snapshot, regime và các giả định tại thời điểm đó
    8. Dòng lệnh và kết quả giá
    9. Điểm vào, vô hiệu và mục tiêu
    10. Quản lý trong lệnh
    11. Kết quả và điều không được hợp lý hóa sau sự kiện


### Phản ví dụ

Mỗi ví dụ tốt phải đi kèm một phản ví dụ có hình dạng gần giống nhưng kết quả khác. Điều này buộc người học nhìn vào vị trí, khả năng duy trì và kết quả, thay vì học thuộc hình mẫu.

### So sánh lựa chọn

Sau mỗi tình huống, ghi thêm:

* Điều gì xảy ra nếu dùng phong cách vào khác?
* Đứng ngoài có phải quyết định tốt hơn không?
* Bằng chứng nào chỉ xuất hiện sau khi đã quá muộn?
* Quy tắc nào cần giữ nguyên dù giao dịch thắng hoặc thua?

### Ghi nhớ

> Một thư viện tình huống tốt dạy quá trình ra quyết định; một album biểu đồ đẹp chỉ dạy cách kể chuyện sau khi biết kết quả.

# PHẦN X — GIẢNG DẠY PHƯƠNG PHÁP

## Chương 93. Lộ trình đào tạo bốn cấp

Để dạy được, kiến thức phải đi từ đọc cuộc đấu giá, đọc thực thi, đọc trụ Options rồi mới hợp nhất. Không cho người học chọn điểm vào trước khi biết vị trí và quyền hạn dữ liệu.

### Nội dung cốt lõi

#### Cấp 1: Người đọc đấu giá

Hiểu AMT, Profile, vùng giá trị, Cân bằng, Khám phá giá, mốc và dịch chuyển giá trị. Yêu cầu là mô tả thị trường mà chưa vội Long/Short.

#### Cấp 2: Người đọc Order Flow

Hiểu Bid/Ask, Delta, CVD, Footprint, Imbalance và Nỗ lực–Kết quả. Phân biệt dữ liệu thô, ứng viên và kết luận.

#### Cấp 3: Người đọc Options

Hiểu contract mapping, expiry/DTE, moneyness, IV, term structure, skew, volume, OI, flow, Greeks, expected move, GEX và exposure scenarios. Phải phân biệt dữ liệu quan sát, dữ liệu tính và dealer narrative.

#### Cấp 4: Người thực hành Tam Trụ

Kết hợp Options regime với Episode, FAR/AAC, luận điểm, vô hiệu, target, risk và nhật ký point-in-time.

#### Tiêu chuẩn chuyển cấp

Người học phải giải thích khái niệm, phản ví dụ, dữ liệu cần, horizon và điều chưa thể kết luận.

### Quy trình giảng dạy

1. Dạy khái niệm.
2. Cho dữ liệu sạch.
3. Cho phản ví dụ.
4. Yêu cầu giải thích lại.
5. Sửa ngôn ngữ/quyền kết luận.
6. Chỉ chuyển sang setup khi nền tảng vững.

### Sai lầm thường gặp

* Dạy GEX trước Options data model.
* Dạy dealer positioning như sự thật.
* Chỉ cho surface đẹp hoặc trade lớn.
* Chấm năng lực theo P\&L.

### Ghi nhớ

> Người học thật sự hiểu Options khi họ có thể nói cả công dụng, horizon, giả định và lý do một kết luận có thể sai.

## Chương 94. Phương pháp giảng một bài

Mỗi bài giảng nên có cùng nhịp để người học xây mô hình tinh thần ổn định.

### Nội dung cốt lõi

#### Mục tiêu

Viết hành vi có thể quan sát, chẳng hạn “phân biệt tái nhập và tái chấp nhận”, thay vì chỉ viết “hiểu sự chấp nhận”.

#### Khái niệm đơn giản

Bắt đầu bằng hình ảnh cuộc đấu giá trước khi đưa thuật ngữ.

#### Định nghĩa kỹ thuật

Nêu dữ liệu, phạm vi, công dụng và giới hạn. Với mỗi chỉ báo phải trả lời:

&#x20;   Nó đo cái gì?
    Nó dùng để làm gì?
    Nó không đo được gì?
    Khi nào dữ liệu mất giá trị?


#### Ví dụ và phản ví dụ

Dùng một ví dụ phù hợp và một trường hợp gần giống nhưng không đủ điều kiện. Cách này ngăn người học chỉ ghi nhớ hình dạng.

#### Điều không được kết luận

Mỗi bài bắt buộc có phần giới hạn. Ví dụ: futures Delta đo áp lực chủ động, không đo vị thế; Options Delta là độ nhạy mô hình; GEX Flip là kết quả phụ thuộc công thức và giả định, không phải điểm đảo chiều bắt buộc.

#### Giải thích lại

Người học phải diễn đạt lại quy trình và tự chỉ ra bằng chứng còn thiếu. Mục tiêu là kiểm tra cách nghĩ, không phải học thuộc câu chữ.

### Quy trình áp dụng

1. Mở bằng vấn đề thực tế.
2. Giải thích khái niệm.
3. Nêu công cụ quan sát.
4. Minh họa.
5. Đưa phản ví dụ.
6. Chốt điều không được kết luận.
7. Yêu cầu người học giải thích lại.

### Sai lầm thường gặp

* Giảng liên tục mà không kiểm tra cách hiểu.
* Chỉ dùng biểu đồ sau khi đã biết kết quả.
* Không sửa ngôn ngữ quá chắc chắn.
* Giới thiệu công cụ mà không nói nó dùng để làm gì.

### Ghi nhớ

> Một bài giảng tốt dạy cách đặt câu hỏi đúng trước khi dạy cách nhận mẫu.

# PHỤ LỤC

## Phụ lục A. Từ điển AMT và công cụ cấu trúc

|Thuật ngữ hoặc công cụ|Khái niệm|Dùng để làm gì|
|-|-|-|
|AMT|Lý thuyết xem thị trường như cuộc đấu giá hai chiều|Xác định Cân bằng, Khám phá giá, vị trí và Sự chấp nhận|
|đấu giá|Quá trình thị trường tìm giá và tổ chức giao dịch|Đọc mục đích hiện tại của chuyển động|
|Vùng cân bằng|Vùng giao dịch hai chiều được duy trì|Ưu tiên luân phiên và kiểm tra phá biên|
|Khám phá giá|Quá trình tìm vùng giá mới|Theo dõi vùng giá trị và POC có dịch theo giá hay không|
|Chuyển tiếp|Giai đoạn giữa cân bằng cũ và cân bằng mới|Hạ độ chắc chắn và chờ thêm bằng chứng|
|Market Profile|Hồ sơ tổ chức giá theo thời gian|Nhìn hình dạng và sự phát triển của cuộc đấu giá|
|TPO|Cơ hội thời gian tại giá|Đo mức độ lặp lại của giá qua các khoảng thời gian|
|Volume Profile|Khối lượng đã khớp phân bổ theo giá|Xác định POC, vùng giá trị, HVN và LVN|
|vùng giá trị|Vùng hoạt động được duy trì theo thước đo đã chọn|Phân biệt giao dịch trong vùng và ngoài vùng|
|POC|Mức hoạt động lớn nhất trong phạm vi|Xác định trung tâm hoạt động|
|VAH / VAL|Biên trên / dưới vùng giá trị|Mở Episode khi giá kiểm tra biên|
|HVN / LVN|Nút khối lượng cao / thấp|Nhìn vùng luân phiên và vùng đi nhanh|
|Initial Balance|Phạm vi đầu phiên theo quy ước|Theo dõi mở rộng phạm vi|
|Composite|Hồ sơ nhiều phiên thuộc cùng một đấu giá|Xác định Vùng cân bằng nhiều ngày và biên lớn|
|OTF|One-Time Framing|Đo nhịp tiến triển một chiều qua các khoảng hoàn tất|
|VWAP|Giá trung bình có trọng số khối lượng|Tham chiếu giá trung bình trong phạm vi|
|Anchored VWAP|VWAP bắt đầu từ mốc chọn trước|Theo dõi giá trung bình từ sự kiện hoặc điểm khởi phát|
|Mốc tham chiếu|Mức hoặc vùng đặt câu hỏi cho thị trường|Chọn nơi mở Episode|
|Episode|Vòng đời tương tác giữa giá và một mốc tham chiếu|Theo dõi từ lần chạm, độ lệch đến kết quả giải quyết|
|Độ lệch|Giá giao dịch ngoài mốc tham chiếu|Đo cuộc thử giá mà không mặc định có quét thanh khoản|
|Sự chấp nhận|Hoạt động hoặc vùng giá trị được duy trì ở vùng mới|Phân biệt AAC với phá biên tạm thời|
|Tái nhập|Giá quay vào vùng cũ về hình học|Bắt đầu kiểm tra FAR|
|Tái chấp nhận|Hoạt động được duy trì lại trong vùng cũ|Xác nhận cuộc đấu giá ngoài đã thất bại|
|FAR|Failed Auction Re-entry|Giao dịch hướng về vùng đấu giá cũ sau tái chấp nhận|
|AAC|Accepted Auction Continuation|Giao dịch tiếp diễn sau khi giá được chấp nhận ngoài vùng cũ|
|PLAR|Đường đi ít cản trở của đấu giá|Xây đường đi và hành lang mục tiêu|

## Phụ lục B. Từ điển Order Flow và công cụ thực thi

|Thuật ngữ hoặc công cụ|Khái niệm|Dùng để làm gì|
|-|-|-|
|Order Flow|Dữ liệu về giao dịch đã thực sự khớp|Đo nỗ lực thực thi và kết quả|
|Bid / Ask|Giá mua chờ cao nhất / giá bán chờ thấp nhất|Hiểu nơi giao dịch chủ động khớp|
|Phía chủ động|Phía chấp nhận mức giá hiện có để khớp ngay|Phân loại mua hoặc bán chủ động|
|Tape / Time \& Sales|Chuỗi giao dịch đã khớp theo thời gian|Đọc tốc độ, nhịp và kích thước giao dịch|
|Footprint|Bid/Ask và hoạt động theo từng mức giá trong thanh|Quan sát vi cấu trúc tại Vị trí|
|Delta|Ask Volume trừ Bid Volume|Đo chênh lệch áp lực chủ động|
|CVD|Delta cộng dồn|Theo dõi tiến trình áp lực chủ động|
|Imbalance|Chênh lệch Bid/Ask theo quy tắc|Phát hiện bất đối xứng thực thi|
|Stacked Imbalance|Nhiều mức liền nhau cùng phía|Nhận diện áp lực chủ động khởi xướng ứng viên|
|Cluster|Cụm hoạt động theo mức giá|Nhìn phân bố khối lượng và giao dịch|
|Volume|Tổng hợp đồng đã khớp|Đo một thành phần của Nỗ lực|
|Số giao dịch|Số lần khớp|Đo độ phân mảnh của hoạt động|
|Kích thước giao dịch trung bình|Khối lượng chia số giao dịch|So sánh đặc tính của hoạt động|
|Ứng viên hấp thụ|Nỗ lực lớn nhưng tiến triển hạn chế tại Vị trí|Đặt giả thuyết lực thụ động đang cản|
|Ứng viên cạn kiệt|Nỗ lực giảm và tiến triển suy yếu|Đặt giả thuyết phía chủ động đang hụt sức|
|Nỗ lực|Khối lượng, số giao dịch, áp lực chủ động, thời gian và tốc độ|Đo mức nỗ lực|
|Kết quả|Tiến triển, phạm vi, sự duy trì, vùng giá trị và POC dịch chuyển|Đo hiệu quả của nỗ lực|
|Khả năng tạo thuận lợi cho giao dịch|Chất lượng tiến triển theo hướng đấu giá đang thử|Phân biệt chuyển động khỏe và yếu|
|MFE / MAE|Thuận lợi / bất lợi tối đa sau điểm vào|Đánh giá lại quản trị và vị trí dừng lỗ|

## Phụ lục C. Từ điển trụ Options

|Thuật ngữ hoặc trường|Khái niệm|Dùng để làm gì|Giới hạn chính|
|-|-|-|-|
|Call / Put|Quyền mua / quyền bán futures underlying|Xác định loại hợp đồng|Không tự đồng nghĩa bullish/bearish|
|Strike|Giá thực hiện|Trục giá của chain|Không phải hỗ trợ/kháng cự bắt buộc|
|Expiry / DTE|Thời điểm đáo hạn / thời gian còn lại|Tách horizon và độ nhạy|Ngày không đủ nếu không biết giờ đáo hạn|
|Underlying futures|Hợp đồng futures mà option dẫn tới|Tính moneyness/Greeks đúng|Không thay bằng Spot tùy ý|
|Bid / Ask / Mid|Báo giá option|Giá, IV và flow classification|Spread rộng/stale làm sai kết quả|
|Volume|Hợp đồng giao dịch trong khoảng đo|Đo hoạt động mới|Không nói vị thế còn mở|
|Open Interest|Hợp đồng còn mở|Đo concentration tồn kho|Không cho biết phía Long/Short|
|ΔOI|Thay đổi OI sau bù trừ|Theo dõi xây/tháo concentration|Thường có độ trễ; cấm look-ahead|
|Premium / Notional|Giá trị giao dịch|So sánh quy mô flow|Không phải rủi ro hướng thuần|
|Moneyness|Vị trí strike so với underlying/forward|Chuẩn hóa surface/skew|Phải dùng cùng quy ước|
|IV|Biến động hàm ý|Đọc giá của rủi ro|Không phải dự báo chắc chắn|
|ATM IV|IV tại vùng ATM theo phương pháp|Mốc term structure|Phải ghi cách chọn ATM|
|Term structure|IV theo expiry|Đọc premium theo horizon|Không suy hướng giá|
|Skew|IV theo moneyness|Đọc bất đối xứng rủi ro|Wing kém thanh khoản dễ gây nhiễu|
|Risk reversal|Chênh IV call/put cùng Delta|Đo hướng skew tương đối|Không trực tiếp là flow|
|Butterfly/Convexity|Độ đắt của wing so ATM|Đọc tail pricing|Phụ thuộc nội suy và thanh khoản|
|Delta|Độ nhạy premium với underlying|Moneyness/risk conversion|Không phải vị thế quan sát|
|Gamma|Độ thay đổi Delta theo underlying|Đọc convexity theo price|Đơn vị/phương pháp phải rõ|
|Vega|Độ nhạy premium với IV|Đọc rủi ro biến động|Khác nhau theo expiry|
|Theta|Độ nhạy premium theo thời gian|Đọc time decay mô hình|Không phải P\&L chắc chắn|
|Vanna|Độ nhạy Delta với IV theo quy ước|Nghiên cứu tương tác price–vol|Chỉ dùng khi công thức rõ|
|Charm|Độ thay đổi Delta theo thời gian|Nghiên cứu expiry dynamics|Không suy hedging chắc chắn|
|GEX|Gamma exposure tổng hợp|Một module exposure|Phụ thuộc phía vị thế và dấu|
|DEX/VEX|Delta/Vega exposure tổng hợp|Đọc sensitivity theo scenario|Không dùng nếu công thức mơ hồ|
|Absolute concentration|Exposure không dấu|Xác định vùng nhạy cảm|Không cho hướng hedging|
|Signed exposure|Exposure có dấu|Xây hedging scenario|Phải nêu giả định vị thế|
|Gamma Flip / Zero Gamma|Mức mô hình đổi dấu|Ranh giới scenario ứng viên|Không phải mức vật lý|
|Call Resistance / Put Support|Nhãn vendor|Vùng tham khảo nếu methodology rõ|Không phải khái niệm chuẩn hay lệnh|
|Expected move|Biên độ theo phương pháp/horizon|So biến động thực tế với định giá|Không phải biên bắt buộc|
|Options Flow|Giao dịch quyền chọn theo thời gian|Đọc hoạt động mới|Ý định có thể là spread/hedge|
|Flow Episode|Vòng đời một cụm flow|Theo dõi lặp lại và phản ứng|Một print đơn lẻ chưa đủ|
|Options Regime|Trạng thái tổng hợp theo horizon|Tóm tắt môi trường|Không tạo hướng độc lập|
|Data quality|Tốt/Hạn chế/Thận trọng/Không dùng|Kiểm soát quyền sử dụng|Phải tính theo từng module|

### Ma trận nguồn và quyền kết luận

|Lớp|Ví dụ|Quyền sử dụng|
|-|-|-|
|Quan sát|Bid/Ask, trade, volume, OI công bố|Mô tả điều đã xuất hiện|
|Tính toán|IV, Greeks, expected move, surface|Mô tả theo mô hình đã ghi|
|Tổng hợp|concentration, GEX/DEX/VEX|So sánh vùng và scenario|
|Suy luận|dealer hedging, pinning, acceleration|Chỉ đặt giả thuyết có phương án thay thế|

## Phụ lục D. Công thức nền tảng

&#x20;   Delta = Ask Volume - Bid Volume

    Kích thước giao dịch trung bình = Khối lượng đã khớp / Số giao dịch

    tiến triển mỗi hợp đồng = tiến triển giá ròng / khối lượng đã khớp

    Tỷ lệ thời gian ngoài vùng = Thời gian ngoài vùng / Tổng thời gian Episode
    Tỷ lệ khối lượng ngoài vùng = Khối lượng ngoài vùng / Tổng khối lượng Episode
    Tỷ lệ số giao dịch ngoài vùng = Số giao dịch ngoài vùng / Tổng số giao dịch Episode

    khối lượng vị thế = ngân sách rủi ro / (khoảng cách dừng lỗ × giá trị mỗi đơn vị + chi phí ước tính)

    Bội số R = P\&L / Rủi ro ban đầu


Các công thức chỉ tạo dữ liệu thô. “Cao”, “thấp”, “hiệu quả” và “cực đoan” cần phân phối theo sản phẩm, phiên và chế độ thị trường.

Các công thức Options phải được version hóa. Tối thiểu cần ghi:

&#x20;   Mid = (Bid + Ask) / 2, chỉ khi báo giá hợp lệ
    DTE = thời gian đến đáo hạn theo quy ước đã chọn
    Expected move\_IV ≈ Underlying × IV × sqrt(Time)
    Exposure = Greek × số hợp đồng × contract multiplier × giả định dấu vị thế


Công thức expected move, Greeks, GEX/DEX/VEX, skew và surface phụ thuộc mô hình, nguồn, đơn vị và quy ước dấu. Mỗi kết quả luôn được xem là dữ liệu mô hình, không phải vị thế dealer quan sát trực tiếp.

## Phụ lục E. Bảng kiểm trước phiên

&#x20;   \[ ] Đúng hợp đồng GC và đúng phiên
    \[ ] Nguồn dữ liệu và dấu thời gian hợp lệ
    \[ ] Lịch sự kiện
    \[ ] Vùng giá trị và POC phiên trước
    \[ ] Composite Vùng giá trị / POC
    \[ ] Initial Balance / phiên qua đêm / mốc tuần tháng
    \[ ] Bối cảnh nhiều phiên và trong ngày

    OPTIONS
    \[ ] Option underlying và expiry mapping đúng
    \[ ] Snapshot point-in-time hợp lệ
    \[ ] Quote quality đủ cho module đang dùng
    \[ ] OI availability date đúng, không look-ahead
    \[ ] ATM IV và term structure
    \[ ] Skew / convexity
    \[ ] Expected move theo horizon
    \[ ] Volume / OI / ΔOI concentration
    \[ ] Options Flow state
    \[ ] Exposure scenario và giả định
    \[ ] Options Regime
    \[ ] Data quality: Tốt / Hạn chế / Thận trọng / Không dùng

    \[ ] Kịch bản FAR / AAC / Luân phiên / Không giao dịch
    \[ ] Giới hạn rủi ro ngày và số giao dịch tối đa
    \[ ] Trạng thái sức khỏe và tâm lý


## Phụ lục F. Bảng kiểm tại mốc tham chiếu

&#x20;   MỐC THAM CHIẾU
    - Loại, vai trò, khung thời gian

    EPISODE
    - Lần tương tác đầu
    - Số lần thử
    - Độ lệch tối đa
    - Thời gian / Khối lượng / Số giao dịch ngoài vùng
    - Tốc độ tái nhập
    - POC cục bộ / Vùng giá trị

    ORDER FLOW
    - Tổng khối lượng / Số giao dịch
    - Ask / Bid / Không xác định
    - Delta / CVD
    - Imbalance / Cluster
    - Nỗ lực–Kết quả

    OPTIONS
    - Snapshot/horizon/data quality
    - ATM IV / term structure / skew
    - Expected move và vùng concentration gần nhất
    - Volume/OI/ΔOI / Flow state
    - Exposure scenario và giả định
    - Options Regime
    - Hỗ trợ / Xung đột / Trung tính / Không dùng

    QUYẾT ĐỊNH
    - FAR / AAC / Luân phiên / Chưa được giải quyết
    - Bằng chứng còn thiếu


## Phụ lục G. Bảng kiểm trước điểm vào

&#x20;   \[ ] Vị trí có ý nghĩa
    \[ ] Episode và hướng của lần thử đấu giá rõ
    \[ ] Họ luận điểm rõ
    \[ ] Phong cách vào rõ
    \[ ] Hành vi kỳ vọng viết được
    \[ ] Vô hiệu cấu trúc / Đấu giá / thời gian rõ
    \[ ] Dừng lỗ và khoảng đệm hợp lý
    \[ ] Hành lang mục tiêu và rào cản rõ
    \[ ] Tỷ lệ lợi nhuận/rủi ro thực thi đủ
    \[ ] Khối lượng nằm trong ngân sách rủi ro
    \[ ] Không có điều kiện bắt buộc không giao dịch
    \[ ] Không FOMO hoặc đuổi giá
    \[ ] Options không phải lý do duy nhất; horizon, dữ liệu và policy đều hợp lệ


## Phụ lục H. Mẫu luận điểm thị trường

&#x20;   Ngày/giờ:
    Sản phẩm:
    Trạng thái:
    Bối cảnh nhiều phiên:
    Bối cảnh trong ngày:
    Vị trí:
    Mốc / Vai trò:
    Episode / Lần thử:
    Bằng chứng chấp nhận:
    Nỗ lực dòng lệnh:
    Kết quả giá:

    OPTIONS
    - Snapshot time / ID:
    - Underlying / Expiry / Horizon:
    - Data quality:
    - ATM IV / Term structure / Skew:
    - Expected move:
    - Concentration gần nhất:
    - Options Flow state:
    - Exposure scenario / giả định:
    - Options Regime:
    - Trạng thái: Hỗ trợ / Xung đột / Trung tính / Không dùng

    Luận điểm: FAR / AAC / Luân phiên / Không giao dịch
    Hành vi kỳ vọng:
    Bằng chứng còn thiếu:
    Phong cách vào:
    Vùng vào:
    Vô hiệu:
    Dừng lỗ:
    Đường mục tiêu:
    Khối lượng:
    Thời hạn luận điểm:


## Phụ lục I. Mẫu nhật ký giao dịch

&#x20;   Mã giao dịch:
    Kịch bản giao dịch:
    Ảnh trước giao dịch:
    Options snapshot ID, model version và dấu thời gian:
    Luận điểm trước điểm vào:
    Điểm vào / Dừng lỗ / Mục tiêu / Khối lượng vị thế:
    Hành động trong giao dịch:
    Thoát lệnh:
    P\&L theo R:
    MFE / MAE:
    Điểm tuân thủ 0–5:
    Lỗi thực thi:
    Lỗi phương pháp:
    Điều học được:
    Đề xuất sửa quy tắc? CÓ / KHÔNG


## Phụ lục J. Mẫu nghiên cứu tình huống để giảng dạy

&#x20;   1. Ảnh trước khi giá đến mốc tham chiếu
    2. Ảnh lần tương tác đầu
    3. Ảnh khi giá thử đấu giá ngoài vùng
    4. Ảnh chấp nhận hoặc tái nhập
    5. Ảnh điều kiện kích hoạt
    6. Ảnh kết quả

    Tại mỗi ảnh, ghi:
    - Điều đã biết
    - Điều chưa biết
    - Kịch bản còn sống
    - Điều kiện vô hiệu
    - Hành động hợp lệ


## Phụ lục K. Thang đánh giá năng lực

|Điểm|Năng lực|
|-|-|
|0|Kể chuyện theo nến và kết quả đã biết|
|1|Xác định được vị trí và mốc tham chiếu|
|2|Xác định được trạng thái và Episode|
|3|Đọc được sự chấp nhận và quan hệ Nỗ lực–Kết quả|
|4|Phân biệt dữ liệu thô, dữ liệu tính và suy luận|
|5|Viết được bằng chứng còn thiếu, vô hiệu, mục tiêu, rủi ro và không giao dịch|

## Phụ lục L. Mười nguyên tắc bất biến

1. Vị trí trước tín hiệu.
2. Trạng thái trước hướng.
3. Episode trước phán quyết.
4. Sự chấp nhận là quá trình, không phải một cây nến.
5. Delta là áp lực chủ động, không phải vị thế.
6. Nỗ lực phải được so với Kết quả.
7. Options định giá sự bất định, không phải chánh án của giá.
8. FAR và AAC cần khả năng duy trì của giá.
9. Không có vô hiệu và không gian mục tiêu thì không có giao dịch.
10. “Chưa đủ bằng chứng” là một kết luận chuyên nghiệp.

## Phụ lục M. Ma trận năng lực công cụ dòng lệnh

|Công cụ|Dữ liệu cần|Quan sát chính|Quyền sử dụng|Giới hạn|
|-|-|-|-|-|
|Footprint Bid × Ask|Giao dịch Bid/Ask|Khối lượng theo mức giá|Kiểm tra nỗ lực và kết quả|Không thấy lệnh bị hủy|
|Tape|Dòng giao dịch đã khớp|Tốc độ, cụm và kích thước giao dịch|Quan sát xung lực và cách lệnh được chia nhỏ|Không biết danh tính|
|DOM|Độ sâu tổng hợp|Thanh khoản hiển thị|Điều kiện thực thi|Lệnh có thể bị rút|
|MBO DOM|Mã lệnh và hàng đợi|Vòng đời và ưu tiên lệnh|Nghiên cứu thanh khoản chi tiết|Phụ thuộc nguồn dữ liệu/cấu hình|
|Pulling/Stacking|Chuỗi cập nhật DOM|Rút/thêm thanh khoản|Bối cảnh độ bền sổ lệnh|Ảnh chụp đơn lẻ vô nghĩa|
|Nhận diện lệnh ẩn|Số giao dịch và dữ liệu MBO|Tái nạp và lượng khớp|Bằng chứng phụ cho hấp thụ|Lệnh ẩn vẫn có thể bị xuyên|
|Công cụ phát hiện lệnh ẩn bổ sung|Dữ liệu theo yêu cầu riêng|Mô hình phát hiện riêng|So sánh chéo|Phụ thuộc thuật toán|
|Nhận diện dừng lỗ|Số giao dịch và dữ liệu phân loại|Cụm kích hoạt dừng lỗ|Bằng chứng phụ về cơ chế thanh khoản|Không biết dừng lỗ dùng để vào hay thoát|
|Nhận diện lệnh quét|Dòng chủ động qua nhiều mức|Cú quét thanh khoản|Quan sát xung lực lấy thanh khoản|Không tự xác nhận FAR/AAC|
|Áp lực thị trường|Số giao dịch|Áp lực và nhịp độ gần đây|Xung lực ngắn hạn|Nhạy với tham số suy giảm|
|Áp lực sổ lệnh|Độ sâu|Áp lực thanh khoản hiển thị|Điều kiện sổ lệnh|Không phải giao dịch đã khớp|
|Sức mạnh thị trường|Tùy thuật toán|Chỉ số tổng hợp|Tham khảo sau khi kiểm tra công thức|Có thể là hộp đen|
|Mức động|Tùy thuật toán|Mốc thay đổi theo dữ liệu|Mốc tham chiếu ứng viên|Cần biết nguồn và khả năng vẽ lại|
|Cực trị thanh chưa hoàn tất\*|Footprint|Cực trị thanh chưa gọn|Mốc vi mô phụ|Không phải nam châm|

## Phụ lục N. Trạng thái kiểm chứng quy tắc

Mỗi quy tắc mới nên ghi rõ:

&#x20;   Tên quy tắc
    Nguồn
    Loại: Cơ chế / Mô hình / Kinh nghiệm
    Trạng thái: Đề xuất / Đang kiểm tra / Đã xác nhận / Bị loại
    Thị trường áp dụng
    Phiên hoặc chế độ tham gia
    Kích thước mẫu
    Đã tính chi phí hay chưa


Bốn tầng nguồn:

|Tầng|Loại nguồn|Quyền sử dụng|
|-|-|-|
|A|Cơ chế thị trường, tài liệu sàn và định nghĩa dữ liệu|Dùng làm nền|
|B|Mô hình và cách diễn giải của giảng viên|Dùng làm khung|
|C|Kinh nghiệm, mẫu hình và hình minh họa|Dùng để đặt giả thuyết\*|
|D|Nhận định thị trường theo thời điểm|Chỉ dùng làm ví dụ lịch sử\*|

## Phụ lục O. Bảng kiểm bối cảnh thị trường mở rộng

&#x20;   \[ ] Hợp đồng GC đúng
    \[ ] Chuyển tháng hợp đồng đã được kiểm tra
    \[ ] Mẫu phiên đúng
    \[ ] Nguồn dữ liệu kết nối ổn định
    \[ ] Phân loại Bid/Ask futures đủ tin cậy
    \[ ] MBO khả dụng nếu dùng công cụ MBO
    \[ ] Lịch tin và chế độ sự kiện đã xác định
    \[ ] Chế độ tham gia hiện tại đã xác định
    \[ ] Futures OI được gắn đúng nhịp cập nhật
    \[ ] COT chỉ dùng cho bối cảnh dài hơn

    OPTIONS
    \[ ] Option underlying mapping đúng
    \[ ] Expiry date/time và DTE đúng
    \[ ] Snapshot point-in-time hợp lệ
    \[ ] Quote quality policy đã qua
    \[ ] OI availability date đúng, không look-ahead
    \[ ] IV/Greeks model version đã ghi
    \[ ] Term structure và skew cùng quy ước
    \[ ] Expected move có phương pháp và horizon
    \[ ] Complex flow/spread được xử lý phù hợp
    \[ ] Exposure scenario nêu rõ giả định dấu
    \[ ] Options Regime và data quality đã ghi

    EXECUTION
    \[ ] Basis GC–CFD đã đo
    \[ ] Chênh lệch mua bán CFD bình thường
    \[ ] Người giao dịch đủ tỉnh táo và chưa chạm giới hạn rủi ro


# KẾT LUẬN

Phương pháp Tam Trụ có thể được cô đọng thành chuỗi câu hỏi:

&#x20;   TA ĐANG Ở ĐÂU?
    → bản đồ đấu giá, vùng giá trị, POC, mốc tham chiếu

    THỊ TRƯỜNG ĐANG Ở TRẠNG THÁI NÀO?
    → Cân bằng, Khám phá giá, Chuyển tiếp

    NÓ ĐANG THỬ LÀM GÌ?
    → Episode đấu giá

    MỨC GIÁ MỚI CÓ ĐƯỢC CHẤP NHẬN KHÔNG?
    → thời gian + khối lượng + số giao dịch + POC/vùng giá trị + sự duy trì

    BÊN NÀO ĐANG ÉP VÀ CÓ HIỆU QUẢ KHÔNG?
    → Order Flow + Nỗ lực–Kết quả + khả năng tạo thuận lợi cho giao dịch

    THỊ TRƯỜNG OPTIONS ĐANG ĐỊNH GIÁ ĐIỀU GÌ?
    → IV + term structure + skew + expected move + volume/OI/flow + exposure scenarios

    LUẬN ĐIỂM NÀO HỢP LỆ?
    → FAR / AAC / Luân phiên / Không giao dịch

    THAM GIA BẰNG CÁCH NÀO?
    → Tái nhập / kiểm tra lại / xác nhận / chờ

    SAI Ở ĐÂU?
    → giá + đấu giá + thời gian + vô hiệu theo bối cảnh

    ĐI ĐẾN ĐÂU?
    → Đường đi ít cản trở + Rào cản trung gian + Hành lang mục tiêu

    RỦI RO BAO NHIÊU?
    → khối lượng vị thế + Chi phí + Rủi ro ngày


Một người giao dịch trưởng thành không cố nhìn thấy tương lai. Họ xây bản đồ đấu giá, đọc giá của sự bất định trên Options, theo dõi cuộc thử giá, đo nỗ lực và kết quả, rồi hành động chỉ khi bằng chứng, horizon, hình học và rủi ro cùng cho phép.

> Bí quyết không nằm ở việc biết nhiều thuật ngữ hơn. Nó nằm ở việc hiểu mỗi công cụ đo cái gì, dùng để làm gì, không đo được gì, và không đặt vốn vào nơi luận điểm chưa có hình dạng.

