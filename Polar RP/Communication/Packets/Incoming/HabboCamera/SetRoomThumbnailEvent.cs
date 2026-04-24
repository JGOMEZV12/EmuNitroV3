using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets;
using Polar.Core;
using Polar.HabboHotel.Camera;
using Polar.HabboHotel.GameClients;
using System;
using System.Threading.Tasks;
using Polar.Communication.Packets.Outgoing.HabboCamera;

namespace Polar.Communication.Packets.Incoming.HabboCamera
{
    class SetRoomThumbnailEvent : IPacketEvent
    {
        // ✅ FIX: Parse ahora es async para no bloquear el hilo mientras espera el servidor de cámara.
        public async void Parse(GameClient Session, ClientPacket Packet)
        {
            if (Session?.GetHabbo() == null || !Session.GetHabbo().InRoom)
                return;

            // ✅ FIX: CurrentRoom puede ser null — verificar antes de usarlo.
            if (Session.GetHabbo().CurrentRoom == null)
                return;

            int count = Packet.PopInt();
            byte[] data = Packet.ReadBytes(count);

            try
            {
                string base64 = Convert.ToBase64String(data);

                if (!base64.Substring(0, 5).ToUpper().Equals("IVBOR"))
                {
                    Logging.WriteLine("Someone tried take a picture with an invalid mime type! (Username: " + Session.GetHabbo().Username + ")");
                    Session.SendMessage(new SendRoomThumbnailAlertComposer());
                    return;
                }

                // ✅ FIX: Llamada async — ya no bloquea el hilo del handler.
                string result = await CameraHelper.RequestAsync("thumbnail", Session.GetHabbo().Id, Session.GetHabbo().CurrentRoom.Id, base64);

                // ✅ FIX: Validar respuesta antes de deserializar para evitar excepción con HTML de error.
                if (string.IsNullOrWhiteSpace(result))
                {
                    Session.SendNotification("It happened some error while trying to save this thumbnail! Try again!");
                    Session.SendMessage(new SendRoomThumbnailAlertComposer());
                    return;
                }

                JSONCamera jsonCamera = JsonConvert.DeserializeObject<JSONCamera>(result);
                if (jsonCamera == null || !jsonCamera.status)
                {
                    Session.SendNotification("It happened some error while trying to save this thumbnail! Try again!");
                    Session.SendMessage(new SendRoomThumbnailAlertComposer());
                    return;
                }

                Session.SendMessage(new SendRoomThumbnailAlertComposer());
            }
            catch (Exception ex)
            {
                Logging.LogException(ex.ToString());
                Session.SendMessage(new SendRoomThumbnailAlertComposer());
            }
        }
    }
}