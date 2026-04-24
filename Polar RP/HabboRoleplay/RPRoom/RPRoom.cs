using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.HabboRoleplay.RPRoom
{
    public class RPRoom
    {
        public int Id { get; set; }
        public string CityRP { get; set; }
        public bool  HospitalRP { get; set; }
        public bool PrisonRP { get; set; }
        public bool CourtRP { get; set; }
        public bool PrisonBackRP { get; set; }
        public bool CamioneroRP { get; set; }
        public bool MecanicoRP { get; set; }
        public bool BasureroRP { get; set; }
        public bool ArmeroRP { get; set; }
        public bool PolStationRP { get; set; }

        public RPRoom(int Id, string CityRP, bool CourtRP, bool HospitalRP, bool PrisonRP, bool PrisonBRP, bool CamioneroRP, bool MecanicoRP, bool BasureroRP, bool ArmeroRP, bool PolStationRP)
        {
            this.Id = Id;
            this.CityRP = CityRP;
            this.HospitalRP = HospitalRP;
            this.PrisonRP = PrisonRP;
            this.CourtRP = CourtRP;
            this.PrisonBackRP = PrisonBRP;
            this.CamioneroRP = CamioneroRP;
            this.MecanicoRP = MecanicoRP;
            this.BasureroRP = BasureroRP;
            this.ArmeroRP = ArmeroRP;
            this.PolStationRP = PolStationRP;
        }
    }
}
