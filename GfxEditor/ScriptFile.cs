﻿namespace GfxEditor;

public static class ScriptFile
{
    private const uint ScrHeader = 0x01524353;
    private const int ScrHeaderSize = 0x04;

    public static byte[] Encrypt(byte[] bytes, uint key)
    {
        var newBlock = new byte[bytes.Length];
        Array.Copy(bytes, newBlock, bytes.Length);

        XorBlock(newBlock, key, newBlock.Length);
        SwapByteOrder(newBlock);

        var body = new byte[bytes.Length + ScrHeaderSize];
        Array.Copy(newBlock, 0, body, 4, newBlock.Length);

        var headerBytes = BitConverter.GetBytes(ScrHeader);
        Array.Copy(headerBytes, 0, body, 0, headerBytes.Length);

        return body;
    }

    public static bool HasScrHeader(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < ScrHeaderSize)
            return false;

        var header = BitConverter.ToUInt32(bytes[..4]);

        return header == ScrHeader;
    }

    public static byte[] Decrypt(ReadOnlySpan<byte> bytes, uint key)
    {
        if (bytes.Length < ScrHeaderSize)
            throw new IOException("Set to decode, but input header is corrupt.");

        var header = BitConverter.ToUInt32(bytes[..4]);

        if (header != ScrHeader)
            throw new Exception("Set to decode, input header is wrong.");

        var body = new byte[bytes.Length - ScrHeaderSize];
        bytes[4..].CopyTo(body);

        SwapByteOrder(body);
        XorBlock(body, key, body.Length);
        return body;
    }

    public static bool TryGetKey(string name, out uint key)
    {
        switch (name)
        {
            case "DF1":
                key = 0x04960552;
                break;
            case "DF2":
                key = 0x01234567;
                break;
            case "DF3":
                key = 0x01234567;
                break;
            case "TFD":
                key = 0x01234567;
                break;
            case "C4":
                key = 0x01234567;
                break;
            case "BHD":
                key = 0x2A5A8EAD;
                break;
            case "TS":
                key = 0x2A5A8EAD;
                break;
            case "JO":
                key = 0x2A5A8EAD;
                break;
            case "JO:B":
                key = 0xABEEFACE;
                break;
            default:
                key = 0;
                return false;
        }

        return true;
    }

    private static void XorBlock(byte[] input, uint key, int length)
    {
        var i = 0;

        if (length <= 0)
            return;

        do
        {
            var temp1 = RotateLeft(key, 11);
            var temp2 = RotateLeft(key + temp1, 4);

            key = temp2 ^ 1;

            input[i] ^= (byte) (temp2 ^ 1);

            i++;
        } while (i < length);
    }

    private static void SwapByteOrder(byte[] input)
    {
        var len = input.Length;
        var half = len/2;

        if (half <= 0)
            return;

        var front = 0;
        var back = len - 1;
        var i = half;
        do
        {
            var temp = input[front];
            input[front] = input[back];
            input[back] = temp;

            front++;
            back--;
            i--;
        } while (i > 0);
    }

    private static uint RotateLeft(uint value, int count)
    {
        return (value << count) | (value >> (32 - count));
    }
}