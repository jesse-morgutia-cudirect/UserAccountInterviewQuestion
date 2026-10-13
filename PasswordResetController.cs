using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Interviews.UserAccounts
{
    [ApiController]
    [Route("api/account/password-reset")]
    public class PasswordResetController : ControllerBase
    {
        private readonly UserAccountService _accountService;

        public PasswordResetController(UserAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpPost]
        public async Task<IActionResult> RequestPasswordReset(PasswordResetRequest request)
        {
            try
            {
                await _accountService.RequestPasswordResetAsync(request.Email);
                return NoContent();
            }
            catch (UserNotFoundException)
            {
                return NotFound();
            }
        }
    }
}
