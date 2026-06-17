using ServerProg_Ind.Contracts;
using ServerProg_Ind.Domain;

namespace ServerProg_Ind.Application;

public static class AuctionMappings
{
    public static AuctionSummaryDto ToSummaryDto(Auction auction)
    {
        return new AuctionSummaryDto(
            auction.Id,
            auction.Title,
            auction.Category,
            auction.Condition,
            auction.Images.OrderBy(x => x.SortOrder).Select(x => x.Url).FirstOrDefault() ?? "/green-toad-logo.svg",
            auction.CurrentBid ?? 0m,
            auction.StartingBid,
            auction.BuyNowPrice,
            auction.BidCount,
            auction.EndTimeUtc,
            auction.Status.ToString());
    }

    public static AuctionDetailDto ToDetailDto(Auction auction)
    {
        return new AuctionDetailDto(
            auction.Id,
            auction.Title,
            auction.Description,
            auction.Category,
            auction.Condition,
            auction.PickupLocation,
            auction.StartingBid,
            auction.MinimumIncrement,
            auction.BuyNowPrice,
            auction.CurrentBid,
            AuctionRules.GetMinimumNextBid(auction),
            auction.BidCount,
            auction.EndTimeUtc,
            auction.Status.ToString(),
            auction.CreatedAtUtc,
            new UserProfileDto(auction.Seller.Id, auction.Seller.DisplayName, auction.Seller.Email, auction.Seller.Handle),
            auction.Winner is null ? null : new UserProfileDto(auction.Winner.Id, auction.Winner.DisplayName, auction.Winner.Email, auction.Winner.Handle),
            auction.FinalPrice,
            auction.Images
                .OrderBy(x => x.SortOrder)
                .Select(x => new AuctionImageDto(x.Id, x.Url, x.SortOrder))
                .ToArray(),
            auction.Bids
                .OrderByDescending(x => x.CreatedAtUtc)
                .Take(20)
                .Select(x => new BidHistoryDto(
                    x.Id,
                    x.Amount,
                    x.CreatedAtUtc,
                    new UserProfileDto(x.Bidder.Id, x.Bidder.DisplayName, x.Bidder.Email, x.Bidder.Handle)))
                .ToArray());
    }

    public static NotificationDto ToDto(Notification notification)
    {
        return new NotificationDto(
            notification.Id,
            notification.Type.ToString(),
            notification.Title,
            notification.Message,
            notification.AuctionId,
            notification.IsRead,
            notification.CreatedAtUtc);
    }
}
