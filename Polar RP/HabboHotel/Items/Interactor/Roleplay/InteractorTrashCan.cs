using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Polar.Utilities;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Rooms.Pathfinding;
using Polar.HabboHotel.Users.Effects;
using MoreLinq;
using Polar.HabboHotel.Groups;

namespace Polar.HabboHotel.Items.Interactor
{
    public class InteractorTrashCan : IFurniInteractor
    {
        public void OnPlace(GameClient Session, Item Item)
        {
        }

        public void OnRemove(GameClient Session, Item Item)
        {
        }

        public void OnTrigger(GameClient Session, Item Item, int Request, bool HasRights)
        {
            // FIX: Validar también GetHabbo() antes de usarlo
            if (Session == null || Session.GetHabbo() == null)
                return;

            RoomUser User = Item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

            if (User == null)
                return;

            if (!Gamemap.TilesTouching(Item.GetX, Item.GetY, User.Coordinate.X, User.Coordinate.Y))
            {
                if (Item.ExtraData == "" || Item.ExtraData == "0")
                    if (User.CanWalk)
                        User.MoveTo(Item.SquareInFront);
            }
            else
            {
                #region Conditions

                #region Group Conditions
                List<Group> Groups = PolarEnvironment.GetGame().GetGroupManager().GetJobsForUser(Session.GetHabbo().Id);

                if (Groups.Count <= 0)
                {
                    Session.SendWhisper("No tienes ningún trabajo para hacer eso.", 1);
                    return;
                }

                int GroupNumber = -1;

                if (Groups[0].GType != 2)
                {
                    if (Groups.Count > 1)
                    {
                        if (Groups[1].GType != 2)
                        {
                            Session.SendWhisper("((No perteneces a ningún trabajo usar ese comando))", 1);
                            return;
                        }
                        GroupNumber = 1;
                    }
                    else
                    {
                        Session.SendWhisper("((No perteneces a ningún trabajo para usar ese comando))", 1);
                        return;
                    }
                }
                else
                {
                    GroupNumber = 0;
                }

                Session.GetRoleplay().JobId = Groups[GroupNumber].Id;
                Session.GetRoleplay().JobRank = Groups[GroupNumber].Members[Session.GetHabbo().Id].UserRank;
                #endregion

                #region Extra Conditions
                if (!GroupManager.JobExists(Session.GetRoleplay().JobId, Session.GetRoleplay().JobRank))
                {
                    Session.GetRoleplay().TimeWorked = 0;
                    Session.GetRoleplay().JobId = 0;
                    Session.GetRoleplay().JobRank = 0;
                    Session.SendWhisper("Lo sentimos, ese trabajo no existe. Te hemos removido ese trabajo.", 1);
                    return;
                }

                if (!GroupManager.HasJobCommand(Session, "basurero"))
                {
                    Session.SendWhisper("Debes tener el trabajo de Basuero recoger contenedores de basura.", 1);
                    return;
                }
                #endregion

                #region Basic Conditions
                if (Session.GetRoleplay().Cuffed)
                {
                    Session.SendWhisper("No puedes hacer eso mientras estás esposad@", 1);
                    return;
                }
                if (!Session.GetRoomUser().CanWalk)
                {
                    Session.SendWhisper("Al parecer no puedes ni moverte para hacer eso.", 1);
                    return;
                }
                if (Session.GetRoleplay().Pasajero)
                {
                    Session.SendWhisper("¡No puedes hacer eso mientras vas de Pasajer@!", 1);
                    return;
                }
                if (Session.GetRoleplay().IsDead)
                {
                    Session.SendWhisper("¡No puedes hacer esto mientras estás muert@!", 1);
                    return;
                }
                if (Session.GetRoleplay().IsJailed)
                {
                    Session.SendWhisper("¡No puedes hacer eso mientras estás encarcelad@!", 1);
                    return;
                }
                if (Session.GetRoleplay().DrivingCar)
                {
                    Session.SendWhisper("¡No puedes hacer eso mientras conduces!", 1);
                    return;
                }
                if (Session.GetRoleplay().EquippedWeapon != null)
                {
                    Session.SendWhisper("¡No puedes hacer eso mientras equipas un arma!", 1);
                    return;
                }
                if (!Session.GetRoleplay().IsWorking)
                {
                    Session.SendWhisper("¡Debes trabajar de recolector de basura para hacer eso!", 1);
                    return;
                }
                if (Session.GetRoleplay().BasuTrashCount >= 15)
                {
                    Session.SendWhisper("¡Ya tienen 15 contenedores recogidos! Vuelvan al Basurero a ':descargarcamion' para recibir su paga.", 1);
                    return;
                }
                #endregion

                #endregion

                if (Item.ExtraData == "")
                    Item.ExtraData = "0";

                if (Item.ExtraData == "0")
                {
                    int Minutes = 5;

                    User.ClearMovement(true);
                    User.SetRot(Rotation.Calculate(User.Coordinate.X, User.Coordinate.Y, Item.GetX, Item.GetY), false);

                    Item.ExtraData = "1";
                    Item.UpdateState(false, true);
                    Item.RequestUpdate(135 * Minutes, true);
                    RoleplayManager.Shout(Session, "*Comienza a recoger la basura del contenedor*", 5);

                    new Thread(() =>
                    {
                        User.CanWalk = false;

                        if (User.CurrentEffect != 4 && Session.GetRoleplay().EquippedWeapon == null)
                            User.ApplyEffect(EffectsList.Twinkle);

                        Thread.Sleep(RoleplayManager.GetTimerByMyJob(Session, "basurero") * 1000);

                        if (User.CurrentEffect != 0 && Session.GetRoleplay().EquippedWeapon == null)
                            User.ApplyEffect(0);

                        if (Session != null && Session.GetRoleplay() != null && Session.GetHabbo() != null)
                            ChooseReward(Session);
                        if (User != null)
                            User.CanWalk = true;

                        Session.GetRoleplay().BasuTrashCount++;
                        Session.SendWhisper("Contenedores: " + Session.GetRoleplay().BasuTrashCount + " / 15", 1);

                        PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Session,
                            "compose_basurero|" +
                            "basucount|" +
                            Session.GetRoleplay().BasuTrashCount + "/15");

                        if (Session.GetRoleplay().BasuTrashCount >= 15)
                            Session.SendWhisper("¡Han llegado a recolectar 15 Contenedores! Ahora vuelvan al Basurero para ':descargarcamion' y recibir su paga.", 1);

                    }).Start();
                }
                else
                    Session.SendWhisper("¡Al parecer este contenedor de basura ya ha sido recogido!", 1);
            }
        }

        public void OnWiredTrigger(Item Item)
        {
        }

        public void ChooseReward(GameClient Session)
        {
            var Random = new CryptoRandom();
            int Chance = Random.Next(1, 101);
            int SecondChance = Random.Next(1, 101);

            #region Drugs
            if (Chance <= 40)
            {
                int Amount;

                if (Chance > 30)
                {
                    Amount = Random.Next(1, 3);
                    Session.GetRoleplay().Cocaine += Amount;
                    RoleplayManager.Shout(Session, "*Encuentra " + Amount + "g de Crack dentro de la basura*", 5);
                }
                else if (Chance <= 30 && Chance > 16)
                {
                    Amount = Random.Next(1, 3);
                    Session.GetRoleplay().Medicina += Amount;
                    RoleplayManager.Shout(Session, "*Encuentra " + Amount + " medicamento(s) dentro de la basura*", 5);
                }
            }
            #endregion

            #region Money
            else if (Chance > 40 && Chance <= 65)
            {
                int Amount = Random.Next(3, 9);
                Session.GetHabbo().Credits += Amount;
                Session.GetHabbo().UpdateCreditsBalance();
                RoleplayManager.Shout(Session, "*Ha encontrado una billetera con $" + Amount + " dentro de la basura*", 5);
            }
            #endregion

            #region No Reward
            else
            {
                Session.SendWhisper("Esta vez no has encontrado nada en el contenedor.", 1);
            }
            #endregion
        }
    }
}
