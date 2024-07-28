using ImageMagick;
using NLua;
using System.Text.RegularExpressions;
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
            Console.WriteLine("请输入立绘fg文件夹的所在路径（无需\"\"）：");
            string? fgImagePath = Console.ReadLine();

            Console.WriteLine("请输入exlist.ipt的文件路径：");
            string? luaFilePath = Console.ReadLine();

            Console.WriteLine("请输入保存位置：");
            string? savePath = Console.ReadLine();

            if (string.IsNullOrEmpty(fgImagePath) || string.IsNullOrEmpty(luaFilePath) || string.IsNullOrEmpty(savePath))
            {
                Console.WriteLine("路径不能为空");
                return;
            }
            if (!Directory.Exists(fgImagePath) || !File.Exists(luaFilePath))
            {
                Console.WriteLine("路径不存在");
                return;
            }
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            Dictionary<object, object>? dictionary = ParseLuaTable(luaFilePath);

            if (dictionary != null)
            {
                if (dictionary["fg"] is Dictionary<object, object> fgDictionary)
                {
                    if (fgDictionary["size"] is not List<object> size || size.Count == 0)
                    {
                        throw new Exception("size not found or empty");
                    }
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
                            path = path[4..]; // remove :fg/../
                            fgObjects.Add(new FgObject(path, head, fuku, pose, face));
                        }
                    }
                    foreach (var fgObject in fgObjects)
                    {
                        foreach (var siz in size)
                        {
                            if (siz != null && fgObject.Path != null)
                            {
                                string savePathWithSizePart = Path.Combine(savePath, fgObject.Path, siz.ToString() ?? string.Empty);
                                string pathWithSize = Path.Combine(fgImagePath, fgObject.Path, siz.ToString() ?? string.Empty);
                                foreach (var pose in fgObject.Pose)
                                {
                                    Parallel.ForEach(fgObject.Fuku, fuku =>
                                    //foreach (var  in )
                                    {
                                        bool special = false;
                                        string fuku_current = fuku;
                                        //if the tail of fuku is |0099, spec to true & remove |0099
                                        if (fuku_current.EndsWith("|0099"))
                                        {
                                            special = true;
                                            fuku_current = fuku[0..^5];
                                        }
                                        // <head><siz><pose[0]><fuku><pose[1]>0
                                        // *sp:fuku: 02 | 0099→02fuku & 0099face
                                        string baseImg = Path.Combine(pathWithSize, $"{fgObject.Head}{siz}{pose[0]}{fuku_current}{pose[1]}0.png");
                                        foreach (var face in fgObject.Face[pose[0]])
                                        {
                                            string layerImg = Path.Combine(pathWithSize, $"{face}.png");
                                            string layer2Img = special ? Path.Combine(pathWithSize, $"{pose[0]}0099.png") : ""; //眼镜
                                            string savePathWithAll = Path.Combine(savePathWithSizePart, $"{fgObject.Head}{siz}{pose[0]}{fuku_current}{pose[1]}0_{face}" + (special ? ($"_{pose[0]}0099.png") : (".png")));
                                            ProcessAndSave(baseImg, layerImg, layer2Img, savePathWithAll, special);
                                        }

                                    });
                                }
                            }
                        }
                        string siz2 = "fa"; //别急着换下一个，还有个fa //这里的代码和上面那块一样
                        if (fgObject.Path != null)
                        {
                            string savePathWithSizePart = Path.Combine(savePath, fgObject.Path, siz2.ToString() ?? string.Empty);
                            string pathWithSize = Path.Combine(fgImagePath, fgObject.Path, siz2.ToString() ?? string.Empty);
                            foreach (var pose in fgObject.Pose)
                            {
                                Parallel.ForEach(fgObject.Fuku, fuku =>
                                {
                                    bool special = false;
                                    string fuku_current = fuku;
                                    if (fuku_current.EndsWith("|0099"))
                                    {
                                        special = true;
                                        fuku_current = fuku[0..^5];
                                    }
                                    string baseImg = Path.Combine(pathWithSize, $"{fgObject.Head}no{pose[0]}{fuku_current}{pose[1]}0.png");
                                    foreach (var face in fgObject.Face[pose[0]])
                                    {
                                        string layerImg = Path.Combine(pathWithSize, $"{face}.png");
                                        string layer2Img = special ? Path.Combine(pathWithSize, $"{pose[0]}0099.png") : "";
                                        string savePathWithAll = Path.Combine(savePathWithSizePart, $"{fgObject.Head}no{pose[0]}{fuku_current}{pose[1]}0_{face}" + (special ? ($"_{pose[0]}0099.png") : (".png")));
                                        ProcessAndSave(baseImg, layerImg, layer2Img, savePathWithAll, special);
                                    }
                                });
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("fg not found");
                }
            }
        }

        private static void ProcessAndSave(string baseImg, string layerImg, string layer2Img, string target, bool special)
        {
            if (File.Exists(target)) 
            {
                Console.WriteLine($"{Path.GetFileName(target)}已存在，跳过！");
                return;
            }
            string? directory = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using MagickImage firstImage = new(baseImg);
            List<int> comment1 = ReadPngComment(firstImage.Comment); //base
            using MagickImage secondImage = new(layerImg);
            List<int> comment2 = ReadPngComment(secondImage.Comment); //face
            int x = comment2[0] - comment1[0]; // face x - base x
            int y = comment2[1] - comment1[1]; // face y - base y
            firstImage.Composite(secondImage, x, y, CompositeOperator.Over);
            if (special)
            {
                using MagickImage thirdImage = new(layer2Img);
                List<int> comment3 = ReadPngComment(thirdImage.Comment); //face
                x = comment3[0] - comment1[0]; // face x - base x
                y = comment3[1] - comment1[1]; // face y - base y
                firstImage.Composite(thirdImage, x, y, CompositeOperator.Over);
            }
            //确保target所处位置文件夹是存在的，不存在则创建

            firstImage.Write(target);
            Console.WriteLine($"{Path.GetFileName(target)}图像合并完成！");
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

        public static List<int> ReadPngComment(string? comment)
        {
            if (comment != null)
            {
                string pattern = @"^pos,(-?\d+),(-?\d+),(-?\d+),(-?\d+)$";
                Match match = Regex.Match(comment, pattern);
                if (match.Success)
                {
                    int x = int.Parse(match.Groups[1].Value);
                    int y = int.Parse(match.Groups[2].Value);
                    int w = int.Parse(match.Groups[3].Value);
                    int h = int.Parse(match.Groups[4].Value);

                    return [x, y, w, h];
                }
                else
                {
                    throw new Exception("Unexpected result");
                }
            }
            else
            {
                throw new Exception("Comment not found");
            }
        }
    }
}
