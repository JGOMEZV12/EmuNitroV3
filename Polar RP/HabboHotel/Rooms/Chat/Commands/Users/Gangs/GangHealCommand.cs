using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Polar.Utilities;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.Database.Interfaces;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Gangs
{
    class GangHealCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_gang_heal"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "Utiliza uno de los botiquines de tu pandilla para curar a un miembro de la pandilla."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length != 2)
            {
                Session.SendWhisper("¡Escriba el nombre de usuario del miembro de la pandilla que desea sanar!", 1);
                return;
            }

            if (Session.GetRoleplay().SpecialCooldowns["medipacks"] > 0)
            {
                Session.SendWhisper("Debes esperar para volver a utilizar otra botiquin!");
                return;
            }

            Group Gang = GroupManager.GetGang(Session.GetRoleplay().GangId);
            GroupRank GangRank = GroupManager.GetGangRank(Session.GetRoleplay().GangId, Session.GetRoleplay().GangRank);

            if (Gang == null)
            {
                Session.SendWhisper("¡No eres parte de ninguna pandilla!", 1);
                return;
            }

            if (Gang.MediPacks <= 0)
            {
                Session.SendWhisper("¡A su pandilla no le quedan medicinas!", 1);
                return;
            }

            if (!GroupManager.HasGangCommand(Session, "gheal"))
            {
                Session.SendWhisper("¡No tienes permiso para usar este comando, solo médico de pandilla puede!", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);

            if (TargetClient == null)
            {
                Session.SendWhisper("Lo sentimos, pero este usuario no pudo ser encontrado", 1);
                return;
            }

            RoomUser RoomUser = Session.GetRoomUser();
            RoomUser TargetRoomUser = Room.GetRoomUserManager().GetRoomUserByHabbo(TargetClient.GetHabbo().Username);
            if (TargetRoomUser == null)
            {
                Session.SendWhisper("Se produjo un error al encontrar a ese usuario, tal vez no están en línea o en esta sala.", 1);
                return;
            }

            if (TargetClient.GetRoleplay().CurHealth == TargetClient.GetRoleplay().MaxHealth)
            {
                Session.SendWhisper(TargetClient.GetHabbo().Username + " no necesita ser curado", 1);
                return;
            }

            Point ClientPos = new Point(RoomUser.X, RoomUser.Y);
            Point TargetClientPos = new Point(TargetRoomUser.X, TargetRoomUser.Y);
            double Distance = RoleplayManager.GetDistanceBetweenPoints2D(ClientPos, TargetClientPos);

            if (Distance > 1)
            {
                Session.SendWhisper("Acércate a " + TargetClient.GetHabbo().Username + " para sanarlos con un medipack!", 1);
                return;
            }

            Session.Shout("*Saca un medipack de su almacenamiento de pandillas y aplica algunas vendas en " + TargetClient.GetHabbo().Username + " para curarlo*", 4);
            Gang.MediPacks -= 1;

            // BattlePass Challenge Integration
            PolarEnvironment.GetGame().GetBattlePassManager().ProgressChallenge(Session, "gang_heal", 1);

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                dbClient.RunQuery("UPDATE `rp_gangs` SET `medipacks` = '" + Gang.MediPacks + "' WHERE `id` = '" + Gang.Id + "'");

            CryptoRandom Random = new CryptoRandom();
            int HealAmount = Random.Next(30, 50);

            new Thread(() =>
            {
                Thread.Sleep(3000);

                if (!TargetClient.GetRoleplay().IsDead)
                {
                    if (TargetRoomUser != null)
                        TargetRoomUser.ApplyEffect(23);

                    int NewHealth = TargetClient.GetRoleplay().CurHealth + HealAmount;

                    if (NewHealth > TargetClient.GetRoleplay().MaxHealth)
                        TargetClient.GetRoleplay().CurHealth = TargetClient.GetRoleplay().MaxHealth;
                    else
                        TargetClient.GetRoleplay().CurHealth = NewHealth;

                    TargetClient.SendWhisper(Session.GetHabbo().Username + " ¡el medipack comienza a tener efecto!", 1);
                }
            }).Start();
            Session.GetRoleplay().CooldownManager.CreateCooldown("medipacks", 1000, 300);
            Session.GetRoleplay().SpecialCooldowns.TryUpdate("medipacks", 300, Session.GetRoleplay().SpecialCooldowns["medipacks"]);
        }
    }
}