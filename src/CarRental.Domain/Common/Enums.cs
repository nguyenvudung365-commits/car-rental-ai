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
    NgungHoatDong = 4
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
    ViDienTu = 4
}
