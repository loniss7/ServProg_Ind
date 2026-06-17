using ServerProg_Ind.Domain;

namespace ServerProg_Ind.Application;

public sealed class AuctionLifecycleProcessor(AuctionLockProvider lockProvider)
{
    public async Task<AuctionFinalizationResult> FinalizeAuctionAsync(
        Auction auction,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var gate = lockProvider.Get(auction.Id);
        await gate.WaitAsync(cancellationToken);
        try
        {
            return AuctionRules.FinalizeAuction(auction, nowUtc);
        }
        finally
        {
            gate.Release();
        }
    }
}
