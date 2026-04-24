
using Polar.Communication.Packets.Outgoing.Inventory.Weapons;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Army;
using Polar.HabboRoleplay.Weapons;

namespace Polar.Communication.Packets.Incoming.Inventory.Weapons
{
    internal class UnEquipRPItemEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            if (Session == null || Session.GetRoleplay() == null)
                return;

            string ItemId = Packet.PopString();  // ItemId del mensaje

            switch (ItemId)
            {
                case "armor":
                case "chaleco":
                    {
                        Session.GetRoleplay().Armor += 1;
                        Session.GetRoleplay().ChalecoPor = 0;
                        Session.SendWhisper("Se quita el chaleco antibalas.", 1);
                        HabboRoleplay.Misc.RoleplayManager.GetLookAndMotto(Session, "poof");
                        Session.GetRoleplay().UpdateInteractingUserDialogues();
                        Session.GetRoleplay().RefreshStatDialogue();
                        Session.SendMessage(new WeaponsComposer(Session));
                        break;
                    }
                default:
                    var roleplay = Session.GetRoleplay();
                    // Validar que el item existe
                    if (roleplay.EquippedWeapon == null)
                    {
                        Session.SendWhisper("¡No tienes un arma equipada!", 1);
                        return;
                    }
                    // Equipar el arma

                    PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(Session, ":desequipar " + ItemId);
                    // Notificar al cliente
                    break;

            }
        }
    }
}