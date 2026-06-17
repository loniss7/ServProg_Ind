using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerProg_Ind.Application;
using ServerProg_Ind.Contracts;

namespace ServerProg_Ind.Controllers;

[ApiController]
[Route("api/auctions")]
public sealed class AuctionsController(AuctionService auctionService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AuctionListResponse>> GetAuctions([FromQuery] AuctionQuery query, CancellationToken cancellationToken)
    {
        return Ok(await auctionService.GetAuctionsAsync(query, cancellationToken));
    }

    [HttpGet("{auctionId:guid}")]
    public async Task<ActionResult<AuctionDetailDto>> GetAuction(Guid auctionId, CancellationToken cancellationToken)
    {
        return Ok(await auctionService.GetAuctionAsync(auctionId, cancellationToken));
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<AuctionDetailDto>> CreateAuction(CancellationToken cancellationToken)
    {
        var userId = UserClaims.GetRequiredUserId(User);
        var (request, photos) = await AuctionService.ParseCreateRequestAsync(Request, cancellationToken);
        var createdAuction = await auctionService.CreateAuctionAsync(request, photos, userId, cancellationToken);
        return Created("/api/auctions", createdAuction);
    }

    [Authorize]
    [HttpPut("{auctionId:guid}")]
    public async Task<ActionResult<AuctionDetailDto>> UpdateAuction(Guid auctionId, UpdateAuctionRequest request, CancellationToken cancellationToken)
    {
        var userId = UserClaims.GetRequiredUserId(User);
        return Ok(await auctionService.UpdateAuctionAsync(auctionId, request, userId, cancellationToken));
    }

    [Authorize]
    [HttpPost("{auctionId:guid}/cancel")]
    public async Task<IActionResult> CancelAuction(Guid auctionId, CancellationToken cancellationToken)
    {
        var userId = UserClaims.GetRequiredUserId(User);
        await auctionService.CancelAuctionAsync(auctionId, userId, cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpPost("{auctionId:guid}/bids")]
    public async Task<ActionResult<BidPlacedResponse>> PlaceBid(Guid auctionId, PlaceBidRequest request, CancellationToken cancellationToken)
    {
        var userId = UserClaims.GetRequiredUserId(User);
        return Ok(await auctionService.PlaceBidAsync(auctionId, request, userId, cancellationToken));
    }

    [Authorize]
    [HttpPost("{auctionId:guid}/confirm-sale")]
    public async Task<ActionResult<AuctionDetailDto>> ConfirmSale(Guid auctionId, CancellationToken cancellationToken)
    {
        var userId = UserClaims.GetRequiredUserId(User);
        return Ok(await auctionService.ConfirmSaleAsync(auctionId, userId, cancellationToken));
    }
}
