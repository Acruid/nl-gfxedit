using System.Runtime.InteropServices;
using TiffLibrary;

namespace GfxEditor;

internal class GfxEdit
{
    public event EventHandler? FileUpdated;

    public FileInfo? OpenFileInfo { get; private set; }
    public File3di OpenedFile { get; private set; } = new();

    public int ActiveLod { get; set; }

    public void NewFile()
    {
        OpenFileInfo = null;
        OpenedFile = new File3di();
        FileDirty();
    }

    public void LoadFile(FileInfo fileInfo)
    {
        if (!fileInfo.Exists)
            return;

        OpenedFile = new File3di();

        OpenFileInfo = fileInfo;
        using var stream = fileInfo.OpenRead();
        using var reader = new BinaryReader(stream);
        OpenedFile.ReadFile(reader);
        FileDirty();
    }

    public void SaveFile(FileInfo fileInfo)
    {
        //TODO: Save file
    }

    public void FileDirty()
    {
        FileUpdated?.Invoke(this, EventArgs.Empty);
    }

    struct RGB
    {
        public byte R;
        public byte G;
        public byte B;
    }

    /// <summary>
    /// Replaces the texture data for the given texture index in the 3DI.
    /// This will update the header with relevant name transparent and size data.
    /// </summary>
    /// <param name="textureIdx">Index of an existing texture in the 3DI.</param>
    /// <param name="tifFile">Optionally transparent TIFF file containing the texture data.</param>
    public bool ReplaceTexture(int textureIdx, FileInfo tifFile)
    {
        if (!tifFile.Exists)
            return false;

        // https://github.com/yigolden/TiffLibrary

        //Memory<byte> colorPlane; // 8 bpp Alpha Value (0-255 levels)
        //Memory<byte> alphaPlane; // 8 bpp Index into palette
        byte[] pixelData;
        File3di.RGBA[] palette = new File3di.RGBA[256]; // 8 bpp RGBA

        var fileBytes = File.ReadAllBytes(tifFile.FullName);

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

        // read palette
        {
            var colorMap = tagReader.ReadColorMap();
            var pal = palette.AsSpan();

            // read palette
            // https://www.awaresystems.be/imaging/tiff/tifftags/colormap.html
            for (int i = 0; i < 256; i++)
            {
                ref var color = ref pal[i];

                // TIFF: [RGB] 3DI: [BGRA]
                color.B = (byte)(colorMap[i + 000] >> 8);
                color.G = (byte)(colorMap[i + 256] >> 8);
                color.R = (byte)(colorMap[i + 512] >> 8);
                // A reserved
            }
        }

        // Read image data
        {
            var stripByteCounts = tagReader.ReadStripByteCounts();
            var stripOffsets = tagReader.ReadStripOffsets();

            var stripBytes = fileBytes.AsSpan().Slice((int)stripOffsets[0], (int)stripByteCounts[0]);
            pixelData = new byte[stripBytes.Length];
            stripBytes.CopyTo(pixelData.AsSpan());
        }

        // update texture with 3 fields
        {
            ref var textureHdr = ref CollectionsMarshal.AsSpan(OpenedFile._textures)[textureIdx];
            OpenedFile._bmLines[textureIdx] = pixelData;
            OpenedFile._palettes[textureIdx] = palette;

            textureHdr.bmHeight = (short)height;
            textureHdr.bmWidth = (short)width;
            textureHdr.bmSize = pixelData.Length;
        }

        return true;
    }

    static bool IsPowerOfTwo(int n)
    {
        return (n > 0) && ((n & (n - 1)) == 0);
    }
}