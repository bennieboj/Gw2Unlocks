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

        var inputList = inputs.ToList();

        // Dedup by icon URL so identical icons occupy a single sheet slot.
        var uniqueInputs = inputList
            .GroupBy(i => i.IconUrl)
            .Select(g => g.First())
            .ToList();

        var urlToUniqueIndex = uniqueInputs
            .Select((u, idx) => (u.IconUrl, idx))
            .ToDictionary(x => x.IconUrl, x => x.idx);

        // Positions for each unique input (indexed by position in uniqueInputs).
        var uniquePositions = new Dictionary<int, (int Sheet, int X, int Y)>();

        Stopwatch sw = Stopwatch.StartNew();
        for (int sheet = 0; sheet * IconsPerSheet < uniqueInputs.Count; sheet++)
        {
            var inputBatch = uniqueInputs
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
            logger.LogDebug("step 1: {durationseconds}", sw.Elapsed.TotalSeconds);

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
            logger.LogDebug("step 2: {durationseconds}", sw.Elapsed.TotalSeconds);

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

                canvas.DrawBitmap(resizedBitmap, x, y, SKSamplingOptions.Default);

                var globalUniqueIndex = sheet * IconsPerSheet + i;
                uniquePositions[globalUniqueIndex] = (sheet, x, y);

                resizedBitmap.Dispose();
            }
            logger.LogDebug("step 3: {durationseconds}", sw.Elapsed.TotalSeconds);

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Webp, 90);
            sheets.Add($"icons_{sheet}.webp", data.ToArray());
        }

        // Build inventory entries for ALL original inputs (including duplicates).
        // Duplicate icon URLs share the same sheet coordinates; a repeated (Type, Id) key
        // (e.g. two recipe sheets unlocking the same recipe) keeps the last entry.
        foreach (var input in inputList)
        {
            var uniqueIndex = urlToUniqueIndex[input.IconUrl];
            if (uniquePositions.TryGetValue(uniqueIndex, out var pos))
            {
                inventoryData.Inventory[$"{input.Type}/{input.Id}"] = new IconSpriteSheetInventoryItem(pos.Sheet, pos.X, pos.Y);
            }
        }

        return new IconSpriteSheetResult(sheets, inventoryData);
    }
}