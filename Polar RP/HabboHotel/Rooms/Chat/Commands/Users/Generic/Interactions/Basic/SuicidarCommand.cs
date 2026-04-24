using System;
using System.Linq;
using Polar.HabboHotel.GameClients;


namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Interactions.Basic
{
    class SuicidarCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_interactions_kiss"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "Explota en pedazos"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions

            if (Session.GetRoleplay().TryGetCooldown("suicidar"))
                return;

            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes suicidarte si estas muerto!", 1);
                return;
            }

            if (Session.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes suicidarte si preso!", 1);
                return;
            }

            if (Session.GetRoomUser().Frozen == true)
            {
                Session.SendWhisper("¡No puedes suicidarte si estas paralizado!", 1);
                return;
            }

            if (Session.GetRoleplay().Cuffed == true)
            {
                Session.SendWhisper("¡No puedes suicidarte si estas esposado!", 1);
                return;
            }

            if (Session.GetHabbo().CurrentRoomId == 126)
            {
                Session.SendWhisper("¡No puedes suicidarte si estas en el infierno!", 1);
                return;
            }
            #endregion

            #region Live Feed
            foreach (GameClient client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
            {
                if (client == null)
                    continue;

                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(client, "event_feedcomposer", "alert|" + Session.GetHabbo().Username + "||" + "Se ha suicidado");
            }
            #endregion

            #region Execute
            Session.Shout(" *Presiona un botón y explota en mil pedazos [Va directo al hospital] * ", 27);
            //PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Session, "compose_combatlog|" + Session.GetHabbo().Username + "|Se ha suicidado:");
            Session.GetRoomUser().ApplyEffect(602);
            Session.GetHabbo().Credits -= 100;
            Session.GetHabbo().UpdateCreditsBalance();
            Session.GetRoleplay().CurHealth = 0;

            #endregion


        }
    }
}