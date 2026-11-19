using System.Reflection;
using CarRental.API.Controllers;
using CarRental.Application.Modules.Bookings;
using Microsoft.AspNetCore.Authorization;

namespace CarRental.Tests.Unit;

/// <summary>Ma trận quyền đợt 1 (reflection trên attribute) và kiểm tra kiến trúc module.</summary>
public class AuthorizationMatrixTests
{
    private static string? Roles(Type controller, string action)
    {
        var m = controller.GetMethods().Single(x => x.Name == action && x.DeclaringType == controller);
        var attr = m.GetCustomAttribute<AuthorizeAttribute>() ?? controller.GetCustomAttribute<AuthorizeAttribute>();
        return attr is null ? null : string.Join(",", attr.Roles!.Split(',').OrderBy(x => x));
    }

    [Theory]
    [InlineData(typeof(CarsController), "Create", "Admin,Staff")]
    [InlineData(typeof(CarsController), "Update", "Admin,Staff")]
    [InlineData(typeof(CarsController), "Approve", "Admin")]
    [InlineData(typeof(CarsController), "Delete", "Admin")]
    [InlineData(typeof(AdminUsersController), "List", "Admin")]
    [InlineData(typeof(AdminUsersController), "CreateStaff", "Admin")]
    [InlineData(typeof(AdminUsersController), "ChangeRole", "Admin")]
    [InlineData(typeof(AdminUsersController), "Lock", "Admin")]
    [InlineData(typeof(AdminUsersController), "Unlock", "Admin")]
    [InlineData(typeof(AuditLogsController), "Query", "Admin")]
    [InlineData(typeof(BlacklistController), "List", "Admin,Staff")]
    [InlineData(typeof(BlacklistController), "Check", "Admin,Staff")]
    [InlineData(typeof(BlacklistController), "Create", "Admin")]
    [InlineData(typeof(BlacklistController), "Remove", "Admin")]
    [InlineData(typeof(ReportsController), "Revenue", "Admin")]
    public void Action_HasExpectedRoles(Type controller, string action, string expected)
        => Assert.Equal(expected, Roles(controller, action));

    [Fact]
    public void Cars_PublicReadsAreAnonymous()
    {
        Assert.Null(Roles(typeof(CarsController), "GetFiltered"));
        Assert.Null(Roles(typeof(CarsController), "GetById"));
    }

    // Kiến trúc: module Bookings chỉ được chạm module Customers qua ICustomerScreening/ScreeningResult.
    [Fact]
    public void BookingsModule_DoesNotReferenceCustomersModule_ExceptScreeningContract()
    {
        var allowed = new[] { "ICustomerScreening", "ScreeningResult", "IDocumentVerification", "ILoyaltyService" };
        var asm = typeof(BookingService).Assembly;
        var bookingTypes = asm.GetTypes().Where(t => t.Namespace == "CarRental.Application.Modules.Bookings").ToList();
        Assert.NotEmpty(bookingTypes);

        var violations = new List<string>();
        foreach (var t in bookingTypes)
        {
            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            var used = new List<Type>();
            used.AddRange(t.GetFields(flags).Select(f => f.FieldType));
            used.AddRange(t.GetProperties(flags).Select(p => p.PropertyType));
            used.AddRange(t.GetConstructors(flags).SelectMany(c => c.GetParameters().Select(p => p.ParameterType)));
            foreach (var m in t.GetMethods(flags))
            {
                used.Add(m.ReturnType);
                used.AddRange(m.GetParameters().Select(p => p.ParameterType));
                used.AddRange(m.GetMethodBody()?.LocalVariables.Select(l => l.LocalType) ?? []);
            }
            if (t.BaseType is not null) used.Add(t.BaseType);
            used.AddRange(t.GetInterfaces());

            foreach (var u in used.SelectMany(Flatten).Distinct())
                if (u.Namespace == "CarRental.Application.Modules.Customers" && !allowed.Contains(u.Name))
                    violations.Add($"{t.Name} -> {u.Name}");
        }
        Assert.True(violations.Count == 0, "Bookings tham chiếu trực tiếp Customers: " + string.Join("; ", violations));
    }

    [Fact]
    public void ApplicationAssembly_DoesNotReferenceInfrastructure()
    {
        var refs = typeof(BookingService).Assembly.GetReferencedAssemblies().Select(a => a.Name);
        Assert.DoesNotContain("CarRental.Infrastructure", refs);
        Assert.DoesNotContain("CarRental.API", refs);
    }

    private static IEnumerable<Type> Flatten(Type t)
    {
        if (t.HasElementType) foreach (var e in Flatten(t.GetElementType()!)) yield return e;
        if (t.IsGenericType) foreach (var a in t.GetGenericArguments().SelectMany(Flatten)) yield return a;
        yield return t;
    }
}
