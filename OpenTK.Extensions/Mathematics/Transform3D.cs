namespace OpenTK.Mathematics
{
    public class Transform3D
    {
        private Vector3 _position = Vector3.Zero;
        private Quaternion _rotation = Quaternion.Identity;
        private Vector3 _scale = Vector3.One;

        private Matrix4 _matrix;
        private bool _dirty = true;

        public static readonly Transform3D Identity = new();

        public Vector3 Position
        {
            get => _position;
            set
            {
                _position = value;
                _dirty = true;
            }
        }

        public Quaternion Rotation
        {
            get => _rotation;
            set
            {
                _rotation = value;
                _dirty = true;
            }
        }

        public Vector3 Scale
        {
            get => _scale;
            set
            {
                _scale = value;
                _dirty = true;
            }
        }

        /// <summary>
        /// Matrix for transforming points from local to world space.
        /// </summary>
        public Matrix4 Matrix
        {
            get
            {
                GenMatrix();
                return _matrix;
            }
        }

        /// <summary>
        /// Matrix for transforming points from world to local space.
        /// </summary>
        public Matrix4 InvMatrix
        {
            get
            {
                GenMatrix();
                return Matrix4.Invert(_matrix);
            }
        }

        /// <summary>
        /// Local X axis.
        /// </summary>
        public Vector3 Forward
        {
            get
            {
                GenMatrix();
                return _matrix.Row0.Xyz;
            }
        }

        /// <summary>
        /// Local Y axis.
        /// </summary>
        public Vector3 Left
        {
            get
            {
                GenMatrix();
                return _matrix.Row1.Xyz;
            }
        }

        /// <summary>
        /// Local Z axis.
        /// </summary>
        public Vector3 Up
        {
            get
            {
                GenMatrix();
                return _matrix.Row2.Xyz;
            }
        }

        private void GenMatrix()
        {
            if(!_dirty)
                return;

            _dirty = false;
            Matrix4.CreateScale(in _scale, out var scale);
            Matrix4.CreateTranslation(in _position, out var translation);
            Matrix4.CreateFromQuaternion(in _rotation, out var rotation);

            _matrix = scale * rotation * translation;
        }

        public Vector3 Transform(Vector3 vec)
        {
            GenMatrix();

            var vHomo = new Vector4(vec, 1);
            var vp = Vector4.TransformRow(vHomo, _matrix);
            return vp.Xyz;
        }

        public Vector3 Transform(float x, float y, float z)
        {
            GenMatrix();

            var vHomo = new Vector4(x, y, z, 1);
            var vp = Vector4.TransformRow(vHomo, _matrix);
            return vp.Xyz;
        }
    }
}
