using ConnectionManager;
using Polar.HabboHotel.GameClients;

// ─────────────────────────────────────────────────────────────────────────────
//  IWebEvent — interfaz de todos los handlers de eventos web
//
//  CAMBIO: el parámetro "socket" pasa de ser Fleck.IWebSocketConnection
//          a ser ConnectionManager.ConnectionInformation.
//
//  Todas las clases que implementan esta interfaz deben actualizarse:
//    • Antes:  public void Execute(GameClient client, string data, IWebSocketConnection socket)
//    • Ahora:  public void Execute(GameClient client, string data, ConnectionInformation socket)
//
//  Para enviar datos desde un IWebEvent:
//    • Antes:  socket.Send(json);
//    • Ahora:  var raw = System.Text.Encoding.UTF8.GetBytes(json);
//              socket.SendData(raw);
//          o bien llamar a WebEventManager.SendDataDirectFast(socket, json)
// ─────────────────────────────────────────────────────────────────────────────

namespace Polar.HabboHotel.Roleplay.Web
{
    public interface IWebEvent
    {
        void Execute(GameClient client, string data, ConnectionInformation socket);
    }
}