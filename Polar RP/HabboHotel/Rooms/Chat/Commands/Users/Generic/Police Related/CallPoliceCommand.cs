using System;
using System.Linq;
using System.Text;
using Polar.Utilities;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Guides;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Guides;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Moderation;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Police
{
    class CallPoliceCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_police_related_call_police"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "llama a la policía"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Por favor escriba un mensaje para que el oficial vea!", 1);
                return;
            }


            if (Session.GetRoleplay().TryGetCooldown("call911"))
                return;

            if (Groups.GroupManager.HasJobCommand(Session, "guide") && Session.GetRoleplay().IsWorking)
            {
                Session.SendWhisper("¡No puedes enviar una llamada de auxilio a otra policía si estás trabajando!", 1);
                return;
            }

            if (Room.TurfEnabled)
            {
                Session.SendWhisper("¡No puedes enviar una llamada de ayuda si estás dentro de una sala de césped!", 1);
                return;
            }

            if (Session.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes enviar una llamada de ayuda si estás encarcelado!", 1);
                return;
            }

            string Message = CommandManager.MergeParams(Params, 1);

            RoleplayManager.Shout(Session, "*Llama al 911 por ayuda*", 5);

            if (string.IsNullOrEmpty(Message))
                PolarEnvironment.GetGame().GetClientManager().JailAlert(Session.GetHabbo().Username + " ha llamado al 911 en " + Room.Name + " ¡Ve allí rápidamente!");
            else
                PolarEnvironment.GetGame().GetClientManager().JailAlert(Session.GetHabbo().Username + " ha llamado al 911 en " + Room.Name + " con el mensaje: " + Message);

            Session.GetRoleplay().CooldownManager.CreateCooldown("call911", 1000, 30);
            //Old
            /* GuideManager guideManager = PolarEnvironment.GetGame().GetGuideManager();
             List<GameClient> HandlingCalls = guideManager.HandlingCalls();

             if (Message.Length <= 10)
             {
                 Session.SendWhisper("¡Escriba un mensaje más descriptivo para que el oficial de policía vea!", 1);
                 return;
             }

             if (HandlingCalls.Count < 1)
             {
                 Session.SendMessage(new OnGuideSessionError());
                 return;
             }

             CryptoRandom Random = new CryptoRandom();
             GameClient RandomPolice = null;

             if (HandlingCalls.Count > 1)
                 RandomPolice = HandlingCalls[Random.Next(0, HandlingCalls.Count)];
             else
                 RandomPolice = HandlingCalls[0];

             if (RandomPolice == null)
             {
                 Session.SendMessage(new OnGuideSessionError());
                 return;
             }

             Session.SendWhisper("¡Tu solicitud de ayuda ha sido enviada!", 1);
             RandomPolice.SendMessage(new BroadcastMessageAlertComposer(Message));


             Session.GetRoleplay().SentRealCall = false;
             Session.GetRoleplay().Sent911Call = true;
             Session.GetRoleplay().CallMessage = Message;
             Session.GetRoleplay().GuideOtherUser = RandomPolice;
             RandomPolice.GetRoleplay().GuideOtherUser = Session;*/
        }
    }
}