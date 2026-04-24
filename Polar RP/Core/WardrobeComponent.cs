using Polar.Database.Interfaces;
using Polar;
using System.Collections.Generic;
using Polar.HabboHotel.Users;
using System.Data;
using System.Reflection.PortableExecutable;
using ConsoleWriter;
using System.Reflection;

namespace Polar.HabboHotel.Users
{
    public class WardrobeComponent
    {
        private readonly Dictionary<int, WardrobeItem> looks;
        private readonly HashSet<int> clothing;

        public WardrobeComponent(Habbo habbo)
        {
            this.looks = new Dictionary<int, WardrobeItem>();


                DataRow GetRow = null;
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("SELECT * FROM users_wardrobe WHERE user_id = @userId");
                    dbClient.AddParameter("userId", habbo.Id);
                    GetRow = dbClient.getRow();
                }

                if (GetRow != null)
                {
                    int slotId = Convert.ToInt32(GetRow["slot_id"]);
                    WardrobeItem wardrobeItem = new WardrobeItem(GetRow, habbo);
                    this.looks.Add(slotId, wardrobeItem);
                }

            this.clothing = new HashSet<int>();
        }

        public WardrobeItem CreateLook(Habbo habbo, int slotId, string look)
        {
            return new WardrobeItem(habbo.Gender, look, slotId, habbo);
        }

        public Dictionary<int, WardrobeItem> GetLooks()
        {
            return this.looks;
        }

        public HashSet<int> GetClothing()
        {
            return this.clothing;
        }

        public void Dispose()
        {
            looks.Clear();
        }

        public class WardrobeItem
        {
            private int slotId;
            private string gender;
            private Habbo habbo;
            private string look;
            private bool needsInsert;
            private bool needsUpdate;

            public WardrobeItem(DataRow reader, Habbo habbo)
            {
                this.gender = reader["gender"].ToString();
                this.look = reader["look"].ToString();
                this.slotId = Convert.ToInt32(reader["slot_id"]);
                this.habbo = habbo;
                this.needsInsert = false;
                this.needsUpdate = false;
            }

            public WardrobeItem(string gender, string look, int slotId, Habbo habbo)
            {
                this.gender = gender;
                this.look = look;
                this.slotId = slotId;
                this.habbo = habbo;
                this.needsInsert = false;
                this.needsUpdate = false;
            }

            public string GetGender()
            {
                return gender;
            }

            public void SetGender(string gender)
            {
                this.gender = gender;
            }

            public Habbo GetHabbo()
            {
                return habbo;
            }

            public void SetHabbo(Habbo habbo)
            {
                this.habbo = habbo;
            }

            public string GetLook()
            {
                return look;
            }

            public void SetLook(string look)
            {
                this.look = look;
            }

            public void SetNeedsInsert(bool needsInsert)
            {
                this.needsInsert = needsInsert;
            }

            public void SetNeedsUpdate(bool needsUpdate)
            {
                this.needsUpdate = needsUpdate;
            }

            public int GetSlotId()
            {
                return slotId;
            }

            public void SetSlotId(int slotId)
            {
                this.slotId = slotId;
            }

            public void Run()
            {
                    if (this.needsInsert)
                    {
                        this.needsInsert = false;
                        this.needsUpdate = false;
                        using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                        {
                            dbClient.SetQuery("INSERT INTO `user_wardrobe` (`user_id`,`slot_id`,`look`,`gender`) VALUES ('" + habbo.Id + "',@slot,@look,@gender)");
                            dbClient.AddParameter("slot", this.slotId);
                            dbClient.AddParameter("look", this.look);
                            dbClient.AddParameter("gender", this.gender.ToUpper());
                            dbClient.RunQuery();
                        }
                    }
                    if (this.needsUpdate)
                    {
                        this.needsUpdate = false;
                        using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                        {
                            dbClient.SetQuery("UPDATE `user_wardrobe` SET `look` = @look, `gender` = @gender WHERE `user_id` = '" + habbo.Id + "' AND `slot_id` = @slot LIMIT 1");
                            dbClient.AddParameter("slot", this.slotId);
                            dbClient.AddParameter("look", this.look);
                            dbClient.AddParameter("gender", this.gender.ToUpper());
                            dbClient.RunQuery();
                        }
                    }

            }
        }
    }
}
