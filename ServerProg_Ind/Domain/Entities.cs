using System.ComponentModel.DataAnnotations;

namespace ServerProg_Ind.Domain;

public enum AuctionStatus
{
    Active = 0,
    Ended = 1,
    Sold = 2,
    Canceled = 3
}

public enum NotificationType
{
    BidAccepted = 0,
    Outbid = 1,
    ClosingSoon = 2,
    AuctionWon = 3,
    AuctionLost = 4,
    AuctionEndedWithoutBids = 5,
    AuctionCanceled = 6,
    AuctionUpdated = 7,
    SaleConfirmed = 8
}

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(120)]
    public string DisplayName { get; set; } = string.Empty;
    [MaxLength(160)]
    public string Email { get; set; } = string.Empty;
    [MaxLength(120)]
    public string Handle { get; set; } = string.Empty;
    [MaxLength(400)]
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Auction> Auctions { get; set; } = new List<Auction>();
    public ICollection<Bid> Bids { get; set; } = new List<Bid>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}

public sealed class Auction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(160)]
    public string Title { get; set; } = string.Empty;
    [MaxLength(4000)]
    public string Description { get; set; } = string.Empty;
    [MaxLength(40)]
    public string Category { get; set; } = string.Empty;
    [MaxLength(40)]
    public string Condition { get; set; } = string.Empty;
    [MaxLength(240)]
    public string PickupLocation { get; set; } = string.Empty;
    public decimal StartingBid { get; set; }
    public decimal MinimumIncrement { get; set; }
    public decimal? BuyNowPrice { get; set; }
    public decimal? CurrentBid { get; set; }
    public int BidCount { get; set; }
    public AuctionStatus Status { get; set; } = AuctionStatus.Active;
    public DateTime EndTimeUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAtUtc { get; set; }
    public DateTime? SoldAtUtc { get; set; }
    public DateTime? CanceledAtUtc { get; set; }
    public bool ClosingSoonNotified { get; set; }

    public Guid SellerId { get; set; }
    public User Seller { get; set; } = null!;
    public Guid? HighestBidderId { get; set; }
    public Guid? WinnerId { get; set; }
    public User? Winner { get; set; }
    public decimal? FinalPrice { get; set; }

    public ICollection<Bid> Bids { get; set; } = new List<Bid>();
    public ICollection<AuctionImage> Images { get; set; } = new List<AuctionImage>();
}

public sealed class Bid
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AuctionId { get; set; }
    public Auction Auction { get; set; } = null!;
    public Guid BidderId { get; set; }
    public User Bidder { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class AuctionImage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AuctionId { get; set; }
    public Auction Auction { get; set; } = null!;
    [MaxLength(400)]
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public sealed class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid? AuctionId { get; set; }
    public NotificationType Type { get; set; }
    [MaxLength(120)]
    public string Title { get; set; } = string.Empty;
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
