namespace TechnicalTest.Domain.Exceptions;

public class DomainException : InvalidOperationException
{
    public DomainException(string message) : base(message)
    {
    }
}

