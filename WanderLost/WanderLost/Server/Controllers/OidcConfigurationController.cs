using Microsoft.AspNetCore.ApiAuthorization.IdentityServer;
using Microsoft.AspNetCore.Mvc;

namespace WanderLost.Server.Controllers;

public class OidcConfigurationController(IClientRequestParametersProvider _clientRequestParametersProvider) : Controller
{
    public IClientRequestParametersProvider ClientRequestParametersProvider { get; } = _clientRequestParametersProvider;

    [HttpGet("_configuration/{clientId}")]
    public IActionResult GetClientRequestParameters([FromRoute] string clientId)
    {
        var parameters = ClientRequestParametersProvider.GetClientParameters(HttpContext, clientId);
        return Ok(parameters);
    }
}