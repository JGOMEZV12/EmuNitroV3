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

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.General
{
    class HireCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_corp_hire"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Contrata a un usuario en tu empresa."; }
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

            if (!GroupManager.HasJobCommand(Session, "hire"))
            {
                Session.SendWhisper("Usted no es un rango lo suficientemente alto en su corporación para usar este comando", 1);
                return;
            }

            if (Session.GetRoleplay().JobId == TargetClient.GetRoleplay().JobId)
            {
                Session.SendWhisper("¡Este ciudadano no trabaja para usted!", 1);
                return;
            }

            if (TargetClient.GetRoleplay().OfferManager.ActiveOffers.ContainsKey("job"))
            {
                Session.SendWhisper("Este ciudadano ya se ha ofrecido un trabajo", 1);
                return;
            }

            var Job = GroupManager.GetJob(Session.GetRoleplay().JobId);
            var JobRank = GroupManager.GetJobRank(Session.GetRoleplay().JobId, 1);

            if (JobRank.HasCommand("guide"))
            {
                if (BlackListManager.BlackList.Contains(TargetClient.GetHabbo().Id))
                {
                    Session.SendWhisper("Lo sentimos, pero este usuario ha sido puesto en la lista negra", 1);
                    return;
                }
            }
            #endregion

            #region Execute
            TargetClient.GetRoleplay().OfferManager.CreateOffer("trabajo", Session.GetHabbo().Id, Job.Id);
            Session.Shout("*Ofrece a " + TargetClient.GetHabbo().Username + " Un trabajo como " + Job.Name + " " + JobRank.Name + "*", 4);
            TargetClient.SendWhisper("Se le ha ofrecido un trabajo como " + Job.Name + " " + JobRank.Name + "! RESPONDA: ':aceptar trabajo' para ser contratado", 1);
            #endregion
        }
    }
}