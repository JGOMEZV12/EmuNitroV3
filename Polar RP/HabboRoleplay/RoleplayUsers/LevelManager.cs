using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Threading;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.GameClients;
using Polar.Utilities;

namespace Polar.HabboRoleplay.RoleplayUsers
{
    public static class LevelManager
    {
        #region Levels Dictionary
        public static readonly Dictionary<int, int> Levels = new Dictionary<int, int>
        {
            {1,0},
            {2,2000},
            {3,4000},
            {4,6000},
            {5,8000},
            {6,10000},
            {7,12000},
            {8,15000},
            {9,20000},
            {10,25000},
            {11,30000},
            {12,35000},
            {13,45000},
            {14,50000},
            {15,55000},
            {16,60000},
            {17,65000},
            {18,70000},
            {19,80000},
            {20,150000},
            {21,250000},
            {22,300000},
            {23,320000},
            {24,350000},
            {25,360000},
            {26,400000},
            {27,420000},
            {28,450000},
            {29,460000},
            {30,500000},
            {31,510000},
            {32,515000},
            {33,520000},
            {34,525000},
            {35,530000},
            {36,535000},
            {37,540000},
            {38,545000},
            {39,550000},
            {40,560000},
            {41,565000},
            {42,570000},
            {43,575000},
            {44,580000},
            {45,585000},
            {46,590000},
            {47,595000},
            {48,600000},
            {49,610000},
            {50,615000}

        };
        #endregion

        #region Intelligence Levels Dictionary
        public static readonly Dictionary<int, int> IntelligenceLevels = new Dictionary<int, int>
        {
            {1,10},
            {2,15},
            {3,20},
            {4,25},
            {5,30},
            {6,35},
            {7,40},
            {8,45},
            {9,50},
            {10,55},
            {11,60},
            {12,65},
            {13,70}
        };
        #endregion

        #region Strength Levels Dictionary
        public static readonly Dictionary<int, int> StrengthLevels = new Dictionary<int, int>
        {
            {1,100},
            {2,200},
            {3,300},
            {4,400},
            {5,500},
            {6,600},
            {7,700},
            {8,800},
            {9,900},
            {10,1000},
            {11,1500},
            {12,2000},
            {13,3500}
        };
        #endregion

        #region Stamina Levels Dictionary
        public static readonly Dictionary<int, int> StaminaLevels = new Dictionary<int, int>
        {
            {1,100},
            {2,300},
            {3,500},
            {4,600},
            {5,800},
            {6,1000},
            {7,1200},
            {8,1400},
            {9,1600},
            {10,2000},
            {11,3000},
            {12,4000},
            {13,5000},
            {14,7000},
            {15,9000},
            {16,1200},
            {17,13000},
            {18,14900},
            {19,15900},
            {20,16000},
            {21,50000}
        };
        #endregion

        public static void AddLevelEXP(GameClient Session, int amount)
        {
            try
            {
                Session.GetRoleplay().RefreshStatDialogue();
                amount = Convert.ToInt32(RoleplayData.GetData("level", "modifier")) * amount;

                if (Session != null && Session.GetRoleplay() != null)
                {
                    Session.GetRoleplay().LevelEXP += amount;

                    if (LevelUp(Session, "level"))
                    {
                        Session.GetRoleplay().Level += 1;
                        Session.GetRoleplay().RefreshStatDialogue();

                        if (PolarEnvironment.GetGame().GetCacheManager().ContainsUser(Session.GetHabbo().Id))
                            PolarEnvironment.GetGame().GetCacheManager().TryUpdateUser(Session);

                        // ✅ Guardar en DB al subir de nivel
                        new UserDataHandler(Session, Session.GetRoleplay()).SaveData();

                        Session.SendWhisper("¡Usted acaba de subir de nivel! Usted está ahora Nivel: " + Session.GetRoleplay().Level + ".");
                    }
                    else
                        Session.SendWhisper("Has recibido " + String.Format("{0:N0}", amount) + " ¡Experiencia hacia su próximo nivel! Necesitas " + String.Format("{0:N0}", (Levels[Session.GetRoleplay().Level + 1] - Session.GetRoleplay().LevelEXP)) + " Llegar al nivel " + (Session.GetRoleplay().Level + 1), 1);

                    PolarEnvironment.GetGame().GetBattlePassManager().AwardExp(Session, amount / 10);
                }
            }
            catch { }
        }

        public static void AddIntelligenceEXP(GameClient Session, int amount)
        {
            try
            {
                amount = Convert.ToInt32(RoleplayData.GetData("intelligence", "modifier")) * amount;

                if (Session != null && Session.GetRoleplay() != null)
                {
                    Session.GetRoleplay().IntelligenceEXP += amount;
                    Session.GetRoleplay().Intelligence += 1;

                    if (LevelUp(Session, "intelligence"))
                    {
                        Session.Shout("*Se siente un poco más inteligente [Inteligencia +1]*", 4);
                        Session.GetRoleplay().RefreshStatDialogue();

                        if (PolarEnvironment.GetGame().GetCacheManager().ContainsUser(Session.GetHabbo().Id))
                            PolarEnvironment.GetGame().GetCacheManager().TryUpdateUser(Session);

                        // ✅ Guardar en DB al subir de nivel
                        new UserDataHandler(Session, Session.GetRoleplay()).SaveData();
                    }
                    else
                        Session.SendWhisper("Aprendizaje: " + String.Format("{0:N0}", Session.GetRoleplay().IntelligenceEXP) + "/" + String.Format("{0:N0}", (IntelligenceLevels[Session.GetRoleplay().Intelligence + 1])), 1);

                    PolarEnvironment.GetGame().GetBattlePassManager().ProgressChallenge(Session, "learn_intelligence", 1);
                }
            }
            catch { }
        }

        public static void AddStrengthEXP(GameClient Session, int amount)
        {
            try
            {
                amount = Convert.ToInt32(RoleplayData.GetData("strength", "modifier")) * amount;

                if (Session != null && Session.GetRoleplay() != null)
                {
                    Session.GetRoleplay().StrengthEXP += amount;

                    if (LevelUp(Session, "strength"))
                    {
                        Session.GetRoleplay().Strength += 1;
                        if (Session.GetRoleplay().MaxHealth < 200)
                            Session.GetRoleplay().MaxHealth += 5;

                        Session.Shout("*Se siente un poco más fuerte y saludable [+1 Fuerza] ¡Enhorabuena!*", 4);
                        Session.GetRoleplay().RefreshStatDialogue();

                        if (PolarEnvironment.GetGame().GetCacheManager().ContainsUser(Session.GetHabbo().Id))
                            PolarEnvironment.GetGame().GetCacheManager().TryUpdateUser(Session);

                        // ✅ Guardar en DB al subir de nivel
                        new UserDataHandler(Session, Session.GetRoleplay()).SaveData();
                    }
                    else
                        Session.SendWhisper("Entrenando: " + String.Format("{0:N0}", Session.GetRoleplay().StrengthEXP) + "/" + String.Format("{0:N0}", (StrengthLevels[Session.GetRoleplay().Strength + 1])), 1);

                    PolarEnvironment.GetGame().GetBattlePassManager().ProgressChallenge(Session, "train_strength", 1);
                }
            }
            catch { }
        }

        public static void AddStaminaEXP(GameClient Session, int amount)
        {
            try
            {
                amount = Convert.ToInt32(RoleplayData.GetData("stamina", "modifier")) * amount;

                if (Session != null && Session.GetRoleplay() != null)
                {
                    Session.GetRoleplay().StaminaEXP += amount;

                    if (LevelUp(Session, "stamina"))
                    {
                        Session.GetRoleplay().Stamina += 1;
                        if (Session.GetRoleplay().MaxEnergy < 200)
                            Session.GetRoleplay().MaxEnergy += 5;

                        Session.Shout("*Se siente un poco más enérgico [+1 Stamina]*", 4);
                        Session.GetRoleplay().RefreshStatDialogue();

                        if (PolarEnvironment.GetGame().GetCacheManager().ContainsUser(Session.GetHabbo().Id))
                            PolarEnvironment.GetGame().GetCacheManager().TryUpdateUser(Session);

                        // ✅ Guardar en DB al subir de nivel
                        new UserDataHandler(Session, Session.GetRoleplay()).SaveData();
                    }
                    else
                        Session.SendWhisper("Entrenando: " + String.Format("{0:N0}", Session.GetRoleplay().StaminaEXP) + "/" + String.Format("{0:N0}", (StaminaLevels[Session.GetRoleplay().Stamina + 1])), 1);

                    PolarEnvironment.GetGame().GetBattlePassManager().ProgressChallenge(Session, "train_stamina", 1);
                }
            }
            catch { }
        }

        public static bool LevelUp(GameClient Session, string Type)
        {
            try
            {
                if (Session != null && Session.GetRoleplay() != null)
                {
                    int Level = 1;
                    int EXP = 0;
                    Dictionary<int, int> Dictionary = null;

                    switch (Type.ToLower())
                    {
                        case "level":
                            {
                                Level = Session.GetRoleplay().Level;
                                EXP = Session.GetRoleplay().LevelEXP;
                                Dictionary = Levels;
                                break;
                            }
                        case "intelligence":
                            {
                                Level = Session.GetRoleplay().Intelligence;
                                EXP = Session.GetRoleplay().IntelligenceEXP;
                                Dictionary = IntelligenceLevels;
                                break;
                            }
                        case "strength":
                            {
                                Level = Session.GetRoleplay().Strength;
                                EXP = Session.GetRoleplay().StrengthEXP;
                                Dictionary = StrengthLevels;
                                break;
                            }
                        case "stamina":
                            {
                                Level = Session.GetRoleplay().Stamina;
                                EXP = Session.GetRoleplay().StaminaEXP;
                                Dictionary = StaminaLevels;
                                break;
                            }
                    }

                    if (Dictionary == null)
                        return false;

                    if (Dictionary.ContainsKey(Level + 1))
                    {
                        if (EXP >= Dictionary[Level + 1])
                            return true;
                    }
                    return false;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static int IntelligenceChance(GameClient Session)
        {
            if (Session == null || Session.GetRoleplay() == null || Session.GetHabbo() == null)
                return 0;

            CryptoRandom Random = new CryptoRandom();
            int Intelligence = Session.GetRoleplay().Intelligence;
            int Multiplier = Random.Next(2, 5);

            if (Intelligence < 0)
                Intelligence = 0;

            if (Intelligence > RoleplayManager.IntelligenceCap)
                Intelligence = RoleplayManager.IntelligenceCap;

            return Intelligence * Multiplier;
        }
    }
}
