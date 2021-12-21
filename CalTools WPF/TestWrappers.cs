using System.IO;
#nullable enable
namespace CalTools_WPF
{
    //Wrappers used to make classes test-friendly
    public interface IDirectoryWrapper
    {
        public bool Exists(string? path) => Directory.Exists(path);
        public DirectoryInfo? GetParent(string path) => Directory.GetParent(path);
        public string[] GetDirectories(string path) => Directory.GetDirectories(path);
        public string[] GetFiles(string path) => Directory.GetFiles(path);
    }
    public interface IFileWrapper
    {
        public void WriteAllLines(string path, string[] contents) => File.WriteAllLines(path, contents);
    }

    internal class DirectoryWrapper : IDirectoryWrapper { }
    internal class FileWrapper : IFileWrapper { }
}
