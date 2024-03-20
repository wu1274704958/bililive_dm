
using System;
using ProtoBuf;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
            
namespace conf.Squad
            {
public enum ESquadType {Normal = 0,Villager = 1,SiegeAttacker = 2,BuildingAttacker = 3,Trebuchet = 4,HuiHuiPao = 5,MultiSquad = 6,}public enum EType {Normal = 0,Gift = 1,Special = 2,CountrySpec = 3,Placeholder = 4,Hide = 100,}
[ProtoContract]
public  partial class SquadData
            {
[ProtoMember(1)] public System.Int32 Id { get; private set; }
[ProtoMember(2)] public System.String Name { get; private set; }
[ProtoMember(3)] public System.Double Price { get; private set; }
[ProtoMember(4)] public System.Double Score { get; private set; }
[ProtoMember(5)] public System.Int32 SquadType { get; private set; }
public ESquadType SquadType_e => (ESquadType)SquadType;
[ProtoMember(6)] public System.Double TrainTime { get; private set; }
[ProtoMember(7)] public System.Int32 Type { get; private set; }
public EType Type_e => (EType)Type;
[ProtoMember(8)] public System.Int32 NextLevel { get; private set; }
public Squad.SquadData NextLevelRef => Squad.SquadDataMgr.GetInstance().Get(NextLevel);
[ProtoMember(9)] public System.Double UpLevelPrice { get; private set; }
[ProtoMember(10)] public System.String PB { get; private set; }
[ProtoMember(11)] public System.Boolean IsBase { get; private set; }
[ProtoMember(12)] public System.Collections.Generic.Dictionary<System.String,System.Int32> CountryMap { get; private set; }
[ProtoMember(13)] public System.Int32 OverloadId { get; private set; }
[ProtoMember(14)] public System.String OverloadCountry { get; private set; }
[ProtoMember(15)] public System.Collections.Generic.Dictionary<System.String,System.Double> OverloadPrice { get; private set; }
[ProtoMember(16)] public System.Collections.Generic.Dictionary<System.String,System.Double> OverloadUpPrice { get; private set; }
[ProtoMember(17)] public System.Collections.Generic.Dictionary<System.String,System.Double> OverloadPriceMult { get; private set; }
[ProtoMember(18)] public System.Collections.Generic.Dictionary<System.String,System.Double> OverloadUpPriceMult { get; private set; }
[ProtoMember(19)] public System.Int16 SquadCount { get; private set; }
}

[ProtoContract]
public  partial class SquadDataMgr
            {

[ProtoMember(1)] private Dictionary<System.Int32,SquadData> _dict = new Dictionary<System.Int32, SquadData>();
public IReadOnlyDictionary<System.Int32,SquadData> Dict => _dict; 
public SquadData Get(System.Int32 id) => _dict.TryGetValue(id, out var t) ? t : null;
private static SquadDataMgr _instance = null;
public static SquadDataMgr GetInstance()=> _instance;
private static FileInfo _lastReadFile = null;
protected SquadDataMgr() { }
public static void InitInstance(FileInfo file)
{
    _instance = null;
    using (FileStream fs = file.Open(FileMode.Open, FileAccess.Read))
    {
        _instance = Serializer.DeserializeWithLengthPrefix<SquadDataMgr>(fs, PrefixStyle.Fixed32);
        Debug.Assert(_instance != null,"Load Config SquadData failed at "+file.FullName);
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
public static void AppendData(Int32 id,SquadData d)
{
    if (_instance == null)
        _instance = new SquadDataMgr();
    Debug.Assert(_instance._dict.ContainsKey(id) == false,"Append Same SquadData id = " + id);
    _instance._dict.Add(id,d);
}
            }
}
