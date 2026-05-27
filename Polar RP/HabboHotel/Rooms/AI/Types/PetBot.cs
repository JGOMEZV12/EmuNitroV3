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
        // FIX: usar Random.Shared (thread-safe, .NET 6+) en vez de new Random() por instancia.
        //      new Random() con el mismo seed en el mismo milisegundo produce la misma secuencia.
        private static readonly Random _rng = Random.Shared;

        private int ActionTimer;
        private int EnergyTimer;
        private int SpeechTimer;

        public PetBot(int VirtualId)
        {
            SpeechTimer = _rng.Next(10, 60);
            ActionTimer = _rng.Next(10, 30 + VirtualId);
            EnergyTimer = _rng.Next(10, 60);
        }

        // FIX: el crash original venía de aquí.
        //   1. Pet.Statusses podía ser null si RoomUser estaba en Dispose().
        //   2. El foreach con ToList() + ContainsKey + Remove es innecesario:
        //      Dictionary.Clear() hace lo mismo en O(1).
        //   3. Ahora con guard nulo antes de cualquier acceso.
        private void RemovePetStatus()
        {
            RoomUser pet = GetRoomUser();
            if (pet?.Statusses == null) return;
            pet.Statusses.Clear();
            pet.UpdateNeeded = true;
        }

        // ── Helper centralizado: obtener Pet con todos los guards ────────────────
        // FIX: antes cada método repetía "Pet == null || Pet.PetData == null".
        //      Centralizado aquí para no olvidar ningún guard.
        private RoomUser GetValidPet()
        {
            RoomUser pet = GetRoomUser();
            if (pet == null || pet.PetData == null || pet.IsDispose)
                return null;
            return pet;
        }

        // ── Helper: obtener sala con guard ───────────────────────────────────────
        private Room GetValidRoom()
        {
            Room room = GetRoom();
            return (room == null || room.mDisposed) ? null : room;
        }

        public override void OnSelfEnterRoom()
        {
            Room room = GetValidRoom();
            RoomUser pet = GetRoomUser();
            if (room == null || pet == null) return;

            Point nextCoord = room.GetGameMap().GetRandomWalkableSquare();
            pet.MoveTo(nextCoord.X, nextCoord.Y);
        }

        public override void OnSelfLeaveRoom(bool Kicked) { }

        public override void OnUserEnterRoom(RoomUser User)
        {
            if (User?.GetClient()?.GetHabbo() == null) return;

            RoomUser pet = GetValidPet();
            if (pet == null) return;

            if (User.GetClient().GetHabbo().Username == pet.PetData.OwnerName)
            {
                string[] speech = PolarEnvironment.GetGame().GetChatManager()
                    .GetPetLocale().GetValue("welcome.speech.pet" + pet.PetData.Type);

                if (speech != null && speech.Length > 0)
                    pet.Chat(speech[RandomNumber.GenerateRandom(0, speech.Length - 1)], false);
            }
        }

        public override void OnUserLeaveRoom(GameClient Client) { }

        public override void OnUserShout(RoomUser User, string Message) { }

        public override void OnTimerTick()
        {
            // FIX: guard único al principio — si Pet o PetData son null, salimos.
            //      Antes el código llegaba a Pet.PetData.DbState antes de comprobar.
            RoomUser pet = GetValidPet();
            if (pet == null) return;

            Room room = GetValidRoom();
            if (room == null) return;

            // ── Speech ────────────────────────────────────────────────────────────
            if (SpeechTimer <= 0)
            {
                if (pet.PetData.DbState != PetDatabaseUpdateState.NeedsInsert)
                    pet.PetData.DbState = PetDatabaseUpdateState.NeedsUpdate;

                RemovePetStatus();

                string[] speech = PolarEnvironment.GetGame().GetChatManager()
                    .GetPetLocale().GetValue("speech.pet" + pet.PetData.Type);

                if (speech != null && speech.Length > 0)
                {
                    string line = speech[RandomNumber.GenerateRandom(0, speech.Length - 1)];

                    if (pet.GetBotRoleplay() == null ||
                        pet.GetBotRoleplay().AIType != RoleplayBotAIType.PET)
                    {
                        if (line.Length != 3)
                            pet.Chat(line, false);
                        else if (pet.Statusses != null)
                            pet.Statusses[line] = TextHandling.GetString(pet.Z);
                    }
                }

                SpeechTimer = PolarEnvironment.GetRandomNumber(20, 120);
            }
            else
            {
                SpeechTimer--;
            }

            // ── Actions ───────────────────────────────────────────────────────────
            if (ActionTimer <= 0)
            {
                try
                {
                    RemovePetStatus();
                    ActionTimer = RandomNumber.GenerateRandom(15, 40 + pet.PetData.VirtualId);

                    if (!pet.RidingHorse && room.GetGameMap() != null)
                    {
                        Point nextCoord = room.GetGameMap().GetRandomWalkableSquare();
                        if (pet.CanWalk)
                            pet.MoveTo(nextCoord.X, nextCoord.Y);
                    }
                }
                catch (Exception e)
                {
                    Logging.HandleException(e, "PetBot.OnTimerTick.Actions");
                }
            }
            else
            {
                ActionTimer--;
            }

            // ── Energy ────────────────────────────────────────────────────────────
            if (EnergyTimer <= 0)
            {
                RemovePetStatus();
                pet.PetData.PetEnergy(true);
                EnergyTimer = RandomNumber.GenerateRandom(30, 120);
            }
            else
            {
                EnergyTimer--;
            }
        }

        // ── Commands ──────────────────────────────────────────────────────────────
        public override void OnUserSay(RoomUser User, string Message)
        {
            if (User == null || Message == null) return;

            RoomUser pet = GetValidPet();
            if (pet == null) return;

            Room room = GetValidRoom();
            if (room == null) return;

            if (pet.PetData.DbState != PetDatabaseUpdateState.NeedsInsert)
                pet.PetData.DbState = PetDatabaseUpdateState.NeedsUpdate;

            string msgLower = Message.ToLower();
            string nameLower = pet.PetData.Name.ToLower();
            string prefix = nameLower + " ";

            // Solo mirar a quien lo llama por su nombre
            if (msgLower.Equals(nameLower))
            {
                pet.SetRot(Rotation.Calculate(pet.X, pet.Y, User.X, User.Y), false);
                return;
            }

            // FIX: guard BEFORE either Substring call.
            // If the message doesn't start with "name " there's nothing to parse.
            if (!msgLower.StartsWith(prefix))
                return;

            // FIX: compute command string ONCE, safely, after the guard above.
            // Use Message (not msgLower) to preserve original casing for the command.
            int commandStart = prefix.Length;
            if (commandStart >= Message.Length)
                return; // message was exactly "name " with nothing after it

            string command = Message.Substring(commandStart);

            bool isOwner = User.GetClient()?.GetHabbo()?.Username?.ToLower()
                           == pet.PetData.OwnerName.ToLower();

            // FIX: TryInvoke called ONCE on the already-safe command string.
            int cmdId = PolarEnvironment.GetGame().GetChatManager()
                            .GetPetCommands().TryInvoke(command);

            bool isPublic = cmdId == 8;

            if (!isOwner && !isPublic)
                return;

            int roll = RandomNumber.GenerateRandom(1, 8);
            bool willObey = pet.PetData.Energy > 10 && roll < 6
                         || pet.PetData.Level > 15
                         || cmdId == 8;

            if (!willObey)
            {
                HandleLazyOrTiredPet(pet, room);
                return;
            }

            RemovePetStatus();
            ExecuteCommand(pet, User, room, cmdId);
            pet.PetData.PetEnergy(false);
        }

        // ── Command dispatch ──────────────────────────────────────────────────────
        private void ExecuteCommand(RoomUser pet, RoomUser user, Room room, int cmdId)
        {
            // FIX: helper local para añadir status de forma segura
            void AddStatus(string key)
            {
                if (pet.Statusses != null)
                    pet.Statusses[key] = TextHandling.GetString(pet.Z);
                pet.UpdateNeeded = true;
            }

            switch (cmdId)
            {
                case 0: // free
                    { var c = room.GetGameMap().GetRandomWalkableSquare(); pet.MoveTo(c.X, c.Y); pet.PetData.AddExperience(10); break; }

                case 1: // sit
                    AddStatus("sit"); pet.PetData.AddExperience(10); ActionTimer = 25; EnergyTimer = 10; break;

                case 2: // down/lay
                    AddStatus("lay"); pet.PetData.AddExperience(10); ActionTimer = 30; EnergyTimer = 5; break;

                case 3: // here
                    {
                        int nx = user.X, ny = user.Y;
                        switch (user.RotBody)
                        {
                            case 0: ny--; break;
                            case 2: nx++; break;
                            case 4: ny++; break;
                            case 6: nx--; break;
                            case 1: nx++; ny--; break;
                            case 3: nx++; ny++; break;
                            case 5: nx--; ny++; break;
                            case 7: nx--; ny--; break;
                        }
                        pet.PetData.AddExperience(10); pet.MoveTo(nx, ny); ActionTimer = 30;
                        break;
                    }

                case 4: AddStatus("beg"); pet.PetData.AddExperience(10); ActionTimer = 20; EnergyTimer = 5; break;
                case 5: AddStatus("ded"); pet.PetData.AddExperience(10); SpeechTimer = 45; ActionTimer = 30; break;
                case 6: pet.UpdateNeeded = true; pet.PetData.AddExperience(10); ActionTimer = 45; EnergyTimer = 3; SpeechTimer = 20; break;
                case 7: pet.MoveTo(user.X, user.Y); pet.PetData.AddExperience(10); ActionTimer = 10; break;
                case 8: pet.UpdateNeeded = true; pet.PetData.AddExperience(10); ActionTimer = 15; break;
                case 9: AddStatus("jmp"); pet.PetData.AddExperience(10); EnergyTimer = 5; SpeechTimer = 10; ActionTimer = 5; break;
                case 10: AddStatus("spk"); pet.PetData.AddExperience(10); SpeechTimer = 5; ActionTimer = 10; break;
                case 11: AddStatus("pla"); pet.PetData.AddExperience(10); ActionTimer = 20; EnergyTimer = 8; break;
                case 12:
                    pet.Statusses?.Remove("spk"); pet.UpdateNeeded = true;
                    pet.PetData.AddExperience(10); SpeechTimer = 60; ActionTimer = 10; break;
                case 13: pet.Chat("ZzzZZZzzzzZzz", false); AddStatus("lay"); pet.PetData.AddExperience(10); EnergyTimer = 5; SpeechTimer = 30; ActionTimer = 45; break;
                case 14: AddStatus("eat"); pet.PetData.AddExperience(10); ActionTimer = 15; EnergyTimer = 5; break;
                case 15: pet.RotBody = (pet.RotBody - 1 < 0 ? 7 : pet.RotBody - 1); pet.UpdateNeeded = true; pet.PetData.AddExperience(5); ActionTimer = 5; break;
                case 16: pet.RotBody = (pet.RotBody + 1 > 7 ? 0 : pet.RotBody + 1); pet.UpdateNeeded = true; pet.PetData.AddExperience(5); ActionTimer = 5; break;
                case 17: pet.UpdateNeeded = true; pet.PetData.AddExperience(10); ActionTimer = 20; EnergyTimer = 8; break;
                case 18: pet.MoveTo(user.X, user.Y); pet.PetData.AddExperience(10); ActionTimer = 10; break;
                case 19: AddStatus("jmp"); pet.PetData.AddExperience(10); EnergyTimer = 5; ActionTimer = 8; break;
                case 20: AddStatus("flt"); pet.PetData.AddExperience(10); ActionTimer = 20; EnergyTimer = 5; break;
                case 21: AddStatus("dan"); pet.PetData.AddExperience(10); ActionTimer = 20; EnergyTimer = 8; break;
                case 22: AddStatus("spn"); pet.PetData.AddExperience(10); ActionTimer = 10; EnergyTimer = 5; break;
                case 23: pet.UpdateNeeded = true; pet.PetData.AddExperience(10); ActionTimer = 10; break;
                case 24: pet.MoveTo(user.X, user.Y); pet.PetData.AddExperience(10); ActionTimer = 10; break;
                case 25: pet.RotBody = (pet.RotBody - 1 < 0 ? 7 : pet.RotBody - 1); pet.UpdateNeeded = true; pet.PetData.AddExperience(5); ActionTimer = 5; break;
                case 26: pet.RotBody = (pet.RotBody + 1 > 7 ? 0 : pet.RotBody + 1); pet.UpdateNeeded = true; pet.PetData.AddExperience(5); ActionTimer = 5; break;
                case 27: AddStatus("rlx"); pet.PetData.AddExperience(10); ActionTimer = 30; EnergyTimer = 5; SpeechTimer = 15; break;
                case 28: AddStatus("croak"); pet.PetData.AddExperience(10); ActionTimer = 10; SpeechTimer = 5; break;
                case 29: pet.MoveTo(user.X, user.Y); pet.PetData.AddExperience(10); ActionTimer = 15; break;
                case 30: AddStatus("wav"); pet.PetData.AddExperience(10); ActionTimer = 10; break;
                case 31: AddStatus("dan"); pet.PetData.AddExperience(10); ActionTimer = 25; EnergyTimer = 8; break;
                case 32: AddStatus("jmp"); pet.PetData.AddExperience(15); EnergyTimer = 8; SpeechTimer = 10; ActionTimer = 10; break;
                case 33: AddStatus("dan"); pet.PetData.AddExperience(10); ActionTimer = 20; EnergyTimer = 8; break;
                case 34: AddStatus("jmp"); pet.PetData.AddExperience(15); EnergyTimer = 8; SpeechTimer = 10; ActionTimer = 10; break;
                case 35: AddStatus("wings"); pet.PetData.AddExperience(10); ActionTimer = 15; break;
                case 36: AddStatus("flame"); pet.PetData.AddExperience(10); ActionTimer = 10; break;
                case 37: AddStatus("hang"); pet.PetData.AddExperience(10); ActionTimer = 20; EnergyTimer = 5; break;
                case 38: AddStatus("eat"); pet.PetData.AddExperience(10); ActionTimer = 10; break;
                case 40: AddStatus("swg"); pet.PetData.AddExperience(10); ActionTimer = 20; EnergyTimer = 8; break;
                case 41: AddStatus("lay"); pet.PetData.AddExperience(10); ActionTimer = 15; EnergyTimer = 5; break;
                case 42: AddStatus("eat"); pet.PetData.AddExperience(20); ActionTimer = 15; EnergyTimer = 10; break;
                case 43: AddStatus("eat"); pet.PetData.AddExperience(10); ActionTimer = 15; EnergyTimer = 5; break;
                case 44: AddStatus("wag"); pet.PetData.AddExperience(10); ActionTimer = 10; EnergyTimer = 5; break;
                case 45: pet.UpdateNeeded = true; pet.PetData.AddExperience(10); ActionTimer = 15; SpeechTimer = 5; break;
                case 46: /* breed — handled externally */ break;

                default:
                    {
                        string[] unknown = PolarEnvironment.GetGame().GetChatManager()
                            .GetPetLocale().GetValue("pet.unknowncommand");
                        if (unknown != null && unknown.Length > 0)
                            pet.Chat(unknown[RandomNumber.GenerateRandom(0, unknown.Length - 1)], false);
                        break;
                    }
            }
        }

        private void HandleLazyOrTiredPet(RoomUser pet, Room room)
        {
            RemovePetStatus();

            if (pet.PetData.Energy < 10)
            {
                // Desmontar jinete si lo hay
                RoomUser rider = room.GetRoomUserManager().GetRoomUserByVirtualId(pet.HorseID);
                if (rider != null && rider.RidingHorse)
                {
                    pet.Chat("Getof my sit", false);
                    rider.RidingHorse = false;
                    pet.RidingHorse = false;
                    rider.ApplyEffect(-1);
                    rider.MoveTo(new Point(pet.X + 1, pet.Y + 1));
                }

                string[] tired = PolarEnvironment.GetGame().GetChatManager()
                    .GetPetLocale().GetValue("pet.tired");
                if (tired != null && tired.Length > 0)
                    pet.Chat(tired[RandomNumber.GenerateRandom(0, tired.Length - 1)], false);

                if (pet.Statusses != null)
                    pet.Statusses["lay"] = TextHandling.GetString(pet.Z);
                pet.UpdateNeeded = true;
                SpeechTimer = 50; ActionTimer = 45; EnergyTimer = 5;
            }
            else
            {
                string[] lazy = PolarEnvironment.GetGame().GetChatManager()
                    .GetPetLocale().GetValue("pet.lazy");
                if (lazy != null && lazy.Length > 0)
                    pet.Chat(lazy[RandomNumber.GenerateRandom(0, lazy.Length - 1)], false);

                pet.PetData.PetEnergy(false);
            }
        }
    }
}