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
using System.Collections.Concurrent;
using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboHotel.Rooms.Chat.Commands;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Developers
{
    class ToDoCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_todo"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Opens the todo list."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            var ToDoList = ToDoManager.ToDoList;

            if (ToDoList == null)
            {
                Session.SendWhisper("Sorry, but the todo list failed to generate!", 1);
                return;
            }

            if (Params[0].ToLower() == "todo")
            {
                if (ToDoList.Count < 1)
                {
                    Session.SendWhisper("The todo list is currently empty! Please use ':todoadd [todo]' to add a new ToDo to the list!", 1);
                    return;
                }

                StringBuilder List = new StringBuilder();
                List.Append("----- TO DO LIST -----\n");
                List.Append("[USERNAME] [ID] [¥]TODO\n\n");

                foreach (ToDo ToDo in ToDoList.Values)
                {
                    List.Append("[" + PolarEnvironment.GetHabboById(ToDo.AddedBy).Username + "] [" + ToDo.Id + "]\n----- [¥] " + ToDo.String + "\n\n");
                }

                Session.SendMessage(new MOTDNotificationComposer(List.ToString()));
                return;
            }
            else if (Params[0].ToLower() == "todoadd" || Params[0].ToLower() == "addtodo" || Params[0].ToLower() == "tda")
            {
                if (Params.Length == 1)
                {
                    Session.SendWhisper("Please enter the todo you would like to add!", 1);
                    return;
                }

                ToDo NewToDo = new ToDo(0, CommandManager.MergeParams(Params, 1), Session.GetHabbo().Id, PolarEnvironment.GetUnixTimestamp());
                ToDoManager.AddNewTodo(NewToDo);

                Session.SendWhisper("You have successfully added a new ToDo!", 1);
                return;
            }
            else if (Params[0].ToLower() == "tododel" || Params[0].ToLower() == "deltodo" || Params[0].ToLower() == "deletetodo" || Params[0].ToLower() == "tododelete" || Params[0].ToLower() == "tdd")
            {
                if (Params.Length == 1)
                {
                    Session.SendWhisper("Please enter the todo id you would like to delete!", 1);
                    return;
                }

                int Id;
                if (!int.TryParse(Params[1], out Id))
                {
                    Session.SendWhisper("Please enter a valid number for the todo id!", 1);
                    return;
                }

                if (!ToDoList.ContainsKey(Id))
                {
                    Session.SendWhisper("Sorry, but this todo does not exist!", 1);
                    return;
                }

                ToDoManager.DeleteToDo(Id);
                Session.SendWhisper("You have successfully removed ToDo Id " + Id + "!", 1);
                return;
            }
        }
    }
}
