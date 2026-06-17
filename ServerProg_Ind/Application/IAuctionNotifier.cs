using ServerProg_Ind.Contracts;

namespace ServerProg_Ind.Application;

public interface IAuctionNotifier
{
    Task SendAuctionSnapshotAsync(AuctionDetailDto auction, CancellationToken cancellationToken);
    Task SendBidAcceptedAsync(BidPlacedResponse response, CancellationToken cancellationToken);
    Task SendNotificationAsync(Guid userId, NotificationDto notification, CancellationToken cancellationToken);
}
