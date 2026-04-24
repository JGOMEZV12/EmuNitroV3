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
    class EstadoCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_criminal_activity_smoke"; }
        }

        public string Parameters
        {
            get { return "%drug%"; }
        }

        public string Description
        {
            get { return "Le permite demostrar a los demás como está su animo."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            string Type = "genial";
            if (Params.Length > 1)
                Type = Params[1].ToLower();

            if (Type == "genial")
            {
                if (Session.GetRoleplay().CurEnergy < 100)
                {
                    Session.SendWhisper("¡Debes tener minimo 100% de energía para sentirte Genial!", 1);
                    return;
                }

                if (Session.GetRoleplay().IsDead)
                {
                    Session.SendWhisper("¡No puedes sentirte genial si estas muerto!", 1);
                    return;
                }

            }

            if (Type == "enamorado")
            {

                if (Session.GetRoleplay().IsDead)
                {
                    Session.SendWhisper("¡No puedes completar esta acción mientras estás muerto!", 1);
                    return;
                }
            }

            if (Type == "molesto" || Type == "arrecho")
            {
                if (Session.GetRoleplay().IsDead)
                {
                    Session.SendWhisper("¡No puedes completar esta acción mientras estás muerto!", 1);
                    return;
                }
            }


            if (Type == "triste")
            {
                if (Session.GetRoleplay().IsDead)
                {
                    Session.SendWhisper("¡No puedes completar esta acción mientras estás muerto!", 1);
                    return;
                }
            }

            if (Type == "aburrido")
            {
                if (Session.GetRoleplay().IsDead)
                {
                    Session.SendWhisper("¡No puedes completar esta acción mientras estás muerto!", 1);
                    return;
                }
            }
            #endregion

            #region Execute
            switch (Type)
            {
                #region Genial
                case "genial":
                    {
                        Session.GetRoomUser().ApplyEffect(907);
                        Session.Shout("*Me siento Genial y contento de estar en " + PolarEnvironment.GetConfig().data["hotel.name"] + "*", 4);

                        break;
                    }
                #endregion

                #region Enamorado
                case "enamorado":
                    {
                        Session.GetRoomUser().ApplyEffect(908);
                        Session.Shout("*Me siento enamorado*", 4);

                        break;
                    }
                #endregion

                #region Molesto
                case "molesto":
                case "arrecho":
                    {
                        Session.GetRoomUser().ApplyEffect(909);
                        Session.Shout("*¡Estoy molesto/arrecho no quiero que me digan nada!*", 4);

                        break;
                    }
                #endregion

                #region Apenado
                case "apenado":
                case "sonrojado":
                    {
                        Session.GetRoomUser().ApplyEffect(910);
                        Session.Shout("*¡Estoy apenado jajaja, lo siento! |*", 4);

                        break;
                    }
                #endregion

                #region Triste
                case "triste":
                    {
                        Session.GetRoomUser().ApplyEffect(911);
                        Session.Shout("*¡Estoy triste me siento muy mal!*", 4);

                        break;
                    }
                #endregion

                #region Aburrido
                case "aburrido":
                    {
                        Session.GetRoomUser().ApplyEffect(913);
                        Session.Shout("*¡Estoy demasiado aburrido, hagamos algo divertido!*", 4);

                        break;
                    }
                #endregion

                #region Default
                default:
                    {
                        Session.SendWhisper("el comando es :Estado 'genial' 'enamorado' 'molesto' 'sonrojado' 'triste'", 1);
                        break;
                    }
                #endregion
            }
            #endregion
        }
    }
}