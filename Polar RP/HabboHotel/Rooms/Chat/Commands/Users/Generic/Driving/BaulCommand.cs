using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;
using Polar.HabboHotel.Items;
using Polar.HabboRoleplay.Vehicles;
using System.Data;
using System.Text.RegularExpressions;
using Polar.HabboRoleplay.VehicleOwned;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Offers
{
    class BaulCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_baul"; }
        }
        // :baul [abrir/cerrar]

        // :baul [guardar] [objeto]
        // :baul [sacar] [id]

        // :baul [sacar] [id] [cantidad]
        // :baul [guardar] [consumible] [cantidad]
        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Interactúa con el Maletero de un vehículo."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            int Dir = 0;
            string toDo = "";

            #region Conditions
            if (Params.Length == 3)
            {
                Dir = 1;
                toDo = Params[1].ToLower();// Guardar/Sacar
            }
            else if (Params.Length == 4)
            {
                Dir = 2;
                toDo = Params[1].ToLower();// Guardar/Sacar (consumibles)
            }
            else if (Params.Length == 2)
            {
                Dir = 3;
                toDo = Params[1].ToLower();// Abrir/Cerrar
            }
            else if (Params.Length == 1)
            {
                Dir = 4;// Ver Interior
            }
            else
            {
                Session.SendWhisper("Comando inválido, usa ':ayuda vehiculos' para ver más información acerca de los maleteros.", 1);
                return;
            }

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
                Session.SendWhisper("¡No puedes hacer eso mientras estás muert@!", 1);
                return;
            }
            if (Session.GetRoleplay().DrivingCar)
            {
                Session.SendWhisper("¡No puedes hacer eso mientras conduces!", 1);
                return;
            }
            #endregion

            if (Session.GetRoleplay().TryGetCooldown("baul", true))
            {
                Session.SendWhisper("Por favor espera un poco para hacer eso nuevamente.", 1);
                return;
            }
            #endregion

            #region Get Car Information
            #region Check Vehicle   
            Vehicle vehicle = null;
            bool found = false;
            int itemfurni = 0, corp = 0, maxtrunk = 0;
            Item BTile = null;
            string itemnm = null;
            foreach (Vehicle Vehicle in VehicleManager.Vehicles.Values)
            {
                if (!found)
                {
                    BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == Vehicle.ItemName && x.Coordinate == Session.GetRoomUser().Coordinate);
                    if (BTile != null)
                    {
                        vehicle = Vehicle;
                        itemfurni = BTile.Id;
                        itemnm = Vehicle.ItemName;
                        corp = Convert.ToInt32(Vehicle.CarCorp);
                        maxtrunk = Convert.ToInt32(Vehicle.MaxTrunks) + Session.GetHabbo().VIPRank;
                        found = true;
                    }
                }
            }

            //Al examinar todos los autos ninguno conincide con el item donde está parado el user...
            if (!found)
            {
                Session.SendWhisper("¡Debes estar sobre un vehículo para interactuar con el maletero!", 1);
                return;
            }
            #endregion

            #region Car Conditions
            if (corp > 0)
            {
                Session.SendWhisper("Lo sentimos, pero no es posible interactuar con el Maletero de los Vehículos de Trabajo.", 1);
                return;
            }
            #endregion

            #region Select Vehicle Stats
            List<VehiclesOwned> VO = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedByFurniId(itemfurni);
            if (VO == null || VO.Count <= 0)
            {
                RoleplayManager.Shout(Session, "* Una grúa ha pasado a recoger el vehículo que " + Session.GetHabbo().Username + " intentaba interactuar.", 4);
                RoleplayManager.PickItem(Session, itemfurni);
                return;
            }
            #endregion

            #endregion

            #region Execute
            if (Dir == 4)// Ver interior
            {
                #region Execute
                if (!VO[0].BaulOpen)
                {
                    Session.SendWhisper("El maletero se encuentra cerrado.", 1);
                    return;
                }

                int Ind = 0;
                string Interior = "";
                foreach (string content in VO[0].Baul)
                {
                    Ind++;
                    if (content.Length > 0)
                        Interior += "[" + Ind + "] " + content + "<br>";
                    else if (Ind <= maxtrunk)
                        Interior += "[" + Ind + "] Vacío <br>";
                }
                if (Ind < maxtrunk)
                {
                    int re = (maxtrunk - Ind);
                    for (int j = 0; j < re; j++)
                    {
                        Ind++;
                        Interior += "[" + Ind + "] Vacío <br>";
                    }
                }
                Session.GetRoleplay().CarWSBaul = Interior;
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "baul");
                RoleplayManager.Shout(Session, "*Mira el interior del Matelero*", 5);
                #endregion
            }
            else if (Dir == 3)// Abrir/Cerrar
            {
                switch (toDo)
                {
                    case "abrir":
                        {
                            #region Abrir
                            if (VO[0].OwnerId != Session.GetHabbo().Id)
                            {
                                Session.SendWhisper("Solo el propietario del Vehículo puede abrir el Maletero.", 1);
                                return;
                            }
                            if (VO[0].BaulOpen)
                            {
                                Session.SendWhisper("El maletero ya se encuentra abierto.", 1);
                                return;
                            }
                            VO[0].BaulOpen = true;
                            RoleplayManager.UpdateVehicleBaul(itemfurni, "baul_state", 1);
                            RoleplayManager.Shout(Session, "*Abre el maletero de su vehículo*", 5);
                            break;
                            #endregion
                        }
                    case "cerrar":
                        {
                            #region Cerrar
                            if (VO[0].OwnerId != Session.GetHabbo().Id)
                            {
                                Session.SendWhisper("Solo el propietario del Vehículo puede cerrar el Maletero.", 1);
                                return;
                            }
                            if (!VO[0].BaulOpen)
                            {
                                Session.SendWhisper("El maletero ya se encuentra cerrado.", 1);
                                return;
                            }
                            VO[0].BaulOpen = false;
                            RoleplayManager.UpdateVehicleBaul(itemfurni, "baul_state", 0);

                            Session.GetRoleplay().CarWSBaul = "";
                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "closebaul");
                            RoleplayManager.Shout(Session, "*Cierra el maletero de su vehículo*", 5);
                            break;
                            #endregion
                        }
                    default:
                        {
                            Session.SendWhisper("Comando inválido. Quizás quisiste usar ':baul abrir' ó ':baul cerrar'.", 1);
                        }
                        break;
                }
            }
            else if (Dir == 2)// Guardar/Sacar (consumibles)
            {
                switch (toDo)
                {
                    case "guardar": // :baul [guardar] [consumible] [cantidad]
                        {
                            #region Guardar                            
                            if (!VO[0].BaulOpen)
                            {
                                Session.SendWhisper("El maletero no encuentra abierto para poder guardar objetos.", 1);
                                return;
                            }
                            switch (Params[2])
                            {
                                case "heroina":
                                    {
                                        #region Execute
                                        int Cant = 0;
                                        if (!int.TryParse(Params[3], out Cant))
                                        {
                                            Session.SendWhisper("Cantidad inválida.", 1);
                                            return;
                                        }
                                        if (Cant <= 0)
                                        {
                                            Session.SendWhisper("Debes ingresar al menos una cantidad de 1.", 1);
                                            return;
                                        }
                                        if (Session.GetRoleplay().Heroina < Cant)
                                        {
                                            Session.SendWhisper("No tienes esa cantidad de Heroina para guardar.", 1);
                                            return;
                                        }

                                        int mytrunk = 0;
                                        string IntBaul = string.Join(";", VO[0].Baul);

                                        //To Check Cants
                                        int CurIntCant = 0;
                                        bool find = false;
                                        foreach (string content in VO[0].Baul)
                                        {
                                            string[] stringSeparatorsInt = new string[] { "-" };
                                            string[] resultInt;
                                            resultInt = content.Split(stringSeparatorsInt, StringSplitOptions.RemoveEmptyEntries);
                                            /*if (content.Contains("-"))
                                            {
                                                foreach (string cantcontnet in resultInt)
                                                {
                                                    if (resultInt[1].Contains("-g. Cocaína"))
                                                    {
                                                        if (int.TryParse(resultInt[0].ToString(), out int CurCant))
                                                        {
                                                            CurIntCant = CurCant;
                                                            int newCant = CurIntCant + Cant;
                                                            StringBuilder builder = new StringBuilder(IntBaul);
                                                            builder.Replace(CurIntCant + "-g. Cocaína;", newCant + "-g. Cocaína;");
                                                            IntBaul = builder.ToString();
                                                            find = true;
                                                        }
                                                    }
                                                }
                                            }*/
                                            if (content.Length > 0)
                                                mytrunk++;
                                        }
                                        if (mytrunk >= maxtrunk)
                                        {
                                            Session.SendWhisper("Al parecer ya no hay más espacio en el maletero.", 1);
                                            return;
                                        }
                                        if (!find)
                                            IntBaul += Cant + "-cc. Heroina;";

                                        VO[0].Baul = IntBaul.Split(';');
                                        RoleplayManager.UpdateVehicleBaul(itemfurni, "baul", IntBaul);
                                        Session.GetRoleplay().Heroina -= Cant;
                                        RoleplayManager.Shout(Session, "*Guarda algo en el maletero del vehículo*", 5);

                                        #region WS Refresh
                                        if (Session.GetRoleplay().ViewBaul)
                                        {
                                            int Ind = 0;
                                            string Interior = "";
                                            foreach (string content in VO[0].Baul)
                                            {
                                                Ind++;
                                                if (content.Length > 0)
                                                    Interior += "[" + Ind + "] " + content + "<br>";
                                                else if (Ind <= maxtrunk)
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                            }
                                            if (Ind < maxtrunk)
                                            {
                                                int re = (maxtrunk - Ind);
                                                for (int j = 0; j < re; j++)
                                                {
                                                    Ind++;
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                                }
                                            }
                                            Session.GetRoleplay().CarWSBaul = Interior;
                                            Session.GetRoleplay().UpdateInteractingUserDialogues();
                                            Session.GetRoleplay().RefreshStatDialogue();
                                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "baul");
                                        }
                                        #endregion
                                        break;
                                        #endregion
                                    }
                                case "crack":
                                case "cocaina":
                                    {
                                        #region Execute
                                        int Cant = 0;
                                        if (!int.TryParse(Params[3], out Cant))
                                        {
                                            Session.SendWhisper("Cantidad inválida.", 1);
                                            return;
                                        }
                                        if (Cant <= 0)
                                        {
                                            Session.SendWhisper("Debes ingresar al menos una cantidad de 1.", 1);
                                            return;
                                        }
                                        if (Session.GetRoleplay().Cocaine < Cant)
                                        {
                                            Session.SendWhisper("No tienes esa cantidad de Cocaína para guardar.", 1);
                                            return;
                                        }

                                        int mytrunk = 0;
                                        string IntBaul = string.Join(";", VO[0].Baul);

                                        //To Check Cants
                                        int CurIntCant = 0;
                                        bool find = false;
                                        foreach (string content in VO[0].Baul)
                                        {
                                            string[] stringSeparatorsInt = new string[] { "-" };
                                            string[] resultInt;
                                            resultInt = content.Split(stringSeparatorsInt, StringSplitOptions.RemoveEmptyEntries);
                                            /*if (content.Contains("-"))
                                            {
                                                foreach (string cantcontnet in resultInt)
                                                {
                                                    if (resultInt[1].Contains("-g. Cocaína"))
                                                    {
                                                        if (int.TryParse(resultInt[0].ToString(), out int CurCant))
                                                        {
                                                            CurIntCant = CurCant;
                                                            int newCant = CurIntCant + Cant;
                                                            StringBuilder builder = new StringBuilder(IntBaul);
                                                            builder.Replace(CurIntCant + "-g. Cocaína;", newCant + "-g. Cocaína;");
                                                            IntBaul = builder.ToString();
                                                            find = true;
                                                        }
                                                    }
                                                }
                                            }*/
                                            if (content.Length > 0)
                                                mytrunk++;
                                        }
                                        if (mytrunk >= maxtrunk)
                                        {
                                            Session.SendWhisper("Al parecer ya no hay más espacio en el maletero.", 1);
                                            return;
                                        }
                                        if (!find)
                                            IntBaul += Cant + "-g. Cocaína;";

                                        VO[0].Baul = IntBaul.Split(';');
                                        RoleplayManager.UpdateVehicleBaul(itemfurni, "baul", IntBaul);
                                        Session.GetRoleplay().Cocaine -= Cant;
                                        RoleplayManager.Shout(Session, "*Guarda algo en el maletero del vehículo*", 5);

                                        #region WS Refresh
                                        if (Session.GetRoleplay().ViewBaul)
                                        {
                                            int Ind = 0;
                                            string Interior = "";
                                            foreach (string content in VO[0].Baul)
                                            {
                                                Ind++;
                                                if (content.Length > 0)
                                                    Interior += "[" + Ind + "] " + content + "<br>";
                                                else if (Ind <= maxtrunk)
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                            }
                                            if (Ind < maxtrunk)
                                            {
                                                int re = (maxtrunk - Ind);
                                                for (int j = 0; j < re; j++)
                                                {
                                                    Ind++;
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                                }
                                            }
                                            Session.GetRoleplay().CarWSBaul = Interior;
                                            Session.GetRoleplay().UpdateInteractingUserDialogues();
                                            Session.GetRoleplay().RefreshStatDialogue();
                                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "baul");
                                        }
                                        #endregion
                                        break;
                                        #endregion
                                    }
                                case "caramelos":
                                    {
                                        #region Execute
                                        int Cant = 0;
                                        if (!int.TryParse(Params[3], out Cant))
                                        {
                                            Session.SendWhisper("Cantidad inválida.", 1);
                                            return;
                                        }
                                        if (Cant <= 0)
                                        {
                                            Session.SendWhisper("Debes ingresar al menos una cantidad de 1.", 1);
                                            return;
                                        }
                                        if (Session.GetRoleplay().Caramelos < Cant)
                                        {
                                            Session.SendWhisper("No tienes esa cantidad de Caramelos para guardar.", 1);
                                            return;
                                        }

                                        int mytrunk = 0;
                                        string IntBaul = string.Join(";", VO[0].Baul);

                                        //To Check Cants
                                        int CurIntCant = 0;
                                        bool find = false;
                                        foreach (string content in VO[0].Baul)
                                        {
                                            string[] stringSeparatorsInt = new string[] { "-" };
                                            string[] resultInt;
                                            resultInt = content.Split(stringSeparatorsInt, StringSplitOptions.RemoveEmptyEntries);
                                            /*if (content.Contains("-"))
                                            {
                                                foreach (string cantcontnet in resultInt)
                                                {
                                                    if (resultInt[1].Contains("-g. Cocaína"))
                                                    {
                                                        if (int.TryParse(resultInt[0].ToString(), out int CurCant))
                                                        {
                                                            CurIntCant = CurCant;
                                                            int newCant = CurIntCant + Cant;
                                                            StringBuilder builder = new StringBuilder(IntBaul);
                                                            builder.Replace(CurIntCant + "-g. Cocaína;", newCant + "-g. Cocaína;");
                                                            IntBaul = builder.ToString();
                                                            find = true;
                                                        }
                                                    }
                                                }
                                            }*/
                                            if (content.Length > 0)
                                                mytrunk++;
                                        }
                                        if (mytrunk >= maxtrunk)
                                        {
                                            Session.SendWhisper("Al parecer ya no hay más espacio en el maletero.", 1);
                                            return;
                                        }
                                        if (!find)
                                            IntBaul += Cant + "-. Caramelos;";

                                        VO[0].Baul = IntBaul.Split(';');
                                        RoleplayManager.UpdateVehicleBaul(itemfurni, "baul", IntBaul);
                                        Session.GetRoleplay().Caramelos -= Cant;
                                        RoleplayManager.Shout(Session, "*Guarda algo en el maletero del vehículo*", 5);

                                        #region WS Refresh
                                        if (Session.GetRoleplay().ViewBaul)
                                        {
                                            int Ind = 0;
                                            string Interior = "";
                                            foreach (string content in VO[0].Baul)
                                            {
                                                Ind++;
                                                if (content.Length > 0)
                                                    Interior += "[" + Ind + "] " + content + "<br>";
                                                else if (Ind <= maxtrunk)
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                            }
                                            if (Ind < maxtrunk)
                                            {
                                                int re = (maxtrunk - Ind);
                                                for (int j = 0; j < re; j++)
                                                {
                                                    Ind++;
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                                }
                                            }
                                            Session.GetRoleplay().CarWSBaul = Interior;
                                            Session.GetRoleplay().UpdateInteractingUserDialogues();
                                            Session.GetRoleplay().RefreshStatDialogue();
                                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "baul");
                                        }
                                        #endregion
                                        break;
                                        #endregion
                                    }
                                case "marihuana":
                                    {
                                        #region Execute
                                        int Cant = 0;
                                        if (!int.TryParse(Params[3], out Cant))
                                        {
                                            Session.SendWhisper("Cantidad inválida.", 1);
                                            return;
                                        }
                                        if (Cant <= 0)
                                        {
                                            Session.SendWhisper("Debes ingresar al menos una cantidad de 1.", 1);
                                            return;
                                        }
                                        if (Session.GetRoleplay().Weed < Cant)
                                        {
                                            Session.SendWhisper("No tienes esa cantidad de Marihuana para guardar.", 1);
                                            return;
                                        }

                                        int mytrunk = 0;
                                        string IntBaul = string.Join(";", VO[0].Baul);

                                        //To Check Cants
                                        int CurIntCant = 0;
                                        bool find = false;
                                        foreach (string content in VO[0].Baul)
                                        {
                                            string[] stringSeparatorsInt = new string[] { "-" };
                                            string[] resultInt;
                                            resultInt = content.Split(stringSeparatorsInt, StringSplitOptions.RemoveEmptyEntries);
                                            /* if (content.Contains("-"))
                                             {
                                                 foreach (string cantcontnet in resultInt)
                                                 {
                                                     if (resultInt[1].Contains("-g. Marihuana"))
                                                     {
                                                         if (int.TryParse(resultInt[0].ToString(), out int CurCant))
                                                         {
                                                             CurIntCant = CurCant;
                                                             int newCant = CurIntCant + Cant;
                                                             StringBuilder builder = new StringBuilder(IntBaul);
                                                             builder.Replace(CurIntCant + "-g. Marihuana;", newCant + "-g. Marihuana;");
                                                             IntBaul = builder.ToString();
                                                             find = true;
                                                         }
                                                     }
                                                 }
                                             }*/
                                            if (content.Length > 0)
                                                mytrunk++;
                                        }
                                        if (mytrunk >= maxtrunk)
                                        {
                                            Session.SendWhisper("Al parecer ya no hay más espacio en el maletero.", 1);
                                            return;
                                        }
                                        if (!find)
                                            IntBaul += Cant + "-g. Marihuana;";

                                        VO[0].Baul = IntBaul.Split(';');
                                        RoleplayManager.UpdateVehicleBaul(itemfurni, "baul", IntBaul);
                                        Session.GetRoleplay().Weed -= Cant;
                                        RoleplayManager.Shout(Session, "*Guarda algo en el maletero del vehículo*", 5);

                                        #region WS Refresh
                                        if (Session.GetRoleplay().ViewBaul)
                                        {
                                            int Ind = 0;
                                            string Interior = "";
                                            foreach (string content in VO[0].Baul)
                                            {
                                                Ind++;
                                                if (content.Length > 0)
                                                    Interior += "[" + Ind + "] " + content + "<br>";
                                                else if (Ind <= maxtrunk)
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                            }
                                            if (Ind < maxtrunk)
                                            {
                                                int re = (maxtrunk - Ind);
                                                for (int j = 0; j < re; j++)
                                                {
                                                    Ind++;
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                                }
                                            }
                                            Session.GetRoleplay().CarWSBaul = Interior;
                                            Session.GetRoleplay().UpdateInteractingUserDialogues();
                                            Session.GetRoleplay().RefreshStatDialogue();
                                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "baul");
                                        }
                                        #endregion
                                        break;
                                        #endregion
                                    }
                                case "repuestos":
                                case "repuesto":
                                    {
                                        #region Execute
                                        int Cant = 0;
                                        if (!int.TryParse(Params[3], out Cant))
                                        {
                                            Session.SendWhisper("Cantidad inválida.", 1);
                                            return;
                                        }
                                        if (Cant <= 0)
                                        {
                                            Session.SendWhisper("Debes ingresar al menos una cantidad de 1.", 1);
                                            return;
                                        }
                                        if (Session.GetRoleplay().MecParts < Cant)
                                        {
                                            Session.SendWhisper("No tienes esa cantidad de Repuestos para guardar.", 1);
                                            return;
                                        }

                                        int mytrunk = 0;
                                        string IntBaul = string.Join(";", VO[0].Baul);

                                        //To Check Cants
                                        int CurIntCant = 0;
                                        bool find = false;
                                        foreach (string content in VO[0].Baul)
                                        {
                                            string[] stringSeparatorsInt = new string[] { "-" };
                                            string[] resultInt;
                                            resultInt = content.Split(stringSeparatorsInt, StringSplitOptions.RemoveEmptyEntries);
                                            /*if (content.Contains("-"))
                                            {
                                                foreach (string cantcontnet in resultInt)
                                                {
                                                    if (resultInt[1].Contains("Repuesto(s)"))
                                                    {
                                                        if (int.TryParse(resultInt[0].ToString(), out int CurCant))
                                                        {
                                                            CurIntCant = CurCant;
                                                            int newCant = CurIntCant + Cant;
                                                            StringBuilder builder = new StringBuilder(IntBaul);
                                                            builder.Replace(CurIntCant + "-Repuesto(s);", newCant + "-Repuesto(s);");
                                                            IntBaul = builder.ToString();
                                                            find = true;
                                                        }
                                                    }
                                                }
                                            }*/
                                            if (content.Length > 0)
                                                mytrunk++;
                                        }
                                        if (mytrunk >= maxtrunk)
                                        {
                                            Session.SendWhisper("Al parecer ya no hay más espacio en el maletero.", 1);
                                            return;
                                        }
                                        if (!find)
                                            IntBaul += Cant + "-Repuesto(s);";

                                        VO[0].Baul = IntBaul.Split(';');
                                        RoleplayManager.UpdateVehicleBaul(itemfurni, "baul", IntBaul);
                                        Session.GetRoleplay().MecParts -= Cant;
                                        RoleplayManager.Shout(Session, "*Guarda algo en el maletero del vehículo*", 5);

                                        #region WS Refresh
                                        if (Session.GetRoleplay().ViewBaul)
                                        {
                                            int Ind = 0;
                                            string Interior = "";
                                            foreach (string content in VO[0].Baul)
                                            {
                                                Ind++;
                                                if (content.Length > 0)
                                                    Interior += "[" + Ind + "] " + content + "<br>";
                                                else if (Ind <= maxtrunk)
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                            }
                                            if (Ind < maxtrunk)
                                            {
                                                int re = (maxtrunk - Ind);
                                                for (int j = 0; j < re; j++)
                                                {
                                                    Ind++;
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                                }
                                            }
                                            Session.GetRoleplay().CarWSBaul = Interior;
                                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "baul");
                                        }
                                        #endregion
                                        break;
                                        #endregion
                                    }
                                case "piezas":
                                case "pieza":
                                    {
                                        #region Execute
                                        int Cant = 0;
                                        if (!int.TryParse(Params[3], out Cant))
                                        {
                                            Session.SendWhisper("Cantidad inválida.", 1);
                                            return;
                                        }
                                        if (Cant <= 0)
                                        {
                                            Session.SendWhisper("Debes ingresar al menos una cantidad de 1.", 1);
                                            return;
                                        }
                                        if (Session.GetRoleplay().ArmPieces < Cant)
                                        {
                                            Session.SendWhisper("No tienes esa cantidad de Piezas para guardar.", 1);
                                            return;
                                        }

                                        int mytrunk = 0;
                                        string IntBaul = string.Join(";", VO[0].Baul);

                                        //To Check Cants
                                        int CurIntCant = 0;
                                        bool find = false;
                                        foreach (string content in VO[0].Baul)
                                        {
                                            string[] stringSeparatorsInt = new string[] { "-" };
                                            string[] resultInt;
                                            resultInt = content.Split(stringSeparatorsInt, StringSplitOptions.RemoveEmptyEntries);
                                            /* if (content.Contains("-"))
                                             {
                                                 foreach (string cantcontnet in resultInt)
                                                 {
                                                     if (resultInt[1].Contains("Piezaa(s)"))
                                                     {
                                                         if (int.TryParse(resultInt[0].ToString(), out int CurCant))
                                                         {
                                                             CurIntCant = CurCant;
                                                             int newCant = CurIntCant + Cant;
                          suma de productos MUY BUG          StringBuilder builder = new StringBuilder(IntBaul);
                                                             builder.Replace(CurIntCant + "-Piezaa(s);", newCant + "-Piezaa(s);");
                                                             IntBaul = builder.ToString();
                                                             find = true;
                                                         }
                                                     }
                                                 }
                                             }*/
                                            if (content.Length > 0)
                                                mytrunk++;
                                        }
                                        if (mytrunk >= maxtrunk)
                                        {
                                            Session.SendWhisper("Al parecer ya no hay más espacio en el maletero.", 1);
                                            return;
                                        }
                                        if (!find)
                                            IntBaul += Cant + "-Pieza(s);";

                                        VO[0].Baul = IntBaul.Split(';');
                                        RoleplayManager.UpdateVehicleBaul(itemfurni, "baul", IntBaul);
                                        Session.GetRoleplay().ArmPieces -= Cant;
                                        RoleplayManager.Shout(Session, "*Guarda algo en el maletero del vehículo*", 5);

                                        #region WS Refresh
                                        if (Session.GetRoleplay().ViewBaul)
                                        {
                                            int Ind = 0;
                                            string Interior = "";
                                            foreach (string content in VO[0].Baul)
                                            {
                                                Ind++;
                                                if (content.Length > 0)
                                                    Interior += "[" + Ind + "] " + content + "<br>";
                                                else if (Ind <= maxtrunk)
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                            }
                                            if (Ind < maxtrunk)
                                            {
                                                int re = (maxtrunk - Ind);
                                                for (int j = 0; j < re; j++)
                                                {
                                                    Ind++;
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                                }
                                            }
                                            Session.GetRoleplay().CarWSBaul = Interior;
                                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "baul");
                                        }
                                        #endregion
                                        break;
                                        #endregion
                                    }
                                default:
                                    {
                                        Session.SendWhisper("Objeto inválido: Solo es posible guardar por cantidad medicamentos/crack/marihuana/repuestos/piezas.", 1);
                                    }
                                    break;
                            }

                            break;
                            #endregion
                        }
                    case "sacar": // :baul [sacar] [id] [cantidad]
                        {
                            #region Sacar        
                            #region Conditions
                            if (!VO[0].BaulOpen)
                            {
                                Session.SendWhisper("El maletero no encuentra abierto para poder sacar objetos.", 1);
                                return;
                            }
                            int BId = 0;
                            if (!int.TryParse(Params[2], out BId))
                            {
                                Session.SendWhisper("ID Inválida", 1);
                                return;
                            }
                            if (BId > maxtrunk || BId <= 0)
                            {
                                Session.SendWhisper("El maletero no tiene ese Slot ID disponible.", 1);
                                return;
                            }
                            int Cant = 0;
                            if (!int.TryParse(Params[3], out Cant))
                            {
                                Session.SendWhisper("Cantidad inválida.", 1);
                                return;
                            }
                            if (Cant <= 0)
                            {
                                Session.SendWhisper("La cantidad debe ser al menos de 1.", 1);
                                return;
                            }
                            #endregion

                            int mytrunk = 0;
                            string IntBaul = string.Join(";", VO[0].Baul);

                            foreach (string content in VO[0].Baul)
                            {
                                if (content.Length > 0)
                                    mytrunk++;
                            }
                            if (BId > mytrunk)
                            {
                                Session.SendWhisper("Al parecer no hay ningún objeto en ese Slot ID.", 1);
                                return;
                            }
                            int nIndx = BId - 1;
                            if (!VO[0].Baul[nIndx].Contains("-"))
                            {
                                Session.SendWhisper("Al parecer no es posible sacar ese objeto por Cantidad. Usa directamente ':baul sacar [ID]'.", 1);
                                return;
                            }

                            string[] stringSeparators2 = new string[] { "-" };
                            string[] Inside;

                            Inside = VO[0].Baul[nIndx].Split(stringSeparators2, StringSplitOptions.RemoveEmptyEntries);

                            int CurCant = 0;
                            if (!int.TryParse(Inside[0], out CurCant))
                            {
                                Session.SendWhisper("Hubo un problema al obtener la cantidad actual del Objeto en ese Slot. Contacte con un Administrador [1].", 1);
                                return;
                            }


                            switch (Inside[1])
                            {
                                case "cc. Heroina":
                                    {
                                        #region Execute
                                        int newCant = CurCant - Cant;
                                        if (newCant < 0)
                                        {
                                            Session.SendWhisper("No hay " + Cant + " g. de Heroina en el Maletero.", 1);
                                            return;
                                        }
                                        StringBuilder builder = new StringBuilder(IntBaul);
                                        if (newCant > 0)
                                            builder.Replace(CurCant + "-cc. Heroina;", newCant + "-cc. Heroina;");
                                        else
                                            builder.Replace(CurCant + "-cc. Heroina;", "");
                                        IntBaul = builder.ToString();
                                        Session.GetRoleplay().Heroina += Cant;
                                        VO[0].Baul = IntBaul.Split(';');
                                        RoleplayManager.UpdateVehicleBaul(itemfurni, "baul", IntBaul);
                                        RoleplayManager.Shout(Session, "*Saca algo del Maletero del Vehículo*", 5);

                                        #region WS Refresh
                                        if (Session.GetRoleplay().ViewBaul)
                                        {
                                            int Ind = 0;
                                            string Interior = "";
                                            foreach (string content in VO[0].Baul)
                                            {
                                                Ind++;
                                                if (content.Length > 0)
                                                    Interior += "[" + Ind + "] " + content + "<br>";
                                                else if (Ind <= maxtrunk)
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                            }
                                            if (Ind < maxtrunk)
                                            {
                                                int re = (maxtrunk - Ind);
                                                for (int j = 0; j < re; j++)
                                                {
                                                    Ind++;
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                                }
                                            }
                                            Session.GetRoleplay().CarWSBaul = Interior;
                                            Session.GetRoleplay().UpdateInteractingUserDialogues();
                                            Session.GetRoleplay().RefreshStatDialogue();
                                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "baul");
                                        }
                                        #endregion
                                        break;
                                        #endregion
                                    }
                                case "g. Cocaína":
                                    {
                                        #region Execute
                                        int newCant = CurCant - Cant;
                                        if (newCant < 0)
                                        {
                                            Session.SendWhisper("No hay " + Cant + " g. de Cocaína en el Maletero.", 1);
                                            return;
                                        }
                                        StringBuilder builder = new StringBuilder(IntBaul);
                                        if (newCant > 0)
                                            builder.Replace(CurCant + "-g. Cocaína;", newCant + "-g. Cocaína;");
                                        else
                                            builder.Replace(CurCant + "-g. Cocaína;", "");
                                        IntBaul = builder.ToString();
                                        Session.GetRoleplay().Cocaine += Cant;
                                        VO[0].Baul = IntBaul.Split(';');
                                        RoleplayManager.UpdateVehicleBaul(itemfurni, "baul", IntBaul);
                                        RoleplayManager.Shout(Session, "*Saca algo del Maletero del Vehículo*", 5);

                                        #region WS Refresh
                                        if (Session.GetRoleplay().ViewBaul)
                                        {
                                            int Ind = 0;
                                            string Interior = "";
                                            foreach (string content in VO[0].Baul)
                                            {
                                                Ind++;
                                                if (content.Length > 0)
                                                    Interior += "[" + Ind + "] " + content + "<br>";
                                                else if (Ind <= maxtrunk)
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                            }
                                            if (Ind < maxtrunk)
                                            {
                                                int re = (maxtrunk - Ind);
                                                for (int j = 0; j < re; j++)
                                                {
                                                    Ind++;
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                                }
                                            }
                                            Session.GetRoleplay().CarWSBaul = Interior;
                                            Session.GetRoleplay().UpdateInteractingUserDialogues();
                                            Session.GetRoleplay().RefreshStatDialogue();
                                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "baul");
                                        }
                                        #endregion
                                        break;
                                        #endregion
                                    }
                                case ". Caramelos":
                                    {
                                        #region Execute
                                        int newCant = CurCant - Cant;
                                        if (newCant < 0)
                                        {
                                            Session.SendWhisper("No hay " + Cant + " g. de Caramelos en el Maletero.", 1);
                                            return;
                                        }
                                        StringBuilder builder = new StringBuilder(IntBaul);
                                        if (newCant > 0)
                                            builder.Replace(CurCant + "-. Caramelos;", newCant + "-. Caramelos;");
                                        else
                                            builder.Replace(CurCant + "-. Caramelos;", "");
                                        IntBaul = builder.ToString();
                                        Session.GetRoleplay().Caramelos += Cant;
                                        VO[0].Baul = IntBaul.Split(';');
                                        RoleplayManager.UpdateVehicleBaul(itemfurni, "baul", IntBaul);
                                        RoleplayManager.Shout(Session, "*Saca algo del Maletero del Vehículo*", 5);

                                        #region WS Refresh
                                        if (Session.GetRoleplay().ViewBaul)
                                        {
                                            int Ind = 0;
                                            string Interior = "";
                                            foreach (string content in VO[0].Baul)
                                            {
                                                Ind++;
                                                if (content.Length > 0)
                                                    Interior += "[" + Ind + "] " + content + "<br>";
                                                else if (Ind <= maxtrunk)
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                            }
                                            if (Ind < maxtrunk)
                                            {
                                                int re = (maxtrunk - Ind);
                                                for (int j = 0; j < re; j++)
                                                {
                                                    Ind++;
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                                }
                                            }
                                            Session.GetRoleplay().CarWSBaul = Interior;
                                            Session.GetRoleplay().UpdateInteractingUserDialogues();
                                            Session.GetRoleplay().RefreshStatDialogue();
                                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "baul");
                                        }
                                        #endregion
                                        break;
                                        #endregion
                                    }
                                case "g. Marihuana":
                                    {
                                        #region Execute
                                        int newCant = CurCant - Cant;
                                        if (newCant < 0)
                                        {
                                            Session.SendWhisper("No hay " + Cant + " g. de Marihuana en el Maletero.", 1);
                                            return;
                                        }
                                        StringBuilder builder = new StringBuilder(IntBaul);
                                        if (newCant > 0)
                                            builder.Replace(CurCant + "-g. Marihuana;", newCant + "-g. Marihuana;");
                                        else
                                            builder.Replace(CurCant + "-g. Marihuana;", "");
                                        IntBaul = builder.ToString();
                                        Session.GetRoleplay().Weed += Cant;
                                        VO[0].Baul = IntBaul.Split(';');
                                        RoleplayManager.UpdateVehicleBaul(itemfurni, "baul", IntBaul);
                                        RoleplayManager.Shout(Session, "*Saca algo del Maletero del Vehículo*", 5);

                                        #region WS Refresh
                                        if (Session.GetRoleplay().ViewBaul)
                                        {
                                            int Ind = 0;
                                            string Interior = "";
                                            foreach (string content in VO[0].Baul)
                                            {
                                                Ind++;
                                                if (content.Length > 0)
                                                    Interior += "[" + Ind + "] " + content + "<br>";
                                                else if (Ind <= maxtrunk)
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                            }
                                            if (Ind < maxtrunk)
                                            {
                                                int re = (maxtrunk - Ind);
                                                for (int j = 0; j < re; j++)
                                                {
                                                    Ind++;
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                                }
                                            }
                                            Session.GetRoleplay().CarWSBaul = Interior;
                                            Session.GetRoleplay().UpdateInteractingUserDialogues();
                                            Session.GetRoleplay().RefreshStatDialogue();
                                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "baul");
                                        }
                                        #endregion
                                        break;
                                        #endregion
                                    }
                                case "Repuesto(s)":
                                    {
                                        #region Execute
                                        int newCant = CurCant - Cant;
                                        if (newCant < 0)
                                        {
                                            Session.SendWhisper("No hay " + Cant + " de repuestos en el Maletero.", 1);
                                            return;
                                        }
                                        StringBuilder builder = new StringBuilder(IntBaul);
                                        if (newCant > 0)
                                            builder.Replace(CurCant + "-Repuesto(s);", newCant + "-Repuesto(s);");
                                        else
                                            builder.Replace(CurCant + "-Repuesto(s);", "");
                                        IntBaul = builder.ToString();
                                        Session.GetRoleplay().MecParts += Cant;
                                        VO[0].Baul = IntBaul.Split(';');
                                        RoleplayManager.UpdateVehicleBaul(itemfurni, "baul", IntBaul);
                                        RoleplayManager.Shout(Session, "*Saca algo del Maletero del Vehículo*", 5);

                                        #region WS Refresh
                                        if (Session.GetRoleplay().ViewBaul)
                                        {
                                            int Ind = 0;
                                            string Interior = "";
                                            foreach (string content in VO[0].Baul)
                                            {
                                                Ind++;
                                                if (content.Length > 0)
                                                    Interior += "[" + Ind + "] " + content + "<br>";
                                                else if (Ind <= maxtrunk)
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                            }
                                            if (Ind < maxtrunk)
                                            {
                                                int re = (maxtrunk - Ind);
                                                for (int j = 0; j < re; j++)
                                                {
                                                    Ind++;
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                                }
                                            }
                                            Session.GetRoleplay().CarWSBaul = Interior;
                                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "baul");
                                        }
                                        #endregion
                                        break;
                                        #endregion
                                    }
                                case "Pieza(s)":
                                    {
                                        #region Execute
                                        int newCant = CurCant - Cant;
                                        if (newCant < 0)
                                        {
                                            Session.SendWhisper("No hay " + Cant + " de piezas en el Maletero.", 1);
                                            return;
                                        }
                                        StringBuilder builder = new StringBuilder(IntBaul);
                                        if (newCant > 0)
                                            builder.Replace(CurCant + "-Pieza(s);", newCant + "-Pieza(s);");
                                        else
                                            builder.Replace(CurCant + "-Pieza(s);", "");

                                        IntBaul = builder.ToString();
                                        Session.GetRoleplay().ArmPieces += Cant;

                                        VO[0].Baul = IntBaul.Split(';');
                                        RoleplayManager.UpdateVehicleBaul(itemfurni, "baul", IntBaul);
                                        RoleplayManager.Shout(Session, "*Saca algo del Maletero del Vehículo*", 5);

                                        #region WS Refresh
                                        if (Session.GetRoleplay().ViewBaul)
                                        {
                                            int Ind = 0;
                                            string Interior = "";
                                            foreach (string content in VO[0].Baul)
                                            {
                                                Ind++;
                                                if (content.Length > 0)
                                                    Interior += "[" + Ind + "] " + content + "<br>";
                                                else if (Ind <= maxtrunk)
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                            }
                                            if (Ind < maxtrunk)
                                            {
                                                int re = (maxtrunk - Ind);
                                                for (int j = 0; j < re; j++)
                                                {
                                                    Ind++;
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                                }
                                            }
                                            Session.GetRoleplay().CarWSBaul = Interior;
                                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "baul");
                                        }
                                        #endregion
                                        break;
                                        #endregion
                                    }
                                default:
                                    {
                                        Session.SendWhisper("Al parecer no es posible sacar ese objeto por Cantidad. Usa directamente ':baul sacar [ID]'.", 1);
                                    }
                                    break;
                            }

                            break;
                            #endregion
                        }
                    default:
                        {
                            Session.SendWhisper("Comando inválido. Quizás quisiste usar ':baul guardar [consumible] [cantidad]' o ':baul sacar [id] [cantidad]'.", 1);
                        }
                        break;
                }
            }
            else if (Dir == 1) // Guardar/Sacar [Objeto/Id]
            {
                switch (toDo)
                {
                    case "guardar":
                        {
                            #region Execute
                            string wname = "";
                            string Type = Params[2].ToLower();
                            wname = Type;
                            #region Weapon Check
                            Weapon weapon = null;
                            foreach (Weapon Weapon in WeaponManager.Weapons.Values)
                            {
                                if (Type.ToLower() == Weapon.Name.ToLower())
                                {
                                    Type = "weapon";
                                    weapon = Weapon;
                                }
                            }
                            #endregion

                            switch (Type)
                            {
                                case "bidon":
                                    {
                                        #region Execute
                                        if (!VO[0].BaulOpen)
                                        {
                                            Session.SendWhisper("El maletero no encuentra abierto para poder guardar objetos.", 1);
                                            return;
                                        }
                                        if (Session.GetRoleplay().Bidon <= 0)
                                        {
                                            Session.SendWhisper("No tienes un Bidón en tu Inventario para guardar.", 1);
                                            return;
                                        }

                                        int mytrunk = 0;
                                        string IntBaul = string.Join(";", VO[0].Baul);

                                        foreach (string content in VO[0].Baul)
                                        {
                                            if (content.Length > 0)
                                                mytrunk++;
                                        }
                                        if (mytrunk >= maxtrunk)
                                        {
                                            Session.SendWhisper("Al parecer ya no hay más espacio en el maletero.", 1);
                                            return;
                                        }
                                        IntBaul += "Bidon;";
                                        VO[0].Baul = IntBaul.Split(';');
                                        RoleplayManager.UpdateVehicleBaul(itemfurni, "baul", IntBaul);
                                        Session.GetRoleplay().Bidon -= 1;
                                        RoleplayManager.Shout(Session, "*Guarda algo en el maletero del vehículo*", 5);

                                        #region WS Refresh
                                        if (Session.GetRoleplay().ViewBaul)
                                        {
                                            int Ind = 0;
                                            string Interior = "";
                                            foreach (string content in VO[0].Baul)
                                            {
                                                Ind++;
                                                if (content.Length > 0)
                                                    Interior += "[" + Ind + "] " + content + "<br>";
                                                else if (Ind <= maxtrunk)
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                            }
                                            if (Ind < maxtrunk)
                                            {
                                                int re = (maxtrunk - Ind);
                                                for (int j = 0; j < re; j++)
                                                {
                                                    Ind++;
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                                }
                                            }
                                            Session.GetRoleplay().CarWSBaul = Interior;
                                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "baul");
                                        }
                                        #endregion
                                        break;
                                        #endregion
                                    }

                                case "objeto":
                                    {
                                        #region Execute
                                        if (!VO[0].BaulOpen)
                                        {
                                            Session.SendWhisper("El maletero no encuentra abierto para poder guardar objetos.", 1);
                                            return;
                                        }
                                        if (Session.GetRoleplay().Object.Length <= 0)
                                        {
                                            Session.SendWhisper("No tienes ningún objeto robado en tu Inventario para guardar.", 1);
                                            return;
                                        }

                                        int mytrunk = 0;
                                        string IntBaul = string.Join(";", VO[0].Baul);

                                        foreach (string content in VO[0].Baul)
                                        {
                                            if (content.Length > 0)
                                                mytrunk++;
                                        }
                                        if (mytrunk >= maxtrunk)
                                        {
                                            Session.SendWhisper("Al parecer ya no hay más espacio en el maletero.", 1);
                                            return;
                                        }
                                        IntBaul += Session.GetRoleplay().Object + ";";
                                        VO[0].Baul = IntBaul.Split(';');
                                        RoleplayManager.UpdateVehicleBaul(itemfurni, "baul", IntBaul);
                                        Session.GetRoleplay().Object = "";
                                        Session.GetRoleplay().ObjectPrice = 0;
                                        RoleplayManager.Shout(Session, "*Guarda algo en el maletero del vehículo*", 5);

                                        #region WS Refresh
                                        if (Session.GetRoleplay().ViewBaul)
                                        {
                                            int Ind = 0;
                                            string Interior = "";
                                            foreach (string content in VO[0].Baul)
                                            {
                                                Ind++;
                                                if (content.Length > 0)
                                                    Interior += "[" + Ind + "] " + content + "<br>";
                                                else if (Ind <= maxtrunk)
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                            }
                                            if (Ind < maxtrunk)
                                            {
                                                int re = (maxtrunk - Ind);
                                                for (int j = 0; j < re; j++)
                                                {
                                                    Ind++;
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                                }
                                            }
                                            Session.GetRoleplay().CarWSBaul = Interior;
                                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "baul");
                                        }
                                        #endregion
                                        break;
                                        #endregion
                                    }

                                case "weapon":
                                case "arma":
                                    {
                                        #region Execute
                                        if (!VO[0].BaulOpen)
                                        {
                                            Session.SendWhisper("El maletero no encuentra abierto para poder guardar objetos.", 1);
                                            return;
                                        }
                                        if (weapon == null)
                                        {
                                            Session.SendWhisper("'" + wname + "' no es un arma válida.");
                                            return;
                                        }
                                        if (!Session.GetRoleplay().OwnedWeapons.ContainsKey(weapon.Name))
                                        {
                                            Session.SendWhisper("No tienes un/a " + weapon.PublicName, 1);
                                            return;
                                        }

                                        int mytrunk = 0;
                                        string IntBaul = string.Join(";", VO[0].Baul);

                                        foreach (string content in VO[0].Baul)
                                        {
                                            if (content.Length > 0)
                                                mytrunk++;
                                        }
                                        if (mytrunk >= maxtrunk)
                                        {
                                            Session.SendWhisper("Al parecer ya no hay más espacio en el maletero.", 1);
                                            return;
                                        }


                                        // Si la trae Equipada => Desequiparlo

                                        if (Session.GetRoleplay().EquippedWeapon != null)
                                        {
                                            if (Session.GetRoleplay().EquippedWeapon.Name == wname)
                                            {
                                                #region Desequipar
                                                RoleplayManager.UpdateMyWeaponStats(Session, "bullets", Session.GetRoleplay().Bullets, Session.GetRoleplay().EquippedWeapon.Name);
                                                RoleplayManager.UpdateMyWeaponStats(Session, "life", Session.GetRoleplay().WLife, Session.GetRoleplay().EquippedWeapon.Name);

                                                string UnEquipMessage = Session.GetRoleplay().EquippedWeapon.UnEquipText;
                                                UnEquipMessage = UnEquipMessage.Replace("[NAME]", Session.GetRoleplay().EquippedWeapon.PublicName);

                                                RoleplayManager.Shout(Session, UnEquipMessage, 5);

                                                if (Session.GetRoomUser().CurrentEffect == Session.GetRoleplay().EquippedWeapon.EffectID)
                                                    Session.GetRoomUser().ApplyEffect(0);

                                                if (Session.GetRoomUser().CarryItemID == Session.GetRoleplay().EquippedWeapon.HandItem)
                                                    Session.GetRoomUser().CarryItem(0);

                                                Session.GetRoleplay().EquippedWeapon = null;

                                                Session.GetRoleplay().Bullets = 0;
                                                #endregion
                                            }
                                        }
                                        RoleplayManager.UpdateToBaulWeaponBaul(Session, itemfurni, weapon.Name);
                                        //RoleplayManager.DropMyWeapon(Session, weapon.Name);
                                        Session.GetRoleplay().OwnedWeapons = null;
                                        Session.GetRoleplay().OwnedWeapons = Session.GetRoleplay().LoadAndReturnWeapons();

                                        IntBaul += weapon.PublicName + ";";
                                        VO[0].Baul = IntBaul.Split(';');
                                        RoleplayManager.UpdateVehicleBaul(itemfurni, "baul", IntBaul);
                                        RoleplayManager.Shout(Session, "*Guarda algo en el maletero del vehículo*", 5);

                                        #region WS Refresh
                                        if (Session.GetRoleplay().ViewBaul)
                                        {
                                            int Ind = 0;
                                            string Interior = "";
                                            foreach (string content in VO[0].Baul)
                                            {
                                                Ind++;
                                                if (content.Length > 0)
                                                    Interior += "[" + Ind + "] " + content + "<br>";
                                                else if (Ind <= maxtrunk)
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                            }
                                            if (Ind < maxtrunk)
                                            {
                                                int re = (maxtrunk - Ind);
                                                for (int j = 0; j < re; j++)
                                                {
                                                    Ind++;
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                                }
                                            }
                                            Session.GetRoleplay().CarWSBaul = Interior;
                                            Session.GetRoleplay().UpdateInteractingUserDialogues();
                                            Session.GetRoleplay().RefreshStatDialogue();
                                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_charweapons", "");
                                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "baul");
                                        }
                                        #endregion
                                        break;
                                        #endregion
                                    }
                                default:
                                    {
                                        Session.SendWhisper("Objeto inválido: Solo es posible guardar bidon/objeto/cocaina/heroina/marihuana/nombre-de-arma.", 1);
                                    }
                                    break;
                            }
                            break;
                            #endregion
                        }
                    case "sacar":
                        {
                            #region Execute

                            #region Conditions
                            if (!VO[0].BaulOpen)
                            {
                                Session.SendWhisper("El maletero no encuentra abierto para poder sacar objetos.", 1);
                                return;
                            }
                            int BId = 0;
                            if (!int.TryParse(Params[2], out BId))
                            {
                                Session.SendWhisper("ID Inválida", 1);
                                return;
                            }
                            if (BId > maxtrunk || BId <= 0)
                            {
                                Session.SendWhisper("El maletero no tiene ese Slot ID disponible.", 1);
                                return;
                            }
                            #endregion

                            string wname = "";
                            int nIndx = BId - 1;


                            int mytrunk = 0;
                            string IntBaul = string.Join(";", VO[0].Baul);

                            foreach (string content in VO[0].Baul)
                            {
                                if (content.Length > 0)
                                    mytrunk++;
                            }
                            if (BId > mytrunk)
                            {
                                Session.SendWhisper("Al parecer no hay ningún objeto en ese Slot ID.", 1);
                                return;
                            }

                            string Type = VO[0].Baul[nIndx].ToLower();
                            wname = Type;

                            #region Weapon Check
                            Weapon weapon = null;
                            foreach (Weapon Weapon in WeaponManager.Weapons.Values)
                            {
                                if (Type.ToLower() == Weapon.PublicName.ToLower())
                                {
                                    Type = "weapon";
                                    weapon = Weapon;
                                }
                            }
                            #endregion

                            #region Rob Object Check
                            string RobObj = Type;
                            if (RoleplayManager.IsRobObject(Type))
                                Type = "objeto";
                            #endregion

                            if (Type.Contains("-"))
                                Type = "consumible";

                            switch (Type)
                            {
                                case "bidon":
                                    {
                                        #region Execute
                                        //int i = IntBaul.IndexOf("Bidon;");
                                        //IntBaul = IntBaul.Remove(i, "Bidon;".Length);

                                        List<string> listStr = VO[0].Baul.ToList();
                                        listStr.RemoveAt(nIndx);
                                        VO[0].Baul = listStr.ToArray();
                                        IntBaul = string.Join(";", VO[0].Baul);

                                        Session.GetRoleplay().Bidon += 1;
                                        RoleplayManager.UpdateVehicleBaul(itemfurni, "baul", IntBaul);
                                        RoleplayManager.Shout(Session, "*Saca algo del Maletero del Vehículo*", 5);

                                        #region WS Refresh
                                        if (Session.GetRoleplay().ViewBaul)
                                        {
                                            int Ind = 0;
                                            string Interior = "";
                                            foreach (string content in VO[0].Baul)
                                            {
                                                Ind++;
                                                if (content.Length > 0)
                                                    Interior += "[" + Ind + "] " + content + "<br>";
                                                else if (Ind <= maxtrunk)
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                            }
                                            if (Ind < maxtrunk)
                                            {
                                                int re = (maxtrunk - Ind);
                                                for (int j = 0; j < re; j++)
                                                {
                                                    Ind++;
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                                }
                                            }
                                            Session.GetRoleplay().CarWSBaul = Interior;
                                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "baul");
                                        }
                                        #endregion
                                        break;
                                        #endregion
                                    }

                                case "objeto":
                                    {
                                        #region Execute
                                        if (Session.GetRoleplay().Object.Length > 0)
                                        {
                                            Session.SendWhisper("Ya tienes un objeto robado en tu inventario. No puedes llevar más de uno a la vez.", 1);
                                            return;
                                        }
                                        //int i = IntBaul.IndexOf("Bidon;");
                                        //IntBaul = IntBaul.Remove(i, "Bidon;".Length);

                                        List<string> listStr = VO[0].Baul.ToList();
                                        listStr.RemoveAt(nIndx);
                                        VO[0].Baul = listStr.ToArray();
                                        IntBaul = string.Join(";", VO[0].Baul);

                                        Session.GetRoleplay().Object = RobObj;
                                        RoleplayManager.SetPriceObject(Session, RobObj);

                                        RoleplayManager.UpdateVehicleBaul(itemfurni, "baul", IntBaul);
                                        RoleplayManager.Shout(Session, "*Saca algo del Maletero del Vehículo*", 5);

                                        #region WS Refresh
                                        if (Session.GetRoleplay().ViewBaul)
                                        {
                                            int Ind = 0;
                                            string Interior = "";
                                            foreach (string content in VO[0].Baul)
                                            {
                                                Ind++;
                                                if (content.Length > 0)
                                                    Interior += "[" + Ind + "] " + content + "<br>";
                                                else if (Ind <= maxtrunk)
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                            }
                                            if (Ind < maxtrunk)
                                            {
                                                int re = (maxtrunk - Ind);
                                                for (int j = 0; j < re; j++)
                                                {
                                                    Ind++;
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                                }
                                            }
                                            Session.GetRoleplay().CarWSBaul = Interior;
                                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "baul");
                                        }
                                        #endregion
                                        break;
                                        #endregion
                                    }

                                #region Palanca & Balde OFF
                                /*
                            case "palanca":
                                {
                                    #region Execute
                                    if (Session.GetRoleplay().Palanca)
                                    {
                                        Session.SendWhisper("Ya tienes una Palanca en tu Inventario. No puedes tener más de una a la vez.", 1);
                                        return;
                                    }
                                    StringBuilder builder = new StringBuilder(IntBaul);
                                    builder.Replace("Palanca;", "");
                                    IntBaul = builder.ToString();
                                    Session.GetRoleplay().Palanca = true;
                                    RoleplayManager.UpdateVehicleBaul(itemfurni, "baul", IntBaul);
                                    RoleplayManager.Shout(Session, "*Saca algo del Maletero del Vehículo*", 5);

                                    #region Refresh WS
                                    if (Session.GetRoleplay().ViewBaul)
                                    {
                                        #region Select Vehicle Stats
                                        using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                                        {
                                            dbClient.SetQuery("SELECT * FROM play_vehicles_owned WHERE furni_id = '" + itemfurni + "' LIMIT 1");
                                            DataRow check = dbClient.getRow();
                                            if (check != null)
                                            {
                                                owner = Convert.ToInt32(check["owner"]);
                                                baul = Convert.ToString(check["baul"]);
                                                baul_state = Convert.ToInt32(check["baul_state"]);

                                            }
                                            else
                                            {
                                                if (corp <= 0)
                                                {
                                                    RoleplayManager.Shout(Session, "* Una grúa ha pasado a recoger el vehículo que " + Session.GetHabbo().Username + " intentaba interactuar.");
                                                    RoleplayManager.PickItem(Session, itemfurni);
                                                    return;
                                                }
                                            }
                                        }
                                        #endregion

                                        #region Get Baul
                                        int Ind = 0;
                                        string IntBaulNew = baul;
                                        string[] stringSeparatorsNew = new string[] { ";" };
                                        string[] resultNew;
                                        resultNew = IntBaulNew.Split(stringSeparatorsNew, StringSplitOptions.RemoveEmptyEntries);
                                        string Interior = "";
                                        foreach (string content in resultNew)
                                        {
                                            Interior += "[" + Ind + "] " + content + "<br>";
                                            Ind++;
                                        }
                                        if (Interior.Length <= 0)
                                        {
                                            for (int i = 1; i <= maxtrunk; i++)
                                            {
                                                Interior += "[" + i + "] Vacío <br>";
                                            }
                                        }
                                        Ind -= 1;
                                        if (Ind < maxtrunk)
                                        {
                                            int re = (maxtrunk - Ind);
                                            for (int j = 0; j < re; j++)
                                            {
                                                Ind++;
                                                Interior += "[" + Ind + "] Vacío <br>";
                                            }
                                        }
                                        Session.GetRoleplay().CarWSBaul = Interior;
                                        PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "baul");
                                        #endregion
                                    }
                                    #endregion
                                    break;
                                    #endregion
                                }
                            case "balde":
                                {
                                    #region Execute
                                    if (Session.GetRoleplay().Balde)
                                    {
                                        Session.SendWhisper("Ya tienes un Balde en tu Inventario. No puedes tener más de una a la vez.", 1);
                                        return;
                                    }
                                    StringBuilder builder = new StringBuilder(IntBaul);
                                    builder.Replace("Balde;", "");
                                    IntBaul = builder.ToString();
                                    Session.GetRoleplay().Balde = true;
                                    RoleplayManager.UpdateVehicleBaul(itemfurni, "baul", IntBaul);
                                    RoleplayManager.Shout(Session, "*Saca algo del Maletero del Vehículo*", 5);

                                    #region Refresh WS
                                    if (Session.GetRoleplay().ViewBaul)
                                    {
                                        #region Select Vehicle Stats
                                        using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                                        {
                                            dbClient.SetQuery("SELECT * FROM play_vehicles_owned WHERE furni_id = '" + itemfurni + "' LIMIT 1");
                                            DataRow check = dbClient.getRow();
                                            if (check != null)
                                            {
                                                owner = Convert.ToInt32(check["owner"]);
                                                baul = Convert.ToString(check["baul"]);
                                                baul_state = Convert.ToInt32(check["baul_state"]);

                                            }
                                            else
                                            {
                                                if (corp <= 0)
                                                {
                                                    RoleplayManager.Shout(Session, "* Una grúa ha pasado a recoger el vehículo que " + Session.GetHabbo().Username + " intentaba interactuar.");
                                                    RoleplayManager.PickItem(Session, itemfurni);
                                                    return;
                                                }
                                            }
                                        }
                                        #endregion

                                        #region Get Baul
                                        int Ind = 0;
                                        string IntBaulNew = baul;
                                        string[] stringSeparatorsNew = new string[] { ";" };
                                        string[] resultNew;
                                        resultNew = IntBaulNew.Split(stringSeparatorsNew, StringSplitOptions.RemoveEmptyEntries);
                                        string Interior = "";
                                        foreach (string content in resultNew)
                                        {
                                            Interior += "[" + Ind + "] " + content + "<br>";
                                            Ind++;
                                        }
                                        if (Interior.Length <= 0)
                                        {
                                            for (int i = 1; i <= maxtrunk; i++)
                                            {
                                                Interior += "[" + i + "] Vacío <br>";
                                            }
                                        }
                                        Ind -= 1;
                                        if (Ind < maxtrunk)
                                        {
                                            int re = (maxtrunk - Ind);
                                            for (int j = 0; j < re; j++)
                                            {
                                                Ind++;
                                                Interior += "[" + Ind + "] Vacío <br>";
                                            }
                                        }
                                        Session.GetRoleplay().CarWSBaul = Interior;
                                        PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "baul");
                                        #endregion
                                    }
                                    #endregion
                                    break;
                                    #endregion
                                }
                                */
                                #endregion

                                case "weapon":
                                case "arma":
                                    {
                                        #region Execute
                                        if (weapon == null)
                                        {
                                            Session.SendWhisper("'" + wname + "' no es un arma válida.");
                                            return;
                                        }
                                        if (Session.GetRoleplay().OwnedWeapons.ContainsKey(weapon.Name))
                                        {
                                            Session.SendWhisper("Ya tienes un/a " + weapon.PublicName + " en tu Invetario. No puedes tener más de una a la vez.", 1);
                                            return;
                                        }

                                        var regex = new Regex(Regex.Escape(weapon.PublicName + ";"));
                                        var newText = regex.Replace(IntBaul, "", 1);
                                        IntBaul = newText.ToString();

                                        RoleplayManager.UpdateToWeaponBaul(Session, VO[0].OwnerId, itemfurni, weapon.Name);
                                        Session.GetRoleplay().OwnedWeapons = null;
                                        Session.GetRoleplay().OwnedWeapons = Session.GetRoleplay().LoadAndReturnWeapons();
                                        VO[0].Baul = IntBaul.Split(';');
                                        RoleplayManager.UpdateVehicleBaul(itemfurni, "baul", IntBaul);
                                        RoleplayManager.Shout(Session, "*Saca algo del Maletero del Vehículo*", 5);

                                        #region WS Refresh
                                        if (Session.GetRoleplay().ViewBaul)
                                        {
                                            int Ind = 0;
                                            string Interior = "";
                                            foreach (string content in VO[0].Baul)
                                            {
                                                Ind++;
                                                if (content.Length > 0)
                                                    Interior += "[" + Ind + "] " + content + "<br>";
                                                else if (Ind <= maxtrunk)
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                            }
                                            if (Ind < maxtrunk)
                                            {
                                                int re = (maxtrunk - Ind);
                                                for (int j = 0; j < re; j++)
                                                {
                                                    Ind++;
                                                    Interior += "[" + Ind + "] Vacío <br>";
                                                }
                                            }
                                            Session.GetRoleplay().CarWSBaul = Interior;
                                            Session.GetRoleplay().UpdateInteractingUserDialogues();
                                            Session.GetRoleplay().RefreshStatDialogue();
                                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_charweapons", "");
                                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "baul");
                                        }
                                        #endregion
                                        break;
                                        #endregion
                                    }
                                case "consumible":
                                    {
                                        #region Execute
                                        Session.SendWhisper("Al parecer ese Objeto debe sacarse especificando la cantidad usando ':baul sacar [id] [cantidad]'.", 1);
                                        break;
                                        #endregion
                                    }
                                default:
                                    {
                                        Session.SendWhisper("Objeto inválido: Solo es posible sacar bidon/objeto/crack/medicamentos/marihuana/nombre-de-arma.", 1);
                                    }
                                    break;
                            }
                            break;
                            #endregion
                        }
                    default:
                        {
                            Session.SendWhisper("Comando inválido. Usa ':baul guardar [objeto]' o ':baul sacar [id]'.", 1);
                        }
                        break;
                }
            }
            #endregion
        }
    }
}