using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Interviews.UserAccounts
{
    [ApiController]
    [Route("api/account/login")]
    public class LoginController : ControllerBase
    {
        private readonly UserAccountService _accountService;

        public LoginController(UserAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpPost]
        public async Task<ActionResult<LoginResult>> Login(LoginRequest request)
        {
            var result = await _accountService.LoginAsync(request.Email, request.Password);
            return result.Success ? Ok(result) : Unauthorized(result);
        }
    }
}
