using System.Linq;

using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Pathfinding;
using Polar.HabboHotel.Rooms.Pathfinding;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Items.Interactor
{
    public class InteractorNuking : IFurniInteractor
    {
        public void OnPlace(GameClient Session, Item Item)
        {

        }

        public void OnRemove(GameClient Session, Item Item)
        {

        }

        public void OnTrigger(GameClient Session, Item Item, int Request, bool HasRights)
        {
            if (Item.GetBaseItem().ItemName.ToLower() == "ads_igorswitch")
                HandleNuke(Session, Item, Request, HasRights);

            if (Item.GetBaseItem().ItemName.ToLower() == "wf_floor_switch2")
                HandleBreakDown(Session, Item, Request, HasRights);
        }

        public void HandleNuke(GameClient Session, Item Item, int Request, bool HasRights)
        {
            if (Session == null || Session.GetHabbo() == null || Item == null)
                return;

            Room Room = Session.GetHabbo().CurrentRoom;
            if (Room == null)
                return;

            RoomUser? User;
            User = Item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

            if (User == null)
                return;

            if (Gamemap.TilesTouching(Item.GetX, Item.GetY, User.X, User.Y))
            {
                if (Item.ExtraData == "")
                    Item.ExtraData = "0";

                if (Item.ExtraData == "0")
                {
                    int Minutes = RoleplayManager.NPACoolDown;

                    User.ClearMovement(true);
                    User.SetRot(Rotation.Calculate(User.Coordinate.X, User.Coordinate.Y, Item.GetX, Item.GetY), false);

                    // 135 Cycles approximately 1 minute
                    Item.ExtraData = "1";
                    Item.UpdateState(false, true);
                    Item.RequestUpdate(135 * Minutes, true);

                    // Start the nuking process.
                    object[] Params = { Session };
                    RoleplayManager.TimerManager.CreateTimer("nuking", 1000, false, Params);

                    Session.Shout("*Comienza a penetrar en la máquina nuclear, ordenando que destruya la ciudad*", 4);

                    #region Notify all on-duty NPA associates

                    lock (PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                    {
                        foreach (GameClient client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                        {
                            if (client == null || client.GetHabbo() == null || client.GetRoleplay() == null)
                                continue;

                            if (!Groups.GroupManager.HasJobCommand(client, "npa") && !client.GetHabbo().GetPermissions().HasRight("corporation_rights"))
                                continue;

                            if (!client.GetRoleplay().IsWorking)
                                continue;

                            if (client.GetRoleplay().DisableRadio)
                                continue;

                            client.SendWhisper("[Alerta RADIO] [NUKE] ¡Advertencia! ¡ATAQUE TERRORISTA! en la máquina nuclear y lo ha mandado para bombardear la ciudad, Averigüe quién es y detenerlos rápidamente.", 30);
                        }
                    }

                    #endregion
                }
                else
                    Session.SendWhisper("Vaya, parece que el interruptor nuking está en tiempo de reutilización. ¡Por favor, inténtelo de nuevo más tarde!", 1);
            }
            else
            {
                if (User.CanWalk)
                    User.MoveTo(Item.SquareInFront);
            }
        }

        public void HandleBreakDown(GameClient Session, Item Item, int Request, bool HasRights)
        {
            if (Session == null || Session.GetHabbo() == null || Item == null)
                return;

            Room Room = Session.GetHabbo().CurrentRoom;
            if (Room == null)
                return;

            RoomUser? User;
            User = Item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

            if (User == null)
                return;

            if (Gamemap.TilesTouching(Item.GetX, Item.GetY, User.X, User.Y))
            {
                if (Item.ExtraData == "")
                    Item.ExtraData = "0";

                if (Item.ExtraData == "0")
                {
                    int Minutes = RoleplayManager.NPACoolDown;

                    User.ClearMovement(true);
                    User.SetRot(Rotation.Calculate(User.Coordinate.X, User.Coordinate.Y, Item.GetX, Item.GetY), false);

                    // 135 Cycles approximately 1 minute
                    Item.ExtraData = "1";
                    Item.UpdateState(false, true);
                    Item.RequestUpdate(135 * Minutes, true);

                    // Start the nuking breakdown process.
                    object[] Params = { Session };
                    RoleplayManager.TimerManager.CreateTimer("nuking_bd", 1000, false, Params);

                    Session.Shout("*Comienza a romper las puertas del sistema nuclear*", 4);
                }
                else
                    Session.SendWhisper("Vaya, parece que el interruptor de avería nuclear está en tiempo de reutilización. Por favor inténtalo de nuevo más tarde.!", 1);
            }
            else
            {
                if (User.CanWalk)
                    User.MoveTo(Item.SquareInFront);
            }
        }

        public void OnWiredTrigger(Item Item)
        {

        }
    }
}