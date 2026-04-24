using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.Core;

namespace Polar.HabboRoleplay.Cooldowns.Types
{
    /// <summary>
    /// Default cooldown
    /// </summary>
    public class DefaultCooldown : Cooldown
    {
        public DefaultCooldown(string Type, GameClient Client, int Time, int Amount)
            : base(Type, Client, Time, Amount)
        {
            TimeLeft = Amount * 1000;
        }

        /// <summary>
        /// Removes the cooldown
        /// </summary>
        public override void Execute()
        {
            try
            {
                if (base.Client == null || base.Client.GetRoleplay() == null || base.Client.GetHabbo() == null)
                {
                    base.EndCooldown();
                    return;
                }

                TimeLeft -= 1000;

                /*if (Type.ToLower() == "reload" && TimeLeft > 0)
                    base.Client.SendWhisper("Recargando arma: " + (TimeLeft / 1000) + "/" + Amount, 1);
                */
                if (TimeLeft > 0)
                    return;

                if (Type.ToLower() == "weed")
                {
                    RoleplayManager.Shout(base.Client, "*Siente su desgaste de la mala hierba y le da hambre [+20]Hambre*", 4);
                    base.Client.GetRoleplay().HighOffWeed = false;
                    base.Client.GetRoleplay().Hunger += 20;
                   
                }
                else if (Type.ToLower() == "textcooldown")
                {
                    if (Client.GetRoomUser() != null)
                        Client.GetRoomUser().ApplyEffect(0);
                }
                else if (Type.ToLower() == "cocaine" || Type.ToLower() == "cocaina")
                {
                    RoleplayManager.Shout(base.Client, "*Se empieza a sentir aliviado, terminando el efecto*", 4);
                    base.Client.GetRoleplay().HighOffCocaine = false;
                    if (Client.GetRoomUser() != null)
                        Client.GetRoomUser().SuperFastWalking = false;
                }
                else if (Type.ToLower() == "heroina")
                {
                    RoleplayManager.Shout(base.Client, "*Se empieza a sentir aliviado, terminando el efecto*", 4);
                    base.Client.GetRoleplay().HighOffHeroina = false;
                    if (Client.GetRoomUser() != null)
                        Client.GetRoomUser().SuperFastWalking = false;
                }
                else if (Type.ToLower() == "heroinaeffect")
                {
                        if (Client.GetRoomUser() != null)
                            Client.GetRoomUser().CarryItem(0);
    
                }
                else if (Type.ToLower() == "medicina" || Type.ToLower() == "pildora")
                {
                    RoleplayManager.Shout(base.Client, "*Se siente un poco mareado de tanta medicina [-1 Animo]*", 4);
                    base.Client.GetRoleplay().HighOffMedicina = false;
                    base.Client.GetRoleplay().Animo -= 1;
                }
                else if (Type.ToLower() == "wizard")
                {
                    RoleplayManager.Shout(base.Client, "*El hechizo empieza a perder efecto*", 4);
                    if (base.Client.GetRoleplay().HechizoRange > 0)
                    {
                        Client.GetRoleplay().HechizoRange = 0;
                    }

                    if (base.Client.GetRoleplay().HechizoDamage > 0)
                    {
                        Client.GetRoleplay().HechizoDamage = 0;
                    }

                    if (base.Client.GetRoleplay().HechizoShield > 0)
                    {
                        Client.GetRoleplay().HechizoShield = 0;
                        Client.GetRoleplay().ChalecoPor = 0;
                    }
                    if (base.Client.GetRoleplay().HechizoShield > 0 && base.Client.GetRoleplay().HechizoHealth > 0)
                    {
                        //base.Client.GetRoleplay().MaxHealth -= base.Client.GetRoleplay().HechizoHealth;
                        if (base.Client.GetRoleplay().CurHealth > 200)
                        {
                            base.Client.GetRoleplay().CurHealth -= base.Client.GetRoleplay().HechizoHealth;
                        }
                        else if (base.Client.GetRoleplay().CurHealth < 100)
                        {
                            base.Client.GetRoleplay().CurHealth -= 50;
                        }

                        Client.GetRoleplay().HechizoShield = 0;
                        Client.GetRoleplay().ChalecoPor = 0;
                    }
                    else if (base.Client.GetRoleplay().HechizoHealth > 0)
                    {
                        
                        //base.Client.GetRoleplay().MaxHealth -= base.Client.GetRoleplay().HechizoHealth;
                        if (base.Client.GetRoleplay().CurHealth > 200)
                        {
                            base.Client.GetRoleplay().CurHealth -= base.Client.GetRoleplay().HechizoHealth;
                        }
                        else if(base.Client.GetRoleplay().CurHealth < 100)
                        {
                            base.Client.GetRoleplay().CurHealth -= 50;
                        }
                        base.Client.GetRoleplay().HechizoHealth = 0;
                    }
                    base.Client.GetRoleplay().UpdateInteractingUserDialogues();
                    base.Client.GetRoleplay().RefreshStatDialogue();
                }
                else if (Type.ToLower() == "reload")
                    base.Client.SendWhisper("¡Arma Recargada!", 1);

                if (base.Client.GetRoleplay().SpecialCooldowns.ContainsKey(Type.ToLower()))
                    base.Client.GetRoleplay().SpecialCooldowns.TryUpdate(Type.ToLower(), TimeLeft, base.Client.GetRoleplay().SpecialCooldowns[Type.ToLower()]);

                base.EndCooldown();
            }
            catch (Exception e)
            {
                Logging.LogCriticalException("Error in Execute() void: " + e);
            }
        }
    }
}