using System;
using ChamberLib.Content;
using System.IO;
using FbxSharp;
using System.Collections.Generic;
using System.Linq;

namespace ChamberLib.FbxSharp
{
    public class FbxModelImporter
    {
        public FbxModelImporter(ModelImporter next=null)
        {
            this.next = next;
        }

        readonly ModelImporter next;

        public ModelContent ImportModel(string filename, IContentImporter importer)
        {
            if (File.Exists(filename))
            {
            }
            else if (File.Exists(filename + ".fbx"))
            {
                filename += ".fbx";
            }
            else if (next != null)
            {
                return next(filename, importer);
            }
            else
            {
                throw new FileNotFoundException("The file could not be found", filename);
            }

            var fimporter = new Importer();

            var scene = fimporter.Import(filename);



            var model = new ModelContent();
            model.Filename = filename;

            var bonesByNode = new Dictionary<Node, BoneContent>();
            foreach (var node in scene.Nodes)
            {
                var bone = BoneFromNode(node);
                bonesByNode[node]=bone;
                model.Bones.Add(bone);
            }
            foreach (var node in scene.Nodes)
            {
                var bone = bonesByNode[node];
                int i;
                int n = node.GetChildCount();
                for (i = 0; i < n; i++)
                {
                    bone.ChildBoneIndexes.Add(scene.Nodes.IndexOf(node.GetChild(i)));
                }
            }

            return model;
        }

        static BoneContent BoneFromNode(Node node)
        {
            var bone = new BoneContent();
            bone.Name = node.Name;

            var translation =
                (node.TranslationActive.Get() ?
                    node.LclTranslation.Get().ToChamber() :
                    Vector3.Zero);
            var scaling =
                (node.ScalingActive.Get() ?
                    node.LclScaling.Get().ToChamber() :
                    Vector3.One);
            var rotationEuler =
                (node.RotationActive.Get() ?
                    node.LclRotation.Get().ToChamber() :
                    Vector3.Zero);

            var order = node.RotationOrder.Get();

            var mt = Matrix.CreateTranslation(translation);
            var mrx = Matrix.CreateRotationX(rotationEuler.X.ToRadians());
            var mry = Matrix.CreateRotationY(rotationEuler.Y.ToRadians());
            var mrz = Matrix.CreateRotationZ(rotationEuler.Z.ToRadians());
            var ms = Matrix.CreateScale(scaling);

            Matrix mr;
            switch (order)
            {
            case Node.ERotationOrder.OrderXYZ:
                mr = mrx * mry * mrz;
                break;
            default:
                throw new NotImplementedException();
            }

            bone.Transform = ms * mr * mt;

            bone.InverseBindPose = Matrix.Identity;

            return bone;
        }
    }
}

