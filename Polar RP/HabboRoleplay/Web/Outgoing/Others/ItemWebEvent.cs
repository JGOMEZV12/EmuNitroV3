using ConnectionManager;
using Polar.Net;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.Database.Interfaces;
using Polar.HabboRoleplay.Weapons;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.HabboHotel.Items;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Wizards;

namespace Polar.HabboHotel.Roleplay.Web.Outgoing
{
    class ItemWebEvent : IWebEvent
    {
        /// <summary>
        /// Executes socket data.
        /// </summary>
        /// <param name="Client"></param>
        /// <param name="Data"></param>
        /// <param name="Socket"></param>
        public void Execute(GameClient Client, string Data, ConnectionInformation Socket)
        {

            if (!PolarEnvironment.GetGame().GetWebEventManager().SocketReady(Client, true) || !PolarEnvironment.GetGame().GetWebEventManager().SocketReady(Socket))
                return;
            

            string Action = (Data.Contains(',') ? Data.Split(',')[0] : Data);
            //string Save = (Data.Contains(' ') ? Data.Split(' ')[1] : Data);

            switch (Action)
            {
                #region editar
                case "edit":
                case "editar":
                    {
                        Room room = Client.GetHabbo().CurrentRoom;
                        string[] ReceivedData = Data.Split(',');
                        int Id = Convert.ToInt32(ReceivedData[1]);
                        Item Item = room.GetRoomItemHandler().GetItem(Id);

                        if (Client.GetHabbo().CurrentRoom != null)
                        {
                            if (Item == null || Item.GetBaseItem() == null)
                                return;

                            if (Item.GetBaseItem().InteractionType != InteractionType.BACKGROUND && Item.GetBaseItem().ItemName != "ads_background")
                                return;

                            string Extrada = Item.ExtraData;
                            string porsi = "state	0	imageUrl	http://link	offsetX	0	offsetY	0	offsetZ	0";
                            string data = "";
                            if (Extrada.StartsWith("state")) {
                                // if (Extrada == string.Empty) { 
                                data = Regex.Replace(Extrada, @"\s+", ",");
                                // data = Regex.Replace(porsi, @"\s+", ",");
                            }
                            else {
                                //data = Regex.Replace(Extrada, @"\s+", ",");
                                data = Regex.Replace(porsi, @"\s+", ",");
                            }
                            
                            string jSon = "";
                            jSon = JsonConvert.SerializeObject(data);
                            PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Client, "compose_mpu|" + Id + "|" + jSon + "");

                            //Client.GetHabbo().CurrentRoom.SendMessage(new ObjectUpdateComposer(Item, Convert.ToInt32(Item.UserID)));
                            return;
                        }
                    }
                    break;
                #endregion

                #region save
                case "save":
                case "guardar":
                    {
                        Room room = Client.GetHabbo().CurrentRoom;
                        string[] ReceivedData = Data.Split(',');
                        int Id = Convert.ToInt32(ReceivedData[1]);
                        string Save = ReceivedData[2];
                        Item Item = room.GetRoomItemHandler().GetItem(Id);
                        //Console.Write(Id);
                        if (Client.GetHabbo().CurrentRoom != null)
                        {
                                if (Item == null || Item.GetBaseItem() == null)
                                    return;

                                if (Item.GetBaseItem().InteractionType != InteractionType.BACKGROUND && Item.GetBaseItem().ItemName != "ads_background")
                                    return;

                                
                                    using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                                    {
                                        dbClient.SetQuery("UPDATE `items` SET `extra_data` = @extraData WHERE `id` = '" + Id + "'");
                                        dbClient.AddParameter("extraData", Save);
                                        dbClient.RunQuery();
                                    }
                                Item.ExtraData = Save;
                                Client.GetHabbo().CurrentRoom.SendMessage(new ObjectUpdateComposer(Item, Convert.ToInt32(Item.UserID)));
                                return;
                        }
                    }
                    break;
                #endregion

                #region Combat Mode
                case "combat":
                    {
                        if (Client.GetRoleplay().IsNoob == true)
                        {
                            Client.SendWhisper("¡Debes esperar que termine la inmunidad!  >:)", 1);
                            return;
                        }

                        Client.GetRoleplay().CombatMode = !Client.GetRoleplay().CombatMode;
                        Client.GetRoleplay().InCombat = false;
                        Client.SendWhisper("Modo de combate: " + (Client.GetRoleplay().CombatMode == true ? "activo" : "desactivado"), 1);
                    }
                    break;
                #endregion

                #region Button Mode
                case "buttonxp":
                    {
                        RoleplayManager.DoubleExp = !RoleplayManager.DoubleExp;
                        Client.SendWhisper("Doble experiencia: " + (RoleplayManager.DoubleExp == true ? "activo" : "desactivado"), 1);
                    }
                    break;
                #endregion

                #region arme
                case "arme":
                case "arma":
                    {
                        Room Room = Client.GetHabbo().CurrentRoom;
                        if (Room == null)
                            return;

                        RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Client.GetHabbo().Id);
                        if (User == null)
                            return;

                        string[] ReceivedData = Data.Split(',');
                        string Arme = ReceivedData[1];

                        Weapon BaseWeapon = WeaponManager.getWeapon(Arme);

                        if (Client.GetRoleplay().EquippedWeapon != null)
                        {
                            if (Client.GetRoleplay().EquippedWeapon.Name == BaseWeapon.Name)
                            {
                                PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(Client, ":desequipar");
                                return;
                            }
                        }

                        if (!Client.GetRoleplay().OwnedWeapons[Arme].CanUse)
                        {
                            Client.SendWhisper("No puedes usar esta arma hasta que pagues una multa de $" + String.Format("{0:N0}", Client.GetRoleplay().OwnedWeapons[Arme].CostFine) + "!", 1);
                            return;
                        }
                        if (Client.GetRoleplay().IsDead)
                        {
                            Client.SendWhisper("¡No puedes sacar un arma mientras estás muerto!", 1);
                            return;
                        }

                        if (Client.GetRoleplay().IsJailed)
                        {
                            Client.SendWhisper("¡No puedes equipar un arma mientras estás encarcelado!", 1);
                            return;
                        }

                        PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(Client, ":equipar " + Arme);
                    }
                    break;
                #endregion

                #region Wizard
                case "hechizo":
                case "wizard":
                    {
                        Room Room = Client.GetHabbo().CurrentRoom;
                        if (Room == null)
                            return;

                        RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Client.GetHabbo().Id);
                        if (User == null)
                            return;

                        string[] ReceivedData = Data.Split(',');
                        string Arme = ReceivedData[1];

                        #region Conditions
                        Hechizos BaseWeapon = HechizosManager.getWizard(Arme);

                        if (BaseWeapon == null)
                        {
                            Client.SendWhisper("¡Este hechizo no existe!", 1);
                            return;
                        }

                        if (Client.GetRoomUser().Frozen)
                            return;


                        if (Client.GetRoleplay().AmbassadorOnDuty)
                        {
                            Client.SendWhisper("No puedes usar hechizo si estas como embajador, trampos@", 1);
                            return;
                        }

                        if (Client.GetRoomUser().RidingHorse == true)
                        {
                            Client.SendWhisper("¡No puede hacer esto si eres un caballo!", 1);
                            return;
                        }

                        if (Client.GetRoleplay().Learning)
                        {
                            Client.SendWhisper("Deja de leer para poder usar tu hechizo");
                            return;
                        }

                        if (Client.GetRoleplay().IsDead)
                        {
                            Client.SendWhisper("¡No puedes usar un hechizo mientras estás muerto!", 1);
                            return;
                        }

                        if (Client.GetRoleplay().IsJailed)
                        {
                            Client.SendWhisper("¡No puedes usar un hechizo mientras estás encarcelado!", 1);
                            return;
                        }

                        if (Client.GetRoleplay().Cuffed)
                        {
                            Client.SendWhisper("No puedes sacar un " + Arme + ", con las manos esposadas", 1);
                            return;
                        }


                        if (Client.GetRoleplay().TryGetCooldown("wizard", true))
                            return;
                        #endregion

                        var Weapon = Client.GetRoleplay().OwnedHechizos[Arme];

                        if (Weapon.Health > 0)
                        {
                            Client.GetRoleplay().HechizoHealth += Weapon.Health;
                           // Client.GetRoleplay().MaxHealth += Weapon.Health;
                            Client.GetRoleplay().CurHealth += Weapon.Health;
                        }

                        if (Weapon.FiringRange > 0)
                        {
                            Client.GetRoleplay().HechizoRange = Weapon.FiringRange;
                        }

                        if (Weapon.FiringDamage > 0)
                        {
                            Client.GetRoleplay().HechizoDamage = Weapon.FiringDamage;
                        }

                        if (Weapon.Shields > 0)
                        {
                            Client.GetRoleplay().HechizoShield = Weapon.Shields;
                            Client.GetRoleplay().ChalecoPor += Weapon.Shields;

                        }

                        Client.SendWhisper(Weapon.Message, 1);
                        Client.GetRoleplay().IsNoob = false;
                        using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                        {
                            dbClient.RunQuery("DELETE FROM `rp_hechizos_owned` WHERE id = '" + Weapon.ID + "' AND base_wizard = '" + Weapon.Name + "'");
                        }
                        Client.GetRoleplay().OwnedHechizos = Client.GetRoleplay().LoadAndReturnHechizos();
                        Client.GetRoleplay().CooldownManager.CreateCooldown("wizard", 60000, 2);

                        Client.GetRoleplay().UpdateInteractingUserDialogues();
                        Client.GetRoleplay().RefreshStatDialogue();
                    }
                    break;
                #endregion

                #region update
                case "update":
                    {

                        string html = "";

                        foreach (Weapon Weapon in Client.GetRoleplay().OwnedWeapons.Values)
                        {
                            html += "<div class=\"item purple\" id=\"" + Weapon.Name + "\" idhb=\"" + Weapon.Name + "\"><img src=\"/armas/" + Weapon.Name + ".png\" style=\"width:28px;\"></div>";
                        }
                        html += "<div class=\"clearfix\"></div>";
                        string SendData = "";
                        SendData += html;

                        Socket.SendWS( "compose_update_inventory|" + SendData);
                    }
                    break;
                #endregion

                #region util
                case "util":
                    {
                        Room Room = Client.GetHabbo().CurrentRoom;
                        if (Room == null)
                            return;

                        RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Client.GetHabbo().Id);
                        if (User == null)
                            return;

                        string[] ReceivedData = Data.Split(',');
                        string Arme = ReceivedData[1];
                        if(Arme == "marihuana") {

                            PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(Client, ":consumir marihuana");

                        }
                        else if (Arme == "cigarro")
                        {
                            PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(Client, ":consumir cigarro");
                        }
                        else if (Arme == "pildora")
                        {
                            PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(Client, ":consumir pildoras");
                        }
                        else if (Arme == "caramelos" || Arme == "caramelo")
                        {
                            PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(Client, ":caramelos");
                        }
                        else if (Arme == "cocaine" || Arme == "cocaina")
                        {
                            PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(Client, ":consumir cocaina");
                        }
                        else if (Arme == "heroine" || Arme == "heroina")
                        {
                            PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(Client, ":consumir heroina");
                        }
                        else if (Arme == "chaleco")
                        {
                            PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(Client, ":ponerchaleco 1");
                        }

                    }
                    break;
                    #endregion
            }
        }

        // ── Helper: envía texto como frame WebSocket usando ConnectionInformation
        private static void SendWS(ConnectionInformation socket, string message)
        {
            if (socket == null || string.IsNullOrEmpty(message)) return;
            socket.SendData(System.Text.Encoding.UTF8.GetBytes(message));
        }

    }
}
