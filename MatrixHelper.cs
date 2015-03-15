using System;

namespace ChamberLib.FbxSharp
{
    public static class MatrixHelper
    {
        public static ChamberLib.Matrix ToChamber(this global::FbxSharp.Matrix m)
        {
            return
                new Matrix(
                    (float)m.M00, (float)m.M01, (float)m.M02, (float)m.M03,
                    (float)m.M10, (float)m.M11, (float)m.M12, (float)m.M13,
                    (float)m.M20, (float)m.M21, (float)m.M22, (float)m.M23,
                    (float)m.M30, (float)m.M31, (float)m.M32, (float)m.M33);
        }
    }
}

