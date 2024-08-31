
using System;
using ProtoBuf;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
            
namespace conf.Gift
            {
public enum EItemType {None = 0,Gift = 1,Ticket = 2,LimitedTime = 4,SpecialGift = 8,LimitedMonth = 16,}
[ProtoContract]
public  partial class GiftItem
            {
[ProtoMember(1)] public System.String Id { get; private set; }
[ProtoMember(2)] public System.Int32 Price { get; private set; }
[ProtoMember(3)] public System.Int32 ItemType { get; private set; }
public EItemType ItemType_e => (EItemType)ItemType;
[ProtoMember(4)] public System.Collections.Generic.Dictionary<System.String,System.Int32> Gifts { get; private set; }
[ProtoMember(5)] public System.Collections.Generic.Dictionary<System.String,System.Int32> ApplyGifts { get; private set; }
[ProtoMember(6)] public System.Collections.Generic.Dictionary<System.Int32,System.Int32> SpawnSquad { get; private set; }
[ProtoMember(7)] public System.Collections.Generic.Dictionary<System.Int32,conf.plugin.AnyArray> Functions { get; private set; }
[ProtoMember(8)] public System.TimeSpan Duration { get; private set; }
[ProtoMember(9)] public System.Int32 Ext { get; private set; }
}

[ProtoContract]
public  partial class GiftItemMgr
            {

[ProtoMember(1)] private Dictionary<System.String,GiftItem> _dict = new Dictionary<System.String, GiftItem>();
public IReadOnlyDictionary<System.String,GiftItem> Dict => _dict; 
public GiftItem Get(System.String id) => _dict.TryGetValue(id, out var t) ? t : null;
private static GiftItemMgr _instance = null;
public static GiftItemMgr GetInstance()=> _instance;
private static FileInfo _lastReadFile = null;
protected GiftItemMgr() { }
public static void InitInstance(FileInfo file)
{
    _instance = null;
    using (FileStream fs = file.Open(FileMode.Open, FileAccess.Read))
    {
        _instance = Serializer.DeserializeWithLengthPrefix<GiftItemMgr>(fs, PrefixStyle.Fixed32);
        Debug.Assert(_instance != null,"Load Config GiftItem failed at "+file.FullName);
        _lastReadFile = file;
        
                foreach(var kv in  _instance.Dict)
                {
                    if(kv.Value != null){if(kv.Value.Functions != null){
            foreach(var it in kv.Value.Functions)
            {
                it.Value.Init();
            }
            
}}  
                }
            
    }
}

public static void Reload()
{
    if(_lastReadFile == null) return;
    InitInstance(_lastReadFile);
}
public static void Save(FileInfo file)
{
    using (FileStream fs = file.Open(FileMode.OpenOrCreate, FileAccess.Write))
    {
        Serializer.SerializeWithLengthPrefix(fs, _instance, PrefixStyle.Fixed32);
    }
}
public static void AppendData(System.String id,GiftItem d)
{
    if (_instance == null)
        _instance = new GiftItemMgr();
    Debug.Assert(_instance._dict.ContainsKey(id) == false,"Append Same GiftItem id = " + id);
    _instance._dict.Add(id,d);
}
            }
}
