using System;
using System.Collections.Generic;
using System.Linq;

namespace BatchProcess
{
    public class RecognisedMarkers
    {
        public List<int> MarkersSeenID = new List<int>();
        public List<double[]> ModelViewMatrix = new List<double[]>();
        public List<List<clsPoint>> Corners = new List<List<clsPoint>>();
        public List<List<clsPoint>> Circles = new List<List<clsPoint>>();
        public List<double> LastSeenMarkerDistances = new List<double>();
        public double[] ProjMatrix = new double[16];

        public List<clsMarkerPoint> ConfirmedMarkers = new List<clsMarkerPoint>();
        public List<clsMarkerPoint> SuspectedMarkers = new List<clsMarkerPoint>();

        public RecognisedMarkers() {
            
        }

        public void Clear() {
            MarkersSeenID.Clear();
            ModelViewMatrix.Clear();
            Corners.Clear();
            Circles.Clear();
            ConfirmedMarkers.Clear();
            SuspectedMarkers.Clear();
        }

        public void GetMarkersCopy() {
            ConfirmedMarkers = mdlRecognise.ConfirmedMarkers.Select(m => m.Copy(true)).ToList();
            SuspectedMarkers = mdlRecognise.mySuspectedMarkers.Select(m => m.Copy(true)).ToList();
        }

        public bool GetMarkerVisible(int myMarkerID) {
            int i;
            for (i = 0; i < MarkersSeenID.Count; i++) {
                if (MarkersSeenID[i] == myMarkerID) return true;
            }
            return false;
        }

        public int GetMarkerIndex(int myMarkerID) {
            int i;
            for (i = 0; i < MarkersSeenID.Count; i++) {
                if (MarkersSeenID[i] == myMarkerID) return i;
            }
            return -1;
        }

    }
}
