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

        public static List<FgObject> FetchFgObjectsFromScript(string path)
        {
            //检查path是否存在，然后foreach获得文件夹下所有.ast文件，循环
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
                    List<FgObject> fgObjects = [];
                    foreach (var line in fgScriptLine)
                    {
                        FgObject fgObject = ParseScriptFGLine(line);
                        // TODO:如果fgObject未被添加到fgObjects中，添加
                        fgObjects.Add(fgObject);
                    }
                    if (fgObjects.Count == 0)
                        throw new Exception("No valid fg object found.");
                    else
                        return fgObjects;
                }
            }
        }

        public static FgObject ParseScriptFGLine(string input)
        {
            input = input.Trim('{', '}');

            var pairs = input.Split(',');

            var result = new Dictionary<string, string>();

            foreach (var pair in pairs)
            {
                var keyValue = pair.Split(['=', '='], 2);
                if (keyValue.Length == 2)
                {
                    string key = keyValue[0].Trim();
                    string value = keyValue[1].Trim().Trim('"'); // 去掉引号
                    result[key] = value;
                }
            }
            foreach (var kv in result)
            {
                Console.WriteLine($"{kv.Key}: {kv.Value}");
            }

            throw new NotImplementedException();
        }
    }
}
