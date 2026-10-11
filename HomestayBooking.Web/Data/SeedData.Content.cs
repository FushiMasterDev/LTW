namespace HomestayBooking.Web.Data;

/// <summary>Nội dung mô tả mẫu: mỗi homestay một đoạn giới thiệu riêng, loại phòng có mô tả và diện tích.</summary>
public static partial class SeedData
{
    /// <summary>Giới thiệu homestay, cùng thứ tự với HomestayData.</summary>
    private static readonly string[] HomestayDescriptions =
    [
        // Hoàn Kiếm
        "Căn nhà ống ba tầng ngay góc phố Hàng Bạc, bước chân ra là chợ đêm cuối tuần và phố đi bộ Hồ Gươm. Phòng cách âm tốt, có cửa sổ nhìn xuống phố cổ, rất hợp với khách lần đầu đến Hà Nội.",
        "Ngôi nhà nhỏ trong ngõ yên tĩnh phố Lò Sũ, đi bộ 3 phút ra hồ Hoàn Kiếm. Chủ nhà là người Hà Nội gốc, sẵn lòng chỉ quán bún chả, phở gánh ngon quanh khu phố.",
        "Nằm giữa phố Tạ Hiện sôi động, phù hợp với nhóm bạn trẻ thích khám phá phố bia và ẩm thực đêm. Phòng ngủ đặt ở tầng trên, lắp kính hai lớp để vẫn ngủ ngon khi phố còn đông.",
        "Nhà phố được giữ lại mặt tiền kiểu Pháp cũ, nội thất gỗ và gạch bông truyền thống. Phố Hàng Gai nổi tiếng lụa tơ tằm, cách Nhà thờ Lớn chỉ 5 phút đi bộ.",
        "Căn gác xép thiết kế mở với trần cao, nhiều ánh sáng tự nhiên, nằm trên phố Mã Mây. Có góc làm việc riêng và wifi mạnh, phù hợp khách công tác kết hợp du lịch.",
        // Ba Đình
        "Biệt thự nhỏ phủ cây xanh trên phố Đội Cấn, gần Lăng Bác và Hoàng thành Thăng Long. Không gian rộng, sân trước để được xe máy, thích hợp gia đình có người lớn tuổi.",
        "Căn hộ tầng cao gần Lotte Center, nhìn thẳng ra đường chân trời Hà Nội về đêm. Tòa nhà có bảo vệ 24/7 và thang máy, tiện cho khách đi công tác.",
        "Ngôi nhà sát hồ Trúc Bạch, sáng sớm có thể chạy bộ quanh hồ hoặc đạp vịt cùng trẻ nhỏ. Gần phố ẩm thực Ngũ Xã với món phở cuốn nổi tiếng.",
        "Homestay ấm cúng trên đường Kim Mã, gần bến xe buýt và các tuyến tàu điện. Giá hợp lý, phòng gọn gàng, phù hợp sinh viên và khách đi một mình.",
        "Nhà vườn trong làng hoa Ngọc Hà xưa, có khoảng sân nhỏ trồng cây và bàn trà ngoài trời. Không khí yên bình hiếm có giữa trung tâm thành phố.",
        // Tây Hồ
        "Nằm trên phố Từ Hoa, cách mép Hồ Tây vài bước chân, nổi tiếng với cảnh hoàng hôn từ ban công tầng ba. Xung quanh có nhiều quán cà phê ven hồ và đường đạp xe.",
        "Ngôi nhà mang phong cách tối giản Nhật Bản ở khu Quảng An, gần đầm sen Quảng Bá. Mùa hè có thể dậy sớm ngắm sen nở và chụp ảnh áo dài.",
        "Studio hiện đại trên phố Xuân Diệu, khu vực đông khách nước ngoài với nhiều nhà hàng quốc tế. Phù hợp cặp đôi muốn kỳ nghỉ ngắn thư thả bên Hồ Tây.",
        "Căn nhà nép trong ngõ phố Nhật Chiêu, tách biệt khỏi tiếng ồn, có sân thượng trồng cây. Chỉ 5 phút đi bộ đến phủ Tây Hồ và đường ven hồ.",
        "Ngay mặt đường Yên Phụ, nhìn ra sông Hồng và cầu Long Biên. Thuận tiện di chuyển cả về phố cổ lẫn khu Hồ Tây, có chỗ để ô tô.",
        // Đống Đa
        "Nhà phố đối diện Văn Miếu – Quốc Tử Giám, trang trí bằng thư pháp và đồ gỗ truyền thống. Lựa chọn quen thuộc của phụ huynh đưa con đi thi hoặc khách yêu văn hóa.",
        "Căn hộ nhìn ra hồ Hoàng Cầu, buổi tối có đường dạo bộ và khu ẩm thực quanh hồ. Gần các trung tâm thương mại và bệnh viện lớn.",
        "Homestay trên phố Thái Hà sầm uất, gần nhiều trường đại học và nhà hát. Phòng mới sửa, có bếp chung đầy đủ dụng cụ để tự nấu ăn.",
        "Ngôi nhà mang phong cách thập niên 80 với đồ cổ, radio và tranh cổ động, trên phố Khâm Thiên. Trải nghiệm hoài niệm lý thú cho khách thích chụp ảnh.",
        "Nhà rộng trên phố Láng Hạ, nhiều phòng ngủ thông nhau, phù hợp đại gia đình hoặc nhóm đông người. Gần công viên Thủ Lệ và Bảo tàng Dân tộc học.",
        // Long Biên
        "Ngôi nhà ở Ngọc Thụy, ngay chân cầu Long Biên, có thể đi bộ lên cầu ngắm bình minh trên sông Hồng. Bãi giữa sông có vườn hoa và đường đạp xe cuối tuần.",
        "Nhà có vườn rộng ở phường Bồ Đề, không khí thoáng đãng, cách phố cổ chỉ 10 phút lái xe qua cầu. Trẻ em có chỗ chạy nhảy thoải mái.",
        "Căn biệt thự trong khu đô thị Vinhomes Riverside với hồ nước, cây xanh và an ninh tốt. Thích hợp gia đình muốn nghỉ dưỡng yên tĩnh mà vẫn gần trung tâm.",
        "Góc nhỏ yên bình ở Gia Thụy, gần công viên và chợ dân sinh. Giá mềm, chủ nhà thân thiện, phù hợp khách ở dài ngày.",
        "Biệt thự gia đình ở Sài Đồng, có sân nướng ngoài trời và phòng khách lớn. Gần Aeon Mall Long Biên để mua sắm, vui chơi cùng trẻ nhỏ.",
        // Sóc Sơn
        "Nằm giữa đồi thông gần đền Gióng, không khí mát lành quanh năm. Buổi tối có thể đốt lửa trại và nướng BBQ cùng bạn bè, chỉ cách sân bay Nội Bài 20 phút.",
        "Khu cắm trại ven hồ Hàm Lợn với nhà gỗ và lều glamping. Hoạt động chèo SUP, câu cá và ngắm hoàng hôn trên mặt hồ là điểm nhấn.",
        "Biệt thự ẩn trong rừng ven hồ Đồng Đò, có bể bơi và khu BBQ riêng. Lựa chọn lý tưởng cho nhóm công ty tổ chức team building cuối tuần.",
        "Khu vườn rộng trồng cây ăn quả ở xã Minh Phú, khách được tự tay hái rau và nấu bữa quê. Nhà thoáng mát, nhiều góc chụp ảnh đồng quê.",
        "Ngôi nhà trên đỉnh đồi ở Bắc Sơn, nhìn toàn cảnh thung lũng và hồ nước phía xa. Đêm trong trời có thể ngắm sao, sáng sớm thường có sương mờ.",
        // Ba Vì
        "Nhà gỗ dưới chân núi Ba Vì, gần Vườn quốc gia và đền Thượng. Thích hợp khách thích leo núi, đi bộ đường rừng và tận hưởng không khí trong lành.",
        "Biệt thự ở Tản Lĩnh, nhiều buổi sáng có biển mây bao quanh. Có bể bơi nước ấm và phòng xông hơi lá thuốc của người Dao.",
        "Nhà tre nằm cạnh suối Ngọc, mùa hè có thể tắm suối mát lạnh. Thiết kế mở, gần gũi thiên nhiên, có võng và chòi nghỉ bên dòng nước.",
        "Biệt thự xây bằng đá ong và đá núi ở Minh Quang, mát vào mùa hè và ấm vào mùa đông. Có sân vườn rộng, phù hợp nhóm đông tổ chức tiệc.",
        "Khu lưu trú sinh thái gần thác Khoang Xanh, chủ trương hạn chế đồ nhựa và dùng thực phẩm địa phương. Hợp với gia đình muốn trẻ nhỏ trải nghiệm thiên nhiên.",
    ];

    /// <summary>Mô tả và diện tích (m²) loại phòng theo hạng phòng; nội thành và ngoại thành khác nhau, diện tích xê dịch theo homestay.</summary>
    private static (string Description, int AreaM2) RoomTypeDetails(string category, bool isSuburb, int index)
    {
        var (description, area) = (category, isSuburb) switch
        {
            ("Phòng đơn", _) => ("Giường đơn 1m2, bàn làm việc nhỏ, phòng tắm riêng. Phù hợp khách đi một mình.", 14),
            ("Phòng đôi", false) => ("Giường đôi 1m6, điều hòa, tủ quần áo và phòng tắm riêng có vòi sen nóng lạnh.", 20),
            ("Phòng đôi", true) => ("Giường đôi 1m6 hướng ra vườn, có hiên nhỏ ngồi uống trà, phòng tắm riêng.", 24),
            ("Phòng gia đình", false) => ("Một giường đôi và hai giường đơn (hoặc hai giường đôi), sofa, phù hợp gia đình có trẻ nhỏ.", 32),
            ("Phòng gia đình", true) => ("Hai giường đôi, khu sinh hoạt chung và ban công nhìn ra vườn, rộng rãi cho gia đình 4 người.", 38),
            ("Nguyên căn", _) => ("Thuê trọn căn: 3–4 phòng ngủ, phòng khách, bếp đầy đủ dụng cụ và sân vườn riêng cho cả nhóm.", 120),
            _ => ("Phòng tiêu chuẩn, đầy đủ tiện nghi cơ bản.", 18),
        };
        return (description, area + index % 3 * 2);
    }
}
