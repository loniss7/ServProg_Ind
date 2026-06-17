using ServerProg_Ind.Domain;

namespace ServerProg_Ind.Tests;

internal static class UserFactory
{
    public static User Create(string email)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = email.Split('@')[0],
            Handle = email.Split('@')[0],
            PasswordHash = "hash"
        };
    }
}

internal static class AuctionFactory
{
    public static Auction Create(Guid sellerId, decimal startingBid, decimal minimumIncrement, decimal? buyNowPrice = null, DateTime? endTimeUtc = null)
    {
        return new Auction
        {
            Id = Guid.NewGuid(),
            SellerId = sellerId,
            Seller = new User { Id = sellerId, Email = "seller@greens.edu", DisplayName = "seller", Handle = "seller", PasswordHash = "hash" },
            Title = "Auction",
            Description = "Description",
            Category = "Tech",
            Condition = "Good",
            PickupLocation = "Library",
            StartingBid = startingBid,
            MinimumIncrement = minimumIncrement,
            BuyNowPrice = buyNowPrice,
            EndTimeUtc = endTimeUtc ?? DateTime.UtcNow.AddHours(2)
        };
    }
}
