using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ServerProg_Ind.Application;
using ServerProg_Ind.Infrastructure.Data;

namespace ServerProg_Ind.Infrastructure.Realtime;

[Authorize]
public sealed class AuctionHub(ApplicationDbContext dbContext) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = UserClaims.GetRequiredUserId(Context.User!);
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.User(userId));
        await base.OnConnectedAsync();
    }

    public Task JoinAuction(Guid auctionId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.Auction(auctionId));
    }

    public Task LeaveAuction(Guid auctionId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNames.Auction(auctionId));
    }

    public async Task RequestSnapshot(Guid auctionId)
    {
        var auction = await dbContext.Auctions
            .AsNoTracking()
            .Include(x => x.Seller)
            .Include(x => x.Winner)
            .Include(x => x.Images.OrderBy(i => i.SortOrder))
            .Include(x => x.Bids.OrderByDescending(b => b.CreatedAtUtc))
                .ThenInclude(x => x.Bidder)
            .SingleOrDefaultAsync(x => x.Id == auctionId);

        if (auction is null)
        {
            return;
        }

        await Clients.Caller.SendAsync("auctionSnapshot", AuctionMappings.ToDetailDto(auction));
    }
}

public static class GroupNames
{
    public static string Auction(Guid auctionId) => $"auction:{auctionId}";
    public static string User(Guid userId) => $"user:{userId}";
}
