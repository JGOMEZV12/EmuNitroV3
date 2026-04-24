using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Polar.Utilities;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Pathfinding;
using Polar.HabboHotel.Rooms.Pathfinding;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;
using Polar.HabboHotel.Items.Crafting;
using Polar.Communication.Packets.Outgoing.Inventory.Furni;

namespace Polar.HabboHotel.Items.Interactor
{
    public class InteractorDeliveryBox : IFurniInteractor
    {
        public void OnPlace(GameClient Session, Item Item) { }

        public void OnRemove(GameClient Session, Item Item) { }

        public void OnTrigger(GameClient Session, Item Item, int Request, bool HasRights)
        {
            // FIX: Validar Session y GetHabbo() juntos al inicio, en el orden correcto
            if (Session == null || Session.GetHabbo() == null)
                return;

            RoomUser User = Item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (User == null)
                return;

            string Type = Item.DeliveryType;

            if (!Gamemap.TilesTouching(Item.GetX, Item.GetY, User.Coordinate.X, User.Coordinate.Y))
                User.MoveTo(Item.SquareInFront);
            else
            {
                User.SetRot(Rotation.Calculate(User.Coordinate.X, User.Coordinate.Y, Item.GetX, Item.GetY), false);

                switch (Type.ToLower())
                {
                    case "weapon":
                        {
                            if (!GroupManager.HasJobCommand(Session, "weapon") && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                            {
                                Session.SendWhisper("Sólo un trabajador de la tienda de armas está certificado para abrir esta caja de entrega", 1);
                                break;
                            }

                            var Weapon = RoleplayManager.DeliveryWeapon;

                            if (Weapon != null)
                                Session.Shout("*Abre la caja de entrega y saca una nueva " + Weapon.PublicName + "s*", 4);
                            else
                                Session.SendWhisper("Esta caja de entrega parece estar vacía", 1);

                            Item.GetRoom().GetRoomItemHandler().RemoveFurniture(null, Item.Id);

                            int NewStock = 50;

                            if (Weapon != null)
                            {
                                WeaponManager.Weapons[Weapon.Name.ToLower()].Stock = NewStock;

                                using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                                {
                                    dbClient.RunQuery("UPDATE `rp_weapons` SET `stock` = '" + NewStock + "' WHERE `name` = '" + Weapon.Name.ToLower() + "'");
                                }
                            }
                            RoleplayManager.UserWhoCalledDelivery = 0;
                            RoleplayManager.DeliveryWeapon = null;
                            RoleplayManager.CalledDelivery = false;
                            break;
                        }
                    default:
                        {
                            Session.SendWhisper("¡El contenido de esta caja de entrega no se pudo encontrar!", 1);
                            break;
                        }
                }
            }
        }

        public void OnWiredTrigger(Item Item) { }
    }
}
