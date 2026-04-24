using ConnectionManager;
﻿using System;
using System.Linq;
using Polar.Net;
using System.Threading.Tasks;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.GameClients;
using System.IO;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Houses;
using System.Collections.Generic;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.RPRoom;
using Polar.HabboRoleplay.Weapons;
using Polar.HabboRoleplay.Turfs;

namespace Polar.HabboHotel.Roleplay.Web.Outgoing.Misc
{
    /// <summary>
    /// ArmeroWebEvent class.
    /// </summary>
    class ArmeroWebEvent : IWebEvent
    {
        /// <summary>
        /// Executes socket data.
        /// </summary>
        /// <param name="Client"></param>
        /// <param name="Data"></param>
        /// <param name="Socket"></param>
        public void Execute(GameClient Client, string Data, ConnectionInformation Socket)
        {

            if (!PolarEnvironment.GetGame().GetWebEventManager().SocketReady(Client, true) || !PolarEnvironment.GetGame().GetWebEventManager().SocketReady(Socket))
                return;

            string Action = (Data.Contains(',') ? Data.Split(',')[0] : Data);

            switch (Action)
            {

                #region Open Pieces
                case "open_pieces":
                    {
                        if (Client.GetRoleplay().TryGetCooldown("viewarmero"))
                            return;

                        Socket.SendWS( "compose_armero|open_pieces|" + Client.GetRoleplay().ArmPieces + "|" + Client.GetRoleplay().ArmMat + "|" + Client.GetRoleplay().ArmMat / 10);
                        Client.GetRoleplay().ViewArmeroPieces = true;
                        Client.GetRoleplay().CooldownManager.CreateCooldown("viewarmero", 1000, 3);
                    }
                    break;
                #endregion

                #region Open Armas
                case "open_weapons":
                    {
                        if (Client.GetRoleplay().TryGetCooldown("viewarmero"))
                            return;

                        Socket.SendWS( "compose_armero|open_weapons|" + Client.GetRoleplay().ArmPieces);
                        Client.GetRoleplay().ViewArmeroWeapons = true;
                        Client.GetRoleplay().CooldownManager.CreateCooldown("viewarmero", 1000, 3);
                    }
                    break;
                #endregion

                #region Close Pieces
                case "close_pieces":
                    {
                        Client.GetRoleplay().ViewArmeroPieces = false;
                        Socket.SendWS( "compose_armero|close_pieces|");
                        break;
                    }
                #endregion

                #region Close Weapons
                case "close_weapons":
                    {
                        Client.GetRoleplay().ViewArmeroWeapons = false;
                        Socket.SendWS( "compose_armero|close_weapons|");
                        break;
                    }
                #endregion

                #region Create Pieces
                case "create_pieces":
                    {
                        if (Client.GetRoleplay().TryGetCooldown("crear"))
                            return;
                        if (Client.GetRoleplay().PassiveMode)
                        {
                            Socket.SendWS( "compose_armero|armmsg|No puedes hacer eso mientras estás en modo pasivo.");
                            return;
                        }
                        if (Client.GetRoomUser() == null || Client.GetRoomUser().GetRoom() == null)
                            return;

                        Room Room = Client.GetRoomUser().GetRoom();

                        if (Room == null)
                            return;

                        string MyCity = Room.City;

                        #region Group Conditions
                        List<Group> Groups = PolarEnvironment.GetGame().GetGroupManager().GetJobsForUser(Client.GetHabbo().Id);

                        if (Groups.Count <= 0)
                        {
                            Socket.SendWS( "compose_armero|armmsg|No tienes ningún trabajo para hacer eso.");
                            return;
                        }

                        int GroupNumber = -1;

                        if (Groups[0].GType != 2)
                        {
                            if (Groups.Count > 1)
                            {
                                if (Groups[1].GType != 2)
                                {
                                    Socket.SendWS( "compose_armero|armmsg|((No perteneces a ningún trabajo usar ese comando))");
                                    return;
                                }
                                GroupNumber = 1; // Segundo indicie de variable
                            }
                            else
                            {
                                Socket.SendWS( "compose_armero|armmsg|((No perteneces a ningún trabajo para usar ese comando))");
                                return;
                            }
                        }
                        else
                        {
                            GroupNumber = 0; // Primer indice de Variable Group
                        }

                        Client.GetRoleplay().JobId = Groups[GroupNumber].Id;
                        Client.GetRoleplay().JobRank = Groups[GroupNumber].Members[Client.GetHabbo().Id].UserRank;
                        #endregion

                        #region Extra Conditions            
                        // Existe el trabajo?
                        if (!GroupManager.JobExists(Client.GetRoleplay().JobId, Client.GetRoleplay().JobRank))
                        {
                            Client.GetRoleplay().TimeWorked = 0;
                            Client.GetRoleplay().JobId = 1; // Desempleado
                            Client.GetRoleplay().JobRank = 1;

                            //Room.Group.DeleteMember(Client.GetHabbo().Id);// OJO ACÁ

                            Socket.SendWS( "compose_armero|armmsg|Lo sentimos, ese trabajo no existe. Te hemos removido ese trabajo.");
                            return;
                        }

                        if (!GroupManager.HasJobCommand(Client, "armero"))
                        {
                            Socket.SendWS( "compose_armero|armmsg|Debes tener el trabajo de Armero para usar ese comando.");
                            return;
                        }
                        RPRoom mData;
                        int ArmID = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetArmeros(MyCity, out mData);//armeros de la cd.
                        if (Client.GetHabbo().CurrentRoomId != ArmID)
                        {
                            Socket.SendWS( "compose_armero|armmsg|Debes ir a la Fábrica de Armas para hacer eso.");
                            return;
                        }

            
                        #endregion

                        /*#region Comodin Conditions
                        Item BTile = null;
                        BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carro" && x.Coordinate == Client.GetRoomUser().Coordinate);
                        if (BTile == null)
                        {
                            Socket.SendWS( "compose_armero|armmsg|Debes acercarte al punto de creación de Piezas de la Fábrica.");
                            return;
                        }
                        #endregion*/

                        #region Execute
                        if (Client.GetRoleplay().ArmMat <= 0)
                        {
                            Socket.SendWS( "compose_armero|armmsg|No tienes materiales para crear piezas.");
                            return;
                        }

                        int Pieces = Client.GetRoleplay().ArmMat / 1;

                        RoleplayManager.Shout(Client, "*Usa " + Client.GetRoleplay().ArmMat + " materiales para crear " + Pieces + " piezas*", 5);
                        Socket.SendWS( "compose_armero|armmsg|<b style='color:green;'>¡Bien hecho! Ahora dirígte al punto de fabricación de armas.</b>");
                        Client.GetRoleplay().ArmPieces += Pieces;
                        Client.GetRoleplay().ArmMat = 0;
                        RoleplayManager.JobSkills(Client, Client.GetRoleplay().JobId, Client.GetRoleplay().ArmLvl, Client.GetRoleplay().ArmXP);
                        Client.GetRoleplay().CooldownManager.CreateCooldown("crear", 1000, 3);

                        PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_armero", "open_pieces");
                        #endregion
                    }
                    break;
                #endregion

                #region Create Weapons
                case "create_weapons":
                    {
                        if (Client.GetRoleplay().TryGetCooldown("crear"))
                            return;
                        if (Client.GetRoleplay().PassiveMode)
                        {
                            Socket.SendWS( "compose_armero|armmsg|No puedes hacer eso mientras estás en modo pasivo.");
                            return;
                        }
                        if (Client.GetRoomUser() == null || Client.GetRoomUser().GetRoom() == null)
                            return;

                        Room Room = Client.GetRoomUser().GetRoom();

                        if (Room == null)
                            return;

                        string MyCity = Room.City;

                        #region Group Conditions
                        List<Group> Groups = PolarEnvironment.GetGame().GetGroupManager().GetJobsForUser(Client.GetHabbo().Id);

                        if (Groups.Count <= 0)
                        {
                            Socket.SendWS( "compose_armero|armmsg|No tienes ningún trabajo para hacer eso.");
                            return;
                        }

                        int GroupNumber = -1;

                        if (Groups[0].GType != 2)
                        {
                            if (Groups.Count > 1)
                            {
                                if (Groups[1].GType != 2)
                                {
                                    Socket.SendWS( "compose_armero|armmsg|((No perteneces a ningún trabajo usar ese comando))");
                                    return;
                                }
                                GroupNumber = 1; // Segundo indicie de variable
                            }
                            else
                            {
                                Socket.SendWS( "compose_armero|armmsg|((No perteneces a ningún trabajo para usar ese comando))");
                                return;
                            }
                        }
                        else
                        {
                            GroupNumber = 0; // Primer indice de Variable Group
                        }

                        Client.GetRoleplay().JobId = Groups[GroupNumber].Id;
                        Client.GetRoleplay().JobRank = Groups[GroupNumber].Members[Client.GetHabbo().Id].UserRank;
                        #endregion

                        #region Extra Conditions            
                        // Existe el trabajo?
                        if (!GroupManager.JobExists(Client.GetRoleplay().JobId, Client.GetRoleplay().JobRank))
                        {
                            Client.GetRoleplay().TimeWorked = 0;
                            Client.GetRoleplay().JobId = 0; // Desempleado
                            Client.GetRoleplay().JobRank = 0;

                            //Room.Group.DeleteMember(Client.GetHabbo().Id);// OJO ACÁ

                            Socket.SendWS( "compose_armero|armmsg|Lo sentimos, ese trabajo no existe. Te hemos removido ese trabajo.");
                            return;
                        }

                        if (!GroupManager.HasJobCommand(Client, "armero"))
                        {
                            Socket.SendWS( "compose_armero|armmsg|Debes tener el trabajo de Armero para usar ese comando.");
                            return;
                        }
                        RPRoom mData;
                        int ArmID = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetArmeros(MyCity, out mData);//armeros de la cd.
                        if (Client.GetHabbo().CurrentRoomId != ArmID)
                        {
                            Socket.SendWS( "compose_armero|armmsg|Debes ir a la Fábrica de Armas para hacer eso.");
                            return;
                        }

                      
                        #endregion

                        #region Comodin Conditions
                        Item BTile = null;
                        BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carr2" && x.Coordinate == Client.GetRoomUser().Coordinate);
                        if (BTile == null)
                        {
                            Socket.SendWS( "compose_armero|armmsg|Debes acercarte al punto de creación de Piezas de la Fábrica.");
                            return;
                        }
                        #endregion

                        string[] ReceivedData = Data.Split(',');
                        string Type = ReceivedData[1].ToString();

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

                        #region Weapon Conditions
                        if (weapon == null)
                        {
                            Socket.SendWS( "compose_armero|armmsg|'" + Type + "' no es un arma válida.");
                            return;
                        }
                        if (Client.GetRoleplay().OwnedWeapons.ContainsKey(weapon.Name))
                        {
                            Socket.SendWS( "compose_armero|armmsg|Ya tienes una " + weapon.PublicName + " en tu inventario. ¡No es posible tener dos armas del mismo tipo en tu Inventario!");
                            return;
                        }
                        if (Client.GetRoleplay().ArmPieces < weapon.CostFine)
                        {
                            Socket.SendWS( "compose_armero|armmsg|Necesitas al menos " + weapon.CostFine + " piezas para crear una " + weapon.PublicName + ".");
                            return;
                        }
                        if (Client.GetRoleplay().ArmLvl < weapon.LevelRequirement)
                        {
                            Socket.SendWS( "compose_armero|armmsg|Necesitas al menos nivel " + weapon.LevelRequirement + "de Armero para crear una " + weapon.PublicName + ".");
                            return;
                        }
                        #endregion

                        #region Execute
                        RoleplayManager.Shout(Client, "*Crea un/a " + weapon.PublicName + " y gasta " + weapon.CostFine + " piezas*", 5);
                        Client.GetRoleplay().ArmPieces -= weapon.CostFine;
                        RoleplayManager.AddWeapon(Client, weapon);
                        Client.GetRoleplay().OwnedWeapons = null;
                        Client.GetRoleplay().OwnedWeapons = Client.GetRoleplay().LoadAndReturnWeapons();
                        RoleplayManager.JobSkills(Client, Client.GetRoleplay().JobId, Client.GetRoleplay().ArmLvl, Client.GetRoleplay().ArmXP);
                        Client.GetRoleplay().CooldownManager.CreateCooldown("crear", 1000, 3);

                        Socket.SendWS( "compose_armero|armmsg|<b style='color:green;'>¡Has fabricado una "+ weapon.PublicName +" nueva!.</b>");

                        PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_armero", "open_weapons");

                        //Client.GetRoleplay().GunsFab++;

                        #region Gang Bonif
                        List<Group> MyGang = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(Client.GetHabbo().Id);
                        if (MyGang != null && MyGang.Count > 0)
                        {
                            if (MyGang[0].BankRuptcy)
                            {
                                Client.SendWhisper("Tu banda está en bancarota y no podrás gozar de los beneficios de ella.", 1);
                            }
                            else
                            {
                                int NewTurfsCount = 0;
                                List<Turf> TF = PolarEnvironment.GetGame().GetGangTurfsManager().getTurfsbyGang(MyGang[0].Id);
                                if (TF != null && TF.Count > 0)
                                    NewTurfsCount = TF.Count;

                                int bonif = RoleplayManager.GangsTurfBonif * NewTurfsCount;
                                if (bonif > 0)
                                {
                                    //MyGang[0].GangFabGuns++;
                                    MyGang[0].Balance += bonif;
                                    //MyGang[0].UpdateStat("gang_fab_guns", MyGang[0].GangFabGuns);
                                    MyGang[0].SetBussines(MyGang[0].Balance, MyGang[0].Stock);

                                    Client.GetHabbo().Credits += bonif;
                                    //Client.GetRoleplay().MoneyEarned += bonif;
                                    Client.GetHabbo().UpdateCreditsBalance();

                                    MyGang[0].AddLog(Client.GetHabbo().Id, Client.GetHabbo().Username + " ha obtenido $ " + String.Format("{0:N0}", bonif) + " para la banda al crear un arma.", bonif);
                                    Client.SendWhisper("¡Tu banda y tú han ganado una bonificación extra de $ " + String.Format("{0:N0}", bonif) + " por la creación de esta arma!", 1);
                                }
                            }
                        }
                        #endregion
                        #endregion
                    }
                    break;
                    #endregion
            }
        }

        // ── Helper: envía texto como frame WebSocket usando ConnectionInformation
        private static void SendWS(ConnectionInformation socket, string message)
        {
            if (socket == null || string.IsNullOrEmpty(message)) return;
            socket.SendData(System.Text.Encoding.UTF8.GetBytes(message));
        }

    }
}
