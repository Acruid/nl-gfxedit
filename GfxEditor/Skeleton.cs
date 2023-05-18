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
        for (int iBone = 0; iBone < bones.Length; iBone++)
        {
            // get the bone and parent bone
            Bone bone = bones[iBone];
            Bone pBone = bones[bone.parentBone];

            // get the rotation bytes for this bone
            byte xRotationByte = rotations[bone.parentBone * 4 + 2];
            byte yRotationByte = rotations[bone.parentBone * 4 + 1];
            byte zRotationByte = rotations[bone.parentBone * 4 + 0];
            
            // convert the rotation bytes to radian floats
            float xRotation = xRotationByte / 255f * MathHelper.TwoPi;
            float yRotation = yRotationByte / 255f * MathHelper.TwoPi;
            float zRotation = zRotationByte / 255f * MathHelper.TwoPi;

            // create a matrix from the animation angles
            Matrix4 rotX = Matrix4.CreateFromAxisAngle( Vector3.UnitX, xRotation);
            Matrix4 rotY = Matrix4.CreateFromAxisAngle(-Vector3.UnitY, yRotation);
            Matrix4 rotZ = Matrix4.CreateFromAxisAngle( Vector3.UnitZ, zRotation);
            Matrix4 pBoneAnmRotationMat = rotX * rotY * rotZ;

            /*
                NOTE: The vertices are in model space. With an input rotation of 0,0,0 and a height of 0,
                this should be outputting an identity matrix.

                The rotations are in model space for each bone. The rotations do not accumulate from the parent bone.

                The 3DI properly sorts the bones/subObs set so that the parent bones always come before the children.

                Each bone is translated to the end of the parent bone. You can find the offset of the parent bone by
                doing bone.ModelPosition - pBone.ModelPosition. The offset vector then gets rotated by the anm
                rotations, and then the difference is outputted in the matrix.

                Example: The Pelvis is the root bone of the skeleton. It always have an offset of vec3.Zero, being the
                root. It has an anm rotation. The matrix for the Pelvis needs to have a translation of 0, and contain the
                rotation, so that the Pelvis skin is properly rotated.
                
                R_Thigh is connected to the hips. It has a model space position and a rotation. The position in parent
                bone space is calculated with bone.ModelPosition - pBone.ModelPosition. This can also be thought of as
                the length of the bone. The position is orbited around the pBone origin using the parent bone rotation,
                then transformed back to model space. The difference in position is calculated between the original
                model position and the transformed position, boneTransformedPosition - bone.ModelPosition. This is now
                inserted into the matrix to transform the bone skin.
            */
            var pLocalPosition = bone.ModelPosition - pBone.ModelPosition;
            
            var pXformLocalPosition = (new Vector4(pLocalPosition, 1) * pBoneAnmRotationMat).Xyz;

            var pBoneTransformedPosition = pBone.ModelPosition + outMatrices[bone.parentBone].Row3.Xyz;

            var boneTransformedPosition = pXformLocalPosition + pBoneTransformedPosition;

            var modelDiffPosition = boneTransformedPosition - bone.ModelPosition;

            // these should be 0 for Pelvis
            var outMatrix = Matrix4.Identity;
            outMatrix.M41 = modelDiffPosition.X;
            outMatrix.M42 = modelDiffPosition.Y;
            outMatrix.M43 = modelDiffPosition.Z;

            /*
                The rotation of R_Thigh is in model space, not relative to the parent bone. This is inserted directly
                into the transform matrix to rotate the skin.
                
                R_Thigh outMatrix should now transform the original bone skin to the end on the parent bone so that
                it stays attached, and rotate the bone based on the rotation in the animation.
            */
            outMatrices[iBone] = outMatrix;
        }
    }
}