using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.Combat;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Weapons;
using Polar.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Combat
{
    class ShootCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_combat_shoot"; }

        }
        

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Dispara tu arma al usuario objetivo."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("el comando es: ':disparar x'.", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(((Session.GetRoleplay().Target != "" && Params[1] == "x") || Session.GetRoleplay().TargetLock) ? Session.GetRoleplay().Target : Params[1]);
            
            if (TargetClient == null)
            {
                RoomUser Bot = Room.GetRoomUserManager().GetBotByName(Params[1]);

                if (Bot != null && Bot.GetBotRoleplay() != null)
                {
                    Session.GetRoleplay().LastCommand = ":disparar " + Params[1];
                    CombatManager.GetCombatType("gun").ExecuteBot(Session, Bot.GetBotRoleplay());
                    return;
                }

                //Session.GetRoleplay().LastCommand = ":disparar " + Params[1];
                Session.SendWhisper("Se ha producido un error al intentar encontrar a ese usuario, tal vez están sin conexión.", 1);
                return;
            }

            RoomUser TargetUser = Room.GetRoomUserManager().GetRoomUserByHabbo(TargetClient.GetHabbo().Username);
            if (TargetUser == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no estén en línea o en esta sala.", 1);
                return;
            }

            /*if (TargetClient.MachineId == Session.MachineId)
            {
                Session.SendWhisper("¡No puedes disparar otra de tus cuentas!", 1);
                return;
            }*/
            if (Session.GetRoleplay().PassiveMode)
            {
                Session.SendWhisper("No puedes disparar en modo pasivo.", 1);
                return;
            }

            if (!Session.GetRoleplay().CombatMode)
            {
                Session.SendWhisper("No tienes el modo combate activado", 1);
                return;
            }

            if (TargetClient.GetRoleplay().PassiveMode)
            {
                Session.SendWhisper("No puedes disparara una persona en modo pasivo.", 1);
                return;
            }

            if (TargetClient.GetRoomUser().Frozen == true)
            {
                Session.SendWhisper("No se puede disparar a alguien que esta aturdido.", 1);
                return;
            }

            if (Session.GetRoleplay().IsNoob == true)
            {
                Session.SendWhisper("¡Debes esperar que termine la inmunidad!  >:)", 1);
                return;
            }

            if (TargetClient.GetRoleplay().IsNoob)
            {
                Session.SendWhisper("¡Este usuario esta bajo inmunidad por pocos segundos!  :(", 1);
                return;
            }

            if (Session.GetRoleplay().EquippedWeapon == null)
            {
                Session.SendWhisper("¡No tienes ningún arma Equipada! Usa :equipar [nombre-arma]", 1);
                return;
            }

            if (TargetClient.GetRoleplay().Cuffed)
            {
                Session.SendWhisper("¡No puedes dispararle a una persona esposada!", 1);
                return;
            }
            if (TargetClient.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes dispararle a una persona muerta!", 1);
                return;
            }
            if (TargetClient.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes dispararle a una persona encarcelada!", 1);
                return;
            }
            /*if (TargetClient.GetConnection().getIp() == Session.GetConnection().getIp())
            {
                Session.SendWhisper("¡No puedes dispararle a tus propias cuentas!", 1);
                return;
            }*/
            if (Session.GetRoleplay().WLife <= 0)
            {
                Session.SendWhisper("¡Tu arma está dañada! Busca a un Armero para que la repare.", 1);
                return;
            }

            /* if ((Session.GetRoleplay().DrivingCar))
             {
                 if (!HabboHotel.Groups.GroupManager.HasJobCommand(Session, "law") && !Session.GetHabbo().GetPermissions().HasRight("corporation_rights"))
                 {
                     Session.SendWhisper("¡No puedes hacer eso mientras vas dentro de un vehículo!", 1);
                     return;
                 }
             }*/
            /* if (Session.GetRoleplay().EquippedWeapon == "electrica")
             {
                 if (HabboHotel.Groups.GroupManager.HasJobCommand(Session, "stun") && (Session.GetRoleplay().IsWorking || Session.GetRoleplay().PoliceTrial) && !TargetClient.GetRoleplay().Cuffed)
                 {
                     // Paralizar / Desparalizar
                     #region Execute
                     RoomUser RoomUser = Session.GetRoomUser();
                     if (TargetUser == null)
                     {
                         Session.SendWhisper("Ha ocurrido un error en encontrar al usuario, probablemente esté desconectado o no está en esta Zona.", 1);
                         return;
                     }
                     if (!Session.GetHabbo().GetPermissions().HasRight("corporation_rights"))
                     {
                         if (TargetClient.GetHabbo().Id == Session.GetHabbo().Id)
                         {
                             Session.SendWhisper("No puedes hacerte eso a ti mism@.", 1);
                             return;
                         }
                         if (HabboHotel.Groups.GroupManager.HasJobCommand(TargetClient, "law"))
                         {
                             Session.SendWhisper("¡No puedes hacer eso entre compañeros de trabajo!", 1);
                             return;
                         }
                     }
                     /*if (Session != TargetClient && TargetClient.GetHabbo().Rank > 3)
                     {
                         Session.SendWhisper("((No puedes hacerle eso a un miembro de la administración))", 1);
                         return;
                     }
                     if (Session.GetRoleplay().DrivingCar)
                     {
                         Session.SendWhisper("¡No puedes hacer eso mientras vas dentro de un vehículo!", 1);
                         return;
                     }//
                     if (TargetClient.GetRoleplay().IsDead)
                     {
                         Session.SendWhisper("¡No puedes hacerle eso a una persona muert@!", 1);
                         return;
                     }

                     if (TargetClient.GetRoleplay().IsJailed)
                     {
                         Session.SendWhisper("¡No puedes hacerle eso a una persona encarcelad@!", 1);
                         return;
                     }
                     if (TargetClient.GetRoomUser().isLying)
                     {
                         Session.SendWhisper("¡No puedes hacerle eso a una persona muerta", 1);
                         return;
                     }
                     /*if (TargetClient.GetRoleplay().DrivingCar)
                     {
                         Session.SendWhisper("¡No puedes hacerle eso a una persona mientras va conduciendo!", 1);
                         return;
                     }
                     if (TargetClient.GetRoleplay().Pasajero)
                     {
                         Session.SendWhisper("¡No puedes hacerle eso a una persona mientras va de pasajer@!", 1);
                         return;
                     }//
                     if (TargetUser.IsAsleep)
                     {
                         Session.SendWhisper("¡No puedes aturdir a un usuario ausente!", 1);
                         return;
                     }
                     if (Session.GetRoleplay().TryGetCooldown("stun"))
                         return;

                     Point ClientPos = new Point(RoomUser.Coordinate.X, RoomUser.Coordinate.Y);
                     Point TargetClientPos = new Point(TargetUser.Coordinate.X, TargetUser.Coordinate.Y);
                     double Distance = RoleplayManager.GetDistanceBetweenPoints2D(ClientPos, TargetClientPos);

                     if (!TargetClient.GetRoomUser().Frozen)
                     {
                         // Paralizar
                         CryptoRandom Random = new CryptoRandom();
                         int Chance = Random.Next(1, 101);

                         if (Distance <= RoleplayManager.StunGunRange)
                         {
                             if (Chance <= 8)
                             {
                                 RoleplayManager.Shout(Session, "*Dispara su pistola paralizadora hacia " + TargetClient.GetHabbo().Username + ", pero falla*", 37);

                                 Session.GetRoleplay().CooldownManager.CreateCooldown("stun", 1000, 3);
                             }
                             else
                             {
                                 RoleplayManager.Shout(Session, "*Dispara su pistola paralizadora hacia " + TargetClient.GetHabbo().Username + " inmovilizándolo inmediatamente*", 37);
                                 TargetClient.GetRoleplay().TimerManager.CreateTimer("stun", 1000, false);
                                 //TargetClient.SendMessage(new FloodControlComposer(15));

                                 if (TargetClient.GetRoleplay().InsideTaxi)
                                     TargetClient.GetRoleplay().InsideTaxi = false;

                                 TargetClient.GetRoleplay().Paralized = true;
                                 TargetClient.GetRoomUser().Frozen = true;
                                 TargetClient.GetRoomUser().CanWalk = false;
                                 TargetClient.GetRoomUser().ClearMovement(true);

                                 #region Desequipar al Concito
                                 if (TargetClient.GetRoleplay().EquippedWeapon != null)
                                 {
                                     string UnEquipMessage = TargetClient.GetRoleplay().EquippedWeapon.UnEquipText;
                                     UnEquipMessage = UnEquipMessage.Replace("[NAME]", TargetClient.GetRoleplay().EquippedWeapon.PublicName);

                                     RoleplayManager.Shout(TargetClient, UnEquipMessage, 5);

                                     if (TargetClient.GetRoomUser().CurrentEffect == TargetClient.GetRoleplay().EquippedWeapon.EffectID)
                                         TargetClient.GetRoomUser().ApplyEffect(0);

                                     if (TargetClient.GetRoomUser().CarryItemID == TargetClient.GetRoleplay().EquippedWeapon.HandItem)
                                         TargetClient.GetRoomUser().CarryItem(0);

                                     TargetClient.GetRoleplay().CooldownManager.CreateCooldown("unequip", 1000, 3);
                                     TargetClient.GetRoleplay().EquippedWeapon = null;

                                     TargetClient.GetRoleplay().WLife = 0;
                                     TargetClient.GetRoleplay().Bullets = 0;
                                 }
                                 #endregion

                                 Session.GetRoleplay().CooldownManager.CreateCooldown("stun", 1000, 3);
                             }
                         }
                         else
                         {
                             RoleplayManager.Shout(Session, "*Dispara su pistola paralizadora hacia " + TargetClient.GetHabbo().Username + ", pero el disparo no lo alcanza*", 37);
                             Session.GetRoleplay().CooldownManager.CreateCooldown("stun", 1000, 3);
                         }

                         #region Sound System
                         foreach (RoomUser RoomUsers in Session.GetRoomUser().GetRoom().GetRoomUserManager().GetRoomUsers())
                         {
                             if (RoomUsers == null || RoomUsers.GetClient() == null)
                                 continue;

                             PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(RoomUsers.GetClient(), "event_feedcomposer", "sound|paralizer");
                         }
                         #endregion
                     }
                     else
                     {
                         // Desparalizar
                         if (Distance <= 1)
                         {
                             RoleplayManager.Shout(Session, "*Ayuda a " + TargetClient.GetHabbo().Username + ", dándole tiempo para recuperarse de su aturdimiento*", 37);

                             TargetClient.GetRoomUser().Frozen = false;
                             TargetClient.GetRoomUser().CanWalk = true;
                             TargetClient.GetRoomUser().ClearMovement(true);
                             return;
                         }
                         else
                         {
                             Session.SendWhisper("Debes estar más cerca de la persona.", 1);
                             return;
                         }
                     }
                     #endregion
                 }
             }
             else
             {*/
            // New Target System
            Session.GetRoleplay().Target = TargetClient.GetHabbo().Username;
            Session.GetRoleplay().LastCommand = ":disparar " + TargetClient.GetHabbo().Username;
            CombatManager.GetCombatType("gun").Execute(Session, TargetClient);
            //}
        }
    }
}