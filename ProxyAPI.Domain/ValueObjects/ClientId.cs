namespace ProxyAPI.Domain.ValueObjects;

public class ClientId : IEquatable<ClientId>
{
    public string Value { get; }

    public ClientId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("ClientId cannot be empty.", nameof(value));

        Value = value;
    }

    public bool Equals(ClientId? other) => other?.Value == Value;
    public override bool Equals(object? obj) => obj is ClientId ci && Equals(ci);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;
}
