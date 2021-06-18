using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalTools_WPF
{
    public interface IDirectoryWrapper
    {
        public bool Exists(string? path);
        public DirectoryInfo? GetParent(string path);
        public string[] GetDirectories(string path);
        public string[] GetFiles(string path);
    }
    internal class DirectoryWrapper : IDirectoryWrapper
    {
        public bool Exists(string? path) => Directory.Exists(path);
        public DirectoryInfo? GetParent(string path) => Directory.GetParent(path);
        public string[] GetDirectories(string path) => Directory.GetDirectories(path);
        public string[] GetFiles(string path) => Directory.GetFiles(path);
    }


    public interface IFileWrapper
    {
        public void WriteAllLines(string path, string[] contents);
    }
    public class FileWrapper : IFileWrapper
    {
        public void WriteAllLines(string path, string[] contents)
        {
            File.WriteAllLines(path, contents);
        }
    }

}
