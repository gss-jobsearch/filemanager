using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

using static FileManager.Models.IFileStorage;

namespace FileManager.Models
{
    public class FileStorage : IFileStorage
    {
        private readonly BlobContainerClient _client;

        public FileStorage(BlobContainerClient client)
        {
            _client = client;
        }

        public async Task<Result> DeleteFile(string path)
        {
            throw new NotImplementedException();
        }

        public async Task<(Result, Stream?)> GetFile(string path)
        {
            throw new NotImplementedException();
        }

        public async Task<(Result, IEnumerable<string>)> List(int maxEntries = 0)
        {
            throw new NotImplementedException();
        }

        public async Task<Result> PutFile(string path, Stream contents)
        {
            throw new NotImplementedException();
        }

    }
}
