using System.Security.Cryptography;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 4)
        {
            Console.WriteLine("Usage: folderSync <sourcePath> <replicaPath> <intervalSeconds> <logFilePath>");
            return;
        }

        string sourcePath = args[0];
        string replicaPath = args[1];
        if (!int.TryParse(args[2], out int intervalSeconds) || intervalSeconds <= 0)
        {
            Console.WriteLine("Error: Invalid interval value.");
            return;
        }
        string logFilePath = args[3];

        if (!Directory.Exists(sourcePath))
        {
            Console.WriteLine("Error: Source path does not exist.");
            return;
        }
        Directory.CreateDirectory(replicaPath);

        Timer timer = new Timer(_ => SyncFolders(sourcePath, replicaPath, logFilePath), null, 0, intervalSeconds * 1000);

        Console.WriteLine("=======================================");
        Console.WriteLine(" Folder Synchronization Tool");
        Console.WriteLine("=======================================");
        Console.WriteLine("Synchronization started. Press [ENTER] to exit.");
        Console.WriteLine("=======================================");

        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            Log(logFilePath, "Process terminated safely.");
            Environment.Exit(0);
        };

        Console.ReadLine();
    }

    static void SyncFolders(string sourcePath, string replicaPath, string logFilePath)
    {
        try
        {
            Log(logFilePath, "Starting synchronization...");

            foreach (var dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                string relativePath = dirPath.Substring(sourcePath.Length + 1);
                string targetDir = Path.Combine(replicaPath, relativePath);

                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                    Log(logFilePath, $"Created directory: {targetDir}");
                }
            }

            foreach (string filePath in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
            {
                string relativePath = filePath.Substring(sourcePath.Length + 1);
                string destPath = Path.Combine(replicaPath, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destPath) ?? replicaPath);

                if (!File.Exists(destPath) || !FilesAreEqual(filePath, destPath))
                {
                    File.Copy(filePath, destPath, true);
                    Log(logFilePath, $"Copied: {filePath} to {destPath}");
                }
            }

            foreach (string filePath in Directory.GetFiles(replicaPath, "*", SearchOption.AllDirectories))
            {
                string relativePath = filePath.Substring(replicaPath.Length + 1);
                string sourceFile = Path.Combine(sourcePath, relativePath);
                if (!File.Exists(sourceFile))
                {
                    File.Delete(filePath);
                    Log(logFilePath, $"Deleted: {filePath}");
                }
            }

            Log(logFilePath, "Synchronization completed.");
        }
        catch (IOException ioEx)
        {
            Log(logFilePath, $"File I/O Error: {ioEx.Message}");
        }
        catch (UnauthorizedAccessException authEx)
        {
            Log(logFilePath, $"Permission Error: {authEx.Message}");
        }
        catch (Exception ex)
        {
            Log(logFilePath, $"Unexpected Error: {ex.Message}");
        }
    }

    static bool FilesAreEqual(string filePath1, string filePath2)
    {
        var fileInfo1 = new FileInfo(filePath1);
        var fileInfo2 = new FileInfo(filePath2);

        if (fileInfo1.Length != fileInfo2.Length ||
            fileInfo1.LastWriteTimeUtc != fileInfo2.LastWriteTimeUtc)
        {
            return false;
        }

        using (var md5 = MD5.Create())
        {
            using (var stream1 = File.OpenRead(filePath1))
            using (var stream2 = File.OpenRead(filePath2))
            {
                return md5.ComputeHash(stream1).SequenceEqual(md5.ComputeHash(stream2));
            }
        }
    }

    static void Log(string logFilePath, string message, string level = "INFO")
    {
        string logMessage = $"[{DateTime.Now}] [{level}] {message}";
        Console.WriteLine(logMessage);
        File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
    }
}