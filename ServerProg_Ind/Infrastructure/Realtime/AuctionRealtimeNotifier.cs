using Microsoft.AspNetCore.SignalR;
using ServerProg_Ind.Application;
using ServerProg_Ind.Contracts;

namespace ServerProg_Ind.Infrastructure.Realtime;

public sealed class AuctionRealtimeNotifier(IHubContext<AuctionHub> hubContext) : IAuctionNotifier
{
    public Task SendAuctionSnapshotAsync(AuctionDetailDto auction, CancellationToken cancellationToken)
    {
        return hubContext.Clients.Group(GroupNames.Auction(auction.Id))
            .SendAsync("auctionUpdated", auction, cancellationToken);
    }

    public Task SendBidAcceptedAsync(BidPlacedResponse response, CancellationToken cancellationToken)
    {
        return hubContext.Clients.Group(GroupNames.Auction(response.AuctionId))
            .SendAsync("bidAccepted", response, cancellationToken);
    }

    public Task SendNotificationAsync(Guid userId, NotificationDto notification, CancellationToken cancellationToken)
    {
        return hubContext.Clients.Group(GroupNames.User(userId))
            .SendAsync("notificationCreated", notification, cancellationToken);
    }
}
