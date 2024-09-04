using CommandLine;
using conf;
using conf.Squad;
using System.Text;


internal class ProgramGenLua
{
    class Options
    {
        [Option('c', "code", Required = true, HelpText = "code output dir", Default = "code")]
        public string CodeOutDir { get; set; }
        [Option('n', "name", Required = false, HelpText = "code file name", Default = "UnitPrice")]
        public string CodeFileName { get; set; }
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

    protected static void WriteFile(string name, string cnt, string suffix, string dir)
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
        File.WriteAllText(path, cnt);
    }

    private static void RunOptions(Options obj)
    {
        SquadDataMgr.InitInstance(new FileInfo(Path.Combine(ConfigMgr.ConfigPath, "SquadData.dat")));
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("return {");
        foreach (var it in SquadDataMgr.GetInstance().Dict)
        {
            sb.AppendLine($"[\"{it.Value.PB}\"] = {it.Value.Price},");
        }
        sb.AppendLine("}");
        WriteFile(obj.CodeFileName, sb.ToString(), ".lua", obj.CodeOutDir);
    }
}




