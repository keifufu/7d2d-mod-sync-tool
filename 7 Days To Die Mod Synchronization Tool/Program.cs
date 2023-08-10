using System;
using System.IO;
using System.Net;
using System.ComponentModel;
using Microsoft.Win32;
using Gameloop.Vdf;
using System.Collections.Generic;
using SharpCompress.Archives;
using System.Linq;

namespace _7_Days_To_Die_Mod_Synchronization_Tool
{
    class Program
    {
        private static readonly string ProjectName = "7 Days To Die Synchronization Tool";
        private static readonly string ArchiveUrl = "https://keifufu.dev/public/7d2d/mods.zip";
        private static readonly string VersionUrl = "https://keifufu.dev/public/7d2d/version.txt";
        private static readonly string AppDataPath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\keifufu\{ProjectName}";
        private static readonly string ArchivePath = $@"{AppDataPath}\mods.zip";
        private static readonly string RegistryPath = $@"SOFTWARE\keifufu\{ProjectName}";
        private static readonly string SteamRegistryPath = @"SOFTWARE\Valve\Steam";

        static void Main(string[] args)
        {
            if (!Directory.Exists(AppDataPath))
            {
                Directory.CreateDirectory(AppDataPath);
            }

            // Make sure it exists before downloading so the user won't have to download the archive for no reason.
            Find7d2dDirectory();

            if (!IsNewVersionAvailable())
            {
                Console.WriteLine("---------------------------");
                Console.WriteLine("No new version is available");
                Console.WriteLine("---------------------------");
                Console.WriteLine();
                Console.WriteLine("Do you wish to continue anyway? (Y)es (N)o");
                ConsoleKeyInfo yesNo2 = Console.ReadKey(true);

                if (yesNo2.Key != ConsoleKey.Y)
                {
                    Environment.Exit(0);
                }
                Console.Clear();
            }
            else
            {
                Console.WriteLine("--------------------------");
                Console.WriteLine("A new version is available");
                Console.WriteLine("--------------------------");
                Console.WriteLine();
            }
            Console.WriteLine("Continuing will overwrite your 7 Days To Die mods folder. This action is irreversible.");
            Console.WriteLine("Are you sure you want to continue? (Y)es (N)o");
            ConsoleKeyInfo yesNo = Console.ReadKey(true);

            if (yesNo.Key != ConsoleKey.Y)
            {
                Environment.Exit(0);
            }
            Console.Clear();

            DownloadArchive();
        }

        private static string GetVersion()
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryPath);
            string version = key.GetValue("version")?.ToString();
            if (version == null)
            {
                key.SetValue("version", "0");
            }
            key.Flush();
            return version;
        }

        private static void SetVersion(string version)
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryPath);
            key.SetValue("version", version);
            key.Flush();
        }

        private static string GetRemoteVersion()
        {
            string remoteVersion = (new WebClient()).DownloadString(VersionUrl);
            return remoteVersion;
        }

        private static bool IsNewVersionAvailable()
        {
            string currentVersion = GetVersion();
            string remoteVersion = GetRemoteVersion();
            return currentVersion != remoteVersion;
        }

        private static string Find7d2dDirectory()
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(SteamRegistryPath);
            string steamDirectory = key.GetValue("SteamPath")?.ToString().Replace("/", @"\");
            if (steamDirectory == null)
            {
                Console.WriteLine("Unable to locate Steam installation.");
                Console.WriteLine("Please contact @keifufu#0727 on Discord for support.");
                Console.WriteLine("Press any key to exit . . .");
                Console.ReadKey();
                Environment.Exit(0);
            }

            List<string> libraryPaths = new List<string>();

            string steamLibraryFoldersPath = $@"{steamDirectory}\steamapps\libraryfolders.vdf";
            dynamic volvo = VdfConvert.Deserialize(File.ReadAllText(steamLibraryFoldersPath));
            for (int i = 0; i < 10; i++)
            {
                var path = Convert.ToString(volvo.Value[Convert.ToString(i)]?.path);
                if (path != null && path.Length > 0)
                {
                    libraryPaths.Add(path);
                }
            }

            string foundPath = "";
            libraryPaths.ForEach(delegate (string path)
            {
                string potention7d2dPath = $@"{path}\steamapps\common\7 Days To Die";
                if (Directory.Exists(potention7d2dPath))
                {
                    foundPath = potention7d2dPath;
                }
            });

            if (foundPath.Length == 0)
            {
                Console.WriteLine("Unable to locate 7 Days To Die installation.");
                Console.WriteLine("Please contact @keifufu#0727 on Discord for support.");
                Console.WriteLine("Press any key to exit . . .");
                Console.ReadKey();
                Environment.Exit(0);
            }

            return foundPath;
        }

        private static void DownloadArchive()
        {
            Console.Write("Downloading mods... ");
            WebClient webClient = new WebClient();
            using ProgressBar progress = new ProgressBar();
            webClient.DownloadFileCompleted += new AsyncCompletedEventHandler((s, e) => DownloadComplete(s, e, progress));
            webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler((s, e) => ProgressChanged(s, e, progress));
            webClient.DownloadFileTaskAsync(new Uri(ArchiveUrl), ArchivePath).Wait();
        }

        private static void ProgressChanged(object sender, DownloadProgressChangedEventArgs e, ProgressBar progress)
        {
            progress.Report((double) e.ProgressPercentage / 100);
        }

        private static void DownloadComplete(object sender, AsyncCompletedEventArgs e, ProgressBar progress)
        {
            progress.Dispose();
            Console.Write("Done.");
            Console.WriteLine();
            InstallMods();
        }

        private static void InstallMods()
        {
            Console.Write("Installing mods... ");
            using ProgressBar progress = new ProgressBar();
            long completed = 0;

            try
            {
                string extractPath = $@"{Find7d2dDirectory()}\mods";
                if (Directory.Exists(extractPath))
                {
                    Directory.Delete(extractPath, true);
                }
                Directory.CreateDirectory(extractPath);

                IArchive archive = ArchiveFactory.Open(ArchivePath);

                double totalSize = archive.Entries.Where(e => !e.IsDirectory).Sum(e => e.Size);

                foreach(IArchiveEntry entry in archive.Entries)
                {
                    string path = $@"{extractPath}\{entry.Key}";
                    if (entry.IsDirectory)
                    {
                        Directory.CreateDirectory(path);
                    }
                    else
                    {
                        entry.WriteToFile(path);
                    }
                    completed += entry.Size;
                    progress.Report(completed / totalSize);
                }

                archive.Dispose();

                Cleanup(progress);
            }
            catch (Exception ex)
            {
                progress.Dispose();
                Console.Write("ERROR");
                Console.WriteLine();
                Console.WriteLine(ex.Message);
                Console.WriteLine("Failed to extract archive.");
                Console.WriteLine("Please contact @keifufu#0727 on Discord for support.");
                Console.WriteLine("Press any key to exit . . .");
                Console.ReadKey();
                Environment.Exit(0);
            }
        }
        private static void Cleanup(ProgressBar progress)
        {
            progress.Dispose();
            File.Delete(ArchivePath);
            string remoteVersion = GetRemoteVersion();
            SetVersion(remoteVersion);
            Console.Write("Done.");
            Console.WriteLine();
            Console.WriteLine("Press any key to exit . . .");
            Console.ReadKey();
            Environment.Exit(0);
        }
    }
}