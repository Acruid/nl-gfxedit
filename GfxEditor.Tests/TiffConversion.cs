using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiffLibrary;

namespace GfxEditor.Tests
{
    [TestFixture]
    internal class TiffConversion
    {
        struct RGB
        {
            public byte R;
            public byte G;
            public byte B;
        }

        [Test]
        public void ReadTiffTextureTest()
        {
            // https://github.com/yigolden/TiffLibrary

            Memory<byte> colorPlane; // 8 bpp Alpha Value (0-255 levels)
            Memory<byte> alphaPlane; // 8 bpp Index into palette
            Memory<RGB> palette = new RGB[256]; // 8 bpp RGB

            var fileBytes = File.ReadAllBytes(@"sample_tex2.tif");

            // Open a TIFF file using its file name
            using TiffFileReader tiff = TiffFileReader.Open(fileBytes);

            using TiffFieldReader fieldReader = tiff.CreateFieldReader();
            /*
             * An image file directory (IFD) is a structure in TIFF file. It contains information about the
             * image, as well as pointers to the actual image data. A single TIFF file can contains multiple
             * IFDs (multiple images). They are stored in the TIFF file like a linked-list. Each IFD contains
             * an pointer to the next, while the TIFF file header contains an pointer to the first IFD.
             */

            // ReadImageFileDirectory method with no parameter will read the list of all the entries in the first IFD.
            TiffImageFileDirectory ifd = tiff.ReadImageFileDirectory();

            // Alternatively, you can use the TiffTagReader helper if you are trying to read well-known tags.
            TiffTagReader tagReader = new TiffTagReader(fieldReader, ifd);

            var width = tagReader.ReadImageWidth();
            var height = tagReader.ReadImageLength();
            var bitsPerSample = tagReader.ReadBitsPerSample();
            var samplesPerPixel = tagReader.ReadSamplesPerPixel();
            var compression = tagReader.ReadCompression();
            var photoInterp = tagReader.ReadPhotometricInterpretation();

            var colorMap = tagReader.ReadColorMap();
            var pal = palette.Span;

            // read palette
            // https://www.awaresystems.be/imaging/tiff/tifftags/colormap.html
            for (int i = 0; i < 256; i++)
            {
                ref var color = ref pal[i];

                color.R = (byte)(colorMap[i + 000] >> 8);
                color.G = (byte)(colorMap[i + 256] >> 8);
                color.B = (byte)(colorMap[i + 512] >> 8);
            }

            {
                var stripByteCounts = tagReader.ReadStripByteCounts();
                var stripOffsets = tagReader.ReadStripOffsets();

                var stripBytes = fileBytes.AsSpan().Slice((int)stripOffsets[0] - samplesPerPixel, (int)stripByteCounts[0]);
                var stride = samplesPerPixel;
                var planeLen = stripBytes.Length / 2; // == samplesPerPixel

                colorPlane = new byte[planeLen];
                var colorSpan = colorPlane.Span;

                alphaPlane = new byte[planeLen];
                var alphaSpan = alphaPlane.Span;

                for (var i = 0; i < planeLen; i++)
                {
                    colorSpan[i] = stripBytes[i * stride + 0];
                    alphaSpan[i] = stripBytes[i * stride + 1];
                }
            }
        }

        
    }
}
