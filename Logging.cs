using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolderSync
{
    internal class Logging
    {

        private static object _lock = new object();

        public static void LogInfo(string info)
        {
            lock (_lock)
            {
                string finalMessage = $"[{DateTime.UtcNow}] {info}";
                Console.WriteLine(finalMessage);
                try
                {
                    using (StreamWriter writer = new StreamWriter(Path.Combine(Program.logFilePath, "Log.txt"), append: true))
                    {
                        writer.AutoFlush = true;
                        writer.WriteLine(finalMessage);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Couldnt write to log file! {ex.Message}");
                }
            }
            
        }




    }
}
