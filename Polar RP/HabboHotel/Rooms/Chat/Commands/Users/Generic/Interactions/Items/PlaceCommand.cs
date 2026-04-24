using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Generic;

using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Farming;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Interactions.Items
{
    class PlaceCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_interactions_place"; }
        }

        public string Parameters
        {
            get { return "%item%"; }
        }

        public string Description
        {
            get { return "Le permite colocar ciertos artículos en el piso."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Params.Length == 1 && Params[0].ToLower() != "jailbreak" && Params[0].ToLower() != "repair")
            {
                if (Params[0].ToLower() == "plantar")
                {
                    Session.SendWhisper("¡Por favor ingrese el ID de la planta que desea usar! Escriba ':farmingstats' para los ids de la planta", 1);
                    return;
                }
                else
                {
                    Session.SendWhisper("Por favor, escriba el elemento que desea colocar", 1);
                    return;
                }
            }

            string Type;
            if (Params[0].ToLower() == "jailbreak")
                Type = "dynamite";
            else if (Params[0].ToLower() == "repair")
                Type = "repair";
            else if (Params[0].ToLower() == "plantar")
                Type = "plantar";
            else
                Type = Params[1].ToLower();
            #endregion

            switch (Type)
            {
                #region Dynamite
                case "dynamite":
                case "explosivo":
                    {
                        if (Session.GetRoleplay().Dynamite < 1)
                        {
                            Session.SendWhisper("¡No tienes dinamita para colocar!", 1);
                            return;
                        }

                        if (Session.GetRoleplay().IsJailed)
                        {
                            Session.SendWhisper("¡No puedo explotar la cerca, estas preso debe ser un complice!", 1);
                            return;
                        }

                        if (JailbreakManager.JailbreakActivated)
                        {
                            Session.SendWhisper("¡Un escape ya está en marcha!", 1);
                            JailbreakManager.JailbreakActivated = false;
                            return;
                        }

                        //int RoomId = Convert.ToInt32(RoleplayData.GetData("jail", "outsideroomid"));
                        int X = Convert.ToInt32(RoleplayData.GetData("jailbreak", "xposition"));
                        int Y = Convert.ToInt32(RoleplayData.GetData("jailbreak", "yposition"));
                        double Z = Convert.ToDouble(RoleplayData.GetData("jailbreak", "zposition"));
                        int Rot = Convert.ToInt32(RoleplayData.GetData("jailbreak", "rotation"));

                        string MyCity = Room.City;

                        HabboRoleplay.RPRoom.RPRoom Data;
                        int ToJailback = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetJailBack(MyCity, out Data);


                        if (Session.GetRoomUser() == null)
                            return;

                        if (Room.Id != ToJailback)
                        {
                            Session.SendWhisper("Usted no está fuera de la prisión para iniciar un jailbreak!", 1);
                            return;
                        }

                        Item BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "bb_rnd_tele" && x.Coordinate == Session.GetRoomUser().Coordinate);

                        if (BTile == null)
                        {
                            Session.SendWhisper("Usted debe estar parado encima de una baldosa de la rotura de la cárcel para comenzar la rotura de la cárcel", 1);
                            return;
                        }

                        List <GameClient> CurrentJailedUsers = PolarEnvironment.GetGame().GetClientManager().GetClients.Where(x => x != null && x.GetHabbo() != null && x.GetRoleplay() != null && x.GetRoleplay().IsJailed).ToList();

                        if (CurrentJailedUsers == null || CurrentJailedUsers.Count <= 0)
                        {
                            Session.SendWhisper("¡No hay nadie en la cárcel ahora mismo!", 1);
                            return;
                        }

                        Session.GetRoleplay().Dynamite--;
                        JailbreakManager.JailbreakActivated = true;
                        Session.Shout("*Coloca un explosivo en la valla, con el objetivo de dejar salir a los prisioneros *", 4);
                        Session.SendWhisper("¡Mata a todos los policías que veas, hasta que logres el objetivo que es liberar a los convictos! (NO TE VAYAS DE LA SALA)");
                        if (!Session.GetRoleplay().WantedFor.Contains("jailbreaking"))
                            Session.GetRoleplay().WantedFor = Session.GetRoleplay().WantedFor + "jailbreaking, ";

                        Item Item = RoleplayManager.PlaceItemToRoom(null, 6088, 0, X, Y, Z, Rot, false, Room.Id, false, "0");
                        Item Item2 = RoleplayManager.PlaceItemToRoom(null, 3011, 0, X, Y, 0, Rot, false, Room.Id, false, "0");

                        object[] Items = { Session, Item, Item2 };
                        RoleplayManager.TimerManager.CreateTimer("dynamite", 500, false, Items);
                        break;
                    }
                #endregion

                #region Fence Repair
                case "repair":
                case "reparar":
                    {
                        if (!JailbreakManager.FenceBroken)
                        {
                            Session.SendWhisper("No hay valla en necesidad de reparación", 1);
                            return;
                        }

                        if (!GroupManager.HasJobCommand(Session, "guide") && !Session.GetHabbo().GetPermissions().HasRight("corporation_rights"))
                        {
                            Session.SendWhisper("¡Sólo un oficial de policía tiene el equipo adecuado para reparar esta valla!", 1);
                            return;
                        }

                        if (!Session.GetRoleplay().IsWorking && !Session.GetHabbo().GetPermissions().HasRight("corporation_rights"))
                        {
                            Session.SendWhisper("Usted debe estar trabajando para reparar esta valla!", 1);
                            return;
                        }

                        if (Session.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("repair"))
                        {
                            Session.SendWhisper("¡Ya estás reparando la valla!", 1);
                            return;
                        }

                        Item BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "bb_rnd_tele" && x.Coordinate == Session.GetRoomUser().Coordinate);

                        if (BTile == null)
                        {
                            Session.SendWhisper("Usted debe estar de pie en la parte superior de un azulejo de reparación para comenzar a reparar la valla!", 1);
                            return;
                        }

                        Session.Shout("*Comienza a reparar la valla, soldando los tubos rotos*", 4);
                        Session.SendWhisper("Usted tiene 2 minutos restantes hasta que reparar esta valla! (PIDA REFUERZOS PARA QUE LO AYUDEN PROTEGIENDOLO)", 1);
                        Session.GetRoleplay().TimerManager.CreateTimer("repair", 1000, false, BTile.Id);

                        if (Session.GetRoomUser().CurrentEffect != 59)
                            Session.GetRoomUser().ApplyEffect(59);
                        break;
                    }
                #endregion

                #region Plant
                case "plantar":
                    {
                        int Id;
                        if (!int.TryParse(Params[1], out Id))
                        {
                            Session.SendWhisper("¡Por favor ingrese el ID de la planta que desea usar! Escriba ':farmingstats' para los ids de la planta.", 1);
                            break;
                        }

                        if (!Session.GetRoleplay().FarmingStats.HasSeedSatchel)
                        {
                            Session.SendWhisper("Usted no tiene una bolsa de semillas para llevar las semillas comprela en la sala: 111", 1);
                            return;
                        }

                        if (Id == 0)
                        {
                            Session.SendWhisper("Usted ha guardado todas sus semillas de nuevo en su bolsa de semillas", 1);
                            Session.GetRoleplay().FarmingItem = null;
                            break;
                        }

                        FarmingItem Item = FarmingManager.GetFarmingItem(Id);

                        ItemData Furni;

                        if (Item.BaseItem == null)
                        {
                            Session.SendWhisper("¡Por favor ingrese el ID de la planta que desea usar! Escriba ':farmingstats' para los ids de la planta.", 1);
                            return;
                        }

                        if (!PolarEnvironment.GetGame().GetItemManager().GetItem(Item.BaseItem, out Furni) || Item == null)
                        {
                            Session.SendWhisper("¡Por favor ingrese el ID de la planta que desea usar! Escriba ':farmingstats' para los ids de la planta.", 1);
                            return;
                        }

                        if (Item.LevelRequired > Session.GetRoleplay().FarmingStats.Level)
                        {
                            Session.SendWhisper("Lo siento, pero no tienes un nivel de cultivo lo suficientemente alto para esta semilla!", 1);
                            return;
                        }

                        Session.GetRoleplay().FarmingItem = Item;

                        int Amount;
                        if (FarmingManager.GetSatchelAmount(Session, false, out Amount))
                        {
                            if (Amount <= 0)
                            {
                                Session.SendWhisper("¡No tienes semillas para sembrar! Comprar algunos en el supermercado.", 1);
                                Session.GetRoleplay().FarmingItem = null;
                                break;
                            }
                            else
                            {
                                Session.SendWhisper("Usted ha preparado su " + Amount + " " + Furni.PublicName + " Semillas para plantar", 1);
                                break;
                            }
                        }
                        else
                        {
                            Session.SendWhisper("¡No tienes semillas para sembrar! Comprar algunos en el supermercado.", 1);
                            Session.GetRoleplay().FarmingItem = null;
                            break;
                        }
                    }
                #endregion

                #region Default
                default:
                    {
                        Session.SendWhisper("Lo sentimos, pero este elemento no se puede colocar abajo!", 1);
                        break;
                    }
                #endregion
            }
        }
    }
}