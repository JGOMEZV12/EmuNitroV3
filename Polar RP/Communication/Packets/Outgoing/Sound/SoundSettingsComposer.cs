using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Sound
{
    internal class SoundSettingsComposer : ServerPacket
    {
        public ICollection<int> ClientVolumes { get; }
        public bool ChatPreference { get; }
        public bool InvitesStatus { get; }
        public bool FocusPreference { get; }
        public int FriendBarState { get; }
        public SoundSettingsComposer(ICollection<int> ClientVolumes, Boolean ChatPreference, Boolean InvitesStatus, Boolean FocusPreference, int FriendBarState)
            : base(ServerPacketHeader.SoundSettingsMessageComposer)
        {
            this.ClientVolumes = ClientVolumes;
            this.ChatPreference = ChatPreference;
            this.InvitesStatus = InvitesStatus;
            this.FocusPreference = FocusPreference;
            this.FriendBarState = FriendBarState;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            foreach (int VolumeValue in ClientVolumes)
            {
                packet.WriteInteger(VolumeValue);
            }

            packet.WriteBoolean(ChatPreference);
            packet.WriteBoolean(InvitesStatus);
            packet.WriteBoolean(FocusPreference);
            packet.WriteInteger(FriendBarState);
            packet.WriteInteger(0);
            packet.WriteInteger(0);
        }
    }
}