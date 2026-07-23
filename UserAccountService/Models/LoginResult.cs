using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserAccountService.Models
{
    public class LoginResult
    {
        public bool Success { get; set; }
        public string FailureReason { get; set; }
    }
}
