using System;
using NUnit.Framework;

using _FbxSharp = FbxSharp;

namespace ChamberLib.FbxSharp.Tests
{
    [TestFixture]
    public class MatrixHelperTest
    {
        [Test]
        public void ElementIndexesAreFlipped()
        {
            var m = new _FbxSharp.Matrix(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            var mm = m.ToChamber();

            Assert.AreEqual(mm.M11, m.M00);
            Assert.AreEqual(mm.M21, m.M01);
            Assert.AreEqual(mm.M31, m.M02);
            Assert.AreEqual(mm.M41, m.M03);
            Assert.AreEqual(mm.M12, m.M10);
            Assert.AreEqual(mm.M22, m.M11);
            Assert.AreEqual(mm.M32, m.M12);
            Assert.AreEqual(mm.M42, m.M13);
            Assert.AreEqual(mm.M13, m.M20);
            Assert.AreEqual(mm.M23, m.M21);
            Assert.AreEqual(mm.M33, m.M22);
            Assert.AreEqual(mm.M43, m.M23);
            Assert.AreEqual(mm.M14, m.M30);
            Assert.AreEqual(mm.M24, m.M31);
            Assert.AreEqual(mm.M34, m.M32);
            Assert.AreEqual(mm.M44, m.M33);

            Assert.AreEqual( 1, mm.M11);
            Assert.AreEqual( 2, mm.M12);
            Assert.AreEqual( 3, mm.M13);
            Assert.AreEqual( 4, mm.M14);
            Assert.AreEqual( 5, mm.M21);
            Assert.AreEqual( 6, mm.M22);
            Assert.AreEqual( 7, mm.M23);
            Assert.AreEqual( 8, mm.M24);
            Assert.AreEqual( 9, mm.M31);
            Assert.AreEqual(10, mm.M32);
            Assert.AreEqual(11, mm.M33);
            Assert.AreEqual(12, mm.M34);
            Assert.AreEqual(13, mm.M41);
            Assert.AreEqual(14, mm.M42);
            Assert.AreEqual(15, mm.M43);
            Assert.AreEqual(16, mm.M44);
        }

        [Test]
        public void RotationXAngleIsFlipped()
        {
            var m = new _FbxSharp.Matrix(
                _FbxSharp.Vector4.Zero,
                new _FbxSharp.Vector4(37, 0, 0),
                _FbxSharp.Vector3.One.ToVector4());

            // when:
            var mm = m.ToChamber();
            var aa = mm.DecomposedRotation.AxisAngle;

            // then:
            Assert.AreEqual(Vector3.UnitX, aa.ToVectorXYZ());
            Assert.AreEqual(0.645772f, aa.W, 0.000001f);

            // when:
            var m2 = Matrix.CreateRotationX((37.0f).ToRadians());
            var aa2 = m2.DecomposedRotation.AxisAngle;

            // then:
            Assert.AreEqual(Vector3.UnitX, aa2.ToVectorXYZ());
            Assert.AreEqual(0.645772f, aa2.W, 0.000001f);
        }

        [Test]
        public void RotationYAngleIsFlipped()
        {
            var m = new _FbxSharp.Matrix(
                _FbxSharp.Vector4.Zero,
                new _FbxSharp.Vector4(0, 37, 0),
                _FbxSharp.Vector3.One.ToVector4());

            // when:
            var mm = m.ToChamber();
            var aa = mm.DecomposedRotation.AxisAngle;

            // then:
            Assert.AreEqual(Vector3.UnitY, aa.ToVectorXYZ());
            Assert.AreEqual(0.645772f, aa.W, 0.000001f);

            // when:
            var m2 = Matrix.CreateRotationY((37.0f).ToRadians());
            var aa2 = m2.DecomposedRotation.AxisAngle;

            // then:
            Assert.AreEqual(Vector3.UnitY, aa2.ToVectorXYZ());
            Assert.AreEqual(0.645772f, aa2.W, 0.000001f);
        }

        [Test]
        public void RotationZAngleIsFlipped()
        {
            var m = new _FbxSharp.Matrix(
                _FbxSharp.Vector4.Zero,
                new _FbxSharp.Vector4(0, 0, 37),
                _FbxSharp.Vector3.One.ToVector4());

            // when:
            var mm = m.ToChamber();
            var aa = mm.DecomposedRotation.AxisAngle;

            // then:
            Assert.AreEqual(Vector3.UnitZ, aa.ToVectorXYZ());
            Assert.AreEqual(0.645772f, aa.W, 0.000001f);

            // when:
            var m2 = Matrix.CreateRotationZ((37.0f).ToRadians());
            var aa2 = m2.DecomposedRotation.AxisAngle;

            // then:
            Assert.AreEqual(Vector3.UnitZ, aa2.ToVectorXYZ());
            Assert.AreEqual(0.645772f, aa2.W, 0.000001f);
        }

        [Test]
        public void TranslationIsPreservedAfterConversion1()
        {
            var m = new _FbxSharp.Matrix(
                    new _FbxSharp.Vector4(2, 3, 4),
                _FbxSharp.Vector4.Zero,
                _FbxSharp.Vector3.One.ToVector4());

            // when:
            var mm = m.ToChamber();
            var translation = mm.DecomposedTranslation;

            // then:
            Assert.AreEqual(2, translation.X);
            Assert.AreEqual(3, translation.Y);
            Assert.AreEqual(4, translation.Z);
        }

        [Test]
        public void TranslationIsPreservedAfterConversion2()
        {
            var m = new _FbxSharp.Matrix(
                new _FbxSharp.Vector4(2, 3, 4),
                _FbxSharp.Vector4.Zero,
                _FbxSharp.Vector3.One.ToVector4());

            // when:
            var mm = m.ToChamber();
            var translation = mm.Translation;

            // then:
            Assert.AreEqual(2, translation.X);
            Assert.AreEqual(3, translation.Y);
            Assert.AreEqual(4, translation.Z);
        }

        [Test]
        public void TranslationIsPreservedAfterConversion3()
        {
            // given:
            var m = new _FbxSharp.Matrix(
                new _FbxSharp.Vector4(2, 3, 4),
                _FbxSharp.Vector4.Zero,
                _FbxSharp.Vector3.One.ToVector4());
            var expected = Matrix.CreateTranslation(2, 3, 4);

            // when:
            var actual = m.ToChamber();

            // then:
            Assert.AreEqual(expected.M11, actual.M11, 0.00001f);
            Assert.AreEqual(expected.M12, actual.M12, 0.00001f);
            Assert.AreEqual(expected.M13, actual.M13, 0.00001f);
            Assert.AreEqual(expected.M14, actual.M14, 0.00001f);
            Assert.AreEqual(expected.M21, actual.M21, 0.00001f);
            Assert.AreEqual(expected.M22, actual.M22, 0.00001f);
            Assert.AreEqual(expected.M23, actual.M23, 0.00001f);
            Assert.AreEqual(expected.M24, actual.M24, 0.00001f);
            Assert.AreEqual(expected.M31, actual.M31, 0.00001f);
            Assert.AreEqual(expected.M32, actual.M32, 0.00001f);
            Assert.AreEqual(expected.M33, actual.M33, 0.00001f);
            Assert.AreEqual(expected.M34, actual.M34, 0.00001f);
            Assert.AreEqual(expected.M41, actual.M41, 0.00001f);
            Assert.AreEqual(expected.M42, actual.M42, 0.00001f);
            Assert.AreEqual(expected.M43, actual.M43, 0.00001f);
            Assert.AreEqual(expected.M44, actual.M44, 0.00001f);
        }

        [Test]
        public void ScaleIsPreservedAfterConversion()
        {
            var m = new _FbxSharp.Matrix(
                _FbxSharp.Vector4.Zero,
                _FbxSharp.Vector4.Zero,
                new _FbxSharp.Vector4(2, 3, 4));
            var expected = Matrix.CreateScale(new Vector3(2, 3, 4));

            // when:
            var actual = m.ToChamber();

            // then:
            Assert.AreEqual(expected.M11, actual.M11, 0.00001f);
            Assert.AreEqual(expected.M12, actual.M12, 0.00001f);
            Assert.AreEqual(expected.M13, actual.M13, 0.00001f);
            Assert.AreEqual(expected.M14, actual.M14, 0.00001f);
            Assert.AreEqual(expected.M21, actual.M21, 0.00001f);
            Assert.AreEqual(expected.M22, actual.M22, 0.00001f);
            Assert.AreEqual(expected.M23, actual.M23, 0.00001f);
            Assert.AreEqual(expected.M24, actual.M24, 0.00001f);
            Assert.AreEqual(expected.M31, actual.M31, 0.00001f);
            Assert.AreEqual(expected.M32, actual.M32, 0.00001f);
            Assert.AreEqual(expected.M33, actual.M33, 0.00001f);
            Assert.AreEqual(expected.M34, actual.M34, 0.00001f);
            Assert.AreEqual(expected.M41, actual.M41, 0.00001f);
            Assert.AreEqual(expected.M42, actual.M42, 0.00001f);
            Assert.AreEqual(expected.M43, actual.M43, 0.00001f);
            Assert.AreEqual(expected.M44, actual.M44, 0.00001f);
        }

        [Test]
        public void RotationXIsPreservedAfterConversion()
        {
            var m = new _FbxSharp.Matrix(
                _FbxSharp.Vector4.Zero,
                new _FbxSharp.Vector4(37, 0, 0),
                _FbxSharp.Vector3.One.ToVector4());
            var expected = Matrix.CreateRotationX(37.0f.ToRadians());

            // when:
            var actual = m.ToChamber();

            // then:
            Assert.AreEqual(expected.M11, actual.M11, 0.00001f);
            Assert.AreEqual(expected.M12, actual.M12, 0.00001f);
            Assert.AreEqual(expected.M13, actual.M13, 0.00001f);
            Assert.AreEqual(expected.M14, actual.M14, 0.00001f);
            Assert.AreEqual(expected.M21, actual.M21, 0.00001f);
            Assert.AreEqual(expected.M22, actual.M22, 0.00001f);
            Assert.AreEqual(expected.M23, actual.M23, 0.00001f);
            Assert.AreEqual(expected.M24, actual.M24, 0.00001f);
            Assert.AreEqual(expected.M31, actual.M31, 0.00001f);
            Assert.AreEqual(expected.M32, actual.M32, 0.00001f);
            Assert.AreEqual(expected.M33, actual.M33, 0.00001f);
            Assert.AreEqual(expected.M34, actual.M34, 0.00001f);
            Assert.AreEqual(expected.M41, actual.M41, 0.00001f);
            Assert.AreEqual(expected.M42, actual.M42, 0.00001f);
            Assert.AreEqual(expected.M43, actual.M43, 0.00001f);
            Assert.AreEqual(expected.M44, actual.M44, 0.00001f);
        }

        [Test]
        public void RotationYIsPreservedAfterConversion()
        {
            var m = new _FbxSharp.Matrix(
                _FbxSharp.Vector4.Zero,
                new _FbxSharp.Vector4(0, 56, 0),
                _FbxSharp.Vector3.One.ToVector4());
            var expected = Matrix.CreateRotationY(56.0f.ToRadians());

            // when:
            var actual = m.ToChamber();

            // then:
            Assert.AreEqual(expected.M11, actual.M11, 0.00001f);
            Assert.AreEqual(expected.M12, actual.M12, 0.00001f);
            Assert.AreEqual(expected.M13, actual.M13, 0.00001f);
            Assert.AreEqual(expected.M14, actual.M14, 0.00001f);
            Assert.AreEqual(expected.M21, actual.M21, 0.00001f);
            Assert.AreEqual(expected.M22, actual.M22, 0.00001f);
            Assert.AreEqual(expected.M23, actual.M23, 0.00001f);
            Assert.AreEqual(expected.M24, actual.M24, 0.00001f);
            Assert.AreEqual(expected.M31, actual.M31, 0.00001f);
            Assert.AreEqual(expected.M32, actual.M32, 0.00001f);
            Assert.AreEqual(expected.M33, actual.M33, 0.00001f);
            Assert.AreEqual(expected.M34, actual.M34, 0.00001f);
            Assert.AreEqual(expected.M41, actual.M41, 0.00001f);
            Assert.AreEqual(expected.M42, actual.M42, 0.00001f);
            Assert.AreEqual(expected.M43, actual.M43, 0.00001f);
            Assert.AreEqual(expected.M44, actual.M44, 0.00001f);
        }

        [Test]
        public void RotationZIsPreservedAfterConversion()
        {
            var m = new _FbxSharp.Matrix(
                _FbxSharp.Vector4.Zero,
                new _FbxSharp.Vector4(0, 0, 91),
                _FbxSharp.Vector3.One.ToVector4());
            var expected = Matrix.CreateRotationZ(91.0f.ToRadians());

            // when:
            var actual = m.ToChamber();

            // then:
            Assert.AreEqual(expected.M11, actual.M11, 0.00001f);
            Assert.AreEqual(expected.M12, actual.M12, 0.00001f);
            Assert.AreEqual(expected.M13, actual.M13, 0.00001f);
            Assert.AreEqual(expected.M14, actual.M14, 0.00001f);
            Assert.AreEqual(expected.M21, actual.M21, 0.00001f);
            Assert.AreEqual(expected.M22, actual.M22, 0.00001f);
            Assert.AreEqual(expected.M23, actual.M23, 0.00001f);
            Assert.AreEqual(expected.M24, actual.M24, 0.00001f);
            Assert.AreEqual(expected.M31, actual.M31, 0.00001f);
            Assert.AreEqual(expected.M32, actual.M32, 0.00001f);
            Assert.AreEqual(expected.M33, actual.M33, 0.00001f);
            Assert.AreEqual(expected.M34, actual.M34, 0.00001f);
            Assert.AreEqual(expected.M41, actual.M41, 0.00001f);
            Assert.AreEqual(expected.M42, actual.M42, 0.00001f);
            Assert.AreEqual(expected.M43, actual.M43, 0.00001f);
            Assert.AreEqual(expected.M44, actual.M44, 0.00001f);
        }
    }
}

