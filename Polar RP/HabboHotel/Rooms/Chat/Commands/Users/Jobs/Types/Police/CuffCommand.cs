using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Police
{
    class CuffCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_police_cuff"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Esposar al usuario con el fin de arrestarlo."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Params.Length == 1)
            {
                Session.SendWhisper("Vaya, se le olvidó ingresar un nombre de usuario", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null)
            {
                Session.SendWhisper("Se ha producido un error al intentar encontrar a ese usuario, tal vez estén sin conexión.", 1);
                return;
            }

            RoomUser RoomUser = Session.GetRoomUser();
            RoomUser TargetUser = Room.GetRoomUserManager().GetRoomUserByHabbo(TargetClient.GetHabbo().Username);
            if (TargetUser == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no están en línea o en esta habitación.", 1);
                return;
            }

            if (!GroupManager.HasJobCommand(Session, "cuff") && !Session.GetRoleplay().PoliceTrial)
            {
                Session.SendWhisper("Sólo un oficial de policía puede utilizar este comando", 1);
                return;
            }
            if (TargetClient.GetRoleplay().PassiveMode)
            {
                Session.SendWhisper("¡No puedes hacerle eso a una persona que está en modo pasivo!", 1);
                return;
            }
            if (!Session.GetRoleplay().IsWorking && !Session.GetRoleplay().PoliceTrial)
            {
                Session.SendWhisper("¡Debes estar trabajando para usar este comando!", 1);
                return;
            }

            if (TargetClient.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes esposar a alguien que está muerto!", 1);
                return;
            }

            if (TargetClient.GetRoleplay().IsJailed && !TargetClient.GetRoleplay().Jailbroken)
            {
                Session.SendWhisper("¡No puedes esposar a alguien que está en la cárcel!", 1);
                return;
            }

            if (!TargetClient.GetRoomUser().Frozen)
            {
                Session.SendWhisper("Usted no puede esposar a alguien que no está aturdido", 1);
                return;
            }

            if (TargetClient.GetRoleplay().Cuffed)
            {
                Session.SendWhisper("Usted no puede esposar a alguien que ya está esposado", 1);
                return;
            }

            if (TargetUser.IsAsleep)
            {
                Session.SendWhisper("Usted no puede esposar a alguien que no está jugando el juego ahora mismo", 1);
                return;
            }
            #endregion

            #region Execute
            Point ClientPos = new Point(RoomUser.X, RoomUser.Y);
            Point TargetClientPos = new Point(TargetUser.X, TargetUser.Y);
            double Distance = RoleplayManager.GetDistanceBetweenPoints2D(ClientPos, TargetClientPos);

            if (Distance <= 1)
            {
                if (TargetClient.GetRoleplay().EquippedWeapon != null)
                {
                    Session.Shout("*Agarra a " + TargetClient.GetHabbo().Username + "'s " + TargetClient.GetRoleplay().EquippedWeapon.PublicName + " y lo golpe para esposarlo*", 37);

                    if (RoleplayManager.ConfiscateWeapons)
                    {
                        using (var DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                        {
                            DB.SetQuery("UPDATE `rp_weapons_owned` SET `can_use` = '0' WHERE `user_id` = @userid AND `base_weapon` = @baseweapon LIMIT 1");
                            DB.AddParameter("userid", TargetClient.GetHabbo().Id);
                            DB.AddParameter("baseweapon", TargetClient.GetRoleplay().EquippedWeapon.Name.ToLower());
                            DB.RunQuery();
                        }

                        TargetClient.GetRoleplay().EquippedWeapon = null;
                        TargetClient.GetRoleplay().OwnedWeapons = null;
                        TargetClient.GetRoleplay().OwnedWeapons = TargetClient.GetRoleplay().LoadAndReturnWeapons();
                    }
                    else
                        TargetClient.GetRoleplay().EquippedWeapon = null;
                }
                Session.Shout("*Saca las esposas de su cinturón y las envuelve alrededor de las manos de " + TargetClient.GetHabbo().Username + "'s para detenerlo*", 37);
                TargetClient.GetRoleplay().Cuffed = true;
                TargetClient.GetRoleplay().CuffedTimeLeft = 8;
                TargetClient.GetRoleplay().TimerManager.CreateTimer("cuff", 1000, false);
                if (TargetClient.GetRoomUser() != null)
                    TargetClient.GetRoomUser().ApplyEffect(590);
                return;
            }
            else
            {
                Session.SendWhisper("Usted debe acercarse a este ciudadano con el fin de esposarlos", 1);
                return;
            }
            #endregion
        }
    }
}