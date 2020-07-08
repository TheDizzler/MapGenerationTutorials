using System.Collections.Generic;
using UnityEngine;

namespace AtomosZ.Tutorials.CellAuto
{
	public class MeshGenerator : MonoBehaviour
	{
		public SquareGrid squareGrid;
		public MeshFilter walls3D;
		public MeshCollider wallCollider3D;
		public MeshFilter cave;
		public MeshFilter ground;

		public float wallHeight = 5;
		public bool is2D;

		List<Vector3> vertices;
		List<int> triangles;
		Dictionary<int, List<Triangle>> triangleDict = new Dictionary<int, List<Triangle>>();
		private List<List<int>> outlines = new List<List<int>>();
		private HashSet<int> checkedVertices = new HashSet<int>();
		private int mapWidth;
		private int mapHeight;


		public void GenerateMesh(int[,] map, float squareSize)
		{
			outlines.Clear();
			checkedVertices.Clear();
			triangleDict.Clear();

			EdgeCollider2D[] currentColliders = gameObject.GetComponents<EdgeCollider2D>();
			for (int i = 0; i < currentColliders.Length; ++i)
			{
				if (Application.isPlaying)
					Destroy(currentColliders[i]);
				else
					DestroyImmediate(currentColliders[i]);
			}

			mapWidth = map.GetLength(0);
			mapHeight = map.GetLength(1);

			squareGrid = new SquareGrid(map, squareSize);

			vertices = new List<Vector3>();
			triangles = new List<int>();

			for (int x = 0; x < squareGrid.squares.GetLength(0); ++x)
			{
				for (int y = 0; y < squareGrid.squares.GetLength(1); ++y)
				{
					TriangulateSquare(squareGrid.squares[x, y]);
				}
			}

			Mesh mapMesh = new Mesh();
			cave.mesh = mapMesh;
			mapMesh.vertices = vertices.ToArray();
			mapMesh.triangles = triangles.ToArray();
			mapMesh.RecalculateNormals();

			Vector2[] uvs = new Vector2[vertices.Count];
			for (int i = 0; i < vertices.Count; ++i)
			{
				float percentX = Mathf.InverseLerp(-mapWidth * squareSize * .5f, mapWidth * squareSize * .5f, vertices[i].x);
				float percentY = Mathf.InverseLerp(-mapHeight * squareSize * .5f, mapHeight * squareSize * .5f, vertices[i].z);
				uvs[i] = new Vector2(percentX, percentY);

			}

			mapMesh.uv = uvs;

			if (is2D)
			{
				cave.transform.eulerAngles = new Vector3(270, 0, 0);
				ground.transform.eulerAngles = new Vector3(270, 0, 0);
				ground.transform.localPosition = new Vector3(0, 0, wallHeight);
				ground.transform.localScale = new Vector3(mapWidth * .1f, 0, mapHeight * .1f);
				walls3D.gameObject.SetActive(false);

				Generate2DColliders();
			}
			else
			{
				cave.transform.eulerAngles = new Vector3(0, 0, 0);
				ground.transform.eulerAngles = new Vector3(0, 0, 0);
				ground.transform.localPosition = new Vector3(0, -wallHeight, 0);
				ground.transform.localScale = new Vector3(mapWidth * .1f, 0, mapHeight * .1f);

				walls3D.gameObject.SetActive(true);

				CreateWallMesh();
			}
		}

		private void Generate2DColliders()
		{
			CalculateMeshOutlines();

			foreach (List<int> outline in outlines)
			{
				EdgeCollider2D edgeCollider = gameObject.AddComponent<EdgeCollider2D>();
				Vector2[] edgePoints = new Vector2[outline.Count];
				for (int i = 0; i < outline.Count; ++i)
				{
					edgePoints[i] = new Vector2(vertices[outline[i]].x, vertices[outline[i]].z);
				}

				edgeCollider.points = edgePoints;
			}
		}

		private void CreateWallMesh()
		{
			CalculateMeshOutlines();

			List<Vector3> wallVertices = new List<Vector3>();
			List<int> wallTriangles = new List<int>();
			Mesh wallMesh = new Mesh();

			foreach (List<int> outline in outlines)
			{
				for (int i = 0; i < outline.Count - 1; ++i)
				{
					int startIndex = wallVertices.Count;
					wallVertices.Add(vertices[outline[i]]); // left vertex
					wallVertices.Add(vertices[outline[i + 1]]); // right vertex
					wallVertices.Add(vertices[outline[i]] - Vector3.up * wallHeight); // bottom left vertex
					wallVertices.Add(vertices[outline[i + 1]] - Vector3.up * wallHeight); // bottom right vertex

					wallTriangles.Add(startIndex + 0);
					wallTriangles.Add(startIndex + 2);
					wallTriangles.Add(startIndex + 3);

					wallTriangles.Add(startIndex + 3);
					wallTriangles.Add(startIndex + 1);
					wallTriangles.Add(startIndex + 0);

				}
			}

			wallMesh.vertices = wallVertices.ToArray();
			wallMesh.triangles = wallTriangles.ToArray();
			walls3D.mesh = wallMesh;

			wallCollider3D.sharedMesh = wallMesh;
		}

		private void TriangulateSquare(Square square)
		{
			switch (square.configuration)
			{
				case 0:
					break;

				// 1 points
				case 1:
					MeshFromPoints(square.centerLeft, square.centerBottom, square.bottomLeft);
					break;
				case 2:
					MeshFromPoints(square.bottomRight, square.centerBottom, square.centerRight);
					break;
				case 4:
					MeshFromPoints(square.topRight, square.centerRight, square.centerTop);
					break;
				case 8:
					MeshFromPoints(square.topLeft, square.centerTop, square.centerLeft);
					break;

				// 2 points
				case 3:
					MeshFromPoints(square.centerRight, square.bottomRight, square.bottomLeft, square.centerLeft);
					break;
				case 6:
					MeshFromPoints(square.centerTop, square.topRight, square.bottomRight, square.centerBottom);
					break;
				case 9:
					MeshFromPoints(square.topLeft, square.centerTop, square.centerBottom, square.bottomLeft);
					break;
				case 12:
					MeshFromPoints(square.topLeft, square.topRight, square.centerRight, square.centerLeft);
					break;
				case 5:
					MeshFromPoints(square.centerTop, square.topRight, square.centerRight, square.centerBottom, square.bottomLeft, square.centerLeft);
					break;
				case 10:
					MeshFromPoints(square.topLeft, square.centerTop, square.centerRight, square.bottomRight, square.centerBottom, square.centerLeft);
					break;

				// 3 points
				case 7:
					MeshFromPoints(square.centerTop, square.topRight, square.bottomRight, square.bottomLeft, square.centerLeft);
					break;
				case 11:
					MeshFromPoints(square.topLeft, square.centerTop, square.centerRight, square.bottomRight, square.bottomLeft);
					break;
				case 13:
					MeshFromPoints(square.topLeft, square.topRight, square.centerRight, square.centerBottom, square.bottomLeft);
					break;
				case 14:
					MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.centerBottom, square.centerLeft);
					break;

				// 4 point
				case 15:
					MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.bottomLeft);
					checkedVertices.Add(square.topLeft.vertexIndex);
					checkedVertices.Add(square.topRight.vertexIndex);
					checkedVertices.Add(square.bottomRight.vertexIndex);
					checkedVertices.Add(square.bottomLeft.vertexIndex);
					break;
			}
		}

		private void MeshFromPoints(params Node[] points)
		{
			AssignVertices(points);

			if (points.Length >= 3)
				CreateTriangle(points[0], points[1], points[2]);
			if (points.Length >= 4)
				CreateTriangle(points[0], points[2], points[3]);
			if (points.Length >= 5)
				CreateTriangle(points[0], points[3], points[4]);
			if (points.Length >= 6)
				CreateTriangle(points[0], points[4], points[5]);
		}

		private void AssignVertices(Node[] points)
		{
			for (int i = 0; i < points.Length; ++i)
			{
				if (points[i].vertexIndex == -1)
				{
					points[i].vertexIndex = vertices.Count;
					vertices.Add(points[i].position);
				}
			}
		}

		private void CreateTriangle(Node a, Node b, Node c)
		{
			triangles.Add(a.vertexIndex);
			triangles.Add(b.vertexIndex);
			triangles.Add(c.vertexIndex);

			Triangle triangle = new Triangle(a.vertexIndex, b.vertexIndex, c.vertexIndex);
			AddTriangleToDictionary(triangle.vertexIndexA, triangle);
			AddTriangleToDictionary(triangle.vertexIndexB, triangle);
			AddTriangleToDictionary(triangle.vertexIndexC, triangle);
		}

		private void AddTriangleToDictionary(int vertexIndexKey, Triangle triangle)
		{
			if (triangleDict.ContainsKey(vertexIndexKey))
				triangleDict[vertexIndexKey].Add(triangle);
			else
			{
				List<Triangle> triangles = new List<Triangle>();
				triangles.Add(triangle);
				triangleDict.Add(vertexIndexKey, triangles);
			}
		}

		private void CalculateMeshOutlines()
		{
			for (int vertexIndex = 0; vertexIndex < vertices.Count; ++vertexIndex)
			{
				if (!checkedVertices.Contains(vertexIndex))
				{
					int newOutlineVertex = GetConnectedOutlineVertex(vertexIndex);
					if (newOutlineVertex != -1)
					{
						checkedVertices.Add(vertexIndex);
						List<int> newOutline = new List<int>();
						newOutline.Add(vertexIndex);
						outlines.Add(newOutline);
						FollowOutline(newOutlineVertex, outlines.Count - 1);
						outlines[outlines.Count - 1].Add(vertexIndex);
					}
				}
			}
		}

		private void FollowOutline(int vertexIndex, int outlineIndex)
		{
			outlines[outlineIndex].Add(vertexIndex);
			checkedVertices.Add(vertexIndex);
			int nextVertexIndex = GetConnectedOutlineVertex(vertexIndex);
			if (nextVertexIndex != -1)
			{
				FollowOutline(nextVertexIndex, outlineIndex);
			}
		}

		private int GetConnectedOutlineVertex(int vertexIndex)
		{
			List<Triangle> trianglesContainingVertex = triangleDict[vertexIndex];

			for (int i = 0; i < trianglesContainingVertex.Count; ++i)
			{
				Triangle triangle = trianglesContainingVertex[i];

				for (int j = 0; j < 3; ++j)
				{
					int vertexB = triangle[j];
					if (vertexB != vertexIndex && !checkedVertices.Contains(vertexB))
						if (IsOutlineEdge(vertexIndex, vertexB))
							return vertexB;
				}
			}

			return -1;
		}


		private bool IsOutlineEdge(int vertexA, int vertexB)
		{
			List<Triangle> trianglesContainingVertexA = triangleDict[vertexA];
			int sharedTriangleCount = 0;

			for (int i = 0; i < trianglesContainingVertexA.Count; ++i)
			{
				if (trianglesContainingVertexA[i].Contains(vertexB))
				{
					if (++sharedTriangleCount > 1)
						break;
				}
			}

			return sharedTriangleCount == 1;
		}


		struct Triangle
		{
			public int vertexIndexA;
			public int vertexIndexB;
			public int vertexIndexC;
			int[] vertices;

			public Triangle(int a, int b, int c)
			{
				this.vertexIndexA = a;
				this.vertexIndexB = b;
				this.vertexIndexC = c;

				vertices = new int[3];
				vertices[0] = a;
				vertices[1] = b;
				vertices[2] = c;
			}

			public int this[int i]
			{
				get { return vertices[i]; }
			}

			public bool Contains(int vertexIndex)
			{
				return vertexIndex == vertexIndexA
					|| vertexIndex == vertexIndexB
					|| vertexIndex == vertexIndexC;
			}
		}


		//void OnDrawGizmos()
		//{
		//	if (squareGrid != null)
		//	{
		//		for (int x = 0; x < squareGrid.squares.GetLength(0); ++x)
		//		{
		//			for (int y = 0; y < squareGrid.squares.GetLength(1); ++y)
		//			{
		//				Gizmos.color = squareGrid.squares[x, y].topLeft.active ? Color.black : Color.white;
		//				Gizmos.DrawCube(squareGrid.squares[x, y].topLeft.position, Vector3.one * .4f);

		//				Gizmos.color = squareGrid.squares[x, y].topRight.active ? Color.black : Color.white;
		//				Gizmos.DrawCube(squareGrid.squares[x, y].topRight.position, Vector3.one * .4f);

		//				Gizmos.color = squareGrid.squares[x, y].bottomRight.active ? Color.black : Color.white;
		//				Gizmos.DrawCube(squareGrid.squares[x, y].bottomRight.position, Vector3.one * .4f);

		//				Gizmos.color = squareGrid.squares[x, y].bottomLeft.active ? Color.black : Color.white;
		//				Gizmos.DrawCube(squareGrid.squares[x, y].bottomRight.position, Vector3.one * .4f);

		//				Gizmos.color = Color.grey;
		//				Gizmos.DrawCube(squareGrid.squares[x, y].centerTop.position, Vector3.one * .15f);
		//				Gizmos.DrawCube(squareGrid.squares[x, y].centerRight.position, Vector3.one * .15f);
		//				Gizmos.DrawCube(squareGrid.squares[x, y].centerBottom.position, Vector3.one * .15f);
		//				Gizmos.DrawCube(squareGrid.squares[x, y].centerLeft.position, Vector3.one * .15f);
		//			}
		//		}
		//	}
		//}

		public class SquareGrid
		{
			public Square[,] squares;
			public SquareGrid(int[,] map, float squareSize)
			{
				int nodeCountX = map.GetLength(0);
				int nodeCountY = map.GetLength(1);
				float mapWidth = nodeCountX * squareSize;
				float mapHeight = nodeCountY * squareSize;

				ControlNode[,] controlNodes = new ControlNode[nodeCountX, nodeCountY];
				for (int x = 0; x < nodeCountX; ++x)
				{
					for (int y = 0; y < nodeCountY; ++y)
					{
						Vector3 pos = new Vector3(
							-mapWidth * .5f + x * squareSize + squareSize * .5f,
							0,
							-mapHeight * .5f + y * squareSize + squareSize * .5f);
						controlNodes[x, y] = new ControlNode(pos, map[x, y] == 1, squareSize);
					}
				}

				squares = new Square[nodeCountX - 1, nodeCountY - 1];

				for (int x = 0; x < nodeCountX - 1; ++x)
				{
					for (int y = 0; y < nodeCountY - 1; ++y)
					{
						squares[x, y] = new Square(controlNodes[x, y + 1], controlNodes[x + 1, y + 1], controlNodes[x + 1, y], controlNodes[x, y]);
					}
				}
			}
		}


		public class Square
		{
			public ControlNode topLeft, topRight, bottomRight, bottomLeft;
			public Node centerTop, centerRight, centerBottom, centerLeft;
			public int configuration;


			public Square(ControlNode topLeft, ControlNode topRight, ControlNode bottomRight, ControlNode bottomleft)
			{
				this.topLeft = topLeft;
				this.topRight = topRight;
				this.bottomRight = bottomRight;
				this.bottomLeft = bottomleft;

				centerTop = topLeft.right;
				centerRight = bottomRight.above;
				centerBottom = bottomLeft.right;
				centerLeft = bottomLeft.above;

				if (topLeft.active)
					configuration += 8;
				if (topRight.active)
					configuration += 4;
				if (bottomRight.active)
					configuration += 2;
				if (bottomLeft.active)
					configuration += 1;
			}
		}

		public class Node
		{
			public Vector3 position;
			public int vertexIndex = -1;

			public Node(Vector3 pos)
			{
				position = pos;
			}
		}

		public class ControlNode : Node
		{
			public bool active;
			public Node above, right;

			public ControlNode(Vector3 pos, bool active, float squareSize) : base(pos)
			{
				this.active = active;
				this.above = new Node(pos + Vector3.forward * squareSize * .5f);
				this.right = new Node(pos + Vector3.right * squareSize * .5f);
			}
		}
	}
}