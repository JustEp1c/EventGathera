using EventGathera.Users.Application.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace EventGathera.Users.Infrastructure.Security;

/// <summary>
/// <inheritdoc/>
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        var hashedInput = Convert.ToHexString(bytes);

        return hashedInput.Equals(hashedPassword, StringComparison.Ordinal);
    }
}
