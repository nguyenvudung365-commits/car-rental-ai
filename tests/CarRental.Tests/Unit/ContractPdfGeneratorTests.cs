using System.Text;
using CarRental.Application.Modules.Bookings;
using CarRental.Infrastructure.Pdf;
using Xunit;

namespace CarRental.Tests.Unit;

public sealed class ContractPdfGeneratorTests
{
    private static ContractData Data(
        string name = "Nguyễn Văn An", string? cccd = "012345678901", string? addr = "12 Lê Lợi, Quận 1, TP. Hồ Chí Minh",
        bool withReturn = false) => new(
        42, withReturn ? "DaTraXe" : "DaXacNhan", new DateTime(2030, 1, 1, 8, 0, 0, DateTimeKind.Utc),
        name, "an@example.vn", "0901234567", cccd, addr,
        "Toyota Vios", "51A-123.45", 5,
        new DateTime(2030, 1, 5), new DateTime(2030, 1, 8), 3, 700_000, 2_100_000,
        withReturn ? new DateTime(2030, 1, 10, 9, 30, 0) : null,
        withReturn ? 2 : null, withReturn ? 1_400_000m : null, withReturn ? 300_000m : null, withReturn ? 3_800_000m : null);

    private static readonly ContractPdfGenerator Gen = new();

    [Fact]
    public void Generate_StartsWithPdfHeader_AndNonEmpty()
    {
        var bytes = Gen.Generate(Data());
        Assert.True(bytes.Length > 0);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public void Generate_EndsWithEofMarker()
    {
        var bytes = Gen.Generate(Data());
        var tail = Encoding.ASCII.GetString(bytes, Math.Max(0, bytes.Length - 32), Math.Min(32, bytes.Length));
        Assert.Contains("%%EOF", tail);
    }

    [Fact]
    public void Generate_WithReturnBlock_Works_AndDiffersFromWithout()
    {
        var without = Gen.Generate(Data());
        var with = Gen.Generate(Data(withReturn: true));
        Assert.Equal("%PDF", Encoding.ASCII.GetString(with, 0, 4));
        Assert.NotEqual(without.Length, with.Length); // có thêm khối "KẾT QUẢ TRẢ XE"
    }

    [Fact]
    public void Generate_VietnameseDiacritics_AllToneMarks_DoesNotThrow()
    {
        var name = "Trần Thị Ngọc Ánh Ưu Đạt Ở Ỹ ẳ ằ ẵ ặ ề ể ễ ệ ồ ổ ỗ ộ ờ ở ỡ ợ ừ ử ữ ự ỳ ỷ ỹ ỵ";
        var bytes = Gen.Generate(Data(name: name, addr: "Đường Nguyễn Huệ, phường Bến Nghé, quận 1"));
        Assert.True(bytes.Length > 1000);
    }

    [Fact]
    public void Generate_NullOptionalFields_DoesNotThrow()
    {
        var bytes = Gen.Generate(Data(cccd: null, addr: null));
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public void Generate_VeryLongAddress_WrapsWithoutThrowing()
    {
        var addr = string.Join(" ", Enumerable.Repeat("Số 123 đường Nguyễn Văn Cừ nối dài", 12));
        var bytes = Gen.Generate(Data(addr: addr));
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void Generate_CalledRepeatedlyAndConcurrently_IsStable()
    {
        var results = Enumerable.Range(0, 8).AsParallel()
            .Select(i => Gen.Generate(Data(withReturn: i % 2 == 0))).ToList();
        Assert.All(results, b => Assert.Equal("%PDF", Encoding.ASCII.GetString(b, 0, 4)));
    }
}
