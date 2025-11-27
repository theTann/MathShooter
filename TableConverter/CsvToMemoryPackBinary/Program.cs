using MemoryPack;
using System.Reflection;

namespace CsvToMemoryPackBinary;

internal class Program {
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

        if (csvToBinary(tables) == false)
            return 2;

        return 0;
    }

    public static Type? getTypeFromAssembly(string typeName) {
        foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            Type? type = assembly.GetType(typeName);
            if (type != null) {
                return type;
            }
        }
        return null;
    }

    private static bool csvToBinary(Dictionary<string, List<List<string>>> tables) {
        string binOutputPath = _sheetConfig.getStr("binresult");

        try {
            DirectoryInfo tempDirectoryInfo = new DirectoryInfo("temp");
            if (tempDirectoryInfo.Exists == false) {
                tempDirectoryInfo.Create();
            }

            DirectoryInfo binDirInfo = new DirectoryInfo(binOutputPath);
            if (binDirInfo.Exists == false) {
                binDirInfo.Create();
            }

            foreach (var kvp in tables) {
                string tableName = kvp.Key;
                List<List<string>> table = kvp.Value;

                Type? tableType = getTypeFromAssembly($"{tableName}Table");
                Type? dataType = getTypeFromAssembly($"{tableName}");
                if (tableType == null || dataType == null) return false;

                object? dataTableObj = Activator.CreateInstance(tableType);
                if (dataTableObj == null) return false;

                var collectionInfo = tableType.GetField($"{tableName}s");
                if (collectionInfo == null) return false;

                var collectionObj = collectionInfo.GetValue(dataTableObj);
                if (collectionObj == null) return false;

                var collectionType = collectionObj.GetType();
                var collectionAddMethod = collectionType.GetMethod("Add"); //, new Type[] { dataType });
                if (collectionAddMethod == null) return false;

                

                var colNames = table[0];
                var colTypes = table[1];

                Common.clearVars();

                for(int i = 0; i < colNames.Count; i++) {
                    string colName = colNames[i];
                    string colType = colTypes[i];
                    Common.checkTableType(colName, colType);
                }

                for (int i = 2; i < table.Count; i++) {
                    var row = table[i];
                    object? dataObj = Activator.CreateInstance(dataType);
                    if (dataObj == null) return false;

                    if (colNames.Count != colTypes.Count || colNames.Count != row.Count) {
                        Console.Error.WriteLine($"invalid data count. please check empty data. tablename : {tableName}, line : {i + 1}");
                        return false;
                    }
                    object? keyObj = null;

                    for (int j = 0; j < colNames.Count; j++) {
                        string colName = colNames[j];
                        string colType = colTypes[j];
                        string strVal = row[j];

                        colName = colName.Replace("*", "");
                        
                        if (Common.tableType == Common.TableType.error) {
                            Console.Error.WriteLine($"key is more than one. {tableName}, colName : {colName}");
                            return false;
                        }

                        bool parseResult = false;

                        PropertyInfo? propertyInfo = dataType.GetProperty(colName);
                        if (propertyInfo == null) {
                            Console.Error.WriteLine($"Property not exist : {tableName}, colName : {colName}, colType : {colType}, strVal : {strVal}");
                            return false;
                        }
                        object? parseData = null;

                        if (colType == "float") {
                            parseResult = float.TryParse(strVal, out float floatVal);
                            parseData = floatVal;
                            propertyInfo.SetValue(dataObj, floatVal);
                        }
                        else if (colType == "int") {
                            parseResult = int.TryParse(strVal, out int intVal);
                            parseData = intVal;
                            propertyInfo.SetValue(dataObj, intVal);
                        }
                        else if (colType == "bool") {
                            parseResult = bool.TryParse(strVal, out bool boolVal);
                            parseData = boolVal;
                            propertyInfo.SetValue(dataObj, boolVal);
                        }
                        else if (colType == "string") {
                            propertyInfo.SetValue(dataObj, strVal);
                            parseData = strVal;
                            parseResult = true;
                        }
                        else if (colType == "uint") {
                            parseResult = uint.TryParse(strVal, out uint uintVal);
                            parseData = uintVal;
                            propertyInfo.SetValue(dataObj, uintVal);
                        }
                        else if (colType == "ulong") {
                            parseResult = ulong.TryParse(strVal, out ulong ulongVal);
                            parseData = ulongVal;
                            propertyInfo.SetValue(dataObj, ulongVal);
                        }
                        else if (colType == "long") {
                            parseResult = long.TryParse(strVal, out long longVal);
                            parseData = longVal;
                            propertyInfo.SetValue(dataObj, longVal);
                        }
                        else if (colType == "double") {
                            parseResult = double.TryParse(strVal, out double doubleVal);
                            parseData = doubleVal;
                            propertyInfo.SetValue(dataObj, doubleVal);
                        }
                        else if (colType == "byte") {
                            parseResult = byte.TryParse(strVal, out byte byteVal);
                            parseData = byteVal;
                            propertyInfo.SetValue(dataObj, byteVal);
                        }
                        else {
                            // todo : serialize에 enum 추가되야하는 그것.
                            // enum 인걸로 하자.
                            var enumType = Type.GetType($"{colType}");
                            if (enumType != null) {
                                parseResult = System.Enum.TryParse(enumType, strVal, out object? enumVal);
                                if (enumVal == null) {
                                    Console.Error.WriteLine($"enum parse error. enumType : {enumType}, strValue : {strVal}");
                                    return false;
                                }
                                parseData = enumVal;
                                propertyInfo.SetValue(dataObj, enumVal);
                            }
                        }
                        if (colName == Common.keyColName)
                            keyObj = parseData;

                        if (parseResult == false) {
                            Console.Error.WriteLine($"can't parse data. data : {tableName}, colType : {colType}, strVal : {strVal}");
                            return false;
                        }
                    }
                    if (Common.tableType == Common.TableType.list) {
                        collectionAddMethod.Invoke(collectionObj, new object[] { dataObj });
                    }
                    else if (Common.tableType == Common.TableType.dictionary) {
                        collectionAddMethod.Invoke(collectionObj, new object[] { keyObj!, dataObj});
                    }
                    else if(Common.tableType == Common.TableType.dictionaryList) {
                        object?[] parameters = new object?[] { keyObj, null };
                        var tryGetValueMethod = collectionType.GetMethod("TryGetValue");
                        if (tryGetValueMethod == null) {
                            Console.Error.WriteLine($"TryGetValue method fail. collection type : {collectionType}");
                            return false;
                        }
                        Type listType = typeof(List<>).MakeGenericType(dataType);
                        bool found = (bool)tryGetValueMethod.Invoke(collectionObj, parameters)!;
                        object? listInstance = parameters[1];

                        if (found == false) {
                            
                            listInstance = Activator.CreateInstance(listType);
                            if(listInstance == null) {
                                Console.Error.WriteLine($"listInstnace is null. dataType : {dataType}, listType : {listType}");
                                return false;
                            }
                            
                            collectionAddMethod.Invoke(collectionObj, new object[] { keyObj!, listInstance });
                            // listInstance
                        }

                        var listAddMethod = listType.GetMethod("Add");
                        if (listAddMethod == null) {
                            Console.Error.WriteLine($"listAddMethod is null. listType : {listType}");
                            return false;
                        }
                        listAddMethod.Invoke(listInstance, new object[] { dataObj });
                    }

                }
                byte[] result = MemoryPackSerializer.Serialize(tableType, dataTableObj);
                string bytesPath = $"temp/{tableName}.bytes";
                File.WriteAllBytes(bytesPath, result);
            }

            string destFolder = binOutputPath;
            string[] files = Directory.GetFiles(destFolder);
            foreach (string file in files)
                File.Delete(file);

            foreach (var kvp in tables) {
                string tableName = kvp.Key;
                string src = $"temp/{tableName}.bytes";
                string dest = $"{binOutputPath}/{tableName}.bytes";
                File.Copy(src, dest, overwrite: true);
            }
        }
        catch (Exception e) {
            Console.Error.WriteLine(e.ToString());
            return false;
        }

        

        return true;
    }
}
