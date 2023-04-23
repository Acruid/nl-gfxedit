using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GfxEditor.Tests
{
    [TestFixture]
    internal class MatrixTests
    {
        [Test]
        public void SRT()
        {
            var translate = Matrix4.CreateTranslation(new Vector3(3, 3, 3));
            var scale = Matrix4.CreateScale(5);

            var result = Vector4.One * (scale * translate);
        }
    }
}
