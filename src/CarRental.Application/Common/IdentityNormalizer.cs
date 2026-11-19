namespace CarRental.Application.Common;

/// <summary>Chuẩn hóa SĐT/CCCD để lưu và so khớp (danh sách chặn, giấy tờ). Trả null khi không còn chữ số nào.</summary>
public static class IdentityNormalizer
{
    /// <summary>Chỉ giữ chữ số; tiền tố quốc tế 84 (từ 11 chữ số) đổi thành 0.</summary>
    public static string? Phone(string? value)
    {
        var digits = Cccd(value);
        if (digits is { Length: >= 11 } && digits.StartsWith("84", StringComparison.Ordinal))
            digits = "0" + digits[2..];
        return digits;
    }

    /// <summary>Bỏ khoảng trắng và mọi ký tự không phải chữ số.</summary>
    public static string? Cccd(string? value)
    {
        var digits = value is null ? "" : new string(value.Where(char.IsAsciiDigit).ToArray());
        return digits.Length == 0 ? null : digits;
    }
}
