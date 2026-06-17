using System.Collections.Concurrent;

namespace ServerProg_Ind.Application;

public sealed class AuctionLockProvider
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public SemaphoreSlim Get(Guid auctionId)
    {
        return _locks.GetOrAdd(auctionId, static _ => new SemaphoreSlim(1, 1));
    }
}
