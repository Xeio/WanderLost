using Microsoft.AspNetCore.Authorization;
using System.Net;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace WanderLost.Server.Authorization;

/// <summary>
/// Prohibits access to metrics unless from docker subnet or localhost
/// </summary>
public class DockerSubnetOnly : AuthorizationHandler<DockerSubnetOnly, HttpContext>, IAuthorizationRequirement
{
    public static readonly IList<IPNetwork> DockerSubnets =
    [
        new(IPAddress.Parse("127.16.0.0"), 12), //Docker subnets
        new(IPAddress.Parse("::ffff:172.16.0.0"), 108), //Ipv6 version of the above
        //There are other possible docker subnets though the above will be used by default first
    ];

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
    DockerSubnetOnly requirement,
    HttpContext resource)
    {
        if (resource.Connection.RemoteIpAddress is not null) 
        {
            if (IPAddress.IsLoopback(resource.Connection.RemoteIpAddress))
            {
                context.Succeed(requirement);
            }
            else if (DockerSubnets.Any(s => s.Contains(resource.Connection.RemoteIpAddress)))
            {
                context.Succeed(requirement);
            }
        }
        return Task.CompletedTask;
    }
}