namespace ProxyAPI.Infrastructure.Exceptions;

public abstract class ProxyAPIException : Exception
{
    protected ProxyAPIException(string message) : base(message) { }
}

public class InvalidClientIdException : ProxyAPIException
{
    public InvalidClientIdException() : base("Client ID must not be empty.") { }
}

public class TokenExpiredException : ProxyAPIException
{
    public TokenExpiredException() : base("Token has expired.") { }
}

public class InvalidStateException : ProxyAPIException
{
    public InvalidStateException(string message) : base(message) { }
}

public class OAuthException : ProxyAPIException
{
    public OAuthException(string message) : base(message) { }
}
