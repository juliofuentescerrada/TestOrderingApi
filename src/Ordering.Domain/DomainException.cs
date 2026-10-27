namespace Ordering.Domain;

/// <summary>Se lanza cuando se viola una invariante del dominio.</summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
