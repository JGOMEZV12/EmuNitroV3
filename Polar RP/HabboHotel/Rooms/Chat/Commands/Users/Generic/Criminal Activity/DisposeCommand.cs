using System;
using System.Linq;
using System.Text;
using System.Drawing;
using Polar.Utilities;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using System.Threading;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Criminal
{
    class DisposeCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_criminal_activity_dispose"; }
        }

        public string Parameters
        {
            get { return "%drug%"; }
        }

        public string Description
        {
            get { return "Elimina las droga seleccionada (weed/cocaine/all)."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            string Type = "all";
            if (Params.Length > 1)
                Type = Params[1].ToLower();

            switch (Type)
            {
                #region Weed
                case "marihuana":
                case "weed":
                    {
                        if (Session.GetRoleplay().Weed <= 0)
                        {
                            Session.SendWhisper("¡No tienes hierba para deshacerse!", 1);
                            break;
                        }

                        Session.GetRoleplay().Weed = 0;
                        Session.Shout("*Saca toda su hierba de su bolsillo y la tira al piso*", 4);
                        break;
                    }
                #endregion

                #region Cocaine
                case "cocaine":
                    {
                        if (Session.GetRoleplay().Cocaine <= 0)
                        {
                            Session.SendWhisper("No tienes cocaína para eliminar", 1);
                            break;
                        }

                        Session.GetRoleplay().Cocaine = 0;
                        Session.Shout("*Saca toda su cocaína de su bolsillo y se deshace de ella*", 4);
                        break;
                    }
                #endregion

                #region All Drugs
                case "all":
                    {
                        if (Session.GetRoleplay().Weed <= 0 && Session.GetRoleplay().Cocaine <= 0 && Session.GetRoleplay().Heroina <= 0)
                        {
                            Session.SendWhisper("¡No tienes drogas para eliminar!", 1);
                            break;
                        }

                        Session.GetRoleplay().Weed = 0;
                        Session.GetRoleplay().Cocaine = 0;
                        Session.GetRoleplay().Heroina = 0;
                        Session.Shout("*Saca todas sus drogas de su bolsillo y las tira al piso*", 4);
                        break;
                    }
                #endregion

                #region Default
                default:
                    {
                        Session.SendWhisper("¡Este tipo de droga no existe!", 1);
                        break;
                    }
                #endregion
            }
        }
    }
}