using System.Linq;
using Polar.Communication.Packets.Outgoing.Inventory.Purse;
using Polar.Database.Interfaces;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using System.Text;
using Polar.Communication.Packets.Outgoing.Notifications;

namespace Polar.HabboHotel.Rooms.Chat.Commands.SpecialRights
{
    internal class GlobalGiveCommand : IChatCommand
    {
        public string PermissionRequired => "command_give_coins";
        public string Parameters => "[MONEDA] [MONTO]";
        public string Description => "Enviar monedas a todos.";

        public async Task Execute(GameClient Session, Room room, string[] Params)
        {

            if (Params.Length == 1)
            {
                StringBuilder List = new StringBuilder();
                List.Append("¿Como puedo dar creditos, diamantes, duckets?\n········································································\n");
                List.Append(":globalgive credits [MONTO] - Créditos a todos los usuarios.\n········································································\n");
                List.Append(":globalgive diamonds [MONTO] - Diamantes a todos los usuarios.\n········································································\n");
                List.Append(":globalgive duckets [MONTO] - Duckets a todos los usuarios.\n········································································\n");
                Session.SendMessage(new MOTDNotificationComposer(List.ToString()));
                return;
            }

            string updateVal = Params[1];
            int amount;
            switch (updateVal.ToLower())
            {
                case "coins":
                case "credits":
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_give_coins"))
                        {
                            Session.SendWhisper("Vaya, parece que usted no tiene los permisos necesarios para utilizar este comando!");
                            break;
                        }
                        
                        if (int.TryParse(Params[2], out amount))
                        {
                        if (amount <= 0)
                        {
                            Session.SendWhisper("El monto no puede contener -", 1);
                            return;
                        }
                        foreach (GameClient client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                            {
                                client.GetHabbo().Credits += amount;
                                client.SendMessage(new CreditBalanceComposer(client.GetHabbo().Credits));
                                //client.SendMessage(new RoomNotificationComposer("command_notification_credits", "message", "Recibiste "+amount+" crédito(s) globales!"));
                            }
                            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                            {
                                dbClient.runFastQuery("UPDATE users SET credits = credits + " + amount);
                            }
                            break;
                        }
                        Session.SendWhisper("Vaya, que parece ser una cantidad no válida!");
                        break;

                case "pixels":
                case "duckets":
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_give_pixels"))
                        {
                            Session.SendWhisper("Vaya, parece que usted no tiene los permisos necesarios para utilizar este comando!");
                            break;
                        }
                        if (int.TryParse(Params[2], out amount))
                        {
                        if (amount <= 0)
                        {
                            Session.SendWhisper("El monto no puede contener -", 1);
                            return;
                        }
                        foreach (GameClient client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                            {
                                client.GetHabbo().Duckets += amount;
                                client.SendMessage(new HabboActivityPointNotificationComposer(
                                    client.GetHabbo().Duckets, amount));
                                //client.SendMessage(new RoomNotificationComposer("command_notification_credits", "message", "Recibiste " + amount + " ducket(s) globales!"));
                            }
                            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                            {
                                dbClient.runFastQuery("UPDATE users SET activity_points = activity_points + " + amount);
                            }
                            break;
                        }
                        Session.SendWhisper("Vaya, que parece ser una cantidad no válida!");
                        break;

                case "diamonds":
                case "diamantes":
                        if (!Session.GetHabbo().GetPermissions().HasCommand("command_give_diamonds"))
                        {
                            Session.SendWhisper("Vaya, parece que usted no tiene los permisos necesarios para utilizar este comando!");
                            break;
                        }
                        if (int.TryParse(Params[2], out amount))
                        {
                        if (amount <= 0)
                        {
                            Session.SendWhisper("El monto no puede contener -", 1);
                            return;
                        }
                        foreach (GameClient client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                            {
                                client.GetHabbo().Diamonds += amount;
                                client.GetHabbo().UpdateDiamondsBalance(amount);
                            }
                            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                            {
                                dbClient.runFastQuery("UPDATE users SET vip_points = vip_points + " + amount);
                            }
                            break;
                        }
                        Session.SendWhisper("Vaya, que parece ser una cantidad no válida!");
                        break;

            }
        }
    }
}