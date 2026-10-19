using HomestayBooking.Web.Data;

namespace HomestayBooking.Tests.Data;

public sealed class SeedAssetPackagingTests
{
    [Theory]
    [InlineData(SeedData.SampleDocumentFile)]
    [InlineData(SeedData.SampleReceiptFile)]
    public async Task DeploymentOutputContainsReadableSampleImage(string fileName)
    {
        // Seed resolves these files relative to the deployed content root, not the source checkout.
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "SampleFiles", fileName);
        Assert.True(File.Exists(path), $"Missing deployment asset: Data/SampleFiles/{fileName}");
        await using var file = File.OpenRead(path);
        var signature = new byte[8];
        Assert.Equal(signature.Length, await file.ReadAsync(signature));
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, signature);
    }
}
