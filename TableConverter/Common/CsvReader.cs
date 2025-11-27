#nullable disable

public static class CsvReader {
    public static List<List<string>> readCsv(string path) {
        List<List<string>> rows = new List<List<string>>();
        StreamReader reader = null;

        try {
            reader = File.OpenText(path);

            // int lineCount = 0;
            while (true) {
                string line = reader.ReadLine();
                if (string.IsNullOrEmpty(line) == true) {
                    break;
                }

                List<string> columns = new List<string>();
                parseToToken(columns, line);
                rows.Add(columns);
                // lineCount++;
            }
        }
        catch {
            return null;
        }
        finally {
            reader?.Close();
            reader?.Dispose();
        }

        return rows;
    }

    private static void parseToToken(List<string> tokens, string line) {
        bool doubleQuetoeOpen = false;
        int startIdx = 0, length = 0;

        for (int i = 0; i < line.Length; i++) {
            char c = line[i];

            if (c == '"') {
                if (doubleQuetoeOpen == false) {
                    doubleQuetoeOpen = true;
                    startIdx = i + 1;
                    length = 0;
                    continue;
                }
                else {
                    doubleQuetoeOpen = false;
                    string token = line.Substring(startIdx, length);
                    tokens.Add(token);
                    startIdx = i + 1;
                    length = 0;
                    continue;
                }
            }
            else if (c == ',' && doubleQuetoeOpen == false) {

                if (length <= 0) {
                    startIdx = i + 1;
                    continue;
                }
                string token = line.Substring(startIdx, length);
                tokens.Add(token);
                startIdx = i + 1;
                length = 0;
                continue;
            }

            length++;
        }

        if (length > 0) {
            string token = line.Substring(startIdx, length);
            tokens.Add(token);
        }
    }
}
