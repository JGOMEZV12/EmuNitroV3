using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.HabboRoleplay.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.HabboHotel.Rooms.Chat.Commands.SpecialRights
{
    class KillCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_kill"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "Matar usuario"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length != 2)
            {
                Session.SendWhisper("Debe ingresar el nombre de usuario que desea matar.", 1);
                return;
            }

            var TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);

            if (TargetClient == null)
            {
                Session.SendWhisper("Este usuario no se pudo encontrar! Quizás están fuera de línea.", 1);
                return;
            }

            if (TargetClient.GetHabbo() == null || TargetClient.GetRoomUser() == null)
            {
                Session.SendWhisper("Este usuario no se pudo encontrar! Quizás están fuera de línea.", 1);
                return;
            }

            if (TargetClient.GetHabbo().VIPRank > 1)
            {
                Session.SendWhisper("¡No puedes matar a otros super miembros del personal!", 1);
                return;
            }
            if (TargetClient.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes matar a una persona encarcelada!", 1);
                return;
            }

            if (TargetClient.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡Esa persona ya está muerta!", 1);
                return;
            }

            if (TargetClient.GetRoleplay().IsWorking)
            {
                WorkManager.RemoveWorkerFromList(TargetClient);
                TargetClient.GetRoleplay().IsWorking = false;
                TargetClient.GetHabbo().Poof();
            }

            if (TargetClient.GetRoleplay().Cuffed)
                TargetClient.GetRoleplay().Cuffed = false;
            string Message = "*Utiliza sus poderes divinos y convoca un rayo, matando a " + TargetClient.GetHabbo().Username + " instantaneamente*";
            int Bubble = 23;

            if (Params[0].ToLower() == "snap")
            {
                Message = "*Aprieta los dedos, haciendo explotar a " + TargetClient.GetHabbo().Username + "*";
                Bubble = 24;
            }

            Session.Shout(Message, Bubble);
            TargetClient.GetRoleplay().CurHealth = 0;
            Session.GetRoleplay().ClearWebSocketDialogue(true);
        }
    }
}
