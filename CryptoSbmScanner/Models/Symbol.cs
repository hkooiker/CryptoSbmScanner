namespace CryptoSbmScanner.Models;

public sealed class Symbol
{
    private Symbol() { }

    public ExchangeId ExchangeId { get; private set; }
    public SymbolId Id { get; private set; }
    public string Name { get; private set; }
    public string Base { get; private set; }
    public string Quote { get; private set; }
    public decimal Volume { get; set; }
    public static Symbol Create(ExchangeId exchangeId, string name, string @base, string quote) => new()
    {
        Id = new SymbolId(Guid.NewGuid()),
        ExchangeId = exchangeId,
        Name = name,
        Base = @base,
        Quote = quote
    };
}

public record SymbolId(Guid Value);