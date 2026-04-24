using System;
using System.Linq;
using System.Data;
using System.Text;
using System.Collections.Generic;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Vehicles;
using Polar.HabboHotel.Rooms;
using Polar.Communication.Packets.Outgoing.Rooms.Avatar;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.GameClients;
using System.Drawing;
using System.Net;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Interactions.Self
{
    class DownCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_driving_down"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Permite bajarte del Vehículo donde vas de pasajero o [pasajero] Permite bajar a alguien de tu vehículo."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            // :bajar
            if (Params.Length == 1)
            {
                #region Conditions

                if (!Session.GetRoleplay().Pasajero)
                {
                    Session.SendWhisper("¡No eres pasajero de nadie!", 1);
                    return;
                }
                if (Session.GetRoleplay().Cuffed)
                {
                    Session.SendWhisper("¡No puedes bajarte del vehículo siendo Escoltad@ o Esposad@!", 1);
                    return;
                }
                if (Session.GetRoleplay().IsDead)
                {
                    Session.SendWhisper("¡No puedes hacer esto mientras estás muert@!", 1);
                    return;
                }
                if (Session.GetRoleplay().IsJailed)
                {
                    Session.SendWhisper("¡No puedes hacer eso mientras estás muert@!", 1);
                    return;
                }

                GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Session.GetRoleplay().ChoferName);
                if (TargetClient == null)
                {
                    Session.SendWhisper("Ha ocurrido un error en buscar a la persona, probablemente esté desconectada.", 1);
                    return;
                }

                if (Session.GetRoleplay().TryGetCooldown("pasajero"))
                    return;
                #endregion

                #region Execute

                // PASAJERO
                Session.GetRoleplay().Pasajero = false;
                Session.GetRoleplay().ChoferName = "";
                Session.GetRoleplay().ChoferID = 0;
                Session.GetRoomUser().CanWalk = true;
                Session.GetRoomUser().FastWalking = false;
                Session.GetRoomUser().TeleportEnabled = false;
                Session.GetRoomUser().AllowOverride = false;
                Session.GetRoleplay().Invisible = false;

                // Descontamos Pasajero
                TargetClient.GetRoleplay().PasajerosCount--;
                if (TargetClient.GetRoleplay().PasajerosCount <= 0)
                    TargetClient.GetRoleplay().Pasajeros = "";
                else
                    TargetClient.GetRoleplay().Pasajeros.Replace(Session.GetHabbo().Username + ";", "");

                // CHOFER 
                TargetClient.GetRoleplay().Chofer = (TargetClient.GetRoleplay().PasajerosCount <= 0) ? false : true;
                if(TargetClient.GetRoomUser() != null)
                    TargetClient.GetRoomUser().AllowOverride = (TargetClient.GetRoleplay().PasajerosCount <= 0) ? false : true;

                if(TargetClient.GetHabbo() != null)
                    RoleplayManager.Shout(Session, "*Baja del vehículo de " + TargetClient.GetHabbo().Username + "*", 5);

                Session.GetHabbo().CurrentRoom.SendMessage(new UsersComposer(Session.GetRoomUser()));
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "close");// WS FUEL
                Session.GetRoleplay().CooldownManager.CreateCooldown("pasajero", 1000, 5);
                #endregion
            }
            // :bajar [pasajero]
            else if (Params.Length == 2)
            {
                bool MyPasaj = false;
                #region Conditions
                if (!Session.GetRoleplay().DrivingCar)
                {
                    Session.SendWhisper("¡No te encuentras conduciendo para llevar a algún pasajero!", 1);
                    return;
                }
                if (!Session.GetRoleplay().Chofer)
                {
                    Session.SendWhisper("¡No llevas a ningún pasajero!", 1);
                    return;
                }

                if (Session.GetRoleplay().IsDead)
                {
                    Session.SendWhisper("¡No puedes hacer esto mientras estás muert@!", 1);
                    return;
                }
                if (Session.GetRoleplay().IsJailed)
                {
                    Session.SendWhisper("¡No puedes hacer eso mientras estás muert@!", 1);
                    return;
                }

                GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
                if (TargetClient == null)
                {
                    Session.SendWhisper("Ha ocurrido un error en buscar a la persona, probablemente esté desconectada.", 1);
                    return;
                }
                if (TargetClient == Session)
                {
                    Session.SendWhisper("No puedes bajarte a ti mismo como si fueses un pasajero.", 1);
                    return;
                }
                // Verificamos si la persona es mi pasajero
                //Vars
                string Pasajeros = Session.GetRoleplay().Pasajeros;
                string[] stringSeparators = new string[] { ";" };
                string[] result;
                result = Pasajeros.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);

                foreach (string psjs in result)
                {
                    GameClient PJ = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(psjs);
                    if (PJ != null)
                    {
                        if (PJ.GetHabbo().Username == TargetClient.GetHabbo().Username)
                            MyPasaj = true;

                    }
                }
                if (!MyPasaj)
                {
                    Session.SendWhisper("¡Esa persona no es tu pasajero!", 1);
                    return;
                }
                if (TargetClient.GetRoleplay().Cuffed)
                {
                    Session.SendWhisper("No puedes bajar a un Convicto escoltado del Vehículo. Detén el motor para bajar ambos de él.", 1);
                    return;
                }
                if (Session.GetRoleplay().TryGetCooldown("pasajero"))
                    return;
                #endregion

                #region Execute

                // PASAJERO
                TargetClient.GetRoleplay().Pasajero = false;
                TargetClient.GetRoleplay().ChoferName = "";
                TargetClient.GetRoleplay().ChoferID = 0;
                TargetClient.GetRoomUser().CanWalk = true;
                TargetClient.GetRoomUser().FastWalking = false;
                TargetClient.GetRoomUser().TeleportEnabled = false;
                TargetClient.GetRoomUser().AllowOverride = false;
                TargetClient.GetRoleplay().Invisible = false;

                // Descontamos Pasajero
                Session.GetRoleplay().PasajerosCount--;
                if (Session.GetRoleplay().PasajerosCount <= 0)
                    Session.GetRoleplay().Pasajeros = "";
                else
                {
                    StringBuilder builder = new StringBuilder(Session.GetRoleplay().Pasajeros);
                    builder.Replace(TargetClient.GetHabbo().Username + ";", "");
                    Session.GetRoleplay().Pasajeros = builder.ToString();
                }

                // CHOFER 
                Session.GetRoleplay().Chofer = (Session.GetRoleplay().PasajerosCount <= 0) ? false : true;
                Session.GetRoomUser().AllowOverride = (Session.GetRoleplay().PasajerosCount <= 0) ? false : true;

                // SI EL PASAJERO ES COMPAÑERO DE BASURERO
                if (TargetClient.GetRoleplay().IsBasuPasaj)
                    TargetClient.GetRoleplay().IsBasuPasaj = false;

                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(TargetClient, "event_vehicle", "close");// WS FUEL

                RoleplayManager.Shout(Session, "*Baja a " + TargetClient.GetHabbo().Username + " de su Vehículo*", 5);
                TargetClient.GetHabbo().CurrentRoom.SendMessage(new UsersComposer(TargetClient.GetRoomUser()));
                Session.GetRoleplay().CooldownManager.CreateCooldown("pasajero", 1000, 5);
                TargetClient.GetRoleplay().CooldownManager.CreateCooldown("pasajero", 1000, 5);                
                #endregion
            }
        }
    }
}
