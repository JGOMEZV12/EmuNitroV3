using System;
using System.Linq;
using System.Data;
using System.Text;
using System.Collections.Concurrent;

using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Administrators
{
    class RPWeaponsCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_rpweapons"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "Proporciona una lista de sus armas de roleplay"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Se te olvidó introducir un nombre de usuario de la persona que quieres comprobar.", 1);
                return;
            }

            #region Variables
            ConcurrentDictionary<string, Weapon> Weapons = new ConcurrentDictionary<string, Weapon>();

            uint id = 0;
            string Username = Params[1];
            GameClients.GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Username);
            #endregion

            #region Generate Weapons Data
            if (TargetClient == null)
            {
                DataTable Weps = null;
                Weapons.Clear();

                using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("SELECT `id` FROM `users` where `username` = '" + Username + "' LIMIT 1");
                    var UserRow = dbClient.getRow();

                    if (UserRow == null)
                    {
                        Session.SendWhisper("Esta persona no existe", 1);
                        return;
                    }

                    int UserId = Convert.ToInt32(UserRow["id"]);

                    dbClient.SetQuery("SELECT * FROM `rp_weapons_owned` WHERE `user_id` = '" + UserId + "'");
                    Weps = dbClient.getTable();

                    if (Weps == null)
                    {
                        Session.SendWhisper("Sorry! Esta persona no tiene armas!", 1);
                        return;
                    }
                    else
                    {
                        foreach (DataRow Row in Weps.Rows)
                        {
                            id++;

                            string basename = Convert.ToString(Row["base_weapon"]);
                            string name = Convert.ToString(Row["name"]);
                            int mindam = Convert.ToInt32(Row["min_damage"]);
                            int maxdam = Convert.ToInt32(Row["max_damage"]);
                            int range = Convert.ToInt32(Row["range"]);
                            int totalbullets = Convert.ToInt32(Row["bullets"]);
                            bool canuse = PolarEnvironment.EnumToBool(Row["can_use"].ToString());
                            int wlife = Convert.ToInt32(Row["life"]);
                            bool isvip = PolarEnvironment.EnumToBool(Row["vip"].ToString());
                            int baulcar = Convert.ToInt32(Row["baul_car_id"]);
                            int effectid = Convert.ToInt32(Row["effectid"]);
                            if (!Weapons.ContainsKey(basename))
                            {
                                Weapon BaseWeapon = WeaponManager.getWeapon(basename);

                                if (BaseWeapon != null)
                                {
                                    Weapon Weapon = new Weapon(id, basename, name, BaseWeapon.FiringText, BaseWeapon.EquipText, BaseWeapon.UnEquipText, BaseWeapon.ReloadText, BaseWeapon.Energy, effectid <= 0 ? BaseWeapon.EffectID : effectid, BaseWeapon.HandItem, range, mindam, maxdam, BaseWeapon.ClipSize, BaseWeapon.ReloadTime, BaseWeapon.Cost, BaseWeapon.CostFine, BaseWeapon.Stock, BaseWeapon.LevelRequirement, canuse, totalbullets, wlife, isvip, baulcar, BaseWeapon.Category);

                                    if (Weapon != null)
                                        Weapons.TryAdd(basename, Weapon);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Username = TargetClient.GetHabbo().Username;
                Weapons = TargetClient.GetRoleplay().OwnedWeapons;
            }
            #endregion

            #region Execute
            if (Weapons.Count <= 0)
            {
                Session.SendWhisper("¡Lo siento! ¡Esta persona no tiene armas!", 1);
                return;
            }
            else
            {
                StringBuilder Message = new StringBuilder().Append("--- " + Username + "'s Owned Weapons ---\n\n");

                lock (TargetClient.GetRoleplay().OwnedWeapons.Values)
                {
                    foreach (Weapon Weapon in TargetClient.GetRoleplay().OwnedWeapons.Values)
                    {
                        Message.Append(Weapon.PublicName + " ---> Vida: " + Weapon.WLife + ", Alcance: " + Weapon.Range + " y Daño: " + Weapon.MinDamage + " - " + Weapon.MaxDamage + ".\n\n");
                        Message.Append("Balas " + Weapon.TotalBullets + "/" + Weapon.ClipSize + " en el cargador.\n\n");
                    }
                }
                Session.SendMessage(new MOTDNotificationComposer(Message.ToString()));
            }
            #endregion
        }
    }
}