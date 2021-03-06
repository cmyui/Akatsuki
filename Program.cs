using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;

/* 
 * This program replaces the previously used server switchers.
 * osu! now supports a simple command line argument to switch.
 */

namespace Akatsuki {
    class Program {
        static StoreLocation[] X509_STORES = { StoreLocation.LocalMachine, StoreLocation.CurrentUser };
        static string[] COMMON_OSU_PATHS = new string[]{ // TODO: more
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "osu!"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "osu!"),
        };

        static bool requestedExecutionLevel(WindowsBuiltInRole role) {
            using (var identity = WindowsIdentity.GetCurrent()) {
                var principal = new WindowsPrincipal(identity);

                return principal.IsInRole(role);
            }
        }

        static void removeCertificates() {
            if (!requestedExecutionLevel(WindowsBuiltInRole.Administrator))
                Utils.Exit("Please run Akatsuki.exe as Administrator - this is required only " +
                           "the first run, so the program can remove any old *.ppy.sh certificates.");

            foreach (var location in X509_STORES) {
                using (var store = new X509Store(StoreName.Root, location)) {
                    // open with read & write perms
                    store.Open(OpenFlags.ReadWrite);

                    // search for akatsuki's cert
                    var certs = store.Certificates.Find(X509FindType.FindBySubjectName, "TheHangout", false);

                    // remove any matches
                    foreach (var cert in certs)
                        store.Remove(cert);
                }
            }
        }

        static void Main(string[] args) {
            var SETTINGS_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Akatsuki");
            var SETTINGS_FILE = Path.Combine(SETTINGS_PATH, "settings.db");

            // settings
            string domain, osu_file_path;
            bool multi_client;

            if (!Directory.Exists(SETTINGS_PATH)) { // program's first run
                removeCertificates();

                // create settings dir & file
                Directory.CreateDirectory(SETTINGS_PATH);
                using (File.Create(SETTINGS_FILE)) { };

                // default settings
                domain = "akatsuki.pw";
                osu_file_path = null;
                multi_client = false;
            } else if (!File.Exists(SETTINGS_FILE)) { // file doesn't exist
                // default settings
                domain = "akatsuki.pw";
                osu_file_path = null;
                multi_client = false;
            } else { // dir & file exist
                // read settings file & make sure format is correct
                string[] lines = File.ReadAllLines(SETTINGS_FILE);
                if (lines.Count(line => !string.IsNullOrWhiteSpace(line)) != 3) {
                    // file corrupted? (TODO: better)
                    File.Delete(SETTINGS_FILE);
                    Directory.Delete(SETTINGS_PATH);
                    Utils.Exit("Settings file corruption detected :(");
                }

                // use saved settings
                domain = lines[0];
                osu_file_path = lines[1];
                multi_client = lines[2].ToLower() == "true";
            }

            // check if there are already any osu! processes running,
            // osu! only allows gameplay with multiple clients if the
            // user has administrative privileges..
            // TODO: cli override? lol
            Process[] osu_procs = Process.GetProcessesByName("osu!");
            if (osu_procs.Length != 0) // osu! is already running.
                Utils.Exit($"{osu_procs[0].MainWindowTitle} is already running, and does not allow multiple clients.\n" +
                            "Please close the game client, and try running Akatsuki.exe again.");

            if (osu_file_path == null) {
                // find osu! file from various common paths
                foreach (var path in COMMON_OSU_PATHS) {
                    if (Directory.Exists(path)) { // directory found
                        var file_path = Path.Combine(path, "osu!.exe");
                        if (File.Exists(file_path)) { // file found
                            osu_file_path = file_path;
                            break;
                        }
                    }
                }
            } else {
                // osu file path provided, make sure it exists.
                if (!File.Exists(osu_file_path))
                    osu_file_path = null;
            }

            if (osu_file_path == null)  // could not find osu! file
                Utils.Exit("Failed to find osu! directory.\n" +
                           "If you installed osu! normally and don't have a strange installation path, please contact an Akatsuki developer.\n" +
                           "If you have a non-standard osu! path, this program does not currently a custom support path (probably will add).");

            // write to settings file
            File.WriteAllLines(SETTINGS_FILE, new string[] {
                domain,
                osu_file_path,
                multi_client ? "true" : "false"
            });

            var cli_args = $"-devserver {domain}";
            if (multi_client)
                cli_args += " -multi";

            // all checks passed, start the game.
            Process.Start(osu_file_path, cli_args);
        }
    }
}
