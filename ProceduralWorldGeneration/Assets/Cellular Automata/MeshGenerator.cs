﻿using UnityEngine;

public class MeshGenerator : MonoBehaviour
{
	public SquareGrid squareGrid;

	public void GenerateMesh(int[,] map, float squareSize)
	{
		squareGrid = new SquareGrid(map, squareSize);
	}


	void OnDrawGizmos()
	{
		if (squareGrid != null)
		{
			for (int x = 0; x < squareGrid.squares.GetLength(0); ++x)
			{
				for (int y = 0; y < squareGrid.squares.GetLength(1); ++y)
				{
					Gizmos.color = squareGrid.squares[x, y].topLeft.active ? Color.black : Color.white;
					Gizmos.DrawCube(squareGrid.squares[x, y].topLeft.position, Vector3.one * .4f);

					Gizmos.color = squareGrid.squares[x, y].topRight.active ? Color.black : Color.white;
					Gizmos.DrawCube(squareGrid.squares[x, y].topRight.position, Vector3.one * .4f);

					Gizmos.color = squareGrid.squares[x, y].bottomRight.active ? Color.black : Color.white;
					Gizmos.DrawCube(squareGrid.squares[x, y].bottomRight.position, Vector3.one * .4f);

					Gizmos.color = squareGrid.squares[x, y].bottomLeft.active ? Color.black : Color.white;
					Gizmos.DrawCube(squareGrid.squares[x, y].bottomRight.position, Vector3.one * .4f);

					Gizmos.color = Color.grey;
					Gizmos.DrawCube(squareGrid.squares[x, y].centerTop.position, Vector3.one * .15f);
					Gizmos.DrawCube(squareGrid.squares[x, y].centerRight.position, Vector3.one * .15f);
					Gizmos.DrawCube(squareGrid.squares[x, y].centerBottom.position, Vector3.one * .15f);
					Gizmos.DrawCube(squareGrid.squares[x, y].centerLeft.position, Vector3.one * .15f);
				}
			}
		}
	}

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
