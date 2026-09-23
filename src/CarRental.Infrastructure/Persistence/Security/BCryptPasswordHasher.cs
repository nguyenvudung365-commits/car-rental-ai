using CarRental.Application.Common;

namespace CarRental.Infrastructure.Security;

public class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string plainPassword) => BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: 12);
    public bool Verify(string plainPassword, string hash) => BCrypt.Net.BCrypt.Verify(plainPassword, hash);
}