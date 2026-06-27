namespace ProxyAPI.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

public class InvalidClientIdException : DomainException
{
    public InvalidClientIdException() : base("Client ID must not be empty.") { }
}

public class TokenExpiredException : DomainException
{
    public TokenExpiredException() : base("Token has expired.") { }
}

public class InvalidStateException : DomainException
{
    public InvalidStateException(string message) : base(message) { }
}

public class OAuthException : DomainException
{
    public OAuthException(string message) : base(message) { }
}
