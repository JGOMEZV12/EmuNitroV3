using Polar.Communication.Packets.Outgoing.Pets;
using Polar.Communication.Packets.Outgoing.Rooms.AI.Pets;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.Database.Interfaces;
using System;

namespace Polar.HabboHotel.Rooms.AI
{
    public enum PetDatabaseUpdateState
    {
        Updated,
        NeedsUpdate,
        NeedsInsert
    }
    public class Pet
    {
        public int PetId;
        public int OwnerId;
        public int RoomId;

        public int AnyoneCanRide;
        public string Color;
        public double CreationStamp;
        public PetDatabaseUpdateState DbState;

        public int Energy;
        public int HairDye;
        public int Saddle;
        public string Name;
        public int Nutrition;
        public int PetHair;
        public bool PlacedInRoom;
        public string Race;
        public int Respect;

        public int Type;
        public int VirtualId;
        public int X;
        public int Y;
        public double Z;
        public int Experience;

        public int[] ExperienceLevels = { 100, 200, 400, 600, 1000, 1300, 1800, 2400, 3200, 4300, 7200, 8500, 10100, 13300, 17500, 23000, 51900, 75000, 128000, 150000 };

        public string GnomeClothing;

        public Pet(int petId, int ownerId, int roomId, string name, int type, string race, string color, int experience, int energy, int nutrition, int respect, double creationStamp, int x, int y, double z, int saddle, int anyoneCanRide, int dye, int petHair, string gnomeClothing)
        {
            PetId = petId;
            OwnerId = ownerId;
            RoomId = roomId;
            Name = name;
            Type = type;
            Race = race;
            Color = color;
            Experience = experience;
            Energy = energy;
            Nutrition = nutrition;
            Respect = respect;
            CreationStamp = creationStamp;
            X = x;
            Y = y;
            Z = z;
            PlacedInRoom = false;
            DbState = PetDatabaseUpdateState.Updated;
            Saddle = saddle;
            AnyoneCanRide = anyoneCanRide;
            PetHair = petHair;
            HairDye = dye;
            GnomeClothing = gnomeClothing;
        }

        // 1. AddExperience - ya lo tienes ✓
        // 2. OnRespect - ya marca NeedsUpdate, pero no guarda
        public void OnRespect()
        {
            Respect++;
            Room.SendMessage(new RespectPetNotificationMessageComposer(this));

            if (DbState != PetDatabaseUpdateState.NeedsInsert)
                DbState = PetDatabaseUpdateState.NeedsUpdate;

            if (Experience <= 150000)
                AddExperience(10); // Save() ya se llama dentro de AddExperience
        }

        public void AddExperience(int amount)
        {
            //Console.WriteLine($"[PET DEBUG] Pet {PetId} AddExperience called, amount: {amount}, current XP: {Experience}, DbState: {DbState}");

            Experience += amount;

            if (Experience > 150000)
                Experience = 150000;

            if (DbState != PetDatabaseUpdateState.NeedsInsert)
                DbState = PetDatabaseUpdateState.NeedsUpdate;

            // Console.WriteLine($"[PET DEBUG] Pet {PetId} XP after: {Experience}, DbState after: {DbState}");

            if (Room != null)
            {
                Room.SendMessage(new AddExperiencePointsComposer(PetId, VirtualId, amount));

                if (Level < MaxLevel && Experience >= ExperienceGoal)
                    LevelUp();
            }

            // Console.WriteLine($"[PET DEBUG] Pet {PetId} calling Save()...");
            Save();
            //Console.WriteLine($"[PET DEBUG] Pet {PetId} Save() completed, DbState: {DbState}");
        }

        private void LevelUp()
        {
            // Cap experience at current level goal
            if (Experience > ExperienceLevels[Level - 1])
                Experience = ExperienceLevels[Level - 1];

            if (DbState != PetDatabaseUpdateState.NeedsInsert)
                DbState = PetDatabaseUpdateState.NeedsUpdate;

            // Notify level up in room
            Room.SendMessage(new ChatComposer(VirtualId, "*leveled up to level " + Level + "*", 0, 0, string.Empty));

            // Send level updated packet
            Room.SendMessage(new PetLevelUpComposer(this));
        }

        public void PetEnergy(bool add)
        {
            int maxE;
            if (add)
            {
                if (Energy == 100) // If Energy is 100, no point.
                    return;

                if (Energy > 85)
                    maxE = MaxEnergy - Energy;
                else
                    maxE = 10;
            }
            else
                maxE = 15; // Remove Max Energy as 15

            if (maxE <= 4)
                maxE = 15;

            int r = PolarEnvironment.GetRandomNumber(4, maxE);

            if (!add)
            {
                Energy -= r;

                if (Energy < 0)
                {
                    Energy = 1;
                    r = 1;
                }
            }
            else

                Energy += r;


            if (DbState != PetDatabaseUpdateState.NeedsInsert)
                DbState = PetDatabaseUpdateState.NeedsUpdate;
        }

        public Room Room
        {
            get
            {
                if (!IsInRoom)
                    return null;

                Room room = null;
                if (PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(RoomId, out room))
                    return room;
                return null;
            }
        }

        public bool IsInRoom => (RoomId > 0);

        public int Level
        {
            get
            {
                for (int level = 0; level < ExperienceLevels.Length; ++level)
                {
                    if (Experience < ExperienceLevels[level])
                        return level + 1;
                }

                return ExperienceLevels.Length;
            }
        }

        public void Save()
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                if (DbState == PetDatabaseUpdateState.NeedsInsert)
                {
                    dbClient.SetQuery("INSERT INTO `bots` (`id`,`user_id`,`room_id`,`name`,`x`,`y`,`z`) VALUES ('" + PetId + "','" + OwnerId + "','" + RoomId + "',@name,'0','0','0')");
                    dbClient.AddParameter("name", Name);
                    dbClient.RunQuery();

                    dbClient.SetQuery("INSERT INTO `bots_petdata` (`type`,`race`,`color`,`experience`,`energy`,`createstamp`,`nutrition`,`respect`) VALUES ('" + Type + "',@race,@color,'0','100','" + CreationStamp + "','0','0')");
                    dbClient.AddParameter("race", Race);
                    dbClient.AddParameter("color", Color);
                    dbClient.RunQuery();
                }
                else if (DbState == PetDatabaseUpdateState.NeedsUpdate)
                {
                    if (Room != null)
                    {
                        RoomUser user = Room.GetRoomUserManager().GetRoomUserByVirtualId(VirtualId);
                        dbClient.RunQuery("UPDATE `bots` SET `room_id` = '" + RoomId + "', `x` = '" + (user != null ? user.X : 0) + "', `y` = '" + (user != null ? user.Y : 0) + "', `z` = '" + (user != null ? user.Z : 0) + "' WHERE `id` = '" + PetId + "' LIMIT 1");
                    }
                    dbClient.RunQuery("UPDATE `bots_petdata` SET `experience` = '" + Experience + "', `energy` = '" + Energy + "', `nutrition` = '" + Nutrition + "', `respect` = '" + Respect + "' WHERE `id` = '" + PetId + "' LIMIT 1");
                }

                DbState = PetDatabaseUpdateState.Updated;
            }
        }

        public static int MaxLevel => 20;

        public int ExperienceGoal =>
            //will error index out of range (need to look into this sometime)
            ExperienceLevels[Level - 1];

        public static int MaxEnergy => 100;

        public static int MaxNutrition => 150;

        public int Age => Convert.ToInt32(Math.Floor((PolarEnvironment.GetUnixTimestamp() - CreationStamp) / 86400));

        public string Look => Type + " " + Race + " " + Color + " " + GnomeClothing;

        public string OwnerName => PolarEnvironment.GetGame().GetClientManager().GetNameById(OwnerId);
    }
}