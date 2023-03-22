using MediatR;

namespace CryptoSbmScanner.Notifications;

public sealed record SymbolsLoaded(string[] Symbols) : INotification;