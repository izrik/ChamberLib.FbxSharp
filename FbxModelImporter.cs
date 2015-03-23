using System;
using ChamberLib.Content;
using System.IO;
using FbxSharp;
using System.Collections.Generic;
using System.Linq;

using _FbxSharp = global::FbxSharp;

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


                    var layer = mesh.GetLayer(0);
                    var normalElement = layer.GetNormals();
                    var normals = normalElement.GetDirectArray().List;

                    if (normalElement.MappingMode == LayerElement.EMappingMode.ByPolygonVertex &&
                        normalElement.ReferenceMode == LayerElement.EReferenceMode.Direct)
                    {
                        if (normals.Count != part.Indexes.Indexes.Length)
                            throw new InvalidOperationException();

                        int i;
                        for (i = 0; i < normals.Count; i++)
                        {
                            var n = normals[i].ToChamber().ToVectorXYZ();
                            var index = part.Indexes.Indexes[i];
                            part.Vertexes.Vertices[index].SetNormal(n);
                        }
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }


                    var uvElement = layer.GetUVs();
                    var uvs = uvElement.GetDirectArray().List;
                    var uvindexes = uvElement.GetIndexArray().List;

                    if (uvElement.MappingMode == LayerElement.EMappingMode.ByPolygonVertex &&
                        uvElement.ReferenceMode == LayerElement.EReferenceMode.IndexToDirect)
                    {
                        if (uvindexes.Count != part.Indexes.Indexes.Length)
                            throw new InvalidOperationException();

                        int i;
                        for (i = 0; i < normals.Count; i++)
                        {
                            var uvindex = uvindexes[i];
                            var index = part.Indexes.Indexes[i];
                            var uv = uvs[uvindex].ToChamber();
                            var uv2 = new Vector2(uv.X, 1-uv.Y);
                            part.Vertexes.Vertices[index].SetTextureCoords(uv2);
                        }
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }


                    var matelem = layer.GetMaterials();
                    var mats = matelem.GetDirectArray().List;
                    var matindexes = matelem.GetIndexArray().List;

                    if (matelem.MappingMode == LayerElement.EMappingMode.AllSame &&
                        matelem.ReferenceMode == LayerElement.EReferenceMode.IndexToDirect)
                    {
                        var material = mats[0];
                        var material2 = new MaterialContent();
                        material2.Shader = importer.ImportShader("$skinned", importer);
                        material2.Alpha = 1;
                        material2.DiffuseColor = new Vector3(0.5f, 0.5f, 0.5f);
                        material2.Name = material.Name;

                        if (material is SurfaceLambert)
                        {
                            var lambert = material as SurfaceLambert;
                            material2.DiffuseColor =
                                ConvertMaterialColor(lambert, "Diffuse", lambert.Diffuse).ToChamber();
                            material2.EmissiveColor =
                                ConvertMaterialColor(lambert, "Emissive", lambert.Emissive).ToChamber();
                        }
                        else
                        {
                            material2.DiffuseColor =
                                ConvertMaterialColor(material, "Diffuse").ToChamber();
                            material2.EmissiveColor =
                                ConvertMaterialColor(material, "Emissive").ToChamber();
                        }

                        if (material is SurfacePhong)
                        {
                            var phong = material as SurfacePhong;

                            material2.SpecularColor =
                                ConvertMaterialColor(phong, "Specular", phong.Specular).ToChamber();

                            var props =
                                phong.FindProperties(
                                    p => p.Name.ToLower() == "shininessexponent" ||
                                         p.Name.ToLower() == "shininess");
                            double shininess = 0;
                            foreach (var prop in props)
                            {
                                if (prop.PropertyDataType == typeof(double))
                                {
                                    shininess = prop.Get<double>();
                                    if (shininess > 0)
                                        break;
                                }
                            }

                            material2.SpecularPower = (float)shininess;
                        }

                        part.Material = material2;
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
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

            if (scene.Poses.Count > 0)
            {
                var pose = scene.Poses[0];
                foreach (var pi in pose.PoseInfos)
                {
                    var bone = bonesByNode[pi.Node];
                    bone.InverseBindPose = pi.Matrix.ToChamber();
                }
            }

            return model;
        }

        static _FbxSharp.Vector3 ConvertMaterialColor(SurfaceMaterial material, string name, Property include=null)
        {
            var name1 = name.ToLower();
            var name2 = name1 + "color";
            var props = material.FindProperties(
                p => p.Name.ToLower() == name1/* ||
                     p.Name.ToLower() == name2*/).ToList();

            var v = new _FbxSharp.Vector3(0, 0, 0);
            if (include != null && !props.Contains(include))
            {
                props.Add(include);
            }

            foreach (var p in props)
            {
                if (p.PropertyDataType == typeof(_FbxSharp.Vector3))
                {
                    v = (_FbxSharp.Vector3)p.GetValue();
                    if (v != _FbxSharp.Vector3.Zero)
                        return v;
                }
            }

            foreach (var p in props)
            {
                if (p.PropertyDataType == typeof(_FbxSharp.Color))
                {
                    v = p.Get<_FbxSharp.Color>().ToVector3();
                    if (v != _FbxSharp.Vector3.Zero)
                        return v;
                }
            }

            return _FbxSharp.Vector3.Zero;
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

