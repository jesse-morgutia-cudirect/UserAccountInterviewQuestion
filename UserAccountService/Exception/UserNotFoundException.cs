using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace UserAccountService.Exception
{
    public class UserNotFoundException : System.Exception
    {
        public UserNotFoundException(string m) : base(m) { }
    }
}
