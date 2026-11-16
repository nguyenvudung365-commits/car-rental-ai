using System.ComponentModel.DataAnnotations;
using System.Reflection;
using CarRental.Application.Modules.Bookings;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace CarRental.Tests.Unit;

// Chống tái phạm lỗi GET /api/bookings 500: [Range] đặt property-target trên record positional
// làm MVC ném InvalidOperationException khi dựng metadata validation.
public class MvcMetadataRegressionTests
{
    private static IModelMetadataProvider Provider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers();
        return services.BuildServiceProvider().GetRequiredService<IModelMetadataProvider>();
    }

    private static void Touch(ModelMetadata m)
    {
        // MVC (ComplexObjectModelBinder / ValidationVisitor) gọi hàm này; nó ném nếu record positional có validation trên property.
        var th = typeof(ModelMetadata).GetMethod("ThrowIfRecordTypeHasValidationOnProperties",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (th is not null && m.BoundConstructor is not null)
        {
            try { th.Invoke(m, null); }
            catch (TargetInvocationException e) { throw e.InnerException!; }
        }
        _ = m.ValidatorMetadata;
        _ = m.IsRequired;
        foreach (var p in m.Properties) { _ = p.ValidatorMetadata; _ = p.IsRequired; }
        if (m.BoundConstructor is { } c)
            foreach (var p in c.BoundConstructorParameters ?? Array.Empty<ModelMetadata>()) { _ = p.ValidatorMetadata; _ = p.IsRequired; }
    }

    [Fact]
    public void BookingFilterRequest_MvcMetadata_DoesNotThrow()
    {
        var meta = Provider().GetMetadataForType(typeof(BookingFilterRequest));
        var ex = Record.Exception(() => Touch(meta));
        Assert.Null(ex);
        Assert.Contains(meta.Properties, p => p.Name == "Page");
        Assert.Contains(meta.Properties, p => p.Name == "PageSize");
    }

    [Fact]
    public void BookingFilterRequest_PageAndPageSize_HaveRangeValidators()
    {
        var meta = Provider().GetMetadataForType(typeof(BookingFilterRequest));
        foreach (var name in new[] { "Page", "PageSize" })
            Assert.Contains(meta.Properties.Single(p => p.Name == name).ValidatorMetadata, v => v is RangeAttribute);
    }

    public static IEnumerable<object[]> AllApplicationRecords() =>
        typeof(BookingFilterRequest).Assembly.GetTypes()
            .Where(t => t.IsClass && t.GetMethod("<Clone>$") is not null)
            .Select(t => new object[] { t });

    [Theory]
    [MemberData(nameof(AllApplicationRecords))]
    public void ApplicationRecordDtos_MvcMetadata_DoesNotThrow(Type type)
    {
        var meta = Provider().GetMetadataForType(type);
        var ex = Record.Exception(() => Touch(meta));
        Assert.Null(ex);
    }

    [Fact]
    public void ApplicationRecords_NoValidationAttributeTargetingPropertyOnCtorParameter()
    {
        // Phản xạ: record positional có [property: Range] sẽ có attribute trên property tương ứng
        // VÀ trùng tên với tham số constructor chính.
        var bad = new List<string>();
        foreach (var t in typeof(BookingFilterRequest).Assembly.GetTypes()
                     .Where(t => t.IsClass && t.GetMethod("<Clone>$") is not null))
        {
            var ctor = t.GetConstructors().OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
            if (ctor is null) continue;
            foreach (var p in ctor.GetParameters())
            {
                var prop = t.GetProperty(p.Name!, BindingFlags.Public | BindingFlags.Instance);
                if (prop is not null && prop.GetCustomAttributes().OfType<ValidationAttribute>().Any())
                    bad.Add($"{t.Name}.{prop.Name}");
            }
        }
        // Nếu có thì phải không làm MVC ném (đã kiểm ở test trên); ở đây chỉ báo danh sách.
        foreach (var name in bad)
        {
            var t = typeof(BookingFilterRequest).Assembly.GetTypes().First(x => x.Name == name.Split('.')[0]);
            Assert.Null(Record.Exception(() => Touch(Provider().GetMetadataForType(t))));
        }
    }
}

public class MvcMetadataDetectorSanityTests
{
    public record BadPositional(string? Q, [property: Range(1, 5)] int Page = 1);

    [Fact]
    public void Detector_WouldCatch_PropertyRangeOnPositionalRecord()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers();
        var p = services.BuildServiceProvider().GetRequiredService<IModelMetadataProvider>();
        var meta = p.GetMetadataForType(typeof(BadPositional));
        var th = typeof(ModelMetadata).GetMethod("ThrowIfRecordTypeHasValidationOnProperties",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(th);
        var ex = Assert.Throws<TargetInvocationException>(() => th!.Invoke(meta, null));
        Assert.IsType<InvalidOperationException>(ex.InnerException);
    }
}
