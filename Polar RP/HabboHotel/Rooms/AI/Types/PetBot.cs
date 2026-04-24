using Polar.Core;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Pathfinding;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.AI;
using Polar.HabboHotel.Rooms.Pathfinding;
using Polar.HabboRoleplay.Bots;
using Polar.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Polar.HabboHotel.Rooms.AI.Types
{
    public class PetBot : BotAI
    {
        private int ActionTimer;
        private int EnergyTimer;
        private int SpeechTimer;

        public PetBot(int VirtualId)
        {
            SpeechTimer = new Random((VirtualId ^ 2) + DateTime.Now.Millisecond).Next(10, 60);
            ActionTimer = new Random((VirtualId ^ 2) + DateTime.Now.Millisecond).Next(10, 30 + VirtualId);
            EnergyTimer = new Random((VirtualId ^ 2) + DateTime.Now.Millisecond).Next(10, 60);
        }

        private void RemovePetStatus()
        {
            RoomUser Pet = GetRoomUser();
            if (Pet != null)
            {
                foreach (KeyValuePair<string, string> KVP in Pet.Statusses.ToList())
                {
                    if (Pet.Statusses.ContainsKey(KVP.Key))
                        Pet.Statusses.Remove(KVP.Key);
                }
            }
        }

        public override void OnSelfEnterRoom()
        {
            Point nextCoord = GetRoom().GetGameMap().GetRandomWalkableSquare();
            if (GetRoomUser() != null)
                GetRoomUser().MoveTo(nextCoord.X, nextCoord.Y);
        }

        public override void OnSelfLeaveRoom(bool Kicked)
        {
        }

        public override void OnUserEnterRoom(RoomUser User)
        {
            if (User.GetClient() != null && User.GetClient().GetHabbo() != null)
            {
                RoomUser Pet = GetRoomUser();
                if (Pet != null && Pet.PetData != null)
                {
                    if (User.GetClient().GetHabbo().Username == Pet.PetData.OwnerName)
                    {
                        string[] Speech = PolarEnvironment.GetGame().GetChatManager().GetPetLocale().GetValue("welcome.speech.pet" + Pet.PetData.Type);
                        if (Speech != null && Speech.Length > 0)
                        {
                            string rSpeech = Speech[RandomNumber.GenerateRandom(0, Speech.Length - 1)];
                            Pet.Chat(rSpeech, false);
                        }
                    }
                }
            }
        }

        public override void OnUserLeaveRoom(GameClient Client)
        {
        }

        public override void OnUserShout(RoomUser User, string Message)
        {
        }

        public override void OnTimerTick()
        {
            RoomUser Pet = GetRoomUser();
            if (Pet == null || Pet.PetData == null)
                return;

            #region Speech

            if (SpeechTimer <= 0)
            {
                if (Pet.PetData.DbState != PetDatabaseUpdateState.NeedsInsert)
                    Pet.PetData.DbState = PetDatabaseUpdateState.NeedsUpdate;

                RemovePetStatus();

                string[] Speech = PolarEnvironment.GetGame().GetChatManager().GetPetLocale().GetValue("speech.pet" + Pet.PetData.Type);
                if (Speech != null && Speech.Length > 0)
                {
                    string rSpeech = Speech[RandomNumber.GenerateRandom(0, Speech.Length - 1)];

                    if (Pet.GetBotRoleplay() == null || Pet.GetBotRoleplay().AIType != RoleplayBotAIType.PET)
                    {
                        if (rSpeech.Length != 3)
                            Pet.Chat(rSpeech, false);
                        else
                            Pet.Statusses.Add(rSpeech, TextHandling.GetString(Pet.Z));
                    }
                }
                SpeechTimer = PolarEnvironment.GetRandomNumber(20, 120);
            }
            else
            {
                SpeechTimer--;
            }

            #endregion

            #region Actions

            if (ActionTimer <= 0)
            {
                try
                {
                    RemovePetStatus();
                    ActionTimer = RandomNumber.GenerateRandom(15, 40 + Pet.PetData.VirtualId);
                    if (!Pet.RidingHorse)
                    {
                        RemovePetStatus();
                        if (GetRoom() != null && GetRoom().GetGameMap() != null)
                        {
                            Point nextCoord = GetRoom().GetGameMap().GetRandomWalkableSquare();
                            if (Pet.CanWalk)
                                Pet.MoveTo(nextCoord.X, nextCoord.Y);
                        }
                    }
                }
                catch (Exception e)
                {
                    Logging.HandleException(e, "PetBot.OnTimerTick");
                }
            }
            else
            {
                ActionTimer--;
            }

            #endregion

            #region Energy

            if (EnergyTimer <= 0)
            {
                RemovePetStatus();
                Pet.PetData.PetEnergy(true);
                EnergyTimer = RandomNumber.GenerateRandom(30, 120);
            }
            else
            {
                EnergyTimer--;
            }

            #endregion
        }

        #region Commands

        public override void OnUserSay(RoomUser User, string Message)
        {
            if (User == null)
                return;

            RoomUser Pet = GetRoomUser();
            if (Pet == null || Pet.PetData == null)
                return;

            if (Pet.PetData.DbState != PetDatabaseUpdateState.NeedsInsert)
                Pet.PetData.DbState = PetDatabaseUpdateState.NeedsUpdate;

            if (Message.ToLower().Equals(Pet.PetData.Name.ToLower()))
            {
                Pet.SetRot(Rotation.Calculate(Pet.X, Pet.Y, User.X, User.Y), false);
                return;
            }

            if ((Message.ToLower().StartsWith(Pet.PetData.Name.ToLower() + " ") && User.GetClient().GetHabbo().Username.ToLower() == Pet.PetData.OwnerName.ToLower()) ||
                (Message.ToLower().StartsWith(Pet.PetData.Name.ToLower() + " ") && PolarEnvironment.GetGame().GetChatManager().GetPetCommands().TryInvoke(Message.Substring(Pet.PetData.Name.ToLower().Length + 1)) == 8))
            {
                string Command = Message.Substring(Pet.PetData.Name.ToLower().Length + 1);

                int r = RandomNumber.GenerateRandom(1, 8);
                if (Pet.PetData.Energy > 10 && r < 6 || Pet.PetData.Level > 15 || PolarEnvironment.GetGame().GetChatManager().GetPetCommands().TryInvoke(Command) == 8)
                {
                    RemovePetStatus();

                    switch (PolarEnvironment.GetGame().GetChatManager().GetPetCommands().TryInvoke(Command))
                    {
                        #region free (0)
                        case 0:
                            RemovePetStatus();
                            Point nextCoord = GetRoom().GetGameMap().GetRandomWalkableSquare();
                            Pet.MoveTo(nextCoord.X, nextCoord.Y);
                            Pet.PetData.AddExperience(10);
                            break;
                        #endregion

                        #region sit (1)
                        case 1:
                            RemovePetStatus();
                            Pet.PetData.AddExperience(10);
                            Pet.Statusses.Add("sit", TextHandling.GetString(Pet.Z));
                            Pet.UpdateNeeded = true;
                            ActionTimer = 25;
                            EnergyTimer = 10;
                            break;
                        #endregion

                        #region down (2)
                        case 2:
                            RemovePetStatus();
                            Pet.Statusses.Add("lay", TextHandling.GetString(Pet.Z));
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(10);
                            ActionTimer = 30;
                            EnergyTimer = 5;
                            break;
                        #endregion

                        #region here (3)
                        case 3:
                            RemovePetStatus();
                            int NewX = User.X;
                            int NewY = User.Y;
                            ActionTimer = 30;
                            if (User.RotBody == 4) NewY = User.Y + 1;
                            else if (User.RotBody == 0) NewY = User.Y - 1;
                            else if (User.RotBody == 6) NewX = User.X - 1;
                            else if (User.RotBody == 2) NewX = User.X + 1;
                            else if (User.RotBody == 3) { NewX = User.X + 1; NewY = User.Y + 1; }
                            else if (User.RotBody == 1) { NewX = User.X + 1; NewY = User.Y - 1; }
                            else if (User.RotBody == 7) { NewX = User.X - 1; NewY = User.Y - 1; }
                            else if (User.RotBody == 5) { NewX = User.X - 1; NewY = User.Y + 1; }
                            Pet.PetData.AddExperience(10);
                            Pet.MoveTo(NewX, NewY);
                            break;
                        #endregion

                        #region beg (4)
                        case 4:
                            RemovePetStatus();
                            Pet.Statusses.Add("beg", TextHandling.GetString(Pet.Z));
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(10);
                            ActionTimer = 20;
                            EnergyTimer = 5;
                            break;
                        #endregion

                        #region play dead (5)
                        case 5:
                            RemovePetStatus();
                            Pet.Statusses.Add("ded", TextHandling.GetString(Pet.Z));
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(10);
                            SpeechTimer = 45;
                            ActionTimer = 30;
                            break;
                        #endregion

                        #region stay (6)
                        case 6:
                            RemovePetStatus();
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(10);
                            ActionTimer = 45;
                            EnergyTimer = 3;
                            SpeechTimer = 20;
                            break;
                        #endregion

                        #region follow (7)
                        case 7:
                            RemovePetStatus();
                            Pet.PetData.AddExperience(10);
                            Pet.MoveTo(User.X, User.Y);
                            ActionTimer = 10;
                            break;
                        #endregion

                        #region stand (8)
                        case 8:
                            RemovePetStatus();
                            Pet.PetData.AddExperience(10);
                            Pet.UpdateNeeded = true;
                            ActionTimer = 15;
                            break;
                        #endregion

                        #region jump (9)
                        case 9:
                            RemovePetStatus();
                            Pet.Statusses.Add("jmp", TextHandling.GetString(Pet.Z));
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(10);
                            EnergyTimer = 5;
                            SpeechTimer = 10;
                            ActionTimer = 5;
                            break;
                        #endregion

                        #region speak (10)
                        case 10:
                            RemovePetStatus();
                            Pet.Statusses.Add("spk", TextHandling.GetString(Pet.Z));
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(10);
                            SpeechTimer = 5;
                            ActionTimer = 10;
                            break;
                        #endregion

                        #region play (11)
                        case 11:
                            RemovePetStatus();
                            Pet.Statusses.Add("pla", TextHandling.GetString(Pet.Z));
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(10);
                            ActionTimer = 20;
                            EnergyTimer = 8;
                            break;
                        #endregion

                        #region silent (12)
                        case 12:
                            RemovePetStatus();
                            Pet.Statusses.Remove("spk");
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(10);
                            SpeechTimer = 60;
                            ActionTimer = 10;
                            break;
                        #endregion

                        #region nest (13)
                        case 13:
                            RemovePetStatus();
                            Pet.Chat("ZzzZZZzzzzZzz", false);
                            Pet.Statusses.Add("lay", TextHandling.GetString(Pet.Z));
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(10);
                            EnergyTimer = 5;
                            SpeechTimer = 30;
                            ActionTimer = 45;
                            break;
                        #endregion

                        #region drink (14)
                        case 14:
                            RemovePetStatus();
                            Pet.Statusses.Add("eat", TextHandling.GetString(Pet.Z));
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(10);
                            ActionTimer = 15;
                            EnergyTimer = 5;
                            break;
                        #endregion

                        #region follow left (15)
                        case 15:
                            RemovePetStatus();
                            Pet.RotBody = (Pet.RotBody - 1 < 0 ? 7 : Pet.RotBody - 1);
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(5);
                            ActionTimer = 5;
                            break;
                        #endregion

                        #region follow right (16)
                        case 16:
                            RemovePetStatus();
                            Pet.RotBody = (Pet.RotBody + 1 > 7 ? 0 : Pet.RotBody + 1);
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(5);
                            ActionTimer = 5;
                            break;
                        #endregion

                        #region play football (17)
                        case 17:
                            RemovePetStatus();
                            Pet.PetData.AddExperience(10);
                            Pet.UpdateNeeded = true;
                            ActionTimer = 20;
                            EnergyTimer = 8;
                            break;
                        #endregion

                        #region come here (18)
                        case 18:
                            RemovePetStatus();
                            Pet.PetData.AddExperience(10);
                            Pet.MoveTo(User.X, User.Y);
                            ActionTimer = 10;
                            break;
                        #endregion

                        #region bounce (19)
                        case 19:
                            RemovePetStatus();
                            Pet.Statusses.Add("jmp", TextHandling.GetString(Pet.Z));
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(10);
                            EnergyTimer = 5;
                            ActionTimer = 8;
                            break;
                        #endregion

                        #region flat (20)
                        case 20:
                            RemovePetStatus();
                            Pet.Statusses.Add("flt", TextHandling.GetString(Pet.Z));
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(10);
                            ActionTimer = 20;
                            EnergyTimer = 5;
                            break;
                        #endregion

                        #region dance (21)
                        case 21:
                            RemovePetStatus();
                            Pet.Statusses.Add("dan", TextHandling.GetString(Pet.Z));
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(10);
                            ActionTimer = 20;
                            EnergyTimer = 8;
                            break;
                        #endregion

                        #region spin (22)
                        case 22:
                            RemovePetStatus();
                            Pet.Statusses.Add("spn", TextHandling.GetString(Pet.Z));
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(10);
                            ActionTimer = 10;
                            EnergyTimer = 5;
                            break;
                        #endregion

                        #region switch tv (23)
                        case 23:
                            RemovePetStatus();
                            Pet.PetData.AddExperience(10);
                            Pet.UpdateNeeded = true;
                            ActionTimer = 10;
                            break;
                        #endregion

                        #region move forward (24)
                        case 24:
                            RemovePetStatus();
                            Pet.PetData.AddExperience(10);
                            Pet.MoveTo(User.X, User.Y);
                            ActionTimer = 10;
                            break;
                        #endregion

                        #region turn left (25)
                        case 25:
                            RemovePetStatus();
                            Pet.RotBody = (Pet.RotBody - 1 < 0 ? 7 : Pet.RotBody - 1);
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(5);
                            ActionTimer = 5;
                            break;
                        #endregion

                        #region turn right (26)
                        case 26:
                            RemovePetStatus();
                            Pet.RotBody = (Pet.RotBody + 1 > 7 ? 0 : Pet.RotBody + 1);
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(5);
                            ActionTimer = 5;
                            break;
                        #endregion

                        #region relax (27)
                        case 27:
                            RemovePetStatus();
                            Pet.Statusses.Add("rlx", TextHandling.GetString(Pet.Z));
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(10);
                            ActionTimer = 30;
                            EnergyTimer = 5;
                            SpeechTimer = 15;
                            break;
                        #endregion

                        #region croak (28)
                        case 28:
                            RemovePetStatus();
                            Pet.Statusses.Add("croak", TextHandling.GetString(Pet.Z));
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(10);
                            ActionTimer = 10;
                            SpeechTimer = 5;
                            break;
                        #endregion

                        #region dip (29)
                        case 29:
                            RemovePetStatus();
                            Pet.PetData.AddExperience(10);
                            Pet.MoveTo(User.X, User.Y);
                            ActionTimer = 15;
                            break;
                        #endregion

                        #region wave (30)
                        case 30:
                            RemovePetStatus();
                            Pet.Statusses.Add("wav", TextHandling.GetString(Pet.Z));
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(10);
                            ActionTimer = 10;
                            break;
                        #endregion

                        #region mambo (31)
                        case 31:
                            RemovePetStatus();
                            Pet.Statusses.Add("dan", TextHandling.GetString(Pet.Z));
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(10);
                            ActionTimer = 25;
                            EnergyTimer = 8;
                            break;
                        #endregion

                        #region high jump (32)
                        case 32:
                            RemovePetStatus();
                            Pet.Statusses.Add("jmp", TextHandling.GetString(Pet.Z));
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(15);
                            EnergyTimer = 8;
                            SpeechTimer = 10;
                            ActionTimer = 10;
                            break;
                        #endregion

                        #region chicken dance (33)
                        case 33:
                            RemovePetStatus();
                            Pet.Statusses.Add("dan", TextHandling.GetString(Pet.Z));
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(10);
                            ActionTimer = 20;
                            EnergyTimer = 8;
                            break;
                        #endregion

                        #region triple jump (34)
                        case 34:
                            RemovePetStatus();
                            Pet.Statusses.Add("jmp", TextHandling.GetString(Pet.Z));
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(15);
                            EnergyTimer = 8;
                            SpeechTimer = 10;
                            ActionTimer = 10;
                            break;
                        #endregion

                        #region spread wings (35)
                        case 35:
                            RemovePetStatus();
                            Pet.Statusses.Add("wings", TextHandling.GetString(Pet.Z));
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(10);
                            ActionTimer = 15;
                            break;
                        #endregion

                        #region breathe fire (36)
                        case 36:
                            RemovePetStatus();
                            Pet.Statusses.Add("flame", TextHandling.GetString(Pet.Z));
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(10);
                            ActionTimer = 10;
                            break;
                        #endregion

                        #region hang (37)
                        case 37:
                            RemovePetStatus();
                            Pet.Statusses.Add("hang", TextHandling.GetString(Pet.Z));
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(10);
                            ActionTimer = 20;
                            EnergyTimer = 5;
                            break;
                        #endregion

                        #region torch (38)
                        case 38:
                            RemovePetStatus();
                            Pet.Statusses.Add("eat", TextHandling.GetString(Pet.Z));
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(10);
                            ActionTimer = 10;
                            break;
                        #endregion

                        #region swing (40)
                        case 40:
                            RemovePetStatus();
                            Pet.Statusses.Add("swg", TextHandling.GetString(Pet.Z));
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(10);
                            ActionTimer = 20;
                            EnergyTimer = 8;
                            break;
                        #endregion

                        #region roll (41)
                        case 41:
                            RemovePetStatus();
                            Pet.Statusses.Add("lay", TextHandling.GetString(Pet.Z));
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(10);
                            ActionTimer = 15;
                            EnergyTimer = 5;
                            break;
                        #endregion

                        #region ring of fire (42)
                        case 42:
                            RemovePetStatus();
                            Pet.Statusses.Add("eat", TextHandling.GetString(Pet.Z));
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(20);
                            ActionTimer = 15;
                            EnergyTimer = 10;
                            break;
                        #endregion

                        #region eat (43)
                        case 43:
                            RemovePetStatus();
                            Pet.Statusses.Add("eat", TextHandling.GetString(Pet.Z));
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(10);
                            ActionTimer = 15;
                            EnergyTimer = 5;
                            break;
                        #endregion

                        #region wag tail (44)
                        case 44:
                            RemovePetStatus();
                            Pet.Statusses.Add("wag", TextHandling.GetString(Pet.Z));
                            Pet.UpdateNeeded = true;
                            Pet.PetData.AddExperience(10);
                            ActionTimer = 10;
                            EnergyTimer = 5;
                            break;
                        #endregion

                        #region count (45)
                        case 45:
                            RemovePetStatus();
                            Pet.PetData.AddExperience(10);
                            Pet.UpdateNeeded = true;
                            ActionTimer = 15;
                            SpeechTimer = 5;
                            break;
                        #endregion

                        #region breed (46)
                        case 46:
                            // Breed logic handled externally via breeding nest interaction
                            break;
                        #endregion

                        default:
                            string[] Speech = PolarEnvironment.GetGame().GetChatManager().GetPetLocale().GetValue("pet.unknowncommand");
                            if (Speech != null && Speech.Length > 0)
                                Pet.Chat(Speech[RandomNumber.GenerateRandom(0, Speech.Length - 1)], false);
                            break;
                    }

                    Pet.PetData.PetEnergy(false);
                }
                else
                {
                    RemovePetStatus();

                    if (Pet.PetData.Energy < 10)
                    {
                        RoomUser UserRiding = GetRoom().GetRoomUserManager().GetRoomUserByVirtualId(Pet.HorseID);

                        if (UserRiding != null && UserRiding.RidingHorse)
                        {
                            Pet.Chat("Getof my sit", false);
                            UserRiding.RidingHorse = false;
                            Pet.RidingHorse = false;
                            UserRiding.ApplyEffect(-1);
                            UserRiding.MoveTo(new Point(GetRoomUser().X + 1, GetRoomUser().Y + 1));
                        }

                        string[] TiredSpeech = PolarEnvironment.GetGame().GetChatManager().GetPetLocale().GetValue("pet.tired");
                        if (TiredSpeech != null && TiredSpeech.Length > 0)
                            Pet.Chat(TiredSpeech[RandomNumber.GenerateRandom(0, TiredSpeech.Length - 1)], false);

                        Pet.Statusses.Add("lay", TextHandling.GetString(Pet.Z));
                        Pet.UpdateNeeded = true;

                        SpeechTimer = 50;
                        ActionTimer = 45;
                        EnergyTimer = 5;
                    }
                    else
                    {
                        string[] LazySpeech = PolarEnvironment.GetGame().GetChatManager().GetPetLocale().GetValue("pet.lazy");
                        if (LazySpeech != null && LazySpeech.Length > 0)
                            Pet.Chat(LazySpeech[RandomNumber.GenerateRandom(0, LazySpeech.Length - 1)], false);

                        Pet.PetData.PetEnergy(false);
                    }
                }
            }
        }

        #endregion
    }
}
