using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserAccountService.Interfaces
{
    public interface IPasswordHasher
    {
        string Hash(string plainText);
        bool Verify(string plainText, string hash);
    }
}
