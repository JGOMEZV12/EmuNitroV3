using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers;
using System.Linq;
using System;

namespace Polar.HabboHotel.Items.Interactor
{
    class InteractorTrainerGym : IFurniInteractor
    {
        /*public Room Room
        {
            get;
            private set;
        }*/

        public void OnPlace(GameClient Session, Item Item)
        {
        }

        public void OnRemove(GameClient Session, Item Item)
        {
        }

        public void OnWiredTrigger(Item Item)
        {
        }

        public void OnTrigger(GameClient Session, Item Item, int Request, bool HasRights)
        {
            Room Room = Session.GetHabbo().CurrentRoom;

            #region Conditions
            if (Session.GetRoomUser() == null)
                return;

            if (Session.GetRoleplay().IsWorking)
            {
                Session.SendWhisper("¡No puedes entrenar mientras haces ejercicio!", 1);
                return;
            }

            if (Session.GetRoleplay().IsWorkingOut)
            {
                Session.SendWhisper("¡Ya estás entrenando!", 1);
                return;
            }

            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("Usted no puede hacer ejercicio mientras estás muerto", 1);
                return;
            }

            if (!Room.GymEnabled)
            {
                Session.SendWhisper("Usted debe estar dentro del gimnasio para entrenar", 1);
                return;
            }

            if (Session.GetRoleplay().CurEnergy <= 0)
            {
                Session.SendWhisper("¡No tienes suficiente energía para hacer ejercicio!", 1);
                return;
            }

            bool HasTreadmill = Room.GetRoomItemHandler().GetFloor.Where(x => (x.GetBaseItem().ItemName.ToLower() == "olympics_c16_treadmill" || x.GetBaseItem().ItemName.ToLower() == "olympics_c16_crosstrainer") && x.Coordinate == Session.GetRoomUser().Coordinate).ToList().Count > 0;

            if (!HasTreadmill)
            {
                Session.SendWhisper("¡Debe estar en una cinta rodante o crosstrainer para entrenar!", 1);
                return;
            }

            Item Treadmill = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => (x.GetBaseItem().ItemName.ToLower() == "olympics_c16_treadmill" || x.GetBaseItem().ItemName.ToLower() == "olympics_c16_crosstrainer") && x.Coordinate == Session.GetRoomUser().Coordinate);

            if (Treadmill == null)
            {
                Session.SendWhisper("¡Debe estar en una cinta rodante o crosstrainer para entrenar!", 1);
                return;
            }

            bool Strength = false;
            if (Treadmill.GetBaseItem().ItemName.ToLower() == "olympics_c16_treadmill")
                Strength = true;

            if (Strength && Session.GetRoleplay().Strength >= RoleplayManager.StrengthCap)
            {
                Session.SendWhisper("Ha alcanzado el nivel máximo de resistencia de: " + RoleplayManager.StrengthCap + "!", 1);
                return;
            }

            if (!Strength && Session.GetRoleplay().Stamina >= RoleplayManager.StaminaCap)
            {
                Session.SendWhisper("Ha alcanzado el nivel máximo de resistencia de: " + RoleplayManager.StaminaCap + "!", 1);
                return;
            }
            #endregion

            #region Execute
            if (Session.GetRoomUser().isSitting)
            {
                Session.GetRoomUser().Z += 0.35;
                Session.GetRoomUser().RemoveStatus("sit");
                Session.GetRoomUser().isSitting = false;
                Session.GetRoomUser().UpdateNeeded = true;
            }
            else if (Session.GetRoomUser().isLying)
            {
                Session.GetRoomUser().Z += 0.35;
                Session.GetRoomUser().RemoveStatus("lay");
                Session.GetRoomUser().isLying = false;
                Session.GetRoomUser().UpdateNeeded = true;
            }

            Treadmill.ExtraData = "1";
            Treadmill.InteractingUser = Session.GetHabbo().Id;
            Treadmill.UpdateState(false, true);
            Treadmill.RequestUpdate(1, true);

            if (!Strength)
                Session.GetRoomUser().ApplyEffect(195);
            else
                Session.GetRoomUser().ApplyEffect(194);

            object[] Data = { Treadmill.Id, Strength };

            Session.GetRoomUser().SetRot(Treadmill.Rotation, false);
            Session.GetRoleplay().IsWorkingOut = true;

            Session.GetRoleplay().TimerManager.CreateTimer("workout", 1000, true, Data);

            if (Strength)
            {
                Session.Shout("*Comienza a correr en la cinta de correr comenzando a ejercitarse su fuerza en sus piernas*", 4);
                Session.SendWhisper("Entrenando: " + String.Format("{0:N0}", Session.GetRoleplay().StrengthEXP) + "/" + String.Format("{0:N0}", ((!LevelManager.StrengthLevels.ContainsKey(Session.GetRoleplay().Strength + 1) ? 5000 : LevelManager.StrengthLevels[Session.GetRoleplay().Strength + 1]))), 1);
            }
            else
            {
                Session.Shout("*Comienza a correr en el crosstrainer comenzando a entrenar su resistencia*", 4);
                Session.SendWhisper("Entrenando: " + String.Format("{0:N0}", Session.GetRoleplay().StaminaEXP) + "/" + String.Format("{0:N0}", ((!LevelManager.StaminaLevels.ContainsKey(Session.GetRoleplay().Stamina + 1) ? 5000 : LevelManager.StaminaLevels[Session.GetRoleplay().Stamina + 1]))), 1);
            }
            #endregion
        }
    }
}
