using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;

using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboRoleplay.VehicleOwned
{
    public class VehiclesOwned
    {
        public int Id;
        public int FurniId;
        public int ItemId;
        public int OwnerId;
        public int LastUserId;
        public string Model;
        public int Fuel;
        public int Km;
        public int State;  //(ENUM) 0 = Abierto| 1 = Con Traba | 2 = No traba y Averid. | 3 = Con traba y Averid.
        public bool Traba;
        public bool Alarm;
        public int Location;
        public int X;
        public int Y;
        public double Z;
        public string[] Baul;
        public bool BaulOpen;
        public int CarLife;
        public int CamCargId;
        public int CamState;// 0: Sin carga | 1: Cargado | 2: Entregado
        public int CamDest;
        public int CamOwnId;
        public VehiclesOwned(int Id, int FurniId, int ItemId, int OwnerId, int LastUserId, string Model, int Fuel, int Km, int State, bool Traba, bool Alarm, int Location, int X, int Y, double Z, string[] Baul, bool BaulOpen, int CarLife, int CamCargId = 0, int CamState = 0, int CamDest = 0, int CamOwnId = 0)
        {
            this.Id = Id;
            this.FurniId = FurniId;
            this.ItemId = ItemId;
            this.OwnerId = OwnerId;
            this.LastUserId = LastUserId;
            this.Model = Model;
            this.Fuel = Fuel;
            this.Km = Km;
            this.State = State;
            this.Traba = Traba;
            this.Alarm = Alarm;
            this.Location = Location;
            this.X = X;
            this.Y = Y;
            this.Z = Z;
            this.Baul = Baul;
            this.BaulOpen = BaulOpen;
            this.CarLife = CarLife;
            this.CamCargId = CamCargId;
            this.CamState = CamState;
            this.CamDest = CamDest;
            this.CamOwnId = CamOwnId;
        }
        
    }
}
