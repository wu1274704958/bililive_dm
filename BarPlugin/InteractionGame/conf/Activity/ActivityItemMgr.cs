
using System;
using ProtoBuf;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
            
namespace conf.Activity
            {
public enum EItemType {Settlement = 1,SignIn = 2,}public enum ECyclePeriodType {None = 0,Daily = 1,Monthly = 2,Annually = 3,Always = 4,}
[ProtoContract]
public  partial class ActivityItem
            {
[ProtoMember(1)] public System.String Id { get; private set; }
[ProtoMember(2)] public System.Boolean Cumulative  { get; private set; }
[ProtoMember(3)] public System.Int32 Priority { get; private set; }
[ProtoMember(4)] public System.Int32 Type { get; private set; }
public EItemType Type_e => (EItemType)Type;
[ProtoMember(5)] public System.Collections.Generic.Dictionary<System.String,System.Int32> Gifts { get; private set; }
[ProtoMember(6)] public System.Collections.Generic.Dictionary<System.Int32,System.Single> Multiplier { get; private set; }
[ProtoMember(7)] public System.Boolean LunarCalendar { get; private set; }
[ProtoMember(8)] public System.Int32 CyclePeriodType { get; private set; }
public ECyclePeriodType CyclePeriodType_e => (ECyclePeriodType)CyclePeriodType;
[ProtoMember(9)] public System.DateTime StartTime { get; private set; }
[ProtoMember(10)] public System.DateTime EndTime { get; private set; }
}

[ProtoContract]
public  partial class ActivityItemMgr
            {

[ProtoMember(1)] private Dictionary<System.String,ActivityItem> _dict = new Dictionary<System.String, ActivityItem>();
public IReadOnlyDictionary<System.String,ActivityItem> Dict => _dict; 
public ActivityItem Get(System.String id) => _dict.TryGetValue(id, out var t) ? t : null;
private static ActivityItemMgr _instance = null;
public static ActivityItemMgr GetInstance()=> _instance;
private static FileInfo _lastReadFile = null;
protected ActivityItemMgr() { }
public static void InitInstance(FileInfo file)
{
    _instance = null;
    using (FileStream fs = file.Open(FileMode.Open, FileAccess.Read))
    {
        _instance = Serializer.DeserializeWithLengthPrefix<ActivityItemMgr>(fs, PrefixStyle.Fixed32);
        Debug.Assert(_instance != null,"Load Config ActivityItem failed at "+file.FullName);
        _lastReadFile = file;
        
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
public static void AppendData(System.String id,ActivityItem d)
{
    if (_instance == null)
        _instance = new ActivityItemMgr();
    Debug.Assert(_instance._dict.ContainsKey(id) == false,"Append Same ActivityItem id = " + id);
    _instance._dict.Add(id,d);
}
            }
}
