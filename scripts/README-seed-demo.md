# Seed dữ liệu demo

Chạy tay, idempotent (chạy lại không nhân đôi; khoá: email/SĐT, biển số, CorrelationId `demo-seed-NN`).
Tự chạy migration trước khi nạp. CHỈ DÙNG DEMO/DEV, không chạy trên production.

```
dotnet run --project tools/CarRental.DemoSeed -- "Server=(localdb)\MSSQLLocalDB;Database=CarRentalDemo;Trusted_Connection=True;TrustServerCertificate=True"
```

Hoặc đặt biến `ConnectionStrings__Default` rồi chạy không tham số.

Tài khoản demo (mật khẩu chung `Demo@12345`): admin@demo.local, staff@demo.local,
an.nguyen@demo.local, binh.tran@demo.local, cuong.le@demo.local.

Dữ liệu: 12 xe (2 xe BaoTri, 1 xe DangThue theo booking), 8 booking đủ 5 trạng thái kèm payment/return record.
Ngày booking tính tương đối theo ngày chạy script.
