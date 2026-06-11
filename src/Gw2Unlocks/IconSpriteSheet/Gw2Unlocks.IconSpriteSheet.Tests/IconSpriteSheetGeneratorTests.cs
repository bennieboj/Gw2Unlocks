using global::Gw2Unlocks.Testing.Common;
using Gw2Unlocks.IconSpriteSheet.Implementation;
using Gw2Unlocks.IconSpriteSheet.Testing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Gw2Unlocks.IconSpriteSheet.Tests;

public class IconSpriteSheetGeneratorTests(ITestOutputHelper output) : ServiceProviderBasedTest<IIconSpriteSheetGenerator>(output)
{
    protected override void Configure(IServiceCollection services)
    {
        services.AddIconSpriteSheetGenerator()
                .AddFakeIconSpriteSheetHttpMessagehandler()
                .AddIconSpriteSheetGeneratorHttpClient()
                .AddFakeHttpMessageHandler();
    }

    [Fact]
    public async Task GenerateSingleInputSucceeds()
    {
        var sut = GetSut();

        var input = new List<IconSpriteSheetInput>
        {
            new("Skin", 1, new System.Uri("http://dummy.com"))
        };

        var output = await sut.Generate(input, TestContext.Current.CancellationToken);

        Assert.Single(output.Files);
        Assert.True(output.InventoryData.Inventory.TryGetValue("Skin/1", out var inventoryItem));
        Assert.Equal(0, inventoryItem.Sheet);
        Assert.Equal(0, inventoryItem.X);
        Assert.Equal(0, inventoryItem.Y);
    }

    [Fact]
    public async Task GenerateSingleInputbutCallFailsSucceeds()
    {
        var sut = GetSut();

        var input = new List<IconSpriteSheetInput>
        {
            new("Skin", 1, new System.Uri("http://fail.com"))
        };

        var output = await sut.Generate(input, TestContext.Current.CancellationToken);

        Assert.Single(output.Files);
        Assert.False(output.InventoryData.Inventory.ContainsKey("Skin/1"));
    }

    [Fact]
    public async Task GenerateMultipleInputsShouldGiveCorrectSheetsRowsAndColsLogic()
    {
        var sut = GetSut();

        // Enough to span:
        // - multiple rows (25 columns per row)
        // - multiple sheets (500 per sheet)
        var inputs = new List<IconSpriteSheetInput>();

        for (int i = 0; i < 520; i++)
        {
            inputs.Add(new IconSpriteSheetInput(
                "Skin",
                i,
                new Uri($"http://dummy.com/{i}")));
        }

        var result = await sut.Generate(inputs, TestContext.Current.CancellationToken);

        // ---- SHEET ASSERTION ----
        // 520 items, 500 per sheet => 2 sheets expected
        Assert.Equal(2, result.Files.Count);

        foreach (var input in inputs)
        {
            Assert.True(result.InventoryData.Inventory.TryGetValue($"{input.Type}/{input.Id}", out var item));

            int index = input.Id;

            // ---- EXPECTED SHEET ----
            int expectedSheet = index / 500;

            // index within sheet
            int localIndex = index % 500;

            // ---- EXPECTED GRID POSITION ----
            int expectedCol = localIndex % 25;
            int expectedRow = localIndex / 25;

            int expectedX = expectedCol * 64;
            int expectedY = expectedRow * 64;

            Assert.Equal(expectedSheet, item.Sheet);
            Assert.Equal(expectedX, item.X);
            Assert.Equal(expectedY, item.Y);
        }
    }
}

internal sealed class FakeHttpMessageHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        if (request.RequestUri != null && request.RequestUri.ToString().Contains("fail", System.StringComparison.OrdinalIgnoreCase))
        {
            response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }
        byte[] pngBytes =
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
            0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41,
            0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
            0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00,
            0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44,
            0xAE, 0x42, 0x60, 0x82
        ];
        response.Content = new ByteArrayContent(pngBytes);

        return Task.FromResult(response);
    }
}