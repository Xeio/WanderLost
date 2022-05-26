using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace WanderLost.Server.Controllers;

public class DynamicClientFilesController : Controller
{
    private readonly IConfiguration _configuration;

    public DynamicClientFilesController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [ResponseCache(Duration = 120)]
    [Route("/appsettings.json")]
    public IActionResult AppSettings()
    {
        var socketEndpint = _configuration["SocketEndpoint"];
        if (string.IsNullOrWhiteSpace(socketEndpint))
        {
            //If not configured, assume socket is at same base URL as host
            socketEndpint = new Uri(new Uri(new Uri(Request.GetEncodedUrl()).GetLeftPart(UriPartial.Authority)), MerchantHub.Path).ToString();
        }
        return Json(new
        {
            SocketEndpoint = socketEndpint,
            ClientVersion = _configuration["ClientVersion"]
        });
    }

    [ResponseCache(Duration = 120)]
    [Route("/js/FirebaseConfig.js")]
    public IActionResult FirebaseConfig()
    {
        var stream = System.IO.File.OpenRead(_configuration["FirebaseClientConfig"]);
        return File(stream, "application/javascript");
    }
}
