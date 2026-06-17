using System.Text.Json.Serialization;

namespace ServerProg_Ind.Contracts;

public sealed record RegisterRequest(string DisplayName, string Email, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record AuthResponse(string Token, DateTime ExpiresAtUtc, UserProfileDto User);

public sealed record UserProfileDto(Guid Id, string DisplayName, string Email, string Handle);

public sealed record ApiError(string Code, string Message);

public sealed record AuctionSummaryDto(
    Guid Id,
    string Title,
    string Category,
    string Condition,
    string CoverImageUrl,
    decimal CurrentBid,
    decimal StartingBid,
    decimal? BuyNowPrice,
    int BidCount,
    DateTime EndTime,
    string Status);

public sealed record AuctionListResponse(int Total, int Page, int PageSize, IReadOnlyList<AuctionSummaryDto> Items);

public sealed record AuctionImageDto(Guid Id, string Url, int SortOrder);

public sealed record BidHistoryDto(Guid Id, decimal Amount, DateTime CreatedAtUtc, UserProfileDto Bidder);

public sealed record AuctionDetailDto(
    Guid Id,
    string Title,
    string Description,
    string Category,
    string Condition,
    string PickupLocation,
    decimal StartingBid,
    decimal MinimumIncrement,
    decimal? BuyNowPrice,
    decimal? CurrentBid,
    decimal MinimumNextBid,
    int BidCount,
    DateTime EndTime,
    string Status,
    DateTime CreatedAtUtc,
    UserProfileDto Seller,
    UserProfileDto? Winner,
    decimal? FinalPrice,
    IReadOnlyList<AuctionImageDto> Images,
    IReadOnlyList<BidHistoryDto> Bids);

public sealed record CreateAuctionRequest(
    string Title,
    string Description,
    string Category,
    string Condition,
    decimal StartingBid,
    decimal MinimumIncrement,
    decimal? BuyNowPrice,
    DateTime EndTime,
    string PickupLocation);

public sealed record UpdateAuctionRequest(
    string Title,
    string Description,
    string Category,
    string Condition,
    decimal StartingBid,
    decimal MinimumIncrement,
    decimal? BuyNowPrice,
    DateTime EndTime,
    string PickupLocation);

public sealed record PlaceBidRequest(decimal Amount);

public sealed record BidPlacedResponse(
    Guid AuctionId,
    decimal AcceptedAmount,
    decimal MinimumNextBid,
    int BidCount,
    string Status,
    bool ClosedByBuyNow);

public sealed record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Message,
    Guid? AuctionId,
    bool IsRead,
    DateTime CreatedAtUtc);

public sealed record MarkNotificationReadRequest(bool IsRead = true);

public sealed class AuctionQuery
{
    public string Sort { get; init; } = "ending_soon";
    public string Category { get; init; } = string.Empty;
    public string Search { get; init; } = string.Empty;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 12;
}

public sealed class CreateAuctionForm
{
    public string Data { get; init; } = string.Empty;
    [JsonIgnore]
    public IReadOnlyList<IFormFile> Photos { get; init; } = Array.Empty<IFormFile>();
}
