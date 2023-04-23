using CommunityToolkit.HighPerformance;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;

namespace GfxEditor;

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
    public List<ModelSubObject[]> _lodSubObjects;
    public List<ModelBoneAnim[]> _lodBoneAnim;
    public List<ModelMaterial[]> _lodMaterials;
    public List<COL_PLANE[]> _lodColPlanes;
    public List<COL_VOLUME[]> _lodColVolumes;

    public File3di()
    {
        _header.Signature = (uint) FileVersion.V8;
        _header.RenderType = LodRenderType_V8.GENERIC;
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

    public unsafe void ReadFile(BinaryReader reader)
    {
        fixed (HEADER* ptr = &_header)
        {
            var span = new Span<byte>(ptr, Marshal.SizeOf<HEADER>());
            reader.Read(span);
        }

        //TODO: Check signature

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

            var positions = new VECTOR4[lodHeader.nVertices]; reader.Read(positions.AsSpan().AsBytes()); _lodPositions.Add(positions);
            var normals = new VECTOR4[lodHeader.nNormals]; reader.Read(normals.AsSpan().AsBytes()); _lodNormals.Add(normals);
            var faces = new ModelFace[lodHeader.nFaces]; reader.Read(faces.AsSpan().AsBytes()); _lodFaces.Add(faces);
            var subObjs = new ModelSubObject[lodHeader.nSubObjects]; reader.Read(subObjs.AsSpan().AsBytes()); _lodSubObjects.Add(subObjs);
            var boneAnims = new ModelBoneAnim[lodHeader.nPartAnims]; reader.Read(boneAnims.AsSpan().AsBytes()); _lodBoneAnim.Add(boneAnims);
            var colPlanes = new COL_PLANE[lodHeader.nColPlanes]; reader.Read(colPlanes.AsSpan().AsBytes()); _lodColPlanes.Add(colPlanes);
            var colVolumes = new COL_VOLUME[lodHeader.nColVolumes]; reader.Read(colVolumes.AsSpan().AsBytes()); _lodColVolumes.Add(colVolumes);
            var materials = new ModelMaterial[lodHeader.nMaterials]; reader.Read(materials.AsSpan().AsBytes()); _lodMaterials.Add(materials);
        }
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
    public unsafe struct HEADER
    {
        public uint Signature;

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
        private byte R;
        private byte G;
        private byte B;
        private byte A;
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
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct ModelFace
    {
        private readonly short null0;
        private readonly short SurfaceIndex;
        public int tu1;
        public int tu2;
        public int tu3;
        public int tv1;
        public int tv2;
        public int tv3;

        public short Vertex1;
        public short Vertex2;
        public short Vertex3;
        public short Normal1;
        public short Normal2;
        public short Normal3;

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

        private readonly byte BitFlags; //v,r flags for some type of setup

        private fixed byte pad0[3]; //v,n padding for the bitflags

        private readonly uint gap14; //u

        private fixed uint null0[7]; //

        private fixed byte texIndex[4]; // index of texture to use
        public byte TexIndex(CamoColor camo) => texIndex[(byte) camo];

        //public byte IndexG; // index of texture to use
        //private readonly byte IndexB; // index of texture to use
        //private readonly byte IndexW; // index of texture to use
        //private readonly byte IndexA; //

        private fixed uint null1[16]; //
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct ModelSubObject
    {
        private int GAP_0;
        public int nVerts; //v,r number of verts in the subObject
        private int PTR_VertGroup; //v,w ptr to vert data for this subObject
        public int nFaces; //v,r number of faces in sub object
        private int PTR_FaceGroup; //v,w ptr to face data of sub object
        private int nNormals; //w,r ''
        private int PTR_NormGroup; //v,w ''
        private int nColVolumes; //v,r ''
        private int PTR_ColVolumes; //v,w ''

        // ignore this if(lodheader.Flags & 1 == false)
        public int parentBone; //v,r parent bone this is attached to

        private int diffXoff; //v,w VecXOff - parentBone.VecXoff
        private int diffYoff; //v,w VecYOff - parentBone.VecYoff
        private int diffZoff; //v,w VecZOff - parentBone.VecZoff

        //v,r if(lodheader.Flags & 1)foreach vert in group, vec.x -= (VecXoff >> 8)
        public int VecXoff; //v,r
        public int VecYoff; //v,r same as above for y
        public int VecZoff; //v,r same as above for z

        private fixed int GAP_1[12];
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct ModelLodHeader
    {
        private fixed int null0[4];
        private readonly int Flags; //v,r if(Flags & 1) offset verts in Bones
        private readonly int length;
        private readonly int PTR_ModelInfo; //v,w ptr to all of the model info after header

        private readonly int SphereRadius;
        private readonly int CircleRadius;
        private readonly int zTotal;
        public readonly int xMin;
        public readonly int xMax;
        public readonly int yMin;
        public readonly int yMax;
        public readonly int zMin;
        public readonly int zMax;

        private fixed int null2[16];

        public readonly int nVertices; //v,r number of verts in lod mesh
        private readonly int null3; //v,w number of verts in lod mesh
        public readonly int nNormals; //v,r number of normals in lod mesh
        private readonly int null4; //v,w ptr to start of normals in-mem
        public readonly int nFaces; //v,r number of faces of mesh
        private readonly int null5; //v,w ptr to start of faces in-mem
        public readonly int nSubObjects; //v,r number of sub objects
        private readonly int null6; //v,w ptr to start of sub objects
        public readonly int nPartAnims; //v,r
        private readonly int null7; //v,w
        public readonly int nMaterials; //v,r
        private readonly int null8; //v,w
        public readonly int nColPlanes; //v,r
        private readonly int null9; //v,w
        public readonly int nColVolumes; //v,r
        private readonly int null10; //v,w
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct ModelBoneAnim
    {
        private fixed byte GAP_0[12];
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct COL_PLANE
    {
        private short x;
        private short y;
        private short z;
        private short distance;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct COL_VOLUME
    {
        private fixed byte GAP0[72];
        private int nColPlanes;        //v,r number of colPlanes that belong to this
        private int PTR_ColPlaneGroup; //v,w group of colPlanes that belog to this
    }

    public enum FileVersion : uint
    {
        ERROR = 0,
        V8 = 0x08494433 //{ '3', 'D', 'I', 0x8 }
    }

    public enum LodRenderType_V8 : uint
    {
        NONE = 0x0,
        GENERIC = 0x676E7263 //"crng"
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