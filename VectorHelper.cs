using System;

namespace ChamberLib.FbxSharp
{
    public static class VectorHelper
    {
        public static ChamberLib.Vector2 ToChamber(this global::FbxSharp.FbxVector2 v)
        {
            return
                new Vector2(
                    (float)v.X,
                    (float)v.Y);
        }

        public static ChamberLib.Vector3 ToChamber(this global::FbxSharp.FbxVector3 v)
        {
            return
                new Vector3(
                    (float)v.X,
                    (float)v.Y,
                    (float)v.Z);
        }

        public static ChamberLib.Vector4 ToChamber(this global::FbxSharp.FbxVector4 v)
        {
            return
                new Vector4(
                    (float)v.X,
                    (float)v.Y,
                    (float)v.Z,
                    (float)v.W);
        }
    }
}

