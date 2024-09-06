
using System;
using ProtoBuf;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
            
namespace conf.CommonConfig
            {
public enum EItemType {None = 0,Gift = 1,Ticket = 2,LimitedTime = 4,SpecialGift = 8,}
[ProtoContract]
public  partial class CommonConfig
            {
[ProtoMember(1)] public System.String Id { get; private set; }
[ProtoMember(2)] public System.Collections.Generic.Dictionary<System.String,System.Int32> DictSI { get; private set; }
[ProtoMember(3)] public System.Collections.Generic.Dictionary<System.String,System.Single> DictSF { get; private set; }
[ProtoMember(4)] public System.Collections.Generic.Dictionary<System.String,System.String> DictSS { get; private set; }
[ProtoMember(5)] public System.Collections.Generic.Dictionary<System.Int32,System.Int32> DictII { get; private set; }
[ProtoMember(6)] public System.Collections.Generic.Dictionary<System.Int32,System.Single> DictIF { get; private set; }
[ProtoMember(7)] public System.Collections.Generic.Dictionary<System.Int32,System.String> DictIS { get; private set; }
[ProtoMember(8)] public System.Collections.Generic.List<System.String> ARRS { get; private set; }
[ProtoMember(9)] public System.Collections.Generic.List<System.Int32> ARRI { get; private set; }
[ProtoMember(10)] public System.Collections.Generic.List<System.Single> ARRF { get; private set; }
[ProtoMember(11)] public System.Collections.Generic.Dictionary<System.String,System.Collections.Generic.KeyValuePair<System.Int32,System.Int32>> DictSII { get; private set; }
[ProtoMember(12)] public System.Collections.Generic.Dictionary<System.Int32,System.Collections.Generic.KeyValuePair<System.Int32,System.Int32>> DictIII { get; private set; }
}

[ProtoContract]
public  partial class CommonConfigMgr
            {

[ProtoMember(1)] private Dictionary<System.String,CommonConfig> _dict = new Dictionary<System.String, CommonConfig>();
public IReadOnlyDictionary<System.String,CommonConfig> Dict => _dict; 
public CommonConfig Get(System.String id) => _dict.TryGetValue(id, out var t) ? t : null;
private static CommonConfigMgr _instance = null;
public static CommonConfigMgr GetInstance()=> _instance;
private static FileInfo _lastReadFile = null;
protected CommonConfigMgr() { }
public static void InitInstance(FileInfo file)
{
    _instance = null;
    using (FileStream fs = file.Open(FileMode.Open, FileAccess.Read))
    {
        _instance = Serializer.DeserializeWithLengthPrefix<CommonConfigMgr>(fs, PrefixStyle.Fixed32);
        Debug.Assert(_instance != null,"Load Config CommonConfig failed at "+file.FullName);
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
public static void AppendData(System.String id,CommonConfig d)
{
    if (_instance == null)
        _instance = new CommonConfigMgr();
    Debug.Assert(_instance._dict.ContainsKey(id) == false,"Append Same CommonConfig id = " + id);
    _instance._dict.Add(id,d);
}
            }
}
