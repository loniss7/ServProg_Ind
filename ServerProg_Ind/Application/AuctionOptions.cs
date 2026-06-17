namespace ServerProg_Ind.Application;

public sealed class AuctionOptions
{
    public int ClosingSoonWindowMinutes { get; init; } = 5;
    public int SchedulerIntervalSeconds { get; init; } = 10;
}
