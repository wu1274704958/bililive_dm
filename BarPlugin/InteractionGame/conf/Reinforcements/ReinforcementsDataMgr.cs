
using System;
using ProtoBuf;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
            
namespace conf.Reinforcements
            {
public enum EType {Normal = 0,}
[ProtoContract]
public  partial class ReinforcementsData
            {
[ProtoMember(1)] public System.Int32 Id { get; private set; }
[ProtoMember(2)] public System.Collections.Generic.Dictionary<System.Int32,System.Double> SquadConf { get; private set; }
[ProtoMember(3)] public System.String Name { get; private set; }
[ProtoMember(4)] public System.Int32 Type { get; private set; }
public EType Type_e => (EType)Type;
[ProtoMember(5)] public System.Byte HpAdded { get; private set; }
[ProtoMember(6)] public System.Byte DamageAdded { get; private set; }
}

[ProtoContract]
public  partial class ReinforcementsDataMgr
            {

[ProtoMember(1)] private Dictionary<System.Int32,ReinforcementsData> _dict = new Dictionary<System.Int32, ReinforcementsData>();
public IReadOnlyDictionary<System.Int32,ReinforcementsData> Dict => _dict; 
public ReinforcementsData Get(System.Int32 id) => _dict.TryGetValue(id, out var t) ? t : null;
private static ReinforcementsDataMgr _instance = null;
public static ReinforcementsDataMgr GetInstance()=> _instance;
private static FileInfo _lastReadFile = null;
protected ReinforcementsDataMgr() { }
public static void InitInstance(FileInfo file)
{
    _instance = null;
    using (FileStream fs = file.Open(FileMode.Open, FileAccess.Read))
    {
        _instance = Serializer.DeserializeWithLengthPrefix<ReinforcementsDataMgr>(fs, PrefixStyle.Fixed32);
        Debug.Assert(_instance != null,"Load Config ReinforcementsData failed at "+file.FullName);
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
public static void AppendData(Int32 id,ReinforcementsData d)
{
    if (_instance == null)
        _instance = new ReinforcementsDataMgr();
    Debug.Assert(_instance._dict.ContainsKey(id) == false,"Append Same ReinforcementsData id = " + id);
    _instance._dict.Add(id,d);
}
            }
}
