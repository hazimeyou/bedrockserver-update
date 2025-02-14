using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using System.Diagnostics;

public class UpdatePlugin : IPlugin
{
    public string Name => "update-plugIn";
    public string Type => "input";
    public string Trigger => "update";
    private bool isRunning = false;
    private string baseDirectory = "C:\\Users\\はじめよう\\source\\repos\\autoconsole-v0.2.0\\autoconsole-v0.2.0\\bin\\Debug\\net8.0";
    public void Execute(string input, Action<string> sendCommand)
    {
        Console.WriteLine($"[DEBUG] {Name} Execute called with input: {input}");
        if (input == "update" && !isRunning)
        {
            Console.WriteLine($"[DEBUG] {Name} Execute called with input: {input}");
            isRunning = true;
            StartUpdateProcess(sendCommand);
        }
    }

    private async void StartUpdateProcess(Action<string> sendCommand)
    {
        sendCommand("サーバー終了しましたか？（終了した場合は!yes まだの場合は!n）");
        if (Console.ReadLine() != "!yes")
        {
            sendCommand("更新プロセスをキャンセルしました。");
            isRunning = false;
            return;
        }

        sendCommand("リンクを貼り付けてください");
        string url = Console.ReadLine();
        string folderName = Path.GetFileNameWithoutExtension(new Uri(url).LocalPath);
        string zipPath = Path.Combine(baseDirectory, "update.zip");
        string extractPath = Path.Combine(baseDirectory, folderName);

        try
        {
            await DownloadFileAsync(url, zipPath, sendCommand);
            ExtractZip(zipPath, extractPath);
            RunServerProcess(extractPath, sendCommand);
        }
        catch (Exception ex)
        {
            sendCommand($"[ERROR] {ex.Message}");
        }
        isRunning = false;
    }

    private async Task DownloadFileAsync(string url, string destination, Action<string> sendCommand)
    {
        using (var client = new WebClient())
        {
            await client.DownloadFileTaskAsync(new Uri(url), destination);
        }
    }

    private void ExtractZip(string zipPath, string extractPath)
    {
        if (!File.Exists(zipPath))
        {
            throw new FileNotFoundException("ダウンロードしたファイルが見つかりません。");
        }

        ZipFile.ExtractToDirectory(zipPath, extractPath, true);
    }

    private void RunServerProcess(string serverPath, Action<string> sendCommand)
    {
        sendCommand("サーバーを起動しています...");
        sendCommand($"cd {serverPath}");
        sendCommand("bedrock_server.exe");
        sendCommand("stop");
        sendCommand($"cd {baseDirectory}");
        HandleWorldsFolder(serverPath, sendCommand);
    }

    private void HandleWorldsFolder(string serverPath, Action<string> sendCommand)
    {
        string backupFolder = Path.Combine(baseDirectory, "backup");
        if (!Directory.Exists(backupFolder))
        {
            Directory.CreateDirectory(backupFolder);
        }

        string[] existingFolders = Directory.GetDirectories(baseDirectory, "bedrock-server-*", SearchOption.TopDirectoryOnly);
        Array.Sort(existingFolders);
        if (existingFolders.Length < 2)
        {
            sendCommand("旧バージョンのサーバーフォルダーが見つかりません。");
            return;
        }

        string oldServerPath = existingFolders[^2];
        string oldWorlds = Path.Combine(oldServerPath, "worlds");
        string newWorlds = Path.Combine(serverPath, "worlds");
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string backupDestination = Path.Combine(backupFolder, $"worlds_backup_{timestamp}");

        if (Directory.Exists(oldWorlds))
        {
            Directory.Move(oldWorlds, backupDestination);
            sendCommand("ワールドをバックアップしました。");
        }

        if (Directory.Exists(newWorlds))
        {
            Directory.Delete(newWorlds, true);
        }
        Directory.Move(backupDestination, newWorlds);
        sendCommand("ワールドの移行が完了しました。");

        Directory.Delete(oldServerPath, true);
        sendCommand("旧フォルダーを削除しました。");
    }
}
