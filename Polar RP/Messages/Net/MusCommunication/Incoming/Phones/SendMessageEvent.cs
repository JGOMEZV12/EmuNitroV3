using Polar.Communication.Packets.Outgoing.Moderation;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.HabboHotel.GameClients;
using Polar.Messages.Net.MusCommunication.Outgoing.Phones;
using System.Threading.Tasks;

namespace Polar.Messages.Net.MusCommunication.Incoming.Phones
{
    class SendMessageEvent : IMusPacketEvent
    {
        public async Task Parse(MusConnection MUS, MusPacketEvent Packet)
        {
            string[] D = Packet.PacketData.Split('|');

            GameClient Client = null;
            if (PolarEnvironment.GetGame() != null && PolarEnvironment.GetGame().GetClientManager() != null)
                Client = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Convert.ToInt32(D[1]));

            if (Client != null)
            {
                /*if (Client.GetRoleplay().TryGetCooldown("msg", true))
                {
                    Client.SendWhisper("Por favor espera un poco para hacer eso nuevamente.", 1);
                    return;
                }*/

                if (Client.GetRoleplay().Phone <= 0)
                    return;

                if (!Client.GetRoleplay().OwnedPhonesApps.ContainsKey(Convert.ToInt32(D[0])))
                    return;

                if (Client.GetHabbo().TimeMuted > 0)
                {
                    Client.SendMessage(new MutedComposer(Client.GetHabbo().TimeMuted));
                    return;
                }

                if (!Client.GetHabbo().GetPermissions().HasRight("room_ignore_mute") && Client.GetHabbo().CurrentRoom.CheckMute(Client))
                {
                    Client.SendWhisper("No puedes escribir, usted ha sido muteado por favor espere...", 1);
                    return;
                }

                string word;
                if (!D[3].Contains("engine/uploads") && !D[3].Contains("giphy") && !Client.GetHabbo().GetPermissions().HasRight("advertisement_filter_override") &&
                     PolarEnvironment.GetGame().GetChatManager().GetFilter().IsUnnaceptableWord(D[3], out word))
                {
                    Client.GetHabbo().BannedPhraseCount++;
                    if (Client.GetHabbo().BannedPhraseCount >= 1)
                    {

                        //User.MoveTo(Room.GetGameMap().Model.DoorX, Room.GetGameMap().Model.DoorY);
                        Client.GetHabbo().TimeMuted = 25;
                        Client.SendNotification("¡Has sido silenciad@ mientras un moderador revisa tu caso, al parecer nombraste un hotel! <b>Aviso: " + Client.GetHabbo().BannedPhraseCount + "/5</b>");
                        PolarEnvironment.GetGame().GetClientManager().StaffAlert(new RoomNotificationComposer("Alerta de publicista:",
                            "Atención, se ha mencionado la palabra <b>" + word.ToUpper() + "</b> en la frase <i>" + D[3] +
                            "</i> dentro de una sala\r\n" + "- Este usuario: <b>" +
                            Client.GetHabbo().Username + "</b>", "filter", "Ir a la Sala", "event:navigator/goto/" +
                            Client.GetHabbo().CurrentRoomId));
                    }
                    if (Client.GetHabbo().BannedPhraseCount >= 5)
                    {
                        PolarEnvironment.GetGame().GetModerationManager().BanUser("System", HabboHotel.Moderation.ModerationBanType.USERNAME, Client.GetHabbo().Username, "Baneado por hacer Spam con la Frase (" + D[3] + ")", (PolarEnvironment.GetUnixTimestamp() + 78892200));
                        Client.Disconnect(true);
                        return;
                    }

                    //Client.SendMessage(new ChatComposer(Client.GetHabbo().CurrentRoom.GetRoomUser().VirtualId, "Mensaje inapropiado", 0, Colour));
                    return;
                }

                //PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_phone", "send_message," + D[2] + "|" + D[3]);

                MUS.SendMessage(new SendMessageComposer(Client, D[2], D[3]));

                //Client.GetRoleplay().CooldownManager.CreateCooldown("msg", 1000, 3);
            }
        }
    }
}
