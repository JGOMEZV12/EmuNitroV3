using System;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms.AI.Speech;
using Polar.HabboHotel.Rooms.AI.Types;
using Polar.HabboHotel.Items.Utilities;
using Polar.Utilities;
using System.Drawing;

namespace Polar.HabboHotel.Rooms.AI
{
    public class RoomBot : IDisposable
    {
        // ─────────────────────────────────────
        //  Propiedades públicas (antes campos)
        // ─────────────────────────────────────
        public int Id { get; private set; }
        public int BotId { get; private set; }
        public int VirtualId { get; set; }
        public int RoomId { get; private set; }
        public int DanceId { get; set; }
        public int OwnerID { get; private set; }
        public int Rot { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public double Z { get; set; }
        public int MinX { get; private set; }
        public int MinY { get; private set; }
        public int MaxX { get; private set; }
        public int MaxY { get; private set; }

        public string Gender { get; set; }
        public string Look { get; set; }
        public string Motto { get; set; }
        public string Name { get; set; }
        public string WalkingMode { get; set; }

        public BotAIType AiType { get; private set; }

        public bool AutomaticChat { get; set; }
        public int SpeakingInterval { get; set; }
        public bool MixSentences { get; set; }

        public bool ForcedMovement { get; set; }
        public int ForcedUserTargetMovement { get; set; }
        public Point TargetCoordinate { get; set; }
        public int TargetUser { get; set; }

        public int ChatBubble { get; set; }

        public RoomUser RoomUser { get; set; }

        // Nota: RandomSpeech (campo) comparte nombre con el tipo — se mantiene así por compatibilidad con el proyecto
        public List<RandomSpeech> RandomSpeech { get; private set; }

        // FIX: instancia única de CryptoRandom — no crear una nueva en cada llamada
        private static readonly CryptoRandom _rng = new CryptoRandom();

        private bool _disposed = false;

        // ─────────────────────────────────────
        //  Constructor
        // ─────────────────────────────────────
        public RoomBot(int botId, int roomId, string aiType, string walkingMode, string name, string motto,
            string look, int x, int y, double z, int rot, int minX, int minY, int maxX, int maxY,
            ref List<RandomSpeech> speeches, string gender, int dance, int ownerID,
            bool automaticChat, int speakingInterval, bool mixSentences, int chatBubble)
        {
            Id = botId;
            BotId = botId;
            RoomId = roomId;

            Name = name ?? string.Empty;
            Motto = motto ?? string.Empty;
            Look = look ?? string.Empty;

            // FIX: gender null-safe con fallback a "M"
            Gender = string.IsNullOrEmpty(gender) ? "M" : gender.ToUpper();

            AiType = BotUtility.GetAIFromString(aiType);
            WalkingMode = walkingMode ?? string.Empty;

            X = x;
            Y = y;
            Z = z;
            Rot = rot;

            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;

            VirtualId = -1;
            RoomUser = null;
            DanceId = dance;
            OwnerID = ownerID;

            AutomaticChat = automaticChat;
            SpeakingInterval = speakingInterval;
            MixSentences = mixSentences;

            ChatBubble = chatBubble;

            ForcedMovement = false;
            ForcedUserTargetMovement = 0;
            TargetCoordinate = new Point();
            TargetUser = 0;

            LoadRandomSpeech(speeches);
        }

        // ─────────────────────────────────────
        //  Propiedades calculadas
        // ─────────────────────────────────────
        public bool IsPet => AiType == BotAIType.PET;

        // ─────────────────────────────────────
        //  Speech
        // ─────────────────────────────────────
        public void LoadRandomSpeech(List<RandomSpeech> source)
        {
            RandomSpeech = new List<RandomSpeech>();

            if (source == null)
                return;

            foreach (RandomSpeech speech in source)
            {
                if (speech.BotID == BotId)
                    RandomSpeech.Add(speech);
            }
        }

        public RandomSpeech GetRandomSpeech()
        {
            if (RandomSpeech == null || RandomSpeech.Count == 0)
                return new RandomSpeech(string.Empty, 0);

            // FIX: usa el _rng estático en vez de instanciar CryptoRandom cada vez
            return RandomSpeech[_rng.Next(0, RandomSpeech.Count - 1)];
        }

        // ─────────────────────────────────────
        //  AI
        // ─────────────────────────────────────
        public BotAI GenerateBotAI(int virtualId)
        {
            switch (AiType)
            {
                case BotAIType.PET: return new PetBot(virtualId);
                case BotAIType.BARTENDER: return new BartenderBot(virtualId);
                case BotAIType.GENERIC:
                default: return new GenericBot(virtualId);
            }
        }

        // ─────────────────────────────────────
        //  IDisposable — patrón correcto
        // ─────────────────────────────────────
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                RandomSpeech?.Clear();
                RandomSpeech = null;
                RoomUser = null;

                // FIX: TargetCoordinate limpiado (faltaba en el original)
                TargetCoordinate = default(Point);
            }

            // Reset value types
            Id = 0;
            BotId = 0;
            VirtualId = 0;
            DanceId = 0;
            RoomId = 0;
            Rot = 0;
            X = 0;
            Y = 0;
            Z = 0;
            MinX = 0;
            MinY = 0;
            MaxX = 0;
            MaxY = 0;
            OwnerID = 0;
            SpeakingInterval = 0;
            ChatBubble = 0;
            ForcedMovement = false;
            ForcedUserTargetMovement = 0;
            TargetUser = 0;

            Gender = null;
            Look = null;
            Motto = null;
            Name = null;
            WalkingMode = null;

            _disposed = true;
        }
    }
}