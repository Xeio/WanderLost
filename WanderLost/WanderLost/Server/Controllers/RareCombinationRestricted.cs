using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using WanderLost.Shared.Data;

namespace WanderLost.Server.Controllers
{
    public class RareCombinationRestricted : AuthorizationHandler<RareCombinationRestricted, HubInvocationContext>, IAuthorizationRequirement
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
            RareCombinationRestricted requirement,
            HubInvocationContext resource)
        {
            if (resource.HubMethodArguments.OfType<ActiveMerchant>().FirstOrDefault() is ActiveMerchant merchant &&
                merchant.IsRareCombination)
            {
                if (context.User.Identity?.IsAuthenticated == true)
                {
                    context.Succeed(requirement);
                }
            }
            else
            {
                //Not a rare combination, so anyone can submit
                context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }
    }
}
