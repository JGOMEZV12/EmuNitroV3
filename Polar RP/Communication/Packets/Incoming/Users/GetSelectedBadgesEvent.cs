using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboRoleplay.Bots;
using Polar.HabboRoleplay.Bots.Manager;
using Polar.HabboHotel.Users;
using Polar.Communication.Packets.Outgoing.Users;
using Polar.HabboRoleplay.Combat;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Utilities;
using System.Drawing;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.HabboRoleplay.Timers.Types;
using Polar.HabboRoleplay.RPRoom;

namespace Polar.Communication.Packets.Incoming.Users
{
    internal class GetSelectedBadgesEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {

            int UserId = Packet.PopInt();

            if (UserId > 1000000)
            {
                int BotId = UserId - 1000000;
                var Bot = RoleplayBotManager.GetCachedBotById(BotId);
                RoomUser Botx = Session.GetHabbo().CurrentRoom.GetRoomUserManager().GetBotByName(Bot.Name);

                if (Session.GetRoleplay().EquippedWeapon == null)
                {
                    if (Botx != null && Botx.GetBotRoleplay() != null)
                    {
                        Session.GetRoleplay().LastCommand = ":golpe " + Bot.Name;
                        CombatManager.GetCombatType("fist").ExecuteBot(Session, Botx.GetBotRoleplay());
                        return;
                    }
                }
                else
                {
                    if (Botx != null && Botx.GetBotRoleplay() != null)
                    {
                        Session.GetRoleplay().LastCommand = ":disparar " + Bot.Name;
                        CombatManager.GetCombatType("gun").ExecuteBot(Session, Botx.GetBotRoleplay());
                        return;
                    }
                }
                if (Bot != null)
                    Session.SendMessage(new HabboUserBadgesComposer(null, Bot));
            }
            else
            {
                Habbo Habbo = PolarEnvironment.GetHabboById(UserId);
                if (Habbo == null)
                    return;

                // NEW RP
                #region Open user statistics dialogue (sockets)

                if (Habbo.GetClient() != null)
                {
                    if (Habbo.GetClient() != Session)
                    {
                        PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_characterbar", "" + Habbo.Username);
                    }
                }

                #endregion

                if (Habbo.GetClient() != Session)
                {
                    if (Session.GetRoleplay().IsWorking == true && Session.GetRoleplay().JobId == 2 && Session.GetHabbo().CurrentRoomId == 2)
                    {
                        if (Habbo.GetClient().GetRoleplay().IsDead)
                        {

                            PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(Session, ":revivir " + Habbo.Username);
                            return;
                        }
                        else
                        {
                            PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(Session, ":curar " + Habbo.Username);
                            return;
                        }
                    }
                    else if (Session.GetRoleplay().IsWorking == true && Session.GetRoleplay().JobId == 2 && Session.GetHabbo().CurrentRoomId != 2)
                    {
                        if (Habbo.GetClient().GetRoleplay().IsDead)
                        {
                            PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(Session, ":revivir " + Habbo.Username);
                            return;
                        }
                        else
                        {
                            PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(Session, ":ayudar " + Habbo.Username);
                            return;
                        }
                    }
                }

                if (Session.GetRoleplay().CombatMode && Habbo.GetClient() != null)
                {
                    #region Basic Conditions
                    if (Session.GetRoleplay().Cuffed)
                    {
                        Session.SendWhisper("No puedes hacer eso mientras estás esposad@", 1);
                        return;
                    }
                    if (Session.GetRoomUser() != null && !Session.GetRoomUser().CanWalk)
                    {
                        Session.SendWhisper("Al parecer no puedes ni moverte para hacer eso.", 1);
                        return;
                    }
                    if (Session.GetRoleplay().IsDead)
                    {
                        Session.SendWhisper("¡No puedes hacer esto mientras estás muert@!", 1);
                        return;
                    }
                    if (Session.GetRoleplay().IsJailed)
                    {
                        Session.SendWhisper("¡No puedes hacer eso mientras estás encarcelad@!", 1);
                        return;
                    }
                    #endregion

                    GameClient TargetClient = Habbo.GetClient();
                    string MyCity = Session.GetHabbo().CurrentRoom.City;
                    RPRoom Data;
                    int JailRID = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetJail(MyCity, out Data);//prision de la cd.
                    int WantedTime = 5;
                    /* if (Session.GetRoleplay().InCombat)
                     {
                         Session.GetRoleplay().InCombat = false;

                         if (Habbo.GetClient() != null && Habbo.GetClient().GetRoomUser() != null && !Habbo.GetClient().GetRoomUser().IsBot)
                         {*/
                    if (Session.GetRoleplay().EquippedWeapon == null)
                    {
                        if (GroupManager.HasJobCommand(Session, "stun") && (Session.GetRoleplay().IsWorking || Session.GetRoleplay().PoliceTrial) && !TargetClient.GetRoleplay().Cuffed)
                        {
                            // Paralizar / Desparalizar
                            #region Execute
                            RoomUser RoomUser = Session.GetRoomUser();
                            RoomUser TargetUser = TargetClient.GetRoomUser();
                            if (TargetUser == null)
                            {
                                Session.SendWhisper("Ha ocurrido un error en encontrar al usuario, probablemente esté desconectado o no está en esta Zona.", 1);
                                return;
                            }
                            if (!Session.GetHabbo().GetPermissions().HasRight("corporation_rights"))
                            {
                                if (TargetClient.GetHabbo().Id == Session.GetHabbo().Id)
                                {
                                    Session.SendWhisper("No puedes hacerte eso a ti mism@.", 1);
                                    return;
                                }
                                if (GroupManager.HasJobCommand(TargetClient, "law"))
                                {
                                    Session.SendWhisper("¡No puedes hacer eso entre compañeros de trabajo!", 1);
                                    return;
                                }
                            }
                            /* if (Session != TargetClient && TargetClient.GetHabbo().Rank > 3)
                             {
                                 Session.SendWhisper("((No puedes hacerle eso a un miembro de la administración))", 1);
                                 return;
                             }
                             if (Session.GetRoleplay().DrivingCar)
                             {
                                 Session.SendWhisper("¡No puedes hacer eso mientras vas dentro de un vehículo!", 1);
                                 return;
                             }*/
                            if (TargetClient.GetRoleplay().IsDead)
                            {
                                Session.SendWhisper("¡No puedes hacerle eso a una persona muert@!", 1);
                                return;
                            }

                            if (TargetClient.GetRoleplay().IsJailed)
                            {
                                Session.SendWhisper("¡No puedes hacerle eso a una persona encarcelad@!", 1);
                                return;
                            }
                            if (Session.GetRoleplay().EquippedWeapon == null)
                            {
                                Session.SendWhisper("¡Debes equiparte el arma!", 1);
                                return;
                            }

                            if (Session.GetRoleplay().EquippedWeapon.Name != "electrica")
                            {
                                Session.SendWhisper("¡Debes equiparte la pistola electrica!", 1);
                                return;
                            }
                            if (TargetClient.GetRoleplay().IsNoob)
                            {
                                Session.SendWhisper("¡Está persona tiene inmunidad!  >:)", 1);
                                return;
                            }
                            if (TargetUser.IsAsleep)
                            {
                                Session.SendWhisper("¡No puedes aturdir a un usuario ausente!", 1);
                                return;
                            }

                            Point ClientPos = new Point(RoomUser.Coordinate.X, RoomUser.Coordinate.Y);
                            Point TargetClientPos = new Point(TargetUser.Coordinate.X, TargetUser.Coordinate.Y);
                            double Distance = RoleplayManager.GetDistanceBetweenPoints2D(ClientPos, TargetClientPos);

                            if (!TargetClient.GetRoomUser().Frozen)
                            {
                                // Paralizar
                                CryptoRandom Random = new CryptoRandom();
                                int Chance = Random.Next(1, 101);
                                if (Distance <= RoleplayManager.StunGunRange)
                                {
                                    if (Chance <= 8)
                                    {
                                        RoleplayManager.Shout(Session, "*Dispara su pistola electrica hacia " + TargetClient.GetHabbo().Username + ", pero falla*", 37);

                                        //Session.GetRoleplay().CooldownManager.CreateCooldown("stun", 1000, 3);
                                    }
                                    else
                                    {
                                        RoleplayManager.Shout(Session, "*Dispara su pistola electrica hacia " + TargetClient.GetHabbo().Username + " inmovilizándolo inmediatamente*", 37);
                                        TargetClient.GetRoleplay().TimerManager.CreateTimer("stun", 1000, false);
                                        //TargetClient.SendMessage(new FloodControlComposer(15));

                                        if (TargetClient.GetRoleplay().InsideTaxi)
                                            TargetClient.GetRoleplay().InsideTaxi = false;

                                        TargetClient.GetRoomUser().Frozen = true;
                                        TargetClient.GetRoomUser().CanWalk = false;
                                        TargetClient.GetRoomUser().ClearMovement(true);

                                        #region Desequipar al Concito
                                        if (TargetClient.GetRoleplay().EquippedWeapon != null)
                                        {
                                            string UnEquipMessage = TargetClient.GetRoleplay().EquippedWeapon.UnEquipText;
                                            UnEquipMessage = UnEquipMessage.Replace("[NAME]", TargetClient.GetRoleplay().EquippedWeapon.PublicName);

                                            TargetClient.SendWhisper(UnEquipMessage, 1);

                                            if (TargetClient.GetRoomUser().CurrentEffect == TargetClient.GetRoleplay().EquippedWeapon.EffectID)
                                                TargetClient.GetRoomUser().ApplyEffect(0);

                                            if (TargetClient.GetRoomUser().CarryItemID == TargetClient.GetRoleplay().EquippedWeapon.HandItem)
                                                TargetClient.GetRoomUser().CarryItem(0);

                                            TargetClient.GetRoleplay().CooldownManager.CreateCooldown("unequip", 1000, 3);
                                            TargetClient.GetRoleplay().EquippedWeapon = null;

                                            TargetClient.GetRoleplay().WLife = 0;
                                            TargetClient.GetRoleplay().Bullets = 0;
                                        }
                                        #endregion


                                        TargetClient.GetRoleplay().SpecialCooldowns.TryUpdate("stun", 1800, TargetClient.GetRoleplay().SpecialCooldowns["stun"]);
                                    }
                                }
                                else
                                {
                                    RoleplayManager.Shout(Session, "*Dispara su pistola electrica hacia " + TargetClient.GetHabbo().Username + ", pero el disparo no lo alcanza*", 37);
                                    
                                }


                                #region Sound System
                                foreach (RoomUser RoomUsers in Session.GetRoomUser().GetRoom().GetRoomUserManager().GetRoomUsers())
                                {
                                    if (RoomUsers == null || RoomUsers.GetClient() == null)
                                        continue;

                                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(RoomUsers.GetClient(), "event_feedcomposer", "sound|paralizer");
                                }

                                if (!TargetClient.GetRoleplay().IsJailed)
                                {
                                    TargetClient.GetRoleplay().IsJailed = true;
                                    TargetClient.GetRoleplay().JailedTimeLeft = WantedTime;
                                    TargetClient.GetRoleplay().TimerManager.CreateTimer("jail", 1000, false);
                                }

                                if (TargetClient.GetHabbo().CurrentRoomId == JailRID)
                                {
                                    RoleplayManager.GetLookAndMotto(TargetClient);
                                    RoleplayManager.SpawnBeds(TargetClient, "bed_silo_one");
                                    TargetClient.SendMessage(new RoomBubbleNotificationComposer("room_jail_prison", "Usted ha sido escoltado por " + Session.GetHabbo().Username + " por " + WantedTime + " minutos!", ""));
                                }
                                else
                                {
                                    TargetClient.SendMessage(new RoomBubbleNotificationComposer("room_jail_prison", "Usted ha sido escoltado por  " + Session.GetHabbo().Username + " por " + WantedTime + " minutos!", ""));
                                    RoleplayManager.SendUserOld2(TargetClient, JailRID);
                                }
                                #endregion
                            }
                            else
                            {
                                // Desparalizar
                                if (Distance <= 1)
                                {
                                    RoleplayManager.Shout(Session, "*Ayuda a " + TargetClient.GetHabbo().Username + ", dándole tiempo para recuperarse de su aturdimiento*", 37);

                                    TargetClient.GetRoomUser().Frozen = false;
                                    TargetClient.GetRoomUser().CanWalk = true;
                                    TargetClient.GetRoomUser().ClearMovement(true);
                                    return;
                                }
                                else
                                {
                                    Session.SendWhisper("Debes estar más cerca de la persona.", 1);
                                    return;
                                }
                            }
                            #endregion
                        }
                        else
                        {
                            RoomUser TargetUser = TargetClient.GetRoomUser();
                            if (TargetUser == null)
                            {
                                Session.SendWhisper("Ha ocurrido un error en encontrar al usuario, probablemente esté desconectado o no está en esta Zona.", 1);
                                return;
                            }
                            if (TargetClient.GetRoleplay().Cuffed)
                            {
                                Session.SendWhisper("¡No puedes golpear a una persona esposada!", 1);
                                return;
                            }
                            if (TargetClient.GetRoleplay().IsNoob == true)
                            {
                                Session.SendWhisper("¡Está persona tiene inmunidad!  >:)", 1);
                                return;
                            }
                            if (TargetClient.GetRoleplay().IsDead)
                            {
                                Session.SendWhisper("¡No puedes golpear a una persona muerta!", 1);
                                return;
                            }
                            if (TargetClient.GetRoleplay().IsJailed)
                            {
                                Session.SendWhisper("¡No puedes golpear a una persona encarcelada!", 1);
                                return;
                            }
                            if (TargetClient.GetRoomUser().isLying)
                            {
                                Session.SendWhisper("¡No puedes hacerle eso a una persona muerta", 1);
                                return;
                            }
                            /* if (TargetClient.GetConnection().getIp() == Session.GetConnection().getIp())
                             {
                                 Session.SendWhisper("¡No puedes golpear a tus propias cuentas!", 1);
                                 return;
                             }*/

                            // New Target System
                            Session.GetRoleplay().Target = TargetClient.GetHabbo().Username;

                            Session.GetRoleplay().LastCommand = ":golpe " + Habbo.Username;
                            CombatManager.GetCombatType("fist").Execute(Session, TargetClient);
                        }
                    }
                    else
                    {
                        // Disparar
                        #region Execute
                        RoomUser TargetUser = TargetClient.GetRoomUser();
                        if (TargetUser == null)
                        {
                            Session.SendWhisper("Ha ocurrido un error en encontrar al usuario, probablemente esté desconectado o no está en esta Zona.", 1);
                            return;
                        }
                        if (TargetClient.GetRoleplay().Cuffed)
                        {
                            Session.SendWhisper("¡No puedes dispararle a una persona esposada!", 1);
                            return;
                        }
                        if (TargetClient.GetRoleplay().IsNoob)
                        {
                            Session.SendWhisper("¡Está persona tiene inmunidad!  >:)", 1);
                            return;
                        }
                        if (TargetClient.GetRoleplay().IsDead)
                        {
                            Session.SendWhisper("¡No puedes dispararle a una persona muerta!", 1);
                            return;
                        }
                        if (TargetClient.GetRoleplay().IsJailed)
                        {
                            Session.SendWhisper("¡No puedes dispararle a una persona encarcelada!", 1);
                            return;
                        }
                        if (TargetClient.GetRoomUser().isLying)
                        {
                            Session.SendWhisper("¡No puedes hacerle eso a una persona muerta", 1);
                            return;
                        }
                        /*if (TargetClient.GetConnection().getIp() == Session.GetConnection().getIp())
                        {
                            Session.SendWhisper("¡No puedes dispararle a tus propias cuentas!", 1);
                            return;
                        }*/
                        if (Session.GetRoleplay().PassiveMode)
                        {
                            Session.SendWhisper("No puedes disparar en modo pasivo.", 1);
                            return;
                        }

                        if (TargetClient.GetRoleplay().PassiveMode)
                        {
                            Session.SendWhisper("No puedes disparara una persona en modo pasivo.", 1);
                            return;
                        }
                        if (Session.GetRoleplay().WLife <= 0)
                        {
                            Session.SendWhisper("¡Tu arma está dañada! Busca a un Armero para que la repare.", 1);
                            return;
                        }
                        /*if (Session.GetRoleplay().DrivingCar)
                        {
                            if (!GroupManager.HasJobCommand(Session, "law") && !Session.GetHabbo().GetPermissions().HasRight("corporation_rights"))
                            {
                                Session.SendWhisper("¡No puedes hacer eso mientras vas dentro de un vehículo!", 1);
                                return;
                            }
                        }*/

                        #region Fuego amigo entre bandas
                        List<Group> MyGang = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(Session.GetHabbo().Id);
                        List<Group> EnemyGang = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(TargetClient.GetHabbo().Id);
                        // OFF
                        /*if (MyGang != null && MyGang.Count > 0)
                        {
                            if (EnemyGang != null && EnemyGang.Count > 0 && EnemyGang[0] == MyGang[0])
                            {
                                Session.SendWhisper("¡No puedes dispararle a tus compañeros de banda!", 1);
                                return;
                            }
                        }*/
                        #endregion

                        #region Tipo de armas

                        #endregion

                        // New Target System
                        Session.GetRoleplay().Target = Habbo.Username;


                        Session.GetRoleplay().LastCommand = ":disparar " + Habbo.Username;
                        CombatManager.GetCombatType("gun").Execute(Session, TargetClient);
                        //Session.GetRoleplay().CooldownManager.CreateCooldown("gun", 250, 1);
                        #endregion

                    }
                        }
                   /* }
                }*/

                Session.SendMessage(new HabboUserBadgesComposer(Habbo));
            }
        }
    }
}