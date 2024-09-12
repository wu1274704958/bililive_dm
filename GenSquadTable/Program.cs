using CommandLine;
using conf;
using conf.Squad;
using GenTable;
using GenTableImpl;
using KeraLua;
using NLua;
using OfficeOpenXml.Style;
using System.Drawing;
using System.Text;


internal class ProgramGenLua
{
    class Options
    {
        [Option('d', "dir", Required = false, HelpText = "table output dir", Default = ".")]
        public string CodeOutDir { get; set; }
        [Option('n', "name", Required = false, HelpText = "table file name", Default = "单位属性")]
        public string CodeFileName { get; set; }
        [Option('l', "lua_dir", Required = true, HelpText = "lua file root path", Default = null)]
        public string LuaRootDir { get; set; }
        [Option('o', "only_normal", Required = false, HelpText = "only export normal", Default = false)]
        public bool OnlyNormal { get; set; }
    }
    public static void Main(string[] args)
    {
        CommandLine.Parser.Default.ParseArguments<Options>(args)
            .WithParsed(RunOptions)
            .WithNotParsed(HandleParseError);
    }

    private static void HandleParseError(IEnumerable<Error> obj)
    {

    }

    protected static void WriteFile(string name, byte[] cnt, string suffix, string dir)
    {
        var delimiter = name.IndexOf('/');
        if (delimiter > 0)
        {
            dir = Path.Combine(dir, name.Substring(0, delimiter));
            name = name.Substring(delimiter + 1);
        }
        if (Directory.Exists(dir) == false)
            Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, name + suffix);
        File.WriteAllBytes(path, cnt);
    }

    private static readonly List<string> Title = new List<string>{ "名字","槽位","code","价格","血量","攻速","速度"
        ,"攻击距离","伤害","伤害范围","是否单体","对空伤害","目标类别" };

    private class CustomRow : ReadonlyRow
    {
        private SquadData data;
        private LuaTable conf;
        private List<object> confKeys = null;
        private Dictionary<string,LuaTable> weaponDefs;
        private LuaTable realConf;

        public CustomRow(SquadData data, LuaTable conf)
        {
            this.data = data;
            this.conf = conf;
            if (conf != null && conf.Keys.Count > 0)
            {
                confKeys = new List<object>();
                foreach(var key in conf.Keys)
                    confKeys.Add(key);
                if (confKeys.Count > 0)
                {
                    realConf = (LuaTable)conf[confKeys[0]];
                    if(realConf != null && realConf["weapondefs"] != null)
                    {
                        weaponDefs = new Dictionary<string, LuaTable>();
                        var weaponsTable = (LuaTable)realConf["weapondefs"];
                        foreach (var k in weaponsTable.Keys)
                        {
                            weaponDefs.Add((string)k, (LuaTable)weaponsTable[k]);
                        }
                    }
                }
            }
            
        }

        public object this[int index]
        {
            get
            {
                switch(Title[index])
                {
                    case "名字": return data?.Name ?? "";
                    case "槽位": return (data?.Slot ?? 0) - 48;
                    case "code":return data == null ? confKeys[0] : data.PB; 
                    case "价格": return data?.Price ?? 0;
                    case "血量": return realConf?["health"] ?? 0;
                    case "攻速": return GetFirstWeaponVal("reloadtime",0);
                    case "速度": return realConf?["speed"] ?? 0;
                    case "攻击距离": return GetFirstWeaponVal("range", 0);
                    case "伤害": return GetDamage("default", "-");
                    case "对空伤害": return GetDamage("vtol", "-");
                    case "目标类别": return GetOnlyTarget(1,"onlytargetcategory","");
                    case "伤害范围": return GetFirstWeaponVal("areaofeffect", 0);
                    case "是否单体": return Convert.ToInt32(GetFirstWeaponVal("impactonly", 0)) == 1;
                }
                return "";
            }
        }

        private object GetDamage(string ty, object def)
        {
            if (weaponDefs == null || weaponDefs.Count == 0)
                return def;
            int min = -1;
            foreach (var weapon in weaponDefs)
            {
                int v = Convert.ToInt32(((LuaTable)weapon.Value["damage"])?[ty] ?? -1);
                if(v > min)
                    min = v;
            }
            return min == -1 ? def : min;
        }

        private object MapCategory(string category,object def)
        {
            switch (category)
            {
                case "VTOL": return "对空";
                case "NOTSUB": return "非潜艇";
                case "SURFACE": return "对地";
                case "EMPABLE": return "电磁脉冲";
                case "NOTHOVER": return "非悬浮";
            }
            return category;
        }

        private object GetOnlyTarget(int idx, string key, object def)
        {
            LuaTable weapons = (LuaTable)realConf["weapons"];
            LuaTable c = null;
            if (weapons != null && (c = (LuaTable)weapons[idx]) != null && c[key] != null)
                return MapCategory((string)c[key], def);
            return def;
        }

        private object GetFirstWeaponVal(string key,object def)
        {
            if (weaponDefs == null || weaponDefs.Count == 0)
                return def;
            foreach (var weapon in weaponDefs)
                return weapon.Value[key] ?? def;
            return def;
        }

        public int Count => Title.Count;

        public ExcelFillStyle fillStyle => ExcelFillStyle.Solid;

        public Color color => (data?.Slot ?? 0) % 2 == 0 ? Color.LightCyan : Color.LightSkyBlue;
    }

    private static void RunOptions(Options obj)
    {
        SquadDataMgr.InitInstance(new FileInfo(Path.Combine(ConfigMgr.ConfigPath, "SquadData.dat")));
        
        List<ReadonlyRow> rows = new List<ReadonlyRow>();
        rows.Add(new TitleRow<string>(Title));
        var lua = new NLua.Lua();

        foreach(var it in SquadDataMgr.GetInstance().Dict)
        {
            if (obj.OnlyNormal && it.Value.Type_e != EType.Normal)
                continue;
            rows.Add(new CustomRow(it.Value,LoadConfig(FindLuaFile(it.Value.PB,Path.Combine(obj.LuaRootDir,"units")),lua,it.Value.PB)));
        }

        var table = GenTable.GenTable.Generate(rows,"Unit");
        if (table != null)
            WriteFile(obj.CodeFileName, table.GetAsByteArray(), ".xlsx", obj.CodeOutDir);
    }

    private static LuaTable LoadConfig(string path,NLua.Lua lua,string key)
    {
        if (path == null)
            return null;
        var code = File.ReadAllText(path);
        var result = lua.DoString(code);
        return result[0] as LuaTable;
    }

    private static string FindLuaFile(string pb,string dir)
    {
        if (!Directory.Exists(dir))
        {
            Console.WriteLine("Directory does not exist.");
            return null;
        }
        string[] files = Directory.GetFiles(dir, $"{pb}.lua");
        if (files.Length > 0)
        {
            return files[0];
        }
        string[] subDirectories = Directory.GetDirectories(dir);
        foreach (var subDir in subDirectories)
        {
            string result = FindLuaFile(pb, subDir);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }
}




