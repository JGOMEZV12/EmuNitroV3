using System;
using System.Linq;
using System.Text;
using System.Drawing;
using Polar.Utilities;
using System.Collections.Generic;
using Polar.HabboRoleplay.Timers;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using System.Threading;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Criminal
{
    class SmokeCommand : IChatCommand
    {
        public string Type;
        public string PermissionRequired
        {
            get { return "command_criminal_activity_smoke"; }
        }

        public string Parameters
        {
            get { return "%droga%"; }
        }

        public string Description
        {
            get { return "Consumir droga, marihuana, cocaina, pildoras y cigarrillos"; }
        }
        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Params.Length > 1)
                Type = Params[1].ToLower();
            #endregion

            #region Execute
            switch (Type)
            {
                #region Weed
                case "marihuana":
                    {
                        if (Session.GetRoleplay().Weed < 1)
                        {
                            Session.SendWhisper("¡Necesitas al menos 1 gramo de marihuana para drogarte!", 1);
                            return;
                        }


                        if (Session.GetRoleplay().TryGetCooldown("weed", false))
                        {
                            Session.SendWhisper("Usted ya está alto de marihuana", 1);
                            return;
                        }

                        if (Session.GetRoleplay().IsDead)
                        {
                            Session.SendWhisper("¡No puedes completar esta acción mientras estás muerto!", 1);
                            return;
                        }

                        //Session.GetRoleplay().CooldownManager.CreateCooldown("weed", 1000, 60);
                        Session.GetRoleplay().Weed--;
                        if ((Session.GetRoleplay().CurHealth + 8) >= Session.GetRoleplay().MaxHealth)
                        {
                            Session.GetRoleplay().CurHealth = Session.GetRoleplay().MaxHealth;
                            Session.GetRoleplay().CurHealth += 20;
                        }
                        else
                        {
                            Session.GetRoleplay().CurHealth += 20;
                        }
                        if ((Session.GetRoleplay().CurEnergy + 8) >= Session.GetRoleplay().MaxEnergy)
                            Session.GetRoleplay().CurEnergy = Session.GetRoleplay().MaxEnergy;
                        else
                            Session.GetRoleplay().CurEnergy += 20;

                        Session.GetRoleplay().Hunger += 5;
                        Session.GetRoomUser().ApplyEffect(511);
                        Session.GetRoleplay().CurEnergy -= 1;
                        Session.GetRoleplay().CooldownManager.CreateCooldown("weed", 1000, 60);
                        Session.Shout("*Saca de su bolsillo un porro de marihuana [+20] Salud, [+20] Energia, [+5] hambrea*", 4);
                        Session.GetRoleplay().UpdateTimerDialogue("Fumar-Effect", "add", 70, 100);
                        Session.GetRoleplay().WeedTimer = new WeedTimer(Session);
                        if (!Session.GetRoleplay().WantedFor.Contains("Fumar sustancias ilegales"))
                            Session.GetRoleplay().WantedFor = Session.GetRoleplay().WantedFor + "Fumar sustancias ilegales, ";
                        break;
                    }
                #endregion

                #region Cigarettes
                case "cig":
                case "cigs":
                case "cigarette":
                case "cigarettes":
                case "cigarrillos":
                case "cigarros":
                case "cigarro":
                    {
                        if (Session.GetRoleplay().Cigarettes < 1)
                        {
                            Session.SendWhisper("¡Usted necesita al menos un cigarrillo para fumarlo!", 1);
                            return;
                        }

                        if (Session.GetRoleplay().CurAlcohol < 0)
                        {
                            Session.SendWhisper("¡Ya estas libre de alcohol en tu organismo!", 1);
                            return;
                        }

                        if (Session.GetRoleplay().TryGetCooldown("cigarette"))
                            return;



                        if (Session.GetRoleplay().IsDead)
                        {
                            Session.SendWhisper("¡No puedes completar esta acción mientras estás muerto!", 1);
                            return;
                        }

                        if (Session.GetRoleplay().IsJailed)
                        {
                            Session.SendWhisper("¡No puedes completar esta acción mientras estás en la cárcel!", 1);
                            return;
                        }
                        Session.GetRoleplay().CooldownManager.CreateCooldown("cigarette", 1000, 5);
                        Session.GetRoleplay().Cigarettes--;
                        if ((Session.GetRoleplay().CurAlcohol - 35) >= Session.GetRoleplay().MaxAlcohol)
                            Session.GetRoleplay().CurAlcohol = Session.GetRoleplay().MaxAlcohol;
                        else
                            Session.GetRoleplay().CurAlcohol -= 35;

                        if ((Session.GetRoleplay().CurEnergy + 2) >= Session.GetRoleplay().MaxEnergy)
                            Session.GetRoleplay().CurEnergy = Session.GetRoleplay().MaxEnergy;
                        else
                            Session.GetRoleplay().CurEnergy += 2;

                        Session.Shout("*Saca un cigarrillo y lo fuma [+2 Energía, -35 Alcohol]*", 4);

                        if (!Room.RoomData.DriveEnabled)
                        {
                            if (!Session.GetRoleplay().WantedFor.Contains("Fumar sustancias legales dentro de un establecimiento"))
                                Session.GetRoleplay().WantedFor = Session.GetRoleplay().WantedFor + " Fumar sustancias legales dentro de un establecimiento, ";
                        }
                        break;
                    }
                #endregion

                #region Cocaine
                case "cocaina":
                    {
                        if (Session.GetRoleplay().Cocaine < 1)
                        {
                            Session.SendWhisper("¡Usted necesita al menos una linea para inhalar cocaina!", 1);
                            return;
                        }

                        if (Session.GetRoleplay().CurEnergy >= Session.GetRoleplay().MaxEnergy)
                        {
                            Session.SendWhisper("¡Ya tienes toda la energía!", 1);
                            return;
                        }

                        if (Session.GetRoleplay().TryGetCooldown("cocaina", false))
                            return;


                        if (Session.GetRoleplay().IsDead)
                        {
                            Session.SendWhisper("¡No puedes completar esta acción mientras estás muerto!", 1);
                            return;
                        }
                        RoomUser User = Session.GetRoomUser();
                        if (User == null)
                            return;
                        Session.GetRoleplay().HighOffCocaine = true;
                        Session.GetRoleplay().CooldownManager.CreateCooldown("cocaina", 1000, 60);
                        Session.GetRoleplay().Cocaine--;
                        if ((Session.GetRoleplay().CurHealth - 3) >= Session.GetRoleplay().MaxHealth)
                            Session.GetRoleplay().CurHealth = Session.GetRoleplay().MaxHealth;
                        else
                            Session.GetRoleplay().CurHealth -= 3;

                        if ((Session.GetRoleplay().CurEnergy + 5) >= Session.GetRoleplay().MaxEnergy)
                            Session.GetRoleplay().CurEnergy = Session.GetRoleplay().MaxEnergy;
                        else
                            Session.GetRoleplay().CurEnergy += 5;

                        Session.GetRoleplay().LevelEXP += 2;

                        Session.Shout("*Saca una linea de cocaina y la inhala con un pitillo [-3 Salud, +5 Energía, +2 Experiencia y adicción]*", 4);
                        Session.SendWhisper("Has recibido 2pts de experiencia (Escribe  :yo) para ver tu estado y tu próximo nivel es: " + (Session.GetRoleplay().Level + 1), 1);


                        if (!Room.RoomData.DriveEnabled)
                        {
                            if (!Session.GetRoleplay().WantedFor.Contains("Consumir sustancias ilegales dentro de un establecimiento"))
                                Session.GetRoleplay().WantedFor = Session.GetRoleplay().WantedFor + " Consumir sustancias ilegales dentro de un establecimiento, ";
                        }
                        break;
                    }
                #endregion

                #region Heroina
                case "heroina":
                    {
                        if (Session.GetRoleplay().Heroina < 10)
                        {
                            Session.SendWhisper("¡Necesitas al menos 10cc de heroina para drogarte!", 1);
                            return;
                        }


                        if (Session.GetRoleplay().TryGetCooldown("heroina", false))
                        {
                            Session.SendWhisper("Usted ya está alto de heroina", 1);
                            return;
                        }

                        if (Session.GetRoleplay().IsDead)
                        {
                            Session.SendWhisper("¡No puedes completar esta acción mientras estás muerto!", 1);
                            return;
                        }
                        RoomUser User = Session.GetRoomUser();
                        if (User == null)
                            return;
                       
                        Session.GetRoleplay().HighOffHeroina = true;
                        Session.GetRoleplay().CooldownManager.CreateCooldown("heroina", 1000, 120);
                        Session.GetRoleplay().CooldownManager.CreateCooldown("heroinaeffect", 1000, 120);
                        Session.GetRoleplay().Heroina -= 10;
                        if ((Session.GetRoleplay().CurHealth + 50) >= Session.GetRoleplay().MaxHealth)
                            Session.GetRoleplay().CurHealth = Session.GetRoleplay().MaxHealth;
                        else
                            Session.GetRoleplay().CurHealth += 50;

                        if ((Session.GetRoleplay().CurEnergy + 50) >= Session.GetRoleplay().MaxEnergy)
                            Session.GetRoleplay().CurEnergy = Session.GetRoleplay().MaxEnergy;
                        else
                            Session.GetRoleplay().CurEnergy += 50;
                        Session.GetRoleplay().LevelEXP += 2;

                        Session.GetRoomUser().CarryItem(1014);
                        Session.Shout("*Saca la inyectadora con la heroina y se la inyecta [+50 Salud, +50 Energía, +2 Experiencia y adicción]*", 4);
                        Session.SendWhisper("Has recibido 2pts de experiencia (Escribe  :yo) para ver tu estado y tu próximo nivel es: " + (Session.GetRoleplay().Level + 1), 1);
                        

                        if (!Room.RoomData.DriveEnabled)
                        {
                            if (!Session.GetRoleplay().WantedFor.Contains("Consumir sustancias ilegales dentro de un establecimiento"))
                                Session.GetRoleplay().WantedFor = Session.GetRoleplay().WantedFor + " Consumir sustancias ilegales dentro de un establecimiento, ";
                        }
                        break;
                    }
                #endregion

                #region Pildoras
                case "pildoras":
                    {
                        if (Session.GetRoleplay().Pildoras < 1)
                        {
                            Session.SendWhisper("¡Usted necesita al menos una pildora!", 1);
                            return;
                        }

                        if (Session.GetRoleplay().TryGetCooldown("pildoras"))
                            return;

                        if (Session.GetRoleplay().IsDead)
                        {
                            Session.SendWhisper("¡No puedes completar esta acción mientras estás muerto!", 1);
                            return;
                        }

                        if (Session.GetRoleplay().IsJailed)
                        {
                            Session.SendWhisper("¡No puedes completar esta acción mientras estás en la cárcel!", 1);
                            return;
                        }
                        Session.GetRoleplay().CooldownManager.CreateCooldown("pildora", 1000, 5);
                        Session.GetRoleplay().Pildoras--;
                        RoomUser User = Session.GetRoomUser();
                        if (User == null)
                            return;

                        User.CarryItem(1013);
                        if ((Session.GetRoleplay().CurHealth - 10) >= Session.GetRoleplay().MaxHealth)
                            Session.GetRoleplay().CurHealth = Session.GetRoleplay().MaxHealth;
                        else
                            Session.GetRoleplay().CurHealth += 10;

                        if ((Session.GetRoleplay().CurEnergy + 10) >= Session.GetRoleplay().MaxEnergy)
                            Session.GetRoleplay().CurEnergy = Session.GetRoleplay().MaxEnergy;
                        else
                            Session.GetRoleplay().CurEnergy += 10;
                        Session.GetRoleplay().StrengthEXP += 50;

                        Session.Shout("*Consume una pildora de fuerza [+10 Salud, +10 Energía, +50 En experiencia de fuerza]*", 4);
                        Session.SendWhisper("Has recibido 50 de experiencia en fuerza (Escribe  :yo) para ver tu estado y tu próximo nivel es: " + (Session.GetRoleplay().Strength + 1), 1);

                        if (!Room.RoomData.DriveEnabled)
                        {
                            if (!Session.GetRoleplay().WantedFor.Contains("Consumir sustancias ilegales dentro de un establecimiento"))
                                Session.GetRoleplay().WantedFor = Session.GetRoleplay().WantedFor + " Consumir sustancias ilegales dentro de un establecimiento, ";
                        }
                        break;
                    }
                #endregion

                #region Default
                default:
                    {
                        Session.SendWhisper("Sólo se puede consumir 'hierba' 'cigarrillos' 'cocaina'", 1);
                        break;
                    }
                #endregion
            }
            #endregion
        }
    }
}