
using System.Net.Http;
using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using coopdx_patcher.Properties;
using System.Security.Cryptography;
using System.Text;
using System.Reflection;

static class Patcher {
    public static readonly string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "coopdx-patcher");
    public static readonly string outPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "sm64coopdx");
    public static readonly string patchPath = Path.Combine(appDataPath, "sm64coopdx.bps");
    public static readonly string resourcesPath = Path.Combine(outPath, "resources.zip");
    static readonly string romPath = Path.Combine(appDataPath, "baserom.us.z64");

    public static readonly string bit = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";
    public static readonly string patchUrl = $"https://sm64coopdx.com/download/sm64coopdx_Windows_{bit}.bps";

    const string shaUsRom = "9bef1128717f958171a4afac3ed78ee2bb4e86ce";

    public static bool DownloadFile(string fileUrl, string savePath, bool overwrite = true) {
        if (overwrite && File.Exists(savePath)) {
            File.Delete(savePath);
        }

        try {
            using (HttpClient client = new HttpClient()) {
                HttpResponseMessage response = client.GetAsync(fileUrl).Result;

                if (response.IsSuccessStatusCode) {
                    using (Stream contentStream = response.Content.ReadAsStreamAsync().Result)
                    using (Stream fileStream = File.Create(savePath)) {
                        contentStream.CopyTo(fileStream);
                    }
                    return true;
                } else {
                    return false;
                }
            }
        } catch {
            return false;
        }
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
        using (var fs = new FileStream(path.Replace("\"", ""), FileMode.Open))
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

    public static void GetROM(string[] args) {
        // get ROM
        if (!File.Exists(romPath)) {
            if (args.Length > 0 && IsSM64USRom(args[0])) {
                File.Copy(args[0], romPath);
            } else {
                string path = "";
                while (string.IsNullOrEmpty(path) || !IsSM64USRom(path)) {
                    Write("Drag SM64 US .z64 ROM on this window and press enter: ", ConsoleColor.Yellow);
                    path = Console.ReadLine();
                }
                File.Copy(path, romPath);
            }
        }
    }

    public static void Download(string what, string url, string path, bool log = false) {
        if (log) Write($"Downloading {what}...", ConsoleColor.DarkGray);

        if (!DownloadFile(url, path)) {
            WriteLine($"Failed to download {what}!", ConsoleColor.Red);
            return;
        }

        if (log) Write(" Done!\n", ConsoleColor.Green);
    }

    public static void CreateExecutable() {
        Write("Applying patch file...", ConsoleColor.DarkGray);

        // write patcher to AppData
        string patcherPath = Path.Combine(appDataPath, "flips.exe");
        if (!File.Exists(patcherPath)) {
            File.WriteAllBytes(patcherPath, Resources.flips);
        }

        // create the patcher, patch sm64coopdx into the ROM
        ProcessStartInfo startInfo = new ProcessStartInfo() {
            FileName = patcherPath,
            Arguments = $"-a {patchPath} {romPath} {Path.Combine(Path.GetFullPath(outPath), "sm64coopdx.exe")}",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // run the patcher
        Process process = new Process() { StartInfo = startInfo };
        process.Start();
        process.WaitForExit();

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
