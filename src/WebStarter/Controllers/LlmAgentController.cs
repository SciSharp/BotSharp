using Microsoft.AspNetCore.Mvc;

namespace WebStarter.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LlmAgentController : ControllerBase
    {
        private readonly ILogger<LlmAgentController> _logger;

        public LlmAgentController(ILogger<LlmAgentController> logger)
        {
            _logger = logger;
        }
    }
}