using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Pathfinding;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.Food;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Quests;
using Polar.HabboRoleplay.RPRoom;
using Polar.HabboRoleplay.Weapons;
using Polar.HabboRoleplay.Turfs;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Commands;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Interactions.Items
{
    class CreateCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_offers_create"; }
        }

        public string Parameters
        {
            get { return "%objeto%"; }
        }

        public string Description
        {
            get { return "Permite crear objetos. EJ: (piezas, armas, etc)"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Variables
            string MyCity = Room.City;
            #endregion

            #region Conditions
            if (Params.Length != 2)
            {
                Session.SendWhisper("Debes ingresar el nombre del objeto. :crear [objeto]", 1);
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

            if (Session.GetRoleplay().TryGetCooldown("crear", true))
            {
                Session.SendWhisper("Por favor espera un poco para hacer eso nuevamente.", 1);
                return;
            }
            #endregion

            #region Execute
            string Type = Params[1].ToLower();

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
                #region Piezas (only Gunners)
                case "piezas":
                    {
                        #region Group Conditions
                        List<Group> Groups = PolarEnvironment.GetGame().GetGroupManager().GetJobsForUser(Session.GetHabbo().Id);

                        if (Groups.Count <= 0)
                        {
                            Session.SendWhisper("No tienes ningún trabajo para hacer eso.", 1);
                            return;
                        }

                        int GroupNumber = -1;

                        if (Groups[0].GType != 2)
                        {
                            if (Groups.Count > 1)
                            {
                                if (Groups[1].GType != 2)
                                {
                                    Session.SendWhisper("((No perteneces a ningún trabajo usar ese comando))", 1);
                                    return;
                                }
                                GroupNumber = 1; // Segundo indicie de variable
                            }
                            else
                            {
                                Session.SendWhisper("((No perteneces a ningún trabajo para usar ese comando))", 1);
                                return;
                            }
                        }
                        else
                        {
                            GroupNumber = 0; // Primer indice de Variable Group
                        }

                        Session.GetRoleplay().JobId = Groups[GroupNumber].Id;
                        Session.GetRoleplay().JobRank = Groups[GroupNumber].Members[Session.GetHabbo().Id].UserRank;
                        #endregion

                        #region Extra Conditions            
                        // Existe el trabajo?
                        if (!GroupManager.JobExists(Session.GetRoleplay().JobId, Session.GetRoleplay().JobRank))
                        {
                            Session.GetRoleplay().TimeWorked = 0;
                            Session.GetRoleplay().JobId = 1; // Desempleado
                            Session.GetRoleplay().JobRank = 1;

                            //Room.Group.DeleteMember(Session.GetHabbo().Id);// OJO ACÁ

                            Session.SendWhisper("Lo sentimos, ese trabajo no existe. Te hemos removido ese trabajo.", 1);
                            return;
                        }

                        if (!GroupManager.HasJobCommand(Session, "armero"))
                        {
                            Session.SendWhisper("Debes tener el trabajo de Armero para usar ese comando.", 1);
                            return;
                        }
                        RPRoom Data;
                        int ArmID = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetArmeros(MyCity, out Data);//armeros de la cd.
                        if (Session.GetHabbo().CurrentRoomId != ArmID)
                        {
                            Session.SendWhisper("Debes ir a la Fábrica de Armas para hacer eso.", 1);
                            return;
                        }

                        if (Session.GetRoleplay().PassiveMode)
                        {
                            Session.SendWhisper("No puedes hacer eso mientras estás en modo pasivo.", 1);
                            return;
                        }

                        #endregion

                        #region Comodin Conditions
                        Item BTile = null;
                        BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carro" && x.Coordinate == Session.GetRoomUser().Coordinate);
                        if (BTile == null)
                        {
                            Session.SendWhisper("Debes acercarte al punto de creación de Piezas de la Fábrica.", 1);
                            return;
                        }
                        #endregion

                        #region Execute
                        int Pieces = Session.GetRoleplay().ArmMat;

                        if (Session.GetRoleplay().ArmMat <= 0)
                        {
                            Session.SendWhisper("No tienes materiales para crear piezas.", 1);
                            return;
                        }

                        RoleplayManager.Shout(Session, "*Usa " + Session.GetRoleplay().ArmMat + " materiales para crear " + Pieces + " piezas*", 5);
                        Session.SendWhisper("¡Bien hecho! Ahora usa el comando ':armas' para ver un listado de ellas y la Cantidad de Piezas que requieren para ser creadas con ':crear [nombre-del-arma]'.", 1);
                        Session.GetRoleplay().ArmPieces += Pieces;
                        Session.GetRoleplay().ArmMat = 0;
                        RoleplayManager.JobSkills(Session, Session.GetRoleplay().JobId, Session.GetRoleplay().ArmLvl, Session.GetRoleplay().ArmXP);
                        Session.GetRoleplay().CooldownManager.CreateCooldown("crear", 1000, 3);
                        #endregion
                    }
                    break;
                #endregion

                #region Arma (only Gunners)
                case "weapon":
                    {
                        #region Group Conditions
                        List<Group> Groups = PolarEnvironment.GetGame().GetGroupManager().GetJobsForUser(Session.GetHabbo().Id);

                        if (Groups.Count <= 0)
                        {
                            Session.SendWhisper("No tienes ningún trabajo para hacer eso.", 1);
                            return;
                        }

                        int GroupNumber = -1;

                        if (Groups[0].GType != 2)
                        {
                            if (Groups.Count > 1)
                            {
                                if (Groups[1].GType != 2)
                                {
                                    Session.SendWhisper("((No perteneces a ningún trabajo usar ese comando))", 1);
                                    return;
                                }
                                GroupNumber = 1; // Segundo indicie de variable
                            }
                            else
                            {
                                Session.SendWhisper("((No perteneces a ningún trabajo para usar ese comando))", 1);
                                return;
                            }
                        }
                        else
                        {
                            GroupNumber = 0; // Primer indice de Variable Group
                        }

                        Session.GetRoleplay().JobId = Groups[GroupNumber].Id;
                        Session.GetRoleplay().JobRank = Groups[GroupNumber].Members[Session.GetHabbo().Id].UserRank;
                        #endregion

                        #region Extra Conditions            
                        // Existe el trabajo?
                        if (!GroupManager.JobExists(Session.GetRoleplay().JobId, Session.GetRoleplay().JobRank))
                        {
                            Session.GetRoleplay().TimeWorked = 0;
                            Session.GetRoleplay().JobId = 0; // Desempleado
                            Session.GetRoleplay().JobRank = 0;

                            //Room.Group.DeleteMember(Session.GetHabbo().Id);// OJO ACÁ

                            Session.SendWhisper("Lo sentimos, ese trabajo no existe. Te hemos removido ese trabajo.", 1);
                            return;
                        }

                        if (!GroupManager.HasJobCommand(Session, "armero"))
                        {
                            Session.SendWhisper("Debes tener el trabajo de Armero para usar ese comando.", 1);
                            return;
                        }
                        RPRoom Data;
                        int ArmID = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetArmeros(MyCity, out Data);//armeros de la cd.
                        if (Session.GetHabbo().CurrentRoomId != ArmID)
                        {
                            Session.SendWhisper("Debes ir a la Fábrica de Armas para hacer eso.", 1);
                            return;
                        }

                        if (Session.GetRoleplay().PassiveMode)
                        {
                            Session.SendWhisper("No puedes hacer eso mientras estás en modo pasivo.", 1);
                            return;
                        }
                        #endregion

                        #region Comodin Conditions
                        Item BTile = null;
                        BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carr2" && x.Coordinate == Session.GetRoomUser().Coordinate);
                        if (BTile == null)
                        {
                            Session.SendWhisper("Debes acercarte al punto de creación de Armas de la Fábrica.", 1);
                            return;
                        }
                        #endregion

                        #region Weapon Conditions
                        if (weapon == null)
                        {
                            Session.SendWhisper("'" + Type + "' no es un arma válida.");
                            return;
                        }
                        if (Session.GetRoleplay().OwnedWeapons.ContainsKey(weapon.Name))
                        {
                            Session.SendWhisper("Ya tienes una " + weapon.PublicName + " en tu inventario. ¡No es posible tener dos armas del mismo tipo en tu Inventario!", 1);
                            return;
                        }
                        if (Session.GetRoleplay().ArmPieces < weapon.CostFine)
                        {
                            Session.SendWhisper("Necesitas al menos " + weapon.CostFine + " piezas para crear una " + weapon.PublicName + ".", 1);
                            return;
                        }
                        if (Session.GetRoleplay().ArmLvl < weapon.LevelRequirement)
                        {
                            Session.SendWhisper("Necesitas al menos nivel " + weapon.LevelRequirement + "de Armero para crear una " + weapon.PublicName + ".", 1);
                            return;
                        }
                        #endregion

                        #region Execute
                        RoleplayManager.Shout(Session, "*Crea un/a " + weapon.PublicName + " y gasta " + weapon.CostFine + " piezas*", 5);
                        Session.GetRoleplay().ArmPieces -= weapon.CostFine;
                        RoleplayManager.AddWeapon(Session, weapon);
                        Session.GetRoleplay().OwnedWeapons = null;
                        Session.GetRoleplay().OwnedWeapons = Session.GetRoleplay().LoadAndReturnWeapons();
                        RoleplayManager.JobSkills(Session, Session.GetRoleplay().JobId, Session.GetRoleplay().ArmLvl, Session.GetRoleplay().ArmXP);
                        Session.GetRoleplay().CooldownManager.CreateCooldown("crear", 1000, 3);

                        //Session.GetRoleplay().GunsFab++;

                        #region Gang Bonif
                        List<Group> MyGang = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(Session.GetHabbo().Id);
                        if (MyGang != null && MyGang.Count > 0)
                        {
                            if (MyGang[0].BankRuptcy)
                            {
                                Session.SendWhisper("Tu banda está en bancarota y no podrás gozar de los beneficios de ella.", 1);
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

                                    Session.GetHabbo().Credits += bonif;
                                    //Session.GetRoleplay().MoneyEarned += bonif;
                                    Session.GetHabbo().UpdateCreditsBalance();

                                   // MyGang[0].AddLog(Session.GetHabbo().Id, Session.GetHabbo().Username + " ha obtenido $ " + String.Format("{0:N0}", bonif) + " para la banda al crear un arma.", bonif);
                                    Session.SendWhisper("¡Tu banda y tú han ganado una bonificación extra de $ " + String.Format("{0:N0}", bonif) + " por la creación de esta arma!", 1);
                                }
                            }
                        }
                        #endregion
                        #endregion
                    }
                    break;
                #endregion

                #region Default
                default:
                    {
                        Session.SendWhisper("'" + Type + "' no es un objeto válido a crear.", 1);
                        break;
                    }
                    #endregion
            }
            #endregion
        }
    }
}