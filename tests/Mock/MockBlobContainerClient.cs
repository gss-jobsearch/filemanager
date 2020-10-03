using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace FileManager.Tests.Mock
{
    public class MockBlobContainerClient : BlobContainerClient
    {
        public readonly ConcurrentDictionary<string, MemoryStream> StoredFiles =
			new ConcurrentDictionary<string, MemoryStream>();

        public override Task<Response> DeleteBlobAsync(
            string blobName,
            DeleteSnapshotsOption snapshotsOption = DeleteSnapshotsOption.None,
            BlobRequestConditions? conditions = null,
            CancellationToken cancellationToken = default)
        {
            if (!StoredFiles.TryRemove(blobName, out var oldValue))
            {
                throw new RequestFailedException("error: BlobNotFound");
            }
            return Task.FromResult<Response>(new MockResponse(202));
        }

        public override BlobClient GetBlobClient(string blobName) =>
		    new MockBlobClient(blobName, this);

        public override AsyncPageable<BlobItem> GetBlobsAsync(
            BlobTraits traits = BlobTraits.None,
            BlobStates states = BlobStates.None,
            string? prefix = null,
            CancellationToken cancellationToken = default)
        {
            return new BlobsList(StoredFiles.Keys);
        }

        public override async Task<Response<BlobContentInfo>> UploadBlobAsync(
            string blobName,
            Stream content,
            CancellationToken cancellationToken = default)
        {
            var destination = new MemoryStream();
            StoredFiles.AddOrUpdate(blobName, destination, (k, v) =>
				throw new RequestFailedException("error: BlobAlreadyExists"));
            await content.CopyToAsync(destination);
            return new MockResponse<BlobContentInfo>(201);
        }

        public Task<Stream> OpenReadBlobAsync(
            string name,
            long position = 0,
            int? bufferSize = null,
            BlobRequestConditions? conditions = null,
            CancellationToken cancellationToken = default)
        {
            if (!StoredFiles.TryGetValue(name, out var stream))
            {
                throw new RequestFailedException("error: BlobNotFound");
            }
            stream.Position = position;
            return Task.FromResult<Stream>(stream);
        }

        public Task<Response<bool>> BlobExistsAsync(string name, CancellationToken cancellationToken = default)
        {
            Response<bool> response = new MockBoolResponse(StoredFiles.ContainsKey(name));
            return Task.FromResult(response);
        }

        public class BlobsList : AsyncPageable<BlobItem>
        {
            private readonly IReadOnlyList<BlobItem> _items;

            public BlobsList(IEnumerable<string> names)
            {
                _items = names
		            .Select(name => BlobsModelFactory.BlobItem(name))
		            .ToList();
		    }

            public override async IAsyncEnumerable<Page<BlobItem>> AsPages(string? continuationToken = null, int? pageSizeHint = null)
            {
                await Task.Delay(0);
                yield return new Page(_items);
            }

            private class Page : Page<BlobItem>
            {
                public override IReadOnlyList<BlobItem> Values { get; }

                public Page(IReadOnlyList<BlobItem> values) => Values = values;

                public override string? ContinuationToken => null;

                public override Response GetRawResponse() => new MockResponse(200);
            }
        }

        private class MockBlobClient : BlobClient
        {
            private readonly MockBlobContainerClient _container;

            public override string Name { get; }

            public MockBlobClient(string name, MockBlobContainerClient container)
            {
                Name = name;
                _container = container;
            }

            public override Task<Response> DeleteAsync(
                DeleteSnapshotsOption snapshotsOption = DeleteSnapshotsOption.None,
                BlobRequestConditions? conditions = null,
                CancellationToken cancellationToken = default)
            {
                return _container.DeleteBlobAsync(Name, snapshotsOption, conditions, cancellationToken);
            }

            public override Task<Response<bool>> ExistsAsync(CancellationToken cancellationToken = default)
            {
                return _container.BlobExistsAsync(Name, cancellationToken);
            }

            public override Task<Response<BlobContentInfo>> UploadAsync(
                Stream content,
                bool overwrite = false,
                CancellationToken cancellationToken = default)
            {
                return _container.UploadBlobAsync(Name, content, cancellationToken);
            }

            public override Task<Stream> OpenReadAsync(
                long position = 0,
                int? bufferSize = null,
                BlobRequestConditions? conditions = null,
                CancellationToken cancellationToken = default)
            {
                return _container.OpenReadBlobAsync(Name, position, bufferSize, conditions, cancellationToken);
            }
        }

        private class MockResponse : Response
        {
            public override int Status { get; }

            public override string ReasonPhrase => throw new NotImplementedException();

            public override Stream? ContentStream
            {
                get => throw new NotImplementedException();
                set => throw new NotImplementedException();
            }
            public override string ClientRequestId
            {
                get => throw new NotImplementedException();
                set => throw new NotImplementedException();
            }

            public override void Dispose() { }

            public MockResponse(int status) => Status = status;

            protected override bool ContainsHeader(string name) => throw new NotImplementedException();

            protected override IEnumerable<HttpHeader> EnumerateHeaders() => throw new NotImplementedException();

            protected override bool TryGetHeader(string name, [NotNullWhen(true)] out string? value) => throw new NotImplementedException();

            protected override bool TryGetHeaderValues(string name, [NotNullWhen(true)] out IEnumerable<string>? values) => throw new NotImplementedException();
        }

        private class MockResponse<T> : Response<T>
        {
            public override T Value => throw new NotImplementedException();
            private readonly Response _response;

            public MockResponse(int status) => _response = new MockResponse(status);

            public override Response GetRawResponse() => _response;
        }

        private class MockBoolResponse : MockResponse<bool>
        {
            public override bool Value { get; }

            public MockBoolResponse(bool value) : base(200)
            {
                Value = value;
            }
        }

    }
}