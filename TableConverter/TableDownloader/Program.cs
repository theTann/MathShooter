using System.IO;

namespace TableDownloader;


internal class Program {
    
    static async Task<int> Main(string[] args) {
        int result = await downloadCsv();
        return result;
    }

    static async Task<int> downloadCsv() {
        Configure config = new();
        config.load("sheet.config");

        string sheetId = config.getStr("sheetId");
        string[] gids = config.getStrArr("gids");
        string[] tableNames = config.getStrArr("tableNames");

        string outputFolder = config.getStr("saveFolder");

        if(gids.Length < 1) {
            Console.Error.WriteLine("invalid gid count.");
            return 1;
        }

        DirectoryInfo tempDirectoryInfo = new DirectoryInfo("temp");
        if (tempDirectoryInfo.Exists == false) {
            tempDirectoryInfo.Create();
        }

        HttpClient[] https = new HttpClient[gids.Length];
        Task<HttpResponseMessage>[] responseTasks = new Task<HttpResponseMessage>[gids.Length];
        Task<string>[] readTasks = new Task<string>[gids.Length];

        try {
            for (int i = 0; i < gids.Length; i++) {
                https[i] = new HttpClient();
                var gid = gids[i];
                string url = $"https://docs.google.com/spreadsheets/d/{sheetId}/export?format=csv&gid={gid}";
                responseTasks[i] = https[i].GetAsync(url);
            }
            
            await Task.WhenAll(responseTasks);

            for (int i = 0; i < responseTasks.Length; i++) {
                HttpResponseMessage responseMessage = responseTasks[i].Result;
                responseMessage.EnsureSuccessStatusCode();
                readTasks[i] = responseMessage.Content.ReadAsStringAsync();
            }
            
            await Task.WhenAll(readTasks);

            for (int i = 0; i < readTasks.Length; i++) {
                string content = readTasks[i].Result;
                string savePath = $"temp/{tableNames[i]}.csv";
                File.WriteAllText(savePath, content);
                responseTasks[i].Result.Dispose();
                https[i].Dispose();
            }
        }
        catch (Exception e) {
            Console.Error.WriteLine(e.ToString());
            return 2;
        }

        try {
            DirectoryInfo outputDirectoryInfo = new(outputFolder);
            if (outputDirectoryInfo.Exists == false) {
                outputDirectoryInfo.Create();
            }
            string[] files = Directory.GetFiles(outputFolder);
            foreach (string file in files)
                File.Delete(file);

            for(int i = 0; i < tableNames.Length; i++) {
                string name = tableNames[i];
                string src = $"temp/{tableNames[i]}.csv";
                string dest = $"{outputFolder}/{tableNames[i]}.csv";
                File.Copy(src, dest, overwrite: true);
            }
        }
        catch (Exception e) {
            Console.Error.WriteLine(e.ToString());
            return 3;
        }
        return 0;
    }
}
