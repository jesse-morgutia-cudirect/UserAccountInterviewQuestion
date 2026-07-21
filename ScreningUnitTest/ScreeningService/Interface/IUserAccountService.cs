using ScreeningService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreeningService.Interface
{
    public interface IUserAccountService
    {
        Task<RegisterResult> RegisterAsync(string email, string password);
        Task<LoginResult> LoginAsync(string email, string password);
        Task RequestPasswordResetAsync(string email);

    }
}
