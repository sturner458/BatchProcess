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
        public List<double> LastSeenMarkerDistances = new List<double>();
        public double[] ProjMatrix = new double[16];

        public List<clsMarkerPoint> ConfirmedMarkers = new List<clsMarkerPoint>();
        public List<clsMarkerPoint> SuspectedMarkers = new List<clsMarkerPoint>();
        public List<clsMarkerPoint> DoorMarkers = new List<clsMarkerPoint>();
        public List<clsMarkerPoint> WallMarkers = new List<clsMarkerPoint>();
        public List<clsMarkerPoint> BulkheadMarkers = new List<clsMarkerPoint>();
        public List<clsMarkerPoint> ObstructMarkers = new List<clsMarkerPoint>();

        public RecognisedMarkers() {
            
        }

        public void Clear() {
            MarkersSeenID.Clear();
            ModelViewMatrix.Clear();
            Corners.Clear();
            ConfirmedMarkers.Clear();
            SuspectedMarkers.Clear();
            DoorMarkers.Clear();
            WallMarkers.Clear();
            BulkheadMarkers.Clear();
            ObstructMarkers.Clear();
        }

        public void GetMarkersCopy() {
            ConfirmedMarkers = mdlRecognise.ConfirmedMarkers.Select(m => m.Copy(true)).ToList();
            SuspectedMarkers = mdlRecognise.mySuspectedMarkers.Select(m => m.Copy(true)).ToList();
            DoorMarkers = mdlRecognise.myDoorMarkers.Select(m => m.Copy(true)).ToList();
            WallMarkers = mdlRecognise.myWallMarkers.Select(m => m.Copy(true)).ToList();
            BulkheadMarkers = mdlRecognise.myBulkheadMarkers.Select(m => m.Copy(true)).ToList();
            ObstructMarkers = mdlRecognise.myObstructMarkers.Select(m => m.Copy(true)).ToList();
        }

        public RecognisedMarkers Copy() {
            RecognisedMarkers _copy = new RecognisedMarkers();

            _copy.MarkersSeenID.AddRange(MarkersSeenID.ToArray());

            for (int i = 0; i < ModelViewMatrix.Count; i++) {
                _copy.ModelViewMatrix.Add(ModelViewMatrix[i].ToArray());
            }

            for (int i = 0; i < Corners.Count; i++) {
                _copy.Corners.Add(Corners[i].Select(p => p.Copy()).ToList());
            }

            _copy.LastSeenMarkerDistances.AddRange(LastSeenMarkerDistances.ToArray());
            _copy.ProjMatrix = ProjMatrix.ToArray();
            _copy.ConfirmedMarkers.AddRange(ConfirmedMarkers.Select(m => m.Copy(true)));
            _copy.SuspectedMarkers.AddRange(SuspectedMarkers.Select(m => m.Copy(true)));
            _copy.DoorMarkers.AddRange(DoorMarkers.Select(m => m.Copy(true)));
            _copy.DoorMarkers.AddRange(WallMarkers.Select(m => m.Copy(true)));
            _copy.BulkheadMarkers.AddRange(BulkheadMarkers.Select(m => m.Copy(true)));
            _copy.ObstructMarkers.AddRange(ObstructMarkers.Select(m => m.Copy(true)));

            return _copy;
        }

        //public RecognisedMarkers QuickCopy() {
        //    inCopy = true;

        //    RecognisedMarkers _copy = new RecognisedMarkers(IsLowRes);

        //    _copy.TimeTaken = TimeTaken;
        //    _copy.TimeDifference = TimeDifference;
        //    _copy.AngularVelocity = AngularVelocity;

        //    _copy.MarkersSeenID.AddRange(MarkersSeenID.ToArray());
        //    for (int i = 0; i < ModelViewMatrix.Count; i++) {
        //        _copy.ModelViewMatrix.Add(ModelViewMatrix[i].ToArray());
        //    }

        //    inCopy = false;
        //    return _copy;
        //}

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

        public void SetMarkerInvisible(int myMarkerID) {
            int i = MarkersSeenID.IndexOf(myMarkerID);
            if (i > -1) {
                MarkersSeenID.RemoveAt(i);
                ModelViewMatrix.RemoveAt(i);
                Corners.RemoveAt(i);
            }
        }

        public List<int> GetConfirmedMarkersSeenID() {
            int i;
            List<int> myMarkerList = new List<int>();

            for (i = 0; i < ConfirmedMarkers.Count; i++) {
                if (GetMarkerVisible(ConfirmedMarkers[i].MarkerID)) myMarkerList.Add(ConfirmedMarkers[i].MarkerID);
            }

            return myMarkerList;
        }

        public List<int> GetSuspectedMarkersSeenID() {
            int i;
            List<int> myMarkerList = new List<int>();

            for (i = 0; i < SuspectedMarkers.Count; i++) {
                if (GetMarkerVisible(SuspectedMarkers[i].MarkerID) && !myMarkerList.Contains(SuspectedMarkers[i].MarkerID)) myMarkerList.Add(SuspectedMarkers[i].MarkerID);
            }

            return myMarkerList;
        }

        public List<int> GetDoorMarkersSeenID() {
            int i;
            List<int> myMarkerList = new List<int>();

            for (i = 0; i < DoorMarkers.Count; i++) {
                if (GetMarkerVisible(DoorMarkers[i].MarkerID)) myMarkerList.Add(DoorMarkers[i].MarkerID);
            }

            return myMarkerList;
        }

        public List<int> GetObstructMarkersSeenID() {
            int i;
            List<int> myMarkerList = new List<int>();

            for (i = 0; i < ObstructMarkers.Count; i++) {
                if (GetMarkerVisible(ObstructMarkers[i].MarkerID)) myMarkerList.Add(ObstructMarkers[i].MarkerID);
            }

            return myMarkerList;
        }

        public List<int> GetWallMarkersSeenID() {
            int i;
            List<int> myMarkerList = new List<int>();

            for (i = 0; i < WallMarkers.Count; i++) {
                if (GetMarkerVisible(WallMarkers[i].MarkerID)) myMarkerList.Add(WallMarkers[i].MarkerID);
            }

            return myMarkerList;
        }

    }
}
