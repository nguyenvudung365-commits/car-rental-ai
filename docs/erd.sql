-- Car Rental AI - ERD SQL Schema
-- Generated: 2026-09-08
-- Target: SQL Server
-- 6 bảng: Customer, Car, CarImage, Booking, Payment, ReturnRecord

-- Drop tables nếu tồn tại (development only)
IF OBJECT_ID('ReturnRecord', 'U') IS NOT NULL DROP TABLE ReturnRecord;
IF OBJECT_ID('Payment', 'U') IS NOT NULL DROP TABLE Payment;
IF OBJECT_ID('Booking', 'U') IS NOT NULL DROP TABLE Booking;
IF OBJECT_ID('CarImage', 'U') IS NOT NULL DROP TABLE CarImage;
IF OBJECT_ID('Car', 'U') IS NOT NULL DROP TABLE Car;
IF OBJECT_ID('Customer', 'U') IS NOT NULL DROP TABLE Customer;
GO

-- Customer: Khách hàng + Staff + Admin
CREATE TABLE Customer (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(200) NOT NULL,
    Phone NVARCHAR(15) NOT NULL UNIQUE,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(500) NOT NULL,
    Role INT NOT NULL, -- 0=Customer, 1=Staff, 2=Admin
    IdentityNumber NVARCHAR(20) NULL,
    Address NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsDeleted BIT NOT NULL DEFAULT 0
);

-- Car: Xe cho thuê
CREATE TABLE Car (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CarType INT NOT NULL, -- 0=Sedan, 1=SUV, 2=Hatchback, 3=Minivan, 4=Pickup
    Brand NVARCHAR(100) NOT NULL,
    Model NVARCHAR(100) NOT NULL,
    LicensePlate NVARCHAR(20) NOT NULL UNIQUE,
    Seats INT NOT NULL,
    CarAgeYears INT NOT NULL,
    BasePricePerDay DECIMAL(18,2) NOT NULL,
    Status INT NOT NULL, -- 0=Available, 1=Rented, 2=Maintenance, 3=Inactive
    OverridePrice DECIMAL(18,2) NULL,
    Description NVARCHAR(1000) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsDeleted BIT NOT NULL DEFAULT 0
);

-- CarImage: Ảnh xe (1 xe nhiều ảnh)
CREATE TABLE CarImage (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CarId INT NOT NULL FOREIGN KEY REFERENCES Car(Id),
    ImageUrl NVARCHAR(500) NOT NULL,
    IsPrimary BIT NOT NULL DEFAULT 0,
    SortOrder INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsDeleted BIT NOT NULL DEFAULT 0
);

-- Booking: Đơn thuê xe
CREATE TABLE Booking (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL FOREIGN KEY REFERENCES Customer(Id),
    CarId INT NOT NULL FOREIGN KEY REFERENCES Car(Id),
    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2 NOT NULL,
    PredictedPricePerDay DECIMAL(18,2) NULL,
    FinalPricePerDay DECIMAL(18,2) NOT NULL,
    TotalPrice DECIMAL(18,2) NOT NULL,
    IsFallback BIT NOT NULL DEFAULT 0,
    Status INT NOT NULL, -- 0=Pending, 1=Confirmed, 2=InProgress, 3=Completed, 4=Cancelled
    CorrelationId NVARCHAR(100) NULL,
    Note NVARCHAR(1000) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsDeleted BIT NOT NULL DEFAULT 0
);

-- Payment: Thanh toán (1-1 với Booking)
CREATE TABLE Payment (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    BookingId INT NOT NULL UNIQUE FOREIGN KEY REFERENCES Booking(Id),
    Amount DECIMAL(18,2) NOT NULL,
    Method INT NOT NULL, -- 0=Cash, 1=BankTransfer, 2=Momo, 3=ZaloPay
    Status INT NOT NULL, -- 0=Pending, 1=Completed, 2=Failed, 3=Refunded
    TransactionCode NVARCHAR(100) NULL,
    PaidAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsDeleted BIT NOT NULL DEFAULT 0
);

-- ReturnRecord: Biên bản trả xe (1-1 với Booking)
CREATE TABLE ReturnRecord (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    BookingId INT NOT NULL UNIQUE FOREIGN KEY REFERENCES Booking(Id),
    ReturnedAt DATETIME2 NOT NULL,
    LateDays INT NOT NULL DEFAULT 0,
    LateFee DECIMAL(18,2) NOT NULL DEFAULT 0,
    DamageFee DECIMAL(18,2) NOT NULL DEFAULT 0,
    DamageNote NVARCHAR(1000) NULL,
    FinalAmount DECIMAL(18,2) NOT NULL,
    StaffId INT NULL FOREIGN KEY REFERENCES Customer(Id),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsDeleted BIT NOT NULL DEFAULT 0
);
GO

-- Index cho tìm kiếm
CREATE INDEX IX_Customer_Email ON Customer(Email) WHERE IsDeleted = 0;
CREATE INDEX IX_Customer_Phone ON Customer(Phone) WHERE IsDeleted = 0;
CREATE INDEX IX_Car_Status ON Car(Status) WHERE IsDeleted = 0;
CREATE INDEX IX_Car_LicensePlate ON Car(LicensePlate) WHERE IsDeleted = 0;
CREATE INDEX IX_Booking_CustomerId ON Booking(CustomerId) WHERE IsDeleted = 0;
CREATE INDEX IX_Booking_CarId ON Booking(CarId) WHERE IsDeleted = 0;
CREATE INDEX IX_Booking_Status ON Booking(Status) WHERE IsDeleted = 0;
CREATE INDEX IX_CarImage_CarId ON CarImage(CarId) WHERE IsDeleted = 0;
GO
