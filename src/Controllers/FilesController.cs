using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FileManager.Models;
using Microsoft.AspNetCore.Mvc;

using static FileManager.Models.IFileStorage;

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
        [DisableFormValueModelBinding]
        [ProducesResponseType(typeof(string), 409)]
        [ProducesResponseType(201)]
        public async Task<IActionResult> Upload(string path)
        {
            path = UnSwaggerPath(path);
            var result = await _storage.PutFile(path, Request.Body);
            switch (result)
            {
                case Result.Succeeded:
                    return CreatedAtAction(nameof(Download),
						new { path = path },
                        null);
                case Result.FileExists:
                    return Conflict("File Exists");
                default:
                    return Problem();
            }
        }

        [HttpGet("{**path:required}")]
        [ProducesErrorResponseType(typeof(void))]
        [ProducesResponseType(404)]
        [Produces("application/octet-stream", Type = typeof(byte[]))]
        [ProducesResponseType(typeof(byte[]), 200)]
        public async Task<IActionResult> Download(string path)
        {
            path = UnSwaggerPath(path);
            var (result, stream) = await _storage.GetFile(path);
            switch (result)
            {
                case Result.Succeeded:
                    return File(stream, "application/octet-stream");
                case Result.FileNotFound:
                    return NotFound();
                default:
                    return Problem();
            }
        }

        [HttpGet()]
        [ProducesResponseType(typeof(IEnumerable<string>), 200)]
        [ProducesResponseType(typeof(IEnumerable<string>), 206)]
        public async Task<IActionResult> List([FromQuery] int max = MaxBlobItems)
        {
            var (result, list) = await _storage.List(max);
            switch (result)
            {
                case Result.Succeeded:
                    return Ok(list);
                case Result.Truncated:
                    return StatusCode(206, list);
                default:
                    return Problem();
            }
        }

        [HttpDelete("{**path:required}")]
        [ProducesErrorResponseType(typeof(void))]
        [ProducesResponseType(404)]
        [ProducesResponseType(202)]
        public async Task<IActionResult> Delete(string path)
        {
            path = UnSwaggerPath(path);
            var result = await _storage.DeleteFile(path);
            switch (result)
            {
                case Result.Succeeded:
                    return Accepted();
                case Result.FileNotFound:
                    return NotFound();
                default:
                    return Problem();
            }
        }

        private static string UnSwaggerPath(string path) => path.Replace("%2F", "/");

    }
}
