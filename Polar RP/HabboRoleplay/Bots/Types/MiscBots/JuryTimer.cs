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
using Polar.Communication.Packets.Outgoing.Polls;
using Polar.HabboHotel.Polls;
using Polar.Core;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Attack timer
    /// </summary>
    public class JuryTimer : BotRoleplayTimer
    {
        public JuryTimer(string Type, RoleplayBot CachedBot, int Time, bool Forever, object[] Params) 
            : base(Type, CachedBot, Time, Forever, Params)
        {
            TimeCount = 0;
        }
 
        /// <summary>
        /// Begins chasing the client
        /// </summary>
        public override void Execute()
        {
            try
            {
                if (base.CachedBot == null || base.CachedBot.DRoomUser == null || base.CachedBot.DRoom == null)
                {

                    SpawnJuryUsersOut(false);
                    Thread.Sleep(200);

                    RoleplayManager.CourtVoteEnabled = false;
                    RoleplayManager.InnocentVotes = 0;
                    RoleplayManager.GuiltyVotes = 0;

                    RoleplayManager.CourtJuryTime = 0;
                    RoleplayManager.CourtTrialIsStarting = false;
                    RoleplayManager.CourtTrialStarted = false;
                    RoleplayManager.Defendant = null;
                    RoleplayManager.InvitedUsersToJuryDuty.Clear();

                    base.EndTimer();

                    return;
                }

                GameClient Client = RoleplayManager.Defendant;
                int CourtRoomId = Convert.ToInt32(RoleplayData.GetData("court", "roomid"));
                RoleplayManager.GenerateRoom(CourtRoomId, out Room Room);

                if (Client == null || Client.LoggingOut || Client.GetHabbo() == null || Client.GetRoleplay() == null)
                {
                    SpawnJuryUsersOut(false);

                    RoleplayManager.CourtVoteEnabled = false;
                    RoleplayManager.InnocentVotes = 0;
                    RoleplayManager.GuiltyVotes = 0;

                    RoleplayManager.CourtJuryTime = 0;
                    RoleplayManager.CourtTrialIsStarting = false;
                    RoleplayManager.CourtTrialStarted = false;
                    RoleplayManager.Defendant = null;
                    RoleplayManager.InvitedUsersToJuryDuty.Clear();

                    if (base.CachedBot != null)
                    {
                        if (base.CachedBot.DRoomUser != null)
                        {
                            base.CachedBot.DRoomUser.Chat("Hola, como el acusado ha dejado el caso de la corte ha sido detenido. ¡Disculpas por cualquier inconveniente!", false, 2);
                        }
                    }
                    base.EndTimer();
                    return;
                }

                RoleplayManager.CourtJuryTime++;

                if (RoleplayManager.CourtJuryTime < 151)
                {
                    if (RoleplayManager.CourtJuryTime == 2)
                    {
                        RoleplayManager.SendUserOld2(Client, CourtRoomId, "");
                        RoleplayManager.GetLookAndMotto(Client);
                        RoleplayManager.SpawnChairs(Client, "uni_lectern");
                        return;
                    }
                    else if (RoleplayManager.CourtJuryTime == 5)
                    {
                        base.CachedBot.DRoomUser.Chat("Bienvenidos, damas y caballeros. invitados al caso de la ciudad del Departamento de Policía de " + PolarEnvironment.GetConfig().data["hotel.name"] + " vs " + Client.GetHabbo().Username + ".", false, 2);
                        return;
                    }
                    else if (RoleplayManager.CourtJuryTime == 10)
                    {
                        if (Client.GetRoleplay().WantedFor != "")
                            base.CachedBot.DRoomUser.Chat("The defendant is accused of " + Client.GetRoleplay().WantedFor.TrimEnd(',', ' ') + ".", false, 2);
                        else
                            base.CachedBot.DRoomUser.Chat("Desafortunadamente, no puedo recuperar las acusaciones del acusado por este juicio, por lo que el jurado escuchará su explicación " + Client.GetHabbo().Username + "!", false, 2);
                        return;
                    }
                    else if (RoleplayManager.CourtJuryTime == 18)
                    {
                        base.CachedBot.DRoomUser.Chat("El acusado está apelando la decisión del Departamento de Policía de " + PolarEnvironment.GetConfig().data["hotel.name"] + " de que son culpables.", false, 2);
                        return;
                    }
                    else if (RoleplayManager.CourtJuryTime == 26)
                    {
                        base.CachedBot.DRoomUser.Chat(Client.GetHabbo().Username + ", Por favor explique a mí mismo y al jurado exactamente ¿Qué sucedió?", false, 2);
                        return;
                    }
                    else if (RoleplayManager.CourtJuryTime == 90)
                    {
                        base.CachedBot.DRoomUser.Chat("Gracias por su explicación.", false, 2);
                        return;
                    }
                    else if (RoleplayManager.CourtJuryTime == 96)
                    {
                        base.CachedBot.DRoomUser.Chat(Client.GetHabbo().Username + ", Si el jurado encuentra que eres inocente, serás liberado de la cárcel.", false, 2);
                        return;
                    }
                    else if (RoleplayManager.CourtJuryTime == 102)
                    {
                        base.CachedBot.DRoomUser.Chat("Sin embargo, si encuentran que usted es culpable, usted permanecerá en la cárcel para servir el resto de su condena.", false, 2);
                        return;
                    }
                    else if (RoleplayManager.CourtJuryTime == 108)
                    {
                        base.CachedBot.DRoomUser.Chat("Oficial, Por favor, quita " + Client.GetHabbo().Username + ".", false, 2);
                        return;
                    }
                    else if (RoleplayManager.CourtJuryTime == 110)
                    {
                        string MyCity = Room.City;

                        HabboRoleplay.RPRoom.RPRoom Data;
                        int JailRID = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetJail(MyCity, out Data);
                        RoleplayManager.SendUserOld2(Client, JailRID);
                        return;
                    }
                    else if (RoleplayManager.CourtJuryTime == 115)
                    {
                        base.CachedBot.DRoomUser.Chat("Ahora depende de usted, el jurado, decidir el destino del acusado.", false, 2);
                        return;
                    }
                    else if (RoleplayManager.CourtJuryTime == 120)
                    {
                        RoleplayManager.CourtVoteEnabled = true;
                        base.CachedBot.DRoomUser.Chat("Por favor vote diciendo ':votar inocente' o ':votar culpable'", false, 2);
                        return;
                    }
                    // FIX Bug 6: Thread.Sleep reemplazado por ticks adicionales (evita starvation del thread pool)
                    else if (RoleplayManager.CourtJuryTime == 140)
                    {
                        int CourtResult = Math.Max(RoleplayManager.InnocentVotes, RoleplayManager.GuiltyVotes);
                        // Primer mensaje — el segundo vendrá en el tick 144 (4 ticks = ~4s)
                        if (CourtResult == 0 || CourtResult == RoleplayManager.GuiltyVotes)
                            base.CachedBot.DRoomUser.Chat("Gracias por su voto. El jurado ha encontrado al acusado, " + Client.GetHabbo().Username + ", culpable de todos los cargos.", false, 2);
                        else
                            base.CachedBot.DRoomUser.Chat("Gracias por su voto. El jurado ha encontrado al acusado, " + Client.GetHabbo().Username + ", Inocente de todos los crímenes.", false, 2);
                        return;
                    }
                    else if (RoleplayManager.CourtJuryTime == 144)
                    {
                        int CourtResult = Math.Max(RoleplayManager.InnocentVotes, RoleplayManager.GuiltyVotes);
                        if (CourtResult == 0 || CourtResult == RoleplayManager.GuiltyVotes)
                        {
                            base.CachedBot.DRoomUser.Chat("El acusado permanecerá en la cárcel y servirá el resto de su condena allí.", false, 2);
                            Client.SendNotification("El jurado lo ha declarado culpable de todos los crímenes. ¡Permanecerá en la cárcel y servirá el resto de su condena!");
                        }
                        else
                        {
                            base.CachedBot.DRoomUser.Chat("Por la presente libero al acusado y los perdono de todos los delitos.", false, 2);
                            Client.SendNotification("El jurado te ha encontrado inocente de todos los crímenes. ¡Usted ha sido liberado de la prisión!");
                            Client.GetRoleplay().IsJailed = false;
                            Client.GetRoleplay().JailedTimeLeft = 0;
                        }
                        return;
                    }
                    else if (RoleplayManager.CourtJuryTime == 150)
                    {
                        base.CachedBot.DRoomUser.Chat("El jurado es agradecido y excusado. La corte se aplazó.", false, 2);

                        if (Room != null)
                        {
                            SpawnJuryUsersOut(true);
                        }
                        return;
                    }
                    return;
                }

                RoleplayManager.CourtVoteEnabled = false;
                RoleplayManager.InnocentVotes = 0;
                RoleplayManager.GuiltyVotes = 0;

                RoleplayManager.CourtJuryTime = 0;
                RoleplayManager.CourtTrialIsStarting = false;
                RoleplayManager.CourtTrialStarted = false;
                RoleplayManager.Defendant = null;
                RoleplayManager.InvitedUsersToJuryDuty.Clear();
                base.EndTimer();

            }
            catch (Exception e)
            {
                Logging.LogRPTimersError("Error in Execute() void: " + e);
                base.EndTimer();
            }
        }

        private void SpawnJuryUsersOut(bool Reward)
        {

            #region Null checks & Court Variables
            if (RoleplayManager.InvitedUsersToJuryDuty == null) return;
            if (RoleplayManager.InvitedUsersToJuryDuty.Count <= 0) return;
            int CourtRoomId = Convert.ToInt32(RoleplayData.GetData("court", "roomid"));
            RoleplayManager.GenerateRoom(CourtRoomId, out Room Room);

            if (Room == null) return;
            int Award = new CryptoRandom().Next(Convert.ToInt32(RoleplayData.GetData("court", "minaward")), Convert.ToInt32(RoleplayData.GetData("court", "maxaward")));
            #endregion

            lock (RoleplayManager.InvitedUsersToJuryDuty)
            {
                foreach (RoomUser User in Room.GetRoomUserManager().GetRoomUsers())
                {
                    #region Null checks
                    if (User == null) continue;
                    if (User.GetClient() == null) continue;
                    if (User.GetClient().GetHabbo() == null) continue;
                    if (User.GetClient().GetRoleplay() == null) continue;
                    if (!RoleplayManager.InvitedUsersToJuryDuty.Contains(User.GetClient())) continue;
                    #endregion

                    #region Reward
                    if (Reward)
                    {
                        User.GetClient().GetHabbo().Credits += Award;
                        User.GetClient().GetHabbo().UpdateCreditsBalance();
                        User.GetClient().SendWhisper("El jurado le ha otorgado $" + String.Format("{0:N0}", Award) + " por tu tiempo. ¡Gracias!", 1);
                    }
                    #endregion

                    RoleplayManager.SpawnChairs(User.GetClient(), "sofachair_silo*2");
                    User.Frozen = false;
                }
            }
        }
    }
}