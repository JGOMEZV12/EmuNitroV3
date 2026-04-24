using Polar.HabboHotel.Users;

namespace Polar.HabboHotel.Rooms.Chat.Commands.VIP
{
    class FlagMeCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_flag_me"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Cambiar tu nombre debes pagar 250.000$"; }
        }

        public Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (!this.CanChangeName(Session.GetHabbo()))
            {
                Session.SendWhisper("Lo sentimos, parece que actualmente no tienes la opción de cambiar tu nombre de usuario.", 1);
                return Task.CompletedTask;
            }

            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_changename", "open");
            return Task.CompletedTask;
            /* if (Session.GetHabbo().Credits < 250000)
             {
                 Session.SendWhisper("No tienes 250.000$ para cambiar tu nombre de usuario.", 1);
                 return Task.CompletedTask;
             }

             Session.GetHabbo().ChangingName = true;
             Session.GetHabbo().Credits -= 250000;
             Session.GetHabbo().UpdateCreditsBalance();
             Session.SendNotification("Cambiar de indentidad cuesta: 10.000 créditos ya han sido descontados. además, Tenga en cuenta que si su nombre de usuario se considera inapropiado, será expulsado de inmediato.\r\rTambién tenga en cuenta que el personal NO cambiará su nombre de usuario nuevamente si tiene un problema con lo que ha elegido.\r\rCierra esta ventana y haz clic en ti mismo para comenzar a elegir un nuevo nombre de usuario.");
             Session.SendMessage(new UserObjectComposer(Session.GetHabbo()));*/


        }



        private bool CanChangeName(Habbo Habbo)
        {
            if (Habbo.Rank == 1 && Habbo.VIPRank == 1 && (Habbo.LastNameChange == 0 || (PolarEnvironment.GetUnixTimestamp() + 604800) > Habbo.LastNameChange))
                return true;
            else if (Habbo.Rank == 1 && Habbo.VIPRank == 2 && (Habbo.LastNameChange == 0 || (PolarEnvironment.GetUnixTimestamp() + 604800) > Habbo.LastNameChange))
                return true;
            else if (Habbo.GetPermissions().HasRight("mod_tool"))
                return true;

            return false;
        }

    }
}
