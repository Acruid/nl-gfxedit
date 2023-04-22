using GfxEditor;
using NUnit.Framework;
using System.IO;

namespace GfxEditor.Tests;

[TestFixture]
public class File3diTests
{
    [Test]
    public void TestRoundTrip()
    {
        // Read the binary file into an array
        byte[] originalFile = File.ReadAllBytes("BSFLGBLU.3DI");

        // Create a MemoryStream from the original file and pass it to an instance of File3di
        using (MemoryStream originalStream = new MemoryStream(originalFile))
        using (BinaryReader reader = new BinaryReader(originalStream))
        {
            File3di file3d = new File3di();
            file3d.ReadFile(reader);

            // Create a new empty MemoryStream and write the contents of the File3di instance to it
            using (MemoryStream newStream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(newStream))
            {
                file3d.WriteFile(writer);

                // Compare the two streams for byte equality
                byte[] originalStreamBytes = originalFile;
                byte[] newStreamBytes = newStream.ToArray();

                // Find the location of the first difference in the streams if they are not equal
                for (int i = 0; i < originalStreamBytes.Length; i++)
                {
                    if (originalStreamBytes[i] != newStreamBytes[i])
                    {
                        var firstDifferenceIndex = i;
                        Assert.Fail($"The two streams are not equal. The first difference is at index {firstDifferenceIndex}.");
                    }
                }
            }
        }
    }
}