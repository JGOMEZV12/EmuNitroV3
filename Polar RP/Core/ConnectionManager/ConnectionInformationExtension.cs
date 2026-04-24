// ConnectionInformationExtensions.cs
using ConnectionManager;

namespace Polar.HabboHotel.Roleplay.Web
{
    public static class ConnectionInformationExtensions
    {
        public static void SendWS(this ConnectionInformation socket, string message)
        {
            if (socket == null || string.IsNullOrEmpty(message)) return;
            socket.SendData(System.Text.Encoding.UTF8.GetBytes(message));
        }
    }
}