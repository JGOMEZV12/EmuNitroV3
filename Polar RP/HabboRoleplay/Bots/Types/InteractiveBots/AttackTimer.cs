using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Bots;
using Polar.HabboRoleplay.Bots.Manager;
using Polar.HabboHotel.Items;
using System.Linq;
using System.Drawing;
using Polar.HabboHotel.Pathfinding;
using System.Threading;
using Polar.HabboHotel.Rooms;
using Polar.Utilities;
using System.Collections.Generic;
using Polar.HabboRoleplay.Weapons;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Attack timer
    /// </summary>
    public class AttackTimer : BotRoleplayTimer
    {

        public AttackTimer(string Type, RoleplayBot CachedBot, int Time, bool Forever, object[] Params) 
            : base(Type, CachedBot, Time, Forever, Params)
        {
            this.TimeCount = 0;
        }
 
        /// <summary>
        /// Begins chasing the client
        /// </summary>
        public override void Execute()
        {
            try
            {
                if (base.CachedBot == null || base.CachedBot.DRoomUser.GetBotRoleplay() == null || base.CachedBot.DRoom == null)
                {
                    this.StopAttacking();
                    return;
                }

                if (GetAttacking() == null || GetAttacking().GetHabbo() == null)
                {
                    this.StopAttacking();
                    return;
                }

                if (GetAttacking().GetRoomUser() == null && GetAttacking().GetHabbo().CurrentRoomId != 0)
                    return;

                if (GetAttacking() != null && GetAttacking().GetHabbo() != null && GetAttacking().GetRoleplay() != null)
                {
                    if (GetAttacking().GetRoleplay().IsDead)
                    {
                        base.CachedBot.DRoomUser.GetBotRoleplay().UserAttacking = null;
                        this.StopAttacking();
                        return;
                    }
                }

                TimeCount++;

                // Waits 1.5 seconds before chasing the user down
                if (TimeCount < 15)
                    return;

                #region Chase user through rooms
                if (GetAttacking().GetHabbo().CurrentRoom != base.CachedBot.DRoom)
                {

                    if (base.CachedBot.DRoomUser.GetBotRoleplay().AIType == RoleplayBotAIType.MAFIAWARS)
                    {
                        this.StopAttacking();
                        return;
                    }

                    List<Item> AllArrows = base.CachedBot.DRoom.GetRoomItemHandler().GetFloor.Where(x => x.GetBaseItem().InteractionType == InteractionType.ARROW).ToList();
                    List<Item> PossibleArrows = AllArrows.Where(x => ItemTeleporterFinder.GetTeleRoomId(ItemTeleporterFinder.GetLinkedTele(x.Id, base.CachedBot.DRoom), base.CachedBot.DRoom) == GetAttacking().GetRoomUser().RoomId).ToList();

                    if (PossibleArrows.Count <= 0)
                    {
                        if (GetAttacking() != null)
                            base.CachedBot.DRoomUser.Chat("Drats! Te escapaste, te atraparé la próxima vez " + GetAttacking().GetHabbo().Username + "!", true, 4);

                        base.CachedBot.DRoomUser.GetBotRoleplay().UserAttacking = null;
                        this.StopAttacking();
                        return;
                    }

                    Item RandTele = PossibleArrows.FirstOrDefault();

                    int LinkedTeleId = ItemTeleporterFinder.GetLinkedTele(RandTele.Id, base.CachedBot.DRoom);
                    int LinkedTeleRoomId = ItemTeleporterFinder.GetTeleRoomId(LinkedTeleId, base.CachedBot.DRoom);
                    RoleplayManager.GenerateRoom(LinkedTeleRoomId, out Room LinkedRoom);

                    //object[] Params = { RandTele, LinkedTeleId, LinkedTeleRoomId, LinkedRoom };
                    object[] Params = { CachedBot, RandTele };

                    if (LinkedRoom.HitEnabled)
                        base.CachedBot.DRoomUser.GetBotRoleplay().TimerManager.CreateTimer("teleport", CachedBot, 100, true, Params);
                    else
                    {
                        if (GetAttacking() != null)
                            base.CachedBot.DRoomUser.Chat("Te escapaste, te agarraré la próxima vez " + GetAttacking().GetHabbo().Username + "!", true, 4);

                        base.CachedBot.DRoomUser.GetBotRoleplay().UserAttacking = null;
                    }

                    this.StopAttacking();
                    return;
                }
                #endregion
                else
                {
                    if (GetAttacking().GetRoleplay().EquippedWeapon == null)
                    {

                        if (CanCombat(GetAttacking(), base.CachedBot.DRoomUser) && !base.CachedBot.DRoomUser.GetBotRoleplay().TryGetCooldown("fist"))
                        {
                            if (HandleCombat(GetAttacking(), base.CachedBot.DRoomUser))
                            {
                                this.StopAttacking();
                                return;
                            }
                        }

                        if (!base.CachedBot.DRoomUser.GetBotRoleplay().TryGetCooldown("fist"))
                            base.CachedBot.DRoomUser.MoveTo(GetAttacking().GetRoomUser().Coordinate);
                        else
                            base.CachedBot.DRoomUser.MoveTo(base.CachedBot.DRoom.GetGameMap().GetRandomWalkableSquare());

                    }
                    else
                    {

                        if (CanCombat(GetAttacking(), base.CachedBot.DRoomUser) && !base.CachedBot.DRoomUser.GetBotRoleplay().TryGetCooldown("gun"))
                        {
                            if (HandleCombat(GetAttacking(), base.CachedBot.DRoomUser))
                            {
                                this.StopAttacking();
                                return;
                            }
                        }

                        if (!base.CachedBot.DRoomUser.GetBotRoleplay().TryGetCooldown("gun"))
                            base.CachedBot.DRoomUser.MoveTo(GetAttacking().GetRoomUser().Coordinate);
                        else
                            base.CachedBot.DRoomUser.MoveTo(base.CachedBot.DRoom.GetGameMap().GetRandomWalkableSquare());

                    }

                    return;
                }

            }
            catch
            {
                this.StopAttacking();
            }
        }

        public void StopAttacking()
        {
            try
            {
                BotRoleplayTimer Timer;

                if (base.CachedBot?.DRoomUser?.GetBotRoleplay() != null)
                {
                    base.CachedBot.DRoomUser.GetBotRoleplay().UserAttacking = null;

                    if (base.CachedBot.DRoomUser.GetBotRoleplay().ActiveTimers != null &&
                        base.CachedBot.DRoomUser.GetBotRoleplay().ActiveTimers.ContainsKey("attack"))
                    {
                        base.CachedBot.DRoomUser.GetBotRoleplay().ActiveTimers.TryRemove("attack", out Timer);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log sin volver a llamar StopAttacking
                Console.WriteLine("Error en StopAttacking(): " + ex.Message);
            }
            finally
            {
                base.EndTimer();
            }
        }

        public bool CanCombat(GameClient Client, RoomUser RoleplayBot)
        {
            double Distance = RoleplayManager.GetDistanceBetweenPoints2D(RoleplayBot.Coordinate, Client.GetRoomUser().Coordinate);

            if (Distance <= 1)
                return true;

            return false;
        }

        public bool HandleCombat(GameClient Client, RoomUser RoleplayBot)
        {
            bool Died = false;
            if (RoleplayBot.GetBotRoleplay().Dead == false)
            {
                int Damage = GetDamage(RoleplayBot);
                if (Client.GetRoleplay().EquippedWeapon != null)
                {
                    if (RoleplayBot.CurrentEffect != 777)
                        RoleplayBot.ApplyEffect(777);
                }

                
                if (Client.GetRoleplay().EquippedWeapon == null)
                {
                    if ((Client.GetRoleplay().CurHealth - Damage) <= 0)
                    {
                        Client.GetRoleplay().CurHealth = 0;
                        RoleplayBot.Chat("*" + (RoleplayBot.GetBotRoleplay().IsPet ? "Ataca" : "Lanza golpe") + " a " + Client.GetHabbo().Username + ", y le da duro*", true, 6);
                        Died = true;
                    }
                    else
                    {
                        Client.GetRoleplay().CurHealth -= Damage;
                        RoleplayBot.Chat("*" + (RoleplayBot.GetBotRoleplay().IsPet ? "Ataca" : "Le tira un golpe") + " a " + Client.GetHabbo().Username + " y le quita " + Damage + "*", true, 6);
                    }

                    //RoleplayBot.GetBotRoleplay().CooldownManager.CreateCooldown("fist", 1000, RoleplayBot.GetBotRoleplay().AttackInterval);
                }
                else
                {

                    if ((Client.GetRoleplay().CurHealth - Damage) <= 0)
                    {
                        Client.GetRoleplay().CurHealth = 0;
                        RoleplayBot.Chat("*Dispara a " + Client.GetHabbo().Username + "*", true, 6);
                        Died = true;
                    }
                    else
                    {
                        Client.GetRoleplay().CurHealth -= Damage;
                        RoleplayBot.Chat("*Dispara a " + Client.GetHabbo().Username + " causandole " + Damage + " de daño*", true, 6);
                    }

                   // RoleplayBot.GetBotRoleplay().CooldownManager.CreateCooldown("gun", 1000, RoleplayBot.GetBotRoleplay().AttackInterval);

                }
               
            }
            return Died;
        }

        public int GetDamage(RoomUser RoleplayBot)
        {
            CryptoRandom Random = new CryptoRandom();

            int Strength = RoleplayBot.GetBotRoleplay().Strength;
            int MinDamage = (Strength - 10) <= 0 ? 1 : (Strength - 10);
            int MaxDamage = Strength + 10;

            // Lucky shot?
            if (Random.Next(0, 1000) < 40)
            {
                MinDamage = Strength + 40;
                MaxDamage = MinDamage + 1;
            }

            return Random.Next(MinDamage, MaxDamage);
        }

        private GameClient GetAttacking()
        {
            return base.CachedBot.DRoomUser.GetBotRoleplay().UserAttacking;
        }
    }
}