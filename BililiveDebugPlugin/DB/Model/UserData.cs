using System.Collections.Generic;
using FreeSql.DataAnnotations;
using System;
using ProtoBuf;


namespace BililiveDebugPlugin.DB.Model
{

    public enum EUserType : uint
    {
        None,
        User,
        Admin
    }
    public enum EItemType : uint
    {
        None,
        Gift,
        Ticket
    }
    [ProtoBuf.ProtoContract]
    public class UserData
    {
        [ProtoMember(1)]
        [Column(IsPrimary = true)]
        public long Id { get;set;}
        [ProtoMember(2)]
        [Column(StringLength = 50)]
        public string Name{ get;set;}
        [ProtoMember(3)]
        [Column(StringLength = 255)]
        public string Icon{ get;set;}
        [ProtoMember(4)]
        public long Score{ get;set;}
        [ProtoMember(5)]
        public long Honor{ get;set;}
        [ProtoMember(6)]
        public int WinTimes{ get;set;}
        [ProtoMember(7)]
        public long SpawnSoldierNum{ get;set;}
        [ProtoMember(8)]
        [Column(MapType = typeof(uint))]
        public EUserType UserType{ get;set;}
        [ProtoMember(9)]
        public int Ext { get;set;}
        [ProtoMember(10)]
        //[Column(ServerTime = DateTimeKind.Local)]
        public DateTime  SignTime { get;set;}
        [ProtoMember(11)]
        public DateTime  UpdateTime { get;set;}

        public override string ToString()
        {
            return $"Id = {Id} Name = {Name} Icon = {Icon} Score = {Score} Honor = {Honor} WinTimes = {WinTimes} SpawnSoldierNum = {SpawnSoldierNum} UserType = {UserType} Ext = {Ext} SignTime = {SignTime}\n";
        }
    }

    public class ItemData
    {
        [Column(IsIdentity = true, IsPrimary = true)]
        public long Id { get;set;}
        public long OwnerId { get;set;}
        [Column(StringLength = 50)]
        public string Name{ get;set;}
        public int Count{ get;set;}
        [Column(MapType = typeof(uint))]
        public EItemType Type{ get;set;}
        public int Price{ get;set;}
        public int Ext{ get;set;}

        public static ItemData Create(string name, EItemType type, int price, int ext = 0)
        {
            return new ItemData(){ Name = name, Type = type, Price = price, Ext = ext };
        }
        public static ItemData Create(ItemData data,int count,long ownerId)
        {
            return new ItemData(){ Name = data.Name, Type = data.Type, Price = data.Price, Ext = data.Ext, Count = count , OwnerId = ownerId };
        }
        
        public override string ToString()
        {
            return $"Id = {Id} OwnerId = {OwnerId} Name = {Name} Count = {Count} Type = {Type} Price = {Price} Ext = {Ext}\n";
        }
    }

    public class SystemData
    {
        [Column(IsIdentity = true, IsPrimary = true)]
        public long Id { get;set;}
        public string StrValue{ get;set;}
        public int IntValue{ get;set;}
        public long LongValue{ get;set;}
        public DateTime DateTimeValue{ get;set;}
    }
}