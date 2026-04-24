using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Polar.HabboHotel.Groups;
using Polar.Database.Interfaces;
using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboHotel.GameClients;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Gangs
{
    class GangTransferCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_gang_transfer"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Transfiere la propiedad de pandillas al encargado de la pandilla."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Variables
            Group Gang = GroupManager.GetGang(Session.GetRoleplay().GangId);
            GroupRank GangRank = GroupManager.GetGangRank(Session.GetRoleplay().GangId, Session.GetRoleplay().GangRank);
            #endregion

            #region Conditions
            if (Gang == null)
            {
                Session.SendWhisper("¡No eres parte de ninguna pandilla!", 1);
                return;
            }

            if (!GroupManager.HasGangCommand(Session, "gtransfer"))
            {
                Session.SendWhisper("¡No tienes permiso para hacer esto!", 1);
                return;
            }

            if (Params.Length < 1)
            {
                Session.SendWhisper("Escribe ':gtransfer yes' si estás seguro de que quieres renunciar a tu pandilla!", 1);
                return;
            }

            if (Params[1].ToString().ToLower() != "yes")
            {
                Session.SendWhisper("escribe ':gtransfer yes' si estás seguro de que quieres renunciar a tu pandilla!", 1);
                return;
            }

            if (Gang.Members.Values.Where(x => x.UserRank == 5).ToList().Count <= 0)
            {
                Session.SendWhisper("¡No hay encargado para transferir la pandilla!", 1);
                return;
            }
            #endregion

            #region Execute
            int GangCoFounder = Gang.Members.Values.FirstOrDefault(x => x.UserRank == 5).UserId;
            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(GangCoFounder);

            if (TargetClient == null || TargetClient.GetHabbo() == null || TargetClient.GetRoleplay() == null)
            {
                Session.SendWhisper("Este usuario no pudo ser encontrado, tal vez están fuera de línea.", 1);
                return;
            }

            Gang.TransferGangOwnership(Session, TargetClient);
            Session.SendWhisper("*Has transferido con éxito tu pandilla a " + TargetClient.GetHabbo().Username + "*", 1);

            foreach (int Member in Gang.Members.Keys)
            {
                GameClient Client = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Member);

                if (Client == null)
                    continue;

                Client.SendWhisper("[PANDILLA] Nuevo dueño: " + TargetClient.GetHabbo().Username + "!", 34);
            }
            #endregion
        }
    }
}