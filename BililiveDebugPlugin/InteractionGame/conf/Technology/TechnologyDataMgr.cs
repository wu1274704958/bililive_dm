
using System;
using ProtoBuf;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
            
namespace conf.Technology
            {
public enum EType {Normal = 0,}
[ProtoContract]
public  partial class TechnologyData
            {
[ProtoMember(1)] public System.Int32 Id { get; private set; }
[ProtoMember(2)] public System.String Country { get; private set; }
[ProtoMember(3)] public System.String Name { get; private set; }
[ProtoMember(4)] public System.Double Price { get; private set; }
[ProtoMember(5)] public System.Double Score { get; private set; }
[ProtoMember(6)] public System.String Key { get; private set; }
[ProtoMember(7)] public System.Int32 Type { get; private set; }
public EType Type_e => (EType)Type;
}

[ProtoContract]
public  partial class TechnologyDataMgr
            {

[ProtoMember(1)] private Dictionary<System.Int32,TechnologyData> _dict = new Dictionary<System.Int32, TechnologyData>();
public IReadOnlyDictionary<System.Int32,TechnologyData> Dict => _dict; 
public TechnologyData Get(System.Int32 id) => _dict.TryGetValue(id, out var t) ? t : null;
private static TechnologyDataMgr _instance = null;
public static TechnologyDataMgr GetInstance()=> _instance;
private static FileInfo _lastReadFile = null;
protected TechnologyDataMgr() { }
public static void InitInstance(FileInfo file)
{
    _instance = null;
    using (FileStream fs = file.Open(FileMode.Open, FileAccess.Read))
    {
        _instance = Serializer.DeserializeWithLengthPrefix<TechnologyDataMgr>(fs, PrefixStyle.Fixed32);
        Debug.Assert(_instance != null,"Load Config TechnologyData failed at "+file.FullName);
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
public static void AppendData(Int32 id,TechnologyData d)
{
    if (_instance == null)
        _instance = new TechnologyDataMgr();
    Debug.Assert(_instance._dict.ContainsKey(id) == false,"Append Same TechnologyData id = " + id);
    _instance._dict.Add(id,d);
}
            }
}
