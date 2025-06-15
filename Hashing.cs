using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolderSync
{
    internal class Hashing
    {

        public static byte[] ComputeSHA256(string filePath)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    return sha256.ComputeHash(stream);
                }
            }
        }

        public static bool AreFilesEqual(byte[] hash1, byte[] hash2)
        {
            return hash1.SequenceEqual(hash2);
        }
    }
}
