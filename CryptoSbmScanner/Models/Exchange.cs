namespace CryptoSbmScanner.Models;

public sealed class Exchange
{
    private Exchange() { }
    public ExchangeId Id { get; private set; }
    public DateTimeOffset ExchangeInfoLastTime { get; set; } = DateTimeOffset.MinValue;    
    public static Exchange Create(string name) => new()
    {
        Id = new ExchangeId(name)
    };
}

public record ExchangeId(string Value);