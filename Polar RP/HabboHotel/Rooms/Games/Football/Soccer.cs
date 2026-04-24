using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.HabboHotel.Rooms.Games.Teams;
using Polar.HabboHotel.Items.Wired;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Rooms.Games.Football
{
    public class Soccer
    {
        private Room _room;
        private Item[] gates;
        private ConcurrentDictionary<int, Item> _balls;
        private bool _gameStarted;
        public Soccer(Room room)
        {
            this._room = room;
            this.gates = new Item[4];
            this._balls = new ConcurrentDictionary<int, Item>();
            this._gameStarted = false;
        }
        public bool GameIsStarted
        {
            get { return this._gameStarted; }
        }
        public void StopGame(bool userTriggered = false)
        {
            this._gameStarted = false;
            if (!userTriggered)
                _room.GetWired().TriggerEvent(WiredBoxType.TriggerGameEnds, null);
        }
        public void StartGame()
        {
            this._gameStarted = true;
        }
        public void AddBall(Item item)
        {
            this._balls.TryAdd(item.Id, item);
        }
        public void RemoveBall(int itemID)
        {
            Item Item = null;
            this._balls.TryRemove(itemID, out Item);
        }
        public void OnUserWalk(RoomUser User)
        {
            if (User == null)
                return;
            foreach (Item item in this._balls.Values.ToList())
            {
                int ballNewX = 0;
                int ballNewY = 0;
                int ballTravelDistance = 2;
                if (Gamemap.TileDistance(User.X, User.Y, item.GetX, item.GetY) == 0)
                {
                    if (User.X == item.GetX && User.Y == item.GetY && User.GoalX == item.GetX && User.GoalY == item.GetY)
                    {
                        ballTravelDistance = 6;
                    }

                    if (User.RotBody == 4)
                    {
                        ballNewX = User.X;
                        ballNewY = User.Y + ballTravelDistance;
                    }
                    else if (User.RotBody == 6)
                    {
                        ballNewX = User.X - ballTravelDistance;
                        ballNewY = User.Y;
                    }
                    else if (User.RotBody == 0)
                    {
                        ballNewX = User.X;
                        ballNewY = User.Y - ballTravelDistance;
                    }
                    else if (User.RotBody == 2)
                    {
                        ballNewX = User.X + ballTravelDistance;
                        ballNewY = User.Y;
                    }
                    else if (User.RotBody == 1)
                    {
                        ballNewX = User.X + ballTravelDistance;
                        ballNewY = User.Y - ballTravelDistance;
                    }
                    else if (User.RotBody == 7)
                    {
                        ballNewX = User.X - ballTravelDistance;
                        ballNewY = User.Y - ballTravelDistance;
                    }
                    else if (User.RotBody == 3)
                    {
                        ballNewX = User.X + ballTravelDistance;
                        ballNewY = User.Y + ballTravelDistance;
                    }
                    else if (User.RotBody == 5)
                    {
                        ballNewX = User.X - ballTravelDistance;
                        ballNewY = User.Y + ballTravelDistance;
                    }
                    if (!this._room.GetRoomItemHandler().CheckPosItem(User.GetClient(), item, ballNewX, ballNewY, item.Rotation, false, false))
                    {
                        // TODO: Calculate how far we can go?
                        if (User.RotBody == 0)
                        {
                            ballNewX = User.X;
                            ballNewY = User.Y + 1;
                        }
                        else if (User.RotBody == 2)
                        {
                            ballNewX = User.X - 1;
                            ballNewY = User.Y;
                        }
                        else if (User.RotBody == 4)
                        {
                            ballNewX = User.X;
                            ballNewY = User.Y - 1;
                        }
                        else if (User.RotBody == 6)
                        {
                            ballNewX = User.X + 1;
                            ballNewY = User.Y;
                        }
                        else if (User.RotBody == 5)
                        {
                            ballNewX = User.X + 1;
                            ballNewY = User.Y - 1;
                        }
                        else if (User.RotBody == 3)
                        {
                            ballNewX = User.X - 1;
                            ballNewY = User.Y - 1;
                        }
                        else if (User.RotBody == 7)
                        {
                            ballNewX = User.X + 1;
                            ballNewY = User.Y + 1;
                        }
                        else if (User.RotBody == 1)
                        {
                            ballNewX = User.X - 1;
                            ballNewY = User.Y + 1;
                        }
                    }
                    if (item.GetRoom().GetGameMap().ValidTile(ballNewX, ballNewY))
                    {
                        MoveBall(item, ballNewX, ballNewY, User);
                    }
                }
            }
        }
        public void RegisterGate(Item item)
        {
            if (gates[0] == null)
            {
                item.team = TEAM.BLUE;
                gates[0] = item;
            }
            else if (gates[1] == null)
            {
                item.team = TEAM.RED;
                gates[1] = item;
            }
            else if (gates[2] == null)
            {
                item.team = TEAM.GREEN;
                gates[2] = item;
            }
            else if (gates[3] == null)
            {
                item.team = TEAM.YELLOW;
                gates[3] = item;
            }
        }
        public void UnRegisterGate(Item item)
        {
            switch (item.team)
            {
                case TEAM.BLUE:
                    {
                        gates[0] = null;
                        break;
                    }
                case TEAM.RED:
                    {
                        gates[1] = null;
                        break;
                    }
                case TEAM.GREEN:
                    {
                        gates[2] = null;
                        break;
                    }
                case TEAM.YELLOW:
                    {
                        gates[3] = null;
                        break;
                    }
            }
        }
        public void onGateRemove(Item item)
        {
            switch (item.GetBaseItem().InteractionType)
            {
                case InteractionType.FOOTBALL_GOAL_RED:
                case InteractionType.footballcounterred:
                    {
                        _room.GetGameManager().RemoveFurnitureFromTeam(item, TEAM.RED);
                        break;
                    }
                case InteractionType.FOOTBALL_GOAL_GREEN:
                case InteractionType.footballcountergreen:
                    {
                        _room.GetGameManager().RemoveFurnitureFromTeam(item, TEAM.GREEN);
                        break;
                    }
                case InteractionType.FOOTBALL_GOAL_BLUE:
                case InteractionType.footballcounterblue:
                    {
                        _room.GetGameManager().RemoveFurnitureFromTeam(item, TEAM.BLUE);
                        break;
                    }
                case InteractionType.FOOTBALL_GOAL_YELLOW:
                case InteractionType.footballcounteryellow:
                    {
                        _room.GetGameManager().RemoveFurnitureFromTeam(item, TEAM.YELLOW);
                        break;
                    }
            }
        }
        public void MoveBall(Item item, int newX, int newY, RoomUser user)
        {
            if (item == null || user == null)
                return;
            if (!_room.GetGameMap().ItemCanBePlacedHere(newX, newY))
                return;
            Point oldRoomCoord = item.Coordinate;
            if (oldRoomCoord.X == newX && oldRoomCoord.Y == newY)
                return;
            double NewZ = _room.GetGameMap().Model.SqFloorHeight[newX, newY];
            _room.SendMessage(new SlideObjectBundleComposer(item.Coordinate.X, item.Coordinate.Y, item.GetZ, newX, newY, NewZ, item.Id, item.Id, item.Id));
            item.ExtraData = "11";
            item.UpdateNeeded = true;
            _room.GetRoomItemHandler().SetFloorItem(null, item, newX, newY, item.Rotation, false, false, false, false);
            this._room.OnUserShoot(user, item);
        }
        public void Dispose()
        {
            Array.Clear(gates, 0, gates.Length);
            gates = null;
            _room = null;
            _balls.Clear();
            _balls = null;
        }
    }
}