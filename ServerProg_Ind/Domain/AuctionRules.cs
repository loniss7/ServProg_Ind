namespace ServerProg_Ind.Domain;

public static class AuctionRules
{
    public static void ValidateAuctionDraft(
        string title,
        string description,
        string category,
        string condition,
        string pickupLocation,
        decimal startingBid,
        decimal minimumIncrement,
        decimal? buyNowPrice,
        DateTime endTimeUtc,
        DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new AppException("title_required", "Title is required.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new AppException("description_required", "Description is required.");
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new AppException("category_required", "Category is required.");
        }

        if (string.IsNullOrWhiteSpace(condition))
        {
            throw new AppException("condition_required", "Condition is required.");
        }

        if (string.IsNullOrWhiteSpace(pickupLocation))
        {
            throw new AppException("pickup_required", "Pickup location is required.");
        }

        if (startingBid <= 0)
        {
            throw new AppException("starting_bid_invalid", "Starting bid must be greater than zero.");
        }

        if (minimumIncrement <= 0)
        {
            throw new AppException("minimum_increment_invalid", "Minimum increment must be greater than zero.");
        }

        if (buyNowPrice is not null && buyNowPrice <= startingBid)
        {
            throw new AppException("buy_now_invalid", "Buy now price must be greater than the starting bid.");
        }

        if (endTimeUtc <= nowUtc.AddHours(1) || endTimeUtc > nowUtc.AddDays(7))
        {
            throw new AppException("end_time_invalid", "End time must be between 1 hour and 7 days from now.");
        }
    }

    public static decimal GetMinimumNextBid(Auction auction)
    {
        if (auction.BidCount == 0 || auction.CurrentBid is null)
        {
            return auction.StartingBid;
        }

        return auction.CurrentBid.Value + auction.MinimumIncrement;
    }

    public static BidPlacementResult PlaceBid(Auction auction, User bidder, decimal requestedAmount, DateTime nowUtc)
    {
        EnsureAuctionIsActive(auction, nowUtc);

        if (auction.SellerId == bidder.Id)
        {
            throw new AppException("seller_cannot_bid", "Seller cannot bid on their own auction.", StatusCodes.Status403Forbidden);
        }

        var minimum = GetMinimumNextBid(auction);
        if (requestedAmount < minimum)
        {
            throw new AppException("bid_too_low", $"Bid must be at least {minimum:0.##}.");
        }

        var acceptedAmount = auction.BuyNowPrice is not null && requestedAmount >= auction.BuyNowPrice.Value
            ? auction.BuyNowPrice.Value
            : requestedAmount;

        var previousHighestBidderId = auction.HighestBidderId;

        var bid = new Bid
        {
            AuctionId = auction.Id,
            BidderId = bidder.Id,
            Bidder = bidder,
            Amount = acceptedAmount,
            CreatedAtUtc = nowUtc
        };

        auction.Bids.Add(bid);
        auction.CurrentBid = acceptedAmount;
        auction.BidCount += 1;
        auction.HighestBidderId = bidder.Id;
        auction.UpdatedAtUtc = nowUtc;

        var closedByBuyNow = auction.BuyNowPrice is not null && acceptedAmount >= auction.BuyNowPrice.Value;
        if (closedByBuyNow)
        {
            auction.Status = AuctionStatus.Ended;
            auction.EndedAtUtc = nowUtc;
            auction.WinnerId = bidder.Id;
            auction.FinalPrice = acceptedAmount;
        }

        return new BidPlacementResult(bid, previousHighestBidderId, closedByBuyNow);
    }

    public static AuctionFinalizationResult FinalizeAuction(Auction auction, DateTime nowUtc)
    {
        if (auction.Status != AuctionStatus.Active || auction.EndTimeUtc > nowUtc)
        {
            return new AuctionFinalizationResult(false, auction.WinnerId, auction.FinalPrice);
        }

        auction.Status = AuctionStatus.Ended;
        auction.EndedAtUtc = nowUtc;
        auction.UpdatedAtUtc = nowUtc;

        if (auction.HighestBidderId is not null && auction.CurrentBid is not null)
        {
            auction.WinnerId = auction.HighestBidderId;
            auction.FinalPrice = auction.CurrentBid;
        }

        return new AuctionFinalizationResult(true, auction.WinnerId, auction.FinalPrice);
    }

    public static void CancelAuction(Auction auction, Guid sellerId, DateTime nowUtc)
    {
        if (auction.SellerId != sellerId)
        {
            throw new AppException("forbidden", "Only the seller can cancel the auction.", StatusCodes.Status403Forbidden);
        }

        if (auction.Status != AuctionStatus.Active)
        {
            throw new AppException("auction_not_active", "Only active auctions can be canceled.");
        }

        if (auction.BidCount > 0)
        {
            throw new AppException("cannot_cancel_after_bid", "Auction cannot be canceled after the first bid.");
        }

        auction.Status = AuctionStatus.Canceled;
        auction.CanceledAtUtc = nowUtc;
        auction.UpdatedAtUtc = nowUtc;
    }

    public static void EnsureAuctionCanBeUpdated(Auction auction, Guid sellerId)
    {
        if (auction.SellerId != sellerId)
        {
            throw new AppException("forbidden", "Only the seller can update the auction.", StatusCodes.Status403Forbidden);
        }

        if (auction.Status != AuctionStatus.Active)
        {
            throw new AppException("auction_not_active", "Only active auctions can be updated.");
        }

        if (auction.BidCount > 0)
        {
            throw new AppException("cannot_update_after_bid", "Auction cannot be updated after the first bid.");
        }
    }

    public static void ConfirmSale(Auction auction, Guid winnerId, DateTime nowUtc)
    {
        if (auction.Status != AuctionStatus.Ended)
        {
            throw new AppException("auction_not_ended", "Only ended auctions can be confirmed.");
        }

        if (auction.WinnerId != winnerId)
        {
            throw new AppException("forbidden", "Only the winner can confirm the sale.", StatusCodes.Status403Forbidden);
        }

        auction.Status = AuctionStatus.Sold;
        auction.SoldAtUtc = nowUtc;
        auction.UpdatedAtUtc = nowUtc;
    }

    public static bool ShouldSendClosingSoon(Auction auction, DateTime nowUtc, TimeSpan window)
    {
        if (auction.Status != AuctionStatus.Active || auction.ClosingSoonNotified)
        {
            return false;
        }

        var remaining = auction.EndTimeUtc - nowUtc;
        return remaining > TimeSpan.Zero && remaining <= window;
    }

    private static void EnsureAuctionIsActive(Auction auction, DateTime nowUtc)
    {
        if (auction.Status != AuctionStatus.Active)
        {
            throw new AppException("auction_not_active", "Auction is not active.");
        }

        if (auction.EndTimeUtc <= nowUtc)
        {
            throw new AppException("auction_ended", "Auction has already ended.");
        }
    }
}

public sealed record BidPlacementResult(Bid Bid, Guid? PreviousHighestBidderId, bool ClosedByBuyNow);

public sealed record AuctionFinalizationResult(bool StateChanged, Guid? WinnerId, decimal? FinalPrice);
