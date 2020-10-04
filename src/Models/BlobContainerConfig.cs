using System;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;

namespace FileManager.Models
{
    public class BlobContainerConfig
    {
        public string? Account { get; set; }
        public string? Container { get; set; }

        public static BlobContainerClient CreateClient(IConfiguration config, string section)
        {
            var options = new BlobContainerConfig();
            config.GetSection(section).Bind(options);
            var credentials = new ChainedTokenCredential(
                new ManagedIdentityCredential(),
                new AzureCliCredential()
            );
            return new BlobContainerClient(
		        options.GetContainerUrl(), credentials);
        }

        private BlobContainerConfig() { }

        public Uri GetContainerUrl()
        {
            if (Account == null)
            {
                throw new ArgumentNullException(nameof(Account));
            }
            if (Container == null)
            {
                throw new ArgumentNullException(nameof(Container));
            }
            return new Uri($"https://{Account}.blob.core.windows.net/{Container}");
        }
    }
}
