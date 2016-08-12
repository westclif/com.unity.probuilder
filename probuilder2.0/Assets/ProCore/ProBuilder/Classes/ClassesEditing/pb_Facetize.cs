using UnityEngine;
using ProBuilder2.Common;
using System.Collections.Generic;

// @todo removes
using System.Linq;

namespace ProBuilder2.MeshOperations
{
	/**
	 *	Functions for decomposing faces to their base triangles.
	 */
	public static class pb_Facetize
	{
		/**
		 *	Break down an object (or some subset of its faces) to triangles.
		 */
		public static pb_ActionResult Facetize(this pb_Object pb, IList<pb_Face> faces, out pb_Face[] newFaces)
		{
			List<pb_Vertex> vertices = new List<pb_Vertex>( pb_Vertex.GetVertices(pb) );
			Dictionary<int, int> lookup = pb.sharedIndices.ToDictionary();

			List<pb_FaceRebuildData> rebuild = new List<pb_FaceRebuildData>();

			foreach(pb_Face face in faces)
			{
				List<pb_FaceRebuildData> res = BreakFaceIntoTris(face, vertices, lookup);
				rebuild.AddRange(res);
			}

			pb_FaceRebuildData.Apply(rebuild, pb, vertices, null, lookup, null);
			pb.DeleteFaces(faces);
			pb.ToMesh();

			newFaces = rebuild.Select(x => x.face).ToArray();

			return new pb_ActionResult(Status.Success, string.Format("Triangulated {0} {1}", faces.Count, faces.Count < 2 ? "Face" : "Faces"));
		}

		private static List<pb_FaceRebuildData> BreakFaceIntoTris(pb_Face face, List<pb_Vertex> vertices, Dictionary<int, int> lookup)
		{
			int[] tris = face.indices;
			int triCount = tris.Length;
			List<pb_FaceRebuildData> rebuild = new List<pb_FaceRebuildData>(triCount / 3);

			for(int i = 0; i < triCount; i += 3)
			{
				pb_FaceRebuildData r = new pb_FaceRebuildData();

				r.face = new pb_Face(face);
				r.face.SetIndices( new int[] { 0, 1, 2} );

				r.vertices = new List<pb_Vertex>() {
					vertices[tris[i  ]],
					vertices[tris[i+1]],
					vertices[tris[i+2]]
				};

				r.sharedIndices = new List<int>() {
					lookup[tris[i  ]],
					lookup[tris[i+1]],
					lookup[tris[i+2]]
				};

				rebuild.Add(r);
			}

			return rebuild;
		}
	}
}