using System.Security.Claims;
using English.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace English.Authorization;

public sealed class ActiveAccountHandler(ApplicationDbContext context)
    : AuthorizationHandler<ActiveAccountRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext authorizationContext,
        ActiveAccountRequirement requirement)
    {
        if (authorizationContext.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var userIdValue = authorizationContext.User
            .FindFirst(ClaimTypes.NameIdentifier)?
            .Value;

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return;
        }

        var isActive = await context.AspNetUsers
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId && user.IsActive);

        if (isActive)
        {
            authorizationContext.Succeed(requirement);
        }
    }
}
