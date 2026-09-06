using Microsoft.AspNetCore.Http;
using NekretnineApp.Services;

namespace NekretnineApp.Tests;

[TestClass]
public class ImageFileValidatorTests
{
    [TestMethod]
    public async Task IsValidAsync_AcceptsJpegWithMatchingSignatureAndContentType()
    {
        var file = CreateFile(
            "property.jpg",
            "image/jpeg",
            new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 });

        Assert.IsTrue(await ImageFileValidator.IsValidAsync(file));
    }

    [TestMethod]
    public async Task IsValidAsync_RejectsContentThatDoesNotMatchExtension()
    {
        var file = CreateFile(
            "property.png",
            "image/png",
            new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

        Assert.IsFalse(await ImageFileValidator.IsValidAsync(file));
    }

    [TestMethod]
    public async Task IsValidAsync_RejectsExecutableExtension()
    {
        var file = CreateFile("property.exe", "application/octet-stream", new byte[] { 0x4D, 0x5A });

        Assert.IsFalse(await ImageFileValidator.IsValidAsync(file));
    }

    private static FormFile CreateFile(string fileName, string contentType, byte[] content)
    {
        return new FormFile(new MemoryStream(content), 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}
