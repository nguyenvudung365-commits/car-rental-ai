namespace CarRental.Domain.Common;

/// <summary>Quyền người dùng — map vào JWT claim "role".</summary>
public enum UserRole
{
    Customer = 1,
    Staff = 2,
    Admin = 3
}

/// <summary>Trạng thái xe (API trả về dạng chuỗi snake_case: san_sang, dang_thue, bao_tri, ngung_hoat_dong).</summary>
public enum CarStatus
{
    SanSang = 1,
    DangThue = 2,
    BaoTri = 3,
    NgungHoatDong = 4,
    /// <summary>Xe do Staff tạo, chờ Admin xác nhận giá và duyệt; không hiện công khai, không đặt được.</summary>
    ChoDuyet = 5
}

/// <summary>Loại xe — dùng để AI dự đoán giá theo phân khúc.</summary>
public enum CarType
{
    Sedan = 1,
    SUV = 2,
    Hatchback = 3,
    MPV = 4,
    Pickup = 5,
    Luxury = 6
}

/// <summary>Hộp số.</summary>
public enum Transmission
{
    SoSan = 1,
    SoTuDong = 2
}

/// <summary>Loại nhiên liệu.</summary>
public enum FuelType
{
    Xang = 1,
    Dau = 2,
    Dien = 3,
    Hybrid = 4
}

/// <summary>Trạng thái booking.</summary>
public enum BookingStatus
{
    ChoXacNhan = 1,
    DaXacNhan = 2,
    DangThue = 3,
    DaTraXe = 4,
    DaHuy = 5
}

/// <summary>Trạng thái thanh toán.</summary>
public enum PaymentStatus
{
    ChuaThanhToan = 1,
    DaCoc = 2,
    DaThanhToan = 3,
    DaHoanTien = 4
}

/// <summary>Phương thức thanh toán.</summary>
public enum PaymentMethod
{
    TienMat = 1,
    ChuyenKhoan = 2,
    TheNoiDia = 3,
    ViDienTu = 4,
    /// <summary>Khoản trừ từ tiền cọc bảo đảm. Dùng 6 (không dùng 5) để tránh trùng giá trị Năng có thể đã dùng cho thanh toán online.</summary>
    TruCoc = 6
}

/// <summary>Loại biên bản bàn giao xe.</summary>
public enum HandoverType
{
    Giao = 1,
    Nhan = 2
}

/// <summary>Loại giao dịch tiền cọc bảo đảm (không phải doanh thu).</summary>
public enum DepositTxType
{
    Thu = 1,
    KhauTru = 2,
    Hoan = 3
}

/// <summary>Trạng thái yêu cầu gia hạn thuê.</summary>
public enum ExtensionStatus
{
    ChoDuyet = 1,
    DaDuyet = 2,
    TuChoi = 3
}

/// <summary>Mức độ trong danh sách chặn khách.</summary>
public enum BlacklistLevel
{
    CanhBao = 1,
    Chan = 2
}

/// <summary>Loại giấy tờ khách nộp để xác minh.</summary>
public enum DocumentType
{
    Cccd = 1,
    BangLai = 2
}

/// <summary>Trạng thái duyệt một giấy tờ.</summary>
public enum DocumentStatus
{
    ChoDuyet = 1,
    DaDuyet = 2,
    TuChoi = 3
}

/// <summary>Tình trạng giấy tờ tổng hợp của khách: DaDuyet khi cả CCCD và bằng lái đã duyệt.</summary>
public enum DocumentsOverallStatus
{
    ChuaNop = 1,
    ChoDuyet = 2,
    TuChoi = 3,
    DaDuyet = 4
}

/// <summary>Hạng thành viên theo tổng điểm tích lũy trọn đời.</summary>
public enum MemberTier
{
    ThanhVien = 1,
    Bac = 2,
    Vang = 3,
    KimCuong = 4
}

/// <summary>Lý do thay đổi điểm thưởng.</summary>
public enum LoyaltyReason
{
    TichDiem = 1,
    DieuChinh = 2
}


/// <summary>Chế độ thuê: theo ngày (mặc định) hoặc theo giờ (4 đến 12 giờ).</summary>
public enum RentalMode
{
    TheoNgay = 1,
    TheoGio = 2
}

public enum CouponType
{
    PhanTram = 1,
    SoTien = 2
}

public enum SurchargeType
{
    CoDinh = 1,
    TheoNgay = 2,
    PhanTram = 3
}

/// <summary>Loại dòng phí của đơn. GiamGia mang số âm.</summary>
public enum ChargeKind
{
    PhuPhi = 1,
    GiamGia = 2,
    TaiXe = 3,
    GiaoNhan = 4,
    MotChieu = 5
}

/// <summary>Loại lịch bảo trì của xe.</summary>
public enum MaintenanceType
{
    BaoDuong = 1,
    DangKiem = 2,
    BaoHiem = 3
}

public enum MaintenanceStatus
{
    KeHoach = 1,
    DangThucHien = 2,
    HoanThanh = 3,
    Huy = 4
}
