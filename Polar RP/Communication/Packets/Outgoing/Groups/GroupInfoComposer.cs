using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Users;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Outgoing.Groups
{
    internal class GroupInfoComposer : ServerPacket
    {
        public Group Group { get; }
        public bool NewWindow { get; }
        public Habbo Habbo { get; }
        public DateTime Origin { get; }

        public GroupInfoComposer(Group group, GameClient session, bool newWindow = false)
            : base(ServerPacketHeader.GroupInfoMessageComposer)
        {
            Group = group;
            NewWindow = newWindow;
            Habbo = session.GetHabbo();
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            bool IsAdmin = false;
            if (Group.IsAdmin(Habbo.Id))
                IsAdmin = true;
            if (Habbo.GetPermissions().HasRight("corporation_rights"))
                IsAdmin = true;

            bool IsOwner = false;
            if (Group.CreatorId == Habbo.Id)
                IsOwner = true;
            if (Habbo.GetPermissions().HasRight("roleplay_corp_manager"))
                IsOwner = true;

            DateTime Origin = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Group.CreateTime);

            packet.WriteInteger(Group.Id);
            packet.WriteBoolean(true);
            packet.WriteInteger(Group.GroupType == GroupType.OPEN ? 0 : Group.GroupType == GroupType.LOCKED ? 1 : 2);
            packet.WriteString(Group.Name);
            packet.WriteString(Group.Description);
            packet.WriteString(Group.Badge);
            packet.WriteInteger(Group.RoomId);
            packet.WriteString((PolarEnvironment.GetGame().GetRoomManager().GenerateRoomData(Group.RoomId) == null) ? "No room found.." : PolarEnvironment.GetGame().GetRoomManager().GenerateRoomData(Group.RoomId).Name);    // room name
            packet.WriteInteger(Group.CreatorId == Habbo.Id ? 3 : Group.HasRequest(Habbo.Id) ? 2 : Group.IsMember(Habbo.Id) ? 1 : 0);
            packet.WriteInteger(Group.Id < 1000 ? (Group.Members.Count + 1) : Group.Members.Count); // Members
            packet.WriteBoolean(false);//?? CHANGED
            packet.WriteString(Origin.Day + "-" + Origin.Month + "-" + Origin.Year);
            packet.WriteBoolean(Group.CreatorId == Habbo.Id);
            packet.WriteBoolean(Group.IsAdmin(Habbo.Id)); // admin
            packet.WriteString(PolarEnvironment.GetUsernameById(Group.CreatorId));
            packet.WriteBoolean(NewWindow); // Show group info
            packet.WriteBoolean(Group.AdminOnlyDeco == 0); // Any user can place furni in home room
            packet.WriteInteger(Group.CreatorId == Habbo.Id ? Group.Requests.Count : Group.IsAdmin(Habbo.Id) ? Group.Requests.Count : Group.IsMember(Habbo.Id) ? 0 : 0); // Pending users
            //base.WriteInteger(0);//what the fuck
            packet.WriteBoolean(Group != null ? Group.ForumEnabled : true);//HabboTalk.
            /*if (Group.IsGang)
            {
                packet.WriteInteger(Group.Id);
                packet.WriteBoolean(true);
                packet.WriteInteger(Group.GroupType == GroupType.OPEN ? 0 : Group.GroupType == GroupType.LOCKED ? 1 : 2);
                packet.WriteString(Group.Name);
                packet.WriteString(Group.Description);
                packet.WriteString(Group.Badge);
                packet.WriteInteger(Group.RoomId);
                packet.WriteString((PolarEnvironment.GetGame().GetRoomManager().GenerateRoomData(Group.RoomId) == null) ? "No room found.." : PolarEnvironment.GetGame().GetRoomManager().GenerateRoomData(Group.RoomId).Name);    // room name
                packet.WriteInteger(Group.CreatorId == Habbo.Id ? 3 : Group.HasRequest(Habbo.Id) ? 2 : Group.IsMember(Habbo.Id) ? 1 : 0);
                packet.WriteInteger(Group.Id < 1000 ? (Group.Members.Count + 1) : Group.Members.Count); // Members
                packet.WriteBoolean(false);//?? CHANGED
                packet.WriteString(Origin.Day + "-" + Origin.Month + "-" + Origin.Year);
                packet.WriteBoolean(IsOwner); // Owner Check
                packet.WriteBoolean(IsAdmin); // Admin Check
                packet.WriteString(PolarEnvironment.GetUsernameById(Group.CreatorId));
                packet.WriteBoolean(NewWindow); // Show group info
                packet.WriteBoolean(Group.AdminOnlyDeco == 0); // Any user can place furni in home room
                packet.WriteInteger((IsOwner || IsAdmin) ? Group.Requests.Count : 0); // Pending users
                                                                                    //packet.WriteInteger(0);//what the fuck
                packet.WriteBoolean(Group != null ? Group.ForumEnabled : true);//HabboTalk.
            }
            else
            {
                packet.WriteInteger(Group.Id);
                packet.WriteBoolean(true);
                packet.WriteInteger(Group.GroupType == GroupType.OPEN ? 0 : Group.GroupType == GroupType.LOCKED ? 1 : 2);
                packet.WriteString(Group.Name);
                packet.WriteString(Group.Description);
                packet.WriteString(Group.Badge);
                packet.WriteInteger(Group.RoomId);
                packet.WriteString((PolarEnvironment.GetGame().GetRoomManager().GenerateRoomData(Group.RoomId) == null) ? "No room found.." : PolarEnvironment.GetGame().GetRoomManager().GenerateRoomData(Group.RoomId).Name);    // room name
                packet.WriteInteger(Group.CreatorId == Habbo.Id ? 3 : Group.HasRequest(Habbo.Id) ? 2 : Group.IsMember(Habbo.Id) ? 1 : 0);
                packet.WriteInteger(Group.Id < 1000 ? (Group.Members.Count + 1) : Group.Members.Count); // Members
                packet.WriteBoolean(false);//?? CHANGED
                packet.WriteString(Origin.Day + "-" + Origin.Month + "-" + Origin.Year);
                packet.WriteBoolean(IsOwner); // Owner Check
                packet.WriteBoolean(IsAdmin); // Admin Check
                packet.WriteString(PolarEnvironment.GetUsernameById(Group.CreatorId));
                packet.WriteBoolean(NewWindow); // Show group info
                packet.WriteBoolean(Group.AdminOnlyDeco == 0); // Any user can place furni in home room
                packet.WriteInteger((IsOwner || IsAdmin) ? Group.Requests.Count : 0); // Pending users
                                                                                    //packet.WriteInteger(0);//what the fuck
                packet.WriteBoolean(Group != null ? Group.ForumEnabled : true);//HabboTalk.
            }*/
        }
    }
}