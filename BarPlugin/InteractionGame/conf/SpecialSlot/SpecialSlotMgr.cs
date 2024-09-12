
using System;
using ProtoBuf;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
            
namespace conf.SpecialSlot
            {

[ProtoContract]
public  partial class SpecialSlot
            {
[ProtoMember(1)] public System.Int32 Id { get; private set; }
[ProtoMember(2)] public System.Int32 Slot { get; private set; }
[ProtoMember(3)] public System.String AddExpr { get; private set; }
[ProtoMember(4)] public System.String TestExpr { get; private set; }
[ProtoMember(5)] public System.Int32 Group { get; private set; }
}

[ProtoContract]
public  partial class SpecialSlotMgr
            {

[ProtoMember(1)] private Dictionary<System.Int32,SpecialSlot> _dict = new Dictionary<System.Int32, SpecialSlot>();
public IReadOnlyDictionary<System.Int32,SpecialSlot> Dict => _dict; 
public SpecialSlot Get(System.Int32 id) => _dict.TryGetValue(id, out var t) ? t : null;
private static SpecialSlotMgr _instance = null;
public static SpecialSlotMgr GetInstance()=> _instance;
private static FileInfo _lastReadFile = null;
protected SpecialSlotMgr() { }
public static void InitInstance(FileInfo file)
{
    _instance = null;
    using (FileStream fs = file.Open(FileMode.Open, FileAccess.Read))
    {
        _instance = Serializer.DeserializeWithLengthPrefix<SpecialSlotMgr>(fs, PrefixStyle.Fixed32);
        Debug.Assert(_instance != null,"Load Config SpecialSlot failed at "+file.FullName);
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
public static void AppendData(System.Int32 id,SpecialSlot d)
{
    if (_instance == null)
        _instance = new SpecialSlotMgr();
    Debug.Assert(_instance._dict.ContainsKey(id) == false,"Append Same SpecialSlot id = " + id);
    _instance._dict.Add(id,d);
}
            }
}
