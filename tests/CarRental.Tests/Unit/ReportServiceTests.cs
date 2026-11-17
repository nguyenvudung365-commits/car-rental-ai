using CarRental.Application.Common;
using CarRental.Application.Modules.Reports;
using Xunit;

namespace CarRental.Tests.Unit;

public class ReportServiceTests
{
    [Fact]
    public void NormalizeRange_DefaultsToLast30Days()
    {
        var (from, to) = ReportService.NormalizeRange(null, null);
        Assert.Equal(30, (to - from).Days);
    }

    [Fact]
    public void NormalizeRange_StripsTimeOfDay()
    {
        var (from, to) = ReportService.NormalizeRange(new DateTime(2026, 11, 1, 15, 0, 0), new DateTime(2026, 11, 5, 9, 0, 0));
        Assert.Equal(new DateTime(2026, 11, 1), from);
        Assert.Equal(new DateTime(2026, 11, 5), to);
    }

    [Fact]
    public void NormalizeRange_RejectsReversedRange()
    {
        Assert.Throws<ValidationAppException>(() =>
            ReportService.NormalizeRange(new DateTime(2026, 11, 5), new DateTime(2026, 11, 1)));
    }

    [Fact]
    public void NormalizeRange_RejectsRangeOverOneYear()
    {
        Assert.Throws<ValidationAppException>(() =>
            ReportService.NormalizeRange(new DateTime(2025, 1, 1), new DateTime(2026, 11, 1)));
    }
}
