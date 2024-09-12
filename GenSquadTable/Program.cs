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
        [Option('i', "input_units", Separator = ',', Required = false, HelpText = "input custom units", Default = null)]
        public IEnumerable<string> InputUnits { get; set; }
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

    private static readonly List<string> Title = new List<string>{ "名字","槽位","code","价格","血量","速度","攻速"
        ,"攻击距离","伤害","伤害范围","是否单体","对空伤害","目标类别" };

    private class CustomRow : ReadonlyRow
    {
        private SquadData data;
        private LuaTable conf;
        private List<object> confKeys = null;
        private Dictionary<string,(LuaTable,LuaTable)> weaponDefs;
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
                        weaponDefs = new Dictionary<string, (LuaTable, LuaTable)>();
                        var weaponsDef = (LuaTable)realConf["weapondefs"];
                        foreach (var k in weaponsDef.Keys)
                        {
                            if (((string)k).IndexOf("dummy") >= 0 || ((string)k).IndexOf("bogus") >= 0)
                                continue;
                            var weapon = FindWeapon((string)k, (LuaTable)realConf["weapons"]);
                            weaponDefs.Add((string)k, ((LuaTable)weaponsDef[k],weapon));
                        }
                    }
                }
            }
            
        }

        private LuaTable FindWeapon(string name, LuaTable table)
        {
            if (table == null) return null;
            foreach (var k in table.Keys)
            {
                var weapon = (LuaTable)table[k];
                string def = null;
                if (weapon != null && weapon["def"] != null && (def = (string)weapon["def"]) != null && def.ToLower() == name)
                    return weapon;
            }
            return null;
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
                    case "攻速": return GetWeaponVal("reloadtime",0);
                    case "速度": return realConf?["speed"] ?? 0;
                    case "攻击距离": return GetWeaponVal("range", 0);
                    case "伤害": return GetDamage("default", "-");
                    case "对空伤害": return GetDamage("vtol", "-");
                    case "目标类别": return GetWeaponVal2("onlytargetcategory","-",(a) => MapCategory((string)a));
                    case "伤害范围": return GetWeaponVal("areaofeffect", 0);
                    case "是否单体": return GetWeaponVal("impactonly", 0,(b)=> Convert.ToUInt32(b) == 1 ? "是":"否");
                }
                return "";
            }
        }

        private object GetDamage(string ty, object def, char separator = '/')
        {
            if (weaponDefs == null || weaponDefs.Count == 0)
                return def;
            var sb = new StringBuilder();
            var i = 0;
            foreach (var weapon in weaponDefs)
            {
                object v = ((LuaTable)weapon.Value.Item1["damage"])?[ty] ?? def;
                sb.Append(v);
                if (i < weaponDefs.Count - 1)
                    sb.Append(separator);
                ++i;
            }
            return sb.ToString();
        }

        private object MapCategory(string category)
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

        private object GetWeaponVal2(string key, object def, Func<object, object> mapFunc = null, char separator = '/')
        {
            if (weaponDefs == null || weaponDefs.Count == 0)
                return mapFunc != null ? mapFunc(def) : def;
            var sb = new StringBuilder();
            var i = 0;
            foreach (var weapon in weaponDefs)
            {
                object v = null;
                if (weapon.Value.Item2 == null)
                    v = def;
                else
                    v = weapon.Value.Item2[key];
                sb.Append(mapFunc != null ? mapFunc(v) : v);
                if (i < weaponDefs.Count - 1)
                    sb.Append(separator);
                ++i;
            }
            return sb.ToString();
        }

        private object GetWeaponVal(string key, object def, Func<object, object> mapFunc = null,char separator = '/')
        {
            if (weaponDefs == null || weaponDefs.Count == 0)
                return mapFunc != null ? mapFunc(def) : def;
            var sb = new StringBuilder();
            var i = 0;
            foreach (var weapon in weaponDefs)
            {
                var v = weapon.Value.Item1[key];
                sb.Append(mapFunc != null ? mapFunc(v) : v);
                if(i < weaponDefs.Count - 1)
                    sb.Append(separator);
                ++i;
            }
            return sb.ToString();
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
        var unitPath = Path.Combine(obj.LuaRootDir, "units");


        if (obj.InputUnits != null && obj.InputUnits.Count() > 0)
        {
            foreach(var it in obj.InputUnits)
            {
                var conf = LoadConfig(FindLuaFile(it, unitPath),lua,it);
                if (conf == null)
                    continue;
                rows.Add(new CustomRow(null, conf));
            }
        }
        else
        {
            foreach (var it in SquadDataMgr.GetInstance().Dict)
            {
                if (obj.OnlyNormal && it.Value.Type_e != EType.Normal)
                    continue;
                rows.Add(new CustomRow(it.Value, LoadConfig(FindLuaFile(it.Value.PB, unitPath), lua, it.Value.PB)));
            }
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




