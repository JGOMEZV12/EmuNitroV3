using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Guides;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.General
{
    class CheckMinutesCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_corp_checkminutes"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Comprueba la cantidad de minutos trabajados para uno de los trabajadores de su corporación."; }
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
            int JobRank = TargetClient.GetRoleplay().JobRank;

            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes hacer esto mientras estés muerto!", 1);
                return;
            }

            if (Session.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes hacer esto mientras estás encarcelado!", 1);
                return;
            }

            if (Session.GetHabbo().GetPermissions().HasRight("corporation_rights"))
            {
                if (!GroupManager.HasJobCommand(Session, "checkminutes"))
                 {
                     Session.SendWhisper("Usted no es un rango lo suficientemente alto en su corporación para usar este comando", 1);
                     return;
                 }
                if (Session.GetRoleplay().JobId != TargetClient.GetRoleplay().JobId)
               {
                   Session.SendWhisper("¡Este ciudadano no trabaja para usted!", 1);
                   return;
               }
            }

            if (Session.GetRoleplay().TryGetCooldown("checkminutes"))
                return;

            #endregion

            #region Execute

            Session.SendWhisper(TargetClient.GetHabbo().Username + " Ha trabajado en '" + GroupManager.GetJob(TargetClient.GetRoleplay().JobId).Name + "' empresa por " + String.Format("{0:N0}", TargetClient.GetRoleplay().TimeWorked) + " minutos!", 1);
            Session.GetRoleplay().CooldownManager.CreateCooldown("checkminutes", 1000, 5);

            #endregion
        }
    }
}