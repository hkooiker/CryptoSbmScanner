﻿using Binance.Net.Interfaces;
using Binance.Net.Interfaces.Clients;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Sockets;
using CryptoSbmScanner.Notifications;
using MediatR;

namespace CryptoSbmScanner.Services;

public sealed class TickerUpdateService : INotificationHandler<SymbolsLoaded>
{
    private readonly IBinanceSocketClient _socketClient;
    private readonly SymbolService _symbolService;

    private UpdateSubscription _subscription;

    public TickerUpdateService(IBinanceSocketClient socketClient, SymbolService symbolService)
    {
        _socketClient = socketClient;
        _symbolService = symbolService;
    }

    public async Task Handle(SymbolsLoaded notification, CancellationToken cancellationToken)
    {
        CallResult<UpdateSubscription> subscription = await _socketClient.SpotStreams.SubscribeToAllTickerUpdatesAsync((data) =>
        {
            foreach(IBinanceTick tick in data.Data)
            {
                _symbolService.Update(tick);
            }
        }, cancellationToken);
        if (subscription.Success)
        {
            _subscription = subscription.Data;
        }       
    }
}