
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Army;
using Polar.HabboRoleplay.Weapons;

namespace Polar.Communication.Packets.Incoming.Inventory.Weapons
{
    internal class EquipRPItemEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            if (Session == null || Session.GetRoleplay() == null)
                return;

            string ItemId = Packet.PopString();  // ItemId del mensaje

            var roleplay = Session.GetRoleplay();

            switch(ItemId)
            {
                case "caramelo":
                case "caramelos":
                    {
                        PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(Session, ":caramelos");
                        break;
                    }
                case "pildora":
                case "pildoras":
                    {
                        PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(Session, ":consumir pildoras");
                        break;
                    }
                case "cigarro":
                case "cigarros":
                    {
                        PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(Session, ":consumir cigarros");
                        break;
                    }
                case "marihuana":
                    {
                        PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(Session, ":consumir marihuana");
                        break;
                    }
                case "cocaina":
                case "cocaine":
                    {
                        PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(Session, ":consumir cocaina");
                        break;
                    }
                case "heroina":
                case "heroine":
                    {
                        PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(Session, ":consumir heroina");
                        break;
                    }
                case "armor":
                case "chaleco":
                    {
                        PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(Session, ":ponerchaleco 1");
                        break;
                    }
                default:
                    // Validar que el item existe
                    if (!roleplay.OwnedWeapons.ContainsKey(ItemId))
                    {
                        // Item no encontrado
                        return;
                    }

                    Weapon weapon = roleplay.OwnedWeapons[ItemId];

                    // Equipar el arma

                    PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(Session, ":equipar " + ItemId);
                    // Notificar al cliente
                    break;

            }
            
            
        }
    }
}