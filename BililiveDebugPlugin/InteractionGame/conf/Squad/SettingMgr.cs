
using System;
using ProtoBuf;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
            
namespace conf.Squad
            {

[ProtoContract]
public  partial class Setting
            {
[ProtoMember(1)] public System.Int32 Id { get; private set; }
[ProtoMember(2)] public System.Collections.Generic.List<System.String> Country { get; private set; }
[ProtoMember(3)] public System.Int32 IntVal { get; private set; }
}

[ProtoContract]
public  partial class SettingMgr
            {

[ProtoMember(1)] private Dictionary<System.Int32,Setting> _dict = new Dictionary<System.Int32, Setting>();
public IReadOnlyDictionary<System.Int32,Setting> Dict => _dict; 
public Setting Get(System.Int32 id) => _dict.TryGetValue(id, out var t) ? t : null;
private static SettingMgr _instance = null;
public static SettingMgr GetInstance()=> _instance;
private static FileInfo _lastReadFile = null;
protected SettingMgr() { }
public static void InitInstance(FileInfo file)
{
    _instance = null;
    using (FileStream fs = file.Open(FileMode.Open, FileAccess.Read))
    {
        _instance = Serializer.DeserializeWithLengthPrefix<SettingMgr>(fs, PrefixStyle.Fixed32);
        Debug.Assert(_instance != null,"Load Config Setting failed at "+file.FullName);
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
public static void AppendData(Int32 id,Setting d)
{
    if (_instance == null)
        _instance = new SettingMgr();
    Debug.Assert(_instance._dict.ContainsKey(id) == false,"Append Same Setting id = " + id);
    _instance._dict.Add(id,d);
}
            }
}
