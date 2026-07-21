

namespace ScreeningService.Service
{
    public class UserNotFoundException : Exception
    {
        public UserNotFoundException(string m) : base(m) { }
    }
}
