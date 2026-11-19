using System.Reflection;
using PdfSharp.Fonts;

namespace CarRental.Infrastructure.Pdf;

/// <summary>Resolver font Be Vietnam Pro dùng chung cho báo cáo PDF.</summary>
public static class PdfFonts
{
    public const string Family = "BeVietnamPro";
    private static readonly object Lock = new();
    private static bool _ready;

    public static void Ensure()
    {
        if (_ready) return;
        lock (Lock)
        {
            // shortcut: trùng resolver với ContractPdfGenerator để tránh xung đột (??= chỉ đặt một lần); gộp khi luồng hợp đồng ổn định
            GlobalFontSettings.FontResolver ??= new Resolver();
            _ready = true;
        }
    }

    private sealed class Resolver : IFontResolver
    {
        public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
            => string.Equals(familyName, Family, StringComparison.OrdinalIgnoreCase)
                ? new FontResolverInfo(isBold ? "BeVietnamPro-Bold" : "BeVietnamPro-Regular")
                : null;

        public byte[]? GetFont(string faceName)
        {
            using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream($"CarRental.Infrastructure.Pdf.Fonts.{faceName}.ttf");
            if (s == null) return null;
            using var ms = new MemoryStream();
            s.CopyTo(ms);
            return ms.ToArray();
        }
    }
}
