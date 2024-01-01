using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Reflection;
using System.Net;
using BsDiff;

static class Patcher {
    public static readonly string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "coopdx-patcher");
    public static readonly string outPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "sm64coopdx");
    public static readonly string patchPath = Path.Combine(appDataPath, "sm64coopdx.bsdiff");
    public static readonly string resourcesPath = Path.Combine(outPath, "resources.zip");
    static readonly string romPath = Path.Combine(appDataPath, "baserom.us.z64");
    static readonly string exePath = Path.Combine(outPath, "sm64coopdx.exe");

    const string shaUsRom = "9bef1128717f958171a4afac3ed78ee2bb4e86ce";

    public static bool DownloadFile(string fileUrl, string savePath, bool overwrite = true) {
        if (overwrite && File.Exists(savePath)) {
            File.Delete(savePath);
        }

        // 15 retry attempts
        for (int i = 0; i < 15; i++) {
            using (WebClient webClient = new WebClient()) {
                try {
                    webClient.DownloadFile(fileUrl, savePath);
                    return true;
                } catch {
                    Write("\nFailed to download file, retrying...", ConsoleColor.Red);
                }
            }
        }
        return false;
    }

    public static void WriteLine(string line, ConsoleColor color = ConsoleColor.Gray) {
        Console.ForegroundColor = color;
        Console.WriteLine(line);
        Console.ForegroundColor = ConsoleColor.Gray;
    }

    public static void Write(string line, ConsoleColor color = ConsoleColor.Gray) {
        Console.ForegroundColor = color;
        Console.Write(line);
        Console.ForegroundColor = ConsoleColor.Gray;
    }

    public static string CalculateFileSHA1(string path) {
        string sha = "";
        using (var fs = new FileStream(path, FileMode.Open))
        using (var bs = new BufferedStream(fs))
        using (var sha1 = new SHA1Managed()) {
            byte[] hash = sha1.ComputeHash(bs);
            StringBuilder formatted = new StringBuilder(2 * hash.Length);
            foreach (byte b in hash) {
                formatted.AppendFormat("{0:X2}", b);
            }
            sha = formatted.ToString();
        }

        return sha;
    }

    public static string CalculateFileMD5(string path) {
        if (!File.Exists(path)) return "";

        using (var md5 = MD5.Create()) {
            using (var stream = File.OpenRead(path)) {
                return Encoding.Default.GetString(md5.ComputeHash(stream));
            }
        }
    }

    public static bool IsSM64USRom(string path) {
        if (string.IsNullOrEmpty(path)) { return false; }

        // calculate sha
        string sha = CalculateFileSHA1(path);

        return sha.ToLower() == shaUsRom.ToLower();
    }

    public static void CreateFolder(string path, bool log = false) {
        if (log) {
            string folderName = new DirectoryInfo(path).Name;
            Write($"Creating {folderName} directory...", ConsoleColor.DarkGray);
        }

        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

        if (log) Write(" Done!\n", ConsoleColor.Green);
    }

    public static void GetROM(string where) {
        // get ROM
        if (!File.Exists(romPath)) {
            if (IsSM64USRom(where)) {
                File.Copy(where.Replace("\"", ""), romPath);
            } else {
                string path = "";
                while (string.IsNullOrEmpty(path) || !IsSM64USRom(path)) {
                    Write("Drag SM64 US .z64 ROM on this window and press enter: ", ConsoleColor.Yellow);
                    path = Console.ReadLine().Replace("\"", "");
                }
                File.Copy(path, romPath);
            }
        }
    }

    public static bool Download(string what, string url, string path, bool log = false) {
        if (log) Write($"Downloading {what}...", ConsoleColor.DarkGray);

        if (!DownloadFile(url, path)) {
            WriteLine($"\nFailed to download {what}! Please try again later.", ConsoleColor.Red);
            return false;
        }

        if (log) Write(" Done!\n", ConsoleColor.Green);
        return true;
    }

    public static void CreateExecutable(string version) {
        Write("Applying patch file...", ConsoleColor.DarkGray);

        // run the patcher
        using (FileStream input = new FileStream(romPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (FileStream output = new FileStream(exePath, FileMode.Create))
            BinaryPatch.Apply(input, () => new FileStream(patchPath, FileMode.Open, FileAccess.Read, FileShare.Read), output);

        File.Delete(patchPath);
        string path = Path.Combine(appDataPath, "version.txt");
        File.WriteAllText(path, version);

        Write(" Done!\n", ConsoleColor.Green);
    }

    public static void RenameFolder(string path, string newPath, bool delete = true) {
        if (!Directory.Exists(path)) return;
        if (delete && Directory.Exists(newPath)) Directory.Delete(newPath, true);

        Directory.Move(path, newPath);
    }

    public static void DeleteFolder(string path) {
        if (!Directory.Exists(path)) return;

        Directory.Delete(path, true);
    }
}
