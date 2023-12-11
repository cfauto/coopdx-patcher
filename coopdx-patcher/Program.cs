using System;
using System.IO;
using System.IO.Compression;

static class Program {
    static void Main(string[] args) {
        Patcher.WriteLine("coopdx-patcher v0.1", ConsoleColor.Cyan);

        Patcher.CreateFolder(Patcher.appDataPath);
        Patcher.GetROM(args);
        Patcher.Download("patch file", Patcher.patchUrl, Patcher.patchPath, true);
        Patcher.CreateFolder(Patcher.outPath, true);
        Patcher.CreateExecutable();

        Patcher.Write("Downloading DLLs...", ConsoleColor.DarkGray);
        Patcher.Download("bass.dll", $"https://sm64coopdx.com/download/dlls/{Patcher.bit}/bass.dll", Path.Combine(Patcher.outPath, "bass.dll"));
        Patcher.Download("bass_fx.dll", $"https://sm64coopdx.com/download/dlls/{Patcher.bit}/bass_fx.dll", Path.Combine(Patcher.outPath, "bass_fx.dll"));
        Patcher.Download("discord_game_sdk.dll", $"https://sm64coopdx.com/download/dlls/{Patcher.bit}/discord_game_sdk.dll", Path.Combine(Patcher.outPath, "discord_game_sdk.dll"));
        Patcher.Write(" Done!\n", ConsoleColor.Green);

        Patcher.Write("Downloading resources...", ConsoleColor.DarkGray);
        Patcher.Download("resources.zip", "https://sm64coopdx.com/download/resources.zip", Patcher.resourcesPath);
        Patcher.Write(" Done!\n", ConsoleColor.Green);

        // extract resources zip
        Patcher.Write("Extracting resources...", ConsoleColor.DarkGray);

        // keep original dynos and mods folders intact
        Patcher.RenameFolder(Path.Combine(Patcher.outPath, "mods"), Path.Combine(Patcher.outPath, "mods_backup"));
        Patcher.RenameFolder(Path.Combine(Patcher.outPath, "dynos"), Path.Combine(Patcher.outPath, "dynos_backup"));

        // extract resources (dynos, lang and mods)
        Patcher.DeleteFolder(Path.Combine(Patcher.outPath, "lang"));
        ZipFile.ExtractToDirectory(Patcher.resourcesPath, Patcher.outPath);

        Patcher.Write(" Done!\n", ConsoleColor.Green);

        // clean up
        Patcher.Write("Cleaning up...", ConsoleColor.DarkGray);
        File.Delete(Patcher.resourcesPath);
        Patcher.Write(" Done!\n", ConsoleColor.Green);

        Patcher.WriteLine("sm64coopdx has been created, have fun :)", ConsoleColor.Yellow);
        Patcher.WriteLine("Press any key to exit.", ConsoleColor.Yellow);
        Console.ReadKey();
    }
}