using System;
using System.Diagnostics;
using System.IO;
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

        static void markAsRun() {
            //if (!File)
        }

        static void Main(string[] args) {
            var akatsuki_data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Akatsuki");

            if (!Directory.Exists(akatsuki_data)) { // program's first run
                removeCertificates();

                // TODO: create settings & mark as run
            }

            // TODO: read settings

            // check if there are already any osu! processes running,
            // osu! only allows gameplay with multiple clients if the
            // user has administrative privileges..
            // TODO: cli override? lol
            Process[] osu_procs = Process.GetProcessesByName("osu!");
            if (osu_procs.Length != 0) // osu! is already running.
                Utils.Exit($"{osu_procs[0].MainWindowTitle} is already running, and does not allow multiple clients.\n" +
                            "Please close the game client, and try running Akatsuki.exe again.");

            // allow domain from command line args
            var domain = (args.Length == 1) ? args[1] : "akatsuki.pw";

            // find osu! file from various common paths
            var osu_file = "";
            foreach (var path in COMMON_OSU_PATHS) {
                if (Directory.Exists(path)) { // directory found
                    var file_path = Path.Combine(path, "osu!.exe");
                    if (File.Exists(file_path)) { // file found
                        osu_file = file_path;
                        break;
                    }
                }
            }

            if (osu_file == "")  // could not find osu! file
                Utils.Exit("Failed to find osu! directory.\n" +
                           "If you installed osu! normally and don't have a strange installation path, please contact an Akatsuki developer.\n" +
                           "If you have a non-standard osu! path, this program does not currently a custom support path (probably will add).");

            // all checks passed, start the game.
            Process.Start(osu_file, $"-devserver {domain}");
        }
    }
}
