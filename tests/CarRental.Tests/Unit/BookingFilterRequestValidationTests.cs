using System.ComponentModel.DataAnnotations;
using CarRental.Application.Modules.Bookings;
using Xunit;

namespace CarRental.Tests.Unit;

/// <summary>Phan trang booking: Page >= 1, PageSize 1..200.</summary>
public sealed class BookingFilterRequestValidationTests
{
    private static bool IsValid(int page, int pageSize)
    {
        var filter = new BookingFilterRequest(null, null, null, null, null) { Page = page, PageSize = pageSize };
        return Validator.TryValidateObject(filter, new ValidationContext(filter), new List<ValidationResult>(), validateAllProperties: true);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 201)]
    public void InvalidPaging_FailsValidation(int page, int pageSize) => Assert.False(IsValid(page, pageSize));

    [Theory]
    [InlineData(1, 10)]
    [InlineData(5, 1)]
    [InlineData(1, 200)]
    public void ValidPaging_PassesValidation(int page, int pageSize) => Assert.True(IsValid(page, pageSize));
}
