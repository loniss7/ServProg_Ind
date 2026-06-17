using ServerProg_Ind.Application;
using ServerProg_Ind.Domain;

namespace ServerProg_Ind.Tests;

public sealed class AuctionBidProcessorTests
{
    [Fact]
    public async Task PlaceBidAsync_SerializesConcurrentBidsOnSameAuction()
    {
        var seller = UserFactory.Create("seller@greens.edu");
        var bidderA = UserFactory.Create("a@greens.edu");
        var bidderB = UserFactory.Create("b@greens.edu");
        var auction = AuctionFactory.Create(seller.Id, startingBid: 100, minimumIncrement: 10);
        var processor = new AuctionBidProcessor(new AuctionLockProvider());

        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var first = Task.Run(async () =>
        {
            await start.Task;
            return await TryPlaceAsync(processor, auction, bidderA, 100);
        });

        var second = Task.Run(async () =>
        {
            await start.Task;
            return await TryPlaceAsync(processor, auction, bidderB, 100);
        });

        start.SetResult();
        var results = await Task.WhenAll(first, second);

        Assert.Equal(1, results.Count(x => x.Success));
        Assert.Equal(1, results.Count(x => !x.Success));
        Assert.Equal(1, auction.BidCount);
        Assert.Equal(100, auction.CurrentBid);
    }

    private static async Task<(bool Success, string? Code)> TryPlaceAsync(AuctionBidProcessor processor, Auction auction, User bidder, decimal amount)
    {
        try
        {
            await processor.PlaceBidAsync(auction, bidder, amount, DateTime.UtcNow, CancellationToken.None);
            return (true, null);
        }
        catch (AppException exception)
        {
            return (false, exception.Code);
        }
    }
}
