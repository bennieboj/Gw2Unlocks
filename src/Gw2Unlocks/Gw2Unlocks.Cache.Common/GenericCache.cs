using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Gw2Unlocks.Cache.Common
{
    public class GenericCache
    {
        protected string CacheFolder { get; }
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Converters = { new JsonStringEnumConverter() },
            PropertyNameCaseInsensitive = true
        };

        public GenericCache(CachePaths cachepaths, string cacheFolder)
        {
            ArgumentNullException.ThrowIfNull(cachepaths);
            this.CacheFolder = Path.Combine(cachepaths.Root, cacheFolder);
            Directory.CreateDirectory(this.CacheFolder);
        }

        // --- Generic helper to read JSON ---
        protected async Task<T> LoadFromFileAsync<T>(string fileName, CancellationToken cancellationToken) where T : new()
        {
            var path = Path.Combine(CacheFolder, fileName);
            if (File.Exists(path))
            {
                var json = await File.ReadAllTextAsync(path, cancellationToken);
                return JsonSerializer.Deserialize<T>(json, JsonOptions)!;
            }
            return new T();
        }

        protected async IAsyncEnumerable<T> StreamFromFileAsyncEnumerable<T>(
            string fileName,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var path = Path.Combine(CacheFolder, fileName);

            if (!File.Exists(path))
                yield break;

            await using var stream = File.OpenRead(path);

            // Stream JSON array
            await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<T>(
                stream,
                JsonOptions,
                cancellationToken))
            {
                if (item is not null)
                    yield return item;
            }
        }

        protected static async IAsyncEnumerable<string> ReadXmlPages(
            Stream stream,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var settings = new XmlReaderSettings { Async = true };
            using var reader = XmlReader.Create(stream, settings);

            while (await reader.ReadAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (reader.NodeType == XmlNodeType.Element && reader.Name == "page")
                {
                    yield return await reader.ReadOuterXmlAsync();
                }
            }
        }

        protected async IAsyncEnumerable<T> StreamFromFileAsyncEnumerable<T>(
            string fileName,
            Func<Stream, CancellationToken, IAsyncEnumerable<T>> reader,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(reader);
            var path = Path.Combine(CacheFolder, fileName);

            if (!File.Exists(path))
                yield break;

            await using var stream = File.OpenRead(path);

            await foreach (var item in reader(stream, cancellationToken)
                .WithCancellation(cancellationToken))
            {
                yield return item;
            }
        }


        protected static async Task WritePagesAsync(
            IAsyncEnumerable<string> pages,
            StreamWriter writer,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(writer);
            await foreach (var page in pages.WithCancellation(cancellationToken))
            {
                await writer.WriteLineAsync(page);
            }
        }

        // --- Generic helper to save JSON from caller-provided data ---
        public async Task SaveToCacheAsync<T>(string fileName, T data, CancellationToken cancellationToken)
        {
            var path = Path.Combine(CacheFolder, fileName);
            var json = JsonSerializer.Serialize(data, JsonOptions);
            await File.WriteAllTextAsync(path, json, cancellationToken);
        }
    }
}
