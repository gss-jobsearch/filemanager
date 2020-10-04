using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using FileManager.Controllers;
using FileManager.Models;
using FileManager.Tests.Mock;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FileManager.Tests
{
    public class FilesControllerTest
    {
        private const string Filename1 = "foo/bar/baz.jpg";
        private const string Filename2 = "foobar123.txt";

        private static readonly byte[] SomeData =
            System.Text.Encoding.UTF8.GetBytes("Here's some\nfun\ndata");

        private static MemoryStream SomeDataStream =>
            new MemoryStream(SomeData);

        private readonly MockBlobContainerClient _container;
        private readonly IServiceProvider _services;

        private FilesController Controller
        {
            get
            {
                var controller = _services.GetRequiredService<FilesController>();
                controller.ControllerContext = new ControllerContext();
                controller.ControllerContext.HttpContext = new DefaultHttpContext();
                return controller;
            }
        }

        public FilesControllerTest()
        {
            var services = new ServiceCollection();
            _container = new MockBlobContainerClient();
            services.AddSingleton<BlobContainerClient>(_container);
            services.AddSingleton<IFileStorage, FileStorage>();
            services.AddTransient<FilesController>();
            _services = services.BuildServiceProvider();
        }

        [Fact]
        public async Task TList()
        {
            IActionResult result = await Controller.List();
            Assert.IsAssignableFrom<ObjectResult>(result);
            ObjectResult objectResult = (ObjectResult)result;
            Assert.Equal(200, objectResult.StatusCode);
            Assert.IsAssignableFrom<IEnumerable<string>>(objectResult.Value);
            IEnumerable<string> list = (IEnumerable<string>)objectResult.Value;
            Assert.Empty(list);

            _container.StoredFiles.AddOrUpdate(Filename1, SomeDataStream,
                (k, v) => throw new Exception("should not be present"));
            result = await Controller.List();
            Assert.IsAssignableFrom<ObjectResult>(result);
            objectResult = (ObjectResult)result;
            Assert.Equal(200, objectResult.StatusCode);
            Assert.IsAssignableFrom<IEnumerable<string>>(objectResult.Value);
            list = (IEnumerable<string>)objectResult.Value;
            Assert.Single(list, Filename1);

            _container.StoredFiles.AddOrUpdate(Filename2, SomeDataStream,
                (k, v) => throw new Exception("should not be present"));
            result = await Controller.List();
            Assert.IsAssignableFrom<ObjectResult>(result);
            objectResult = (ObjectResult)result;
            Assert.Equal(200, objectResult.StatusCode);
            Assert.IsAssignableFrom<IEnumerable<string>>(objectResult.Value);
            list = (IEnumerable<string>)objectResult.Value;
            Assert.Equal<string>(new string[] { Filename1, Filename2 }.OrderBy(s => s), list.OrderBy(s => s));

            result = await Controller.List(1);
            Assert.IsAssignableFrom<ObjectResult>(result);
            objectResult = (ObjectResult)result;
            Assert.Equal(206, objectResult.StatusCode);
            Assert.IsAssignableFrom<IEnumerable<string>>(objectResult.Value);
            list = (IEnumerable<string>)objectResult.Value;
            List<string>? asList = list.ToList();
            Assert.Single(asList);
            Assert.True((asList[0] == Filename1) || (asList[0] == Filename2));
        }

        [Fact]
        public async Task TGetFile()
        {
            _container.StoredFiles.AddOrUpdate(Filename1, SomeDataStream,
                (k, v) => throw new Exception("should not be present"));
            IActionResult result = await Controller.Download(Filename1);
            Assert.IsAssignableFrom<FileStreamResult>(result);
            FileStreamResult streamResult = (FileStreamResult)result;
            Stream stream = (Stream)streamResult.FileStream;
            var buffer = new byte[SomeData.Length];

            int readCount = await stream.ReadAsync(buffer, 0, SomeData.Length);
            Assert.Equal(SomeData.Length, readCount);
            Assert.Equal<byte>(SomeData, buffer);
            Assert.Equal(0, await stream.ReadAsync(buffer, 0, SomeData.Length));

            result = await Controller.Download(Filename2);
            Assert.IsAssignableFrom<IStatusCodeActionResult>(result);
            IStatusCodeActionResult statusResult = (IStatusCodeActionResult)result;
            Assert.Equal(404, statusResult.StatusCode);
        }

        [Fact]
        public async Task TPutFile()
        {
            var controller = Controller;
            controller.HttpContext.Request.Body = SomeDataStream;
            var result = await controller.Upload(Filename1);
            Assert.IsAssignableFrom<IStatusCodeActionResult>(result);
            IStatusCodeActionResult statusResult = (IStatusCodeActionResult)result;
            Assert.Equal(201, statusResult.StatusCode);

            _container.StoredFiles.AddOrUpdate(Filename2, SomeDataStream,
                (k, v) => throw new Exception("should not be present"));

            controller = Controller;
            controller.HttpContext.Request.Body = SomeDataStream;
            result = await controller.Upload(Filename2);
            Assert.IsAssignableFrom<IStatusCodeActionResult>(result);
            statusResult = (IStatusCodeActionResult)result;
            Assert.Equal(409, statusResult.StatusCode);
        }

        [Fact]
        public async Task TDeleteFile()
        {
            IActionResult result = await Controller.Delete(Filename1);
            Assert.IsAssignableFrom<IStatusCodeActionResult>(result);
            IStatusCodeActionResult statusResult = (IStatusCodeActionResult)result;
            Assert.Equal(404, statusResult.StatusCode);
            Assert.Empty(_container.StoredFiles);

            _container.StoredFiles.AddOrUpdate(Filename2, SomeDataStream,
                (k, v) => throw new Exception("should not be present"));
            result = await Controller.Delete(Filename2);
            Assert.IsAssignableFrom<IStatusCodeActionResult>(result);
            statusResult = (IStatusCodeActionResult)result;
            Assert.Equal(202, statusResult.StatusCode);
            Assert.Empty(_container.StoredFiles);

            _container.StoredFiles.AddOrUpdate(Filename1, SomeDataStream,
                (k, v) => throw new Exception("should not be present"));
            _container.StoredFiles.AddOrUpdate(Filename2, SomeDataStream,
                (k, v) => throw new Exception("should not be present"));
            result = await Controller.Delete(Filename2);
            Assert.IsAssignableFrom<IStatusCodeActionResult>(result);
            statusResult = (IStatusCodeActionResult)result;
            Assert.Equal(202, statusResult.StatusCode);
            Assert.Single(_container.StoredFiles.Keys, Filename1);
        }

    }
}
