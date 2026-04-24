using System;
using System.Collections.Generic;
using System.Linq;

namespace Polar.HabboHotel.Catalog
{
    public class CatalogPage
    {
        private int _id;
        private int _parentId;
        private string _caption;
        private string _pageLink;
        private int _icon;
        private int _minRank;
        private int _minVIP;
        private bool _visible;
        private bool _enabled;
        private string _template;

        public List<string> _pageStrings1 { get; private set; }
        public List<string> _pageStrings2 { get; private set; }

        public Dictionary<int, CatalogItem> Items { get; private set; }
        public Dictionary<int, CatalogItem> ItemOffers { get; private set; }

        // Constructor principal (el que usa el nuevo CatalogManager)
        public CatalogPage(int Id, int ParentId, string Enabled, string Caption, string PageLink, int Icon,
            int MinRank, int MinVIP, string Visible, string Template, string PageStrings1, string PageStrings2, string PageText,
            Dictionary<int, CatalogItem> Items, Dictionary<int, CatalogItem> itemOffers)
        {
            this._id = Id;
            this._parentId = ParentId;
            this._enabled = ParseBool(Enabled);
            this._caption = Caption;
            this._pageLink = PageLink;
            this._icon = Icon;
            this._minRank = MinRank;
            this._minVIP = MinVIP;
            this._visible = ParseBool(Visible);
            this._template = Template;

            // Parse PageStrings1
            this._pageStrings1 = ParsePageStrings(PageStrings1, Template);

            // Parse PageStrings2
            this._pageStrings2 = ParsePageStrings(PageStrings2, Template);

            this.Items = Items ?? new Dictionary<int, CatalogItem>();
            this.ItemOffers = itemOffers ?? new Dictionary<int, CatalogItem>();
        }

        // Constructor antiguo para compatibilidad (con flatOffers)
        public CatalogPage(int Id, int ParentId, string Enabled, string Caption, string PageLink, int Icon, int MinRank, int MinVIP,
              string Visible, string Template, string PageStrings1, string PageStrings2, Dictionary<int, CatalogItem> Items, ref Dictionary<int, int> flatOffers)
        {
            this._id = Id;
            this._parentId = ParentId;
            this._enabled = ParseBool(Enabled);
            this._caption = Caption;
            this._pageLink = PageLink;
            this._icon = Icon;
            this._minRank = MinRank;
            this._minVIP = MinVIP;
            this._visible = ParseBool(Visible);
            this._template = Template;

            // Parse PageStrings1
            this._pageStrings1 = ParsePageStrings(PageStrings1, Template);

            // Parse PageStrings2
            this._pageStrings2 = ParsePageStrings(PageStrings2, Template);

            this.Items = Items ?? new Dictionary<int, CatalogItem>();

            // Inicializar ItemOffers
            this.ItemOffers = new Dictionary<int, CatalogItem>();

            // Filtrar solo items con OfferId válido y activo
            // En el constructor antiguo de CatalogPage:
            foreach (var item in Items.Values)
            {
                // Cambiar esto:
                if (item.OfferId > 0 && item.OfferActive)

                    // Por esto (usando la propiedad correcta):
                    if (item.OfferId > 0 && item.HaveOffer) // o item.OfferActive
                    {
                        if (!ItemOffers.ContainsKey(item.OfferId))
                        {
                            ItemOffers.Add(item.OfferId, item);
                        }
                        // También actualizar flatOffers para consistencia
                        if (!flatOffers.ContainsKey(item.OfferId))
                        {
                            flatOffers.Add(item.OfferId, Id);
                        }
                    }
            }
        }

        // Método helper para parsear booleanos
        private bool ParseBool(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            return value.ToLower() == "1" ||
                   value.ToLower() == "true" ||
                   value.ToLower() == "yes" ||
                   value.ToLower() == "y";
        }

        // Método helper para parsear cadenas de página
        private List<string> ParsePageStrings(string pageStrings, string template)
        {
            var result = new List<string>();

            if (!string.IsNullOrEmpty(pageStrings))
            {
                // Arcturus often uses | for everything, but sometimes , if it was imported from Plus
                // and layouts might need specific splits.
                char separator = pageStrings.Contains('|') ? '|' : (pageStrings.Contains(';') ? ';' : ',');

                // Some Arcturus layouts use | but the last part might be empty or specific.
                // We split while keeping empty entries as the client packet index matters.
                string[] split = pageStrings.Split(separator);

                foreach (string str in split)
                {
                    result.Add(str);
                }
            }

            return result;
        }

        // Método para verificar si un offerId pertenece a esta página
        public bool HasOffer(int offerId)
        {
            return ItemOffers.ContainsKey(offerId);
        }

        // Método para obtener item por OfferId
        public CatalogItem GetItemByOfferId(int offerId)
        {
            return ItemOffers.TryGetValue(offerId, out CatalogItem item) ? item : null;
        }

        // Método para obtener item por ItemId
        public CatalogItem GetItem(int itemId)
        {
            return Items.TryGetValue(itemId, out CatalogItem item) ? item : null;
        }

        // Método para validar si el usuario puede acceder a esta página
        public bool CanAccess(int userRank, int userVipRank)
        {
            if (!Enabled) return false;
            if (userRank < MinimumRank) return false;
            if (userRank == 1 && userVipRank < MinimumVIP) return false;
            return true;
        }

        // Método para verificar si la página está visible para el usuario
        public bool IsVisibleToUser(int userRank, int userVipRank)
        {
            if (!Visible) return false;
            return CanAccess(userRank, userVipRank);
        }

        // Propiedades
        public int Id
        {
            get { return this._id; }
            set { this._id = value; }
        }

        public int ParentId
        {
            get { return this._parentId; }
            set { this._parentId = value; }
        }

        public bool Enabled
        {
            get { return this._enabled; }
            set { this._enabled = value; }
        }

        public string Caption
        {
            get { return this._caption; }
            set { this._caption = value; }
        }

        public string PageLink
        {
            get { return this._pageLink; }
            set { this._pageLink = value; }
        }

        public int Icon
        {
            get { return this._icon; }
            set { this._icon = value; }
        }

        public int MinimumRank
        {
            get { return this._minRank; }
            set { this._minRank = value; }
        }

        public int MinimumVIP
        {
            get { return this._minVIP; }
            set { this._minVIP = value; }
        }

        public bool Visible
        {
            get { return this._visible; }
            set { this._visible = value; }
        }

        public string Template
        {
            get { return this._template; }
            set { this._template = value; }
        }

        public List<string> PageStrings1
        {
            get { return this._pageStrings1 ?? (_pageStrings1 = new List<string>()); }
            private set { this._pageStrings1 = value; }
        }

        public List<string> PageStrings2
        {
            get { return this._pageStrings2 ?? (_pageStrings2 = new List<string>()); }
            private set { this._pageStrings2 = value; }
        }
    }
}