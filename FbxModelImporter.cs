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
            }
            foreach (var node in scene.Nodes)
            {
                var bone = bonesByNode[node];

                if (node.GetNodeAttributeCount() > 0 &&
                    node.GetNodeAttributeByIndex(0) is Mesh)
                {
                    var mesh = (Mesh)node.GetNodeAttributeByIndex(0);
                    var mesh2 = new MeshContent();
                    model.Meshes.Add(mesh2);




                    bool isSkinned = true;
                    ShaderContent shader;
                    if (mesh.GetDeformerCount() < 1)
                    {
                        isSkinned = false;
                        shader = importer.ImportShader("$basic", importer);
                    }
                    else if (mesh.GetDeformerCount() > 1)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        shader = importer.ImportShader("$skinned", importer);
                    }



                    // extract the blend indices and weights

                    var vertices =
                        Enumerable.Range(0, mesh.GetControlPointsCount())
                            .Select(ix =>
                                new Vertex_PBiBwNT {
                                    Position = mesh.GetControlPointAt((int)ix).ToChamber().ToVectorXYZ()
                                })
                            .ToList();

                    // extract the blend indices and weights

                    if (mesh.GetDeformerCount() == 1)
                    {
                        if (!(mesh.GetDeformer(0) is Skin))
                            throw new NotImplementedException("Only Skin deformers are implemented");

                        var boneIndicesL =
                            Enumerable.Range(
                                0,
                                mesh.GetControlPointsCount())
                            .Select(x => new List<float>())
                            .ToList();
                        var boneWeightsL =
                            Enumerable.Range(
                                0,
                                mesh.GetControlPointsCount())
                            .Select(x => new List<float>())
                            .ToList();

                        int i;

                        var skin = (Skin)mesh.GetDeformer(0);
                        foreach (var cluster in skin.Clusters)
                        {
                            var cnode = cluster.GetLink();
                            var cbone = bonesByNode[cnode];
                            var boneIndex = model.Bones.IndexOf(cbone);

                            for (i = 0; i < cluster.ControlPointIndices.Count; i++)
                            {
                                var index = cluster.ControlPointIndices[i];
                                var weight = cluster.ControlPointWeights[i];

                                if (boneIndicesL[index].Count > 4 ||
                                    boneWeightsL[index].Count > 4)
                                {
                                    throw new NotImplementedException("Too many indices or weights");
                                }

                                boneIndicesL[index].Add((float)boneIndex);
                                boneWeightsL[index].Add((float)weight);

                                if (boneIndicesL[index].Count > 4 ||
                                    boneWeightsL[index].Count > 4)
                                {
                                    throw new NotImplementedException("Too many indices or weights");
                                }
                            }
                        }

                        Func<List<float>, Vector4> convert = fs => {
                            if (fs.Count == 4) return new Vector4(fs[0], fs[1], fs[2], fs[3]);
                            if (fs.Count == 3) return new Vector4(fs[0], fs[1], fs[2],     0);
                            if (fs.Count == 2) return new Vector4(fs[0], fs[1],     0,     0);
                            if (fs.Count == 1) return new Vector4(fs[0],     0,     0,     0);
                            return Vector4.Zero;
                        };

                        var boneIndicesV = boneIndicesL.Select(convert).ToList();
                        var boneWeightsV = boneWeightsL.Select(convert).ToList();

                        for (i = 0; i < vertices.Count; i++)
                        {
                            var vertex = vertices[i];
                            vertex.SetBlendIndices(boneIndicesV[i]);
                            vertex.SetBlendWeights(boneWeightsV[i]);
                            vertices[i] = vertex;
                        }
                    }


                    // construct a list of polygons
                    //  beyond this point, we don't need `vertices` until it
                    //  gets re-used to build the vertex buffer

                    var polygons = mesh.PolygonIndexes.Select(p =>
                        new PolygonBuilder {
                            PolygonVertexIndexes = p,
                            Vertexes = p.Select(ix => vertices[(int)ix]).ToList(),
                        }).ToList();

                    var polygonsByMaterial = new Dictionary<SurfaceMaterial, List<PolygonBuilder>>();

                    // organize the mesh's polygons by material

                    var layer = mesh.GetLayer(0);
                    var matelem = layer.GetMaterials();
                    var matindexes = matelem.MaterialIndexes.List;// GetIndexArray().List;
                    if (matelem.ReferenceMode != LayerElement.EReferenceMode.IndexToDirect)
                        throw new NotImplementedException("A materials must have a reference mode of IndexToDirect");
                    if (matelem.MappingMode == LayerElement.EMappingMode.AllSame)
                    {
                        // only one material
                        var material = node.GetMaterial(matindexes[0]);

                        polygonsByMaterial[material] = new List<PolygonBuilder>(polygons);
                    }
                    else if (matelem.MappingMode == LayerElement.EMappingMode.ByPolygon)
                    {
                        // multiple materials
                        foreach (var mat in node.Materials)
                        {
                            polygonsByMaterial[mat] = new List<PolygonBuilder>();
                        }
                        int i;
                        for (i = 0; i < matindexes.Count; i++)
                        {
                            var mat = node.Materials[matindexes[i]];
                            polygonsByMaterial[mat].Add(polygons[i]);
                        }
                    }
                    else
                    {
                        throw new NotImplementedException("Materials must have mapping modes of AllSame or ByPolygon");
                    }

                    // extract the vertex normals

                    if (layer.GetNormals() != null)
                    {
                        var normalElement = layer.GetNormals();
                        if (normalElement.MappingMode != LayerElement.EMappingMode.ByPolygonVertex)
                        {
                            throw new NotImplementedException("Normals layer elements must have a mapping mode of ByPolygonVertex");
                        }
                        if (normalElement.ReferenceMode != LayerElement.EReferenceMode.Direct &&
                        normalElement.ReferenceMode != LayerElement.EReferenceMode.IndexToDirect)
                        {
                            throw new NotImplementedException("Normals layer elements must have a reference mode of Direct or IndexToDirect");
                        }
                        int k = 0;
                        foreach (var poly in polygons)
                        {
                            int i;
                            for (i = 0; i < poly.Vertexes.Count; i++)
                            {
                                int nindex;
                                if (normalElement.ReferenceMode == LayerElement.EReferenceMode.Direct)
                                {
                                    nindex = k;
                                }
                                else
                                {
                                    nindex = normalElement.GetIndexArray().GetAt(k);
                                }

                                var v = normalElement.GetDirectArray().GetAt(nindex);
                                var vertex = poly.Vertexes[i];
                                vertex.Normal = v.ToChamber().ToVectorXYZ();
                                poly.Vertexes[i] = vertex;
                                k++;
                            }
                        }
                    }

                    // extract the texture coordinates

                    if (layer.GetUVs() != null)
                    {
                        var uvElement = layer.GetUVs();
                        if (uvElement.MappingMode != LayerElement.EMappingMode.ByPolygonVertex)
                        {
                            throw new NotImplementedException("UV layer elements must have a mapping mode of ByPolygonVertex");
                        }
                        if (uvElement.ReferenceMode != LayerElement.EReferenceMode.Direct &&
                            uvElement.ReferenceMode != LayerElement.EReferenceMode.IndexToDirect)
                        {
                            throw new NotImplementedException("UV layer elements must have a reference mode of Direct or IndexToDirect");
                        }
                        int k = 0;
                        foreach (var poly in polygons)
                        {
                            int i;
                            for (i = 0; i < poly.Vertexes.Count; i++)
                            {
                                int nindex;
                                if (uvElement.ReferenceMode == LayerElement.EReferenceMode.Direct)
                                {
                                    nindex = k;
                                }
                                else
                                {
                                    nindex = uvElement.GetIndexArray().GetAt(k);
                                }

                                var v = uvElement.GetDirectArray().GetAt(nindex);
                                var vv = v.ToChamber();
                                var vertex = poly.Vertexes[i];
                                vertex.SetTextureCoords(new ChamberLib.Vector2(vv.X, 1 - vv.Y));
                                poly.Vertexes[i] = vertex;
                                k++;
                            }
                        }
                    }

                    // trianglize polygons with more than three points

                    foreach (var mat in polygonsByMaterial.Keys.ToArray())
                    {
                        var polys = polygonsByMaterial[mat];
                        polygonsByMaterial[mat] = polys.SelectMany(p => {
                            if (p.Vertexes.Count < 3) throw new InvalidOperationException();
                            if (p.Vertexes.Count == 3) return p.Yield();
                            int i;
                            var newPolys = new List<PolygonBuilder>();
                            for (i = 2; i < p.Vertexes.Count; i++)
                            {
                                var pb = new PolygonBuilder();

                                pb.PolygonVertexIndexes = new List<long>();
                                pb.PolygonVertexIndexes.Add(p.PolygonVertexIndexes[0]);
                                pb.PolygonVertexIndexes.Add(p.PolygonVertexIndexes[i-1]);
                                pb.PolygonVertexIndexes.Add(p.PolygonVertexIndexes[i]);

                                pb.Vertexes = new List<Vertex_PBiBwNT>();
                                pb.Vertexes.Add(p.Vertexes[0]);
                                pb.Vertexes.Add(p.Vertexes[i-1]);
                                pb.Vertexes.Add(p.Vertexes[i]);

                                newPolys.Add(pb);
                            }
                            return newPolys;
                        }).ToList();
                    }

                    // construct the vertex and index buffers

                    var vertset = new HashSet<Vertex_PBiBwNT>();
                    vertices.Clear();
                    var indices = new List<int>();

                    var vertexBuffer = new VertexBufferContent();
                    var indexBuffer = new IndexBufferContent();

                    model.VertexBuffers.Add(vertexBuffer);
                    model.IndexBuffers.Add(indexBuffer);

                    foreach (var mat in polygonsByMaterial.Keys)
                    {
                        var polys = polygonsByMaterial[mat];
                        var polyverts = polys.SelectMany(p => p.Vertexes).ToArray();

                        var startIndex = indices.Count;

                        vertices.AddRange(polyverts.Except(vertset));

                        var polyindices = polyverts.Select(p => vertices.IndexOf(p));
                        indices.AddRange(polyindices);

                        vertset.AddRange(polyverts);

                        mesh2.Parts.Add(new PartContent() {
                            Material = GetMaterialFromMaterial(mat, shader, importer, filename),
                            StartIndex = startIndex,
                            PrimitiveCount = polys.Count,
                            Vertexes = vertexBuffer,
                            Indexes = indexBuffer,
                        });
                    }

                    indexBuffer.Indexes = indices.Select(ix => (short)ix).ToArray();

                    if (isSkinned)
                    {
                        vertexBuffer.Vertices = vertices.Cast<IVertex>().ToArray();
                    }
                    else
                    {
                        vertexBuffer.Vertices =
                            vertices.Select<Vertex_PBiBwNT, IVertex>(v =>
                                new Vertex_PNT {
                                    Position = v.Position,
                                    Normal = v.Normal,
                                    TextureCoords = v.TextureCoords,
                                }).ToArray();
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

        static Dictionary<SurfaceMaterial, Dictionary<ShaderContent, MaterialContent>> _materialsCache = new Dictionary<SurfaceMaterial, Dictionary<ShaderContent, MaterialContent>>();

        static MaterialContent GetMaterialFromMaterial(SurfaceMaterial material, ShaderContent shader, IContentImporter importer, string filename)
        {
            if (_materialsCache.ContainsKey(material) && _materialsCache[material].ContainsKey(shader))
            {
                return _materialsCache[material][shader];
            }

            if (!_materialsCache.ContainsKey(material))
            {
                _materialsCache[material] = new Dictionary<ShaderContent, MaterialContent>();
            }

            var material2 = new MaterialContent();
            material2.Shader = shader;
            material2.Alpha = 1;
            material2.DiffuseColor = new Vector3(0.5f, 0.5f, 0.5f);
            material2.Name = material.Name;

            Property diffuseProp;
            Property emissiveProp;
            Property textureProp;
            if (material is SurfaceLambert)
            {
                var lambert = material as SurfaceLambert;
                diffuseProp = GetMaterialColorProperty(lambert, "Diffuse", lambert.Diffuse);
                emissiveProp = GetMaterialColorProperty(lambert, "Emissive", lambert.Emissive);
                textureProp = FindTextureProperty(lambert, "Diffuse", lambert.Diffuse);
            }
            else
            {
                diffuseProp = GetMaterialColorProperty(material, "Diffuse");
                emissiveProp = GetMaterialColorProperty(material, "Emissive");
                textureProp = FindTextureProperty(material, "Diffuse");
            }

            if (diffuseProp != null)
            {
                material2.DiffuseColor = GetPropertyValue(diffuseProp);
            }
            if (emissiveProp != null)
            {
                material2.EmissiveColor = GetPropertyValue(emissiveProp);
            }
            if (textureProp != null)
            {
                var tex = GetMaterialTexture(textureProp, importer, filename);
                if (tex != null)
                    material2.Texture = tex;
            }

            if (material is SurfacePhong)
            {
                var phong = material as SurfacePhong;

                var specularProp = GetMaterialColorProperty(phong, "Specular", phong.Specular);
                if (specularProp != null)
                {
                    material2.SpecularColor = GetPropertyValue(specularProp);

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
            }

            _materialsCache[material][shader] = material2;

            return material2;
        }

        static Property GetMaterialColorProperty(SurfaceMaterial material, string name, Property include=null)
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
                        return p;
                }
            }

            foreach (var p in props)
            {
                if (p.PropertyDataType == typeof(_FbxSharp.Color))
                {
                    v = p.Get<_FbxSharp.Color>().ToVector3();
                    if (v != _FbxSharp.Vector3.Zero)
                        return p;
                }
            }

            return null;
        }

        static ChamberLib.Vector3 GetPropertyValue(Property property)
        {
            if (property.PropertyDataType == typeof(_FbxSharp.Vector3))
            {
                return ((_FbxSharp.Vector3)property.GetValue()).ToChamber();
            }
            if (property.PropertyDataType == typeof(_FbxSharp.Color))
            {
                return property.Get<_FbxSharp.Color>().ToVector3().ToChamber();
            }

            throw new InvalidOperationException();
        }

        static Property FindTextureProperty(SurfaceMaterial material, string name, Property include=null)
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
                foreach (var src in p.SrcObjects)
                {
                    if (src is Texture)
                    {
                        return p;
                    }
                }
            }

            return null;
        }

        static TextureContent GetMaterialTexture(Property property, IContentImporter importer, string modelFilename)
        {
            foreach (var srcobj in property.SrcObjects)
            {
                var tex = srcobj as Texture;
                if (tex != null)
                {
                    var textureFilename = Path.Combine(Path.GetDirectoryName(modelFilename), tex.Filename);

                    return importer.ImportTexture2D(textureFilename, importer);
                }
            }

            return null;
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

