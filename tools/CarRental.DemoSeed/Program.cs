// Nạp dữ liệu mẫu (DEMO) cho Car Rental. Chạy tay, idempotent. KHÔNG dùng cho production.
// Dùng: dotnet run --project tools/CarRental.DemoSeed -- "<connection string>"
//   hoặc đặt biến môi trường ConnectionStrings__Default.
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Bookings;
using CarRental.Domain.Modules.Cars;
using CarRental.Domain.Modules.Customers;
using CarRental.Infrastructure.Persistence;
using CarRental.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

const string DemoPassword = "Demo@12345";

var conn = args.FirstOrDefault() ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default");
if (string.IsNullOrWhiteSpace(conn))
{
    Console.Error.WriteLine("Thiếu connection string. Truyền làm tham số đầu tiên hoặc đặt ConnectionStrings__Default.");
    return 1;
}

var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(conn).Options;
using var db = new ApplicationDbContext(options);
db.Database.Migrate();

var hasher = new BCryptPasswordHasher(); // cùng thuật toán với AuthService (BCrypt, work factor 12)
var today = DateTime.UtcNow.Date;
int addedUsers = 0, addedCars = 0, addedBookings = 0;

// ---------- Tài khoản ----------
Customer EnsureUser(string name, string email, string phone, UserRole role, string? cccd, string? addr)
{
    var u = db.Customers.FirstOrDefault(c => c.Email == email || c.PhoneNumber == phone);
    if (u != null) return u;
    u = new Customer
    {
        FullName = name, Email = email, PhoneNumber = phone, Role = role,
        PasswordHash = hasher.Hash(DemoPassword), IdentityNumber = cccd, Address = addr
    };
    db.Customers.Add(u);
    db.SaveChanges();
    addedUsers++;
    return u;
}

EnsureUser("Quản trị viên Demo", "admin@demo.local", "0900000001", UserRole.Admin, null, null);
var staff = EnsureUser("Nhân viên Demo", "staff@demo.local", "0900000011", UserRole.Staff, null, null);
var kh1 = EnsureUser("Nguyễn Văn An", "an.nguyen@demo.local", "0912000001", UserRole.Customer, "079200000001", "12 Lê Lợi, Quận 1, TP.HCM");
var kh2 = EnsureUser("Trần Thị Bình", "binh.tran@demo.local", "0912000002", UserRole.Customer, "001200000002", "25 Hai Bà Trưng, Hoàn Kiếm, Hà Nội");
var kh3 = EnsureUser("Lê Hoàng Cường", "cuong.le@demo.local", "0912000003", UserRole.Customer, "048200000003", "88 Bạch Đằng, Hải Châu, Đà Nẵng");

// ---------- Xe ----------
Car EnsureCar(CarType type, string brand, string model, string plate, int seats, int age, decimal price, CarStatus status, string desc)
{
    var c = db.Cars.FirstOrDefault(x => x.LicensePlate == plate);
    if (c != null) return c;
    c = new Car
    {
        CarType = type, Brand = brand, Model = model, LicensePlate = plate, Seats = seats,
        CarAgeYears = age, BasePricePerDay = price, Status = status, Description = desc
    };
    db.Cars.Add(c);
    db.SaveChanges();
    addedCars++;
    return c;
}

var vios     = EnsureCar(CarType.Sedan,     "Toyota",     "Vios",      "51A-100.01", 5, 2, 500_000,   CarStatus.SanSang, "Sedan phổ thông, tiết kiệm nhiên liệu.");
EnsureCar(CarType.Sedan,                    "Hyundai",    "Accent",    "51A-100.02", 5, 3, 480_000,   CarStatus.SanSang, "Sedan gọn gàng, phù hợp đi phố.");
var mazda3   = EnsureCar(CarType.Sedan,     "Mazda",      "Mazda3",    "30A-100.03", 5, 1, 700_000,   CarStatus.SanSang, "Sedan thiết kế đẹp, lái thú vị.");
var fortuner = EnsureCar(CarType.SUV,       "Toyota",     "Fortuner",  "51K-100.04", 7, 2, 1_100_000, CarStatus.SanSang, "SUV 7 chỗ máy dầu, đi đường dài.");
var crv      = EnsureCar(CarType.SUV,       "Honda",      "CR-V",      "30G-100.05", 7, 2, 950_000,   CarStatus.SanSang, "SUV rộng rãi, vận hành êm.");
EnsureCar(CarType.SUV,                      "Kia",        "Seltos",    "43A-100.06", 5, 1, 750_000,   CarStatus.SanSang, "SUV cỡ nhỏ, nhiều tiện nghi.");
EnsureCar(CarType.Hatchback,                "Hyundai",    "Grand i10", "51A-100.07", 5, 3, 400_000,   CarStatus.SanSang, "Hatchback nhỏ gọn, giá rẻ.");
EnsureCar(CarType.Hatchback,                "Kia",        "Morning",   "29A-100.08", 5, 4, 400_000,   CarStatus.BaoTri,  "Hatchback đô thị (đang bảo trì).");
var xpander  = EnsureCar(CarType.MPV,       "Mitsubishi", "Xpander",   "51A-100.09", 7, 1, 650_000,   CarStatus.SanSang, "MPV 7 chỗ cho gia đình.");
EnsureCar(CarType.MPV,                      "Toyota",     "Innova",    "51A-100.10", 7, 4, 600_000,   CarStatus.BaoTri,  "MPV 7 chỗ bền bỉ (đang bảo trì).");
var ranger   = EnsureCar(CarType.Pickup,    "Ford",       "Ranger",    "51C-100.11", 5, 2, 900_000,   CarStatus.SanSang, "Bán tải mạnh mẽ, chở hàng và đi off-road.");
var merc     = EnsureCar(CarType.Luxury,    "Mercedes",   "C300 AMG",  "30K-100.12", 5, 1, 1_500_000, CarStatus.SanSang, "Sedan hạng sang, dịp quan trọng.");

// ---------- Booking (khoá idempotent: CorrelationId) ----------
// Mỗi xe chỉ có các khoảng ngày không giao nhau.
Booking? EnsureBooking(string key, Customer cust, Car car, int startOff, int endOff, BookingStatus status, string? note)
{
    if (db.Bookings.Any(b => b.CorrelationId == key)) return null;
    var start = today.AddDays(startOff);
    var end = today.AddDays(endOff);
    var price = Math.Clamp(car.OverridePrice ?? car.BasePricePerDay, 400_000m, 1_500_000m);
    var b = new Booking
    {
        CustomerId = cust.Id, CarId = car.Id, StartDate = start, EndDate = end,
        PredictedPricePerDay = null, FinalPricePerDay = price, IsFallback = true,
        TotalPrice = price * (end - start).Days, Status = status, CorrelationId = key, Note = note
    };
    db.Bookings.Add(b);
    db.SaveChanges();
    addedBookings++;
    return b;
}

void AddPayment(Booking b, decimal amount, PaymentMethod m, PaymentStatus s, string code, int paidDaysAgo)
{
    db.Payments.Add(new Payment
    {
        BookingId = b.Id, Amount = amount, Method = m, Status = s, TransactionCode = code,
        PaidAt = s is PaymentStatus.DaCoc or PaymentStatus.DaThanhToan ? DateTime.UtcNow.AddDays(-paidDaysAgo) : null
    });
    db.SaveChanges();
}

void AddReturn(Booking b, DateTime returnedAt, decimal damage, string? damageNote)
{
    var (lateDays, lateFee, final) = CarRental.Application.Modules.Bookings.RentalService.CalculateReturn(
        b.EndDate, returnedAt, b.FinalPricePerDay, b.TotalPrice, damage);
    db.ReturnRecords.Add(new ReturnRecord
    {
        BookingId = b.Id, ReturnedAt = returnedAt, LateDays = lateDays, LateFee = lateFee,
        DamageFee = damage, DamageNote = damageNote, FinalAmount = final, StaffId = staff.Id
    });
    db.SaveChanges();
}

// 1) Chờ xác nhận (chưa có thanh toán)
EnsureBooking("demo-seed-01", kh1, vios, 5, 8, BookingStatus.ChoXacNhan, "Đặt đi Vũng Tàu cuối tuần.");

// 2) Đã xác nhận + đã cọc 30%
var b2 = EnsureBooking("demo-seed-02", kh2, fortuner, 2, 5, BookingStatus.DaXacNhan, "Cần xe 7 chỗ đi Đà Lạt.");
if (b2 != null) AddPayment(b2, b2.TotalPrice * 0.3m, PaymentMethod.ChuyenKhoan, PaymentStatus.DaCoc, "DEMO-DEP-002", 1);

// 3) Đã xác nhận, có khoản cọc chờ ghi nhận (demo mark-paid)
var b3 = EnsureBooking("demo-seed-03", kh3, mazda3, 3, 6, BookingStatus.DaXacNhan, null);
if (b3 != null) AddPayment(b3, b3.TotalPrice * 0.3m, PaymentMethod.ViDienTu, PaymentStatus.ChuaThanhToan, "DEMO-PEND-003", 0);

// 4) Đang thuê (xe chuyển DangThue), đã thanh toán đủ
var b4 = EnsureBooking("demo-seed-04", kh1, xpander, -2, 2, BookingStatus.DangThue, "Gia đình đi du lịch.");
if (b4 != null)
{
    db.Cars.First(x => x.Id == b4.CarId).Status = CarStatus.DangThue;
    db.SaveChanges();
    AddPayment(b4, b4.TotalPrice, PaymentMethod.TheNoiDia, PaymentStatus.DaThanhToan, "DEMO-PAY-004", 3);
}

// 5) Đã trả xe trễ 1 ngày + phí hư hỏng nhẹ
var b5 = EnsureBooking("demo-seed-05", kh2, crv, -10, -7, BookingStatus.DaTraXe, "Công tác Hải Phòng.");
if (b5 != null)
{
    AddReturn(b5, today.AddDays(-6), 200_000m, "Trầy xước nhẹ cản sau.");
    var rr = db.ReturnRecords.First(r => r.BookingId == b5.Id);
    AddPayment(b5, b5.TotalPrice, PaymentMethod.ChuyenKhoan, PaymentStatus.DaThanhToan, "DEMO-PAY-005A", 10);
    AddPayment(b5, rr.LateFee + rr.DamageFee, PaymentMethod.TienMat, PaymentStatus.DaThanhToan, "DEMO-PAY-005B", 6);
}

// 6) Đã trả xe đúng hạn, thanh toán đủ
var b6 = EnsureBooking("demo-seed-06", kh3, ranger, -20, -17, BookingStatus.DaTraXe, null);
if (b6 != null)
{
    AddReturn(b6, b6.EndDate, 0, null);
    AddPayment(b6, b6.TotalPrice, PaymentMethod.TienMat, PaymentStatus.DaThanhToan, "DEMO-PAY-006", 17);
}

// 7) Đã hủy (không thanh toán)
EnsureBooking("demo-seed-07", kh1, merc, 10, 12, BookingStatus.DaHuy, "Khách đổi lịch.");

// 8) Đã trả xe đúng hạn, cùng xe Vios với booking 1 nhưng ở quá khứ (không trùng lịch)
var b8 = EnsureBooking("demo-seed-08", kh2, vios, -15, -13, BookingStatus.DaTraXe, null);
if (b8 != null)
{
    AddReturn(b8, b8.EndDate, 0, null);
    AddPayment(b8, b8.TotalPrice, PaymentMethod.ViDienTu, PaymentStatus.DaThanhToan, "DEMO-PAY-008", 13);
}

// ---------- Dữ liệu số lượng lớn (idempotent: bỏ qua nếu đã có booking demo-bulk-*) ----------
// Mỗi xe chỉ có booking không giao nhau; xe mới tạo riêng nên không đụng 8 booking ở trên.
if (!db.Bookings.Any(b => b.CorrelationId!.StartsWith("demo-bulk-")))
{
    var rnd = new Random(42); // cố định để chạy lại ra cùng dữ liệu
    var passHash = hasher.Hash(DemoPassword); // hash 1 lần dùng chung (BCrypt chậm)
    string[] ho = { "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng", "Bùi", "Đỗ", "Hồ", "Ngô", "Dương" };
    string[] dem = { "Văn", "Thị", "Minh", "Quốc", "Thanh", "Hoàng", "Ngọc", "Gia", "Anh", "Đức" };
    string[] ten = { "An", "Bình", "Châu", "Dũng", "Hà", "Hùng", "Khánh", "Lan", "Linh", "Long", "Mai", "Nam", "Phúc", "Quân", "Sơn", "Tâm", "Thảo", "Trang", "Tuấn", "Việt" };
    string[] tinh = { "Quận 1, TP.HCM", "Quận 7, TP.HCM", "Cầu Giấy, Hà Nội", "Hải Châu, Đà Nẵng", "Ninh Kiều, Cần Thơ", "Nha Trang, Khánh Hòa", "Đà Lạt, Lâm Đồng" };

    var users = new List<Customer>();
    for (var i = 1; i <= 60; i++)
        users.Add(new Customer
        {
            FullName = $"{ho[rnd.Next(ho.Length)]} {dem[rnd.Next(dem.Length)]} {ten[rnd.Next(ten.Length)]}",
            Email = $"bulk{i:000}@demo.local", PhoneNumber = $"0913{100000 + i}", Role = UserRole.Customer,
            PasswordHash = passHash, IdentityNumber = $"079{300000000 + i}", Address = $"{rnd.Next(1, 200)} Đường số {rnd.Next(1, 50)}, {tinh[rnd.Next(tinh.Length)]}"
        });
    users = users.Where(u => !db.Customers.Any(c => c.Email == u.Email || c.PhoneNumber == u.PhoneNumber)).ToList();
    db.Customers.AddRange(users);
    addedUsers += users.Count;

    (CarType T, string Brand, string Model, int Seats, int Price)[] catalog =
    {
        (CarType.Sedan, "Toyota", "Camry", 5, 900_000), (CarType.Sedan, "Honda", "City", 5, 550_000), (CarType.Sedan, "Kia", "K3", 5, 600_000),
        (CarType.SUV, "Hyundai", "Santa Fe", 7, 1_000_000), (CarType.SUV, "Mazda", "CX-5", 5, 850_000), (CarType.SUV, "VinFast", "VF e34", 5, 800_000),
        (CarType.Hatchback, "VinFast", "Fadil", 5, 420_000), (CarType.Hatchback, "Toyota", "Wigo", 5, 400_000),
        (CarType.MPV, "Suzuki", "Ertiga", 7, 600_000), (CarType.MPV, "Kia", "Carnival", 7, 1_300_000),
        (CarType.Pickup, "Mitsubishi", "Triton", 5, 850_000), (CarType.Luxury, "BMW", "520i", 5, 1_450_000), (CarType.Luxury, "Lexus", "RX350", 5, 1_500_000),
    };
    var cars = new List<Car>();
    for (var i = 1; i <= 45; i++)
    {
        var m = catalog[(i - 1) % catalog.Length];
        var price = Math.Clamp(m.Price + rnd.Next(-3, 4) * 10_000, 400_000, 1_500_000);
        var status = i % 15 == 0 ? CarStatus.BaoTri : i % 22 == 0 ? CarStatus.NgungHoatDong : CarStatus.SanSang;
        cars.Add(new Car
        {
            CarType = m.T, Brand = m.Brand, Model = m.Model, LicensePlate = $"{(i % 2 == 0 ? "51H" : "30L")}-{200 + i / 100}.{i % 100:00}",
            Seats = m.Seats, CarAgeYears = rnd.Next(0, 6), BasePricePerDay = price, Status = status, Description = $"{m.Brand} {m.Model} - xe demo số lượng lớn."
        });
    }
    cars = cars.Where(c => !db.Cars.Any(x => x.LicensePlate == c.LicensePlate)).ToList();
    db.Cars.AddRange(cars);
    addedCars += cars.Count;
    db.SaveChanges(); // cần Id trước khi tạo booking
    // Nạp lại từ DB để chạy lại sau lần lỗi giữa chừng (user/xe đã có nhưng chưa có booking) vẫn đủ dữ liệu.
    users = db.Customers.Where(c => c.Email.StartsWith("bulk") && c.Email.EndsWith("@demo.local")).ToList();
    cars = db.Cars.Where(c => c.Description != null && c.Description.EndsWith("xe demo số lượng lớn.")).ToList();

    var seq = 0;
    foreach (var car in cars.Where(c => c.Status == CarStatus.SanSang))
    {
        var day = -120 + rnd.Next(0, 10); // dựng lịch từ quá khứ tới tương lai, mỗi xe một dòng thời gian
        while (day < 60)
        {
            var len = rnd.Next(1, 8);
            var start = today.AddDays(day);
            var end = start.AddDays(len);
            day += len + rnd.Next(1, 9);
            var cust = users.Count > 0 ? users[rnd.Next(users.Count)] : kh1;
            var fb = rnd.Next(100) < 25;
            var predicted = fb ? (decimal?)null : Math.Round(car.BasePricePerDay * (decimal)(0.9 + rnd.NextDouble() * 0.3) / 1000m) * 1000m;
            var final = Math.Clamp(predicted ?? car.BasePricePerDay, 400_000m, 1_500_000m);

            BookingStatus st;
            if (end <= today) st = rnd.Next(100) < 12 ? BookingStatus.DaHuy : BookingStatus.DaTraXe;
            else if (start <= today) st = BookingStatus.DangThue;
            else st = rnd.Next(100) switch { < 15 => BookingStatus.DaHuy, < 55 => BookingStatus.ChoXacNhan, _ => BookingStatus.DaXacNhan };

            var b = new Booking
            {
                CustomerId = cust.Id, CarId = car.Id, StartDate = start, EndDate = end, PredictedPricePerDay = predicted,
                FinalPricePerDay = final, TotalPrice = final * len, IsFallback = fb, Status = st,
                CorrelationId = $"demo-bulk-{++seq:0000}", Note = rnd.Next(3) == 0 ? "Đi công tác / du lịch." : null,
                CreatedAt = start.AddDays(-rnd.Next(1, 10))
            };
            if (st == BookingStatus.DangThue) car.Status = CarStatus.DangThue;
            if (st == BookingStatus.DaXacNhan)
                b.Payments.Add(new Payment { Amount = b.TotalPrice * 0.3m, Method = PaymentMethod.ChuyenKhoan, Status = PaymentStatus.DaCoc, TransactionCode = $"BULK-DEP-{seq:0000}", PaidAt = DateTime.UtcNow.AddDays(-rnd.Next(0, 3)) });
            if (st is BookingStatus.DangThue or BookingStatus.DaTraXe)
                b.Payments.Add(new Payment { Amount = b.TotalPrice, Method = (PaymentMethod)rnd.Next(1, 5), Status = PaymentStatus.DaThanhToan, TransactionCode = $"BULK-PAY-{seq:0000}", PaidAt = start.AddDays(-1) });
            if (st == BookingStatus.DaTraXe)
            {
                var returnedAt = end.AddDays(rnd.Next(100) < 20 ? rnd.Next(1, 3) : 0);
                var damage = rnd.Next(100) < 10 ? rnd.Next(1, 6) * 100_000m : 0m;
                var (lateDays, lateFee, finalAmount) = CarRental.Application.Modules.Bookings.RentalService.CalculateReturn(
                    b.EndDate, returnedAt, b.FinalPricePerDay, b.TotalPrice, damage);
                b.ReturnRecord = new ReturnRecord
                {
                    ReturnedAt = returnedAt, LateDays = lateDays, LateFee = lateFee, DamageFee = damage,
                    DamageNote = damage > 0 ? "Trầy xước nhẹ." : null, FinalAmount = finalAmount, StaffId = staff.Id
                };
            }
            db.Bookings.Add(b);
            addedBookings++;
        }
    }
    db.SaveChanges();
}

// ---------- Thông số chi tiết xe (hộp số, nhiên liệu, năm sản xuất, mô tả) ----------
// Idempotent: chỉ điền cho xe chưa có hộp số. Năm sản xuất = năm hiện tại - tuổi xe.
var CarSpecs = new Dictionary<string, (Transmission T, FuelType F, string Desc)>
{
    ["BMW|520i"] = (Transmission.SoTuDong, FuelType.Xang, "Sedan hạng sang 5 chỗ, động cơ 2.0L turbo mạnh mẽ, nội thất da cao cấp, màn hình cong và hệ thống âm thanh chất lượng cao. Phù hợp đón tiếp đối tác, đám cưới và chuyến công tác quan trọng."),
    ["Ford|Ranger"] = (Transmission.SoTuDong, FuelType.Dau, "Bán tải máy dầu mạnh mẽ, thùng sau rộng, gầm cao và hệ dẫn động 2 cầu. Phù hợp chở hàng, đi đường đèo núi hoặc off-road nhẹ."),
    ["Honda|CR-V"] = (Transmission.SoTuDong, FuelType.Xang, "SUV 7 chỗ rộng rãi, vận hành êm, trang bị Honda Sensing hỗ trợ lái an toàn. Hàng ghế sau thoải mái, hợp đi gia đình đường dài."),
    ["Honda|City"] = (Transmission.SoTuDong, FuelType.Xang, "Sedan hạng B thiết kế trẻ trung, cabin rộng so với phân khúc, tiết kiệm nhiên liệu. Lựa chọn tốt để đi phố và đi tỉnh."),
    ["Hyundai|Accent"] = (Transmission.SoTuDong, FuelType.Xang, "Sedan gọn gàng, giá thuê dễ chịu, màn hình giải trí và camera lùi tiện dụng. Phù hợp đi làm, đi phố, chạy dịch vụ."),
    ["Hyundai|Grand i10"] = (Transmission.SoSan, FuelType.Xang, "Hatchback nhỏ gọn, dễ lái và dễ đỗ trong phố đông, rất tiết kiệm xăng. Phù hợp người mới lái và di chuyển nội thành."),
    ["Hyundai|Santa Fe"] = (Transmission.SoTuDong, FuelType.Dau, "SUV 7 chỗ máy dầu, nội thất sang trọng, nhiều tiện nghi và cửa sổ trời toàn cảnh. Hợp chuyến du lịch gia đình, nhóm bạn."),
    ["Kia|Carnival"] = (Transmission.SoTuDong, FuelType.Dau, "MPV 7 chỗ cao cấp, cửa lùa điện, hàng ghế thương gia ngả sâu. Rất phù hợp đưa đón nhóm khách, gia đình đông người."),
    ["Kia|K3"] = (Transmission.SoTuDong, FuelType.Xang, "Sedan hạng C thiết kế thể thao, vô lăng bọc da, màn hình cảm ứng và nhiều tính năng an toàn. Lái nhẹ và êm."),
    ["Kia|Morning"] = (Transmission.SoSan, FuelType.Xang, "Hatchback đô thị cỡ nhỏ, linh hoạt, chi phí vận hành thấp. Hợp di chuyển một mình hoặc đi cặp đôi."),
    ["Kia|Seltos"] = (Transmission.SoTuDong, FuelType.Xang, "SUV cỡ nhỏ 5 chỗ, gầm cao, màn hình lớn và nhiều tiện nghi. Cân bằng giữa đi phố và đi xa."),
    ["Lexus|RX350"] = (Transmission.SoTuDong, FuelType.Xang, "SUV hạng sang, động cơ V6 êm ái, cách âm xuất sắc, ghế massage và hệ thống âm thanh Mark Levinson. Trải nghiệm cao cấp cho dịp đặc biệt."),
    ["Mazda|CX-5"] = (Transmission.SoTuDong, FuelType.Xang, "SUV 5 chỗ thiết kế KODO, vô lăng chính xác, nội thất tinh tế. Được nhiều khách chọn cho các chuyến đi cuối tuần."),
    ["Mazda|Mazda3"] = (Transmission.SoTuDong, FuelType.Xang, "Sedan thiết kế đẹp, lái thú vị, nội thất chăm chút và cách âm tốt. Hợp người thích cảm giác lái."),
    ["Mercedes|C300 AMG"] = (Transmission.SoTuDong, FuelType.Xang, "Sedan hạng sang gói thể thao AMG, động cơ 2.0L turbo, nội thất ambient light 64 màu. Dành cho sự kiện và những dịp quan trọng."),
    ["Mitsubishi|Triton"] = (Transmission.SoTuDong, FuelType.Dau, "Bán tải bền bỉ, hệ dẫn động Super Select, khoang lái rộng. Phù hợp đi công trình, chở đồ và đường xấu."),
    ["Mitsubishi|Xpander"] = (Transmission.SoTuDong, FuelType.Xang, "MPV 7 chỗ phổ biến, khoang hành khách rộng, gầm cao dễ đi đường xấu. Lựa chọn kinh tế cho gia đình."),
    ["Suzuki|Ertiga"] = (Transmission.SoTuDong, FuelType.Xang, "MPV 7 chỗ nhỏ gọn, tiết kiệm nhiên liệu, giá thuê mềm. Hợp gia đình nhỏ hoặc nhóm bạn đi gần."),
    ["Toyota|Camry"] = (Transmission.SoTuDong, FuelType.Xang, "Sedan hạng D rộng rãi, vận hành êm ái, bền bỉ và giữ giá. Thích hợp đi công tác, đón khách."),
    ["Toyota|Fortuner"] = (Transmission.SoTuDong, FuelType.Dau, "SUV 7 chỗ máy dầu, gầm cao, khả năng vượt địa hình tốt. Chuyên đi đường dài, đi Tây Nguyên, đi núi."),
    ["Toyota|Innova"] = (Transmission.SoTuDong, FuelType.Xang, "MPV 7 chỗ bền bỉ, ghế ngồi thoải mái, chi phí vận hành thấp. Lựa chọn quen thuộc cho gia đình."),
    ["Toyota|Vios"] = (Transmission.SoTuDong, FuelType.Xang, "Sedan phổ thông tiết kiệm nhiên liệu, dễ lái, ít hỏng vặt. Phù hợp đi phố, đi tỉnh, người mới thuê xe."),
    ["Toyota|Wigo"] = (Transmission.SoTuDong, FuelType.Xang, "Hatchback nhỏ gọn, rất tiết kiệm xăng, dễ đỗ xe trong phố. Giá thuê thấp nhất phân khúc."),
    ["VinFast|Fadil"] = (Transmission.SoTuDong, FuelType.Xang, "Hatchback đô thị của VinFast, vô lăng nhẹ, màn hình 7 inch, nhiều tính năng an toàn. Hợp đi nội thành."),
    ["VinFast|VF e34"] = (Transmission.SoTuDong, FuelType.Dien, "SUV điện 5 chỗ, chạy khoảng 300 km mỗi lần sạc, vận hành êm và không phát thải. Chi phí quãng đường rất thấp."),
};
var thisYear = DateTime.UtcNow.Year;
int specsFilled = 0;
foreach (var c in db.Cars.Where(x => x.Transmission == null).ToList())
{
    if (!CarSpecs.TryGetValue($"{c.Brand}|{c.Model}", out var sp)) continue;
    c.Transmission = sp.T; c.FuelType = sp.F; c.ManufactureYear = thisYear - c.CarAgeYears; c.Description = sp.Desc;
    specsFilled++;
}
db.SaveChanges();
Console.WriteLine($"Đã điền thông số chi tiết cho {specsFilled} xe.");

Console.WriteLine($"Xong. Thêm mới: {addedUsers} tài khoản, {addedCars} xe, {addedBookings} booking.");
Console.WriteLine($"Tổng hiện có: {db.Customers.Count()} tài khoản, {db.Cars.Count()} xe, {db.Bookings.Count()} booking.");
Console.WriteLine();
Console.WriteLine($"Tài khoản DEMO (chỉ dùng demo) - mật khẩu chung: {DemoPassword}");
Console.WriteLine("  Admin   : admin@demo.local");
Console.WriteLine("  Staff   : staff@demo.local");
Console.WriteLine("  Khách 1 : an.nguyen@demo.local");
Console.WriteLine("  Khách 2 : binh.tran@demo.local");
Console.WriteLine("  Khách 3 : cuong.le@demo.local");
return 0;
