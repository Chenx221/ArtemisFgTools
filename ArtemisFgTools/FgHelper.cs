using System.Reflection;
using System.Xml.Linq;

namespace ArtemisFgTools
{
    public class FgHelper
    {
        public class FgObject(string path, string head, List<string> fuku, List<List<string>> pose, Dictionary<string, List<string>> face)
        {
            public string Path { get; set; } = path;
            public string Head { get; set; } = head;
            public List<string> Fuku { get; set; } = fuku;
            public List<List<string>> Pose { get; set; } = pose;
            public Dictionary<string, List<string>> Face { get; set; } = face;
        }
        //{"fg",ch="零",size="z1",mx=40,mode=3,resize=1,path=":fg/rei/z1/",file="rei_z1a0200",face="a0001",head="rei_z1a",lv=4,id=11},
        public class FgRecord : IEquatable<FgRecord>
        {
            public string ChName { get; set; }
            public string Size { get; set; }
            public string File { get; set; }
            public string Face { get; set; }

            public override bool Equals(object? obj)
            {
                return Equals(obj as FgRecord);
            }

            public bool Equals(FgRecord? other)
            {
                return other != null &&
                       ChName == other.ChName &&
                       Size == other.Size &&
                       File == other.File &&
                       Face == other.Face;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(ChName, Size, File, Face);
            }
        }
        public static HashSet<FgRecord> FetchFgObjectsFromScript(string path)
        {
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException("The path does not exist.");
            else
            {
                List<string> fgScriptLine = [];
                foreach (string f in Directory.GetFiles(path, "*.ast"))
                {
                    using StreamReader sr = new(f);
                    string? line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("{\"fg\","))
                            fgScriptLine.Add(line);
                    }
                }
                if (fgScriptLine.Count == 0)
                    throw new Exception("No valid fg script line found.");
                else
                {
                    HashSet<FgRecord> fgRecords = [];
                    foreach (var line in fgScriptLine)
                    {
                        FgRecord? fgRecord = ParseScriptFGLine(line);
                        if (fgRecord != null)
                            fgRecords.Add(fgRecord);

                    }
                    if (fgRecords.Count == 0)
                        throw new Exception("No valid fg object found.");
                    else
                        return fgRecords;
                }
            }
        }

        public static FgRecord? ParseScriptFGLine(string input)
        {
            FgRecord fgRecord = new();
            input = input.Trim('{', '}');
            var pairs = input.Split(',');
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split(['=', '='], 2);
                if (keyValue.Length == 2)
                {
                    string key = keyValue[0].Trim();
                    string value = keyValue[1].Trim().Trim('"').Trim('}');
                    PropertyInfo? property = typeof(FgRecord).GetProperty(char.ToUpper(key[0]) + key[1..]);
                    if (property != null)
                    {
                        if (property.PropertyType == typeof(int))
                        {
                            if (int.TryParse(value, out int intValue))
                                property.SetValue(fgRecord, intValue);
                            else
                                throw new Exception($"Invalid integer value '{value}' for property '{key}'.");
                        }
                        else if (property.PropertyType == typeof(string))
                        {
                            property.SetValue(fgRecord, value);
                        }
                        // 其他类型的处理可以在这里添加
                    }
                }
            }
            //补个chname
            if (fgRecord.File != null)
                fgRecord.ChName = GetCharacterEngName(fgRecord.File);
            return fgRecord.Size == null ? null : fgRecord;
        }

        public static string GetCharacterEngName(string input)
        {
            int underscoreIndex = input.IndexOf('_');
            if (underscoreIndex > 0)
                return input[..underscoreIndex];
            throw new Exception("Not supported character name format.");
        }
    }
}
