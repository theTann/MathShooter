public static class Common {
    public enum TableType {
        list,
        dictionary,
        dictionaryList,
        error,
    }

    public static TableType tableType = TableType.list;
    public static string? keyType = null;
    public static string? keyColName = null;

    public static void clearVars() {
        tableType = TableType.list;
        keyType = null;
        keyColName = null;
    }

    public static string checkTableType(string colName, string type) {
        if (colName.EndsWith("**") == true) {
            if (tableType != TableType.list) {
                tableType = TableType.error;
                return colName;
            }

            tableType = TableType.dictionaryList;
            keyType = type;
            keyColName = colName.Replace("**", "");
            return keyColName;
        }

        if (colName.EndsWith("*") == true) {
            if (tableType != TableType.list) {
                tableType = TableType.error;
                return colName;
            }
            tableType = TableType.dictionary;
            keyType = type;
            keyColName = colName.Replace("*", "");
            return keyColName;
        }

        return colName;
    }
}