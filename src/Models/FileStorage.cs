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
            BlobClient blob = _client.GetBlobClient(path);
            try
            {
                using Response response = await blob.DeleteAsync();
			    return ((response.Status / 100) == 2) ? Result.Succeeded : Result.Failed;
            }
            catch (RequestFailedException ex)
            {
                return ex.Message.Contains("BlobNotFound") ?
				    Result.FileNotFound : Result.Failed;
            }
        }

        public async Task<(Result, Stream?)> GetFile(string path)
        {
            BlobClient blob = _client.GetBlobClient(path);
            try
            {
                bool exists = await blob.ExistsAsync();
                return exists ?
					(Result.Succeeded, await blob.OpenReadAsync()) :
                    (Result.FileNotFound, null);
            }
            catch (RequestFailedException)
            {
                return (Result.Failed, null);
            }
        }

        public async Task<(Result, IEnumerable<string>)> List(int maxEntries = 0)
        {
            var paths = new List<string>();
            var blobs = _client.GetBlobsAsync(BlobTraits.None);
            try
            {
                if (maxEntries > 0)
                {
                    int count = 0;
                    await foreach (var blob in blobs)
                    {
                        if (++count > maxEntries)
                        {
                            return (Result.Truncated, paths);
                        }
                        paths.Add(blob.Name);
                    }
                }
                else
                {
                    await foreach (var blob in blobs)
                    {
                        paths.Add(blob.Name);
                    }
                }
                return (Result.Succeeded, paths);
            }
            catch (RequestFailedException)
            {
                return (Result.Failed, new string[0]);
            }
        }

        public async Task<Result> PutFile(string path, Stream contents)
        {
            BlobClient blob = _client.GetBlobClient(path);
            try
            {
                Response response = (await blob.UploadAsync(contents, overwrite: false)).GetRawResponse();
			    return ((response.Status / 100) == 2) ? Result.Succeeded : Result.Failed;
            }
            catch (RequestFailedException ex)
            {
                return ex.Message.Contains("BlobAlreadyExists") ?
                    Result.FileExists : Result.Failed;
            }
        }

    }
}
