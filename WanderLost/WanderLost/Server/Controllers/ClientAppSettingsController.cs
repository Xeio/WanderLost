using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace WanderLost.Server.Controllers
{
    public class ClientAppSettingsController : Controller
    {
        private readonly IConfiguration _configuration;

        public ClientAppSettingsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [Route("/appsettings.json")]
        public IActionResult Index()
        {
            var socketEndpint = _configuration["SocketEndpoint"];
            if (string.IsNullOrWhiteSpace(socketEndpint))
            {
                //If not configured, assume socket is at same base URL as host
                socketEndpint = new Uri(new Uri(new Uri(Request.GetEncodedUrl()).GetLeftPart(UriPartial.Authority)), MerchantHub.Path).ToString();
            }
            return Json(new
            {
                SocketEndpoint = socketEndpint
            });
        }
    }
}
