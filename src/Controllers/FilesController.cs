using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FileManager.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileManager.Controllers
{
    [ApiController]
    [Route("api/file")]
    public class FilesController : ControllerBase
    {
        private const int MaxBlobItems = 500;

        private readonly IFileStorage _storage;

        public FilesController(IFileStorage storage)
        {
            _storage = storage;
        }

        [HttpPost("{**path:required}")]
        [BinaryContent]
        [ProducesResponseType(typeof(string), 409)]
        [ProducesResponseType(201)]
        public async Task<IActionResult> Upload(string path)
        {
            throw new NotImplementedException();
        }

        [HttpGet("{**path:required}")]
        [ProducesErrorResponseType(typeof(void))]
        [ProducesResponseType(404)]
        [Produces("application/octet-stream", Type = typeof(byte[]))]
        [ProducesResponseType(typeof(byte[]), 200)]
        public async Task<IActionResult> Download(string path)
        {
            throw new NotImplementedException();
        }

        [HttpGet()]
        [ProducesResponseType(typeof(IEnumerable<string>), 200)]
        [ProducesResponseType(typeof(IEnumerable<string>), 206)]
        public async Task<IActionResult> List([FromQuery] int max = MaxBlobItems)
        {
            throw new NotImplementedException();
        }

        [HttpDelete("{**path:required}")]
        [ProducesErrorResponseType(typeof(void))]
        [ProducesResponseType(404)]
        [ProducesResponseType(202)]
        public async Task<IActionResult> Delete(string path)
        {
            throw new NotImplementedException();
        }

    }
}
