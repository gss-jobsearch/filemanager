using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FileManager.Models
{
    public interface IFileStorage
    {
        Task<(Result, IEnumerable<string>)> List(int maxEntries = 0);

        Task<(Result, Stream?)> GetFile(string path);

        Task<Result> PutFile(string path, Stream contents);

        Task<Result> DeleteFile(string path);

        public enum Result
        {
            Succeeded,
            Failed,
            Truncated,
            FileExists,
            FileNotFound
        }
    }
}