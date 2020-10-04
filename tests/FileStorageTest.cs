using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileManager.Models;
using FileManager.Tests.Mock;
using Xunit;

using static FileManager.Models.IFileStorage;

namespace FileManager.Tests
{
    public class FileStorageTest
    {
        private const string Filename1 = "foo/bar/baz.jpg";
        private const string Filename2 = "foobar123.txt";

        private static readonly byte[] SomeData =
            Encoding.UTF8.GetBytes("Here's some\nfun\ndata");

        private static MemoryStream SomeDataStream =>
		    new MemoryStream(SomeData);

        private readonly IFileStorage _storage;
        private readonly MockBlobContainerClient _container;

        public FileStorageTest()
        {
            _container = new MockBlobContainerClient();
            _storage = new FileStorage(_container);
        }

        [Fact]
        public async Task TList()
        {
            var (result, list) = await _storage.List();
            Assert.Equal(Result.Succeeded, result);
            Assert.Empty(list);

            _container.StoredFiles.AddOrUpdate(Filename1, SomeDataStream,
				(k, v) => throw new Exception("should not be present"));
            (result, list) = await _storage.List();
            Assert.Equal(Result.Succeeded, result);
            Assert.Single(list, Filename1);

            _container.StoredFiles.AddOrUpdate(Filename2, SomeDataStream,
				(k, v) => throw new Exception("should not be present"));
            (result, list) = await _storage.List();
            Assert.Equal(Result.Succeeded, result);
            Assert.Equal<string>(new string[] { Filename1, Filename2 }.OrderBy(s => s), list.OrderBy(s => s));

            (result, list) = await _storage.List(1);
            Assert.Equal(Result.Truncated, result);
            List<string>? asList = list.ToList();
            Assert.Single(asList);
            Assert.True((asList[0] == Filename1) || (asList[0] == Filename2));
        }

        [Fact]
        public async Task TGetFile()
        {
            _container.StoredFiles.AddOrUpdate(Filename1, SomeDataStream,
				(k, v) => throw new Exception("should not be present"));
            var (result, stream) = await _storage.GetFile(Filename1);
            var buffer = new byte[SomeData.Length];

            Assert.Equal(Result.Succeeded, result);
            Assert.NotNull(stream);
            int readCount = await stream!.ReadAsync(buffer, 0, SomeData.Length);
            Assert.Equal(SomeData.Length, readCount);
            Assert.Equal<byte>(SomeData, buffer);
            Assert.Equal(0, await stream!.ReadAsync(buffer, 0, SomeData.Length));

            (result, stream) = await _storage.GetFile(Filename2);
            Assert.Equal(Result.FileNotFound, result);
            Assert.Null(stream);
        }

        [Fact]
        public async Task TPutFile()
        {
            var result = await _storage.PutFile(Filename1, SomeDataStream);
            Assert.Equal(Result.Succeeded, result);

            _container.StoredFiles.AddOrUpdate(Filename2, SomeDataStream,
				(k, v) => throw new Exception("should not be present"));

            result = await _storage.PutFile(Filename2, SomeDataStream);
            Assert.Equal(Result.FileExists, result);
        }

        [Fact]
        public async Task TDeleteFile()
        {
            var result = await _storage.DeleteFile(Filename1);
            Assert.Equal(Result.FileNotFound, result);
            Assert.Empty(_container.StoredFiles);

            _container.StoredFiles.AddOrUpdate(Filename2, SomeDataStream,
				(k, v) => throw new Exception("should not be present"));
            result = await _storage.DeleteFile(Filename2);
            Assert.Equal(Result.Succeeded, result);
            Assert.Empty(_container.StoredFiles);

            _container.StoredFiles.AddOrUpdate(Filename1, SomeDataStream,
				(k, v) => throw new Exception("should not be present"));
            _container.StoredFiles.AddOrUpdate(Filename2, SomeDataStream,
				(k, v) => throw new Exception("should not be present"));
            result = await _storage.DeleteFile(Filename2);
            Assert.Equal(Result.Succeeded, result);
            Assert.Single(_container.StoredFiles.Keys, Filename1);
        }
    }
}
