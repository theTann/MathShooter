
public class Configure {
    Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();

    public void load(string filename) {
        var lines = File.ReadAllLines(filename);
        for (int i = 0; i < lines.Length; i++) {
            string line = lines[i].Trim();
            if (line.StartsWith('#'))
                continue;

            var tokens = line.Split('=');
            if (tokens.Length != 2) {
                continue;
            }
            string token0 = tokens[0].Trim();
            string token1 = tokens[1].Trim();
            keyValuePairs[token0] = token1;
        }
    }

    public string getStr(string key) {
        return keyValuePairs[key];
    }

    public string[] getStrArr(string key) {
        string val = getStr(key);
        var tokens = val.Split(',');
        for (int i = 0; i < tokens.Length; i++) {
            string token = tokens[i].Trim();
            tokens[i] = token;
        }

        return tokens;
    }
}
