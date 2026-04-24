using System;
using System.Threading;
using System.Linq;
using System.Text;
using Polar.HabboRoleplay.Wizards;
using Polar.Database.Interfaces;
using Polar.Communication.Packets.Outgoing.Notifications;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Combat
{
    class HechizosCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_combat_equip"; }
        }

        public string Parameters
        {
            get { return "%wizard%"; }
        }

        public string Description
        {
            get { return "Utilizar el hechizo deseado."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Params.Length == 1)
            {
                Session.SendWhisper("¡Vaya, olvidó ingresar el nombre del hechizo o :hechizo lista para ver!", 1);
                return;
            }

            if (Params[1] == "lista")
            {
                if (Session.GetRoleplay().OwnedHechizos.Count <= 0)
                {
                    Session.SendWhisper("¡No tienes hechizos!", 1);
                    return;
                }

                StringBuilder Message = new StringBuilder().Append("--- TUS HECHIZOS ---\n\n");

                lock (Session.GetRoleplay().OwnedHechizos.Values)
                {
                    foreach (Hechizos Wizard in Session.GetRoleplay().OwnedHechizos.Values)
                    {
                        Message.Append("- "+Wizard.PublicName+".\n");
                        Message.Append("Vida: " + Wizard.Health + ".\n");
                        Message.Append("Escudo: " + Wizard.Shields + ".\n");
                        Message.Append("Daño Max: " + Wizard.FiringDamage + ".\n");
                        Message.Append("Rango Max: " + Wizard.FiringRange + ".\n\n");
                    }
                }
                Session.SendMessage(new MOTDNotificationComposer(Message.ToString()));

            }

            string GunName = Params[1].ToLower();
            Hechizos BaseWeapon = HechizosManager.getWizard(GunName);

            if (BaseWeapon == null)
            {
                Session.SendWhisper("¡Este hechizo no existe!", 1);
                return;
            }

            if (Session.GetRoomUser().Frozen)
                return;


            if (Session.GetRoleplay().AmbassadorOnDuty)
            {
                Session.SendWhisper("No puedes usar hechizo si estas como embajador, trampos@", 1);
                return;
            }

            if (Session.GetRoomUser().RidingHorse == true)
            {
                Session.SendWhisper("¡No puede hacer esto si eres un caballo!", 1);
                return;
            }

            if (Session.GetRoleplay().Learning)
            {
                Session.SendWhisper("Deja de leer para poder usar tu hechizo");
                return;
            }

            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes usar un hechizo mientras estás muerto!", 1);
                return;
            }

            if (Session.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes usar un hechizo mientras estás encarcelado!", 1);
                return;
            }

            if (Session.GetRoleplay().Cuffed)
            {
                Session.SendWhisper("No puedes sacar un " + GunName + ", con las manos esposadas", 1);
                return;
            }
            

            if (Session.GetRoleplay().TryGetCooldown("wizard", true))
                return;
            #endregion

            #region Execute
            var Weapon = Session.GetRoleplay().OwnedHechizos[GunName];

            if (Weapon.Health > 0)
            {
                Session.GetRoleplay().HechizoHealth = Weapon.Health;
               // Session.GetRoleplay().MaxHealth += Weapon.Health;
                Session.GetRoleplay().CurHealth += Weapon.Health;
            }

            if (Weapon.FiringRange > 0)
            {
                Session.GetRoleplay().HechizoRange = Weapon.FiringRange;
            }

            if (Weapon.FiringDamage > 0)
            {
                Session.GetRoleplay().HechizoDamage = Weapon.FiringDamage;
            }

            if (Weapon.Shields > 0)
            {
                Session.GetRoleplay().HechizoShield = Weapon.Shields;
                Session.GetRoleplay().ChalecoPor += Weapon.Shields;

            }

            Session.GetRoleplay().UpdateInteractingUserDialogues();
            Session.GetRoleplay().RefreshStatDialogue();
            Session.SendWhisper(Weapon.Message, 1);
            Session.GetRoleplay().IsNoob = false;
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("DELETE FROM `rp_hechizos_owned` WHERE id = '"+ Weapon.ID +"' AND base_wizard = '"+ Weapon.Name+"'");
            }
            Session.GetRoleplay().OwnedHechizos = Session.GetRoleplay().LoadAndReturnHechizos();
            Session.GetRoleplay().CooldownManager.CreateCooldown("wizard", 60000, 2);
            return;
            #endregion
        }
    }
}