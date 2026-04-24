using System;

namespace Polar.HabboHotel.Users.UserDataManagement
{
    public class UserDataNotFoundException : Exception
    {
        public UserDataNotFoundException(string reason) : base(reason) { }
    }
}
