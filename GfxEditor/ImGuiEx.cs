using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using OpenTK.Mathematics;
using Quaternion_Num = System.Numerics.Quaternion;
using Vector3 = OpenTK.Mathematics.Vector3;
using Vector3_Num = System.Numerics.Vector3;
using Color = System.Drawing.Color;

namespace Engine.ImGuiBindings
{
    public static class ImGuiEx
    {
        public static void SliderColor3(string label, ref Color compColor)
        {
            var color = compColor.ToArgb();
            Span<int> hsv = stackalloc int[3];
            int alpha;

            // ARGB -> RGBA
            hsv[2] = (color & 0x000000FF) >> 0;
            hsv[1] = (color & 0x0000FF00) >> 8;
            hsv[0] = (color & 0x00FF0000) >> 16;
            alpha = unchecked((int)((color & 0xFF000000) >> 24));

            ImGui.SliderInt3(label, ref hsv[0], 0, 255);

            int ret = alpha << 24 | hsv[0] << 16 | hsv[1] << 8 | hsv[2];

            compColor = Color.FromArgb(ret);
        }

        public static void InputColor3(string label, ref Color compColor)
        {
            var color = compColor.ToArgb();
            Span<int> hsv = stackalloc int[3];
            int alpha;

            // ARGB -> RGBA
            hsv[2] = (color & 0x000000FF) >> 0;
            hsv[1] = (color & 0x0000FF00) >> 8;
            hsv[0] = (color & 0x00FF0000) >> 16;
            alpha = unchecked((int)((color & 0xFF000000) >> 24));

            ImGui.InputInt3(label, ref hsv[0]);

            int ret = alpha << 24 | hsv[0] << 16 | hsv[1] << 8 | hsv[2];

            compColor = Color.FromArgb(ret);
        }

        public static void InputRadians(string label, ref float v)
        {
            var deg = MathHelper.RadiansToDegrees(v);
            ImGui.InputFloat(label, ref deg, 1f, 15f);
            v = MathHelper.DegreesToRadians(deg);
        }

        public static string? InputString(string label, string? value)
        {
            var str = value ?? string.Empty;

            ImGui.InputText(label, ref str, 32, ImGuiInputTextFlags.None);

            return string.IsNullOrWhiteSpace(str) ? null : str;
        }

        public static void InputQuaternion(string label, ref Quaternion_Num q)
        {
            var v = q.ToTk();
            var r = v.ToEulerAngles();

            var eDegVec = new Vector3_Num(
                MathHelper.RadiansToDegrees(r.X),
                MathHelper.RadiansToDegrees(r.Y),
                MathHelper.RadiansToDegrees(r.Z));

            ImGui.InputFloat3("Orientation", ref eDegVec);

            var res = new Vector3(
                MathHelper.DegreesToRadians(eDegVec.X),
                MathHelper.DegreesToRadians(eDegVec.Y),
                MathHelper.DegreesToRadians(eDegVec.Z));

            q = OpenTK.Mathematics.Quaternion.FromEulerAngles(res).ToNumeric();
        }
    }
}
