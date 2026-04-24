using Polar.Core;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Camera;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Polar.Communication.Packets.Outgoing.HabboCamera;
using Polar.HabboRoleplay.Misc;

namespace Polar.Communication.Packets.Incoming.HabboCamera
{
    public class HabboCameraPictureDataEvent : IPacketEvent
    {
        // ✅ FIX: Parse ahora es async para no bloquear el hilo mientras espera el servidor de cámara.
        public async void Parse(GameClient Session, ClientPacket Packet)
        {
            // ✅ FIX: CurrentRoom puede ser null si el usuario salió de la sala entre el clic y el paquete.
            if (Session?.GetHabbo() == null || Session.GetHabbo().CurrentRoom == null)
                return;

            int count = Packet.PopInt();
            byte[] data = Packet.ReadBytes(count);

            try
            {
                string base64 = Convert.ToBase64String(data);

                if (!base64.Substring(0, 5).ToUpper().Equals("IVBOR"))
                {
                    Console.WriteLine("¡Alguien intentó tomar una fotografía con un tipo mime no válido! (Username: " + Session.GetHabbo().Username + ")");
                    return;
                }

                // ✅ FIX: Llamada async — ya no bloquea el hilo del handler.
                string result = await CameraHelper.RequestAsync("camera", Session.GetHabbo().Id, Session.GetHabbo().CurrentRoom.Id, base64);

                // ✅ FIX: Validar que la respuesta no esté vacía antes de deserializar.
                //   Si el servidor devuelve HTML de error, DeserializeObject lanzaría excepción.
                if (string.IsNullOrWhiteSpace(result))
                {
                    Session.SendNotification("¡Se produjo un error al intentar guardar esta imagen! ¡Inténtalo de nuevo!");
                    return;
                }

                JSONCamera jsonCamera = JsonConvert.DeserializeObject<JSONCamera>(result);
                if (jsonCamera == null || !jsonCamera.status)
                {
                    Session.SendNotification("¡Se produjo un error al intentar guardar esta imagen! ¡Inténtalo de nuevo!");
                    return;
                }

                Session.GetHabbo().lastPhotoPreview = jsonCamera;
                Session.SendMessage(new CameraSendImageUrlComposer(jsonCamera.preview));
            }
            catch (Exception ex)
            {
                Logging.LogException(ex.ToString());
                Session.SendNotification("¡Se produjo un error al intentar guardar esta imagen! ¡Inténtalo de nuevo!");
            }
        }
    }
}