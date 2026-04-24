using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.Communication.Packets.Outgoing.Navigator;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Administrators
{
    class AdminTaxiCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_admin_taxi"; }
        }

        public string Parameters
        {
            get { return "%room_id%"; }
        }

        public string Description
        {
            get { return "Al instante te lleva a la identificación de la habitación deseada."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            UInt32 RoomId = 0;

            if (Params.Length == 1)
            {
                Session.SendWhisper("¡Ups, se le olvidó ingresar un ID de habitación!", 1);
                return;
            }

            if (!UInt32.TryParse(Params[1].ToString(), out RoomId))
            {
                Session.SendWhisper("Introduzca un numero válido", 1);
                return;
            }

            if (!RoleplayManager.GenerateRoom(Convert.ToInt32(RoomId), out Room TargetRoom))
            {
                Session.SendWhisper("[TAXI] Lo sentimos, no pudimos encontrar esa habitación", 1);
                return;
            }

            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes taxiar mientras estás muerto!", 1);
                return;
            }

            if (Session.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes taxi mientras estás encarcelado!", 1);
                return;
            }

            if (Session.GetRoleplay().CurAlcohol >= Session.GetRoleplay().MaxAlcohol)
            {
                Session.SendWhisper("¡Estas borracho! Estas tan borracho que ni tu iphone encuentras para llamar un taxi", 1);
                Session.SendWhisper("Intenta fumar unos CIGARRILLOS para calmar tu estado de ebriedad", 1);
                return;
            }

            if (RoomId == Session.GetHabbo().CurrentRoomId)
            {
                Session.SendWhisper("¡Ya estás en esa habitación!", 1);
                return;
            }

            if (!Session.GetHabbo().Username.Contains("Ying"))
                Session.Shout("*Entra en su Carro Staff y unidades fuera*", 23);


            RoleplayManager.SendUserOld2(Session, (int)RoomId);
        }
    }
}