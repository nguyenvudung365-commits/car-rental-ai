using Microsoft.Data.SqlClient;
using Xunit;

namespace CarRental.Tests.Support;

/// <summary>
/// [Fact] cần SQL Server/LocalDB. Nếu không kết nối được thì test được đánh dấu BỎ QUA (Skipped) thay vì "xanh giả".
/// Đặt CARRENTAL_TEST_CONN để dùng SQL Server khác.
/// </summary>
public sealed class LocalDbFactAttribute : FactAttribute
{
    private static readonly Lazy<bool> Available = new(Probe);

    public static string ServerConnection =>
        Environment.GetEnvironmentVariable("CARRENTAL_TEST_CONN")
        ?? "Server=(localdb)\\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true";

    public LocalDbFactAttribute()
    {
        if (!Available.Value) Skip = "Không kết nối được SQL Server/LocalDB (đặt CARRENTAL_TEST_CONN nếu cần).";
    }

    private static bool Probe()
    {
        try
        {
            using var c = new SqlConnection(ServerConnection + ";Connect Timeout=10");
            c.Open();
            return true;
        }
        catch (Exception) { return false; }
    }
}
