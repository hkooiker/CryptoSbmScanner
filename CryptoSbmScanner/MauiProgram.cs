using Binance.Net;
using CommunityToolkit.Maui;
using CryptoExchange.Net.Objects;
using CryptoSbmScanner.Pages;
using CryptoSbmScanner.Repositories;
using CryptoSbmScanner.Services;
using CryptoSbmScanner.ViewModels;
using Microsoft.Extensions.Logging;

namespace CryptoSbmScanner;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
        builder.Services
            .AddBinance((client, socket) =>
            {
                socket.SpotStreamsOptions.SocketResponseTimeout = TimeSpan.FromMinutes(1);
            }, ServiceLifetime.Singleton)
            .AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(MauiProgram).Assembly);
            })
            .AddSingleton<ExchangeRepository>()
            .AddSingleton<SymbolRepository>()
            .AddSingleton<CandleRepository>()
            .AddSingleton<IntervalRepository>()
            .AddSingleton<BinanceService>()
            .AddSingleton<SymbolService>()
            .AddSingleton<KlineUpdateService>()
            .AddSingleton<TickerUpdateService>()
            .AddSingleton<MainPage, MainViewModel>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}