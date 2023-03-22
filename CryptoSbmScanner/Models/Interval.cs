namespace CryptoSbmScanner.Models;

public sealed class Interval
{
    private Interval() { }
    public IntervalPeriod Id { get; private set; }
    public IntervalPeriod? ConstructFrom { get; private set; }
    public static Interval Create(IntervalPeriod id, IntervalPeriod? constructFrom = null) => new()
    {
        Id = id,
        ConstructFrom = constructFrom
    };
}

public enum IntervalPeriod
{
    OneMinute = 60,
    TwoMinutes = 60 * 2,
    ThreeMinutes = 60 * 3,
    FiveMinutes = 60 * 5,
    TenMinutes = 60 * 10,
    FifteenMinutes = 60 * 15,
    ThirtyMinutes = 60 * 30,
    OneHour = 60 * 60,
    TwoHours = 60 * 60 * 2,
    ThreeHours = 60 * 60 * 3,
    FourHours = 60 * 60 * 4,
    SixHours = 60 * 60 * 6,
    EightHours = 60 * 60 * 8,
    TwelveHours = 60 * 60 * 12,
    OneDay = 60 * 60 * 24
}