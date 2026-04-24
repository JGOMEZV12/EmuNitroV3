#region

using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using log4net;

#endregion

namespace Polar.Core
{
    class CatalogSettings
    {

        public static string CATALOG_NOTICE_1 = "";
        public static string CATALOG_IMG_NOTICE_1 = "";
        public static string CATALOG_URL_NOTICE_1 = "";
        public static int CATALOG_PAGEID_NOTICE_1 = -1;
        public static string CATALOG_NOTICE_2 = "";
        public static string CATALOG_IMG_NOTICE_2 = "";
        public static string CATALOG_URL_NOTICE_2 = "";
        public static int CATALOG_PAGEID_NOTICE_2 = -1;
        public static string CATALOG_NOTICE_3 = "";
        public static string CATALOG_IMG_NOTICE_3 = "";
        public static string CATALOG_URL_NOTICE_3 = "";
        public static int CATALOG_PAGEID_NOTICE_3 = -1;
        public static string CATALOG_NOTICE_4 = "";
        public static string CATALOG_IMG_NOTICE_4 = "";
        public static string CATALOG_URL_NOTICE_4 = "";
        public static int CATALOG_PAGEID_NOTICE_4 = -1;
        private static readonly ILog log = LogManager.GetLogger("Polar.Core");

        public static bool RunCatalogSettings()
        {
            foreach (var @params in from line in File.ReadAllLines("Settings/catalog.ini", Encoding.Default) where !String.IsNullOrWhiteSpace(line) && line.Contains("=") select line.Split('='))
            {
                switch (@params[0])
                {
                    case "catalog.index.notice.1":
                        CATALOG_NOTICE_1 = @params[1];
                        break;
                    case "catalog.img.notice.1":
                        CATALOG_IMG_NOTICE_1 = @params[1];
                        break;
                    case "catalog.link.notice.1":
                        CATALOG_URL_NOTICE_1 = @params[1];
                        break;
                    case "catalog.pageid.notice.1":
                        CATALOG_PAGEID_NOTICE_1 = int.Parse(@params[1]);
                        break;
                    case "catalog.index.notice.2":
                        CATALOG_NOTICE_2 = @params[1];
                        break;
                    case "catalog.img.notice.2":
                        CATALOG_IMG_NOTICE_2 = @params[1];
                        break;
                    case "catalog.link.notice.2":
                        CATALOG_URL_NOTICE_2 = @params[1];
                        break;
                    case "catalog.pageid.notice.2":
                        CATALOG_PAGEID_NOTICE_2 = int.Parse(@params[1]);
                        break;
                    case "catalog.index.notice.3":
                        CATALOG_NOTICE_3 = @params[1];
                        break;
                    case "catalog.img.notice.3":
                        CATALOG_IMG_NOTICE_3 = @params[1];
                        break;
                    case "catalog.link.notice.3":
                        CATALOG_URL_NOTICE_3 = @params[1];
                        break;
                    case "catalog.pageid.notice.3":
                        CATALOG_PAGEID_NOTICE_3 = int.Parse(@params[1]);
                        break;
                    case "catalog.index.notice.4":
                        CATALOG_NOTICE_4 = @params[1];
                        break;
                    case "catalog.img.notice.4":
                        CATALOG_IMG_NOTICE_4 = @params[1];
                        break;
                    case "catalog.link.notice.4":
                        CATALOG_URL_NOTICE_4 = @params[1];
                        break;
                    case "catalog.pageid.notice.4":
                        CATALOG_PAGEID_NOTICE_4 = int.Parse(@params[1]);
                        break;
                }
            }
            //log.Info("» Catalog Settings -> CARGADO!");
            Out.WriteLine("Catalog Settings -> CARGADO!", "Polar.Core", ConsoleColor.DarkGray);
            return true;
        }
    }
}