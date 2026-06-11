using Microsoft.Extensions.Logging;
using SkiaSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.IconSpriteSheet.Implementation;

internal sealed class IconSpriteSheetGenerator : IIconSpriteSheetGenerator
{
    private readonly HttpClient httpClient;
    private readonly ILogger<IconSpriteSheetGenerator> logger;
    private const int IconSize = 64;
    private const int IconsPerSheet = 500;
    private const int Columns = 25;

    public IconSpriteSheetGenerator(HttpClient httpClient, ILogger<IconSpriteSheetGenerator> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        this.httpClient = httpClient;
        this.logger = logger;
    }
    public async Task<IconSpriteSheetResult> Generate(IEnumerable<IconSpriteSheetInput> inputs, CancellationToken cancellationToken)
    {
        var sheets = new Dictionary<string, byte[]>();
        var inventoryData = new IconSpriteSheetInventoryData();

        inputs = [.. inputs];
        Stopwatch sw = Stopwatch.StartNew();
        for (int sheet = 0; sheet * IconsPerSheet < inputs.Count(); sheet++)
        {
            var inputBatch = inputs
                .Skip(sheet * IconsPerSheet)
                .Take(IconsPerSheet)
                .ToList();

            int rows = (int)Math.Ceiling(inputBatch.Count / (double)Columns);

            int width = Columns * IconSize;
            int height = rows * IconSize;

            var downloaded = new ConcurrentDictionary<int, byte[]>();

            await Parallel.ForEachAsync(
                Enumerable.Range(0, inputBatch.Count),
                cancellationToken,
                async (i, ct) =>
                {
                    var input = inputBatch[i];

                    using var response = await httpClient.GetAsync(input.IconUrl, ct);

                    if (!response.IsSuccessStatusCode)
                        return;

                    downloaded[i] = await response.Content.ReadAsByteArrayAsync(ct);
                });
            logger.LogInformation("step 1: {durationseconds}", sw.Elapsed.TotalSeconds);

            sw = Stopwatch.StartNew();
            var resized = new ConcurrentDictionary<int, SKBitmap>();
            Parallel.ForEach(
                downloaded,
                new ParallelOptions
                {
                    CancellationToken = cancellationToken
                },
                item =>
                {
                    using var original = SKBitmap.Decode(item.Value);

                    if (original == null)
                        return;

                    var bitmap = original.Resize(
                        new SKImageInfo(IconSize, IconSize),
                        SKSamplingOptions.Default);

                    if (bitmap != null)
                    {
                        resized[item.Key] = bitmap;
                    }
                });
            logger.LogInformation("step 2: {durationseconds}", sw.Elapsed.TotalSeconds);

            sw = Stopwatch.StartNew();
            using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.Transparent);
            for (int i = 0; i < inputBatch.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!resized.TryGetValue(i, out var resizedBitmap))
                    continue;

                var input = inputBatch[i];

                int col = i % Columns;
                int row = i / Columns;

                int x = col * IconSize;
                int y = row * IconSize;

                canvas.DrawBitmap(resizedBitmap, x, y);

                inventoryData.Inventory.Add($"{input.Type}/{input.Id}", new IconSpriteSheetInventoryItem(sheet, x, y));

                resizedBitmap.Dispose();
            }
            logger.LogInformation("step 3: {durationseconds}", sw.Elapsed.TotalSeconds);

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Webp, 90);
            sheets.Add($"icons_{sheet}.webp", data.ToArray());
        }
        return new IconSpriteSheetResult(sheets, inventoryData);
    }
}