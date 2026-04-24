using System.Drawing;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Pathfinding;
using System;
using Polar.HabboRoleplay.Houses;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Items.Interactor
{
    public class InteractorHouseSing : IFurniInteractor
    {
        public void OnPlace(GameClient Session, Item Item) { }

        public void OnRemove(GameClient Session, Item Item) { }

        public void OnTrigger(GameClient Session, Item Item, int Request, bool HasRights)
        {
            // FIX: Validar Session antes de usarlo
            if (Session == null || Session.GetRoleplay() == null)
                return;

            if (Session.GetRoomUser() == null)
                return;

            var User = Session.GetRoomUser();

            if (Session.GetRoleplay().WebSocketConnection != null)
            {
                if (Rooms.Gamemap.TilesTouching(Item.GetX, Item.GetY, User.X, User.Y))
                {
                    var House = PolarEnvironment.GetGame().GetHouseManager().GetHouseBySignItem(Item.Id);

                    if (House != null)
                    {
                        string Owner = GetHouseOwner(House.OwnerId);

                        if (Owner != null)
                        {
                            Session.GetRoleplay().HouseSignId = House.ItemId;
                            Session.GetRoleplay().HouseOwner = Owner;
                            Session.GetRoleplay().ViewHouse = true;
                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_house", "open");
                        }
                        else
                        {
                            Session.SendNotification("((Ha ocurrido un problema al buscar la información de la casa o terreno. Contacte con un Administrador))");
                        }
                    }
                    else
                    {
                        Session.SendNotification("((No se pudo encontrar la casa asociada a este cartel. Contacte con un Administrador))");
                    }
                }
                else
                {
                    User.MoveTo(Item.SquareBehind);
                }
            }
            else
            {
                Session.SendWhisper("((El WebSocket del servidor está Offline. Por favor contacte con un Administrador))", 1);
            }
        }

        private string GetHouseOwner(int ownerId)
        {
            var habbo = PolarEnvironment.GetHabboById(ownerId);
            if (habbo != null)
                return habbo.Username;
            return PolarEnvironment.GetUsernameById(ownerId);
        }

        public void OnWiredTrigger(Item Item) { }
    }
}
