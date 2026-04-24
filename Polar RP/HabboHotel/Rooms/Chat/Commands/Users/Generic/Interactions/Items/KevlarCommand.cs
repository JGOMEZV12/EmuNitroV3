using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Subscriptions;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.Food;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Quests;
using Polar.Core;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Interactions.Items
{
    class KevlarCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_police_kevlar"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Agarra el Kevlar que esta frente a ti."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            try
            {
                #region Variables
                Item Item = null;
                RoomUser User = Session?.GetRoomUser();
                #endregion

                #region Conditions
                if (User == null)
                    return;

                var roleplay = Session.GetRoleplay();
                if (roleplay == null)
                {
                    Session.SendWhisper("Ocurrió un error al recuperar tus datos de rol.", 1);
                    return;
                }

                if (roleplay.IsDead)
                {
                    Session.SendWhisper("¡No puedes usar esto estando muerto!", 1);
                    return;
                }

                if (roleplay.IsJailed)
                {
                    Session.SendWhisper("¡No puedes usar esto en la cárcel!", 1);
                    return;
                }

                if (roleplay.ChalecoPor > 0)
                {
                    Session.SendWhisper("¡Ya tienes un chaleco antibalas equipado!", 1);
                    return;
                }

                if (!GroupManager.HasJobCommand(Session, "law"))
                {
                    Session.SendWhisper("¡Sólo un oficial de policía puede utilizar este comando!", 1);
                    return;
                }

                if (!roleplay.IsWorking)
                {
                    Session.SendWhisper("¡Debes estar trabajando para usar este comando!", 1);
                    return;
                }

                if (Session.GetHabbo().Credits < 500)
                {
                    Session.SendWhisper("¡Necesitas 500 créditos para comprar un chaleco! Trabaja más duro.", 1);
                    return;
                }

                var job = GroupManager.GetJob(roleplay.JobId);
                if (job == null)
                {
                    Session.SendWhisper("¡Ocurrió un error al obtener tu trabajo!", 1);
                    return;
                }

                var rank = GroupManager.GetJobRank(job.Id, roleplay.JobRank);
                if (rank == null || !rank.CanWorkHere(Room.Id))
                {
                    Session.SendWhisper($"Esta no es una de tus salas de trabajo. Sólo puedes trabajar en las salas: {String.Join(",", rank?.WorkRooms)}.", 1);
                    return;
                }
                #endregion

                #region Execute
                var roomHandler = Room?.GetRoomItemHandler();
                if (roomHandler != null && roomHandler.GetFloor != null)
                {
                    Item fenceToRemove = roomHandler.GetFloor.FirstOrDefault(x => x.BaseItem == 20074);

                    if (fenceToRemove != null)
                    {
                        roomHandler.RemoveFurniture(null, fenceToRemove.Id);
                    }
                }

                roleplay.ChalecoPor = 300;
                Session.GetHabbo().Credits -= 500;
                Session.GetHabbo().UpdateCreditsBalance();
                HabboRoleplay.Misc.RoleplayManager.GetLookAndMotto(Session, "poof");
                Session.Shout("*Se equipa con chaleco antibalas y un casco militar [+300 protección]*", 4);

                if (Item != null)
                {
                    roomHandler?.RemoveFurniture(Session, Item.Id);
                }
                #endregion
            }
            catch (Exception ex)
            {
                Logging.LogException($"KevlarCommand.Execute: Error - {ex.Message}");
            }
        }

    }
}