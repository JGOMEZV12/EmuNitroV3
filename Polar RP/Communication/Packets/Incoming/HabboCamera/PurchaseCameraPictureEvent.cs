using Polar.Communication.Packets.Outgoing.HabboCamera;
using Polar.Database.Interfaces;
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
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            if (Session?.GetHabbo() == null) return;

            // ✅ FIX #1: lastPhotoPreview no tenía null-check.
            //   Un paquete manipulado (comprar sin haber tomado foto) causaba NullReferenceException.
            JSONCamera jsonInfo = Session.GetHabbo().lastPhotoPreview;
            if (jsonInfo == null)
            {
                Session.SendNotification("¡Debes tomar una foto antes de poder comprarla!");
                return;
            }

            // ✅ FIX #2: Int32.Parse sin manejo de error.
            //   Si el config tiene un valor no numérico el servidor lanzaba excepción en startup.
            string imagen2Str = PolarEnvironment.GetConfig().data["Camera_img_2"];
            if (!int.TryParse(imagen2Str, out int imagenint2))
            {
                //Logging.LogException("PurchaseCameraPictureEvent: Camera_img_2 no es un entero válido: " + imagen2Str);
                return;
            }

            ItemData ItemDataSmall;
            if (!PolarEnvironment.GetGame().GetItemManager().GetItem(imagenint2, out ItemDataSmall))
                return;

            // ✅ FIX #3: CurrentRoom puede ser null si el usuario salió de la sala.
            string roomName = Session.GetHabbo().CurrentRoom?.Name ?? "una sala";

            string roomId = jsonInfo.room_id;
            double timestamp = jsonInfo.timestamp;
            string md5image = jsonInfo.encrypted_id;
            string username = Session.GetHabbo().Username;

            // ✅ FIX #4: ExtraData se construía con concatenación directa de valores de usuario.
            //   Si el username contenía " o \ el JSON quedaba malformado.
            //   Ahora se serializa con Newtonsoft para garantizar escape correcto.
            string photoUrl = CameraHelper.BASE_URL + "photos/" + md5image + ".png";
            string ExtraData = JsonConvert.SerializeObject(new
            {
                w = photoUrl,
                n = username,
                s = Session.GetHabbo().Id.ToString(),
                u = "0",
                t = timestamp.ToString()
            });

            PolarEnvironment.SendMs2(
                "",
                RoleplayManager.CDNSWF + "/newfoto/" + jsonInfo.preview,
                "¡Mira está foto que ha tomada por " + username + "!",
                "En " + roomName,
                Session.GetHabbo().Look,
                true);

            Session.GetHabbo().GetInventoryComponent().AddNewItem(0, ItemDataSmall.Id, ExtraData, 0, true, false, 0, 0);
            Session.GetHabbo().GetInventoryComponent().UpdateItems(false);
            Session.SendMessage(new CamereFinishPurchaseComposer());
        }
    }
}