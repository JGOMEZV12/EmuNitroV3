using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.General
{
    class StatsCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_general_stats"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Proporciona una lista de tus estadísticas de juego de rol."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            Group job = GroupManager.GetJob(Session.GetRoleplay().JobId);
            GroupRank rank = GroupManager.GetJobRank(Session.GetRoleplay().JobId, Session.GetRoleplay().JobRank);

            StringBuilder MarriedMesssage = new StringBuilder();
            if (Session.GetRoleplay().MarriedTo != 0) MarriedMesssage.Append(PolarEnvironment.GetGame().GetCacheManager().GenerateUser(Session.GetRoleplay().MarriedTo).Username);
            else MarriedMesssage.Append("Nadie");

            StringBuilder HijoMesssage = new StringBuilder();
            if (Session.GetRoleplay().Hijo != 0) HijoMesssage.Append(PolarEnvironment.GetGame().GetCacheManager().GenerateUser(Session.GetRoleplay().Hijo).Username);
            else HijoMesssage.Append("Nadie");

            StringBuilder JailMessage = new StringBuilder();
            if (Session.GetRoleplay().IsJailed) JailMessage.Append("Estás encarcelado por otro " + Session.GetRoleplay().JailedTimeLeft + " minutos");
            else JailMessage.Append("No estás encarcelado");

            StringBuilder DeadMessage = new StringBuilder();
            if (Session.GetRoleplay().IsDead) DeadMessage.Append("Estás muerto por otro " + Session.GetRoleplay().DeadTimeLeft + " minutos");
            else DeadMessage.Append("No estas muerto");

            StringBuilder WantedMessage = new StringBuilder();
            if (Session.GetRoleplay().IsWanted) WantedMessage.Append("Te buscan por otro " + Session.GetRoleplay().WantedTimeLeft + " minutos");
            else WantedMessage.Append("Usted no es querido");

            StringBuilder ProbationMessage = new StringBuilder();
            if (Session.GetRoleplay().OnProbation) ProbationMessage.Append("Estás en libertad condicional por otro " + Session.GetRoleplay().ProbationTimeLeft + " minutos");
            else ProbationMessage.Append("Usted no está en libertad condicional");

            StringBuilder SendhomeMessage = new StringBuilder();
            if (Session.GetRoleplay().SendHomeTimeLeft > 0) SendhomeMessage.Append("Usted es enviado a casa para otro " + Session.GetRoleplay().SendHomeTimeLeft + " minutos");
            else SendhomeMessage.Append("No se le envía a casa del trabajo");

            StringBuilder PhoneType = new StringBuilder();
            if (Session.GetRoleplay().Phone == 0)
                PhoneType.Append("No tienes teléfono");
            else
                PhoneType.Append("Tienes Teléfono con número: " + Session.GetRoleplay().PhoneNumber);

            Group Gang = GroupManager.GetGang(Session.GetRoleplay().GangId);
            GroupRank GangRank = GroupManager.GetGangRank(Session.GetRoleplay().GangId, Session.GetRoleplay().GangRank);

            string grank = "\n";
            if (Session.GetRoleplay().GangId > 1000)
            {
                if (GangRank != null)
                    grank = "Rango pandilla : " + GangRank.Name + "\n\n";
            }

            StringBuilder CarType = new StringBuilder();
            if (Session.GetRoleplay().CarType == 0)
                CarType.Append("No tienes carro");
            else if (Session.GetRoleplay().CarType == 1)
                CarType.Append("Tienes un Toyota Corolla. usa 3 combustibles cada 10 seconds");
            else if (Session.GetRoleplay().CarType == 2)
                CarType.Append("tienes un Honda Accord. usa 2 combustibles cada 10 seconds");
            else
                CarType.Append("Usted tiene el más extravagante Nissan GTR. Usted usa 1 combustible por cada 10 segundos");

            StringBuilder CarFuel = new StringBuilder();
            if (Session.GetRoleplay().CarType == 0)
                CarFuel.Append("");
            else
            {
                if (Session.GetRoleplay().CarFuel > 0)
                    CarFuel.Append("Gasolina: Tienes " + String.Format("{0:N0}", Session.GetRoleplay().CarFuel) + " Galones\n");
                else
                    CarFuel.Append("Gasolina: No tienes combustible\n");
            }

            StringBuilder MessageToSend = new StringBuilder().Append(
                                   "-------- Sus estadísticas --------\n\n" +

                                   "--- Relaciones ---\n" +
                                   "Relación sentimental con: " + MarriedMesssage + "\n" +
                                   "Relación materna con: " + HijoMesssage + "\n" +
                                   "Pandilla : " + (Gang == null ? "No tiene pandilla" : Gang.Name) + "\n" +
                                   grank +


                                   "--- Humanidad ---\n" +
                                   "Salud: " + String.Format("{0:N0}", Session.GetRoleplay().CurHealth) + "/" + Session.GetRoleplay().MaxHealth + "\n" +
                                   "Energia: " + Session.GetRoleplay().CurEnergy + "/" + Session.GetRoleplay().MaxEnergy + "\n" +
                                   "Hambre: " + Session.GetRoleplay().Hunger + "/100\n" +
                                   "Higiene: " + Session.GetRoleplay().Hygiene + "/100\n" +
                                   "Ganas de ir al baño: " + Session.GetRoleplay().Poop + "/100\n" +
                                   "Alcohol: " + Session.GetRoleplay().CurAlcohol + "/100\n" +
                                   "Animo: " + Session.GetRoleplay().Animo + "/100\n" +
                                   "Embarazo: " + Session.GetRoleplay().Embarazo + "/1\n" +
                                   "Probabilidad de SIDA: " + Session.GetRoleplay().Sida + "/100\n\n" +

                                   "--- Estadísticas nivelables ---\n" +
                                   "Inteligencia: " + Session.GetRoleplay().Intelligence + "/" + RoleplayManager.IntelligenceCap + " --- EXP: " + String.Format("{0:N0}", Session.GetRoleplay().IntelligenceEXP) + " / " + String.Format("{0:N0}", (!LevelManager.IntelligenceLevels.ContainsKey(Session.GetRoleplay().Intelligence + 1) ? 100000 : LevelManager.IntelligenceLevels[Session.GetRoleplay().Intelligence + 1])) + "\n" +
                                   "Fuerza: " + Session.GetRoleplay().Strength + "/" + RoleplayManager.StrengthCap + " --- EXP FUERZA: " + String.Format("{0:N0}", Session.GetRoleplay().StrengthEXP) + " / " + String.Format("{0:N0}", (!LevelManager.StrengthLevels.ContainsKey(Session.GetRoleplay().Strength + 1) ? 100000 : LevelManager.StrengthLevels[Session.GetRoleplay().Strength + 1])) + "\n" +
                                   "Resistencia: " + Session.GetRoleplay().Stamina + "/" + RoleplayManager.StaminaCap + " --- EXP RESISTENCIA: " + String.Format("{0:N0}", Session.GetRoleplay().StaminaEXP) + " / " + String.Format("{0:N0}", (!LevelManager.StaminaLevels.ContainsKey(Session.GetRoleplay().Stamina + 1) ? 100000 : LevelManager.StaminaLevels[Session.GetRoleplay().Stamina + 1])) + "\n\n" +

                                    "--- Trabajo ---\n" +
                                   "Trabajo: " + job.Name + " " + rank.Name + "\n" +
                                   "Pago: $" + rank.Pay + " per 10 minutos\n" +
                                   "Enviado a casa: " + SendhomeMessage + "\n" +
                                   "Minutos trabajados: " + String.Format("{0:N0}", Session.GetRoleplay().TimeWorked) + "\n\n" +

                                   "--- Encarcelado - muertes - Buscado - Libertad condicional---\n" +
                                   "Encarcelado: " + JailMessage + "\n" +
                                   "Muertes: " + DeadMessage + "\n" +
                                   "Buscado: " + WantedMessage + "\n" +
                                   "Libertad condicional: " + ProbationMessage + "\n\n" +

                                   "--- otras ---\n" +
                                   "Puñaetazos: " + String.Format("{0:N0}", Session.GetRoleplay().Punches) + "\n" +
                                   "Asesinatos: " + String.Format("{0:N0}", Session.GetRoleplay().Kills) + "\n" +
                                   "Asesinatos a coñazos: " + String.Format("{0:N0}", Session.GetRoleplay().HitKills) + "\n" +
                                   "Asesinatos con armas: " + String.Format("{0:N0}", Session.GetRoleplay().GunKills) + "\n" +
                                   "Muertes: " + String.Format("{0:N0}", Session.GetRoleplay().Deaths) + "\n" +
                                   "Policías muertos: " + String.Format("{0:N0}", Session.GetRoleplay().CopDeaths) + "\n" +
                                   "Arrestos: " + String.Format("{0:N0}", Session.GetRoleplay().Arrests) + "\n" +
                                   "Arrestado: " + String.Format("{0:N0}", Session.GetRoleplay().Arrested) + "\n" +
                                   "Evasiones: " + String.Format("{0:N0}", Session.GetRoleplay().Evasions) + "\n\n" +

                                   "--- Bancario ---\n" +
                                   "Corriente: $" + String.Format("{0:N0}", Session.GetRoleplay().BankChequings) + "\n" +
                                   "ahorro: $" + String.Format("{0:N0}", Session.GetRoleplay().BankSavings) + "\n\n" +

                                   "--- Inventario ---\n" +
                                   "Telefono: " + PhoneType + "\n" +
                                   "Carro: " + CarType + "\n" +
                                   CarFuel +
                                   "Balas: " + String.Format("{0:N0}", Session.GetRoleplay().Bullets) + "\n" +
                                   "Dinamitas: " + String.Format("{0:N0}", Session.GetRoleplay().Dynamite) + "\n" +
                                   "Cigarros: " + String.Format("{0:N0}", Session.GetRoleplay().Cigarettes) + "\n" +
                                   "Marihuana: " + String.Format("{0:N0}", Session.GetRoleplay().Weed) + " Gramos\n" +
                                   "Heroina: " + String.Format("{0:N0}", Session.GetRoleplay().Heroina) + "cc\n" +
                                   "Cocaina: " + String.Format("{0:N0}", Session.GetRoleplay().Cocaine) + " Gramos\n" +
                                   "Medicinas: " + String.Format("{0:N0}", Session.GetRoleplay().Medicina) + " Gramos\n" +
                                   "Pildoras: " + String.Format("{0:N0}", Session.GetRoleplay().Pildoras) + " Gramos\n" +
                                   "Caramelos: " + String.Format("{0:N0}", Session.GetRoleplay().Caramelos) + " Unidades\n\n" +
                                   "Chaleco: " + String.Format("{0:N0}", Session.GetRoleplay().ChalecoPor) + " Unidades\n\n" +

                                   "--- Estadísticas Agrícolas ---\n" +
                                   "Utilice el comando :farming para ver sus estadísticas de cultivo\n\n" +

                                   "--- Dueño de armas ---\n" +
                                   "Usa :misarmas Comando para ver sus armas poseídas\n");

            Session.SendMessage(new MOTDNotificationComposer(MessageToSend.ToString()));
        }
    }
}