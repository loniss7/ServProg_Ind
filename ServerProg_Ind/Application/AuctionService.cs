using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ServerProg_Ind.Contracts;
using ServerProg_Ind.Domain;
using ServerProg_Ind.Infrastructure.Data;

namespace ServerProg_Ind.Application;

public sealed class AuctionService(
    ApplicationDbContext dbContext,
    IWebHostEnvironment environment,
    AuctionBidProcessor bidProcessor,
    AuctionLifecycleProcessor lifecycleProcessor,
    IAuctionNotifier notifier,
    IOptions<AuctionOptions> options,
    ILogger<AuctionService> logger)
{
    private readonly AuctionOptions _options = options.Value;

    public async Task<AuctionListResponse> GetAuctionsAsync(AuctionQuery query, CancellationToken cancellationToken)
    {
        var normalizedPage = Math.Max(1, query.Page.GetValueOrDefault(1));
        var normalizedPageSize = Math.Clamp(query.PageSize.GetValueOrDefault(12), 1, 50);

        var auctions = dbContext.Auctions
            .AsNoTracking()
            .Include(x => x.Images)
            .Where(x => x.Status == AuctionStatus.Active);

        var category = query.Category?.Trim();
        if (!string.IsNullOrWhiteSpace(category))
        {
            auctions = auctions.Where(x => x.Category == category);
        }

        var search = query.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            auctions = auctions.Where(x => x.Title.Contains(search) || x.Description.Contains(search));
        }

        var sort = query.Sort?.Trim();
        if (string.IsNullOrWhiteSpace(sort))
        {
            sort = "ending_soon";
        }

        auctions = sort switch
        {
            "newest" => auctions.OrderByDescending(x => x.CreatedAtUtc),
            "price_asc" => auctions.OrderBy(x => x.CurrentBid ?? x.StartingBid),
            "price_desc" => auctions.OrderByDescending(x => x.CurrentBid ?? x.StartingBid),
            "most_bids" => auctions.OrderByDescending(x => x.BidCount).ThenBy(x => x.EndTimeUtc),
            _ => auctions.OrderBy(x => x.EndTimeUtc)
        };

        var total = await auctions.CountAsync(cancellationToken);
        var items = await auctions
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        return new AuctionListResponse(total, normalizedPage, normalizedPageSize, items.Select(AuctionMappings.ToSummaryDto).ToArray());
    }

    public async Task<AuctionDetailDto> GetAuctionAsync(Guid auctionId, CancellationToken cancellationToken)
    {
        var auction = await LoadAuctionGraphAsync(auctionId, cancellationToken)
            ?? throw new AppException("auction_not_found", "Auction not found.", StatusCodes.Status404NotFound);

        return AuctionMappings.ToDetailDto(auction);
    }

    public async Task<AuctionDetailDto> CreateAuctionAsync(CreateAuctionRequest request, IReadOnlyList<IFormFile> photos, Guid sellerId, CancellationToken cancellationToken)
    {
        var seller = await dbContext.Users.SingleOrDefaultAsync(x => x.Id == sellerId, cancellationToken)
            ?? throw new AppException("seller_not_found", "Seller not found.", StatusCodes.Status404NotFound);

        var nowUtc = DateTime.UtcNow;
        AuctionRules.ValidateAuctionDraft(
            request.Title,
            request.Description,
            request.Category,
            request.Condition,
            request.PickupLocation,
            request.StartingBid,
            request.MinimumIncrement,
            request.BuyNowPrice,
            request.EndTime.ToUniversalTime(),
            nowUtc);

        var auction = new Auction
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category.Trim(),
            Condition = request.Condition.Trim(),
            PickupLocation = request.PickupLocation.Trim(),
            StartingBid = request.StartingBid,
            MinimumIncrement = request.MinimumIncrement,
            BuyNowPrice = request.BuyNowPrice,
            EndTimeUtc = request.EndTime.ToUniversalTime(),
            SellerId = sellerId,
            Seller = seller,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };

        dbContext.Auctions.Add(auction);
        await dbContext.SaveChangesAsync(cancellationToken);

        var storedImages = await SaveImagesAsync(auction.Id, photos, cancellationToken);
        if (storedImages.Count == 0)
        {
            var defaultImage = new AuctionImage
            {
                AuctionId = auction.Id,
                Url = "/green-toad-logo.svg",
                SortOrder = 0
            };
            dbContext.AuctionImages.Add(defaultImage);
            storedImages.Add(defaultImage);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created auction {AuctionId} by seller {SellerId}", auction.Id, sellerId);
        var detail = await GetAuctionAsync(auction.Id, cancellationToken);
        return detail;
    }

    public async Task<AuctionDetailDto> UpdateAuctionAsync(Guid auctionId, UpdateAuctionRequest request, Guid sellerId, CancellationToken cancellationToken)
    {
        var auction = await dbContext.Auctions
            .AsSplitQuery()
            .Include(x => x.Seller)
            .Include(x => x.Winner)
            .Include(x => x.Images)
            .Include(x => x.Bids)
                .ThenInclude(x => x.Bidder)
            .SingleOrDefaultAsync(x => x.Id == auctionId, cancellationToken)
            ?? throw new AppException("auction_not_found", "Auction not found.", StatusCodes.Status404NotFound);

        AuctionRules.EnsureAuctionCanBeUpdated(auction, sellerId);
        AuctionRules.ValidateAuctionDraft(
            request.Title,
            request.Description,
            request.Category,
            request.Condition,
            request.PickupLocation,
            request.StartingBid,
            request.MinimumIncrement,
            request.BuyNowPrice,
            request.EndTime.ToUniversalTime(),
            DateTime.UtcNow);

        auction.Title = request.Title.Trim();
        auction.Description = request.Description.Trim();
        auction.Category = request.Category.Trim();
        auction.Condition = request.Condition.Trim();
        auction.PickupLocation = request.PickupLocation.Trim();
        auction.StartingBid = request.StartingBid;
        auction.MinimumIncrement = request.MinimumIncrement;
        auction.BuyNowPrice = request.BuyNowPrice;
        auction.EndTimeUtc = request.EndTime.ToUniversalTime();
        auction.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Updated auction {AuctionId}", auctionId);

        var detail = AuctionMappings.ToDetailDto(auction);
        await notifier.SendAuctionSnapshotAsync(detail, cancellationToken);
        return detail;
    }

    public async Task CancelAuctionAsync(Guid auctionId, Guid sellerId, CancellationToken cancellationToken)
    {
        var auction = await dbContext.Auctions
            .AsSplitQuery()
            .Include(x => x.Seller)
            .Include(x => x.Winner)
            .Include(x => x.Images)
            .Include(x => x.Bids)
                .ThenInclude(x => x.Bidder)
            .SingleOrDefaultAsync(x => x.Id == auctionId, cancellationToken)
            ?? throw new AppException("auction_not_found", "Auction not found.", StatusCodes.Status404NotFound);

        AuctionRules.CancelAuction(auction, sellerId, DateTime.UtcNow);
        var notification = CreateNotification(auction.SellerId, auction.Id, NotificationType.AuctionCanceled, "Auction canceled", $"Listing \"{auction.Title}\" was canceled by the seller.");
        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Canceled auction {AuctionId}", auctionId);
        await notifier.SendAuctionSnapshotAsync(AuctionMappings.ToDetailDto(auction), cancellationToken);
        await notifier.SendNotificationAsync(notification.UserId, AuctionMappings.ToDto(notification), cancellationToken);
    }

    public async Task<BidPlacedResponse> PlaceBidAsync(Guid auctionId, PlaceBidRequest request, Guid bidderId, CancellationToken cancellationToken)
    {
        var auction = await dbContext.Auctions
            .AsSplitQuery()
            .Include(x => x.Seller)
            .Include(x => x.Winner)
            .Include(x => x.Images)
            .Include(x => x.Bids)
                .ThenInclude(x => x.Bidder)
            .SingleOrDefaultAsync(x => x.Id == auctionId, cancellationToken)
            ?? throw new AppException("auction_not_found", "Auction not found.", StatusCodes.Status404NotFound);

        var bidder = await dbContext.Users.SingleOrDefaultAsync(x => x.Id == bidderId, cancellationToken)
            ?? throw new AppException("bidder_not_found", "Bidder not found.", StatusCodes.Status404NotFound);

        var result = await bidProcessor.PlaceBidAsync(auction, bidder, request.Amount, DateTime.UtcNow, cancellationToken);
        dbContext.Bids.Add(result.Bid);

        Notification? outbidNotification = null;
        if (result.PreviousHighestBidderId is Guid previousHighest && previousHighest != bidder.Id)
        {
            outbidNotification = CreateNotification(
                previousHighest,
                auction.Id,
                NotificationType.Outbid,
                "You were outbid",
                $"A higher bid was placed on \"{auction.Title}\".");
            dbContext.Notifications.Add(outbidNotification);
        }

        var bidderNotification = CreateNotification(
            bidder.Id,
            auction.Id,
            NotificationType.BidAccepted,
            "Bid accepted",
            result.ClosedByBuyNow
                ? $"Your bid closed \"{auction.Title}\" immediately."
                : $"You are now leading on \"{auction.Title}\".");
        dbContext.Notifications.Add(bidderNotification);

        if (result.ClosedByBuyNow)
        {
            dbContext.Notifications.Add(CreateNotification(
                auction.SellerId,
                auction.Id,
                NotificationType.AuctionWon,
                "Auction finished",
                $"\"{auction.Title}\" closed at the buy now price."));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Accepted bid on auction {AuctionId} by bidder {BidderId} for {Amount}", auctionId, bidderId, result.Bid.Amount);

        if (outbidNotification is not null)
        {
            await notifier.SendNotificationAsync(outbidNotification.UserId, AuctionMappings.ToDto(outbidNotification), cancellationToken);
        }

        await notifier.SendNotificationAsync(bidderNotification.UserId, AuctionMappings.ToDto(bidderNotification), cancellationToken);

        var detail = await GetAuctionAsync(auctionId, cancellationToken);
        var response = new BidPlacedResponse(
            auctionId,
            result.Bid.Amount,
            detail.MinimumNextBid,
            detail.BidCount,
            detail.Status,
            result.ClosedByBuyNow);

        await notifier.SendBidAcceptedAsync(response, cancellationToken);
        await notifier.SendAuctionSnapshotAsync(detail, cancellationToken);
        return response;
    }

    public async Task<AuctionDetailDto> ConfirmSaleAsync(Guid auctionId, Guid winnerId, CancellationToken cancellationToken)
    {
        var auction = await dbContext.Auctions
            .AsSplitQuery()
            .Include(x => x.Seller)
            .Include(x => x.Winner)
            .Include(x => x.Images)
            .Include(x => x.Bids)
                .ThenInclude(x => x.Bidder)
            .SingleOrDefaultAsync(x => x.Id == auctionId, cancellationToken)
            ?? throw new AppException("auction_not_found", "Auction not found.", StatusCodes.Status404NotFound);

        AuctionRules.ConfirmSale(auction, winnerId, DateTime.UtcNow);
        var sellerNotification = CreateNotification(
            auction.SellerId,
            auction.Id,
            NotificationType.SaleConfirmed,
            "Sale confirmed",
            $"The winner confirmed the sale for \"{auction.Title}\".");
        dbContext.Notifications.Add(sellerNotification);
        await dbContext.SaveChangesAsync(cancellationToken);

        await notifier.SendNotificationAsync(sellerNotification.UserId, AuctionMappings.ToDto(sellerNotification), cancellationToken);
        var detail = AuctionMappings.ToDetailDto(auction);
        await notifier.SendAuctionSnapshotAsync(detail, cancellationToken);
        return detail;
    }

    public async Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var notifications = await dbContext.Notifications
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(50)
            .ToListAsync(cancellationToken);

        return notifications.Select(AuctionMappings.ToDto).ToArray();
    }

    public async Task<NotificationDto> MarkNotificationAsync(Guid notificationId, Guid userId, bool isRead, CancellationToken cancellationToken)
    {
        var notification = await dbContext.Notifications
            .SingleOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId, cancellationToken)
            ?? throw new AppException("notification_not_found", "Notification not found.", StatusCodes.Status404NotFound);

        notification.IsRead = isRead;
        await dbContext.SaveChangesAsync(cancellationToken);
        return AuctionMappings.ToDto(notification);
    }

    public async Task ProcessScheduledWorkAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        var activeAuctions = await dbContext.Auctions
            .AsSplitQuery()
            .Include(x => x.Seller)
            .Include(x => x.Winner)
            .Include(x => x.Images)
            .Include(x => x.Bids)
                .ThenInclude(x => x.Bidder)
            .Where(x => x.Status == AuctionStatus.Active)
            .ToListAsync(cancellationToken);

        var closingSoonWindow = TimeSpan.FromMinutes(_options.ClosingSoonWindowMinutes);
        var updatedAuctions = new List<Auction>();
        var createdNotifications = new List<Notification>();

        foreach (var auction in activeAuctions)
        {
            if (AuctionRules.ShouldSendClosingSoon(auction, nowUtc, closingSoonWindow))
            {
                auction.ClosingSoonNotified = true;
                updatedAuctions.Add(auction);
                var recipients = auction.Bids.Select(x => x.BidderId)
                    .Append(auction.SellerId)
                    .Distinct()
                    .ToArray();

                foreach (var userId in recipients)
                {
                    var notification = CreateNotification(
                        userId,
                        auction.Id,
                        NotificationType.ClosingSoon,
                        "Auction closing soon",
                        $"\"{auction.Title}\" closes in less than {_options.ClosingSoonWindowMinutes} minutes.");
                    dbContext.Notifications.Add(notification);
                    createdNotifications.Add(notification);
                }
            }

            var finalization = await lifecycleProcessor.FinalizeAuctionAsync(auction, nowUtc, cancellationToken);
            if (!finalization.StateChanged)
            {
                continue;
            }

            updatedAuctions.Add(auction);
            logger.LogInformation("Finalized auction {AuctionId}. Winner: {WinnerId}", auction.Id, finalization.WinnerId);

            if (auction.WinnerId is null)
            {
                var notification = CreateNotification(
                    auction.SellerId,
                    auction.Id,
                    NotificationType.AuctionEndedWithoutBids,
                    "Auction ended without bids",
                    $"No bids were placed on \"{auction.Title}\".");
                dbContext.Notifications.Add(notification);
                createdNotifications.Add(notification);
            }
            else
            {
                var winnerNotification = CreateNotification(
                    auction.WinnerId.Value,
                    auction.Id,
                    NotificationType.AuctionWon,
                    "You won the auction",
                    $"You won \"{auction.Title}\" for {auction.FinalPrice:0.##}.");
                dbContext.Notifications.Add(winnerNotification);
                createdNotifications.Add(winnerNotification);

                var sellerNotification = CreateNotification(
                    auction.SellerId,
                    auction.Id,
                    NotificationType.AuctionWon,
                    "Auction ended",
                    $"\"{auction.Title}\" ended with a winner.");
                dbContext.Notifications.Add(sellerNotification);
                createdNotifications.Add(sellerNotification);

                foreach (var losingBidderId in auction.Bids.Select(x => x.BidderId).Distinct().Where(x => x != auction.WinnerId))
                {
                    var notification = CreateNotification(
                        losingBidderId,
                        auction.Id,
                        NotificationType.AuctionLost,
                        "Auction ended",
                        $"You did not win \"{auction.Title}\".");
                    dbContext.Notifications.Add(notification);
                    createdNotifications.Add(notification);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var auction in updatedAuctions.DistinctBy(x => x.Id))
        {
            var detail = AuctionMappings.ToDetailDto(auction);
            await notifier.SendAuctionSnapshotAsync(detail, cancellationToken);
        }

        foreach (var notification in createdNotifications)
        {
            await notifier.SendNotificationAsync(notification.UserId, AuctionMappings.ToDto(notification), cancellationToken);
        }
    }

    public static async Task<(CreateAuctionRequest Request, IReadOnlyList<IFormFile> Photos)> ParseCreateRequestAsync(HttpRequest httpRequest, CancellationToken cancellationToken)
    {
        if (httpRequest.HasFormContentType)
        {
            var form = await httpRequest.ReadFormAsync(cancellationToken);
            var data = form["data"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(data))
            {
                throw new AppException("invalid_payload", "Form field 'data' is required.");
            }

            var request = JsonSerializer.Deserialize<CreateAuctionRequest>(data, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                          ?? throw new AppException("invalid_payload", "Unable to parse auction payload.");
            return (request, form.Files.ToArray());
        }

        var requestFromJson = await httpRequest.ReadFromJsonAsync<CreateAuctionRequest>(cancellationToken: cancellationToken)
                              ?? throw new AppException("invalid_payload", "Request body is required.");
        return (requestFromJson, Array.Empty<IFormFile>());
    }

    private async Task<List<AuctionImage>> SaveImagesAsync(Guid auctionId, IReadOnlyList<IFormFile> photos, CancellationToken cancellationToken)
    {
        var images = new List<AuctionImage>();
        if (photos.Count == 0)
        {
            return images;
        }

        var root = Path.Combine(environment.ContentRootPath, "uploads", "auctions", auctionId.ToString("N"));
        Directory.CreateDirectory(root);

        var index = 0;
        foreach (var photo in photos.Where(x => x.Length > 0))
        {
            var extension = Path.GetExtension(photo.FileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".bin";
            }

            var fileName = $"{index:00}{extension}";
            var absolutePath = Path.Combine(root, fileName);
            await using var stream = File.Create(absolutePath);
            await photo.CopyToAsync(stream, cancellationToken);

            images.Add(new AuctionImage
            {
                AuctionId = auctionId,
                Url = $"/uploads/auctions/{auctionId:N}/{fileName}",
                SortOrder = index
            });

            index += 1;
        }

        dbContext.AuctionImages.AddRange(images);
        return images;
    }

    private Notification CreateNotification(Guid userId, Guid? auctionId, NotificationType type, string title, string message)
    {
        return new Notification
        {
            UserId = userId,
            AuctionId = auctionId,
            Type = type,
            Title = title,
            Message = message,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private Task<Auction?> LoadAuctionGraphAsync(Guid auctionId, CancellationToken cancellationToken)
    {
        return dbContext.Auctions
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Seller)
            .Include(x => x.Winner)
            .Include(x => x.Images)
            .Include(x => x.Bids.OrderByDescending(b => b.CreatedAtUtc))
                .ThenInclude(x => x.Bidder)
            .SingleOrDefaultAsync(x => x.Id == auctionId, cancellationToken);
    }
}
