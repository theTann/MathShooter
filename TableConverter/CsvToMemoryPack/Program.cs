using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace CsvToMemoryPack;

internal class Program {
    static string tabStr = "    ";
    static Configure _sheetConfig = new();

    static int Main(string[] args) {
        _sheetConfig.load("sheet.config");

        string[] tableNames = _sheetConfig.getStrArr("tableNames");
        string csvFolder = _sheetConfig.getStr("saveFolder");

        Dictionary<string, List<List<string>>> tables = new();

        foreach (var tableName in tableNames) {
            string csvPath = $"{csvFolder}/{tableName}.csv";
            List<List<string>> table = CsvReader.readCsv(csvPath);
            if (table == null) {
                Console.Error.WriteLine($"read csv fail : {csvPath}");
                return 1;
            }
            tables.Add(tableName, table);
        }
        int result = generateCsharpCodeFromCsv(tables);
        if (result != 0)
            return result;

        if (addFileLinkToCsproj(tables.Keys.ToList()) == false)
            return 10;

        if(buildSerializer() == false) {
            return 11;
        }
        return 0;
    }

    static int generateCsharpCodeFromCsv(Dictionary<string, List<List<string>>> tables) {
        try {
            DirectoryInfo tempDirectoryInfo = new DirectoryInfo("temp");
            if (tempDirectoryInfo.Exists == false) {
                tempDirectoryInfo.Create();
            }

            foreach (var kvp in tables) {
                string tableName = kvp.Key;
                List<List<string>> csv = kvp.Value;

                // 헤더(변수(컬럼)이름과 타입정보)
                if (csv.Count < 1) {
                    return 3;
                }

                StringBuilder sb = new StringBuilder();
                string tempOutputFile = $"temp/{tableName}_gen.cs";
                writeHeader(sb, tableName);
                writeCsv(sb, csv);

                if(Common.tableType == Common.TableType.error) {
                    Console.Error.WriteLine($"table type error");
                    return 4;
                }

                writeFooter(sb, tableName);

                writeTableHeader(sb, tableName);
                writeTableFooter(sb, tableName);

                File.WriteAllText(tempOutputFile, sb.ToString());
            }
        }
        catch (Exception ex) {
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }

        try {
            string destFolder = _sheetConfig.getStr("csharpFolder");

            DirectoryInfo outputDirectoryInfo = new(destFolder);
            if (outputDirectoryInfo.Exists == false) {
                outputDirectoryInfo.Create();
            }
            string[] files = Directory.GetFiles(destFolder);
            foreach (string file in files)
                File.Delete(file);

            foreach (var kvp in tables) {
                string tableName = kvp.Key;
                string src = $"temp/{tableName}_gen.cs";
                string dest = $"{destFolder}/{tableName}_gen.cs";
                File.Copy(src, dest, overwrite: true);
            }
        }
        catch (Exception e) {
            Console.Error.WriteLine(e.ToString());
            return 2;
        }
        return 0;
    }


    public static void writeHeader(StringBuilder sb, string tableName) {
        sb.AppendLine($"#nullable disable");
        sb.AppendLine($"using System;");
        sb.AppendLine($"using System.Collections.Generic;");
        sb.AppendLine($"using MemoryPack;");
        sb.AppendLine($"");
        sb.AppendLine($"[MemoryPackable]");
        sb.AppendLine($"[Serializable]");
        sb.AppendLine($"public partial class {tableName}");
        sb.AppendLine($"{{");
    }

    public static void writeFooter(StringBuilder sb, string tableName) {
        sb.AppendLine($"}}");
        sb.AppendLine($"");

    }

    public static void writeTableHeader(StringBuilder sb, string tableName) {
        sb.AppendLine($"[MemoryPackable]");
        sb.AppendLine($"[Serializable]");
        sb.AppendLine($"public partial class {tableName}Table");
        sb.AppendLine($"{{");
        if(Common.tableType == Common.TableType.list)
            sb.AppendLine($"{tabStr}[MemoryPackOrder(1)] public List<{tableName}> {tableName}s = new();");
        else if (Common.tableType == Common.TableType.dictionaryList)
            sb.AppendLine($"{tabStr}[MemoryPackOrder(1)] public Dictionary<{Common.keyType}, List<{tableName}>> {tableName}s = new();");
        else if (Common.tableType == Common.TableType.dictionary)
            sb.AppendLine($"{tabStr}[MemoryPackOrder(1)] public Dictionary<{Common.keyType}, {tableName}> {tableName}s = new();");
    }

    public static void writeTableFooter(StringBuilder sb, string tableName) {
        sb.AppendLine($"}}");
        sb.AppendLine($"");
    }

    public static void writeCsv(StringBuilder sb, List<List<string>> csv) {
        List<string> columName = csv[0];
        List<string> types = csv[1];

        Common.clearVars();

        for (int i = 0; i < columName.Count; i++) {
            string col = columName[i];
            string type = types[i];

            col = Common.checkTableType(col, type);
            if (Common.tableType == Common.TableType.error)
                return;

            sb.AppendLine($"{tabStr}[MemoryPackOrder({i+1})] public {type} {col} {{ get; set; }}");
        }
    }

    public static bool addFileLinkToCsproj(List<string> tableNames) {
        string destFolder = _sheetConfig.getStr("csharpFolder");
        string serializerFolder = _sheetConfig.getStr("serializerPath");

        DirectoryInfo di = new DirectoryInfo(destFolder);

        try {
            string projectFilename = $"{serializerFolder}/CsvToMemoryPackBinary.csproj";
            StringBuilder stringBuilder = new StringBuilder();
            var lines = File.ReadAllLines(projectFilename);
            
            bool appended = false;
            for (int i = 0; i < lines.Length; i++) {
                var line = lines[i];
                string trimedLine = line.Trim();

                // append generated files.
                if (trimedLine.StartsWith("<ItemGroup>") == true && appended == false) {
                    appended = true;

                    stringBuilder.AppendLine(line);

                    foreach (var tableName in tableNames) {
                        string filename = $"{tableName}_gen.cs";
                        string filePath = $"{di.FullName}\\{filename}";
                        stringBuilder.AppendLine($"    <Compile Include=\"{filePath}\" Link=\"{filename}\" />");
                    }
                }
                else if (trimedLine.StartsWith("<Compile Include=") == false) {
                    stringBuilder.AppendLine(line);
                }
                else {
                    // 이전에 있던 generated된 cs에 대한건 패스한다(위에서 추가했으니까)
                    bool isGeneratedFile = false;
                    if (trimedLine.Contains(di.FullName) == true) {
                        isGeneratedFile = true;
                    }

                    foreach (var tableName in tableNames) {
                        if (trimedLine.Contains($"{tableName}_gen.cs") == true) {
                            isGeneratedFile = true;
                            break;
                        }
                    }

                    if (isGeneratedFile == true)
                        continue;

                    stringBuilder.AppendLine(line);
                }
            }
            FileInfo fileInfo = new FileInfo(projectFilename);
            fileInfo.Delete();
            File.WriteAllText(projectFilename, stringBuilder.ToString());
        }
        catch (Exception e) { Console.Error.WriteLine(e.Message); return false; }

        return true;
    }

    public static bool buildSerializer() {
        string serializerFolder = _sheetConfig.getStr("serializerPath");
        StringBuilder output = new StringBuilder();
        StringBuilder error = new StringBuilder();
        var prevEncoding = Console.OutputEncoding;

        try {
            ProcessStartInfo startInfo = new ProcessStartInfo("cmd") {
                WorkingDirectory = $"{serializerFolder}/..",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            startInfo.StandardOutputEncoding = Encoding.UTF8;
            startInfo.StandardErrorEncoding = Encoding.UTF8;

            Process process = new Process();
            process.StartInfo = startInfo;
            process.OutputDataReceived += (s, e) => { if (e.Data != null) output.AppendLine(e.Data); };
            process.ErrorDataReceived += (s, e) => { if (e.Data != null) error.AppendLine(e.Data); };

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            string cmd = $"dotnet build CsvToMemoryPackBinary --configuration Release";
            process.StandardInput.WriteLine(cmd);
            process.StandardInput.WriteLine("exit");
            process.StandardInput.Flush();

            process.WaitForExit();
            
            Console.OutputEncoding = Encoding.UTF8;
            Console.Write(output.ToString());
            if (error.Length > 0) {
                Console.Error.WriteLine(error.ToString());
                return false;
            }
        }
        catch (Exception e) {
            Console.Error.WriteLine($"exception occur. exception : {e}");
            return false;
        }
        finally {
            Console.OutputEncoding = prevEncoding;
        }

        return true;
    }
}
