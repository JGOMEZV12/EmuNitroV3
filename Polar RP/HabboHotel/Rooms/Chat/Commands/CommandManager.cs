using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polar.Database.Interfaces;
using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Items.Wired;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic;
using Polar.HabboHotel.Rooms.Chat.Commands.Users.Events;
using Polar.HabboHotel.Rooms.Chat.Commands.Users.Gangs;
using Polar.HabboHotel.Rooms.Chat.Commands.Users.Apartment;
using Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.General;
using Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Hospital;
using Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Police;
using Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Clothing;
using Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Bank;
using Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Basurero;
using Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Restaurant;
using Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Criminal;
using Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Banking;
using Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Combat;
using Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.General;
using Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Offers;
using Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Police;
using Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Timers;
using Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Purchasing;
using Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Toggles;
using Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Interactions.Items;
using Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Interactions.Self;
using Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Interactions.Marriage;

using Polar.HabboHotel.Rooms.Chat.Commands.VIP;
using Polar.HabboHotel.Rooms.Chat.Commands.Moderators;
using Polar.HabboHotel.Rooms.Chat.Commands.Moderators.Trials;
using Polar.HabboHotel.Rooms.Chat.Commands.Moderators.Seniors;
using Polar.HabboHotel.Rooms.Chat.Commands.Administrators;
using Polar.HabboHotel.Rooms.Chat.Commands.Managers;
using Polar.HabboHotel.Rooms.Chat.Commands.Developers;
using Polar.HabboHotel.Rooms.Chat.Commands.Owners;
using Polar.HabboHotel.Rooms.Chat.Commands.SpecialRights;
using Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Interactions.Basic;
using Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Bounties;
using Polar.HabboHotel.Rooms.Chat.Commands.Ambassadors;
using Polar.HabboHotel.Rooms.Chat.Pets.Commands;
using Polar.HabboHotel.Rooms.Chat.Commands.User.Fun;
using Polar.HabboHotel.Rooms.Chat.Commands.User;
using Polar.Core;
using Polar.HabboHotel.Rooms.Chat.Commands.Users;

namespace Polar.HabboHotel.Rooms.Chat.Commands
{
    public class CommandManager
    {
        /// <summary>
        /// Command Prefix only applies to custom commands.
        /// </summary>
        private string _prefix = ":";

        /// <summary>
        /// Commands registered for use.
        /// </summary>
        public Dictionary<string, IChatCommand> _commands;
        public Dictionary<string, IChatCommand> _jobcommands;
        public Dictionary<string, IChatCommand> _gangcommands;
        public Dictionary<string, IChatCommand> _staffcommands;
        public Dictionary<string, IChatCommand> _vehiclescommands;
        public Dictionary<string, IChatCommand> _rpCommands;
        public Dictionary<string, IChatCommand> _ambassadorcommands;
        public Dictionary<string, IChatCommand> _vipcommands;
        public Dictionary<string, IChatCommand> _eventcommands;
        public List<string> _aliases;

        /// <summary>
        /// The default initializer for the CommandManager
        /// </summary>
        public CommandManager(string Prefix)
        {
            this._prefix = Prefix;
            this._commands = new Dictionary<string, IChatCommand>();
            this._jobcommands = new Dictionary<string, IChatCommand>();
            this._gangcommands = new Dictionary<string, IChatCommand>();
            this._ambassadorcommands = new Dictionary<string, IChatCommand>();
            this._staffcommands = new Dictionary<string, IChatCommand>();
            this._vehiclescommands = new Dictionary<string, IChatCommand>();
            this._rpCommands = new Dictionary<string, IChatCommand>();
            this._vipcommands = new Dictionary<string, IChatCommand>();
            this._eventcommands = new Dictionary<string, IChatCommand>();
            this._aliases = new List<string>();

            this.RegisterUsers();
            this.RegisterUsersGangs();
            this.RegisterUsersJobs();
            this.RegisterVIP();
            this.RegisterAmbassadors();
            this.RegisterTrialModerators();
            this.RegisterModerators();
            this.RegisterSeniorModerators();
            this.RegisterAdministrators();
            this.RegisterManagers();
            this.RegisterDevelopers();
            this.RegisterOwners();
            this.RegisterSpecialRights();
        }

        /// <summary>
        /// Request the text to parse and check for commands that need to be executed.
        /// </summary>
        /// <param name="Session">Session calling this method.</param>
        /// <param name="Message">The message to parse.</param>
        /// <returns>True if parsed or false if not.</returns>
        public async Task<bool> Parse(GameClient Session, string Message)
        {
            try
            {
                // Verificación inicial más completa
                if (Session == null || Session.GetHabbo() == null || Session.GetHabbo().CurrentRoom == null)
                    return false;

                var habbo = Session.GetHabbo();
                var currentRoom = habbo.CurrentRoom;

                if (!Message.StartsWith(_prefix))
                    return false;

               /* #region Commands List

                #region :commands
                if (Message.ToLower() == _prefix + "comandos")
            {
                StringBuilder List = new StringBuilder();
                List.Append("Esta es la lista de comandos que tiene disponible:\n\n");
                List.Append(":tcomandos - Proporciona una lista de todos los comandos de trabajo disponibles.\n");
                List.Append(":pcomandos - Proporciona una lista de todos los comandos de pandilla disponibles.\n");
                List.Append(":vipcommands - Proporciona una lista de todos los comandos VIP disponibles.\n");
                List.Append(":staffcomandos - Proporciona una lista de todos los comandos de personal disponibles.\n------------------------------------------------------------------------------------\n");

                foreach (var CmdList in _commands.ToList())
                {
                    if (_aliases.Contains(CmdList.Key.ToLower()))
                        continue;

                    if (!string.IsNullOrEmpty(CmdList.Value.PermissionRequired))
                    {
                        if (!Session.GetHabbo().GetPermissions().HasCommand(CmdList.Value.PermissionRequired))
                            continue;
                    }

                    //List.Append("\n");
                    List.Append(":" + CmdList.Key + " " + CmdList.Value.Parameters + " - " + CmdList.Value.Description + "\n");
                }
                Session.SendMessage(new MOTDNotificationComposer(List.ToString()));
                return true;
            }
            #endregion

            #region :jobcommands
            if (Message.ToLower() == _prefix + "tcomandos")
            {
                StringBuilder List = new StringBuilder();
                List.Append("Esta es la lista de comandos de trabajo disponibles:\n\n");
                foreach (var CmdList in _jobcommands.ToList())
                {
                    if (_aliases.Contains(CmdList.Key.ToLower()))
                        continue;

                    /*if (CmdList.Key == "trabajar" || CmdList.Key == "notrabajar" || CmdList.Key == "empresas" || CmdList.Key == "cinfo")
                        List.Append(":" + CmdList.Key + " " + CmdList.Value.Parameters + " - " + CmdList.Value.Description + "\n");
                    else if (CmdList.Key == "promover" || CmdList.Key == "degradar")
                    {
                        if (Session.GetHabbo().GetPermissions().HasRight("corporation_rights"))
                            List.Append(":" + CmdList.Key + " " + CmdList.Value.Parameters + " - " + CmdList.Value.Description + "\n");
                    }
                    else if (CmdList.Key == "ra" || CmdList.Key == "togglera")
                    {
                        if (Session.GetHabbo().GetPermissions().HasRight("corporation_rights") || Groups.GroupManager.HasJobCommand(Session, CmdList.Key.ToLower()))
                            List.Append(":" + CmdList.Key + " " + CmdList.Value.Parameters + " - " + CmdList.Value.Description + "\n");
                    }
                    else
                    {
                        if (Groups.GroupManager.HasJobCommand(Session, CmdList.Key.ToLower()))
                            List.Append(":" + CmdList.Key + " " + CmdList.Value.Parameters + " - " + CmdList.Value.Description + "\n");
                    }//
                    List.Append(":" + CmdList.Key + " " + CmdList.Value.Parameters + " - " + CmdList.Value.Description + "\n");

                }
                Session.SendMessage(new MOTDNotificationComposer(List.ToString()));
                return true;
            }
            #endregion

            #region :gangcommands
            if (Message.ToLower() == _prefix + "pcomandos")
            {
                StringBuilder List = new StringBuilder();
                List.Append("Estos son los comandos disponibles de pandillas:\n\n");
                foreach (var CmdList in _gangcommands.ToList())
                {
                    if (_aliases.Contains(CmdList.Key.ToLower()))
                        continue;

                    if (CmdList.Key == "ginfo" || CmdList.Key == "glist" || CmdList.Key == "turfs" || CmdList.Key == "gcreate" || CmdList.Key == "gleave")
                        List.Append(":" + CmdList.Key + " " + CmdList.Value.Parameters + " - " + CmdList.Value.Description + "\n");
                    else if (CmdList.Key == "gcapture" && GroupManager.GetGang(Session.GetRoleplay().GangId).IsGang)
                        List.Append(":" + CmdList.Key + " " + CmdList.Value.Parameters + " - " + CmdList.Value.Description + "\n");
                    else if (Groups.GroupManager.HasGangCommand(Session, CmdList.Key))
                        List.Append(":" + CmdList.Key + " " + CmdList.Value.Parameters + " - " + CmdList.Value.Description + "\n");
                }
                Session.SendMessage(new MOTDNotificationComposer(List.ToString()));
                return true;
            }
            #endregion

            #region :staffcommands
            if (Message.ToLower() == _prefix + "staffcomandos")
            {
                if (!Session.GetHabbo().GetPermissions().HasRight("mod_tool"))
                {
                    Session.SendWhisper("Tu no eres staff", 1);
                    return true;
                }
                else
                {
                    StringBuilder List = new StringBuilder();
                    List.Append("Esta es la lista de comandos de personal que tiene disponibles:\n\n");
                    foreach (var CmdList in _staffcommands.ToList())
                    {
                        if (_aliases.Contains(CmdList.Key.ToLower()))
                            continue;

                        if (!string.IsNullOrEmpty(CmdList.Value.PermissionRequired))
                        {
                            if (!Session.GetHabbo().GetPermissions().HasCommand(CmdList.Value.PermissionRequired) && Session.GetHabbo().GetPermissions().HasRight("mod_tool"))
                                continue;
                        }

                        List.Append(":" + CmdList.Key + " " + CmdList.Value.Parameters + " - " + CmdList.Value.Description + "\n");
                    }
                    Session.SendMessage(new MOTDNotificationComposer(List.ToString()));
                    return true;
                }
            }
            #endregion

            #region :vipcommands
            if (Message.ToLower() == _prefix + "vipcommands")
            {
                if (Session.GetHabbo().VIPRank < 1 && !Session.GetHabbo().GetClubManager().HasSubscription("habbo_vip"))
                {
                    Session.SendWhisper("No eres VIP", 1);
                    return true;
                }
                else
                {
                    StringBuilder List = new StringBuilder();
                    List.Append("Esta es la lista de comandos vip que tiene disponibles:\n\n");
                    foreach (var CmdList in _vipcommands.ToList())
                    {
                        if (_aliases.Contains(CmdList.Key.ToLower()))
                            continue;

                        List.Append(":" + CmdList.Key + " " + CmdList.Value.Parameters + " - " + CmdList.Value.Description + "\n");
                    }
                    Session.SendMessage(new MOTDNotificationComposer(List.ToString()));
                    return true;
                }
            }
                #endregion

                #endregion*/

                if (Message == _prefix + "commands" || Message == _prefix + "comandos")
                {
                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_commands", "open");
                    return true;
                }

                Message = Message.Substring(1);
                string[] Split = Message.Split(' ');

                if (Split.Length == 0)
                    return false;

                IChatCommand Cmd = null;

                // Verifica que el comando exista
                if (!_commands.TryGetValue(Split[0].ToLower(), out Cmd))
                    return false;

                // Verifica que el comando no sea null
                if (Cmd == null)
                    return false;


                // Verificación de permisos con null checks
                if (!string.IsNullOrEmpty(Cmd.PermissionRequired))
                {
                    var permissions = habbo.GetPermissions();
                    if (permissions == null)
                        return false;

                    if (!permissions.HasCommand(Cmd.PermissionRequired))
                        return false;
                }

                // Verificación para comandos de ganga con null checks
                if (_gangcommands.ContainsKey(Split[0].ToLower()))
                {
                    var roleplay = habbo.GetClient().GetRoleplay();
                    if (roleplay == null)
                        return false;

                    // Más verificaciones específicas para comandos de ganga
                }

                // Ejecutar el comando con verificaciones
                habbo.IChatCommand = Cmd;

                // Verifica que el wired trigger no cause null reference
                if (currentRoom?.GetWired() != null)
                {
                    currentRoom.GetWired().TriggerEvent(WiredBoxType.TriggerUserSaysCommand, habbo, this);
                }

                await Cmd.Execute(Session, currentRoom, Split);
                return true;
            }
            catch (Exception ex)
            {
                // Log del error para debugging
                Logging.LogException($"Error en CommandManager.Parse: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        #region Commands
        /// <summary>
        /// User set of commands
        /// </summary>
        private void RegisterUsers()
        {
            // General
            this.Register("mpu", new MPUCommand());
            this.Register("text", new TextCommand());
            this.Register("re", new ReCommand());
            this.Register("info", new AboutCommand());
            this.Register("inmunidad", new InmunidadCommand());
            this.Register("actualizaciones", new ChangeLogCommand());
            this.Register("yo", new StatsCommand());
            this.Register("hidewired", new HideWiredCommand());
            this.Register("agriculturastats", new FarmingStatsCommand());
            this.Register("farming", new FarmingStatsCommand(), "", true);
            this.Register("misarmas", new WeaponsCommand());
            this.Register("conectados", new OnlineCommand());
            this.Register("online", new OnlineCommand(), "", true);
            this.Register("salasactivas", new HotRoomsCommand());
            this.Register("hr", new HotRoomsCommand(), "", true);
            this.Register("taxi", new TaxiCommand());
            this.Register("ir", new IrCommand());
            this.Register("bus", new BusCommand());
            this.Register("casinoinfo", new CasinoinfoCommand());
            this.Register("disco", new DiscoCommand());
            this.Register("notaxi", new StopTaxiCommand());
            this.Register("estoy", new RoomIDCommand());
            this.Register("salainfo", new RoomInfoCommand());
            this.Register("regenerarmapa", new RegenMapsCommand());
            this.Register("emptyitems", new EmptyItemsCommand());
            this.Register("poof", new PoofCommand());
            this.Register("desconectar", new LogOutCommand());
            this.Register("cambiarclase", new ChangeClassCommand());
            this.Register("flechas", new ArrowCommand());
            this.Register("walk", new ArrowCommand());
            this.Register("prefix", new PrefixCommand());

            // Timers & Cooldowns
            this.Register("timeleft", new TimeLeftCommand());
            this.Register("cooldowns", new CooldownsCommand());
            this.Register("cds", new CooldownsCommand(), "", true);
            this.Register("cd", new CooldownsCommand(), "", true);

            // Banking
            this.Register("saldo", new BalanceCommand(), "rp");
            this.Register("tanque", new TanqueCommand(), "rp");
            this.Register("cedula", new CedulaCommand(), "rp");
            this.Register("basura", new CamionCommand(), "rp");
            this.Register("depositar", new DepositCommand(), "rp");
            this.Register("retirar", new WithdrawCommand(), "rp");

            // Criminal Activity
            this.Register("leyes", new LawsCommand(), "rp");
            this.Register("noticias", new TutorialCommand(), "rp");
            this.Register("tutorialbr", new TutorialbrCommand());
            this.Register("businfo", new BusinfoCommand(), "rp");
            this.Register("robar", new RobCommand(), "rp");
            this.Register("robarbanco", new RobBankCommand(), "rp");
            this.Register("robarcajero", new RobATMCommand(), "rp");
            this.Register("robartienda", new RobartiendaCommand(), "rp");
            this.Register("norobarbanco", new RobBankCommand(true), "rp");
            this.Register("norobarcajero", new RobATMCommand(true), "rp");
            this.Register("fuga", new FugaCommand(), "rp");

            this.Register("medicina", new MedicinaCommand(), "rp");
            this.Register("caramelos", new CaramelosCommand(), "rp");
            this.Register("botardrogas", new DisposeCommand(), "rp");
            this.Register("consumir", new SmokeCommand(), "rp");

            //dd
            this.Register("darpermisos", new GiveRightsCommand());
            this.Register("leer", new LearningCommand());
            this.Register("noleer", new NoleerCommand());
            this.Register("tirarbasura", new TirarBasuraCommand());

            // Purchasing Goods
            this.Register("comprarbalas", new BuyBulletsCommand(), "rp");
            //this.Register("bullets", new BuyBulletsCommand(), "", true);
            this.Register("comprarsaldo", new BuyCreditCommand(), "rp");
            //this.Register("credit", new BuyCreditCommand(), "", true);
            this.Register("comprarcombustible", new BuyFuelCommand(), "rp");
            //this.Register("fuel", new BuyFuelCommand(), "", true);
            this.Register("comprarticket", new BuyTicketCommand(), "rp");

            // Combat
            //this.Register("modocombate", new CombatModeCommand());
            this.Register("cmode", new CombatModeCommand(), "rp", true);
            this.Register("golpe", new HitCommand());
            //this.Register("sacar", new EquipCommand());
            this.Register("hechizo", new HechizosCommand(), "rp", true);
            this.Register("equipar", new EquipCommand(), "rp", true);
            //this.Register("guardar", new UnEquipCommand());
            this.Register("desequipar", new UnEquipCommand(), "rp", true);
            this.Register("disparar", new ShootCommand(), "rp");
            this.Register("recargar", new ReloadGunCommand(), "rp");
            this.Register("permiso", new PermisoCommand());
            this.Register("permisoweed", new PermisoWeedCommand());

            // Offers
            this.Register("dar", new GiveCommand(), "staff");
            this.Register("ctransferir", new CtransferirCommand(), "rp");
            this.Register("atransferir", new AtransferirCommand(), "rp");
            this.Register("ofertas", new OffersCommand(), "rp");
           //this.Register("ofrecer", new OfferCommand());
            this.Register("ofrecer", new SellCommand(), "rp");// Old OfferCommand            
            this.Register("aceptar", new AcceptCommand(), "rp");
            this.Register("rechazar", new DeclineCommand(), "rp");
            this.Register("vender", new SellCommand(), "rp");
            //this.Register("vender", new VenderCommand());
            //this.Register("acc", new AcceptWeaponCommand());
            this.Register("renunciar", new RenunciarCommand(), "job");

            // Police Related
            this.Register("llamarpolicia", new CallPoliceCommand(), "rp");
            this.Register("emergencia", new EmergenciaCommand(), "rp");
            this.Register("911", new CallPoliceCommand(), "rp");
            this.Register("fianza", new BailCommand(), "rp");
            this.Register("rendicion", new SurrenderCommand(), "rp");
            this.Register("buscados", new WantedListCommand(), "rp");
            this.Register("wl", new WantedListCommand());
            this.Register("escoltar", new EscortCommand(), "job");

            #region Basurero
            this.Register("descargarcamion", new ReturnBasuCommand());
            #endregion
            #region Mecanico
            this.Register("reparar", new SellCommand(), "job");
            this.Register("mamada", new SellCommand(), "rp");
            this.Register("revisar", new ReviewMecCommand(), "job");
            #endregion
            this.Register("crear", new CreateCommand());
            this.Register("servicio", new ServiceCommand());
            this.Register("mapa", new MapCommand());
            this.Register("map", new MapCommand());

            #region Camionero
            this.Register("cargas", new LoadsCamCommand(), "job");
            this.Register("cargarcamion", new CargarCamCommand(), "job");
            this.Register("depositarcarga", new DepositCamCommand(), "job");
            this.Register("entregarcamion", new ReturnCamCommand(), "job");
            this.Register("abandonarcarga", new LeaveCamCommand(), "job");
            #endregion

            // Court
            this.Register("juicio", new TrialCommand(), "rp");
            this.Register("votar", new VoteCommand(), "rp");

            // Toggles
            this.Register("apagartelefono", new ToggleTextsCommand());
            this.Register("desactivasusurros", new DisableWhispersCommand());
            this.Register("disablemimic", new DisableMimicCommand());

            // Self Interactions
            this.Register("manejar", new DriveCommand(), "vehicle");
            this.Register("detener", new DriveCommand(), "vehicle", true);
            //this.Register("nomanejar", new DriveCommand());
            this.Register("subir", new UpCommand(), "vehicle");
            this.Register("bajar", new DownCommand(), "vehicle");
            this.Register("abrircarro", new OpenCommand(), "vehicle");
            this.Register("cerrarcarro", new CloseCommand(), "vehicle");
            this.Register("localizar", new LocalizarCommand(), "vehicle");
            this.Register("misautos", new MyCarsCommand(), "vehicle");
            this.Register("comprarcarro", new BuyCarCommand(), "vehicle");
            this.Register("sit", new SitCommand());
            this.Register("stand", new StandCommand());
            this.Register("lay", new LayCommand());
            this.Register("dance", new DanceCommand());
            this.Register("me", new MeCommand());

            // Item Interaction
            this.Register("llamarenvio", new CallDeliveryCommand());
            this.Register("comer", new EatCommand(), "rp");
            this.Register("agarrar", new AgarrarCommand(), "rp");
            //this.Register("kevlar", new KevlarCommand());
            this.Register("beber", new DrinkCommand(), "rp");
            this.Register("entrenar", new WorkoutCommand(), "rp");
            this.Register("plantar", new PlaceCommand(), "rp");
            this.Register("place", new PlaceCommand());
            this.Register("colocar", new PlaceCommand(), "rp");
            //this.Register("reparar", new PlaceCommand());
            this.Register("jugar", new JugarCommand(), "rp");
            this.Register("llorar", new LlorarCommand(), "rp");
            this.Register("manos", new ManosCommand(), "rp");
            this.Register("reir", new ReirCommand(), "rp");

            // Marriage Interaction
            this.Register("casarse", new MarryCommand(), "rp");
            this.Register("hijo", new HijoCommand(), "rp");
            this.Register("embarazar", new EmbarazarCommand(), "rp");
            //this.Register("propose", new MarryCommand(), "", true);
            this.Register("divorcio", new DivorceCommand(), "rp");
            this.Register("abandonar", new AbandonarCommand(), "rp");
            this.Register("sexo", new SexCommand(), "rp");

            // User Interaction
            this.Register("comprar", new BuyCommand(), "rp");
            this.Register("baul", new BaulCommand(), "vehicle");
            this.Register("usarbidon", new UseBidonCommand(), "vehicle");
            this.Register("combustible", new BuyFuelCommand(), "vehicle");
            this.Register("llenartanque", new BuyFuelFillCommand(), "vehicle");
            this.Register("cachetada", new SlapCommand(), "rp");
            this.Register("acariciar", new AcariciarCommand(), "rp");
            this.Register("eyacular", new EyacularCommand(), "rp");
            this.Register("besar", new KissCommand(), "rp");
            this.Register("mear", new MearCommand(), "rp");
            this.Register("masturbarse", new MasturbarseCommand(), "rp");
            this.Register("tocar", new AgarratetaCommand(), "rp");
            this.Register("estado", new EstadoCommand());
            this.Register("suicidar", new SuicidarCommand(), "rp");
            this.Register("secuestrar", new SecuestrarCommand(), "rp");
            this.Register("escupir", new EscupirCommand(), "rp");
            this.Register("tequiero", new TequieroCommand(), "rp");
            this.Register("teamo", new TeamoCommand(), "rp");
            this.Register("patear", new PatearCommand(), "rp");
            this.Register("oral", new OralCommand(), "rp");
            this.Register("anal", new AnalCommand(), "rp");
            this.Register("abrazar", new HugCommand(), "rp");
            this.Register("violar", new RapeCommand(), "rp");
            this.Register("nalgada", new NalgadaCommand(), "rp");
            this.Register("coquetear", new CoquetearCommand(), "rp");
            this.Register("masaje", new MasajeCommand(), "rp");


            // Apartment
            this.Register("kick", new KickCommand());
            this.Register("wired", new WiredCommand());
            this.Register("roomkick", new RoomKickCommand());
            this.Register("pickall", new PickAllCommand());
            //this.Register("chooser", new ChooserCommand());
            this.Register("comprarcasa", new BuyApartmentCommand());
            this.Register("ponerprecio", new SetPriceommand(), "rp");

            this.Register("entrar", new EnterCommand());
            this.Register("salir", new ExitCommand());

            // Events
            this.Register("eventstore", new PurchaseEventCommand(), "eventlog");

            // Gambling
            this.Register("apostar", new GamblingCommand(), "eventlog");

            // Bounties
            this.Register("recompensa", new AddBountyCommand(), "rp");
            this.Register("norecompensa", new RemoveBountyCommand(), "rp");
            this.Register("rlista", new BountyListCommand(), "rp");

            // Translation
            this.Register("translate", new TranslateCommand(), "rp");
            this.Register("stranslate", new StopTranslateCommand(), "rp");

            // Misc
            // this.Register("ayuda", new HelpCommand());
            this.Register("ayuda", new HelpCommand());
            this.Register("helpamb", new AmbassadorHelpCommand());
            this.Register("ride", new RideCommand());

            #region Phones
            this.Register("minumero", new MyNumberCommand());
            this.Register("sms", new SmsCommand());
            this.Register("whats", new WhatsCommand());
            #endregion  

        }

        /// <summary>
        /// User set of gang commands
        /// </summary>
        private void RegisterUsersGangs()
        {
            this.Register("ginfo", new GangInfoCommand(), "gang");
            this.Register("glist", new GangListCommand(), "gang");
            this.Register("turfs", new GangTurfsCommand(), "gang");
            this.Register("gcreate", new GangCreateCommand(), "gang");
            this.Register("ginvite", new GangInviteCommand(), "gang");
            this.Register("gleave", new GangLeaveCommand(), "gang");
            this.Register("gkick", new GangKickCommand(), "gang");
            this.Register("gmsj", new GangMessageCommand(), "gang");
            this.Register("gcapture", new GangCaptureCommand(), "gang");
            this.Register("gbackup", new GangBackupCommand(), "gang");
            this.Register("grank", new GangRankCommand(), "gang");
            this.Register("gtransfer", new GangTransferCommand(), "gang");
            this.Register("gheal", new GangHealCommand(), "gang");
            this.Register("delgang", new DeleteGangCommand(), "gang");
        }

        /// <summary>
        /// User set of job commands
        /// </summary>
        private void RegisterUsersJobs()
        {
            // General
            this.Register("trabajar", new StartWorkCommand(), "job");
            this.Register("notrabajar", new StopWorkCommand(), "job");
            this.Register("empresas", new CorpListCommand(), "job");
            this.Register("infoempresas", new CorpInfoCommand(), "job");
            this.Register("promover", new PromoteCommand(), "job");
            this.Register("degradar", new DemoteCommand(), "job");
            this.Register("sendhome", new SendhomeCommand(), "job");
            this.Register("contratar", new HireCommand(), "job");
            this.Register("despedir", new FireCommand(), "job");
            this.Register("verminutos", new CheckMinutesCommand(), "job");

            // Hospital
            this.Register("revivir", new DischargeCommand(), "job");
            //this.Register("aceptarmuerte", new AcceptDeathCommand());
            this.Register("curar", new HealCommand(), "job");
            this.Register("ayudar", new AyudarCommand(), "job");
            //this.Register("curar", new CurarCommand(), "joblog");
            this.Register("vacuna", new VacunaCommand(), "job");
            this.Register("pinchar", new PincharCommand(), "job");
            this.Register("ponerchaleco", new PonerchalecoCommand(), "rp");
            this.Register("comprarchaleco", new ComprarChalecoCommand(), "rp");
            //this.Register("chaleco", new ChalecopoliciaCommand(), "joblog");
            this.Register("explosivos", new ExplosivosCommand(), "rp");
            this.Register("hidratar", new HidratacionCommand(), "rp");
            //this.Register("hidratacion", new HidratacionCommand(), "joblog", true);
            


            // Police
            this.Register("radio", new RadioAlertCommand(), "job");
            this.Register("r", new RadioAlertCommand(), "job");
            this.Register("tradio", new ToggleRadioAlertCommand(), "job");
            //this.Register("toggleradio", new ToggleRadioAlertCommand(), "joblog");
            this.Register("buscar", new LawCommand(), "job");
            this.Register("kevlar", new KevlarCommand(), "job");
            this.Register("nobuscar", new UnLawCommand(), "job");
            this.Register("paralizar", new StunCommand(), "job");
            this.Register("desparalizar", new UnStunCommand(), "job");
           /* this.Register("spray", new StunCommand(), "joblog");
            this.Register("nospray", new UnStunCommand(), "joblog");*/
            this.Register("esposar", new CuffCommand(), "job");
            this.Register("noesposar", new UnCuffCommand(), "job");
            this.Register("cateo", new SearchCommand(), "job");
            //this.Register("catear", new SearchCommand(), "joblog");
            this.Register("arrestar", new ArrestCommand(), "job");
            this.Register("liberar", new ReleaseCommand(), "job");
            this.Register("ptrial", new PoliceTrialCommand(), "job");
            this.Register("unptrial", new PoliceTrialCommand(), "job", true);
            this.Register("limpiarlista", new ClearWantedCommand(), "job");
            //this.Register("cw", new ClearWantedCommand(), "joblog", true);
            this.Register("flashbang", new FlashBangCommand(), "job");
            this.Register("refuerzos", new BackupCommand(), "job");
            //this.Register("ref", new BackupCommand(), "job");
            this.Register("carinfo", new CheckCarInfoCommand(), "job");
            //this.Register("infocar", new CheckCarInfoCommand(), "joblog");

            // Restaurant & Cafe
            this.Register("servir", new ServeCommand(), "job");

            // Banking
            this.Register("abrircuenta", new OpenAccountCommand(), "job");
            this.Register("versaldo", new CheckBalanceCommand(), "job");

            // Clothing
            this.Register("descuento", new DiscountCommand(), "job");

            /*// Heticos
            this.Register("pasajero", new PasajeroCommand());
            this.Register("subir", new SubirCommand());*/

        }

        /// <summary>
        /// VIP set of commands
        /// </summary>
        private void RegisterVIP()
        {
            // Free fire
            //this.Register("poner", new PonerCommand(), "vip");
           
            this.Register("fastwalk", new FastwalkCommand(), "vip");
            this.Register("push", new PushCommand(), "vip");
            this.Register("pull", new PullCommand(), "vip");
            this.Register("flagme", new FlagMeCommand(), "vip");
            //this.Register("setsh", new SetSHCommand(), "vip");
            this.Register("stopsh", new StopSHCommand(), "vip");
            this.Register("opendimmer", new OpenDimmerCommand(), "vip");
            //this.Register("odimmer", new OpenDimmerCommand(), "vip", true);
            this.Register("vipa", new VIPAlertCommand(), "vip");
            //this.Register("va", new VIPAlertCommand(), "vip", true);
            //this.Register("v", new VIPAlertCommand(), "vip", true);
            this.Register("vipalerta", new ToggleVIPAlertCommand(), "vip");
            /*this.Register("toggleva", new ToggleVIPAlertCommand(), "vip", true);
            this.Register("togglev", new ToggleVIPAlertCommand(), "vip", true);*/
            this.Register("moonwalk", new MoonwalkCommand(), "vip");
        }

        /// <summary>
        /// Ambassador set of commands
        /// </summary>
        private void RegisterAmbassadors()
        {
            this.Register("aa", new AmbassadorAlertCommand(), "ambassadorlog");
            this.Register("atrabajar", new AmbassadorOnDutyCommand(), "ambassadorlog");
            this.Register("aofftrabajar", new AmbassadorOffDutyCommand(), "ambassadorlog");
        }

        /// <summary>
        /// Trial Moderator set of commands
        /// </summary>
        private void RegisterTrialModerators()
        {
            this.Register("sa", new StaffAlertCommand(), "staff");
            this.Register("strabajar", new OnDutyCommand(), "staff");
            this.Register("offstrabajar", new OffDutyCommand(), "staff");
        }

        /// <summary>
        /// Moderator set of commands
        /// </summary>
        private void RegisterModerators()
        {
            this.Register("alert", new AlertCommand(), "staff");
            this.Register("ban", new BanCommand(), "staff");
            this.Register("mute", new MuteCommand(), "staff");
            this.Register("unmute", new UnmuteCommand(), "staff");
            this.Register("userinfo", new UserInfoCommand(), "staff");
            this.Register("update", new UpdateCommand(), "staff");
            this.Register("poll", new PollCommand(), "staff");
            this.Register("givespecial", new GiveSpecialReward(), "staff");
        }

        /// <summary>
        /// Senior Moderator set of commands
        /// </summary>
        private void RegisterSeniorModerators()
        {
            this.Register("ha", new HotelAlertCommand(), "staff");
            this.Register("wha", new WhisperHotelAlertCommand(), "staff");
            this.Register("nha", new NoticeHotelAlertCommand(), "staff");
            this.Register("ipban", new IPBanCommand(), "staff");
            this.Register("roomalert", new RoomAlertCommand(), "staff");
            this.Register("roommute", new RoomMuteCommand(), "staff");
            this.Register("roomunmute", new RoomUnmuteCommand(), "staff");
            this.Register("summon", new SummonCommand(), "staff");
            this.Register("follow", new FollowCommand(), "staff");
            this.Register("unload", new UnloadCommand(), "staff");
            this.Register("senduser", new SendUserCommand(), "staff");
            this.Register("dartrabajo", new SuperHireCommand(), "staff");
        }

        /// <summary>
        /// Administrator set of commands
        /// </summary>
        private void RegisterAdministrators()
        {
            this.Register("boveda", new VaultCommand(), "staff");
            this.Register("at", new AdminTaxiCommand(), "staff");
            this.Register("deleteroom", new DeleteRoomCommand(), "staff");
            this.Register("setz", new SetSHCommand(), "staff");
            this.Register("hal", new HALCommand(), "staff");
            this.Register("mip", new MIPCommand(), "staff");
            this.Register("rpstats", new RPStatsCommand(), "staff");
            this.Register("rpweapons", new RPWeaponsCommand(), "staff");
            this.Register("rpfarming", new RPFarmingStatsCommand(), "staff");
            this.Register("override", new OverrideCommand(), "staff");
            this.Register("teleport", new TeleportCommand(), "staff");
            this.Register("spull", new SuperPullCommand(), "staff");
            this.Register("spush", new SuperPushCommand(), "staff");
            this.Register("eventha", new EventAlertCommand(), "staff");
            this.Register("restore", new RestoreCommand(), "staff");
            this.Register("adminrelease", new AdminReleaseCommand(), "staff");
            this.Register("adminjail", new AdminJailCommand(), "staff");
            this.Register("roomrestore", new RoomRestoreCommand(), "staff");
            this.Register("roomrelease", new RoomReleaseCommand(), "staff");
            this.Register("roomheal", new RoomHealCommand(), "staff");
            this.Register("warptome", new WarpToMeCommand(), "staff");
            this.Register("warpmeto", new WarpMeToCommand(), "staff");
            this.Register("blacklist", new BlackListCommand(), "staff");
            this.Register("unblacklist", new UnBlackListCommand(), "staff");
            this.Register("coordbot", new BotRPCommand(), "staff");
        }

        /// <summary>
        /// Manager set of commands
        /// </summary>
        private void RegisterManagers()
        {

            this.Register("givebadge", new GiveBadgeCommand(), "staff");
            this.Register("roombadge", new RoomBadgeCommand(), "staff");
            this.Register("massbadge", new MassBadgeCommand(), "staff");
            this.Register("globalgive", new GlobalGiveCommand(), "staff");
            this.Register("freeze", new FreezeCommand(), "staff");
            this.Register("unfreeze", new UnFreezeCommand(), "staff");
            this.Register("flagother", new FlagOtherCommand(), "staff");
            this.Register("flag", new FlagOtherCommand(), "staff", true);
            this.Register("mimic", new MimicCommand(), "staff");
            this.Register("togglewhispers", new ToggleWhispersCommand(), "staff");
            this.Register("disconnect", new DisconnectCommand(), "staff");
            this.Register("dc", new DisconnectCommand(), "staff", true);
            this.Register("purge", new PurgeCommand(), "staff");
            this.Register("purga", new PurgeCommand(), "staff");
            this.Register("checklottery", new StopEventCommand(), "staff", true);
            this.Register("accountcheck", new AccountCheckCommand(), "staff");
            this.Register("checkaccount", new AccountCheckCommand(), "staff", true);
            this.Register("namecheck", new NameCheckCommand(), "staff");
            this.Register("checkname", new NameCheckCommand(), "staff", true);
            this.Register("summonstaff", new SummonStaffCommand(), "staff");
            this.Register("checkpoll", new CheckPollCommand(), "staff");
            this.Register("pollcheck", new CheckPollCommand(), "staff", true);
            this.Register("warpalltome", new WarpAllToMeCommand(), "staff");
            this.Register("sendroom", new SendRoomCommand(), "staff");
            this.Register("freezeroom", new FreezeRoomCommand(), "staff");
            this.Register("unfreezeroom", new UnFreezeRoomCommand(), "staff");
            //this.Register("wonline", new WOnlineCommand(), "staff");
            this.Register("makebota", new MakeBotActionCommand(), "staff");
            this.Register("quitarwhatsapp", new BanChatterCommand(), "staff");
            this.Register("darwhatsapp", new UnBanChatterCommand(), "staff");
            this.Register("deletechat", new DeleteChatCommand(), "staff");
            this.Register("tlock", new TLockCommand(), "staff");
        }

        /// <summary>
        /// Developer set of commands
        /// </summary>
        private void RegisterDevelopers()
        {
            this.Register("bubble", new BubbleCommand(), "staff");
            this.Register("handitem", new HandItemCommand(), "staff");
            this.Register("enable", new EnableCommand(), "staff");
            this.Register("coords", new CoordsCommand(), "staff");
            this.Register("setspeed", new SetSpeedCommand(), "staff");
            this.Register("startquestion", new StartQuestionCommand(), "staff");
            this.Register("kickbots", new KickBotsCommand(), "staff");
            this.Register("kickpets", new KickPetsCommand(), "staff");
            this.Register("disablediagonal", new DisableDiagonalCommand(), "staff");
            this.Register("room", new RoomCommand(), "staff");
            this.Register("bot", new BotCommand(), "staff");
            this.Register("activebots", new ActiveBotsCommand(), "staff");
            this.Register("fixweapons", new FixWeaponsCommand(), "staff");
            this.Register("whispertile", new SetWhisperTileCommand(), "staff");
            this.Register("page", new HtmlPageCommand(), "staff");
            this.Register("upage", new HtmlUPageCommand(), "staff");
            this.Register("uipage", new HtmlUIPageCommand(), "staff");
            this.Register("rpage", new HtmlRPageCommand(), "staff");
            this.Register("maintenance", new MaintenanceCommand(), "staff");
            this.Register("maint", new MaintenanceCommand(), "staff", true);

            //this.Register("todo", new ToDoCommand(), "staff");
            //this.Register("todoadd", new ToDoCommand(), "staff");
            //this.Register("addtodo", new ToDoCommand(), "staff", true);
            //this.Register("tda", new ToDoCommand(), "staff", true);
            //this.Register("tododel", new ToDoCommand(), "staff");
            //this.Register("tododelete", new ToDoCommand(), "staff", true);
            //this.Register("deltodo", new ToDoCommand(), "staff", true);
            //this.Register("deletetodo", new ToDoCommand(), "staff", true);
            //this.Register("tdd", new ToDoCommand(), "staff", true);

        }

        /// <summary>
        /// Owner set of commands
        /// </summary>
        private void RegisterOwners()
        {
            //this.Register("fastwalk", new FastwalkCommand(), "staff");
            this.Register("forcesit", new ForceSitCommand(), "staff");
            this.Register("forcelay", new ForceLayCommand(), "staff");
            this.Register("allaroundme", new AllAroundMeCommand(), "staff");
            this.Register("alleyesonme", new AllEyesOnMeCommand(), "staff");
            this.Register("massdance", new MassDanceCommand(), "staff");
            this.Register("massenable", new MassEnableCommand(), "staff");
            this.Register("summonall", new SummonAllCommand(), "staff");
            this.Register("releaseall", new ReleaseAllCommand(), "staff");
            this.Register("restoreall", new RestoreAllCommand(), "staff");
            this.Register("invisible", new InvisibleCommand(), "staff");
            this.Register("visible", new VisibleCommand(), "staff");
            this.Register("massact", new MassActionCommand(), "staff");
            this.Register("unidle", new UnIdleCommand(), "staff");
            this.Register("unban", new UnBanCommand(), "staff");
        }

        /// <summary>
        /// Special Right set of commands
        /// </summary>
        private void RegisterSpecialRights()
        {
            this.Register("makepet", new MakePetCommand(), "staff");
            this.Register("transformall", new TransformAllCommand(), "staff");
            this.Register("roomtransform", new RoomMakePetCommand(), "staff");
            this.Register("summonpets", new SummonPetsCommand(), "staff");
            this.Register("pet", new PetTransformCommand(), "staff");
            this.Register("colour", new ColourChangeCommand(), "staff");
            this.Register("color", new ColourChangeCommand(), "staff", true);
            this.Register("changeuclass", new ChangeUClassCommand(), "staff");
            //this.Register("sfastwalk", new SuperFastwalkCommand(), "staff");
            //this.Register("mpu", new MPUCommand(), "staff");
            this.Register("makesay", new MakeSayCommand(), "staff");
            this.Register("sayall", new SayAllCommand(), "staff");
            this.Register("coins", new GiveCoinsCommand(), "staff");
            this.Register("duckets", new GiveDucketsCommand(), "staff");
            this.Register("diamonds", new GiveDiamondsCommand(), "staff");
            this.Register("rcoins", new TakeCoinsCommand(), "staff");
            this.Register("rduckets", new TakeDucketsCommand(), "staff");
            this.Register("rdiamonds", new TakeDiamondsCommand(), "staff");
            this.Register("epoints", new GiveEventPointsCommand(), "staff");
            this.Register("rank", new GiveRankCommand(), "staff");
            this.Register("kill", new KillCommand(), "staff");
            this.Register("setstat", new SetStatCommand(), "staff");
            this.Register("sethp", new SetStatCommand(), "staff", true);
            this.Register("snap", new KillCommand(), "staff");
            this.Register("setenergy", new SetStatCommand(), "staff", true);
            this.Register("sethunger", new SetStatCommand(), "staff", true);
            this.Register("sethygiene", new SetStatCommand(), "staff", true);
            this.Register("givevip", new GiveVIPCommand(), "staff");
            this.Register("takevip", new TakeVIPCommand(), "staff");
            this.Register("banvip", new BanVIPCommand(), "staff");
            this.Register("unbanvip", new UnBanVIPCommand(), "staff");
        }
        #endregion

        /// <summary>
        /// Registers a Chat Command.
        /// </summary>
        /// <param name="CommandText">Text to type for this command.</param>
        /// <param name="Command">The command to execute.</param>
        public void Register(string CommandText, IChatCommand Command, string Type = "", bool IsAlias = false)
        {
            if (IsAlias && !this._aliases.Contains(CommandText))
                this._aliases.Add(CommandText);

            switch (Type.ToLower())
            {
                case "job":
                case "joblog":
                    {
                        this._commands.Add(CommandText, Command);
                        this._jobcommands.Add(CommandText, Command);
                        break;
                    }
                case "gang":
                    {
                        this._commands.Add(CommandText, Command);
                        this._gangcommands.Add(CommandText, Command);
                        break;
                    }
                case "rp":
                    {
                        this._commands.Add(CommandText, Command);
                        this._rpCommands.Add(CommandText, Command);
                        break;
                    }
                case "vehicle":
                    {
                        this._commands.Add(CommandText, Command);
                        this._vehiclescommands.Add(CommandText, Command);
                        break;
                    }
                case "vip":
                    {
                        this._commands.Add(CommandText, Command);
                        this._vipcommands.Add(CommandText, Command);
                        break;
                    }
                case "staff":
                    {
                        this._commands.Add(CommandText, Command);
                        this._staffcommands.Add(CommandText, Command);
                        break;
                    }
                case "ambassadorlog":
                    {
                        this._commands.Add(CommandText, Command);
                        this._ambassadorcommands.Add(CommandText, Command);
                        break;
                    }
                case "eventlog":
                    {
                        this._commands.Add(CommandText, Command);
                        this._eventcommands.Add(CommandText, Command);
                        break;
                    }
                default:
                    {
                        this._commands.Add(CommandText, Command);
                        break;
                    }
            }
        }

        public static string MergeParams(string[] Params, int Start)
        {
            var Merged = new StringBuilder();
            for (int i = Start; i < Params.Length; i++)
            {
                if (i > Start)
                    Merged.Append(" ");
                Merged.Append(Params[i]);
            }

            return Merged.ToString();
        }

        public static string GenerateRainbowText(string Name)
        {
            StringBuilder NewName = new StringBuilder();

            string[] Colours = { "FF0000", "FFA500", "FFFF00", "008000", "0000FF", "800080" };

            int Count = 0;
            int Count2 = 0;
            while (Count < Name.Length)
            {
                NewName.Append("<font color='#" + Colours[Count2] + "'>" + Name[Count] + "</font>");

                Count++;
                Count2++;

                if (Count2 >= 6)
                    Count2 = 0;
            }

            return NewName.ToString();
        }

        public void LogCommand(int UserId, string Data, string MachineId, string Type)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                if (Type.ToLower() == "staff")
                    dbClient.SetQuery("INSERT INTO `command_logs_staff` (`user_id`,`data_string`,`machine_id`, `timestamp`) VALUES (@UserId,@Data,@MachineId,@Timestamp)");
                else if (Type.ToLower() == "ambassador")
                    dbClient.SetQuery("INSERT INTO `command_logs_ambassador` (`user_id`,`data_string`,`machine_id`, `timestamp`) VALUES (@UserId,@Data,@MachineId,@Timestamp)");
                else if (Type.ToLower() == "job")
                    dbClient.SetQuery("INSERT INTO `command_logs_jobs` (`user_id`,`data_string`,`machine_id`, `timestamp`) VALUES (@UserId,@Data,@MachineId,@Timestamp)");
                else if (Type.ToLower() == "vip")
                    dbClient.SetQuery("INSERT INTO `command_logs_vip` (`user_id`,`data_string`,`machine_id`, `timestamp`) VALUES (@UserId,@Data,@MachineId,@Timestamp)");
                else if (Type.ToLower() == "event")
                    dbClient.SetQuery("INSERT INTO `command_logs_events` (`user_id`,`data_string`,`machine_id`, `timestamp`) VALUES (@UserId,@Data,@MachineId,@Timestamp)");
                else
                    dbClient.SetQuery("INSERT INTO `command_logs_users` (`user_id`,`data_string`,`machine_id`, `timestamp`) VALUES (@UserId,@Data,@MachineId,@Timestamp)");
                dbClient.AddParameter("UserId", UserId);
                dbClient.AddParameter("Data", Data);
                dbClient.AddParameter("MachineId", MachineId);
                dbClient.AddParameter("Timestamp", PolarEnvironment.GetUnixTimestamp());
                dbClient.RunQuery();
            }
        }

        public bool TryGetCommand(string Command, out IChatCommand IChatCommand)
        {
            return this._commands.TryGetValue(Command, out IChatCommand);
        }
    }
}