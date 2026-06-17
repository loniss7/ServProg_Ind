using ServerProg_Ind.Domain;

namespace ServerProg_Ind.Tests;

public sealed class AuctionRulesTests
{
    [Fact]
    public void PlaceBid_AcceptsValidBidAndUpdatesAuction()
    {
        var seller = UserFactory.Create("seller@greens.edu");
        var bidder = UserFactory.Create("bidder@greens.edu");
        var auction = AuctionFactory.Create(seller.Id, startingBid: 100, minimumIncrement: 10);

        var result = AuctionRules.PlaceBid(auction, bidder, 100, DateTime.UtcNow);

        Assert.Equal(100, result.Bid.Amount);
        Assert.False(result.ClosedByBuyNow);
        Assert.Equal(1, auction.BidCount);
        Assert.Equal(100, auction.CurrentBid);
        Assert.Equal(bidder.Id, auction.HighestBidderId);
    }

    [Fact]
    public void PlaceBid_RejectsBidBelowMinimumIncrement()
    {
        var seller = UserFactory.Create("seller@greens.edu");
        var firstBidder = UserFactory.Create("first@greens.edu");
        var secondBidder = UserFactory.Create("second@greens.edu");
        var auction = AuctionFactory.Create(seller.Id, startingBid: 100, minimumIncrement: 10);

        AuctionRules.PlaceBid(auction, firstBidder, 100, DateTime.UtcNow);

        var exception = Assert.Throws<AppException>(() => AuctionRules.PlaceBid(auction, secondBidder, 109, DateTime.UtcNow));
        Assert.Equal("bid_too_low", exception.Code);
    }

    [Fact]
    public void PlaceBid_RejectsSellerBid()
    {
        var seller = UserFactory.Create("seller@greens.edu");
        var auction = AuctionFactory.Create(seller.Id, startingBid: 100, minimumIncrement: 10);

        var exception = Assert.Throws<AppException>(() => AuctionRules.PlaceBid(auction, seller, 100, DateTime.UtcNow));
        Assert.Equal("seller_cannot_bid", exception.Code);
    }

    [Fact]
    public void PlaceBid_BuyNowClosesAuctionImmediately()
    {
        var seller = UserFactory.Create("seller@greens.edu");
        var bidder = UserFactory.Create("winner@greens.edu");
        var auction = AuctionFactory.Create(seller.Id, startingBid: 100, minimumIncrement: 10, buyNowPrice: 150);

        var result = AuctionRules.PlaceBid(auction, bidder, 170, DateTime.UtcNow);

        Assert.True(result.ClosedByBuyNow);
        Assert.Equal(AuctionStatus.Ended, auction.Status);
        Assert.Equal(150, auction.FinalPrice);
        Assert.Equal(bidder.Id, auction.WinnerId);
    }

    [Fact]
    public void FinalizeAuction_SelectsHighestBidderWhenEndTimePassed()
    {
        var seller = UserFactory.Create("seller@greens.edu");
        var bidder = UserFactory.Create("winner@greens.edu");
        var auction = AuctionFactory.Create(seller.Id, startingBid: 100, minimumIncrement: 10, endTimeUtc: DateTime.UtcNow.AddMinutes(-1));

        AuctionRules.PlaceBid(auction, bidder, 100, DateTime.UtcNow.AddMinutes(-2));
        var result = AuctionRules.FinalizeAuction(auction, DateTime.UtcNow);

        Assert.True(result.StateChanged);
        Assert.Equal(AuctionStatus.Ended, auction.Status);
        Assert.Equal(bidder.Id, auction.WinnerId);
        Assert.Equal(100, auction.FinalPrice);
    }

    [Fact]
    public void FinalizeAuction_WithoutBidsEndsWithoutWinner()
    {
        var seller = UserFactory.Create("seller@greens.edu");
        var auction = AuctionFactory.Create(seller.Id, startingBid: 100, minimumIncrement: 10, endTimeUtc: DateTime.UtcNow.AddMinutes(-1));

        var result = AuctionRules.FinalizeAuction(auction, DateTime.UtcNow);

        Assert.True(result.StateChanged);
        Assert.Equal(AuctionStatus.Ended, auction.Status);
        Assert.Null(auction.WinnerId);
        Assert.Null(auction.FinalPrice);
    }
}
