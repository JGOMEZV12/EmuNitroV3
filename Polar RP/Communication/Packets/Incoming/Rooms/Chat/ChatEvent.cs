using System;
using System.Threading.Tasks;
using Polar.Utilities;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Logs;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.Communication.Packets.Outgoing.Moderation;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Avatar;
using System.Collections.Generic;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;

namespace Polar.Communication.Packets.Incoming.Rooms.Chat
{
    public class ChatEvent : IPacketEvent
    {
        public async void Parse(GameClient Session, ClientPacket Packet)
        {
            try
            {
                if (Session == null || Session.GetHabbo() == null || !Session.GetHabbo().InRoom)
                    return;

                Room Room = Session.GetHabbo().CurrentRoom;
                if (Room == null)
                    return;

                RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
                if (User == null)
                    return;

                if (Session.LoggingOut)
                    return;

                string Message = StringCharFilter.Escape(Packet.PopString(), false, Session);
                if (Message.Length > 150)
                    Message = Message.Substring(0, 150);

                int Bubble = Packet.PopInt();
                string Colour = Packet.PopString();

                if (string.IsNullOrEmpty(Message))
                    return;

                if (Bubble != 0 && !User.GetClient().GetHabbo().GetPermissions().HasRight("use_any_bubble"))
                    Bubble = 0;

                ChatStyle Style = null;
                if (!PolarEnvironment.GetGame().GetChatManager().GetChatStyles().TryGetStyle(Bubble, out Style) || (Style.RequiredRight.Length > 0 && !Session.GetHabbo().GetPermissions().HasRight(Style.RequiredRight)))
                    Bubble = 0;

                // ✅ Si es comando salta el flood, si no lo aplica
                bool isCommand = Message.StartsWith(":", StringComparison.CurrentCulture);

                if (!isCommand)
                {
                    if (PolarEnvironment.GetUnixTimestamp() < Session.GetHabbo().FloodTime && Session.GetHabbo().FloodTime != 0)
                        return;

                    if (Session.GetHabbo().TimeMuted > 0)
                    {
                        Session.SendMessage(new MutedComposer(Session.GetHabbo().TimeMuted));
                        return;
                    }

                    if (!Session.GetHabbo().GetPermissions().HasRight("room_ignore_mute") && Room.CheckMute(Session))
                    {
                        Session.SendWhisper("No puedes escribir, usted ha sido muteado por favor espere...", 1);
                        return;
                    }
                }
                else
                {
                    // Comandos solo verifican mute, no flood
                    if (Session.GetHabbo().TimeMuted > 0)
                    {
                        Session.SendMessage(new MutedComposer(Session.GetHabbo().TimeMuted));
                        return;
                    }
                }

                User.LastBubble = Session.GetHabbo().CustomBubbleId == 0 ? Bubble : Session.GetHabbo().CustomBubbleId;

                if (!Session.GetHabbo().GetPermissions().HasRight("mod_tool"))
                {
                    int MuteTime;
                }

                if (isCommand)
                {
                    if (await PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(Session, Message))
                        return;
                }

                PolarEnvironment.GetGame().GetChatManager().GetLogs().StoreChatlog(new ChatlogEntry(Session.GetHabbo().Id, Room.Id, Message, UnixTimestamp.GetNow(), Session.GetHabbo(), Room));

                string word;
                if (!Message.Contains("engine/uploads") && !Message.Contains("giphy") && !Session.GetHabbo().GetPermissions().HasRight("advertisement_filter_override") &&
                     PolarEnvironment.GetGame().GetChatManager().GetFilter().IsUnnaceptableWord(Message, out word))
                {
                    Session.GetHabbo().BannedPhraseCount++;
                    if (Session.GetHabbo().BannedPhraseCount >= 1)
                    {
                        User.MoveTo(Room.GetGameMap().Model.DoorX, Room.GetGameMap().Model.DoorY);
                        Session.GetHabbo().TimeMuted = 25;
                        Session.SendNotification("¡Has sido silenciad@ mientras un moderador revisa tu caso, al parecer nombraste un hotel! <b>Aviso: " + Session.GetHabbo().BannedPhraseCount + "/5</b>");
                        PolarEnvironment.GetGame().GetClientManager().StaffAlert(new RoomNotificationComposer("Alerta de publicista:",
                            "Atención, se ha mencionado la palabra <b>" + word.ToUpper() + "</b> en la frase <i>" + Message +
                            "</i> dentro de una sala\r\n" + "- Este usuario: <b>" +
                            Session.GetHabbo().Username + "</b>", "filter", "Ir a la Sala", "event:navigator/goto/" +
                            Session.GetHabbo().CurrentRoomId));
                    }
                    if (Session.GetHabbo().BannedPhraseCount >= 5)
                    {
                        PolarEnvironment.GetGame().GetModerationManager().BanUser("System", HabboHotel.Moderation.ModerationBanType.USERNAME, Session.GetHabbo().Username, "Baneado por hacer Spam con la Frase (" + Message + ")", (PolarEnvironment.GetUnixTimestamp() + 78892200));
                        Session.Disconnect(true);
                        return;
                    }

                    Session.SendMessage(new ChatComposer(User.VirtualId, "Mensaje inapropiado", 0, Bubble, Colour));
                    return;
                }

                if (Message.ToLower().Equals("o/"))
                {
                    Room.SendMessage(new ActionComposer(User.VirtualId, 1));
                    return;
                }

                if (Message.ToLower().Equals("t/"))
                {
                    Room.SendMessage(new ActionComposer(User.VirtualId, 5));
                    return;
                }

                if (Message.ToLower().Equals(".i."))
                {
                    Room.SendMessage(new ActionComposer(User.VirtualId, 7));
                    return;
                }

                if (Message.ToLower().Equals(":d"))
                {
                    Room.SendMessage(new ActionComposer(User.VirtualId, 3));
                    return;
                }

                if (Message.ToLower().Equals("z/"))
                {
                    Room.SendMessage(new ActionComposer(User.VirtualId, 4));
                    return;
                }

                if (Message.ToLower().Equals(":*"))
                {
                    Room.SendMessage(new ActionComposer(User.VirtualId, 2));
                    return;
                }

                User.UnIdle();

                var roleplay = Session.GetRoleplay();

                if (roleplay != null)
                {
                    if (User != null && Room != null)
                    {
                        if (roleplay.IsWorking && HabboHotel.Groups.GroupManager.HasJobCommand(Session, "guide"))
                            User.OnChat(37, Message, false, Colour);
                        else if (roleplay.StaffOnDuty && Session.GetHabbo().GetPermissions().HasRight("mod_tool"))
                            User.OnChat(23, Message, false, Colour);
                        else if (roleplay.AmbassadorOnDuty && Session.GetHabbo().GetPermissions().HasRight("ambassador"))
                            User.OnChat(37, Message, false, Colour);
                        else if (roleplay.CurHealth > 25 && roleplay.CurHealth <= 40 && !roleplay.IsDead)
                            User.OnChat(5, Message, false, Colour);
                        else if (roleplay.CurHealth <= 25 && !roleplay.IsDead)
                            User.OnChat(3, Message, false, Colour);
                        else if (roleplay.IsDead)
                            User.OnChat(3, "[ " + Message + " ]", false, Colour);
                        else
                            User.OnChat(User.LastBubble, Message, false, Colour);
                    }
                    else
                    {
                        if (Room != null && Session.GetHabbo() != null)
                            Room.SendMessage(new ChatComposer(Session.GetHabbo().Id, Message, 0, Bubble, Colour));
                    }
                }
                else
                {
                    if (User != null && Room != null)
                        User.OnChat(User.LastBubble, Message, false, Colour);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ChatEvent Error: {ex.Message}\nStackTrace: {ex.StackTrace}");

                if (Session != null && Session.GetHabbo() != null)
                    Session.SendWhisper("Ocurrió un error al procesar tu mensaje. Por favor, intenta de nuevo.", 1);
            }
        }
    }
}