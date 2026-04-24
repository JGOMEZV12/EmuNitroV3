using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Polar.HabboRoleplay.PlayInternet
{
    public class PlayInternet
    {
        public int Id { get; set; }
        public string URL { get; set; }
        public string  Name { get; set; }
        public string Description { get; set; }
        public int AuthorId { get; set; }
        public string Code { get; set; }

        public PlayInternet(int Id, string URL, string Name, string Description, int AuthorId, string Code)
        {
            this.Id = Id;
            this.URL = URL;
            this.Name = Name;
            this.Description = Description;
            this.AuthorId = AuthorId;
            this.Code = Code;
        }
    }
}
