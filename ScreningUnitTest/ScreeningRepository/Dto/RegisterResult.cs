using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreeningService.Dto
{
    public class RegisterResult
    {
        public bool Success { get; set; }
        public string UserId { get; set; }
        public string FailureReason { get; set; }
    }
}
