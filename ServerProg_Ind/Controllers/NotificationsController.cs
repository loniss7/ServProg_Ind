using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerProg_Ind.Application;
using ServerProg_Ind.Contracts;

namespace ServerProg_Ind.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController(AuctionService auctionService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> GetNotifications(CancellationToken cancellationToken)
    {
        var userId = UserClaims.GetRequiredUserId(User);
        return Ok(await auctionService.GetNotificationsAsync(userId, cancellationToken));
    }

    [HttpPost("{notificationId:guid}/read")]
    public async Task<ActionResult<NotificationDto>> MarkNotification(Guid notificationId, MarkNotificationReadRequest request, CancellationToken cancellationToken)
    {
        var userId = UserClaims.GetRequiredUserId(User);
        return Ok(await auctionService.MarkNotificationAsync(notificationId, userId, request.IsRead, cancellationToken));
    }
}
