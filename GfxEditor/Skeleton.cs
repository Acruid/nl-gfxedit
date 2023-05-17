using OpenTK.Mathematics;
using static GfxEditor.File3di;

namespace GfxEditor;

using Bone = ModelSegmentMesh;

internal static class Skeleton
{
    public static void CalculateBoneTransforms(ReadOnlySpan<ModelSegmentMesh> bones,
        ReadOnlySpan<byte> rotations,
        float anmHeight,
        Span<Matrix4> outMatrices)
    {
        outMatrices[0] = Matrix4.Identity;

        // calculate the bone transforms
        for (int i = 0; i < bones.Length; i++)
        {
            // get the bone and parent bone
            Bone bone = bones[i];

            // get the rotation bytes for this bone
            // byte zRotationByte = rotations[i * 3];
            // byte yRotationByte = rotations[i * 3 + 1];
            // byte xRotationByte = rotations[i * 3 + 2];

            byte zRotationByte = 0;
            byte yRotationByte = 32;
            byte xRotationByte = 0;

            // convert the rotation bytes to floats
            float zRotation = zRotationByte / 255f * MathHelper.TwoPi;
            float yRotation = yRotationByte / 255f * MathHelper.TwoPi;
            float xRotation = xRotationByte / 255f * MathHelper.TwoPi;

            // create a quaternion from the Euler angles
            Quaternion rotation = Quaternion.FromEulerAngles(xRotation, yRotation, zRotation);

            // calculate the model space rotation matrix
            //Matrix4 modelRotationMatrix = Matrix4.CreateFromQuaternion(rotation);
            Matrix4 modelRotationMatrix = Matrix4.Identity;

            // calculate the model space translation matrix
            Matrix4 modelTranslationMatrix = Matrix4.CreateTranslation(bone.ModelPosition);

            // get the parent bone matrix
            // for the root bone, it's parent references itself, so at the top of the function the special case is handled.
            Matrix4 parentBoneMatrix = outMatrices[bone.parentBone];

            Vector3 parentModelPos = bones[bone.parentBone].ModelPosition;

            Vector3 parentOffsetPos = bone.ModelPosition - parentModelPos;


            // calculate local position offset from the parent bone
            //Matrix4 localPosition = modelTranslationMatrix * Matrix4.Invert(parentBoneMatrix);
            //Vector3 localPosition = Vector3.TransformPosition(bone.ModelPosition, Matrix4.Invert(parentBoneMatrix));
            Vector3 localPosition = Vector3.Transform(parentOffsetPos, rotation) + parentModelPos;
            //Vector3 localPosition = bone.ModelPosition;

            // insert it into the model rotation...
            modelRotationMatrix.M41 = localPosition.X;
            modelRotationMatrix.M42 = localPosition.Y;
            modelRotationMatrix.M43 = localPosition.Z;

            // Finally output the combined matrix
            //outMatrices[i] = modelRotationMatrix;
            outMatrices[i] = Matrix4.Identity;
        }
    }
}