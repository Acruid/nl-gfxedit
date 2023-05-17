using CommunityToolkit.HighPerformance;
using System.Runtime.InteropServices;
using System.Text;
using OpenTK.Mathematics;

namespace GfxEditor;

using Pointer = UInt32;

public sealed class File3di
{
    public HEADER _header;

    public List<TEXTURE> _textures;
    public List<byte[]> _bmLines;
    public List<RGBA[]> _palettes;

    public List<ModelLodHeader> _lodHeaders;
    public List<VECTOR4[]> _lodPositions;
    public List<VECTOR4[]> _lodNormals;
    public List<ModelFace[]> _lodFaces;
    public List<ModelSegmentMesh[]> _lodSubObjects;
    public List<ModelBoneAnim[]> _lodBoneAnim;
    public List<ModelMaterial[]> _lodMaterials;
    public List<COL_PLANE[]> _lodColPlanes;
    public List<COL_VOLUME[]> _lodColVolumes;

    public File3di()
    {
        _header.Signature = FileVersion.V8;
        _header.RenderType = LodRenderType_V8.generic;
        _header.Precision = 16;

        _textures = new List<TEXTURE>();
        _bmLines = new List<byte[]>();
        _palettes = new List<RGBA[]>();

        _lodHeaders = new(_header.nLODs);
        _lodPositions = new(_header.nLODs);
        _lodNormals = new(_header.nLODs);
        _lodFaces = new(_header.nLODs);
        _lodSubObjects = new(_header.nLODs);
        _lodBoneAnim = new(_header.nLODs);
        _lodMaterials = new(_header.nLODs);
        _lodColPlanes = new(_header.nLODs);
        _lodColVolumes = new(_header.nLODs);
    }

    public unsafe bool ReadFile(BinaryReader reader)
    {
        fixed (HEADER* ptr = &_header)
        {
            var span = new Span<byte>(ptr, Marshal.SizeOf<HEADER>());
            var bytes = reader.Read(span);

            if (bytes != Marshal.SizeOf<HEADER>())
                return false;
        }

        //TODO: Check signature
        if (_header.Signature != FileVersion.V8)
            return false;

        var texCount = reader.ReadInt32();
        _textures = new List<TEXTURE>(texCount);
        _bmLines = new List<byte[]>(texCount);
        _palettes = new List<RGBA[]>(texCount);

        var texturesSpan = _textures.AsCapacitySpan();

        for (var i = 0; i < texCount; i++)
        {
            reader.Read(texturesSpan.Slice(i, 1).AsBytes());

            ref var texture = ref texturesSpan[i];
            _bmLines.Add(reader.ReadBytes(texture.bmSize));

            RGBA[] palette = new RGBA[256];
            reader.Read(palette.AsSpan().AsBytes());
            _palettes.Add(palette);
        }

        _lodHeaders = new(_header.nLODs);
        _lodPositions = new(_header.nLODs);
        _lodNormals = new(_header.nLODs);
        _lodFaces = new(_header.nLODs);
        _lodSubObjects = new(_header.nLODs);
        _lodBoneAnim = new(_header.nLODs);
        _lodMaterials = new(_header.nLODs);
        _lodColPlanes = new(_header.nLODs);
        _lodColVolumes = new(_header.nLODs);

        var lodHeaderSpan = _lodHeaders.AsCapacitySpan();
        for (var i = 0; i < _header.nLODs; i++)
        {
            reader.Read(lodHeaderSpan.Slice(i, 1).AsBytes());
            ref var lodHeader = ref lodHeaderSpan[i];

            var positions = new VECTOR4[lodHeader.nPositions]; reader.Read(positions.AsSpan().AsBytes()); _lodPositions.Add(positions);
            var normals = new VECTOR4[lodHeader.nNormals]; reader.Read(normals.AsSpan().AsBytes()); _lodNormals.Add(normals);
            var faces = new ModelFace[lodHeader.nFaces]; reader.Read(faces.AsSpan().AsBytes()); _lodFaces.Add(faces);
            var segments = new ModelSegmentMesh[lodHeader.nSegments]; reader.Read(segments.AsSpan().AsBytes()); _lodSubObjects.Add(segments);
            var boneAnims = new ModelBoneAnim[lodHeader.nPartAnims]; reader.Read(boneAnims.AsSpan().AsBytes()); _lodBoneAnim.Add(boneAnims);
            var colPlanes = new COL_PLANE[lodHeader.nColPlanes]; reader.Read(colPlanes.AsSpan().AsBytes()); _lodColPlanes.Add(colPlanes);
            var colVolumes = new COL_VOLUME[lodHeader.nColVolumes]; reader.Read(colVolumes.AsSpan().AsBytes()); _lodColVolumes.Add(colVolumes);
            var materials = new ModelMaterial[lodHeader.nMaterials]; reader.Read(materials.AsSpan().AsBytes()); _lodMaterials.Add(materials);
        }

        return true;
    }

    public unsafe void WriteFile(BinaryWriter writer)
    {
        fixed (HEADER* ptr = &_header)
        {
            var span = new Span<byte>(ptr, Marshal.SizeOf<HEADER>());
            writer.Write(span);
        }

        writer.Write(_textures.Count);

        if(_textures.Count > 0)
        {
            var texturesSpan = _textures.AsSpan();
            var bmLinesSpan = _bmLines.AsSpan();
            var palettesSpan = _palettes.AsSpan();

            for (var i = 0; i < _textures.Count; i++)
            {
                writer.Write(texturesSpan.Slice(i, 1).AsBytes());
                writer.Write(bmLinesSpan[i].AsSpan().AsBytes());
                writer.Write(palettesSpan[i].AsSpan().AsBytes());
            }
        }

        if(_lodHeaders.Count > 0)
        {
            var lodHeaderSpan = _lodHeaders.AsSpan();
            var positions = _lodPositions.AsSpan();
            var normals = _lodNormals.AsSpan();
            var faces = _lodFaces.AsSpan();
            var subObjs = _lodSubObjects.AsSpan();
            var boneAnims = _lodBoneAnim.AsSpan();
            var colPlanes = _lodColPlanes.AsSpan();
            var colVolumes = _lodColVolumes.AsSpan();
            var materials = _lodMaterials.AsSpan();

            for (var i = 0; i < _lodHeaders.Count; i++)
            {
                writer.Write(lodHeaderSpan.Slice(i, 1).AsBytes());
                writer.Write(positions[i].AsSpan().AsBytes());
                writer.Write(normals[i].AsSpan().AsBytes());
                writer.Write(faces[i].AsSpan().AsBytes());
                writer.Write(subObjs[i].AsSpan().AsBytes());
                writer.Write(boneAnims[i].AsSpan().AsBytes());
                writer.Write(colPlanes[i].AsSpan().AsBytes());
                writer.Write(colVolumes[i].AsSpan().AsBytes());
                writer.Write(materials[i].AsSpan().AsBytes());
            }
        }
    }

    #region Utility
    public int FaceOffset(int lod, int n)
    {
        if (n <= 0)
            return 0;

        var Bones = _lodSubObjects[lod];
        var off = 0;
        for (var i = 0; i < n; i++)
            off += Bones[i].nFaces;

        return off;
    }

    public int VecOffset(int lod, int n)
    {
        if (n <= 0)
            return 0;

        var Bones = _lodSubObjects[lod];
        var off = 0;
        for (var i = 0; i < n; i++)
            off += Bones[i].nVerts;

        return off;
    }

    #endregion

    #region Binary Structs

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct FixedQ15_16
    {
        private const float Fractional = 1 << 16;

        private readonly int RawValue; //Q15.16

        public FixedQ15_16(int rawValue) => RawValue = rawValue;

        public static implicit operator float(FixedQ15_16 val) => val.RawValue / Fractional;
        public static explicit operator FixedQ15_16(float val) => new((int)Math.Round(val * Fractional));

        public override string ToString() => $"{(float)this:F5}";
    }

    public readonly struct FixedQ1_14
    {
        private const float Fractional = 1 << 14;

        private readonly short RawValue; //Q1.14

        public FixedQ1_14(short rawValue) => RawValue = rawValue;

        public static implicit operator float(FixedQ1_14 val) => val.RawValue / Fractional;
        public static explicit operator FixedQ1_14(float val) => new((short)Math.Round(val * Fractional));

        public override string ToString() => $"{(float)this:F5}";
    }

    public readonly struct FixedQ7_8
    {
        private const float Fractional = 1 << 8;

        private readonly short RawValue; //Q7.8

        public FixedQ7_8(short rawValue) => RawValue = rawValue;

        public static implicit operator float(FixedQ7_8 val) => val.RawValue / Fractional;
        public static explicit operator FixedQ7_8(float val) => new((short)Math.Round(val * Fractional));

        public override string ToString() => $"{(float)this:F5}";
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct HEADER
    {
        public FileVersion Signature;

        // assumed to be ASCII encoded text bytes
        private fixed byte name[0xC];

        private int PAD_0;
        public int nLODs;
        private fixed int minLodDist[4];
        private fixed uint renderType[4];
        public int Precision;
        private fixed int PAD_1[16];

        public int get_MinLodDist(int lod)
        {
            return minLodDist[lod] / 0xFF;
        }

        public bool set_MinLodDist(int lod, int dst)
        {
            if (minLodDist[Math.Min(0, lod - 1)] <= dst * 0xFF)
                return false;
            else
                minLodDist[lod] = dst * 0xFF;

            return true;
        }

        public string Name
        {
            get
            {
                fixed (byte* namePtr = name)
                {
                    ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(namePtr, 0xC);
                    span = CleanString(span);
                    return Encoding.ASCII.GetString(span);
                }
            }
            set
            {
                fixed (byte* namePtr = name)
                {
                    var span = new Span<byte>(namePtr, 0xC);
                    span.Clear();
                    int count = Math.Min(value.Length, 0xB);
                    ReadOnlySpan<char> charSpan = value.AsSpan().Slice(0, count);
                    Encoding.ASCII.GetBytes(charSpan, span);
                }
            }
        }

        public LodRenderType_V8 RenderType
        {
            get => (LodRenderType_V8) renderType[0];
            set
            {
                for(var i = 0; i < 4; i++)
                {
                    renderType[i] = (uint)value;
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct RGBA
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct TEXTURE
    {
        private fixed byte name[0x1C]; // ASCII encoded bytes
        public int bmSize;
        public short TexIndex;
        public byte Flags;
        private byte GAP0;
        public short bmWidth;
        public short bmHeight;
        private fixed byte GAP1[12];

        public string Name
        {
            get
            {
                fixed (byte* namePtr = name)
                {
                    ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(namePtr, 0x1C);
                    span = CleanString(span);
                    return Encoding.ASCII.GetString(span);
                }
            }
            set
            {
                fixed (byte* namePtr = name)
                {
                    var span = new Span<byte>(namePtr, 0x1C);
                    span.Clear();
                    int count = Math.Min(value.Length, 0x1B);
                    ReadOnlySpan<char> charSpan = value.AsSpan().Slice(0, count);
                    Encoding.ASCII.GetBytes(charSpan, span);
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct VECTOR4
    {
        public short x;
        public short y;
        public short z;
        public short w;


        public static implicit operator Vector4i(in VECTOR4 vec)
        {
            return new Vector4i(vec.x, vec.y, vec.z, vec.w);
        }

        public static implicit operator Vector4(in VECTOR4 vec)
        {
            var x = new FixedQ7_8(vec.x);
            var y = new FixedQ7_8(vec.y);
            var z = new FixedQ7_8(vec.z);
            var w = new FixedQ7_8(vec.w);

            return new Vector4(x, y, z, w);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct ModelFace
    {
        private readonly short null0;
        private readonly short SurfaceIndex;
        public fixed int texCoordU[3];
        public fixed int texCoordV[3];

        public fixed short PositonIdx[3];
        public fixed short NormalIdx[3];

        private readonly int Distance;
        private readonly int xMin;
        private readonly int xMax;
        private readonly int yMin;
        private readonly int yMax;
        private readonly int zMin;
        private readonly int zMax;

        public int MaterialIndex; //v,r index of material for faces
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct ModelMaterial
    {
        public fixed byte name[16];

        public byte BitFlags;        //v,r flags for some type of setup
        public byte Transparentflag; //v,r something with transparent texture strides

        private fixed byte pad0[2]; //v,n padding for the bitflags

        private readonly uint gap14; //u

        private fixed uint null0[7]; //

        private fixed byte texIndex[4]; // index of camo texture to use (Green, Brown, White, Alpha)
        public byte TexIndex(CamoColor camo) => texIndex[(byte) camo];
        
        private fixed uint null1[16]; //
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct ModelSegmentMesh
    {
        private int GAP_0;
        public int nVerts; //v,r number of verts in the subObject
        private Pointer _VertGroup; //v,w ptr to vert data for this subObject
        public int nFaces; //v,r number of faces in sub object
        private Pointer _FaceGroup; //v,w ptr to face data of sub object
        public int nNormals; //r,r ''
        private Pointer _NormGroup; //v,w ''
        public int nColVolumes; //v,r ''
        private Pointer _ColVolumes; //v,w ''

        // ignore this if(lodheader.Flags & 1 == false)
        public int parentBone; //v,r parent bone this is attached to

        private int _diffXoff; //v,w VecXOff - parentBone.VecXoff
        private int _diffYoff; //v,w VecYOff - parentBone.VecYoff
        private int _diffZoff; //v,w VecZOff - parentBone.VecZoff

        //v,r if(lodheader.Flags & 1)foreach vert in group, vec.x -= (VecXoff >> 8)
        public int VecXoff; //v,r
        public int VecYoff; //v,r same as above for y
        public int VecZoff; //v,r same as above for z

        private fixed int GAP_1[12];
        
        public readonly Vector3 ModelPosition =>
            new()
            {
                X = new FixedQ15_16(VecXoff),
                Y = new FixedQ15_16(VecYoff),
                Z = new FixedQ15_16(VecZoff)
            };
    }

    [Flags]
    public enum LodHeaderFlags
    { /* Misc attribute flags for a model lod. */
        OffsetArmatures = 1 /* offset verts in Bones */
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct ModelLodHeader
    {
        private fixed byte _gap0[16];
        public LodHeaderFlags Flags; //v,r if(Flags & 1) offset verts in Bones
        public int length;           //v,r length of model data after this header
        private readonly int _gap1;

        public int SphereRadius; //v,r bounding sphere around the vertex positions
        public int CircleRadius; //v,r bounding circle around the x/y vertex positions
        public int zTotal;       //v,r height of the vertex AABB, Round(zMax - zMin, 3)
        public int xMin;
        public int xMax;
        public int yMin;
        public int yMax;
        public int zMin;
        public int zMax;

        private fixed byte _gap2[02];
        public short texLoopCount;    //v,r Number of textures in the animated texture loop
        private fixed byte _gap3[22];
        public short loopInterval;    //v,r Delay between textures in animated texture loop
        private fixed byte _gap4[36];

        public int nPositions;       //v,r number of vertex positions in lod model
        private readonly int _gap5;
        public int nNormals;         //v,r number of vertex normals in lod model
        private readonly int _gap6;
        public int nFaces;           //v,r number of model faces in lod model
        private readonly int _gap7;
        public int nSegments;        //v,r number of Segments in lod model
        private readonly int _gap8;
        public int nPartAnims;       //v,r number of PartAnims in lod model
        private readonly int _gap9;
        public int nMaterials;       //v,r number of Materials in lod model
        private readonly int _gap10;
        public int nColPlanes;       //v,r number of Collision Planes in lod model
        private readonly int _gap11;
        public int nColVolumes;      //v,r number of Collision Volumes in lod model
        private readonly int _gap12;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct ModelBoneAnim
    {
        private fixed byte GAP_0[12];
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct COL_PLANE
    {
        public FixedQ1_14 x;
        public FixedQ1_14 y;
        public FixedQ1_14 z;
        public FixedQ7_8 distance;
    }

    public enum CollisionVolumeType : uint
    {
        Normal = 1,
        Climbable = 4,
        Armory = 6,
        Attachable = 7
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct COL_VOLUME
    {
        public CollisionVolumeType VolumeType;

        int gap0;

        public FixedQ15_16 XMedian;
        public FixedQ15_16 YMedian;
        public FixedQ15_16 ZMedian;

        public FixedQ15_16 BoundingSphere1;
        public FixedQ15_16 BoundingSphere2;
        public FixedQ15_16 BoundingSphere3;

        public FixedQ15_16 HalfLength;
        public FixedQ15_16 HalfWidth;
        public FixedQ15_16 HalfHeight;

        int gap1;

        public FixedQ15_16 xMin;
        public FixedQ15_16 xMax;
        public FixedQ15_16 yMin;
        public FixedQ15_16 yMax;
        public FixedQ15_16 zMin;
        public FixedQ15_16 zMax;

        public int CollisionPlaneCount;
        int gap2;
    }

    public enum FileVersion : uint
    {
        ERROR = 0,
        V8 = 0x08494433 //{ '3', 'D', 'I', 0x8 }
    }

    public enum LodRenderType_V8 : uint
    {
        generic = 0x676E7263,
        organic = 0x6F726730,
        tree = 0x65726574,
        tire = 0x65726974,
        ef3d = 0x65663364,
        effect = 0x65666374,
        palm = 0x6C61706D,
        building = 0x67646C62,
        building2 = 0x646C6232,
        ka29 = 0x616B3239,
        hind = 0x646E6968,
        bird = 0x64726962,
        penguin = 0x676E6570,
        fish = 0x68736966,
        tank = 0x6B6E6174,
        brdm = 0x6D647262,
        pungi_stick = 0x676E7570,
        weapon = 0x6E707765,
        helo = 0x6F6C6568
    }

    public enum CamoColor : byte
    {
        Green = 0,
        Brown = 1,
        White = 2,
        Alpha = 3
    }

    private static ReadOnlySpan<byte> CleanString(ReadOnlySpan<byte> span)
    {
        span = span.TrimStart((byte)'\0');
        var index = span.IndexOf((byte)'\0');
        if (index >= 0)
            span = span.Slice(0, index);
        return span;
    }

    #endregion Binary Structs
}