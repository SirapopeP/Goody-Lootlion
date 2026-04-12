using Microsoft.AspNetCore.Mvc;

namespace Lootlion.Api.Http;

public static class ControllerBaseExtensions
{
    /// <summary>UserId จาก JWT — โยน <see cref="InvalidOperationException"/> ถ้าไม่มี claim</summary>
    public static Guid GetCurrentUserId(this ControllerBase controller) =>
        controller.User.GetUserId();
}
