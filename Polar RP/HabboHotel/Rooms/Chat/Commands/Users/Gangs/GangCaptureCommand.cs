using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboRoleplay.Turfs;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Pathfinding;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Gangs
{
    class GangCaptureCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_gang_capture"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Invadir terrirorio."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Variables
            Group Gang = GroupManager.GetGang(Session.GetRoleplay().GangId);
            bool InsideTurf = false;
            #endregion

            Item item = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "army_c15_groupflag");

            // ✅ FIX: FirstOrDefault devuelve null si no hay ninguna flag en la sala
            if (item == null)
            {
                Session.SendWhisper("No se encontró ninguna bandera de territorio en esta sala.", 1);
                return;
            }

            if (!item.GetRoom().TurfEnabled)
            {
                Session.SendWhisper("Este no es un territorio.", 1);
                return;
            }
                

            RoomUser User = item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

            if (!item.GetBaseItem().ItemName.Contains("army_c15_groupflag"))
                return;

            if (!Gamemap.TilesTouching(item.GetX, item.GetY, User.Coordinate.X, User.Coordinate.Y))
            {
                if (item.ExtraData == "" || item.ExtraData == "0")
                    if (User.CanWalk)
                        User.MoveTo(item.SquareInFront);
            }
            else
            {
                if (Session.GetRoleplay().TryGetCooldown("capturing", true))
                    return;

                #region Conditions

                if (Session.GetRoleplay().TurfCapturing)
                {
                    Session.SendWhisper("Ya te encuentras capturando el territorio. ¡Procura no moverte!", 1);
                    return;
                }
                if (User.GetRoom().TurfCapturing)
                {
                    Session.SendWhisper("¡El territorio ya está siendo capturado!", 1);
                    return;
                }

                if (Gang.BankRuptcy)
                {
                    Session.SendWhisper("¡Tu banda está en banca rota! No pueden seguir operando en ella.", 1);
                    return;
                }

                if (Session.GetRoleplay().GangId <= 0)
                {
                    Session.SendWhisper("¡No perteneces a ninguna Pandilla!", 1);
                    return;
                }

                if (Session.GetRoleplay().IsDead)
                {
                    Session.SendWhisper("No puedes capturar un territorio de pandillas mientras estás muerto", 1);
                    return;
                }

                if (Session.GetRoleplay().PassiveMode)
                {
                    Session.SendWhisper("No puedes capturar en modo pasivo.", 1);
                    return;
                }

                if (Session.GetRoleplay().IsJailed)
                {
                    Session.SendWhisper("No puedes hacer esto estando en la carcel", 1);
                    return;
                }

                if (Session.GetRoomUser().Frozen)
                {
                    Session.SendWhisper("No puedes hacer esto estando aturdido", 1);
                    return;
                }

                if (Session.GetRoleplay().DrivingCar)
                {
                    Session.SendWhisper("No puedes capturar un césped mientras conduces un vehículo", 1);
                    return;
                }
                #endregion

                if (User.GetRoom().Group != null)
                {
                    if (User.GetRoom().Group == Gang)
                    {
                        Session.SendWhisper("¡Este barrio ya pertence a tu banda!", 1);
                        return;
                    }
                    else
                    {
                        // Alertamos a los integrantes de la banda atacada
                        foreach (GameClient client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                        {
                            if (client == null || client.GetHabbo() == null)
                                continue;

                            List<Groups.Group> thegroup = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(client.GetHabbo().Id);

                            if (thegroup == null || thegroup.Count <= 0)
                                continue;

                            if (thegroup[0] != User.GetRoom().Group)
                                continue;

                            if (client.GetRoleplay().DisableRadio == true)
                                continue;

                            client.SendWhisper("[RADIO] ¡Están atacando nuestro " + User.GetRoom().Name + "! ¡Vamos a defenderlo!", 30);
                        }
                    }
                }

                #region Execute
                Session.Shout("*Empieza apoderarse del territorio para que sea de: " + Gang.Name + " [-10 Energia]*", 4);
                Session.GetRoleplay().CurEnergy -= 10;
                Session.SendWhisper("¡Te quedan 2 minutos para capturar este territorio de pandillas!", 1);

                //Session.GetRoleplay().CapturingTurf = Room;
                Session.GetRoleplay().TurfCapturing = true;
                Session.GetRoleplay().LoadingTimeLeft = RoleplayManager.TurfCapTime;
                Session.GetRoleplay().TurfFlagId = item.Id;
                Room.TurfCapturing = true;
                Room.TurfUserAtackerId = Session.GetHabbo().Id;

                Session.GetRoleplay().TimerManager.CreateTimer("turfcapture", 1000, false);


                Group CurrentGang = GroupManager.GetGang(Room.Group.Id);

                if (CurrentGang.Id > 1000)
                {
                    lock (CurrentGang.Members.Values)
                    {
                        foreach (GroupMember Member in CurrentGang.Members.Values)
                        {
                            GameClient Client = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Member.UserId);

                            if (Client == null)
                                continue;

                            Client.SendWhisper("[PANDILLA] Territorio atacado: " + Room.Name + " [RoomID: " + Room.Id + "] está siendo capturado [VE DE INMEDIATO]", 34);
                        }
                    }
                }
                #endregion
            }
        }
    }
}