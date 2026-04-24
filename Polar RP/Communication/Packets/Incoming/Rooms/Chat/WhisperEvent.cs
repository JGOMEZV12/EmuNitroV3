using System;
using System.Collections.Generic;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.Core;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Quests;
using Polar.HabboHotel.Rooms;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing.Moderation;
using Polar.Utilities;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboHotel.Rooms.Chat.Commands;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;

namespace Polar.Communication.Packets.Incoming.Rooms.Chat
{
    public class WhisperEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            if (!Session.GetHabbo().InRoom)
                return;

            Room Room = Session.GetHabbo().CurrentRoom;
            if (Room == null)
                return;

            if (!Session.GetHabbo().GetPermissions().HasRight("mod_tool") && Room.CheckMute(Session))
            {
                Session.SendWhisper("No puedes escribir, usted ha sido muteado por favor espere...", 1);
                return;
            }

            if (PolarEnvironment.GetUnixTimestamp() < Session.GetHabbo().FloodTime && Session.GetHabbo().FloodTime != 0)
                return;

            if (Session.LoggingOut)
                return;

            string[] Params = Packet.PopString().Split(' ');

            if (Params.Length < 2)
                return;

            string ToUser = Params[0];
            string Message = CommandManager.MergeParams(Params, 1);

            int Bubble = Packet.PopInt();
            string Colour = Packet.PopString();

            if (Bubble != 0 && !Session.GetHabbo().GetPermissions().HasRight("use_any_bubble"))
                Bubble = 0;

            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Username);
            if (User == null)
                return;

            RoomUser User2 = Room.GetRoomUserManager().GetRoomUserByHabbo(ToUser);
            if (User2 == null)
                return;

            if (Session.GetHabbo().TimeMuted > 0)
            {
                Session.SendMessage(new MutedComposer(Session.GetHabbo().TimeMuted));
                return;
            }

            ChatStyle Style = null;
            if (!PolarEnvironment.GetGame().GetChatManager().GetChatStyles().TryGetStyle(Bubble, out Style) || (Style.RequiredRight.Length > 0 && !Session.GetHabbo().GetPermissions().HasRight(Style.RequiredRight)))
                Bubble = 0;

            User.LastBubble = Session.GetHabbo().CustomBubbleId == 0 ? Bubble : Session.GetHabbo().CustomBubbleId;

            if (!Session.GetHabbo().GetPermissions().HasRight("mod_tool"))
            {
                int MuteTime;
                if (User.IncrementAndCheckFlood(out MuteTime))
                {
                    Session.SendMessage(new FloodControlComposer(MuteTime));
                    return;
                }
            }

            if (!User2.GetClient().GetHabbo().ReceiveWhispers && !Session.GetHabbo().GetPermissions().HasRight("room_whisper_override"))
            {
                Session.SendWhisper("¡Vaya, este usuario tiene sus susurros desactivados!", 1);
                return;
            }

            PolarEnvironment.GetGame().GetChatManager().GetLogs().StoreChatlog(new Polar.HabboHotel.Rooms.Chat.Logs.ChatlogEntry(Session.GetHabbo().Id, Room.Id, "<Whisper to " + ToUser + ">: " + Message, UnixTimestamp.GetNow(), Session.GetHabbo(), Room));
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

                Session.SendMessage(new ChatComposer(User.VirtualId, "Mensaje inapropiado", 0, Bubble));
                return;
            }

            if (Session.GetRoleplay() != null)
            {
                if (!Session.GetRoleplay().IsWorking)
                    User.UnIdle();
            }
            else
                User.UnIdle();

            //User.SendNameBubblePacket();
            if (Session.GetRoleplay() != null)
            {
                if (User.GetClient().GetRoleplay().IsWorking && HabboHotel.Groups.GroupManager.HasJobCommand(Session, "guide"))
                {
                    if (User.GetClient().GetHabbo().Translating)
                    {
                        string LG1 = User.GetClient().GetHabbo().FromLanguage.ToLower();
                        string LG2 = User.GetClient().GetHabbo().ToLanguage.ToLower();

                        User.GetClient().SendMessage(new WhisperComposer(User.VirtualId, PolarEnvironment.translate(Message, LG1, LG2) + " [" + LG1.ToUpper() + " -> " + LG2.ToUpper() + "]", 0, 37, Colour));
                    }
                    else
                        User.GetClient().SendMessage(new WhisperComposer(User.VirtualId, Message, 0, 37));
                }
                else
                {
                    if (User.GetClient().GetHabbo().Translating)
                    {
                        string LG1 = User.GetClient().GetHabbo().FromLanguage.ToLower();
                        string LG2 = User.GetClient().GetHabbo().ToLanguage.ToLower();

                        User.GetClient().SendMessage(new WhisperComposer(User.VirtualId, PolarEnvironment.translate(Message, LG1, LG2) + " [" + LG1.ToUpper() + " -> " + LG2.ToUpper() + "]", 0, User.LastBubble, Colour));
                    }
                    else
                        User.GetClient().SendMessage(new WhisperComposer(User.VirtualId, Message, 0, User.LastBubble, Colour));
                }
            }
            else
            {
                if (User.GetClient().GetHabbo().Translating)
                {
                    string LG1 = User.GetClient().GetHabbo().FromLanguage.ToLower();
                    string LG2 = User.GetClient().GetHabbo().ToLanguage.ToLower();

                    User.GetClient().SendMessage(new WhisperComposer(User.VirtualId, PolarEnvironment.translate(Message, LG1, LG2) + " [" + LG1.ToUpper() + " -> " + LG2.ToUpper() + "]", 0, User.LastBubble));
                }
                else
                    User.GetClient().SendMessage(new WhisperComposer(User.VirtualId, Message, 0, User.LastBubble));
            }

            if (User2 != null && !User2.IsBot && User2.UserId != User.UserId)
            {
                if (!User2.GetClient().GetHabbo().MutedUsers.Contains(Session.GetHabbo().Id))
                {
                    if (User.GetClient().GetHabbo().Translating)
                    {
                        string LG1 = User.GetClient().GetHabbo().FromLanguage.ToLower();
                        string LG2 = User.GetClient().GetHabbo().ToLanguage.ToLower();

                        User2.GetClient().SendMessage(new WhisperComposer(User.VirtualId, PolarEnvironment.translate(Message, LG1, LG2) + " [" + LG1.ToUpper() + " -> " + LG2.ToUpper() + "]", 0, User.LastBubble));
                    }
                    else
                        User2.GetClient().SendMessage(new WhisperComposer(User.VirtualId, Message, 0, User.LastBubble));
                }
            }

            List<RoomUser> ToNotify = Room.GetRoomUserManager().GetRoomUserByRank(9);
            if (ToNotify.Count > 0)
            {
                foreach (RoomUser user in ToNotify)
                {
                    if (user != null && user.HabboId != User2.HabboId && user.HabboId != User.HabboId)
                    {
                        if (user.GetClient() != null && user.GetClient().GetHabbo() != null && !user.GetClient().GetHabbo().IgnorePublicWhispers)
                        {
                            if (User.GetClient().GetHabbo().Translating)
                            {
                                string LG1 = User.GetClient().GetHabbo().FromLanguage.ToLower();
                                string LG2 = User.GetClient().GetHabbo().ToLanguage.ToLower();

                                user.GetClient().SendMessage(new WhisperComposer(User.VirtualId, "[Susurra a " + ToUser + "] " + PolarEnvironment.translate(Message, LG1, LG2) + " [" + LG1.ToUpper() + " -> " + LG2.ToUpper() + "]", 0, User.LastBubble));
                            }
                            else
                                user.GetClient().SendMessage(new WhisperComposer(User.VirtualId, "[Susurra a " + ToUser + "] " + Message, 0, User.LastBubble));
                        }
                    }
                }
            }
            //User.SendNamePacket();
        }
    }
}