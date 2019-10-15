using System;
using System.Collections.Generic;

namespace BatchProcess
{
    public class clsSketch
    {

        List<clsPoint> myPoints = new List<clsPoint>();
        public List<clsPoint> Points
        {
            get
            {
                return myPoints;
            }
        }

        public clsPoint Point(int n)
        {
            if (n < 0 | n > myPoints.Count - 1)
            {
                return null;
            }
            else
            {
                return myPoints[n].Copy();
            }
        }

        public clsPoint PointNoCopy(int n)
        {
            if (n < 0 | n > myPoints.Count - 1)
            {
                return null;
            }
            else
            {
                return myPoints[n];
            }
        }

        public int NumPoints
        {
            get { return myPoints.Count - 1; }
        }

        public clsLine Segment(int i)
        {
            if (i < 0 | i > myPoints.Count - 1)
                return null;
            if (i < myPoints.Count - 1)
            {
                return new clsLine(Point(i), Point(i + 1));
            }
            else
            {
                return new clsLine(Point(i), Point(0));
            }
        }


        public clsSketch()
        {
        }

        public clsSketch(double[] p)
        {
            int i;

            for (i = 0; i <= p.Length - 2; i += 2)
            {
                myPoints.Add(new clsPoint(p[i], p[i + 1]));
            }
        }

        //Public Sub New(ByVal p() As clsPoint)
        //    Dim i As Integer

        //    For i = 0 To UBound(p)
        //        myPoints.Add(p(i).Copy)
        //    Next
        //End Sub

        public clsSketch(List<clsPoint> p)
        {
            int i;

            for (i = 0; i <= p.Count - 1; i++)
            {
                myPoints.Add(p[i].Copy());
            }
        }

        public clsSketch Copy()
        {
            clsSketch aSketch = new clsSketch();
            int i;

            aSketch = new clsSketch();
            for (i = 0; i <= myPoints.Count - 1; i++)
            {
                aSketch.AddPoint(myPoints[i]);
            }
            return aSketch;
        }

        public void Clear()
        {
            myPoints.Clear();
        }

        public void RemovePoint(int i)
        {
            if (i < 0 | i > myPoints.Count - 1)
                return;
            myPoints.RemoveAt(i);
        }

        public void AddPoint(clsPoint aPoint, int i = -1)
        {
            if (aPoint == null)
                return;
            if (i == -1)
            {
                myPoints.Add(aPoint.Copy());
            }
            else
            {
                myPoints.Insert(i, aPoint.Copy());
            }
        }

        public void AddPoint(double x, double y)
        {
            myPoints.Add(new clsPoint(x, y));
        }

        //Rotates the order of points anti clockwise
        public void RotatePoints()
        {
            myPoints.Add(myPoints[0]);
            myPoints.RemoveAt(0);
        }

        //Rotates the order of points clockwise
        public void RotatePointsClockwise()
        {
            myPoints.Insert(0, myPoints[myPoints.Count - 1]);
            myPoints.RemoveAt(myPoints.Count - 1);
        }

        public void Mirror()
        {
            int i;
            int j;
            int n;
            clsPoint tempPoint;

            n = Convert.ToInt32((myPoints.Count - 1) / 2);

            //Start by swapping points 0 and 1
            //Then swap points nPoints and 2
            //Then swap points nPoints-1 and 3
            //etc

            tempPoint = myPoints[0];
            myPoints[0] = myPoints[1];
            myPoints[1] = tempPoint;

            i = myPoints.Count - 1;
            j = 2;
            while (i > j)
            {
                tempPoint = myPoints[i];
                myPoints[i] = myPoints[j];
                myPoints[j] = tempPoint;
                i = i - 1;
                j = j + 1;
            }
        }

        public void LoadSketch(System.IO.StreamReader sr)
        {
            double x;
            double y;
            int i;
            int n;

            n = Convert.ToInt32(sr.ReadLine());
            for (i = 0; i <= n; i++)
            {
                x = Convert.ToDouble(sr.ReadLine());
                y = Convert.ToDouble(sr.ReadLine());
                myPoints.Add(new clsPoint(x, y));
            }
        }

        public void SaveSketch(System.IO.StreamWriter sr)
        {
            int i;

            sr.WriteLine(myPoints.Count - 1);
            for (i = 0; i <= myPoints.Count - 1; i++)
            {
                sr.WriteLine(myPoints[i].x);
                sr.WriteLine(myPoints[i].y);
            }
        }

        public bool IsPointInside(clsPoint p1)
        {
            //Uses the "winding number" to check if a point lies inside or outside the sketch
            int i;
            double a;
            double a1;
            clsLine l1;
            clsLine l2;

            a = 0;
            l1 = new clsLine(p1, Point(0));
            for (i = 1; i <= myPoints.Count - 1; i++)
            {
                l2 = new clsLine(p1, Point(i));
                a1 = mdlGeometry.Angle(l1, l2);
                a = a + a1;
                l1 = l2.Copy();
            }
            l2 = new clsLine(p1, Point(0));
            a1 = mdlGeometry.Angle(l1, l2);
            a = a + a1;
            if (mdlGeometry.IsSameDbl(a, 0))
                return false;
            return true;
        }

        public bool IsPointInside2(clsPoint p1)
        {
            //Avoids the case when one of the points is exactly on one of the lines
            if (IsPointTouching(p1))
                return false;
            return IsPointInside(p1);
        }

        public bool IsPointTouching(clsPoint p1)
        {
            //Avoids the case when the point is exactly on one of the lines
            int i;
            int j;
            clsLine l1;
            bool bRet;

            bRet = false;
            for (i = 0; i <= myPoints.Count - 1; i++)
            {
                j = i + 1;
                if (j > myPoints.Count - 1)
                    j = 0;
                l1 = new clsLine(Point(i), Point(j));
                if (l1.IsOnShortLine(p1))
                    bRet = true;
            }
            return bRet;
        }

        public bool IsPointInSketch(clsPoint p1)
        {
            //Avoids the case when one the point is exactly one of the points
            int i;
            bool bRet;

            bRet = false;
            for (i = 0; i <= myPoints.Count - 1; i++)
            {
                if (Point(i) == p1)
                    bRet = true;
            }
            return bRet;
        }

        public int NumCommonPoints(clsSketch aSketch, double aTol)
        {
            int i;
            int j;
            int n;

            n = -1;
            for (i = 0; i <= NumPoints; i++)
            {
                for (j = 0; j <= aSketch.NumPoints; j++)
                {
                    if (Point(i) == aSketch.Point(j))
                        n = n + 1;
                }
            }
            return n;
        }

        public bool Overlaps(clsSketch aSketch, double aTol)
        {
            int i;
            int j;
            int k;
            clsLine l1;
            clsLine l2;
            clsPoint p1;
            bool isInside;

            //Disregard the case of when one sketch is contained in the other.
            isInside = true;
            for (i = 0; i <= NumPoints; i++)
            {
                if (aSketch.IsPointInside2(Point(i)) == false)
                    isInside = false;
            }
            if (isInside)
                return false;
            isInside = true;
            for (i = 0; i <= aSketch.NumPoints; i++)
            {
                if (IsPointInside2(aSketch.Point(i)) == false)
                    isInside = false;
            }
            if (isInside)
                return false;

            //Firstly see if a point of one sketch lies inside the other
            for (i = 0; i <= NumPoints; i++)
            {
                if (aSketch.IsPointInside2(Point(i)))
                    return true;
            }
            for (i = 0; i <= aSketch.NumPoints; i++)
            {
                if (IsPointInside2(aSketch.Point(i)))
                    return true;
            }

            //Next, do more than 3 points coincide?
            if (NumCommonPoints(aSketch, aTol) > 1)
                return true;

            //Is there a non-trivial intersection?
            for (i = 0; i <= NumPoints; i++)
            {
                if (i < NumPoints)
                {
                    k = i + 1;
                }
                else
                {
                    k = 0;
                }
                l1 = new clsLine(Point(i), Point(k));

                //Is there a non-trivial intersection?
                for (j = 0; j <= aSketch.NumPoints; j++)
                {
                    if (j < aSketch.NumPoints)
                    {
                        k = j + 1;
                    }
                    else
                    {
                        k = 0;
                    }
                    l2 = new clsLine(aSketch.Point(j), aSketch.Point(k));
                    p1 = l1.Intersect(l2);
                    if (p1 != null)
                    {
                        if (l1.IsOnShortLine(p1, 0, true) & l2.IsOnShortLine(p1, 0, true))
                            return true;
                    }
                }
            }
            return false;
        }

        public List<clsPoint> Intersect(clsLine l1, bool sortByDistToL1P1 = false)
        {
            List<clsPoint> myPts = new List<clsPoint>();
            int i;
            int j;
            clsLine l2;
            clsPoint p1;
            double d1;
            double d2;

            for (i = 0; i <= NumPoints; i++)
            {
                j = i + 1;
                if (j > NumPoints)
                    j = 0;
                l2 = new clsLine(Point(i), Point(j));
                p1 = l1.IntersectShortLine1(l2);
                if (p1 != null)
                    myPts.Add(p1);
            }

            if (sortByDistToL1P1)
            {
                for (i = 0; i <= myPts.Count - 2; i++)
                {
                    d1 = myPts[i].Dist(l1.P1);
                    for (j = i + 1; j <= myPts.Count - 1; j++)
                    {
                        d2 = myPts[j].Dist(l1.P1);
                        if (d2 < d1)
                        {
                            p1 = myPts[i];
                            myPts[i] = myPts[j];
                            myPts[j] = p1;
                        }
                    }
                }
            }

            return myPts;
        }

        public void Move(double x, double y)
        {
            int i;

            for (i = 0; i <= myPoints.Count - 1; i++)
            {
                myPoints[i].Move(x, y);
            }
        }

        public void Rotate(double ang)
        {
            int i;

            for (i = 0; i <= myPoints.Count - 1; i++)
            {
                myPoints[i].Rotate(ang);
            }
        }

        public void RotateAboutPoint(clsPoint p1, double ang)
        {
            int i;

            for (i = 0; i <= myPoints.Count - 1; i++)
            {
                myPoints[i].RotateAboutPoint(p1, ang);
            }
        }

        public void GetBounds(ref double myMinX, ref double myMaxX, ref double myMinY, ref double myMaxY)
        {
            List<double> x = new List<double>();
            List<double> y = new List<double>();

            x.AddRange(Array.ConvertAll(myPoints.ToArray(), (clsPoint p1) => p1.x));
            y.AddRange(Array.ConvertAll(myPoints.ToArray(), (clsPoint p1) => p1.y));

            x.Sort();
            y.Sort();

            myMinX = x[0];
            myMaxX = x[x.Count - 1];
            myMinY = y[0];
            myMaxX = y[y.Count - 1];
        }
        public bool SelfIntersects()
        {
            int i;
            int i1;
            int j;
            int j1;
            int n;
            clsLine l1;
            clsLine l2;

            for (i = 0; i <= NumPoints - 2; i++)
            {
                i1 = i + 1;
                l1 = new clsLine(Point(i), Point(i1));

                n = NumPoints;
                if (i == 0)
                    n = n - 1;
                //Don't need to intersect the first line with the last, because they touch
                for (j = i + 2; j <= n; j++)
                {
                    j1 = j + 1;
                    if (j1 > NumPoints)
                        j1 = 0;
                    l2 = new clsLine(Point(j), Point(j1));

                    if (l1.IntersectShortLines(l2) != null)
                        return true;
                }
            }
            return false;
        }

        public int GetPoint(clsPoint p1)
        {
            //Gets the index of the 2d point
            int i;

            for (i = 0; i <= NumPoints; i++)
            {
                if (p1 == myPoints[i])
                    return i;
            }
            return -1;
        }

        public clsPoint Centre()
        {
            double x;
            double y;
            int i;

            x = 0;
            y = 0;
            for (i = 0; i <= NumPoints; i++)
            {
                x = x + myPoints[i].x;
                y = y + myPoints[i].y;
            }
            x = x / (NumPoints + 1);
            y = y / (NumPoints + 1);
            return new clsPoint(x, y);
        }

        public void InsertPoint(clsPoint aPoint, int anIndex)
        {
            if (anIndex == -1 | anIndex > myPoints.Count)
            {
                anIndex = myPoints.Count;
            }
            myPoints.Insert(anIndex, aPoint.Copy());
        }

        public double Length()
        {
            int i;
            int j;
            double d = 0;

            for (i = 0; i <= NumPoints; i++)
            {
                j = i + 1;
                if (j > NumPoints)
                    j = 0;
                d = d + Point(i).Dist(Point(j));
            }

            return d;
        }

        public int GetNearestPointIndex(clsPoint p1)
        {
            int i;
            int n;
            double d;
            double minD;

            n = 0;
            minD = 10000;
            for (i = 0; i <= NumPoints; i++)
            {
                d = Point(i).Dist(p1);
                if (d < minD)
                {
                    minD = d;
                    n = i;
                }
            }
            return n;
        }

        public double DistanceToNearestSegment(clsPoint p1, ref int nSeg, ref int nPt, int avoidPt1 = -1, int avoidPt2 = -1, int includePt = -1)
        {
            int i;
            int j;
            clsLine l1;
            double d;
            double minD;

            //Find the point nearest
            nPt = GetNearestPointIndex(p1);

            //Find the segment closest to the point to add (ignore riser segments)
            nSeg = 0;
            minD = 10000;
            for (i = 0; i <= NumPoints; i++)
            {
                if ((i != avoidPt1 & i != avoidPt2) & (includePt == -1 || (i == includePt | i == includePt - 1 | (includePt == 0 & i == NumPoints))))
                {
                    j = i + 1;
                    if (j > NumPoints)
                        j = 0;
                    l1 = new clsLine(Point(i), Point(j));
                    d = l1.DistanceToShortLine(p1);
                    if (d < minD)
                    {
                        minD = d;
                        nSeg = i;
                    }
                }
            }
            return minD;
        }
    }
}