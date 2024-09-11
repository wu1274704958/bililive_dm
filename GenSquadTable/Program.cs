using CommandLine;
using conf;
using conf.Squad;
using GenTable;
using GenTableImpl;
using KeraLua;
using NLua;
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

    private static readonly List<string> Title = new List<string>{ "名字","槽位","code","价格","血量","攻速","速度" };

    private class CustomRow : ReadonlyRow
    {
        private SquadData data;
        private LuaTable conf;

        public CustomRow(SquadData data, LuaTable conf)
        {
            this.data = data;
            this.conf = conf;
        }

        public object this[int index]
        {
            get
            {
                switch(Title[index])
                {
                    case "名字": return data.Name;
                    case "槽位": return data.Slot - 48;
                    case "code": return data.PB;
                    case "价格": return data.Price;
                    case "血量": return conf?["health"] ?? 0;
                    case "攻速": return GetReload();
                    case "速度": return conf?["speed"] ?? 0;
                }
                return "";
            }
        }

        private object GetReload()
        {
            throw new NotImplementedException();
        }

        public int Count => Title.Count;
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
        var res0 = result[0] as LuaTable;
        if(res0 != null)
        {
            var unit = res0[key] as LuaTable;
            return unit;
        }
        return null;
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




