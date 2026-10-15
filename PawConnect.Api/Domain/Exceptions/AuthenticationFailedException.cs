namespace PawConnect.Api.Domain.Exceptions;

public class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException(string message) : base(message)
    {
    }
}