using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Interviews.UserAccounts
{
    [ApiController]
    [Route("api/account/register")]
    public class RegistrationController : ControllerBase
    {
        private readonly UserAccountService _accountService;

        public RegistrationController(UserAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpPost]
        public async Task<ActionResult<RegisterResult>> Register(RegisterRequest request)
        {
            var result = await _accountService.RegisterAsync(request.Email, request.Password);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
