namespace BTGPactual.Fondos.Domain.Exceptions;

public class DomainException : InvalidOperationException
{
    public DomainException(string message) : base(message)
    {
    }
}

