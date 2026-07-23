using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserAccountService.Interfaces
{
    public interface IEmailService
    {
        Task SendWelcomeEmailAsync(string email);
        Task SendPasswordResetEmailAsync(string email, string resetToken);
    }
}
