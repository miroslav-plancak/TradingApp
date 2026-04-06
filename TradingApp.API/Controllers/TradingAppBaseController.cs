using Microsoft.AspNetCore.Mvc;
using TradingApp.Business.Interfaces.Logger;

namespace TradingApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class TradingAppBaseController<T> : ControllerBase
    {
        protected readonly ITradingAppLogger _logger;

        protected TradingAppBaseController(ITradingAppLogger logger)
        {
            _logger = logger;
            var controllerName = typeof(T).Name;

        }
    }
}
