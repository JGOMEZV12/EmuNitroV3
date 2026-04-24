using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Interactions.Marriage
{
    class JugarCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_interactions_marry"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Comienza a jugar en videojuegos."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (!Session.GetRoleplay().NearItem("gmesps3", 1))
            {
                Session.SendWhisper("¡Debes estar frente a un playstation 3!");
                return;
            }

            if (Session.GetRoleplay().Animo > 100)
            {
                Session.SendWhisper("Ya tienes tu estado de animo al máximo ¡Estás feliz!.", 1);
                return;
            }
            #endregion

            #region Execute
            Session.SendWhisper("¡Acabas de subir al máximo tu estado de animo!", 1);

            Session.GetRoleplay().CurEnergy = Session.GetRoleplay().MaxEnergy;
            Session.GetRoleplay().Animo = 100; 
            Session.GetRoomUser().ApplyEffect(907);
            Session.Shout("*Toma los controles de su videojuegos y comienza a jugar [100% Felicidad] [Máximo de Energía]", 4);
            #endregion
        }
    }
}