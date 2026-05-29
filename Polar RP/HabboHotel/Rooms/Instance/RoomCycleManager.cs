using Polar.Communication.Packets.Outgoing.Rooms.Avatar;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Communication.Packets.Outgoing.Rooms.Session;
using Polar.Core;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Rooms.AI;
using Polar.HabboHotel.Rooms.TraxMachine;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Polar.HabboHotel.Rooms.Instance
{
    public class RoomCycleManager
    {
        private readonly Room _room;
        private bool _cycleOdd;
        private long _cycleTimestamp;
        private int _idleCycles;

        public RoomCycleManager(Room room)
        {
            _room = room;
            _cycleOdd = false;
            _cycleTimestamp = 0;
            _idleCycles = 0;
        }

        public long CycleTimestamp => _cycleTimestamp;

        public void ResetIdleCycles()
        {
            _idleCycles = 0;
        }

        public async Task Cycle()
        {
            if (_room == null || _room.mDisposed) return;

            _cycleOdd = !_cycleOdd;
            _cycleTimestamp = PolarEnvironment.GetUnixTimestamp();

            // Verificación de promoción
            if (_room.HasActivePromotion && _room.Promotion.HasExpired)
                _room.EndPromotion();

            int activeUsers = _room.GetRoomUserManager()?.userCount ?? 0;
            int activeBots = _room.GetRoomUserManager()?._bots?.Count ?? 0;

            if (activeUsers > 0 || activeBots > 0)
            {
                _idleCycles = 0;

                // 1. Ciclo de Items
                try { _room.GetRoomItemHandler()?.OnCycle(); }
                catch (Exception e) { Logging.LogException("RoomItemHandling.OnCycle: " + e); }

                // 2. Ciclo de Usuarios
                try { _room.GetRoomUserManager()?.OnCycle(); }
                catch (Exception e) { Logging.LogException("RoomUserManager.OnCycle: " + e); }

                // 3. Ciclo de Wired
                try { _room.GetWired()?.OnCycle(); }
                catch (Exception e) { Logging.LogException("WiredComponent.OnCycle: " + e); }

                // 4. Ciclo de Juegos
                try { if (_room.GetGameItemHandler() != null) _room.GetGameItemHandler().OnCycle(); }
                catch (Exception e) { Logging.LogException("GameItemHandler.OnCycle: " + e); }

                // 5. Ciclo de Trax
                try { _room.GetTraxManager()?.OnCycle(); }
                catch (Exception e) { Logging.LogException("TraxManager.OnCycle: " + e); }

                // 6. Actualizaciones de estado
                try { _room.GetRoomUserManager()?.SerializeStatusUpdates(); }
                catch (Exception e) { Logging.LogException("RoomUserManager.SerializeStatusUpdates: " + e); }
            }
            else
            {
                _idleCycles++;
                // Si la sala está vacía por 60 ciclos (30 segundos a 500ms) se descarga
                if (_idleCycles >= 60 && !_room.HasActivePromotion)
                {
                    _ = PolarEnvironment.GetGame().GetRoomManager().UnloadRoom(_room);
                    return;
                }
            }
        }

        public bool CycleRoomUser(RoomUser user)
        {
            if (user == null) return false;

            bool update = user.UpdateNeeded;

            // Procesar Hand Item (CarryItem)
            if (user.CarryItemID > 0)
            {
                user.CarryTimer--;
                if (user.CarryTimer <= 0)
                {
                    user.CarryItem(0);
                    update = true;
                }
            }

            // Procesar Idle / Sleep
            user.IdleTime++;
            if (!user.IsBot && !user.IsAsleep && user.IdleTime >= 4000)
            {
                user.IsAsleep = true;
                _room.SendMessage(new SleepComposer(user, true));

                var rp = user.GetClient()?.GetRoleplay();
                if (rp != null && !rp.IsJailed && !rp.IsDead)
                {
                    rp.BreakGeneralTimer = true;
                    user.GetClient().GetHabbo().Motto = "[DORMIDO] " + rp.Class;
                    user.GetClient().GetHabbo().Poof(true);
                }
                update = true;
            }

            // Procesar Spam Ticks
            user.HandleSpamTicks();

            if (user.isRolling)
            {
                if (user.rollerDelay <= 0)
                {
                    _room.GetRoomUserManager().UpdateUserStatus(user, false);
                    user.isRolling = false;
                }
                else
                {
                    user.rollerDelay--;
                }
                update = true;
            }

            if (user.RidingHorse) user.ApplyEffect(77);

            // Efectos de suelo (Skates, Swim, etc)
            ProcessUserFloorEffects(user);

            // Lógica Unificada de SIT / LAY
            if (!user.IsWalking)
            {
                if (user.ForceSit || user.ForceLay)
                {
                    // Mantener estado forzado si lo tiene
                }
                else
                {
                    var map = _room.GetGameMap();
                    if (map != null)
                    {
                        var items = map.GetAllRoomItemForSquare(user.X, user.Y);
                        var chair = items.FirstOrDefault(i => i.GetBaseItem().IsSeat);
                        var bed = items.FirstOrDefault(i => i.GetBaseItem().IsBed());

                        if (chair != null)
                        {
                            if (!user.isSitting || user.Z != chair.GetZ)
                            {
                                user.SetStatus("sit", chair.GetBaseItem().Height.ToString().Replace(',', '.'));
                                user.Z = chair.GetZ;
                                user.RotBody = chair.Rotation;
                                user.RotHead = chair.Rotation;
                                user.isSitting = true;
                                user.isLying = false;
                                update = true;
                            }
                        }
                        else if (bed != null)
                        {
                            if (!user.isLying || user.Z != bed.GetZ)
                            {
                                user.SetStatus("lay", bed.GetBaseItem().Height.ToString().Replace(',', '.') + " null");
                                user.Z = bed.GetZ;
                                user.RotBody = bed.Rotation;
                                user.RotHead = bed.Rotation;
                                user.isLying = true;
                                user.isSitting = false;
                                update = true;
                            }
                        }
                        else
                        {
                            // Si no hay silla ni cama, quitamos los estados
                            if (user.isSitting || user.isLying)
                            {
                                user.RemoveStatus("sit");
                                user.RemoveStatus("lay");
                                user.isSitting = false;
                                user.isLying = false;
                                update = true;
                            }
                        }
                    }
                }
            }

            if (update)
            {
                user.UpdateNeeded = true;
            }

            return update;
        }

        public void ProcessUserFloorEffects(RoomUser user, int nextX = -1, int nextY = -1)
        {
            if (user == null || user.IsBot || user.GetClient()?.GetHabbo() == null) return;

            int x = nextX == -1 ? user.X : nextX;
            int y = nextY == -1 ? user.Y : nextY;

            // Solo recalcular si el usuario cambió de tile desde la última vez
            if (nextX == -1 && user.X == user.LastEffectX && user.Y == user.LastEffectY) return;

            if (nextX == -1)
            {
                user.LastEffectX = user.X;
                user.LastEffectY = user.Y;
            }

            try
            {
                var map = _room.GetGameMap();
                if (map == null) return;

                byte effectByte = map.EffectMap[x, y];
                if (effectByte > 0)
                {
                    if (user.GetClient().GetHabbo().Effects().CurrentEffect == 0)
                        user.CurrentItemEffect = ItemEffectType.NONE;

                    ItemEffectType type = ByteToItemEffectEnum.Parse(effectByte);
                    if (type == user.CurrentItemEffect) return;

                    switch (type)
                    {
                        case ItemEffectType.Iceskates:
                            user.GetClient().GetHabbo().Effects().ApplyEffect(
                                user.GetClient().GetHabbo().Gender == "M" ? 38 : 39);
                            user.CurrentItemEffect = ItemEffectType.Iceskates;
                            break;
                        case ItemEffectType.Normalskates:
                            user.GetClient().GetHabbo().Effects().ApplyEffect(
                                user.GetClient().GetHabbo().Gender == "M" ? 55 : 56);
                            user.CurrentItemEffect = type;
                            break;
                        case ItemEffectType.SWIM:
                            user.GetClient().GetHabbo().Effects().ApplyEffect(29);
                            user.CurrentItemEffect = type;
                            break;
                        case ItemEffectType.SwimLow:
                            user.GetClient().GetHabbo().Effects().ApplyEffect(30);
                            user.CurrentItemEffect = type;
                            break;
                        case ItemEffectType.SwimHalloween:
                            user.GetClient().GetHabbo().Effects().ApplyEffect(37);
                            user.CurrentItemEffect = type;
                            break;
                        case ItemEffectType.PublicPool:
                            user.GetClient().GetHabbo().Effects().ApplyEffect(28);
                            user.CurrentItemEffect = type;
                            break;
                    }
                }
                else if (user.CurrentItemEffect != ItemEffectType.NONE)
                {
                    user.GetClient().GetHabbo().Effects().ApplyEffect(0);
                    user.CurrentItemEffect = ItemEffectType.NONE;
                }
            }
            catch (Exception ex)
            {
                Logging.LogException("ProcessUserFloorEffects: " + ex);
            }
        }
    }
}
