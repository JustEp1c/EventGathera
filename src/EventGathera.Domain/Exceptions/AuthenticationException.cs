namespace EventGathera.Domain.Exceptions;

/// <summary>
/// Исключение при аутентификации
/// </summary>
public class AuthenticationException : Exception
{
    public AuthenticationException(string message) : base(message) { }
}
