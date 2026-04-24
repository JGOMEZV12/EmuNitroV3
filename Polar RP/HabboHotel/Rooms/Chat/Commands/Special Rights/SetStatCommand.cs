using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.HabboHotel.Rooms.Chat.Commands.SpecialRights
{
    class SetStatCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_set_stat"; }
        }

        public string Parameters
        {
            get { return "%username% %stat% %amount%"; }
        }

        public string Description
        {
            get { return "otorgas stats a un usuario."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length != 4 && Params[0].ToLower() != "setmhp" && Params[0].ToLower() != "setmenergy" && Params[0].ToLower() != "sethp" && Params[0].ToLower() != "setenergy" && Params[0].ToLower() != "sethunger" && Params[0].ToLower() != "sethygiene" && Params[0].ToLower() != "setchaleco" && Params[0].ToLower() != "setsida" && Params[0].ToLower() != "setfelicidad")
            {
                Session.SendWhisper("Debes ingresar el nombre de usuario, la estadística y la cantidad que deseas darles.", 1);
                return;
            }

            if (Params.Length != 3 && (Params[0].ToLower() == "setmhp" || Params[0].ToLower() == "setmenergy" || Params[0].ToLower() == "sethp" || Params[0].ToLower() == "setenergy" || Params[0].ToLower() == "sethunger" || Params[0].ToLower() == "sethygiene" || Params[0].ToLower() == "setchaleco" && Params[0].ToLower() != "setsida" && Params[0].ToLower() != "setfelicidad"))
            {
                Session.SendWhisper("Debes ingresar el nombre de usuario y la cantidad que deseas darles.", 1);
                return;
            }

            var TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);

            if (TargetClient == null)
            {
                Session.SendWhisper("¡Este usuario no pudo ser encontrado!Tal vez están fuera de línea.", 1);
                return;
            }

            if (TargetClient.GetHabbo() == null || TargetClient.GetRoomUser() == null)
            {
                Session.SendWhisper("¡Este usuario no pudo ser encontrado!Tal vez están fuera de línea.", 1);
                return;
            }

            string Type = "";
            if (Params[0].ToLower() == "sethp")
                Type = "hp";
            else if (Params[0].ToLower() == "setenergy")
                Type = "energy";
            else if (Params[0].ToLower() == "sethunger")
                Type = "hunger";
            else if (Params[0].ToLower() == "setsida")
                Type = "sida";
            else if (Params[0].ToLower() == "sethygiene")
                Type = "hygiene";
            else if (Params[0].ToLower() == "setchaleco")
                Type = "chaleco";
            else if (Params[0].ToLower() == "setfelicidad")
                Type = "felicidad";
            else
                Type = Params[2].ToLower();

            switch (Type)
            {
                #region Health
                case "hp":
                case "vida":
                case "health":
                    {
                        string Amount = Params[0].ToLower() == "sethp" ? Params[2] : Params[3];

                        int HPAmount;
                        if (int.TryParse(Amount, out HPAmount))
                        {
                            if (HPAmount <= 0)
                            {
                                Session.SendWhisper("El monto no puede contener -", 1);
                                return;
                            }

                            if (HPAmount < 0)
                                HPAmount = 0;

                            if (TargetClient.GetHabbo().VIPRank > 1 && HPAmount == 0)
                            {
                                Session.SendWhisper("¡No puedes matar a otros súper miembros del personal!", 1);
                                return;
                            }

                            TargetClient.GetRoleplay().CurHealth = HPAmount;

                            Session.SendWhisper("Establecido con éxito " + TargetClient.GetHabbo().Username + "'s health to " + HPAmount + "!", 1);
                        }
                        else
                            Session.SendWhisper("¡Por favor ingrese un número valido!", 1);
                        break;
                    }
                #endregion

                #region Energy
                case "energy":
                case "energia":
                    {
                        string Amount = Params[0].ToLower() == "setenergy" ? Params[2] : Params[3];

                        int EnergyAmount;
                        if (int.TryParse(Amount, out EnergyAmount))
                        {
                            if (EnergyAmount <= 0)
                            {
                                Session.SendWhisper("El monto no puede contener -", 1);
                                return;
                            }

                            if (EnergyAmount < 0)
                                EnergyAmount = 0;
                            
                            TargetClient.GetRoleplay().CurEnergy = EnergyAmount;

                            Session.SendWhisper("Se le ha establecido con éxito la energia: " + TargetClient.GetRoleplay().CurEnergy + " a " + TargetClient.GetHabbo().Username + " !", 1);
                        }
                        else
                            Session.SendWhisper("¡Por favor ingrese un número valido!", 1);
                        break;
                    }
                #endregion

                #region MaxHealth
                case "maxhp":
                case "maxvida":
                case "maxhealth":
                    {
                        string Amount = Params[0].ToLower() == "setmhp" ? Params[2] : Params[3];

                        int HPAmount;
                        if (int.TryParse(Amount, out HPAmount))
                        {
                            if (HPAmount <= 0)
                            {
                                Session.SendWhisper("El monto no puede contener -", 1);
                                return;
                            }

                            if (HPAmount < 0)
                                HPAmount = 0;

                            if (TargetClient.GetHabbo().VIPRank > 1 && HPAmount == 0)
                            {
                                Session.SendWhisper("¡No puedes matar a otros súper miembros del personal!", 1);
                                return;
                            }

                            TargetClient.GetRoleplay().MaxHealth = HPAmount;

                            Session.SendWhisper("Establecido con éxito " + TargetClient.GetHabbo().Username + "'s health to " + HPAmount + "!", 1);
                        }
                        else
                            Session.SendWhisper("¡Por favor ingrese un número valido!", 1);
                        break;
                    }
                #endregion

                #region MaxEnergy
                case "maxenergy":
                case "maxenergia":
                    {
                        string Amount = Params[0].ToLower() == "setmenergy" ? Params[2] : Params[3];

                        int EnergyAmount;
                        if (int.TryParse(Amount, out EnergyAmount))
                        {
                            if (EnergyAmount <= 0)
                            {
                                Session.SendWhisper("El monto no puede contener -", 1);
                                return;
                            }

                            if (EnergyAmount < 0)
                                EnergyAmount = 0;

                            TargetClient.GetRoleplay().MaxEnergy = EnergyAmount;

                            Session.SendWhisper("Se le ha establecido con éxito la energia: " + TargetClient.GetRoleplay().MaxEnergy + " a " + TargetClient.GetHabbo().Username + " !", 1);
                        }
                        else
                            Session.SendWhisper("¡Por favor ingrese un número valido!", 1);
                        break;
                    }
                #endregion

                #region Stamina
                case "stamina":
                case "stam":
                    {
                        int Amount;
                        if (int.TryParse(Params[3], out Amount))
                        {
                            if (Amount <= 0)
                            {
                                Session.SendWhisper("El monto no puede contener -", 1);
                                return;
                            }

                            if (Amount < 0)
                                Amount = 0;

                            if (Amount > RoleplayManager.StaminaCap)
                                Amount = RoleplayManager.StaminaCap;

                            TargetClient.GetRoleplay().Stamina = Amount;
                            TargetClient.GetRoleplay().StaminaEXP = LevelManager.StaminaLevels[TargetClient.GetRoleplay().Stamina];
                            TargetClient.GetRoleplay().MaxEnergy = ((Amount * 5) + 100);
                            Session.SendWhisper("Se le ha establecido con éxito la stamina: " + Amount + " a " + TargetClient.GetHabbo().Username + " !", 1);
                        }
                        else
                            Session.SendWhisper("¡Por favor ingrese un número valido!", 1);
                        break;
                    }
                #endregion

                #region Intelligence
                case "intelligence":
                case "intel":
                case "int":
                    {
                        int Amount;
                        if (int.TryParse(Params[3], out Amount))
                        {
                            if (Amount <= 0)
                            {
                                Session.SendWhisper("El monto no puede contener -", 1);
                                return;
                            }

                            if (Amount < 0)
                                Amount = 0;

                            if (Amount > RoleplayManager.IntelligenceCap)
                                Amount = RoleplayManager.IntelligenceCap;

                            TargetClient.GetRoleplay().Intelligence = Amount;
                            TargetClient.GetRoleplay().IntelligenceEXP = LevelManager.IntelligenceLevels[TargetClient.GetRoleplay().Intelligence];
                            Session.SendWhisper("Se le ha establecido con éxito la inteligencia: " +Amount + " a " + TargetClient.GetHabbo().Username + " !", 1);
                        }
                        else
                            Session.SendWhisper("¡Por favor ingrese un número valido!", 1);
                        break;
                    }
                #endregion

                #region Strength
                case "strength":
                case "str":
                    {
                        int Amount;
                        if (int.TryParse(Params[3], out Amount))
                        {
                            if (Amount <= 0)
                            {
                                Session.SendWhisper("El monto no puede contener -", 1);
                                return;
                            }

                            if (Amount < 0)
                                Amount = 0;

                            TargetClient.GetRoleplay().Strength = Amount;

                            if (LevelManager.StrengthLevels.ContainsKey(TargetClient.GetRoleplay().Strength))
                                TargetClient.GetRoleplay().StrengthEXP = LevelManager.StrengthLevels[TargetClient.GetRoleplay().Strength];

                            Session.SendWhisper("Establecido con éxito a " + TargetClient.GetHabbo().Username + " la fuerza: " + Amount + "!", 1);
                        }
                        else
                            Session.SendWhisper("¡Por favor ingrese un número valido!", 1);
                        break;
                    }
                #endregion

                #region Level
                case "level":
                    {
                        int Amount;
                        if (int.TryParse(Params[3], out Amount))
                        {
                            if (Amount <= 0)
                            {
                                Session.SendWhisper("El monto no puede contener -", 1);
                                return;
                            }

                            if (Amount < 1)
                                Amount = 1;

                            if (!LevelManager.Levels.ContainsKey(Amount) || Amount > RoleplayManager.LevelCap)
                            {
                                Session.SendWhisper("Ese nivel no existe", 1);
                                return;
                            }

                            TargetClient.GetRoleplay().Level = Amount;
                            TargetClient.GetRoleplay().LevelEXP = LevelManager.Levels[TargetClient.GetRoleplay().Level];
                            if (TargetClient.GetHabbo().CurrentRoom != null)
                            {
                                var currentRoomUser = TargetClient.GetHabbo().CurrentRoom.GetRoomUserManager()?.GetRoomUserByHabbo(TargetClient.GetHabbo().Id);
                                if (currentRoomUser != null)
                                {
                                    TargetClient.SendMessage(new UserChangeComposer(currentRoomUser, true));
                                    TargetClient.GetHabbo().CurrentRoom.SendMessage(new UserChangeComposer(currentRoomUser, false));
                                }
                            }
                            Session.SendWhisper("Se le ha establecido con éxito el nivel: " + Amount + " a " + TargetClient.GetHabbo().Username + " !", 1);
                            TargetClient.SendWhisper("Un administrador establece el nivel de tu personaje en " + Amount + "!", 1);

                        }
                        else
                            Session.SendWhisper("¡Por favor ingrese un número valido!", 1);
                        break;
                    }
                #endregion

                #region EXP
                case "xp":
                case "exp":
                    {
                        int Amount;
                        if (int.TryParse(Params[3], out Amount))
                        {
                            if (Amount <= 0)
                            {
                                Session.SendWhisper("El monto no puede contener -", 1);
                                return;
                            }
                            TargetClient.GetRoleplay().LevelEXP += Amount;
                            Session.SendWhisper("Diste a " + TargetClient.GetHabbo().Username + " " + Amount + " XP! Su EXP Ahora es: " + TargetClient.GetRoleplay().LevelEXP + "!", 1);
                            TargetClient.SendWhisper("Un administrador te dio " + Amount + " XP!", 1);
                        }
                        else
                            Session.SendWhisper("¡Por favor ingrese un número valido!", 1);
                        break;
                    }
                #endregion

                #region Hunger
                case "hunger":
                    {
                        string Amount = Params[0].ToLower() == "sethunger" ? Params[2] : Params[3];

                        int HungerAmount;
                        if (int.TryParse(Amount, out HungerAmount))
                        {
                            if (HungerAmount <= 0)
                            {
                                Session.SendWhisper("El monto no puede contener -", 1);
                                return;
                            }
                            TargetClient.GetRoleplay().Hunger = HungerAmount;
                            Session.SendWhisper("Establecido con éxito a " + TargetClient.GetHabbo().Username + " el hambre a: " + HungerAmount + "!", 1);
                        }
                        else
                            Session.SendWhisper("¡Por favor ingrese un número valido!", 1);
                        break;
                    }
                #endregion

                #region sida
                case "sida":
                    {
                        string Amount = Params[0].ToLower() == "setsida" ? Params[2] : Params[3];

                        int SidaAmount;
                        if (int.TryParse(Amount, out SidaAmount))
                        {
                            if (SidaAmount <= 0)
                            {
                                Session.SendWhisper("El monto no puede contener -", 1);
                                return;
                            }
                            TargetClient.GetRoleplay().Sida = SidaAmount;
                            Session.SendWhisper("Establecido con éxito a: " + TargetClient.GetHabbo().Username + " el sida en: " + SidaAmount + "!", 1);
                        }
                        else
                            Session.SendWhisper("¡Por favor ingrese un número valido!", 1);
                        break;
                    }
                #endregion

                #region basura
                case "basura":
                    {
                        string Amount = Params[0].ToLower() == "setbasura" ? Params[2] : Params[3];

                        int BasuraAmount;
                        if (int.TryParse(Amount, out BasuraAmount))
                        {
                            if (BasuraAmount <= 0)
                            {
                                Session.SendWhisper("El monto no puede contener -", 1);
                                return;
                            }
                            TargetClient.GetRoleplay().BasuTrashCount = BasuraAmount;
                            Session.SendWhisper("Establecido con éxito " + TargetClient.GetHabbo().Username + " basura " + BasuraAmount + "!", 1);
                        }
                        else
                            Session.SendWhisper("¡Por favor ingrese un número valido!", 1);
                        break;
                    }
                #endregion


                #region Animo
                case "animo":
                    {
                        string Amount = Params[0].ToLower() == "setanimo" ? Params[2] : Params[3];

                        int AnimoAmount;
                        if (int.TryParse(Amount, out AnimoAmount))
                        {
                            if (AnimoAmount <= 0)
                            {
                                Session.SendWhisper("El monto no puede contener -", 1);
                                return;
                            }
                            TargetClient.GetRoleplay().Animo = AnimoAmount;
                            Session.SendWhisper("Establecido con éxito a: " + TargetClient.GetHabbo().Username + " su animo es: " + AnimoAmount + "!", 1);
                        }
                        else
                            Session.SendWhisper("¡Por favor ingrese un número valido!", 1);
                        break;
                    }
                #endregion

                #region Hygiene
                case "hygiene":
                    {
                        string Amount = Params[0].ToLower() == "sethygiene" ? Params[2] : Params[3];

                        int HygieneAmount;
                        if (int.TryParse(Amount, out HygieneAmount))
                        {
                            if (HygieneAmount <= 0)
                            {
                                Session.SendWhisper("El monto no puede contener -", 1);
                                return;
                            }
                            TargetClient.GetRoleplay().Hygiene = HygieneAmount;
                            Session.SendWhisper("Establecido con éxito a: " + TargetClient.GetHabbo().Username + " su higiene a: " + HygieneAmount + "!", 1);
                        }
                        else
                            Session.SendWhisper("¡Por favor ingrese un número valido!", 1);
                        break;
                    }
                #endregion


                #region Hygiene
                case "chaleco":
                    {
                        string Amount = Params[0].ToLower() == "setchaleco" ? Params[2] : Params[3];

                        int HygieneAmount;
                        if (int.TryParse(Amount, out HygieneAmount))
                        {
                            if (HygieneAmount <= 0)
                            {
                                Session.SendWhisper("El monto no puede contener -", 1);
                                return;
                            }
                            if (TargetClient.GetHabbo().CurrentRoom != null)
                            {
                                var currentRoomUser = TargetClient.GetHabbo().CurrentRoom.GetRoomUserManager()?.GetRoomUserByHabbo(TargetClient.GetHabbo().Id);
                                if (currentRoomUser != null)
                                {
                                    TargetClient.SendMessage(new UserChangeComposer(currentRoomUser, true));
                                    TargetClient.GetHabbo().CurrentRoom.SendMessage(new UserChangeComposer(currentRoomUser, false));
                                }
                            }
                            TargetClient.GetRoleplay().ChalecoPor = HygieneAmount;
                            Session.SendWhisper("Establecido con éxito " + TargetClient.GetHabbo().Username + "'s chaleco" + HygieneAmount + "!", 1);
                        }
                        else
                            Session.SendWhisper("¡Por favor ingrese un número valido!", 1);
                        break;
                    }
                #endregion

                #region Poop
                case "poop":
                    {
                        string Amount = Params[0].ToLower() == "setmierda" ? Params[2] : Params[3];

                        int PoopAmount;
                        if (int.TryParse(Amount, out PoopAmount))
                        {
                            if (PoopAmount <= 0)
                            {
                                Session.SendWhisper("El monto no puede contener -", 1);
                                return;
                            }
                            TargetClient.GetRoleplay().Poop = PoopAmount;
                            Session.SendWhisper("Mierda puesta con exito a " + TargetClient.GetHabbo().Username + " por un valor de: " + PoopAmount + "!", 1);
                        }
                        else
                            Session.SendWhisper("¡Por favor ingrese un número valido!", 1);
                        break;
                    }
                #endregion

                #region Minutes
                case "minutes":
                    {
                        int Amount;
                        if (int.TryParse(Params[3], out Amount))
                        {
                            TargetClient.GetRoleplay().TimeWorked = Amount;
                            Session.SendWhisper("Establecido con éxito " + TargetClient.GetHabbo().Username + "'s timeworked to " + Amount + "!", 1);
                        }
                        else
                            Session.SendWhisper("¡Por favor ingrese un número valido!", 1);
                        break;
                    }
                #endregion

                #region Noob Timer
                case "noobtime":
                case "noobtimer":
                    {
                        int Amount;
                        if (int.TryParse(Params[3], out Amount))
                        {
                            if (Amount <= 0)
                            {
                                TargetClient.GetRoleplay().IsNoob = false;
                                TargetClient.GetRoleplay().NoobTimeLeft = 0;
                            }
                            else
                            {
                                if (!TargetClient.GetRoleplay().IsNoob)
                                    TargetClient.GetRoleplay().IsNoob = true;

                                TargetClient.GetRoleplay().NoobTimeLeft = Amount;

                                if (!TargetClient.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("noob"))
                                    TargetClient.GetRoleplay().TimerManager.CreateTimer("noob", 1000, true);
                            }

                            Session.SendWhisper("Establecido con éxito " + TargetClient.GetHabbo().Username + "'s noob timer to " + Amount + "!", 1);
                        }
                        else
                            Session.SendWhisper("¡Por favor ingrese un número valido!", 1);
                        break;
                    }
                #endregion

                case "list":
                    {
                        Session.SendWhisper("tienes los siguientes datos: maxhealth, maxenergy, health, energy, strength, intelligence, hunger, poop, sida, hygiene, minutes, noobtimer, level, and xp!", 1);
                        break;
                    }
                default:
                    {
                        Session.SendWhisper("No existe, usa ':setstat list' para ver las opciones", 1);
                        break;
                    }
            }
        }
    }
}
