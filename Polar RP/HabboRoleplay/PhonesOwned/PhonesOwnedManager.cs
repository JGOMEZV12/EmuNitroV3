using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Pathfinding;
using log4net;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboRoleplay.PhoneOwned
{
    public class PhonesOwnedManager
    {
        /// <summary>
        /// log4net
        /// </summary>
        private readonly ILog log = LogManager.GetLogger("Polar.HabboRoleplay.PhoneOwned");


        /// <summary>
        /// Thread-safe dictionary containing all houses
        /// </summary>
        public static ConcurrentDictionary<int, PhonesOwned> _PhonesOwned = new ConcurrentDictionary<int, PhonesOwned>();

        /// <summary>
        /// Initializes the house list dictionary
        /// </summary>
        public void Init()
        {
            _PhonesOwned.Clear();

            DataTable PhOwn;

            using (IQueryAdapter DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                DB.SetQuery("SELECT * FROM `rp_phones_owned`");
                PhOwn = DB.getTable();

                if (PhOwn != null)
                {
                    foreach (DataRow Row in PhOwn.Rows)
                    {
                        int Id = Convert.ToInt32(Row["id"]);
                        uint PhoneId = Convert.ToUInt32(Row["phone_id"]);
                        int OwnerId = Convert.ToInt32(Row["user_id"]);
                        string PhoneNumber = Convert.ToString(Row["phone_number"]);

                        PhonesOwned newPhOwn = new PhonesOwned(Id, PhoneId, OwnerId, PhoneNumber);
                        _PhonesOwned.TryAdd(Id, newPhOwn);
                    }
                }
            }
            log.Info("Loaded " + _PhonesOwned.Count + " PhonesOwned");
        }

        public List<PhonesOwned> getAllPhonesOwned()
        {
            List<PhonesOwned> PO = new List<PhonesOwned>();

            foreach (var item in _PhonesOwned)
            {
                PO.Add(item.Value);
            }
            return PO;
        }

        // Obtener el elemento con el key del diccionario
        public PhonesOwned getPhonesOwned(int ID)
        {
            if (_PhonesOwned.ContainsKey(ID))
                return _PhonesOwned[ID];
            else
                return null;
        }

        // Obtener una lista del elemento del diccionario con el key
        public List<PhonesOwned> getPhonesOwnedList(int Id)
        {
            List<PhonesOwned> PO = new List<PhonesOwned>();

            lock (_PhonesOwned)
            {
                if (_PhonesOwned.Values.Where(x => x.Id == Id).ToList().Count > 0)
                    PO.Add(_PhonesOwned.Values.FirstOrDefault(x => x.Id == Id));
            }
            return PO;
        }

        // Obtener una lista del elemento del diccionario con el PhoneId
        public List<PhonesOwned> getPhonesOwnedByPhoneId(int PhoneId)
        {
            List<PhonesOwned> PO = new List<PhonesOwned>();

            foreach (var phone in _PhonesOwned)
            {
                if (phone.Value.PhoneId == PhoneId)
                    PO.Add(phone.Value);
            }
            return PO;
        }

        // Método para Crear teléfono en DB play_phones_owned (Al comprar teléfonos nuevos)
        public bool TryCreatePhoneOwned(GameClient Session, uint PhoneId, int UserId, string PhoneNumber, out PhonesOwned PO)
        {
            PO = null;

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                PO = new PhonesOwned(0, PhoneId, UserId, PhoneNumber);

                dbClient.SetQuery("INSERT INTO `rp_phones_owned` (`phone_id`, `user_id`, `phone_number`) VALUES (@phoneid, @owner, @phonenumber)");
                dbClient.AddParameter("phoneid", PhoneId);
                dbClient.AddParameter("owner", UserId);
                dbClient.AddParameter("phonenumber", PhoneNumber);

                PO.Id = Convert.ToInt32(dbClient.InsertQuery());

                if (Session != null)
                {
                    Session.GetRoleplay().Phone = PO.Id;
                    Session.GetRoleplay().PhoneModelId = Convert.ToInt32(PO.PhoneId);
                    Session.GetRoleplay().PhoneNumber = PO.PhoneNumber;
                }

                if (!_PhonesOwned.TryAdd(PO.Id, PO))
                    return false;
            }
            return true;
        }

        // Métofo para actualizar datos del teléfono
        public bool UpdatePhoneOwner(GameClient Session, uint PhoneID, bool ToDB, out PhonesOwned POD)
        {
            POD = null;

            if (Session == null)
                return false;

            List<PhonesOwned> PO = PolarEnvironment.GetGame().GetPhonesOwnedManager().getPhonesOwnedList(Session.GetRoleplay().Phone);
            if (PO != null && PO.Count > 0)
            {
                PO[0].PhoneId = PhoneID;
                Session.GetRoleplay().PhoneModelId = Convert.ToInt32(PhoneID);

                if (ToDB)
                {
                    using (var DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                    {
                        DB.SetQuery("UPDATE `rp_phones_owned` SET `phone_id` = @phoneid WHERE `rp_phones_owned`.`id` = @id");
                        DB.AddParameter("id", Session.GetRoleplay().Phone);
                        DB.AddParameter("phoneid", PhoneID);
                        DB.RunQuery();
                    }
                }
                POD = getPhonesOwned(PO[0].Id);
                return (POD != null) ? true : false;
            }
            return false;
        }

        // Devuelve lista con los teléfonos de cierto dueño
        public List<PhonesOwned> getMyPhonesOwned(int OwnerId)
        {
            List<PhonesOwned> PO = new List<PhonesOwned>();

            foreach (var item in _PhonesOwned)
            {
                if (item.Value.OwnerId == OwnerId)
                    PO.Add(item.Value);
            }
            return PO;
        }
    }
}
