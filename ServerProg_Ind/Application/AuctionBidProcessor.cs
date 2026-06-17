using ServerProg_Ind.Domain;

namespace ServerProg_Ind.Application;

public sealed class AuctionBidProcessor(AuctionLockProvider lockProvider)
{
    public async Task<BidPlacementResult> PlaceBidAsync(
        Auction auction,
        User bidder,
        decimal amount,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var gate = lockProvider.Get(auction.Id);
        await gate.WaitAsync(cancellationToken);
        try
        {
            return AuctionRules.PlaceBid(auction, bidder, amount, nowUtc);
        }
        finally
        {
            gate.Release();
        }
    }
}
