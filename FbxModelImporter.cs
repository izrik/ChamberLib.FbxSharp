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



            var material = new MaterialContent();
            material.Shader = importer.ImportShader("$skinned", importer);
            material.Alpha = 1;
            material.DiffuseColor = new Vector3(0.5f, 0.5f, 0.5f);

            var model = new ModelContent();
            model.Filename = filename;

            var bonesByNode = new Dictionary<Node, BoneContent>();
            foreach (var node in scene.Nodes)
            {
                var bone = BoneFromNode(node);
                bonesByNode[node]=bone;
                model.Bones.Add(bone);

                if (node.GetNodeAttributeCount() > 0 &&
                    node.GetNodeAttributeByIndex(0) is Mesh)
                {
                    var mesh = (Mesh)node.GetNodeAttributeByIndex(0);
                    var mesh2 = new MeshContent();
                    model.Meshes.Add(mesh2);
                    var part = new PartContent();
                    mesh2.Parts.Add(part);
                    part.Vertexes = new VertexBufferContent();
                    part.Vertexes.Vertices =
                        mesh.GetControlPoints()
                            .Select(v => new Vertex_PBiBwNT(){
                                Position = v.ToChamber().ToVectorXYZ(),
                                Normal = Vector3.UnitY,
                            })
                            .Cast<IVertex>()
                            .ToArray();
                    model.VertexBuffers.Add(part.Vertexes);
                    part.NumVertexes = part.Vertexes.Vertices.Length;
                    part.Indexes = new IndexBufferContent();
                    part.Indexes.Indexes =
                        mesh.PolygonIndexes
                            .SelectMany(p => {
                                if (p.Count != 3) throw new InvalidOperationException();
                                return p;
                            })
                            .Select(i => (short)i)
                            .ToArray();
                    model.IndexBuffers.Add(part.Indexes);
                    part.PrimitiveCount = part.Indexes.Indexes.Length / 3;
                    part.Material = material;
                }
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

