using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Weapons;

namespace Polar.Communication.Packets.Outgoing.Inventory.Weapons
{
    internal class WeaponsComposer : ServerPacket
    {
        public WeaponsComposer(GameClient Session)
     : base(ServerPacketHeader.RPItemList)
        {
            var roleplay = Session.GetRoleplay();
            var ownedWeapons = roleplay.OwnedWeapons.Values;
            var equippedWeapon = roleplay.EquippedWeapon;
            var equippedArmorIndex = roleplay.EquippedArmorIndex; // Necesitas crear esta propiedad

            int consumiblesAcumulados = 0;
            if (roleplay.Caramelos > 0) consumiblesAcumulados++;
            if (roleplay.Weed > 0) consumiblesAcumulados++;
            if (roleplay.Cigarettes > 0) consumiblesAcumulados++;
            if (roleplay.Cocaine > 0) consumiblesAcumulados++;
            if (roleplay.Pildoras > 0) consumiblesAcumulados++;

            // AHORA: Los chalecos se cuentan 1 por cada unidad (NO acumulados)
            int totalItems = ownedWeapons.Count + roleplay.Armor + consumiblesAcumulados;

            base.WriteInteger(totalItems);

            // Escribir armas
            foreach (Weapon weapon in ownedWeapons)
            {
                WriteWeapon(weapon, equippedWeapon);
            }

            // ESCRIBIR ARMOR (1 ITEM POR CADA CHALECO)
            for (int i = 0; i < roleplay.Armor; i++)
            {
                int armorId = 5000 + i;  // ID único para cada chaleco
                bool isEquipped = (i == roleplay.EquippedArmorIndex); // Solo UNO equipado

                base.WriteInteger(armorId);                          // ID único
                base.WriteString("chaleco");                          // Name
                base.WriteInteger(1);                                 // Cantidad (1 por item)
                base.WriteString("armor");                            // Type
                base.WriteBoolean(true);                              // Equippable
                base.WriteBoolean(isEquipped);                        // Equipped (solo 1 true)
            }

            // Escribir consumibles (1 item por tipo con cantidad acumulada)
            WriteConsumible(1000, "caramelo", "caramelo", roleplay.Caramelos);
            WriteConsumible(2000, "marihuana", "weed", roleplay.Weed);
            WriteConsumible(3000, "cigarro", "cigarro", roleplay.Cigarettes);
            WriteConsumible(4000, "cocaina", "cocaina", roleplay.Cocaine);
            WriteConsumible(6000, "pildora", "pildora", roleplay.Pildoras);
        }

        // Método específico para armas
        private void WriteWeapon(Weapon weapon, Weapon equippedWeapon)
        {
            base.WriteInteger(Convert.ToInt32(weapon.ID));      // ID
            base.WriteString(weapon.Name);                      // Name
            base.WriteInteger(Convert.ToInt32(weapon.TotalBullets)); // TotalBullets
            base.WriteString("weapon");                          // Type

            base.WriteBoolean(true);                             // Equippable
            base.WriteBoolean(equippedWeapon != null &&
                                 equippedWeapon.Name == weapon.Name ? true : false); // Equipped
        }

        private void WriteConsumible(int id, string name, string type, int cantidad)
        {
            if (cantidad <= 0) return;

            base.WriteInteger(id);
            base.WriteString(name);
            base.WriteInteger(cantidad);  // Cantidad total
            base.WriteString("others");
            base.WriteBoolean(false);
            base.WriteBoolean(false);
        }
    }
}