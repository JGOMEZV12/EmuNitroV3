using System;
using System.Collections.Generic;
using System.Data;
using Polar.Database.Interfaces;
using Polar;
using Polar.HabboHotel.Items;
using Polar.Communication.Packets.Outgoing;

namespace Polar.HabboHotel.Catalog
{
    public class TargetedOffersManager
    {

        public TargetedOffers TargetedOffer;
        public void Initialize(IQueryAdapter dbClient)
        {
            TargetedOffer = null;

            dbClient.SetQuery("SELECT * FROM targeted_offers WHERE active = 'true' LIMIT 1;");
            var row = dbClient.getRow();


            if (row == null)
                return;
            TargetedOffer = new TargetedOffers((int)row["id"], (int)row["limit"], Convert.ToInt32(PolarEnvironment.GetUnixTimestamp() +
                (((int)row["time"] + 10) * 1)), (row["open"].ToString() == "show"), (row["active"].ToString() == "true"), (string)row["code"],
                (string)row["title"], (string)row["description"], (string)row["image"], (string)row["icon"],
                (string)row["money_type"], (string)row["items"], (string)row["price"]);
        }
    }

    public class TargetedOffers
    {
        public int Id, Limit, Time, Expire;
        public bool Open, Active;
        public string Code, Title, Description, Image, Icon, MoneyType;
        public string[] Items, Price;
        public List<TargetedItems> Products;

        public TargetedOffers(int id, int limit, int time, bool open, bool active, string code, string title,
            string description, string image, string icon, string moneyType, string items, string price)
        {
            Id = id;
            Limit = limit;
            Time = time - PolarEnvironment.GetIUnixTimestamp();
            Open = open;
            Active = active;
            Code = code;
            Title = title;
            Description = description;
            Image = image;
            Icon = icon;
            MoneyType = moneyType;
            Items = items.Split(';');
            Price = price.Split(';');
            Expire = time;

            Products = new List<TargetedItems>();
            foreach (var item in Items)
            {
                var itemType = item.Split(',')[0];
                var itemProduct = item.Split(',')[1];
                Products.Add(new TargetedItems(Id, itemType, itemProduct));
            }
        }

        public int MoneyCode(string moneyType)
        {
            switch (moneyType)
            {
                case "duckets":
                    return 0;

                case "diamonds":
                    return 5;

                default:
                    return 0;
            }
        }

        public void Serialize(ServerPacket message)
        {
            message.WriteInteger(Open ? 4 : 1);
            message.WriteInteger(Id);
            message.WriteString(Code);
            message.WriteString(Code);
            message.WriteInteger(int.Parse(Price[0]));
            message.WriteInteger(int.Parse(Price[1]));
            message.WriteInteger(MoneyCode(MoneyType));
            message.WriteInteger(Limit);
            message.WriteInteger(Time);
            message.WriteString(Title);
            message.WriteString(Description);
            message.WriteString(Image);
            message.WriteString(Icon);
            message.WriteInteger(0);
            message.WriteInteger(Products.Count);
            foreach (var product in Products) message.WriteString(string.Empty);
        }
    }

    public class TargetedItems
    {
        public int TargetedId;
        public string ItemType, Item;

        public TargetedItems(int targetedId, string itemType, string item)
        {
            TargetedId = targetedId;
            ItemType = itemType;
            Item = item;
        }
    }
}
