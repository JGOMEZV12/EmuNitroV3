using System;
using System.Linq;
using System.Text;
using System.Threading;
using Polar.Utilities;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;
using System.Collections.Generic;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.Communication.Packets.Outgoing.Guides;

namespace Polar.HabboRoleplay.Bots.Types
{
    public class BusinessBot : RoleplayBotAI
    {
        int VirtualId;
        CryptoRandom Rand;
        public bool CheckForOtherWorkers;
        public int OnDutyCheckInterval;
        public int CurOnDutyCheckTime;

        public BusinessBot(int VirtualId)
        {
            this.OnDuty = true;
            this.CheckForOtherWorkers = true;
            this.CurOnDutyCheckTime = 0;
            this.VirtualId = VirtualId;

            Rand = new CryptoRandom();
        }

        public override void OnDeployed(GameClient Client)
        {
            //OnDuty = false;
            this.StartActivities();
        }

        public override void OnDeath(GameClient Client)
        {

        }

        public override void OnArrest(GameClient Client)
        {

        }

        public override void OnAttacked(GameClient Client)
        {

        }

        public override void OnUserLeaveRoom(GameClient Client)
        {
            if (!OnDuty)
                return;
        }

        public override void OnUserEnterRoom(GameClient Client)
        {
            if (!OnDuty)
                return;

            if (!GetRoomUser().IsWalking)
            {
                // Look at the user 
            }
        }

        public override void OnUserUseTeleport(GameClient Client, object[] Params)
        {
            if (!OnDuty)
                return;

            if (Client == null) return;
            if (Client.GetRoomUser() == null) return;

            if (Client == GetBotRoleplay().UserFollowing || Client == GetBotRoleplay().UserAttacking)
                GetBotRoleplay().StartTeleporting(GetRoomUser(), GetRoom(), Params);
        }

        public override void OnUserSay(RoomUser User, string Message)
        {
            if (!OnDuty)
                return;

            GameClient Client = User.GetClient();

            if (Client == null)
                return;
            HandleRequest(Client, Message);
        }

        public override void OnUserShout(RoomUser User, string Message)
        {
            if (!OnDuty)
                return;

            if (User.GetClient() == null)
                return;
            HandleRequest(User.GetClient(), Message);
        }

        public override void OnMessaged(GameClient Client, string Message)
        {
            if (!OnDuty)
                return;
        }

        public override void HandleRequest(GameClient Client, string Message)
        {
            if (!OnDuty)
                return;

            if (GetBotRoleplay().WalkingToItem)
                return;

            if (RespondToSpeech(Client, Message))
                return;

            lock (GroupManager.Jobs)
            {
                List<string> JobNames = GroupManager.Jobs.Values.Select(x => x.Name.ToLower()).ToList();
                string Name = GetBotRoleplay().Name.ToLower();

                if (Message.ToLower() == "ascender")
                {
                    var Job = GroupManager.GetJob(Client.GetRoleplay().JobId);

                    if (Job == null || Job.Id == 1)
                    {
                        string WhisperMessage = "¡Consiga un trabajo primero!";
                        Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                        return;
                    }

                    if (!GroupManager.JobExists(Job.Id, Client.GetRoleplay().JobRank + 1) || Client.GetRoleplay().JobRank > 3)
                    {
                        string WhisperMessage = "Lo siento, pero no puedo darte más ascensos. ¡Pregúntele al fundador de su empresa!";
                        Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                        return;
                    }

                    if (GroupManager.HasJobCommand(Client, "guide"))
                    {
                        string WhisperMessage = "Lo siento, pero sólo el jefe de policía puede promover!";
                        Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                        return;
                    }

                    if (GroupManager.HasJobCommand(Client, "farming"))
                    {
                        string WhisperMessage = "¡Lo siento, pero los agricultores automáticamente obtener promovido por nivelar su agricultura!";
                        Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                        return;
                    }

                    int TimeRequired = Client.GetRoleplay().JobRank * 150;

                    if (Client.GetRoleplay().TimeWorked >= TimeRequired)
                    {
                        var OldJobRank = GroupManager.GetJobRank(Job.Id, Client.GetRoleplay().JobRank);
                        var NewJobRank = GroupManager.GetJobRank(Job.Id, Client.GetRoleplay().JobRank + 1);

                        GetRoomUser().Chat("*Asciende a " + Client.GetHabbo().Username + " de" + OldJobRank.Name + " a " + NewJobRank.Name + " en " + Job.Name + "*", true);

                        if (Client.GetRoleplay().IsWorking)
                        {
                            WorkManager.RemoveWorkerFromList(Client);
                            Client.GetRoleplay().IsWorking = false;
                            Client.GetHabbo().Poof();
                        }

                        Client.GetRoleplay().JobRank++;
                        Job.UpdateJobMember(Client.GetHabbo().Id);
                        return;
                    }
                    else
                    {
                        string WhisperMessage = "Tu necesitas " + TimeRequired + " trabajados para obtener un asceso";
                        Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                        return;
                    }
                }
                if (Message.ToLower() == Name)
                    GetRoomUser().Chat("Hey " + Client.GetHabbo().Username + ", ¿Necesitas ayuda? diga: trabajos", true);
                else if (Message.ToLower() == "trabajo" || Message.ToLower() == "jobs" || Message.ToLower() == "business" || Message.ToLower() == "trabajos")
                {
                    StringBuilder JobList = new StringBuilder().Append("------ Trabajos disponibles ------\nPara solicitar un trabajo escribe el código tal cual como aparece en este listado y luego :aceptar trabajo para aceptarlo.");
                    List<GroupRank> JobRanks = GroupManager.Jobs.Values.Where(x => x.Id > 1).Select(x => GroupManager.GetJobRank(x.Id, 1)).Where(x => x != null).ToList();

                    foreach (GroupRank Rank in JobRanks)
                    {
                        Group Job = GroupManager.GetJob(Rank.JobId);

                        if (Job != null && Job.Members.Count < Rank.Limit)
                            JobList.Append("\n\nCODIGO: " + Job.Name + " \nPAGA $" + Rank.Pay + " Cada 5 minutos.\n  [" + (Rank.Limit - Job.Members.Count) + " PUESTOS DISPONIBLES] \n\n");
                    }
                    Client.SendMessage(new MOTDNotificationComposer(JobList.ToString()));
                }
                else if (JobNames.Contains(Message.ToLower()))
                {
                    Group Job = GroupManager.Jobs.Values.FirstOrDefault(x => x.Name.ToLower() == Message.ToLower());

                    if (Job == null)
                        return;

                    GroupRank JobRank = GroupManager.GetJobRank(Job.Id, 1);

                    if (JobRank == null)
                        return;

                    if (Job.Members.Values.Where(x => x.UserRank == 1).ToList().Count < JobRank.Limit)
                    {
                        if (JobRank.HasCommand("guide") && BlackListManager.BlackList.Contains(Client.GetHabbo().Id))
                        {
                            string WhisperMessage = "Lo siento, pero se le ha incluido en la lista negra de unirse a la corporación de policía!";
                            Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                            return;
                        }

                        if (Client.GetRoleplay().JobId == Job.Id)
                        {
                            string WhisperMessage = "Ya trabajas en el " + Job.Name + " empresa";
                            Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                            return;
                        }

                        if (Client.GetRoleplay().OfferManager.ActiveOffers.ContainsKey("trabajo"))
                        {
                            string WhisperMessage = "Lo sentimos, pero ya se le ha ofrecido un trabajo! Por favor escriba ': aceptar job' o ':recharzar job' primero (marque ':ofertas' para ver la oferta de trabajo)!";
                            Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                            return;
                        }

                        Client.GetRoleplay().OfferManager.CreateOffer("trabajo", 0, Job.Id, this);
                        Client.SendWhisper("Se le ha ofrecido un trabajo como " + Job.Name + " " + JobRank.Name + "DIGA ': aceptar trabajo' para conseguir contratado", 1);
                        GetRoomUser().Chat("*Ofrece a " + Client.GetHabbo().Username + " Un trabajo en el " + Job.Name + " empresa*", true);
                        return;
                    }
                    else
                    {
                        string WhisperMessage = "Lo siento pero en " + Job.Name + " Corporación no está contratando a nadie ahora mismo!";
                        Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                        return;
                    }
                }
            }
        }

        public override void StopActivities()
        {

        }

        public override void StartActivities()
        {

        }
    }
}