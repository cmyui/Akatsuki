using System;
using System.Diagnostics;

namespace Akatsuki {
    class Utils {
        public static void Exit(string msg) {
            Console.WriteLine(msg);
            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();

            Process.GetCurrentProcess().Kill();
        }
    }
}
