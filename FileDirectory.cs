using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace FolderSync
{
    internal class FileDirectory
    {

        public static bool ValidatePaths(string path)
        {
            return Directory.Exists(path);
        }

        public static bool ValidateFileExists(string path)
        {
            return File.Exists(path);
        }

        public static string NormalizePath(string path)
        {
            if (IsNormalized(path)) return path;

            return path.Length > 260 ? @"\\?\" + path : path;
        }

        public static bool IsNormalized(string path)
        {
            return path.StartsWith(@"\\?\");
        }

        public static Stack<(string, bool)> GetAllPaths(string path)
        {
            Stack<string> dirs = new Stack<string>();
            dirs.Push(NormalizePath(path));

            Stack<(string, bool)> dirsReturn = new Stack<(string, bool)>();

            while (dirs.Count > 0)
            {
                string current = dirs.Pop();

                try
                {
                    foreach (var file in Directory.GetFiles(current))
                    {
                        dirsReturn.Push((NormalizePath(file), false));

                    }

                    foreach (var subdir in Directory.GetDirectories(current))
                    {
                        dirsReturn.Push((NormalizePath(subdir), true));
                        dirs.Push(NormalizePath(subdir));
                    }
                }
                catch (Exception ex)
                {
                    Logging.LogInfo($"Couldnt get information about all files and paths in {current}");
                }

            }
            return dirsReturn;
        }

        public static bool CompareFileInfos(string file1FilePath, string file2FilePath)
        {
            FileInfo f1 = new FileInfo(file1FilePath);
            FileInfo f2 = new FileInfo(file2FilePath);

            if (!f2.Exists)
                return false;

            if (f1.Length != f2.Length)
                return false;



            var hashTask1 = Task.Run(() => Hashing.ComputeSHA256(file1FilePath));
            var hashTask2 = Task.Run(() => Hashing.ComputeSHA256(file2FilePath));

            Task.WaitAll(hashTask1, hashTask2);


            return Hashing.AreFilesEqual(hashTask1.Result,hashTask2.Result);
            
        }

        public static void ValidateFiles(Stack<(string path, bool isFolder)> stack)
        {
            foreach (var (path, isFolder) in stack)
            {
                if (isFolder)
                {
                    string destPath = path.Replace(Program.source, Program.destination);
                    if (!Directory.Exists(destPath))
                    {
                        Directory.CreateDirectory(destPath);
                        Logging.LogInfo($"Created folder at {destPath}");
                    }
                }
            }


            var files = stack.Where(item => !item.isFolder).ToArray();
            Parallel.ForEach(files, (file) =>
            {
                string destPath = file.path.Replace(Program.source, Program.destination);
                if (!CompareFileInfos(file.path, destPath))
                {
                    SafeOverwrite(file.path, destPath);
                }
            });
        }

        public static void SafeOverwrite(string sourceFilePath, string destFilePath)
        {
            string tempFileLocation = Path.Combine(Path.GetDirectoryName(destFilePath), Path.GetRandomFileName());
            string fileName = Path.GetFileName(sourceFilePath);
            File.Copy(sourceFilePath, tempFileLocation, overwrite: true);
            Logging.LogInfo($"Copied file from {sourceFilePath} to {Path.GetDirectoryName(tempFileLocation)}");
            if (File.Exists(destFilePath))
            {
                Logging.LogInfo($"Overwritten existing file {fileName} from source");
                File.Replace(tempFileLocation, destFilePath, null);
            }
            else
            {
                Logging.LogInfo($"Moved new file to destination {fileName}");
                File.Move(tempFileLocation, destFilePath);
            }


            
        }

        public static void DeleteFiles(Stack<(string path, bool isFolder)> sourceFiles, Stack<(string path, bool isFolder)> destFiles)
        {

            var normalizedA = sourceFiles.Select(item => (path: item.Item1.Replace(Program.source, Program.destination), item.Item2)).ToList();
            var setA = new HashSet<(string, bool)>(normalizedA);


            var onlyInB = destFiles.Where(item => !setA.Contains(item)).ToList();

            var filesToDelete = onlyInB.Where(item => !item.isFolder).ToList();
            Parallel.ForEach(filesToDelete, file =>
            {
                try
                {
                    RemoveAttributes(file.path);
                    Logging.LogInfo($"Deleting file at {file.path}");
                    File.Delete(file.path);
                }
                catch (Exception ex)
                {
                    Logging.LogInfo($"Failed to delete file {file.path}: {ex.Message}");
                }
            });


            var dirsToDelete = onlyInB
                .Where(item => item.isFolder)
                .Select(item => item.path)
                .OrderByDescending(path => path.Count(c => c == Path.DirectorySeparatorChar))
                .ToList();

            foreach (var dir in dirsToDelete)
            {
                try
                {
                    RemoveAttributes(dir);
                    Directory.Delete(dir, false);
                    Logging.LogInfo($"Deleted directory {dir}");
                }
                catch (Exception ex)
                {
                    Logging.LogInfo($"Can't delete directory {dir}: {ex.Message}");
                }
            }
        }

        public static void RemoveAttributes(string directoryPath)
        {
            if (File.Exists(directoryPath))
            {
                var fileInfo = new FileInfo(directoryPath);
                var attributes = fileInfo.Attributes;
                attributes &= ~FileAttributes.Hidden;
                attributes &= ~FileAttributes.ReadOnly;
                attributes &= ~FileAttributes.System;
                fileInfo.Attributes = attributes;
            }
            else if (Directory.Exists(directoryPath))
            {
                var directoryInfo = new DirectoryInfo(directoryPath);
                var attributes = directoryInfo.Attributes;
                attributes &= ~FileAttributes.Hidden;
                attributes &= ~FileAttributes.ReadOnly;
                attributes &= ~FileAttributes.System;
                directoryInfo.Attributes = attributes;
            }
            else
            {
                Logging.LogInfo($"Path does not exist {directoryPath}");
            }
        }
    }
}
