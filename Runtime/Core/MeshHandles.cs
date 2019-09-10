using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;

namespace UnityEngine.ProBuilder
{
    static class MeshHandles
    {
        static List<int> s_VertexList = new List<int>();

        /// <summary>
        /// Populate a renderable's mesh with a face highlight mesh matching the selected triangles array.
        /// </summary>
        internal static void CreateFaceMesh(ProBuilderMesh mesh, Mesh target)
        {
            target.Clear();
            target.vertices = mesh.positionsInternal;
            target.triangles = mesh.selectedFacesInternal.SelectMany(x => x.indexes).ToArray();
        }

        internal static void CreateEdgeMesh(ProBuilderMesh mesh, Mesh target)
        {
            if (!BuiltinMaterials.geometryShadersSupported)
            {
                CreateEdgeBillboardMesh(mesh, target);
                return;
            }

            int edgeCount = 0;
            int faceCount = mesh.faceCount;

            for (int i = 0; i < faceCount; i++)
                edgeCount += mesh.facesInternal[i].edgesInternal.Length;

            int elementCount = System.Math.Min(edgeCount, ushort.MaxValue / 2 - 1);
            int[] tris = new int[elementCount * 2];

            int edgeIndex = 0;

            for (int i = 0; i < faceCount && edgeIndex < elementCount; i++)
            {
                for (int n = 0; n < mesh.facesInternal[i].edgesInternal.Length && edgeIndex < elementCount; n++)
                {
                    var edge = mesh.facesInternal[i].edgesInternal[n];

                    int positionIndex = edgeIndex * 2;

                    tris[positionIndex + 0] = edge.a;
                    tris[positionIndex + 1] = edge.b;

                    edgeIndex++;
                }
            }

            target.Clear();
            target.name = "ProBuilder::EdgeMesh" + target.GetInstanceID();
            target.vertices = mesh.positionsInternal;
            target.subMeshCount = 1;
            target.SetIndices(tris, MeshTopology.Lines, 0);
        }

        internal static void CreateEdgeMesh(ProBuilderMesh mesh, Mesh target, Edge[] edges)
        {
            if (!BuiltinMaterials.geometryShadersSupported)
            {
                CreateEdgeBillboardMesh(mesh, target, edges);
                return;
            }

            int edgeCount = System.Math.Min(edges.Length, ushort.MaxValue / 2 - 1);
            int[] indexes = new int[edgeCount * 2];

            for (int n = 0; n < edgeCount; n++)
            {
                var edge = edges[n];
                var ind = n * 2;

                indexes[ind + 0] = edge.a;
                indexes[ind + 1] = edge.b;
            }

            target.Clear();
            target.name = "ProBuilder::EdgeMesh" + target.GetInstanceID();
            target.vertices = mesh.positionsInternal;
            target.subMeshCount = 1;
            target.SetIndices(indexes, MeshTopology.Lines, 0);
        }

        /// <summary>
        /// Populate a renderable's mesh with a spattering of vertices representing both selected and not selected.
        /// </summary>
        internal static void CreateVertexMesh(ProBuilderMesh mesh, Mesh target)
        {
            s_VertexList.Clear();

            for (int i = 0, c = mesh.sharedVerticesInternal.Length; i < c; i++)
                s_VertexList.Add(mesh.sharedVerticesInternal[i][0]);

            CreateVertexMesh(mesh, target, s_VertexList);
        }

        internal static void CreateVertexMesh(ProBuilderMesh mesh, Mesh target, IList<int> indexes)
        {
            if (BuiltinMaterials.geometryShadersSupported)
                BuildVertexMeshInternal(mesh, target, indexes);
            else
                BuildVertexMeshLegacy(mesh, target, indexes);
        }

        internal static void BuildVertexMeshLegacy(ProBuilderMesh mesh, Mesh target, IList<int> indexes)
        {
            const ushort k_MaxPointCount = ushort.MaxValue / 4;

            int billboardCount = indexes.Count;

            if (billboardCount > k_MaxPointCount)
                billboardCount = k_MaxPointCount;

            var positions = mesh.positionsInternal;

            Vector3[] t_billboards = new Vector3[billboardCount * 4];
            Vector2[] t_uvs = new Vector2[billboardCount * 4];
            Vector2[] t_uv2 = new Vector2[billboardCount * 4];
            int[] t_tris = new int[billboardCount * 6];

            int n = 0;
            int t = 0;

            Vector3 up = Vector3.up;
            Vector3 right = Vector3.right;

            for (int i = 0; i < billboardCount; i++)
            {
                t_billboards[t + 0] = positions[indexes[i]];
                t_billboards[t + 1] = positions[indexes[i]];
                t_billboards[t + 2] = positions[indexes[i]];
                t_billboards[t + 3] = positions[indexes[i]];

                t_uvs[t + 0] = Vector3.zero;
                t_uvs[t + 1] = Vector3.right;
                t_uvs[t + 2] = Vector3.up;
                t_uvs[t + 3] = Vector3.one;

                t_uv2[t + 0] = -up - right;
                t_uv2[t + 1] = -up + right;
                t_uv2[t + 2] = up - right;
                t_uv2[t + 3] = up + right;

                t_tris[n + 0] = t + 0;
                t_tris[n + 1] = t + 1;
                t_tris[n + 2] = t + 2;
                t_tris[n + 3] = t + 1;
                t_tris[n + 4] = t + 3;
                t_tris[n + 5] = t + 2;

                t += 4;
                n += 6;
            }

            target.Clear();
            target.vertices = t_billboards;
            target.uv = t_uvs;
            target.uv2 = t_uv2;
            target.triangles = t_tris;
        }

        internal static void CreatePointBillboardMesh(List<Vector3> points, Mesh mesh)
        {
            const ushort k_MaxPointCount = ushort.MaxValue / 4;

            int billboardCount = points.Count;

            if (billboardCount > k_MaxPointCount)
                billboardCount = k_MaxPointCount;

            Vector3[] t_billboards = new Vector3[billboardCount * 4];
            Vector2[] t_uvs = new Vector2[billboardCount * 4];
            Vector2[] t_uv2 = new Vector2[billboardCount * 4];
            int[] t_tris = new int[billboardCount * 6];

            int n = 0;
            int t = 0;

            Vector3 up = Vector3.up;
            Vector3 right = Vector3.right;

            for (int i = 0; i < billboardCount; i++)
            {
                t_billboards[t + 0] = points[i];
                t_billboards[t + 1] = points[i];
                t_billboards[t + 2] = points[i];
                t_billboards[t + 3] = points[i];

                t_uvs[t + 0] = Vector3.zero;
                t_uvs[t + 1] = Vector3.right;
                t_uvs[t + 2] = Vector3.up;
                t_uvs[t + 3] = Vector3.one;

                t_uv2[t + 0] = -up - right;
                t_uv2[t + 1] = -up + right;
                t_uv2[t + 2] = up - right;
                t_uv2[t + 3] = up + right;

                t_tris[n + 0] = t + 0;
                t_tris[n + 1] = t + 1;
                t_tris[n + 2] = t + 2;
                t_tris[n + 3] = t + 1;
                t_tris[n + 4] = t + 3;
                t_tris[n + 5] = t + 2;

                t += 4;
                n += 6;
            }

            mesh.Clear();
            mesh.vertices = t_billboards;
            mesh.uv = t_uvs;
            mesh.uv2 = t_uv2;
            mesh.triangles = t_tris;
        }

        /// <summary>
        /// Draw a set of vertices.
        /// </summary>
        static void BuildVertexMeshInternal(ProBuilderMesh mesh, Mesh target, IEnumerable<int> indexes)
        {
            target.Clear();
            target.name = "pb_ElementGraphics::PointMesh";
            target.vertices = mesh.positionsInternal;
            target.subMeshCount = 1;
            target.SetIndices(indexes as int[] ?? indexes.ToArray(), MeshTopology.Points, 0);
        }

        internal static void CreateEdgeBillboardMesh(ProBuilderMesh mesh, Mesh target)
        {
            target.Clear();
            const ushort k_MaxPointCountUShort = ushort.MaxValue / 4;
            var lineCount = mesh.edgeCount;

            target.indexFormat = lineCount > k_MaxPointCountUShort
                ? Rendering.IndexFormat.UInt32
                : Rendering.IndexFormat.UInt16;

            var vertices = mesh.positionsInternal;

            Vector3[] positions = new Vector3[lineCount * 4];
            Color[] nextVertex = new Color[lineCount * 4];

            int[] quads = new int[lineCount * 4];
            int n = 0;

            foreach(var face in mesh.facesInternal)
            {
                foreach (var edge in face.edgesInternal)
                {
                    Vector3 a = vertices[edge.a], b = vertices[edge.b];
                    Vector3 c = b + (b - a);

                    positions[n + 0] = a;
                    positions[n + 1] = a;
                    positions[n + 2] = b;
                    positions[n + 3] = b;

                    nextVertex[n + 0] = new Color(b.x, b.y, b.z, 1f);
                    nextVertex[n + 1] = new Color(b.x, b.y, b.z, -1f);
                    nextVertex[n + 2] = new Color(c.x, c.y, c.z, 1f);
                    nextVertex[n + 3] = new Color(c.x, c.y, c.z, -1f);

                    quads[n + 0] = n + 0;
                    quads[n + 1] = n + 1;
                    quads[n + 2] = n + 3;
                    quads[n + 3] = n + 2;

                    n += 4;
                }
            }

            target.vertices = positions;
            target.colors = nextVertex;
            target.subMeshCount = 1;
            target.SetIndices(quads, MeshTopology.Quads, 0);
        }

        internal static void CreateEdgeBillboardMesh(ProBuilderMesh mesh, Mesh target, ICollection<Edge> edges)
        {
            target.Clear();

            const ushort k_MaxPointCountUShort = ushort.MaxValue / 4;

            var lineCount = edges.Count;

            target.indexFormat = lineCount > k_MaxPointCountUShort
                ? Rendering.IndexFormat.UInt32
                : Rendering.IndexFormat.UInt16;

            var vertices = mesh.positionsInternal;

            Vector3[] positions = new Vector3[lineCount * 4];
            Color[] nextVertex = new Color[lineCount * 4];

            int[] quads = new int[lineCount * 6];

            int n = 0;

            foreach (var edge in edges)
            {
                Vector3 a = vertices[edge.a], b = vertices[edge.b];
                Vector3 c = b + (b - a);

                positions[n + 0] = a;
                positions[n + 1] = a;
                positions[n + 2] = b;
                positions[n + 3] = b;

                nextVertex[n + 0] = new Color(b.x, b.y, b.z, 1f);
                nextVertex[n + 1] = new Color(b.x, b.y, b.z, -1f);
                nextVertex[n + 2] = new Color(c.x, c.y, c.z, 1f);
                nextVertex[n + 3] = new Color(c.x, c.y, c.z, -1f);

                quads[n + 0] = n + 0;
                quads[n + 1] = n + 1;
                quads[n + 2] = n + 3;
                quads[n + 3] = n + 2;

                n += 4;
            }

            target.vertices = positions;
            target.colors = nextVertex;
            target.subMeshCount = 1;
            target.SetIndices(quads, MeshTopology.Quads, 0);
        }
    }
}
