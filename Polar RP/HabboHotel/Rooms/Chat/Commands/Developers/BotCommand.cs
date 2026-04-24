using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms.AI;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Inventory.Pets;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Items;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Developers
{
    class BotCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_kick_pets"; }
        }

        public string Parameters
        {
            get { return "%command%"; }
        }

        public string Description
        {
            get { return "Testing bots"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {

            if (Params.Length == 1)
            {
                Session.SendWhisper("Oops, you forgot to enter a command!", 1);
                return;
            }

            string Command = Convert.ToString(Params[1]);

            switch(Command)
            {

                #region follow
                case "follow":
                case "followme":
                    {
                        RoomUser Target = null;

                        foreach (RoomUser Bot in Room.GetRoomUserManager()._bots.Values)
                        {
                            if (!Bot.IsRoleplayBot)
                                continue;

                            Target = Bot;
                        }

                        if (Target == null)
                        {
                            Session.SendWhisper("No roleplay bot to interact with was found, sorry!");
                            return;
                        }

                        Target.GetBotRoleplay().UserFollowing = Session;
                        Target.GetBotRoleplay().Following = true;

                        Session.Shout("*Tells " + Target.GetBotRoleplay().Name + " to follow me");

                        return;
                    }
                #endregion

                #region attack
                case "attack":
                case "attackme":
                case "fight":
                case "fightme":
                    {
                        RoomUser Target = null;

                        foreach (RoomUser Bot in Room.GetRoomUserManager()._bots.Values)
                        {
                            if (!Bot.IsRoleplayBot)
                                continue;

                            Target = Bot;
                        }

                        if (Target == null)
                        {
                            Session.SendWhisper("No roleplay bot to interact with was found, sorry!");
                            return;
                        }

                        Target.GetBotRoleplay().UserAttacking = Session;
                        Target.GetBotRoleplay().Roaming = false;
                        Target.GetBotRoleplay().Attacking = true;

                        Session.Shout("*Tells " + Target.GetBotRoleplay().Name + " to attack me");

                        return;
                    }
                #endregion

                #region tele
                case "randomtele":
                case "tele":
                    {

                        RoomUser Target = null;
                        Item Randtele = null;
                        foreach(RoomUser Bot in Room.GetRoomUserManager()._bots.Values)
                        {
                            if (!Bot.IsRoleplayBot)
                                continue;

                            Target = Bot;
                            
                        }

                        if (Target == null)
                        {
                            Session.SendWhisper("No roleplay bot to interact with was found, sorry!");
                            return;
                        }

                        foreach (Item Item in Room.GetRoomItemHandler().GetFloor.ToList())
                        {
                            if (Item == null || Item.GetBaseItem() == null)
                                continue;

                            if (Item.GetBaseItem().InteractionType != InteractionType.ARROW)
                                continue;

                            Randtele = Item;
                        }

                        if (Randtele == null)
                        {
                            Session.SendWhisper("No TELEPORT to interact with was found, sorry!");
                            return;
                        }


                        Target.GetBotRoleplay().TeleporterEntering = Randtele;
                        int LinkedTele = ItemTeleporterFinder.GetLinkedTele(Target.GetBotRoleplay().TeleporterEntering.Id, Room);
                        int TeleRoomId = ItemTeleporterFinder.GetTeleRoomId(LinkedTele, Room);

                        if (RoleplayManager.GenerateRoom(TeleRoomId, out Room NewRoom))
                        {
                            Target.GetBotRoleplay().TeleporterExiting = NewRoom.GetRoomItemHandler().GetItem(LinkedTele);
                            Target.GetBotRoleplay().Teleporting = true;
                        }

                        Session.Shout("*Tells " + Target.GetBotRoleplay().Name + " to enter an arrow*");

                        return;
                    }
                    #endregion

            }

        }
    }
}
