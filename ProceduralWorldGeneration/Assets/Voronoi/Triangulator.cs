using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// https://www.flipcode.com/archives/Efficient_Polygon_Triangulation.shtml
/// </summary>
public class Triangulator2
{
	private const float EPSILON = 0.0000000001f;


	public static bool Process(List<Vector3> contour, out int[] result)
	{
		result = new int[contour.Count * 3];
		/* allocate and initialize list of Vertices in polygon */
		int n = contour.Count;
		if (n < 3)
			return false;

		int[] V = new int[n];

		/* we want a counter-clockwise polygon in V */

		if (0.0f < Area(contour))
			for (int v = 0; v < n; v++)
				V[v] = v;
		else
			for (int v = 0; v < n; v++)
				V[v] = (n - 1) - v;

		int nv = n;

		/*  remove nv-2 Vertices, creating 1 triangle every time */
		int count = 2 * nv;   /* error detection */

		int index = 0;
		for (int m = 0, v = nv - 1; nv > 2;)
		{
			/* if we loop, it is probably a non-simple polygon */
			if (0 >= (count--))
			{
				//** Triangulate: ERROR - probable bad polygon!
				Debug.Log("Triangulate: ERROR - probable bad polygon!");
				return false;
			}

			/* three consecutive vertices in current polygon, <u,v,w> */
			int u = v;
			if (nv <= u)
				u = 0;     /* previous */
			v = u + 1;
			if (nv <= v)
				v = 0;     /* new v    */
			int w = v + 1;
			if (nv <= w)
				w = 0;     /* next     */

			if (Snip(contour, u, v, w, nv, V))
			{
				int a, b, c, s, t;

				/* true names of the vertices */
				a = V[u];
				b = V[v];
				c = V[w];

				/* output Triangle */
				result[index++] = a;
				result[index++] = b;
				result[index++] = c;

				m++;

				/* remove v from remaining polygon */
				for (s = v, t = v + 1; t < nv; s++, t++)
					V[s] = V[t];
				nv--;

				/* resest error detection counter */
				count = 2 * nv;
			}
		}

		return true;
	}

	private static float Area(List<Vector3> contour)
	{
		int n = contour.Count;
		float a = 0.0f;
		for (int p = n - 1, q = 0; q < n; p = q++)
		{
			a += contour[p].x * contour[q].y - contour[q].x * contour[p].y;
		}

		return a * .5f;
	}

	private static bool InsideTriangle(
		float Ax, float Ay, float Bx, float By,
		float Cx, float Cy, float Px, float Py)
	{
		float ax, ay, bx, by, cx, cy, apx, apy, bpx, bpy, cpx, cpy;
		float cCROSSap, bCROSScp, aCROSSbp;

		ax = Cx - Bx;
		ay = Cy - By;
		bx = Ax - Cx;
		by = Ay - Cy;
		cx = Bx - Ax;
		cy = By - Ay;
		apx = Px - Ax;
		apy = Py - Ay;
		bpx = Px - Bx;
		bpy = Py - By;
		cpx = Px - Cx;
		cpy = Py - Cy;

		aCROSSbp = ax * bpy - ay * bpx;
		cCROSSap = cx * apy - cy * apx;
		bCROSScp = bx * cpy - by * cpx;

		return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
	}


	private static bool Snip(List<Vector3> contour, int u, int v, int w, int n, int[] V)
	{
		int p;
		float Ax, Ay, Bx, By, Cx, Cy, Px, Py;

		Ax = contour[V[u]].x;
		Ay = contour[V[u]].y;

		Bx = contour[V[v]].x;
		By = contour[V[v]].y;

		Cx = contour[V[w]].x;
		Cy = contour[V[w]].y;

		if (EPSILON > (((Bx - Ax) * (Cy - Ay)) - ((By - Ay) * (Cx - Ax))))
			return false;

		for (p = 0; p < n; p++)
		{
			if ((p == u) || (p == v) || (p == w))
				continue;
			Px = contour[V[p]].x;
			Py = contour[V[p]].y;
			if (InsideTriangle(Ax, Ay, Bx, By, Cx, Cy, Px, Py))
				return false;
		}

		return true;
	}
}


/// <summary>
/// http://wiki.unity3d.com/index.php?title=Triangulator
/// </summary>
public class Triangulator
{
	private List<Vector3> m_points = new List<Vector3>();

	public Triangulator(Vector3[] points)
	{
		m_points = new List<Vector3>(points);
	}

	public int[] Triangulate()
	{
		List<int> indices = new List<int>();

		int n = m_points.Count;
		if (n < 3)
			return indices.ToArray();

		int[] V = new int[n];
		if (Area() > 0)
		{
			for (int v = 0; v < n; v++)
				V[v] = v;
		}
		else
		{
			for (int v = 0; v < n; v++)
				V[v] = (n - 1) - v;
		}

		int nv = n;
		int count = 2 * nv;
		for (int v = nv - 1; nv > 2;)
		{
			if ((count--) <= 0)
				return indices.ToArray();

			int u = v;
			if (nv <= u)
				u = 0;
			v = u + 1;
			if (nv <= v)
				v = 0;
			int w = v + 1;
			if (nv <= w)
				w = 0;

			if (Snip(u, v, w, nv, V))
			{
				int a, b, c, s, t;
				a = V[u];
				b = V[v];
				c = V[w];
				indices.Add(a);
				indices.Add(b);
				indices.Add(c);
				for (s = v, t = v + 1; t < nv; s++, t++)
					V[s] = V[t];
				nv--;
				count = 2 * nv;
			}
		}

		indices.Reverse();
		return indices.ToArray();
	}

	private float Area()
	{
		int n = m_points.Count;
		float A = 0.0f;
		for (int p = n - 1, q = 0; q < n; p = q++)
		{
			Vector2 pval = m_points[p];
			Vector2 qval = m_points[q];
			A += pval.x * qval.y - qval.x * pval.y;
		}
		return (A * 0.5f);
	}

	private bool Snip(int u, int v, int w, int n, int[] V)
	{
		int p;
		Vector2 A = m_points[V[u]];
		Vector2 B = m_points[V[v]];
		Vector2 C = m_points[V[w]];
		if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x))))
			return false;
		for (p = 0; p < n; p++)
		{
			if ((p == u) || (p == v) || (p == w))
				continue;
			Vector2 P = m_points[V[p]];
			if (InsideTriangle(A, B, C, P))
				return false;
		}
		return true;
	}

	private bool InsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
	{
		float ax, ay, bx, by, cx, cy, apx, apy, bpx, bpy, cpx, cpy;
		float cCROSSap, bCROSScp, aCROSSbp;

		ax = C.x - B.x;
		ay = C.y - B.y;
		bx = A.x - C.x;
		by = A.y - C.y;
		cx = B.x - A.x;
		cy = B.y - A.y;
		apx = P.x - A.x;
		apy = P.y - A.y;
		bpx = P.x - B.x;
		bpy = P.y - B.y;
		cpx = P.x - C.x;
		cpy = P.y - C.y;

		aCROSSbp = ax * bpy - ay * bpx;
		cCROSSap = cx * apy - cy * apx;
		bCROSScp = bx * cpy - by * cpx;

		return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
	}
}