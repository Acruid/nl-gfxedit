using System.Runtime.InteropServices;

namespace GfxEditor;

public class FileKsa
{
    private Memory<byte> _data;

    public FileKsa(Memory<byte> data)
    {
        _data = data;
    }

    public ref HEADER GetHeader()
    {
        return ref MemoryMarshal.AsRef<HEADER>(_data.Span);
    }

    public Span<ANIMATION> GetAnimations()
    {
        ref HEADER header = ref GetHeader();
        int headerSize = Marshal.SizeOf<HEADER>();
        int animationSize = Marshal.SizeOf<ANIMATION>();
        return MemoryMarshal.Cast<byte, ANIMATION>(_data.Slice(headerSize, header.nAnims * animationSize).Span);
    }

    public Span<KEYFRAME> GetKeyframes()
    {
        Span<ANIMATION> animations = GetAnimations();

        // TODO: const time with header.Length/anm count math
        int numKeyframes = 0;
        for (int i = 0; i < animations.Length; i++)
        {
            numKeyframes += animations[i].numKeyframes;
        }

        int headerSize = Marshal.SizeOf<HEADER>();
        int animationSize = Marshal.SizeOf<ANIMATION>();
        int keyframeSize = Marshal.SizeOf<KEYFRAME>();
        int offset = headerSize + animations.Length * animationSize;
        return MemoryMarshal.Cast<byte, KEYFRAME>(_data.Slice(offset, numKeyframes * keyframeSize).Span);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct HEADER
    {
        // Assumed to be an ASCII encoded text byte string
        public fixed byte Signature[4];  // Signature of file ("KSA\0")
        public int const1;               // DF2:assert(val==1), version?
                                         // Assumed to be an ASCII encoded text byte string
        public fixed byte Filename[32];  // writes the filename to here, if len > 31 this will overflow :S
        public int PTR_Contents;         // pointer to byte[Length] of file contents in mem
        public int Length;               // length file contents (filesize - header)
        public int PTR_Contents1;        // duplicate of PTR_File
        public int nAnims;               // number of animations
        public int PTR_StartData;        // file:val==PTR_File, mem:PTR_Contents+28*nRecords
        public fixed byte unk5[40];      // u
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct ANIMATION
    {
        public int numKeyframes; // number of keyframes this animation points to
        private int PTR_Data;     // ptr to data in mem (sum of all data before it, after records)
        public int nextAnmIndex; // the next ANIMATION index to play after this one is finished (same number = loops)
        public int nextAnmFrame; // the KEYFRAME index to start at when playing the next animation (1 = 2nd keyframe of ANIMATION)
                                 // the bounds are not checked, you can offset into ANY other keyframe :D
        public fixed byte unk0[12]; // u
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VEC4
    {
        public byte X;
        public byte Y;
        public byte Z;
        public byte W;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct KEYFRAME
    {
        public VEC4 hips;
        public VEC4 torso;
        public VEC4 head;
        public VEC4 upArmRight;
        public VEC4 upArmLeft;
        public VEC4 lowArmRight;
        public VEC4 lowArmLeft;
        public VEC4 handRight;
        public VEC4 handLeft;
        public VEC4 thighRight;
        public VEC4 thighLeft;
        public VEC4 legRight;
        public VEC4 legLeft;
        public VEC4 footRight;
        public VEC4 footLeft;
        private VEC4 unk0;
        private VEC4 unk1;
        public short height; // height offset from ground, 0=hips,-270=feet
        private short unk2;
        private fixed int unk3[4]; // u
    }
}
