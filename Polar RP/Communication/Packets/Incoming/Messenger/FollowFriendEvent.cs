using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Misc;

using Polar.Communication.Packets.Outgoing.Messenger;

namespace Polar.Communication.Packets.Incoming.Messenger
{
    internal class FollowFriendEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {

            if (Session == null || Session.GetHabbo() == null || Session.GetHabbo().GetMessenger() == null)
                return;

            int BuddyId = Packet.PopInt();


            if (BuddyId == 0 || BuddyId == Session.GetHabbo().Id)
                return;

            GameClient Client = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(BuddyId);
            if (Client == null || Client.GetHabbo() == null)
                return;

            if (!Client.GetHabbo().InRoom)
            {
                Session.SendMessage(new FollowFriendFailedComposer(2));
                Session.GetHabbo().GetMessenger().UpdateFriend(Client.GetHabbo().Id, Client, true);
                return;
            }
            else if (Session.GetHabbo().CurrentRoom != null && Client.GetHabbo().CurrentRoom != null)
            {
                if (Session.GetHabbo().CurrentRoom.RoomId == Client.GetHabbo().CurrentRoom.RoomId)
                    return;
            }

            RoomData roomData = PolarEnvironment.GetGame().GetRoomManager().GenerateRoomData(Client.GetHabbo().CurrentRoomId);


            if (Session.GetHabbo().GetPermissions().HasRight("can_follow_friends"))
            {

                RoleplayManager.SendUser(Session, Client.GetHabbo().CurrentRoomId);
            }
            else
            {
                Session.SendNotification("En un mundo rol no puedes seguir a tus amigos con magia. ¡Pregúntale dónde está y camina! Como en la vida real, es muy fácil. Quizás un vehículo podría ayudarte ;)");
                return;
            }
        
            /*{
            if (Session == null || Session.GetHabbo() == null || Session.GetHabbo().GetMessenger() == null)
                return;

            if (!RoleplayManager.FollowFriends)
            {
                Session.SendWhisper("OOPS, la administración del servidor ha desactivado la capacidad de seguir a tus amigos.", 1);
                return;
            }

            int BuddyId = Packet.PopInt();
            bool IsVip = Session.GetHabbo().VIPRank < 1 ? false : true;
            int Cost = IsVip ? 0 : 3;
            int Time = IsVip ? (5 + DayNightManager.GetTaxiTime()) : (10 + DayNightManager.GetTaxiTime());
            string TaxiText = IsVip ? " VIP" : "";
            bool OnDuty = false;

            if (BuddyId == 0 || BuddyId == Session.GetHabbo().Id)
                return;

            GameClient Client = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(BuddyId);
            if (Client == null || Client.GetHabbo() == null)
                return;

            if (!Client.GetHabbo().InRoom)
            {
                Session.SendMessage(new FollowFriendFailedComposer(2));
                Session.GetHabbo().GetMessenger().UpdateFriend(Client.GetHabbo().Id, Client, true);
                return;
            }
            else if (Session.GetHabbo().CurrentRoom != null && Client.GetHabbo().CurrentRoom != null)
            {
                if (Session.GetHabbo().CurrentRoom.RoomId == Client.GetHabbo().CurrentRoom.RoomId)
                    return;
            }

            if (!Client.GetHabbo().AllowConsoleMessages)
            {
                if (Session.GetHabbo().InRoom)
                    Session.SendWhisper("Lo siento, pero este ciudadano ha apagado su teléfono, así que no puedes seguirlos.", 1);
                else
                    Session.SendNotification("Lo siento, pero este ciudadano ha apagado su teléfono, así que no puedes seguirlos.");
                return;
            }

            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes seguir a tu amigo mientras estés muerto!", 1);
                return;
            }

            if (Session.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes seguir a tu amigo mientras estás encarcelado!", 1);
                return;
            }

            if (Session.GetRoomUser() != null)
            {
                if (Session.GetRoomUser().Frozen)
                {
                    Session.SendWhisper("¡No puedes seguir a tu amigo mientras estés congelado o aturdido!", 1);
                    return;
                }
            }

            if (Session.GetHabbo().GetPermissions().HasRight("mod_tool") && Session.GetRoleplay().StaffOnDuty)
                OnDuty = true;
            if (Session.GetHabbo().VIPRank > 1)
                OnDuty = true;

            if (Session.GetHabbo().CurrentRoom != null)
            {
                if (!Session.GetHabbo().CurrentRoom.TaxiFromEnabled && !OnDuty)
                {
                    Session.SendWhisper("[HOLO TAXI] ¡Lo siento, no podemos sacarte de este cuarto!", 1);
                    return;
                }
            }

            if (Session.GetRoleplay().Cuffed)
            {
                Session.SendWhisper("¡los taxistas están demasiado asustados para llevarte con esas esposas pegadas a ti!", 1);
                return;
            }

            bool PoliceCost = false;
            if (HabboHotel.Groups.GroupManager.HasJobCommand(Session, "guide") && Session.GetRoleplay().IsWorking)
                PoliceCost = true;

            if (Session.GetHabbo().Credits < Cost && Cost > 0 && !OnDuty && !PoliceCost)
            {
                Session.SendWhisper("[HOLO TAXI] ¡No tienes suficiente dinero para dar un paseo!", 1);
                return;
            }

            if (Session.GetRoleplay().InsideTaxi)
            {
                Session.SendWhisper("[HOLO TAXI] Ya te estoy recogiendo! diga ':notaxi' si cambias de opinión!", 1);
                return;
            }

            RoomData roomData = PolarEnvironment.GetGame().GetRoomManager().GenerateRoomData(Client.GetHabbo().CurrentRoomId);

            if (roomData == null)
            {
                Session.SendWhisper("[HOLO TAXI] Lo siento, no he logrado encontrar ese lugar", 1);
                return;
            }

            if (!roomData.TaxiToEnabled && !OnDuty)
            {
                Session.SendWhisper("[HOLO TAXI] Lo sentimos, no podemos taxi a esta habitación", 1);
                return;
            }

            if (!roomData.BusToEnabled && !OnDuty)
            {
                Session.SendWhisper("[BUS] Lo sentimos, no podemos ir en BUS a esta habitación", 1);
                return;
            }

            if (roomData.TutorialEnabled && !OnDuty)
            {
                Session.SendWhisper("No se puede taxi a una sala de tutorial, lo siento", 1);
                return;
            }

            if (Session.GetHabbo().CurrentRoom != null)
            {
                if (Session.GetHabbo().CurrentRoom.TutorialEnabled && !OnDuty)
                {
                    Session.SendWhisper("¡No puedes salir de una sala de tutorías! Termine el tutorial en su lugar.", 1);
                    return;
                }
            }

            if (Session.GetRoleplay().TexasHoldEmPlayer > 0)
            {
                Session.SendWhisper("No se puede taxi en medio de un Texas Hold 'Em juego!", 1);
                return;
            }

            Session.GetRoleplay().InsideTaxi = true;
            bool PoliceTaxi = false;

            if (!OnDuty)
            {
                if (HabboHotel.Groups.GroupManager.HasJobCommand(Session, "guide") && Session.GetRoleplay().IsWorking)
                {
                    Cost = 0;
                    Time = 5;

                    if (Session.GetRoomUser() != null)
                        Session.GetRoomUser().ApplyEffect(19);

                    Session.Shout("*Saque su Radio de Policía y pide una rápida recolección para " + roomData.Name + " [ID: " + roomData.Id + "]*", 37);
                    PoliceTaxi = true;
                }
                else
                {
                    if (Session.GetRoomUser() != null)
                        Session.GetRoomUser().ApplyEffect(596);

                    Session.Shout("*Llama un " + TaxiText + " Taxi para ir a " + roomData.Name + " [ID: " + roomData.Id + "]*", 4);
                }

                new Thread(() =>
                {
                    for (int i = 0; i < (Time + 1) * 10; i++)
                    {
                        if (Session.GetRoleplay() == null)
                            break;

                        if (Session.GetRoleplay().InsideTaxi)
                            Thread.Sleep(100);
                        else
                            break;
                    }
                    if (Session.GetRoleplay() != null)
                    {
                        if (Session.GetRoleplay().InsideTaxi)
                        {
                            if (Cost > 0)
                            {
                                Session.GetHabbo().Credits -= Cost;
                                Session.GetHabbo().UpdateCreditsBalance();
                            }

                            if (PoliceTaxi)
                            {
                                if (Session.GetRoomUser() != null)
                                    Session.GetRoomUser().ApplyEffect(EffectsList.CarPolice);
                                Session.Shout("*Entra en la patrulla de policía de su compañero*", 37);
                            }
                            else
                                Session.Shout("*Sale " + TaxiText + " y ya esta en el sitio*", 4);
                            RoleplayManager.SendUser(Session, roomData.Id);
                        }
                    }
                }).Start();
            }
            else
            {
                Session.Shout("*Utiliza sus poderes divinos y sigue a" + Client.GetHabbo().Username + "*", 23);
                RoleplayManager.SendUser(Session, Client.GetHabbo().CurrentRoomId);
                PolarEnvironment.GetGame().GetChatManager().GetCommands().LogCommand(Session.GetHabbo().Id, "follow " + Client.GetHabbo().Username, Session.GetHabbo().MachineId, "staff");
            }*/

        }
    }
}
