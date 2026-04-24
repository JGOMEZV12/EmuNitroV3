using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboHotel.GameClients;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Gangs
{
    class GangRankCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_gang_rank"; }
        }

        public string Parameters
        {
            get { return "%user% %rank%"; }
        }

        public string Description
        {
            get { return "Establece el rango de pandilla del usuario seleccionado."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length != 3)
            {
                Session.SendWhisper("Por favor, introduzca un nombre de usuario y un rango para asignarlos.", 1);
                return;
            }

            Group Gang = GroupManager.GetGang(Session.GetRoleplay().GangId);
            GroupRank GangRank = GroupManager.GetGangRank(Session.GetRoleplay().GangId, Session.GetRoleplay().GangRank);

            if (Gang == null)
            {
                Session.SendWhisper("¡No eres parte de ninguna pandilla!", 1);
                return;
            }

            if (Gang.Id <= 1000)
            {
                Session.SendWhisper("¡No eres parte de ninguna pandilla!", 1);
                return;
            }

            if (!GroupManager.HasGangCommand(Session, "grank"))
            {
                Session.SendWhisper("¡No eres lo suficientemente alto para usar este comando!", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);

            if (TargetClient == Session)
            {
                Session.SendWhisper("¡No puedes usar este comando en ti!", 1);
                return;
            }

            if (TargetClient == null || TargetClient.GetHabbo() == null || TargetClient.GetRoleplay() == null)
            {
                Session.SendWhisper("No se pudo encontrar a este usuario, quizás estén desconectados.", 1);
                return;
            }

            if (TargetClient.GetRoleplay().GangId != Gang.Id)
            {
                Session.SendWhisper("¡Este usuario no está en la misma pandilla que tú!", 1);
                return;
            }

            int Rank;

            if (int.TryParse(Params[2], out Rank))
            {
                if (Rank < 1 || Rank > 5)
                {
                    Session.SendWhisper("Por favor ingrese un rango válido (1 - 5)", 1);
                    return;
                }

                GroupRank NewGangRank = GroupManager.GetGangRank(Gang.Id, Rank);

                if (GroupManager.GetGangMembersByRank(Gang.Id, Rank).Count >= NewGangRank.Limit && NewGangRank.Limit != 0)
                {
                    Session.SendWhisper("¡Lo siento!Hay demasiados miembros en el " + NewGangRank.Name + " rango", 1);
                    return;
                }

                if (Rank > TargetClient.GetRoleplay().GangRank)
                    Session.Shout("*Asciende a " + TargetClient.GetHabbo().Username + " a " + NewGangRank.Name + " en la pandilla: " + Gang.Name + "*", 4);
                else
                    Session.Shout("*Degrada a " + TargetClient.GetHabbo().Username + " al cargo " + NewGangRank.Name + " de la pandilla: " + Gang.Name + "*", 4);

                TargetClient.GetRoleplay().GangRank = Rank;
                TargetClient.GetRoleplay().GangRequest = 0;

                Gang.UpdateGangMember(TargetClient.GetHabbo().Id);
            }
            else
            {
                Session.SendWhisper("coloca un rango válido (1 - 5)", 1);
                return;
            }
        }
    }
}