using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Bots.Manager;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Administrators
{
    class BotRPCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_override"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Teclea :coordbot o :coorbot info para más información"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            RoomUser User = Session.GetRoomUser();
            if (User == null)
                return;

            if (Params.Length == 1)
            {
                Session.SendWhisper("Uy, no olvide introducir el nombre del bot!");
                return;
            }

            if (Params[1].ToLower() == "info")
            {
                StringBuilder Message = new StringBuilder().Append("----- Cambiar coordenadas del bot -----\n\n");
                Message.Append("Si el bot tiene un espacio en su nombre. Ej: <b>'Dr. Sevilla'</b> deberas colocar asi: <b>'Dr.%Sevilla'</b>\n\n");
                Message.Append("Ejecuta: <b>:coordbot Dr.%Sevilla X,Y,Z,R</b>\n\n");
                Message.Append("<b>OJO:</b> Ej: <b>':coordbot (BotName) 19,13,4,0'</b> que equivalen a <b>X,Y,Z,R</b>!");
                Session.SendNotification(Message.ToString());
                return;
            }

            if (Params.Length < 3)
            {
                Session.SendWhisper("Faltan las coordenadas! Usa: :coordbot NombreBot X,Y,Z,R");
                return;
            }

            string BotName = Params[1].Replace("%", " ");
            object[] Coords = Params[2].Split(',');

            if (Coords.Length < 4)
            {
                Session.SendWhisper("Las coordenadas deben tener formato X,Y,Z,R — Ej: 19,13,4,0");
                return;
            }

            int coordX, coordY, coordZ, coordR;
            if (!int.TryParse(Coords[0].ToString(), out coordX) ||
                !int.TryParse(Coords[1].ToString(), out coordY) ||
                !int.TryParse(Coords[2].ToString(), out coordZ) ||
                !int.TryParse(Coords[3].ToString(), out coordR))
            {
                Session.SendWhisper("Las coordenadas deben ser números enteros.");
                return;
            }

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("UPDATE `rp_bots` SET `spawn_x` = '" + coordX + "', `spawn_y` = '" + coordY + "', `spawn_z` = '" + coordZ + "', `spawn_rot` = '" + coordR + "' WHERE `name` = '" + BotName + "' AND `spawn_id` = '" + Room.Id + "'");
            }

            RoomUser TargetBot = Room.GetRoomUserManager().GetRoleplayBotByName(BotName);

            if (TargetBot != null)
            {
                int BotId = TargetBot.GetBotRoleplay().Id;

                if (RoleplayBotManager.CachedRoleplayBots.ContainsKey(BotId))
                {
                    RoleplayBotManager.CachedRoleplayBots[BotId].X = coordX;
                    RoleplayBotManager.CachedRoleplayBots[BotId].Y = coordY;
                    RoleplayBotManager.CachedRoleplayBots[BotId].Z = coordZ;
                    RoleplayBotManager.CachedRoleplayBots[BotId].SpawnRot = coordR;
                }

                RoleplayBotManager.EjectDeployedBot(TargetBot, Room);
                RoleplayBotManager.DeployBotByID(BotId, "default");

                Session.SendWhisper("Bot '" + BotName + "' reposicionado correctamente en " + coordX + "," + coordY + "," + coordZ + " rot:" + coordR);
            }
            else
            {
                Session.SendWhisper("No se encontró ningún bot llamado '" + BotName + "' en esta sala.");
            }
        }
    }
}
