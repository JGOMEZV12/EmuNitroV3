using System;
using System.Linq;
using System.Collections.Generic;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Bots.Manager;
using Polar.HabboRoleplay.Bots;
using Polar.Utilities;
using System.Threading;

namespace Polar.HabboRoleplay.Misc
{
    public static class HuntManager
    {
        private static Timer _huntTimer;
        private static bool _isRunning = false;

        public static void Initialize()
        {
            if (_isRunning) return;
            _isRunning = true;
            _huntTimer = new Timer(OnTick, null, 30000, 60000); // Ticks every minute
        }

        private static void OnTick(object state)
        {
            try
            {
                var rooms = PolarEnvironment.GetGame().GetRoomManager().GetRooms();
                foreach (var room in rooms)
                {
                    if (room.HuntZoneEnabled)
                    {
                        ProcessHuntRoom(room);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error
            }
        }

        private static void ProcessHuntRoom(Room room)
        {
            int currentHuntPets = room.GetRoomUserManager().GetRoleplayBots().Count(b => b.GetBotRoleplay() != null && b.GetBotRoleplay().Motto.Contains("[CAZA]"));

            if (currentHuntPets < 5) // Limit per room
            {
                SpawnHuntPet(room);
            }
        }

        private static void SpawnHuntPet(Room room)
        {
            CryptoRandom random = new CryptoRandom();

            // Randomly choose a pet type from 1 to 19 (based on provided types)
            int petType = random.Next(1, 20);
            string look = PetFigureForType(petType);
            string name = GetPetNameForType(petType);

            var WalkableSquare = room.GetGameMap().GetRandomWalkableSquare();

            // Create a temporary bot instance
            int botId = random.Next(900000, 999999); // Temporary unique ID

            // Pet data format: type|race|color
            string[] lookParts = look.Split(' ');
            string petData = lookParts[0] + "|1|FFFFFF";

            RoleplayBot huntBot = new RoleplayBot(
                botId, 1, name, "M", look, "[CAZA] Mascota Salvaje", 100, 100, 10, 1, room.Id, WalkableSquare.X, WalkableSquare.Y, 0, 0,
                "pet", RoleplayBotAIType.PET, 5, 3, 2, 0, true, false, false, 0, "none", "none", true, 0, "1,5", 0, petData
            );

            // Register it in CachedRoleplayBots so DeployBotByID works
            if (!RoleplayBotManager.CachedRoleplayBots.ContainsKey(botId))
            {
                RoleplayBotManager.CachedRoleplayBots.TryAdd(botId, huntBot);
            }

            RoleplayBotManager.DeployBotByID(botId, "default", room.Id);
        }

        public static string GetPetNameForType(int type)
        {
            Random rand = new Random();
            string[] names;

            switch (type)
            {
                case 1: // Bear
                    names = new[] { "Oso_Feroz", "Garra_Blanca", "Kodiak_Salvaje", "Ursa" };
                    break;
                case 2: // Lion
                    names = new[] { "Simba_Salvaje", "Rey_Selva", "Garra_Dorada", "Mufasa" };
                    break;
                case 3: // Rhino
                    names = new[] { "Cuerno_Roto", "Tanque_Gris", "Rino_Bravo", "Embestida" };
                    break;
                case 4: // Dragon
                    names = new[] { "Aliento_Fuego", "Escama_Roja", "Draco_Salvaje", "Fafnir" };
                    break;
                case 5: // Monkey
                    names = new[] { "Mono_Alisto", "Chimpance_Loco", "Garra_Agil", "Kong_Pequeño" };
                    break;
                case 6: // Horse
                    names = new[] { "Rayo_Veloz", "Crines_Negras", "Corcel_Libre", "Relampago" };
                    break;
                case 7: // Bunny
                    names = new[] { "Salto_Rapido", "Conejo_Pillo", "Orejas_Largas", "Tambor" };
                    break;
                case 8: // Pigeon
                    names = new[] { "Vuelo_Alto", "Pluma_Gris", "Mensajera_Loca", "Torcaza" };
                    break;
                case 9: // Demon Monkey
                    names = new[] { "Mono_Infernal", "Garra_Oscura", "Mandril_Poseido", "Azazel" };
                    break;
                case 10: // Baby Bear
                    names = new[] { "Osezno_Tierno", "Pequeña_Garra", "Peluche_Bravo", "Baloo" };
                    break;
                case 11: // Gnome
                    names = new[] { "Gnomo_Enojon", "Barba_Larga", "Duende_Maligno", "Gnomo_Cazador" };
                    break;
                case 12: // Kitten
                    names = new[] { "Gatito_Cazador", "Zarpas_Pequeñas", "Michi_Salvaje", "Felix" };
                    break;
                case 13: // Piglet
                    names = new[] { "Cerdito_Valiente", "Puerquito_Gordo", "Bacon_Salvaje", "Oink_Oink" };
                    break;
                case 14: // Haloompa
                    names = new[] { "Haloompa_Misterioso", "Enano_Magico", "Criatura_Extraña", "Umpa_Lumpa" };
                    break;
                case 15: // Pterosaur
                    names = new[] { "Ptero_Veloz", "Ala_Gigante", "Pico_Afilado", "Sombra_Aerea" };
                    break;
                case 16: // Velociraptor
                    names = new[] { "Garra_Veloz", "Raptor_Feroz", "Cazador_Prehistorico", "Blue" };
                    break;
                case 17: // Cow
                    names = new[] { "Vaca_Loca", "Cuernos_Largos", "Mu_Salvaje", "Lola" };
                    break;
                case 18: // Penguin
                    names = new[] { "Pico_Frio", "Pinguino_Deslizante", "Aleta_Negra", "Pingu" };
                    break;
                case 19: // Elephant
                    names = new[] { "Trompa_Larga", "Gigante_Gris", "Dumbo_Salvaje", "Colmillo_Blanco" };
                    break;
                default:
                    names = new[] { "Mascota_Salvaje", "Animal_Bravo", "Criatura_Libre", "Bestia" };
                    break;
            }

            return names[rand.Next(names.Length)];
        }

        public static string PetFigureForType(int Type)
        {
            Random _random = new Random();

            switch (Type)
            {
                default:
                #region Bear Figures
                case 1:
                    {
                        int RandomNumber = _random.Next(1, 4);
                        switch (RandomNumber)
                        {
                            default:
                            case 1:
                                return "4 2 e4feff 2 2 -1 0 3 -1 0";
                            case 2:
                                return "4 3 e4feff 2 2 -1 0 3 -1 0";
                            case 3:
                                return "4 1 eaeddf 2 2 -1 0 3 -1 0";
                            case 4:
                                return "4 0 ffffff 2 2 -1 0 3 -1 0";
                        }
                    }
                #endregion

                #region Lion Figures
                case 2:
                    {
                        int RandomNumber = _random.Next(1, 11);
                        switch (RandomNumber)
                        {
                            default:
                            case 1:
                                return "6 0 ffffff 2 2 -1 0 3 -1 0";
                            case 2:
                                return "6 1 ffffff 2 2 -1 0 3 -1 0";
                            case 3:
                                return "6 2 ffffff 2 2 -1 0 3 -1 0";
                            case 4:
                                return "6 3 ffffff 2 2 -1 0 3 -1 0";
                            case 5:
                                return "6 4 ffffff 2 2 -1 0 3 -1 0";
                            case 6:
                                return "6 0 ffd8c9 2 2 -1 0 3 -1 0";
                            case 7:
                                return "6 5 ffffff 2 2 -1 0 3 -1 0";
                            case 8:
                                return "6 11 ffffff 2 2 -1 0 3 -1 0";
                            case 9:
                                return "6 2 ffe49d 2 2 -1 0 3 -1 0";
                            case 10:
                                return "6 11 ff9ae 2 2 -1 0 3 -1 0";
                            case 11:
                                return "6 2 ff9ae 2 2 -1 0 3 -1 0";
                        }
                    }
                #endregion

                #region Rhino Figures
                case 3:
                    {
                        int RandomNumber = _random.Next(1, 7);
                        switch (RandomNumber)
                        {
                            default:
                            case 1:
                                return "7 5 aeaeae 2 2 -1 0 3 -1 0";
                            case 2:
                                return "7 7 ffc99a 2 2 -1 0 3 -1 0";
                            case 3:
                                return "7 5 cccccc 2 2 -1 0 3 -1 0";
                            case 4:
                                return "7 5 9adcff 2 2 -1 0 3 -1 0";
                            case 5:
                                return "7 5 ff7d6a 2 2 -1 0 3 -1 0";
                            case 6:
                                return "7 6 cccccc 2 2 -1 0 3 -1 0";
                            case 7:
                                return "7 0 cccccc 2 2 -1 0 3 -1 0";
                        }
                    }
                #endregion

                #region Dragon Figures
                case 4:
                    {
                        int RandomNumber = _random.Next(1, 6);
                        switch (RandomNumber)
                        {
                            default:
                            case 1:
                                return "12 0 ffffff 2 2 -1 0 3 -1 0";
                            case 2:
                                return "12 1 ffffff 2 2 -1 0 3 -1 0";
                            case 3:
                                return "12 2 ffffff 2 2 -1 0 3 -1 0";
                            case 4:
                                return "12 3 ffffff 2 2 -1 0 3 -1 0";
                            case 5:
                                return "12 4 ffffff 2 2 -1 0 3 -1 0";
                            case 6:
                                return "12 5 ffffff 2 2 -1 0 3 -1 0";
                        }
                    }
                #endregion

                #region Monkey Figures
                case 5:
                    {
                        int RandomNumber = _random.Next(1, 14);
                        switch (RandomNumber)
                        {
                            default:
                            case 1:
                                return "14 0 ffffff 2 2 -1 0 3 -1 0";
                            case 2:
                                return "14 1 ffffff 2 2 -1 0 3 -1 0";
                            case 3:
                                return "14 2 ffffff 2 2 -1 0 3 -1 0";
                            case 4:
                                return "14 3 ffffff 2 2 -1 0 3 -1 0";
                            case 5:
                                return "14 6 ffffff 2 2 -1 0 3 -1 0";
                            case 6:
                                return "14 4 ffffff 2 2 -1 0 3 -1 0";
                            case 7:
                                return "14 5 ffffff 2 2 -1 0 3 -1 0";
                            case 8:
                                return "14 7 ffffff 2 2 -1 0 3 -1 0";
                            case 9:
                                return "14 8 ffffff 2 2 -1 0 3 -1 0";
                            case 10:
                                return "14 9 ffffff 2 2 -1 0 3 -1 0";
                            case 11:
                                return "14 10 ffffff 2 2 -1 0 3 -1 0";
                            case 12:
                                return "14 11 ffffff 2 2 -1 0 3 -1 0";
                            case 13:
                                return "14 12 ffffff 2 2 -1 0 3 -1 0";
                            case 14:
                                return "14 13 ffffff 2 2 -1 0 3 -1 0";
                        }
                    }
                #endregion

                #region Horse Figures
                case 6:
                    {
                        int RandomNumber = _random.Next(1, 20);
                        switch (RandomNumber)
                        {
                            default:
                            case 1:
                                return "15 2 ffffff 2 2 -1 0 3 -1 0";
                            case 2:
                                return "15 3 ffffff 2 2 -1 0 3 -1 0";
                            case 3:
                                return "15 4 ffffff 2 2 -1 0 3 -1 0";
                            case 4:
                                return "15 5 ffffff 2 2 -1 0 3 -1 0";
                            case 5:
                                return "15 6 ffffff 2 2 -1 0 3 -1 0";
                            case 6:
                                return "15 7 ffffff 2 2 -1 0 3 -1 0";
                            case 7:
                                return "15 8 ffffff 2 2 -1 0 3 -1 0";
                            case 8:
                                return "15 9 ffffff 2 2 -1 0 3 -1 0";
                            case 9:
                                return "15 10 ffffff 2 2 -1 0 3 -1 0";
                            case 10:
                                return "15 11 ffffff 2 2 -1 0 3 -1 0";
                            case 11:
                                return "15 12 ffffff 2 2 -1 0 3 -1 0";
                            case 12:
                                return "15 13 ffffff 2 2 -1 0 3 -1 0";
                            case 13:
                                return "15 14 ffffff 2 2 -1 0 3 -1 0";
                            case 14:
                                return "15 15 ffffff 2 2 -1 0 3 -1 0";
                            case 15:
                                return "15 16 ffffff 2 2 -1 0 3 -1 0";
                            case 16:
                                return "15 17 ffffff 2 2 -1 0 3 -1 0";
                            case 17:
                                return "15 78 ffffff 2 2 -1 0 3 -1 0";
                            case 18:
                                return "15 77 ffffff 2 2 -1 0 3 -1 0";
                            case 19:
                                return "15 79 ffffff 2 2 -1 0 3 -1 0";
                            case 20:
                                return "15 80 ffffff 2 2 -1 0 3 -1 0";
                        }
                    }
                #endregion

                #region Bunny Figures
                case 7:
                    {
                        int RandomNumber = _random.Next(1, 8);
                        switch (RandomNumber)
                        {
                            default:
                            case 1:
                                return "17 1 ffffff";
                            case 2:
                                return "17 2 ffffff";
                            case 3:
                                return "17 3 ffffff";
                            case 4:
                                return "17 4 ffffff";
                            case 5:
                                return "17 5 ffffff";
                            case 6:
                                return "18 0 ffffff";
                            case 7:
                                return "19 0 ffffff";
                            case 8:
                                return "20 0 ffffff";
                        }
                    }
                #endregion

                #region Pigeon Figures (White & Black)
                case 8:
                    {
                        int RandomNumber = _random.Next(1, 3);
                        switch (RandomNumber)
                        {
                            default:
                            case 1:
                                return "21 0 ffffff";
                            case 2:
                                return "22 0 ffffff";
                        }
                    }
                #endregion

                #region Demon Monkey Figures
                case 9:
                    {
                        int RandomNumber = _random.Next(1, 3);
                        switch (RandomNumber)
                        {
                            default:
                            case 1:
                                return "23 0 ffffff";
                            case 2:
                                return "23 1 ffffff";
                            case 3:
                                return "23 3 ffffff";
                        }
                    }
                #endregion

                #region Baby Bear Figures
                case 10:
                    {
                        int RandomNumber = _random.Next(1, 3);
                        switch (RandomNumber)
                        {
                            default:
                            case 1:
                                return "24 0 ffffff";
                            case 2:
                                return "24 1 ffffff";
                        }
                    }
                #endregion

                #region Gnome Figures
                case 11:
                    {
                        int RandomNumber = _random.Next(1, 4);
                        switch (RandomNumber)
                        {
                            default:
                            case 1:
                                return "26 1 ffffff 5 0 -1 0 4 402 5 3 301 4 1 101 2 2 201 3";
                            case 2:
                                return "26 1 ffffff 5 0 -1 0 1 102 13 3 301 4 4 401 5 2 201 3";
                            case 3:
                                return "26 6 ffffff 5 1 102 8 2 201 16 4 401 9 3 303 4 0 -1 6";
                            case 4:
                                return "26 30 ffffff 5 0 -1 0 3 303 4 4 401 5 1 101 2 2 201 3";
                        }
                    }
                #endregion

                #region Kitten Figures
                case 12:
                    {
                        int RandomNumber = _random.Next(1, 3);
                        switch (RandomNumber)
                        {
                            default:
                            case 1:
                                return "28 0 ffffff";
                            case 2:
                                return "28 1 ffffff";
                        }
                    }
                #endregion

                #region Piglet Figures
                case 13:
                    {
                        int RandomNumber = _random.Next(1, 3);
                        switch (RandomNumber)
                        {
                            default:
                            case 1:
                                return "30 0 ffffff";
                            case 2:
                                return "30 1 ffffff";
                        }
                    }
                #endregion


                #region Haloompa Figures
                case 14:
                    {
                        int RandomNumber = _random.Next(1, 3);
                        switch (RandomNumber)
                        {
                            default:
                            case 1:
                                return "31 0 ffffff";
                            case 2:
                                return "31 1 ffffff";
                        }
                    }
                #endregion

                #region Pterosaur Figures
                case 15:
                    {
                        int RandomNumber = _random.Next(1, 3);
                        switch (RandomNumber)
                        {
                            default:
                            case 1:
                                return "33 0 ffffff";
                            case 2:
                                return "33 1 ffffff";
                        }
                    }
                #endregion

                #region Velociraptor Figures
                case 16:
                    {
                        int RandomNumber = _random.Next(1, 3);
                        switch (RandomNumber)
                        {
                            default:
                            case 1:
                                return "34 0 ffffff";
                            case 2:
                                return "34 1 ffffff";
                        }
                    }
                #endregion

                #region Cow Figures
                case 17:
                    {
                        int RandomNumber = _random.Next(1, 3);
                        switch (RandomNumber)
                        {
                            default:
                            case 1:
                                return "35 0 ffffff";
                            case 2:
                                return "35 1 ffffff";
                        }
                    }
                #endregion

                #region Penguin Figures
                case 18:
                    {
                        int RandomNumber = _random.Next(1, 3);
                        switch (RandomNumber)
                        {
                            default:
                            case 1:
                                return "36 0 ffffff";
                            case 2:
                                return "36 1 ffffff";
                        }
                    }
                #endregion

                #region Elephant Figures
                case 19:
                    {
                        int RandomNumber = _random.Next(1, 3);
                        switch (RandomNumber)
                        {
                            default:
                            case 1:
                                return "37 0 ffffff";
                            case 2:
                                return "37 1 ffffff";
                        }
                    }
                #endregion

            }
        }
    }
}