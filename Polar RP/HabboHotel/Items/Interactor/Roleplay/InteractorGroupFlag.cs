using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Polar.Utilities;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Inventory.Furni;
using Polar.HabboHotel.Users.Effects;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Turfs;

namespace Polar.HabboHotel.Items.Interactor
{
    public class InteractorGroupFlag : IFurniInteractor
    {
        public void OnPlace(GameClient Session, Item Item)
        {
        }

        public void OnRemove(GameClient Session, Item Item)
        {
        }

        public void OnTrigger(GameClient Session, Item Item, int Request, bool HasRights)
        {
            // Verificación de que el ítem es una bandera de grupo
            if (!Item.GetBaseItem().ItemName.Contains("army_c15_groupflag"))
                return;

            if (Session == null || Session.GetHabbo() == null || Session.GetRoleplay() == null)
                return;

            // Obtener el usuario dentro de la sala
            RoomUser User = Item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (User == null)
                return;

            // Verificar si la habitación tiene Turf habilitado (territorio de pandillas)
            if (!Item.GetRoom().TurfEnabled)
            {
                Session.SendWhisper("¡La habitación en la que te encuentras no es un territorio de pandillas!", 1);
                return;
            }

            // Verificar si el usuario pertenece a una pandilla
            if (Session.GetRoleplay().GangId <= 0)
            {
                Session.SendWhisper("¡No perteneces a ninguna pandilla!", 1);
                return;
            }

            // Verificar si el usuario está tocando el ítem
            if (!Gamemap.TilesTouching(Item.GetX, Item.GetY, User.Coordinate.X, User.Coordinate.Y))
            {
                if (Item.ExtraData == "" || Item.ExtraData == "0" || Item.ExtraData == "1")
                    if (User.CanWalk)
                    {
                        User.MoveTo(Item.SquareInFront);
                    }
                return;
            }

            // Verificar si el usuario tiene el tiempo de cooldown activo para capturar
            if (Session.GetRoleplay().TryGetCooldown("capturing", true))
                return;

            #region Conditions

            #region Group Conditions
            Group Gang = GroupManager.GetGang(Session.GetRoleplay().GangId);
            if (Gang == null)
            {
                Session.SendWhisper("No perteneces a ninguna banda para capturar territorios.", 1);
                return;
            }

            // Verificar si la banda está en quiebra
            if (Gang.BankRuptcy || Gang.Balance <= 0)
            {
                Session.SendWhisper("¡Tu banda está en banca rota! No pueden seguir operando en ella.", 1);
                return;
            }
            #endregion

            #region Basic Conditions
            // Verificaciones básicas de estado del jugador
            if (Session.GetRoleplay().Cuffed)
            {
                Session.SendWhisper("No puedes hacer eso mientras estás esposad@", 1);
                return;
            }

            if (!Session.GetRoomUser().CanWalk)
            {
                Session.SendWhisper("Al parecer no puedes ni moverte para hacer eso.", 1);
                return;
            }

            if (Session.GetRoleplay().PassiveMode)
            {
                Session.SendWhisper("No puedes capturar en modo pasivo.", 1);
                return;
            }

            if (Session.GetRoleplay().Pasajero)
            {
                Session.SendWhisper("¡No puedes hacer eso mientras vas de pasajer@!", 1);
                return;
            }

            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes hacer esto mientras estás muert@!", 1);
                return;
            }

            if (Session.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes hacer eso mientras estás encarcelad@", 1);
                return;
            }

            if (Session.GetRoleplay().DrivingCar)
            {
                Session.SendWhisper("¡No puedes hacer eso mientras conduces!", 1);
                return;
            }

            if (Session.GetRoleplay().IsWorking)
            {
                Session.SendWhisper("¡No puedes hacer eso mientras trabajas!", 1);
                return;
            }
            #endregion

            // Verificar que no esté capturando otro territorio
            if (Session.GetRoleplay().TurfCapturing)
            {
                Session.SendWhisper("Ya te encuentras capturando el territorio. ¡Procura no moverte!", 1);
                return;
            }

            // Verificar si el territorio ya está siendo capturado
            if (User.GetRoom().TurfCapturing)
            {
                Session.SendWhisper("¡El territorio ya está siendo capturado!", 1);
                return;
            }
            #endregion

            // Si el territorio ya pertenece a la misma banda
            if (User.GetRoom().Group != null)
            {
                if (User.GetRoom().Group == Gang)
                {
                    Session.SendWhisper("¡Este barrio ya pertenece a tu banda!", 1);
                    return;
                }
                else
                {
                    // Alertar a los integrantes de la banda atacada
                    foreach (GameClient client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                    {
                        if (client == null || client.GetHabbo() == null)
                            continue;

                        List<Groups.Group> thegroup = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(client.GetHabbo().Id);

                        if (thegroup == null || thegroup.Count <= 0)
                            continue;

                        if (thegroup[0] != User.GetRoom().Group)
                            continue;

                        if (client.GetRoleplay().DisableRadio == true)
                            continue;

                        // Notificar por radio
                        client.SendWhisper("[RADIO] ¡Están atacando nuestro " + User.GetRoom().Name + "! ¡Vamos a defenderlo!", 30);
                    }
                }
            }

            // Iniciar la captura del territorio
            User.GetRoom().TurfCapturing = true;
            User.GetRoom().TurfUserAtackerId = Session.GetHabbo().Id;

            // Mensaje de inicio de captura
            Session.Shout("*Empieza apoderarse del territorio para que sea de: " + Gang.Name + " ¡No te muevas! [-10 Energia]*", 4);
            Session.GetRoleplay().CurEnergy -= 10;
            Session.SendWhisper("¡Te quedan 2 minutos para capturar este territorio de pandillas!", 1);

            // Actualizar el estado del jugador y la sala
            Session.GetRoleplay().TurfCapturing = true;
            Session.GetRoleplay().LoadingTimeLeft = RoleplayManager.TurfCapTime;
            Session.GetRoleplay().TurfFlagId = Item.Id;

            // Crear el temporizador para la captura del territorio
            Session.GetRoleplay().TimerManager.CreateTimer("turfcapture", 1000, false);
        }

        // Método no utilizado (OnWiredTrigger)
        public void OnWiredTrigger(Item Item)
        {
        }
    }
}
