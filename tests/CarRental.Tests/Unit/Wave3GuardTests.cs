using CarRental.Application.Common;
using CarRental.Application.Modules.Bookings;
using CarRental.Application.Modules.Customers;
using CarRental.Domain.Common;
using Moq;

namespace CarRental.Tests.Unit;

public class Wave3GuardTests
{
    [Fact]
    public async Task CreatePayment_TruCoc_400_InnerNotCalled()
    {
        var inner = new Mock<IRentalService>();
        var g = new RentalServiceGuard(inner.Object, new Mock<IBookingService>().Object, new Mock<IDocumentVerification>().Object, new Mock<ILoyaltyService>().Object);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => g.CreatePaymentAsync(1, new CreatePaymentRequest(100_000, PaymentMethod.TruCoc, null)));
        Assert.Equal("INVALID_METHOD", ex.Code);
        inner.Verify(i => i.CreatePaymentAsync(It.IsAny<int>(), It.IsAny<CreatePaymentRequest>(), It.IsAny<int?>()), Times.Never);
    }
}
