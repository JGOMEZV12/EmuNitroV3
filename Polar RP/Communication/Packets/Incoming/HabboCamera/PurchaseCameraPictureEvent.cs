using Polar.Communication.Packets.Outgoing.HabboCamera;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Camera;
using Polar.HabboRoleplay.Misc;
using Newtonsoft.Json;
using System;

namespace Polar.Communication.Packets.Incoming.HabboCamera
{
    class PurchaseCameraPictureEvent : IPacketEvent
    {
        private const string PHOTO_BASE_URL = "https://swf.kekolands.com/newfoto/";

        public void Parse(GameClient Session, ClientPacket Packet)
        {
            Console.WriteLine("[Camera:Purchase] Paquete recibido");

            if (Session?.GetHabbo() == null)
            {
                Console.WriteLine("[Camera:Purchase] Session o Habbo es null — saliendo");
                return;
            }

            JSONCamera jsonInfo = Session.GetHabbo().lastPhotoPreview;
            if (jsonInfo == null)
            {
                Console.WriteLine("[Camera:Purchase] lastPhotoPreview es null — el usuario no ha tomado foto");
                Session.SendNotification("¡Debes tomar una foto antes de poder comprarla!");
                return;
            }

            Console.WriteLine($"[Camera:Purchase] jsonInfo OK — encrypted_id={jsonInfo.encrypted_id} preview={jsonInfo.preview}");

            string imagen2Str = PolarEnvironment.GetConfig().data["Camera_img_2"];
            Console.WriteLine($"[Camera:Purchase] Camera_img_2='{imagen2Str}'");

            if (!int.TryParse(imagen2Str, out int imagenint2))
            {
                Console.WriteLine("[Camera:Purchase] Camera_img_2 no es un entero válido — saliendo");
                return;
            }

            ItemData ItemDataSmall;
            if (!PolarEnvironment.GetGame().GetItemManager().GetItem(imagenint2, out ItemDataSmall))
            {
                Console.WriteLine($"[Camera:Purchase] ItemData no encontrado para id={imagenint2} — saliendo");
                return;
            }

            Console.WriteLine($"[Camera:Purchase] ItemData OK — id={ItemDataSmall.Id}");

            string roomName = Session.GetHabbo().CurrentRoom?.Name ?? "una sala";
            string roomId = jsonInfo.room_id;
            double timestamp = jsonInfo.timestamp;
            string md5image = jsonInfo.encrypted_id;
            string username = Session.GetHabbo().Username;

            string photoUrl = PHOTO_BASE_URL + "photos/" + md5image + ".png";
            Console.WriteLine($"[Camera:Purchase] photoUrl={photoUrl}");

            string ExtraData = JsonConvert.SerializeObject(new
            {
                w = photoUrl,
                n = username,
                s = Session.GetHabbo().Id.ToString(),
                u = "0",
                t = timestamp.ToString()
            });

            Console.WriteLine($"[Camera:Purchase] ExtraData={ExtraData}");

            try
            {
                Session.GetHabbo().GetInventoryComponent().AddNewItem(0, ItemDataSmall.Id, ExtraData, 0, true, false, 0, 0);
                Console.WriteLine("[Camera:Purchase] AddNewItem OK");
                Session.GetHabbo().GetInventoryComponent().UpdateItems(false);
                Console.WriteLine("[Camera:Purchase] UpdateItems OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Camera:Purchase] Error al añadir item: {ex.Message}");
            }

            try
            {
                Session.SendMessage(new CamereFinishPurchaseComposer());
                Console.WriteLine("[Camera:Purchase] CamereFinishPurchaseComposer enviado — DONE");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Camera:Purchase] Error al enviar composer: {ex.Message}");
            }
        }
    }
}