using System.Text;

namespace FolderSync
{
    internal class Program
    {
        public static string source = "";
        public static string destination = "";
        public static string logFilePath = "";
        public static int syncInterval;
        static System.Timers.Timer syncTimer;
        static ManualResetEvent quitEvent = new(false);
        static void Main(string[] args)
        {
            Console.WriteLine($"{DateTime.UtcNow} Welcome to file syncing console app");

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--source" && i + 1 < args.Length)
                {
                    source = args[i + 1];
                }
                else if (args[i] == "--dest" && i + 1 < args.Length)
                {
                    destination = args[i + 1];
                }
                else if (args[i] == "--log" && i + 1 < args.Length)
                {
                    logFilePath = args[i + 1];
                }
                else if (args[i] == "--sync" && i + 1 < args.Length)
                {
                    syncInterval = int.Parse(args[i + 1]);
                }
            }
            ValidateArgs();

            if (!FileDirectory.ValidatePaths(destination))
            {
                Console.WriteLine("Couldnt find the destination path folder");
                Console.WriteLine("Creating folder");
                Directory.CreateDirectory(destination);
                Logging.LogInfo($"Creating new folder {destination}");
            }

            Sync();

            syncTimer = new System.Timers.Timer(syncInterval * 60 * 1000);
            syncTimer.Elapsed += SyncTimer_Elapsed;
            syncTimer.AutoReset = true;
            syncTimer.Enabled = true;

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("Exiting application");
                quitEvent.Set();
            };

            Console.WriteLine("Press Ctrl + C to exit.");
            quitEvent.WaitOne();

        }

        private static void SyncTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            Sync();
        }

        public static void ValidateArgs()
        {
            if (string.IsNullOrEmpty(source))
            {
                Console.WriteLine("Couldn't find source path argument");
                Console.ReadKey();
                Environment.Exit(0);
            }
            if (string.IsNullOrEmpty(destination))
            {
                Console.WriteLine("Coudln't find destination path argument");
                Console.ReadKey();
                Environment.Exit(0);
            }
            if (syncInterval <= 0)
            {
                Console.WriteLine("Sync interval is set to 0 or below");
                Console.ReadKey();
                Environment.Exit(0);
            }
            if (string.IsNullOrEmpty(logFilePath))
            {
                Console.WriteLine("Couldnt find log file path argument");
                Console.ReadKey();
                Environment.Exit(0);
            }
        }

        public static void Sync()
        {
            Stack<(string, bool)> sourceStack = FileDirectory.GetAllPaths(source);
            Stack<(string, bool)> destStack = FileDirectory.GetAllPaths(destination);

            FileDirectory.DeleteFiles(sourceStack, destStack);
            FileDirectory.ValidateFiles(sourceStack);
        }
    }
}
