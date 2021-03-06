#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace MediaPortal.UI.SkinEngine.DirectX.Triangulate
{
  /// <summary>
  /// Represents a simple polygon of <see cref="CPoint2D"/> vertices and provides
  /// common polygon functions.
  /// </summary>
  public class CPolygon : List<CPoint2D>
  {
    public CPolygon(IEnumerable<PointF> points) : this(points.Select(point => new CPoint2D(point.X, point.Y)), false) { }

    /// <summary>
    /// Creates a new simple polygon with the vertices in the specified <paramref name="points"/> array.
    /// The polygon will reject consecutive points with a very small distance, i.e. it will skip such
    /// points from the input.
    /// The polygon will be closed automatically.
    /// </summary>
    /// <param name="points">The points to be used for the new polygon.</param>
    public CPolygon(IEnumerable<CPoint2D> points) : this(points, false) { }

    /// <summary>
    /// Creates a new simple polygon with the vertices in the specified <paramref name="points"/> array.
    /// </summary>
    /// <param name="points">The points to be used for the new polygon.</param>
    /// <param name="ignoreChecks">If set to <c>true</c>, the given points will be taken without modification.
    /// If set to <c>true</c>, the constructor will reject consecutive points with a very small distance,
    /// i.e. it will skip such points from the input. It will also be closed automatically.
    /// </param>
    protected CPolygon(IEnumerable<CPoint2D> points, bool ignoreChecks)
    {
      CPoint2D lastPoint = null;
      foreach (CPoint2D currentPoint in points)
      {
        if (!ignoreChecks && lastPoint != null && CPoint2D.SamePoints(lastPoint, currentPoint))
          continue;
        Add(currentPoint);
        lastPoint = currentPoint;
      }
      if (!ignoreChecks && CPoint2D.SamePoints(this[Count - 1], this[0]))
        RemoveAt(Count - 1);

      if (!ignoreChecks)
        CheckProperPolygon();
    }

    /// <summary>
    /// After modifications of the polygon, this method can be called to check if the polygon's invariants
    /// are still met.
    /// </summary>
    /// <remarks>
    /// Invariants which are checked:
    /// <list type="bullet">
    /// <item>The polygon has at least three vertices</item>
    /// <item>The polygon is a simple polygon</item>
    /// </list>
    /// </remarks>
    public void CheckProperPolygon()
    {
      if (Count < 3)
        throw new InvalidInputGeometryDataException("Polygon needs at least 3 vertices");
      for (int i = 0; i < Count; i++)
        if (LineSegmentIntersectsBorder(this[i], this[CorrectIndex(i + 1)]))
          throw new InvalidInputGeometryDataException("Polygon is self-intersecting");
    }

    /// <summary>
    /// Calculates this polygon's area.
    /// </summary>
    public float GetPolygonArea()
    {
      return Math.Abs(GetPolygonArea(this));
    }

    /// <summary>
    /// Checks whether the vertex with the given index is a concave point or a convex point.
    /// For this method, the polygon vertices need to be in counter clockwise direction. If the vertices
    /// are in clockwise order, the returned value must be inverted.
    /// </summary>
    public VertexType GetPolygonVertexType(int pointIndex)
    {
      CPoint2D pti = this[pointIndex];
      CPoint2D ptj = this[CorrectIndex(pointIndex - 1)];
      CPoint2D ptk = this[CorrectIndex(pointIndex + 1)];

      float fArea = GetPolygonArea(new CPoint2D[] { ptj, pti, ptk });

      return fArea > 0 ? VertexType.ConcavePoint : VertexType.ConvexPoint;
    }

    /// <summary>
    /// Checks whether this polygon is convex ore concave.
    /// </summary>
    public PolygonType GetPolygonType()
    {
      return GetPolygonType(this);
    }

    /// <summary>
    /// Returns the information whether the specified point <paramref name="p"/> is located inside this polygon.
    /// </summary>
    /// <param name="p">Point to check.</param>
    public bool IsPointInside(CPoint2D p)
    {
      int i, j;
      bool result = false;
      for (i = 0, j = Count - 1; i < Count; j = i++) {
        if ((((this[i].Y <= p.Y) && (p.Y < this[j].Y)) ||
             ((this[j].Y <= p.Y) && (p.Y < this[i].Y))) &&
            (p.X < (this[j].X - this[i].X) * (p.Y - this[i].Y) /
              (this[j].Y - this[i].Y) + this[i].X))
          result ^= true;
      }
      return result;
    }

    /// <summary>
    /// Returns the information whether the specified line segment intersects any border line of this polygon.
    /// </summary>
    /// <remarks>
    /// This method only checks intersections of the given line segment between the points <paramref name="vi"/>
    /// and <paramref name="vj"/>, not the entire line beyond those end points.
    /// See http://local.wasp.uwa.edu.au/~pbourke/geometry/lineline2d/
    /// </remarks>
    /// <param name="vi">First point of line to check.</param>
    /// <param name="vj">Second point of line to check.</param>
    public bool LineSegmentIntersectsBorder(CPoint2D vi, CPoint2D vj)
    {
      // Line between the vertices:
      float x1 = vi.X;
      float y1 = vi.Y;
      float x2 = vj.X;
      float y2 = vj.Y;

      // Check if there are no intersections with other polygon lines
      for (int i = 0; i < Count; i++)
      {
        int j = (i + 1) % Count;  // Index for the next point

        //CPolygon line:
        float x3 = this[i].X;
        float y3 = this[i].Y;
        float x4 = this[j].X;
        float y4 = this[j].Y;

        double de = (y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1);

        if (Math.Abs(de) < ConstantValue.SmallValue)  // Lines are parallel
          continue;

        double ua = ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / de;
        double ub = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3)) / de;

        if ((ua > ConstantValue.SmallValue) && (ua < 1 - ConstantValue.SmallValue) &&
            (ub > ConstantValue.SmallValue) && (ub < 1 - ConstantValue.SmallValue))
          // Intersection point found
          return true;
      }
      return false;
    }

    /// <summary>
    /// Returns the information whether the vertex with the given index is a principal vertex.
    /// </summary>
    /// <remarks>
    /// A vertex pi of a polygon P is a principal vertex if the diagonal pi-1, pi+1 intersects the boundary of P
    /// only at pi-1 and pi+1.
    /// See http://cgm.cs.mcgill.ca/~godfried/teaching/cg-projects/97/Ian/glossary.html
    /// </remarks>
    /// <param name="pointIndex">Index of the vertex to check.</param>
    public bool IsPrincipalVertex(int pointIndex)
    {
      return !LineSegmentIntersectsBorder(this[CorrectIndex(pointIndex - 1)], this[CorrectIndex(pointIndex + 1)]);
    }

    /// <summary>
    /// Returns the information whether the vertex with the given index is an ear of this polygon.
    /// </summary>
    /// <remarks>
    /// An ear is a principal vertex which is located inside the polygon.
    /// See http://cgm.cs.mcgill.ca/~godfried/teaching/cg-projects/97/Ian/glossary.html
    /// </remarks>
    /// <param name="pointIndex">Index of the vertex to check.</param>
    public bool IsEar(int pointIndex)
    {
      CPoint2D vi = this[CorrectIndex(pointIndex - 1)];
      CPoint2D vj = this[CorrectIndex(pointIndex + 1)];
      return IsPrincipalVertex(pointIndex) && IsPointInside(new CPoint2D((vi.X + vj.X) / 2f, (vi.Y + vj.Y) / 2f));
    }

    /// <summary>
    /// Cuts the first ear of this polygon which can be found and returns it.
    /// </summary>
    /// <remarks>
    /// Except for triangles every simple polygon has at least two non-overlapping ears.
    /// This method will cut the first ear from this polygon and return it. The ear will be removed from
    /// this polygon, so the remaining polygon will have one vertex less than before.
    /// See http://cgm.cs.mcgill.ca/~godfried/teaching/cg-projects/97/Ian/glossary.html
    /// </remarks>
    /// <returns>Polygon representing the ear which was cut, or <c>null</c>, if no ear could be cut.</returns>
    public CPolygon CutEar()
    {
      if (Count <= 3)
        return null;
      for (int i = 0; i < Count; i++)
      {
        if (IsEar(i))
        {
          CPoint2D pti = this[CorrectIndex(i - 1)];
          CPoint2D ptj = this[i];
          CPoint2D ptk = this[CorrectIndex(i + 1)];
          CPolygon ear = new CPolygon(new CPoint2D[] {pti, ptj, ptk}, true);
          RemoveAt(i);
          return ear;
        }
      }
      return null;
    }

    /// <summary>
    /// Reverses the polygon vertices to a different direction (clockwise &lt;--&gt; counter clockwise).
    /// </summary>
    public void ReverseVerticesDirection()
    {
      RevertPointsDirection(this);
    }

    /// <summary>
    /// Checks if the vertices of this polygon are ordered clockwise or counter clockwise.
    /// </summary>
    public PolygonDirection GetVerticesDirection()
    {
      return GetPointsDirection(this);
    }

    /// <summary>
    /// Triangulates this polygon to an enumeration of triangle polygons.
    /// This polygon will not be changed by this method.
    /// </summary>
    public IEnumerable<CPolygon> Triangulate()
    {
      CPolygon remainingPolygon = new CPolygon(this, true);

      while (remainingPolygon.Count > 3)
      {
        CPolygon p = remainingPolygon.CutEar();
        if (p == null)
        {
          ServiceRegistration.Get<ILogger>().Warn("Serious problem while triangulating polygon '{0}'", p);
          yield break;
        }
        yield return p;
      }
      // Add remaining polynom, which is convex
      yield return remainingPolygon;
    }

    /// <summary>
    /// Corrects the given <paramref name="pointIndex"/> to be greater or equal to <c>0</c> and lower than
    /// <see cref="List{T}.Count"/>.
    /// </summary>
    public int CorrectIndex(int pointIndex)
    {
      int result = pointIndex % Count;
      return result < 0 ? result + Count : result;
    }

    /// <summary>
    /// Calculates the area of the polygon made by the given <paramref name="points"/>.
    /// </summary>
    /// <remarks>
    /// Restriction: The polygon is not self intersecting.
    /// See http://local.wasp.uwa.edu.au/~pbourke/geometry/polyarea/
    /// </remarks>
    /// <returns>Area of the specified polygon. Polygons with different point directions will produce a result
    /// with a different sign:
    /// If area &gt; 0: points are in clockwise order.
    /// If area &lt; 0: points are in counter clockwise order.
    /// </returns>
    public static float GetPolygonArea(IList<CPoint2D> points)
    {
      float fArea = 0;
      int nNumOfPts = points.Count;

      for (int i = 0; i < nNumOfPts; i++)
      {
        int j = (i + 1) % nNumOfPts;
        fArea += points[i].X * points[j].Y;
        fArea -= (points[i].Y * points[j].X);
      }

      fArea = fArea / 2;
      return fArea;
    }

    /// <summary>
    /// Checks if the vertices of the polygon with the given <paramref name="points"/> are ordered
    /// in clockwise or counter clockwise direction.
    /// </summary>
    /// <remarks>
    /// Restriction: the polygon is not self intersecting.
    /// See http://local.wasp.uwa.edu.au/~pbourke/geometry/clockwise/index.html
    /// </remarks>
    public static PolygonDirection GetPointsDirection(IList<CPoint2D> points)
    {
      int nCount = 0;
      int nVertices = points.Count;

      if (nVertices < 3)
        return PolygonDirection.Clockwise;

      for (int i = 0; i < nVertices; i++)
      {
        int j = (i + 1) % nVertices;
        int k = (i + 2) % nVertices;

        float crossProduct = GetCrossProduct(points[i], points[j], points[k]);

        if (crossProduct > 0)
          nCount++;
        else
          nCount--;
      }

      return nCount < 0 ? PolygonDirection.Counter_Clockwise : PolygonDirection.Clockwise;
    }

    /// <summary>
    /// Reverts the given <paramref name="points"/> from clockwise order to counter clockwise order and
    /// vice-versa.
    /// </summary>
    public static void RevertPointsDirection(IList<CPoint2D> points)
    {
      int nVertices = points.Count;
      for (int i = 0; i < nVertices / 2; i++)
      {
        CPoint2D temp = points[i];
        points[i] = points[nVertices - 1 - i];
        points[nVertices - 1 - i] = temp;
      }
    }

    /// <summary>
    /// Checks whether the given <paramref name="points"/> make a convex polygon or a concave polygon.
    /// </summary>
    /// <remarks>
    /// Restriction: the polygon is not self intersecting
    /// See http://local.wasp.uwa.edu.au/~pbourke/geometry/clockwise/index.html
    /// </remarks>
    public PolygonType GetPolygonType(IList<CPoint2D> points)
    {
      int numPoints = points.Count;
      int numPos = 0;
      int numNeg = 0;
      for (int i = 0; i < numPoints; i++)
      {
        int j = (i + 1) % numPoints;
        int k = (i + 2) % numPoints;

        float crossProduct = GetCrossProduct(points[i], points[j], points[k]);

        if (crossProduct > 0)
          numPos++;
        else if (crossProduct < 0)
          numNeg++;

        if (numPos > 0 && numNeg > 0)
          // Concave polygons have a mixture of cross product signs
          return PolygonType.Concave;
      }
      // Convex polygons have all the same cross product signs
      return PolygonType.Convex;
    }

    /// <summary>
    /// Returns the cross product of the vectors between three consecutive vertices.
    /// </summary>
    /// <param name="vi">First vertex.</param>
    /// <param name="vj">Second vertex.</param>
    /// <param name="vk">Third vertex.</param>
    /// <returns>Cross product: (vj - vi) x (vk - vj)</returns>
    public static float GetCrossProduct(CPoint2D vi, CPoint2D vj, CPoint2D vk)
    {
      return (vj.X - vi.X) * (vk.Y - vj.Y) - (vj.Y - vi.Y) * (vk.X - vj.X);
    }
  }
}
