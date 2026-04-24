using System;
using System.Linq;
using log4net;
using Polar.HabboHotel;
using Polar.HabboRoleplay.Bots.Manager;
using Polar.Communication.Packets.Outgoing.Moderation;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Moderation;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Rooms.Chat.Commands;
using Polar.HabboRoleplay.Misc;
using System.Data;
using System.Collections.Generic;
using Polar.HabboHotel.Items;
using System.Text;
using Polar.HabboRoleplay.Vehicles;

namespace Polar.Core
{
    public class ConsoleCommandHandler
    {
        private static readonly ILog log = LogManager.GetLogger("Polar.Core.ConsoleCommandHandler");

        public static void InvokeCommand(string inputData)
        {
            if (string.IsNullOrEmpty(inputData))
                return;
            try
            {
                #region Command parsing
                string[] parameters = inputData.Split(' ');
                switch (parameters[0].ToLower())
                {
                    #region targeted
                    case "relampago":
                    case "targeted":
                        PolarEnvironment.GetGame().GetTargetedOffersManager().Initialize(PolarEnvironment.GetDatabaseManager().GetQueryReactor());
                        break;
                    #endregion targeted

                    #region crackable
                    case "crackable":
                    case "ecotron":
                    case "pinata":
                        PolarEnvironment.GetGame().GetPinataManager().Initialize(PolarEnvironment.GetDatabaseManager().GetQueryReactor());
                        PolarEnvironment.GetGame().GetFurniMaticRewardsMnager().Initialize(PolarEnvironment.GetDatabaseManager().GetQueryReactor());
                        break;
                    #endregion crackable

                    #region General

                    #region wha
                    case "wha":
                        {
                            string Notice = CommandManager.MergeParams(parameters, 1);

                            foreach (var client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                            {
                                if (client == null)
                                    continue;

                                if (client.GetHabbo() == null)
                                    continue;

                                if (client.LoggingOut)
                                    continue;

                                client.SendWhisper("[HOTEL Alert] " + Notice, 33);
                            }

                            log.Info("Sent WHISPER Alert: '" + Notice + "'");

                            break;
                        }
                    #endregion

                    #region ban
                    case "pban":
                    case "ban":
                        {
                            string User = parameters[1].ToLower();
                            GameClient Session = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(User);
                            Session.SendNotification("¡Usted ha sido baneado!");

                            if (Session != null)
                            {
                                #region Online Ban
                                if (Session.GetHabbo() == null)
                                    return;

                                PolarEnvironment.GetGame().GetModerationManager().BanUser("[SISTEMA AUTOMÁTICO]", ModerationBanType.USERNAME, Session.GetHabbo().Username, "Automatic Ban", 1538641023.14615);
                                
                                if (Session != null)
                                {
                                    Session.Disconnect(true);
                                }
                                #endregion
                            }
                            else
                            {
                                #region Offline Ban
                                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                                {
                                    double ExpireTimestamp = 1538641023.14615;
                                    dbClient.SetQuery("REPLACE INTO `bans` (`bantype`, `value`, `reason`, `expire`, `added_by`,`added_date`) VALUES ('user', '" + User + "','Automatic console ban', " + ExpireTimestamp + ", '[SISTEMA AUTOMÁTICO]', '" + PolarEnvironment.GetUnixTimestamp() + "');");
                                    dbClient.RunQuery();
                                }
                                #endregion
                            }


                            log.Info("Baneado exitosamente: '" + User + "'");

                            break;
                        }
                    #endregion

                    #region unban
                    case "unban":
                        {
                            string User = parameters[1].ToLower();

                            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                            {
                                dbClient.SetQuery("SELECT `ip_last`,`machine_id` FROM `users` where `username` = '" + User + "' LIMIT 1");
                                var Row = dbClient.getRow();

                                var IPLast = Convert.ToString(Row["ip_last"]);
                                var MachineID = Convert.ToString(Row["machine_id"]);

                                dbClient.RunQuery("DELETE FROM `bans` WHERE `value` = '" + User + "' OR `value` = '" + IPLast + "' OR `value` = '" + MachineID + "'");
                            }

                             log.Info("Successfully unbanned: '" + User + "'!");

                            break;
                        }
                    #endregion

                    #region vehicles
                    case "vehicles":
                    case "cars":
                    case "vehiculo":
                        {
                            VehicleManager.Initialize();
                            PolarEnvironment.GetGame().GetVehiclesOwnedManager().Init();
                            break;
                        }
                    #endregion

                    #region dc
                    case "dc":
                        {
                            #region Variables
                            string User = parameters[1].ToLower();
                            GameClient Session = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(User);
                            #endregion

                            #region Conditions
                            if (Session == null)
                            {
                                log.Info("'" + User + "' is offline!");
                                return;
                            }
                            #endregion

                            Session.Disconnect(true);
                            log.Info("'" + User + "' was successfully disconnected!");

                            break;
                        }
                    #endregion

                    #region senduser
                    case "senduser":
                        {
                            #region Variables
                            string User = parameters[1].ToLower();
                            GameClient Session = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(User);

                            int RoomId = 1;
                            if (!int.TryParse(parameters[2], out RoomId))
                            {
                                log.Info("Invalid RoomID!");
                                return;
                            }

                            RoomId = Convert.ToInt32(parameters[2]);
                            if (!HabboRoleplay.Misc.RoleplayManager.GenerateRoom(RoomId, out Room TargetRoom))
                            {
                                log.Info("Room is NULL!");
                                return;
                            }
                            #endregion

                            #region Null checks
                            if (Session == null)
                            {
                                log.Info("'" + User + "' is offline!");
                                return;
                            }

                            if (Session.GetRoleplay() == null)
                            {
                                log.Info("'" + User + "' is offline!");
                                return;
                            }

                            if (Session.GetHabbo() == null)
                            {
                                log.Info("'" + User + "' is offline!");
                                return;
                            }
                            #endregion

                            #region Conditions
                            if (TargetRoom == Session.GetHabbo().CurrentRoom)
                            {
                                log.Info("This user is already in that room!");
                                return;

                            }

                            if (Session.GetRoleplay().IsDead)
                            {
                                Session.GetRoleplay().IsDead = false;
                                Session.GetRoleplay().ReplenishStats();
                            }

                            if (Session.GetRoleplay().IsJailed)
                            {
                                Session.GetRoleplay().IsJailed = false;
                                Session.GetRoleplay().JailedTimeLeft = 0;
                            }
                            #endregion

                            RoleplayManager.SendUserOld2(Session, RoomId, "You have been sent to room " + TargetRoom.Name + " [RoomID: " + RoomId + "] by an Administrator!");
                            log.Info("Successfully sent: '" + Session.GetHabbo().Username + "' to RoomID: '" + RoomId + "'");
                            break;
                        }
                    #endregion

                    case "mutant":
                        {
                            //PolarEnvironment.GetGame().GetAntiMutant().Init();
                            log.Info("Todos los looks del servidor se han actualizado.");
                            break;
                        }


                    #region kill
                    case "kill":
                        {

                            #region Variables
                            string User = parameters[1].ToLower();
                            GameClient Session = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(User);
                            #endregion

                            #region Null Checks
                            if (Session == null)
                            {
                                log.Info("'" + User + "' Esta desconectado");
                                return;
                            }

                            if (Session.GetRoleplay() == null)
                            {
                                log.Info("'" + User + "' Esta desconectado");
                                return;
                            }

                            if (Session.GetHabbo() == null)
                            {
                                log.Info("'" + User + "' Esta desconectado");
                                return;
                            }
                            #endregion

                            Session.GetRoleplay().CurHealth = 0;
                            log.Info("Asesino correctamente: '" + Session.GetHabbo().Username + "'");

                            break;
                        }
                    #endregion


                    #endregion

                    #region Server Management

                    #region stop
                    case "stop":
                    case "shutdown":
                        {
                            Logging.DisablePrimaryWriting(true);
                            Logging.WriteLine("El servidor está guardando muebles de los usuarios, habitaciones, etc. ¡ESPERE A CERRAR EL SERVIDOR, NO SALGA DEL PROCESO EN EL TASK MANAGER!", ConsoleColor.Yellow);
                            PolarEnvironment.PerformShutDown(false, false);
                            break;
                        }
                    #endregion

                    #region stop
                    case "reiniciar":
                    case "restart":
                        {
                            Logging.DisablePrimaryWriting(true);
                            Logging.WriteLine("El servidor está guardando muebles de los usuarios, habitaciones, etc. ¡ESPERE A CERRAR EL SERVIDOR, NO SALGA DEL PROCESO EN EL TASK MANAGER!", ConsoleColor.Yellow);
                            PolarEnvironment.PerformShutDown(false, true);
                            break;
                        }
                    #endregion

                    #region configs
                    case "config":
                    case "settings":
                        ExtraSettings.RunExtraSettings();
                        CatalogSettings.RunCatalogSettings();
                        break;
                    #endregion

                    #region refresh
                    case "update":
                    case "refresh":
                        {

                            if (parameters.Length < 2)
                            {
                                Console.WriteLine("Invalid console syntax!: :update <toupdate>");
                                return;
                            }

                            string ToRefresh = parameters[1].ToLower();
                            
                            switch (ToRefresh)
                            {

                                #region p
                                case "ranks":
                                case "rights":
                                case "permissions":
                                    {

                                        PolarEnvironment.GetGame().GetPermissionManager().Init();

                                        log.Info("Successfully refreshed rights!");

                                        break;
                                    }
                                #endregion

                                #region rpbots
                                case "bots":
                                case "bot":
                                case "rpbots":
                                case "rpbot":
                                    {

                                        log.Info("Successfully refreshed Roleplay bots!");
                                        RoleplayBotManager.Initialize(true);

                                        lock (PolarEnvironment.GetGame().GetClientManager().GetClients)
                                        {
                                            foreach (var client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                                            {
                                                if (client == null || client.GetHabbo() == null)
                                                    continue;

                                                client.SendWhisper("[System] All Bots have been reloaded!", 33);
                                            }
                                        }

                                    }
                                break;
                                #endregion

                                #region users
                                case "users":
                                    {

                                        foreach(GameClient user in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                                        {
                                            if (user == null)
                                                continue;

                                            if (user.LoggingOut)
                                                continue;

                                            if (user.GetRoleplay() != null)
                                            {
                                                if (user.GetRoleplay().WebSocketConnection != null)
                                                    user.GetRoleplay().SendTopAlert("Everybody in the hotel has been disconnected!");
                                            }

                                            user.Disconnect(false);
                                        }

                                        break;
                                    }
                                #endregion

                                #region chats
                                case "chats":
                                    {
                                        HabboRoleplay.Web.Util.ChatRoom.WebSocketChatManager.Initialiaze();
                                        break;
                                    }
                                #endregion

                                #region botwait
                                case "botwait":
                                    {

                                        foreach(RoomUser User in RoleplayBotManager.DeployedRoleplayBots.Values)
                                        {
                                            if (User == null)
                                                continue;

                                            if (User.GetBotRoleplay() == null)
                                                continue;

                                            log.Info(User.GetBotRoleplay().Name + " (BotWait: " +
                                                User.GetBotRoleplay().RoomStayTime + "/" +
                                                User.GetBotRoleplay().RoomStayInterval + ") : (BotCooldown: " +
                                                User.GetBotRoleplay().RoamCooldown + ")" + " : (BotVirtualId: " + User.GetBotRoleplay().VirtualId + ")");
                                        }

                                    }
                                    break;
                                #endregion

                                #region commands
                                case "commands":
                                    {
                                        PolarEnvironment.GetGame().GetChatManager()._commands = new CommandManager(":");
                                        Console.WriteLine("Successfully refreshed commands!", ConsoleColor.Yellow);
                                    }
                                    break;
                               #endregion
                            }
                        }
                        break;
                    #endregion

                    #region sockets
                    case "sockets":
                        {
                            Logging.WriteLine("Websocket count:" + PolarEnvironment.GetGame().GetWebEventManager()._webSockets.Count + "", ConsoleColor.Yellow);
                            string Append = "";

                            


                            Logging.WriteLine(Append);
                        }
                        break;
                    #endregion
                    
                    #region sockets
                    case "chats":
                        {
                            Logging.WriteLine("TOTAL Chat count:" + HabboRoleplay.Web.Util.ChatRoom.WebSocketChatManager.RunningChatRooms.Count + "", ConsoleColor.Red);
                            string Append = "";

                            foreach (HabboRoleplay.Web.Util.ChatRoom.WebSocketChatRoom ChatRoom in HabboRoleplay.Web.Util.ChatRoom.WebSocketChatManager.RunningChatRooms.Values)
                            {
                                Append += "\n\nNombre chat: " + ChatRoom.ChatName + "\n";
                                Append += "Chat dueño: " + ChatRoom.ChatOwner + "\n";
                                Append += "Usarios: " + ChatRoom.ChatUsers.Count + "\n";
                                Append += "Chatlog count: " + ChatRoom.ChatLogs.Count + "\n";
                                Append += "--------Users--------";
                                foreach(GameClient User in ChatRoom.ChatUsers.Keys)
                                {
                                    Append += "\nUsername: " + User.GetHabbo().Username + "";
                                }
                                Append += "\n---------------------";
                            }


                            Logging.WriteLine(Append, ConsoleColor.Red);
                        }
                        break;
                    #endregion

                    #region clear
                    case "clear":
                        {
                            Console.Clear();
                            break;
                        }
                    #endregion

                    #region alert
                    case "alert":
                        {
                            string Notice = inputData.Substring(6);

                            PolarEnvironment.GetGame().GetClientManager().SendMessage(new BroadcastMessageAlertComposer(PolarEnvironment.GetGame().GetLanguageLocale().TryGetValue("console.noticefromadmin") + "\n\n" + Notice));

                            log.Info("Se envió la alerta correctamente");
                            break;
                        }
                    #endregion

                    #region furni and catalog commands

                    #region Catalog stuff
                    case "updatefurni":
                        {
                            PolarEnvironment.GetGame().GetItemManager().UpdateFurniSpecial();
                            PolarEnvironment.GetGame().GetItemManager().ProductDataMaker();
                            PolarEnvironment.GetGame().GetItemManager().DownloadFurnis();
                            log.Info("Completely updated all furni!");
                            break;
                        }
                    case "furnidata":
                        {
                            PolarEnvironment.GetGame().GetItemManager().UpdateFurniSpecial();
                            log.Info("Successfully updated special furnidata.");
                            break;
                        }
                    case "furni":
                        {
                            PolarEnvironment.GetGame().GetItemManager().DownloadFurnis();
                            log.Info("Successfully updated database with furnidata.");
                            break;
                        }
                    case "productdata":
                        {
                            PolarEnvironment.GetGame().GetItemManager().ProductDataMaker();
                            break;
                        }
                    #endregion

                    #region FurniFix2
                    case "furnifix2":
                        {
                            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                            {
                                dbClient.SetQuery("SELECT * FROM `furniture_old`");
                                DataTable Table = dbClient.getTable();

                                dbClient.SetQuery("SELECT * FROM `items`");
                                DataTable ToFix = dbClient.getTable();

                                int Count = 0;
                                int Count2 = 0;

                                if (Table != null && ToFix != null)
                                {
                                    Dictionary<int, int> Changes = new Dictionary<int, int>();

                                    int counter = 0;
                                    int counter2 = 0;

                                    foreach (DataRow Row in Table.Rows)
                                    {
                                        string Name = Convert.ToString(Row["item_name"]);
                                        int Id = Convert.ToInt32(Row["id"]);

                                        ItemData Data = null;
                                        if (!PolarEnvironment.GetGame().GetItemManager().GetItem(Name, out Data))
                                            continue;

                                        if (Changes.ContainsKey(Id))
                                            continue;

                                        counter++;
                                        Changes.Add(Id, Data.Id);

                                        if (counter > 100)
                                        {
                                            counter2++;
                                            counter = 0;
                                            Console.WriteLine("dictionary: " + (counter2 * 100) + " items added so far");
                                        }
                                    }
                                    Console.WriteLine("Made the dictionary");

                                    StringBuilder String = new StringBuilder();

                                    foreach (DataRow Row in ToFix.Rows)
                                    {
                                        int DataId = Convert.ToInt32(Row["id"]);
                                        int UserId = Convert.ToInt32(Row["user_id"]);
                                        int RoomId = Convert.ToInt32(Row["room_id"]);
                                        int BaseItem = Convert.ToInt32(Row["base_item"]);
                                        string ExtraData = Row["extra_data"].ToString();
                                        int X = Convert.ToInt32(Row["x"]);
                                        int Y = Convert.ToInt32(Row["y"]);
                                        double Z = Convert.ToDouble(Row["z"]);
                                        int Rot = Convert.ToInt32(Row["rot"]);
                                        string WallPos = Row["wall_pos"].ToString();
                                        int LimitedNum = Convert.ToInt32(Row["limited_number"]);
                                        int LimitedStack = Convert.ToInt32(Row["limited_stack"]);

                                        if (Changes.ContainsKey(BaseItem))
                                        {
                                            Count++;

                                            if (Count >= 100)
                                            {
                                                Count = 0;
                                                Count2++;
                                                Console.WriteLine("Updated " + (Count2 * 100) + " items so far!");
                                            }

                                            String.Append("INSERT INTO `items_new` VALUES ('" + DataId + "','" + UserId + "','" + RoomId + "','" + Changes[BaseItem] + "','" + ExtraData + "','" + X + "','" + Y + "','" + Z + "','" + Rot + "','" + WallPos + "','" + LimitedNum + "','" + LimitedStack + "');\n");
                                        }
                                    }
                                    ConsoleWriter.Writer.WriteProductData(String.ToString());
                                }
                                Console.WriteLine("finished");
                            }
                            break;
                        }
                    #endregion

                    #region FurniFix
                    case "furnifix":
                        {
                            Dictionary<int, int> Changes = new Dictionary<int, int>();

                            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                            {
                                dbClient.SetQuery("SELECT * FROM `furniture_fix`");
                                DataTable Table = dbClient.getTable();

                                if (Table != null)
                                {
                                    foreach (DataRow Row in Table.Rows)
                                    {
                                        string Name = Convert.ToString(Row["item_name"]);
                                        int OldId = Convert.ToInt32(Row["id"]);
                                        int NewId = Convert.ToInt32(Row["new_id"]);

                                        if (NewId == 0 && !Changes.ContainsKey(OldId))
                                        {
                                            dbClient.SetQuery("SELECT * FROM `furniture` WHERE `item_name` = '" + Name.ToLower() + "' LIMIT 1");
                                            DataRow Data = dbClient.getRow();

                                            if (Data != null)
                                            {
                                                int RealNewId = Convert.ToInt32(Data["id"]);

                                                Changes.Add(OldId, RealNewId);
                                            }
                                        }
                                    }
                                    Console.WriteLine("made the dictionary");

                                    foreach (var Pair in Changes)
                                    {
                                        dbClient.RunQuery("UPDATE `furniture_fix` SET `new_id` = '" + Pair.Value + "' WHERE `id` = '" + Pair.Key + "' LIMIT 1");
                                    }

                                    Console.WriteLine("FINISHED MAKING FURNITURE FIX TABLE");
                                }
                            }
                            break;
                        }
                    #endregion

                    #endregion

                    #endregion

                    #region Default
                    default:
                        {
                            log.Error(parameters[0].ToLower() + " es un comando desconocido o no compatible. Escriba ayuda para obtener más información");
                            break;
                        }
                    #endregion
                }
                #endregion
            }
            catch (Exception e)
            {
                log.Error("Error in command [" + inputData + "]: " + e);
            }
        }
    }
}