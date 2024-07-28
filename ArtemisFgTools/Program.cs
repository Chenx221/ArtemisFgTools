using ImageMagick;
using NLua;
namespace ArtemisFgTools
{
    internal class Program
    {
        public class FgObject(string path, string head, List<string> fuku, List<List<string>> pose, Dictionary<string, List<string>> face)
        {
            public string Path { get; set; } = path;
            public string Head { get; set; } = head;
            public List<string> Fuku { get; set; } = fuku;
            public List<List<string>> Pose { get; set; } = pose;
            public Dictionary<string, List<string>> Face { get; set; } = face;
        }
        static void Main()
        {
            string luaFilePath = "G:/x221.local/pc/ja/extra/exlist.ipt";
            Dictionary<object, object>? dictionary = ParseLuaTable(luaFilePath);

            if (dictionary != null)
            {
                if (dictionary["fg"] is Dictionary<object, object> fgDictionary)
                {
                    var size = fgDictionary["size"] as List<object>;
                    fgDictionary.Remove("size");

                    //convert to FgObject
                    List<FgObject> fgObjects = [];
                    foreach (var fg in fgDictionary)
                    {
                        if (fg.Value is Dictionary<object, object> fgValue)
                        {
                            var fuku = ConvertToStringList(fgValue["fuku"] as List<object>);
                            var pose = ConvertToNestedStringList(fgValue["pose"] as List<object>);
                            var face = ConvertToStringDictionary(fgValue["face"] as Dictionary<object, object>);
                            //check null
                            if (fgValue["path"] is not string path || fgValue["head"] is not string head || fuku == null || pose == null || face == null)
                            {
                                Console.WriteLine("fg object has null value");
                                continue;
                            }
                            fgObjects.Add(new FgObject(path, head, fuku, pose, face));
                        }
                    }
                    Console.WriteLine($"fg count: {fgObjects.Count}");
                    //Todo: Combine fgObjects with image position
                    //Todo: Get image position from png comment
                    String comment = ReadPngComment("G:/x221.local/fg/baa/fa/a0001.png");

                }
                else
                {
                    Console.WriteLine("fg not found");
                }
            }
        }

        static Dictionary<object, object>? ParseLuaTable(string luaFilePath)
        {
            Lua lua = new();
            try
            {
                lua.DoFile(luaFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading Lua file: {ex.Message}");
                return null;
            }

            LuaTable luaTable = lua.GetTable("exfgtable");
            if (luaTable != null)
            {
                return (Dictionary<object, object>?)LuaTableToSs(luaTable);
            }
            else
            {
                Console.WriteLine("Lua table not found");
                return null;
            }
        }

        static object LuaTableToSs(LuaTable luaTable)
        {
            if (NeedConvertList(luaTable))
            {
                List<object> list = [];
                foreach (var key in luaTable.Keys)
                {
                    object value = luaTable[key];
                    if (value is LuaTable nestedTable)
                    {
                        list.Add(LuaTableToSs(nestedTable));
                    }
                    else
                    {
                        list.Add(value);
                    }
                }
                return list;
            }
            else
            {
                Dictionary<object, object> dictionary = [];
                foreach (var key in luaTable.Keys)
                {
                    object value = luaTable[key];
                    if (value is LuaTable nestedTable)
                    {
                        dictionary[key] = LuaTableToSs(nestedTable);
                    }
                    else
                    {
                        dictionary[key] = value;
                    }
                }
                return dictionary;
            }
        }

        private static bool NeedConvertList(LuaTable luaTable)
        {
            long index = 1;
            foreach (var key in luaTable.Keys)
            {
                if (key is string)
                {
                    return false;
                }
                else
                {
                    long n = (long)key;
                    if (n != index)
                    {
                        return false;
                    }
                    index++;
                }

            }
            return true;
        }

        private static List<string>? ConvertToStringList(List<object>? list)
        {
            return list?.ConvertAll(item => item?.ToString() ?? string.Empty);
        }

        private static List<List<string>> ConvertToNestedStringList(List<object>? list)
        {
            if (list == null) return [];
            return list.ConvertAll(item => ConvertToStringList(item as List<object>) ?? []);
        }

        private static Dictionary<string, List<string>>? ConvertToStringDictionary(Dictionary<object, object>? dictionary)
        {
            if (dictionary == null) return null;

            Dictionary<string, List<string>> result = [];
            foreach (var kvp in dictionary)
            {
                result[kvp.Key?.ToString() ?? string.Empty] = ConvertToStringList(kvp.Value as List<object>) ?? [];
            }
            return result;
        }

        public static string ReadPngComment(string filePath)
        {
            if (File.Exists(filePath))
            {
                // Read image from file
                using var image = new MagickImage(filePath);

                if(image.Comment != null)
                {
                    return image.Comment;
                }
                else
                {
                    throw new Exception("Comment not found");
                }
            }
            else
            {
                Console.WriteLine("File does not exist.");
            }
        }


    }
}
