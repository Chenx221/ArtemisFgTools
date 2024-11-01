using ImageMagick;
using NLua;
using System.Text.RegularExpressions;
using static ArtemisFgTools.FgHelper;
namespace ArtemisFgTools
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                if (args[0] == "-h")
                    Console.WriteLine("Usage: tools.exe -c <fgPath> (-s <scriptPath> | -t <luaTablePath>) -o <outputPath>");
                else
                    Console.WriteLine("Invalid arguments, Please check the usage via -h");
            }
            else if (args.Length != 6)
                Console.WriteLine("Invalid arguments, Please check the usage via -h");
            else if (args[0] != "-c" || !(args[2] == "-s" || args[2] == "-t") || args[4] != "-o")
                Console.WriteLine("Invalid arguments, Please check the usage via -h");
            else
            {
                if (!Directory.Exists(args[5]))
                    Directory.CreateDirectory(args[5]);
                if (!Directory.Exists(args[1]))
                    Console.WriteLine("Invalid fg path");
                else if (args[2] == "-s")
                {
                    if (!Directory.Exists(args[3]))
                        Console.WriteLine("Invalid script path");
                    else
                        PreProcess2(args[1], args[5], args[3]);
                }
                else
                {
                    if (!File.Exists(args[3]))
                        Console.WriteLine("Invalid lua table path");
                    else
                        PreProcess(args[1], args[5], args[3]);
                }
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static void PreProcess2(string fgImagePath, string savePath, string scriptPath)
        {
            HashSet<FgRecord> fgRecords = FetchFgObjectsFromScript(scriptPath);
            if (fgRecords.Count == 0)
                throw new Exception("No valid fg object found.");
            //重新写个，我也懒得将FgRecord转FgObject了
            //Tips:如果有单独饰品素材，可能前面的解析会有遗漏 //反正遥かなるニライカナイ里没有戴眼镜的角色 (笑
            Process2(fgImagePath, savePath, fgRecords);
        }

        private static void PreProcess(string fgImagePath, string savePath, string luaFilePath)
        {
            Dictionary<object, object> dictionary = ParseLuaTable(luaFilePath) ?? throw new Exception("Lua table parsing failed");

            if (dictionary["fg"] is Dictionary<object, object> fgDictionary)
            {
                if (fgDictionary["size"] is not List<object> size || size.Count == 0)
                    throw new Exception("size not found or empty");
                fgDictionary.Remove("size");

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
                            throw new Exception("fg object has null value");
                        path = path[4..]; // remove :fg/../
                        fgObjects.Add(new FgObject(path, head, fuku, pose, face));
                    }
                }
                Process(fgImagePath, savePath, size, fgObjects);
            }
            else
                Console.WriteLine("fg not found");
        }

        private static void Process2(string fgImagePath, string savePath, HashSet<FgRecord> fgRecords)
        {
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 6 // 设置最大并行度
            };

            Parallel.ForEach(fgRecords, parallelOptions, fgRecord =>
            {
                string originImageBase = Path.Combine(fgImagePath, fgRecord.ChName, fgRecord.Size, fgRecord.File + ".png");
                string originImageFace = Path.Combine(fgImagePath, fgRecord.ChName, fgRecord.Size, fgRecord.Face + ".png");
                string targetFilename = $"{fgRecord.File}_{fgRecord.Face}.png";
                string targetPath = Path.Combine(savePath, fgRecord.Size);

                if (!File.Exists(originImageBase) || !File.Exists(originImageFace))
                {
                    Console.WriteLine("ERROR, Image not found. Details:");
                    Console.WriteLine($"Base: {originImageBase}");
                    Console.WriteLine($"Face: {originImageFace}");
                    return;
                }
                if (!Directory.Exists(targetPath))
                    Directory.CreateDirectory(targetPath);
                targetPath = Path.Combine(targetPath, targetFilename);
                if (File.Exists(targetPath))
                {
                    Console.WriteLine("File already exists, skipping...");
                    return;
                }
                ProcessAndSaveLite(originImageBase, originImageFace, targetPath);
            });

        }

        private static void Process(string fgImagePath, string savePath, List<object> size, List<FgObject> fgObjects)
        {
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
                            {
                                //💢 skip //For ハミダシクリエイティブ
                                if (fuku == "99")
                                {
                                    return;
                                }
                                bool special = false;
                                string special_text = "";
                                string fuku_current = fuku;
                                int index = fuku_current.IndexOf('|');
                                if (index != -1)
                                {
                                    special = true;
                                    special_text = fuku_current[(index + 1)..];
                                    fuku_current = fuku_current[..index];
                                }
                                // <head><siz><pose[0]><fuku><pose[1]>0
                                // *sp:fuku: 02 | 0099→02fuku & 0099face
                                string baseImg = Path.Combine(pathWithSize, $"{fgObject.Head}{siz}{pose[0]}{fuku_current}{pose[1]}0.png");
                                foreach (var face in fgObject.Face[pose[0]])
                                {
                                    string layerImg = Path.Combine(pathWithSize, $"{face}.png");
                                    string layer2Img = special ? Path.Combine(pathWithSize, $"{pose[0]}{special_text}.png") : ""; //眼镜
                                    string savePathWithAll = Path.Combine(savePathWithSizePart, $"{fgObject.Head}{siz}{pose[0]}{fuku_current}{pose[1]}0_{face}" + (special ? ($"_{pose[0]}{special_text}.png") : (".png")));
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
                            //💢 skip //For ハミダシクリエイティブ
                            if (fuku == "99")
                            {
                                return;
                            }
                            bool special = false;
                            string special_text = "";
                            string fuku_current = fuku;
                            int index = fuku_current.IndexOf('|');
                            if (index != -1)
                            {
                                special = true;
                                special_text = fuku_current[(index + 1)..];
                                fuku_current = fuku_current[..index];
                            }
                            string baseImg = Path.Combine(pathWithSize, $"{fgObject.Head}no{pose[0]}{fuku_current}{pose[1]}0.png");
                            foreach (var face in fgObject.Face[pose[0]])
                            {
                                string layerImg = Path.Combine(pathWithSize, $"{face}.png");
                                string layer2Img = special ? Path.Combine(pathWithSize, $"{pose[0]}{special_text}.png") : "";
                                string savePathWithAll = Path.Combine(savePathWithSizePart, $"{fgObject.Head}no{pose[0]}{fuku_current}{pose[1]}0_{face}" + (special ? ($"_{pose[0]}{special_text}.png") : (".png")));
                                ProcessAndSave(baseImg, layerImg, layer2Img, savePathWithAll, special);
                            }
                        });
                    }
                }
            }
        }

        private static void ProcessAndSaveLite(string baseImg, string faceImg, string target)
        {
            using MagickImage firstImage = new(baseImg);
            List<int> comment1 = ReadPngComment(firstImage.Comment); //base
            using MagickImage secondImage = new(faceImg);
            List<int> comment2 = ReadPngComment(secondImage.Comment); //face
            int x = comment2[0] - comment1[0]; // face x - base x
            int y = comment2[1] - comment1[1]; // face y - base y
            firstImage.Composite(secondImage, x, y, CompositeOperator.Over);
            firstImage.Write(target);
            Console.WriteLine($"Image {Path.GetFileName(target)} processing completed.");
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
                return (Dictionary<object, object>?)LuaTableToSs(luaTable);
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
