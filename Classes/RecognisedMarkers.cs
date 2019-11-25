using System;
using System.Collections.Generic;

namespace BatchProcess
{
    public class RecognisedMarkers {

        public bool IsLowRes;

        public List<int> MarkersSeenID = new List<int>();
        public List<double[]> ModelViewMatrix = new List<double[]>();
        public List<double> LastSeenDistances = new List<double>();
        public double[] ProjMatrix = new double[16];

        public List<clsMarkerPoint> ConfirmedMarkers = new List<clsMarkerPoint>();
        public List<clsMarkerPoint> SuspectedMarkers = new List<clsMarkerPoint>();
        public List<clsMarkerPoint> DoorMarkers = new List<clsMarkerPoint>();
        public List<clsMarkerPoint> WallMarkers = new List<clsMarkerPoint>();
        public List<clsMarkerPoint> BulkheadMarkers = new List<clsMarkerPoint>();
        public List<clsMarkerPoint> ObstructMarkers = new List<clsMarkerPoint>();

        public RecognisedMarkers(bool _lowRes)
        {
            IsLowRes = _lowRes;
        }

        public void Clear()
        {
            MarkersSeenID.Clear();
            ModelViewMatrix.Clear();
            ConfirmedMarkers.Clear();
            SuspectedMarkers.Clear();
            DoorMarkers.Clear();
            WallMarkers.Clear();
            BulkheadMarkers.Clear();
            ObstructMarkers.Clear();
        }

        public void GetMarkersCopy()
        {
            int i;

            ConfirmedMarkers.Clear();
            for (i = 0; i < mdlRecognise.ConfirmedMarkers.Count; i++) {
                ConfirmedMarkers.Add(mdlRecognise.ConfirmedMarkers[i].Copy(true));
            }

            SuspectedMarkers.Clear();
            for (i = 0; i < mdlRecognise.mySuspectedMarkers.Count; i++) {
                SuspectedMarkers.Add(mdlRecognise.mySuspectedMarkers[i].Copy(true));
            }

            DoorMarkers.Clear();
            for (i = 0; i < mdlRecognise.myDoorMarkers.Count; i++) {
                DoorMarkers.Add(mdlRecognise.myDoorMarkers[i].Copy(true));
            }

            WallMarkers.Clear();
            for (i = 0; i < mdlRecognise.myWallMarkers.Count; i++) {
                WallMarkers.Add(mdlRecognise.myWallMarkers[i].Copy(true));
            }

            BulkheadMarkers.Clear();
            for (i = 0; i < mdlRecognise.myBulkheadMarkers.Count; i++) {
                BulkheadMarkers.Add(mdlRecognise.myBulkheadMarkers[i].Copy(true));
            }

            ObstructMarkers.Clear();
            for (i = 0; i < mdlRecognise.myObstructMarkers.Count; i++) {
                ObstructMarkers.Add(mdlRecognise.myObstructMarkers[i].Copy(true));
            }
        }

        public RecognisedMarkers Copy()
        {
            int i;
            List<double> f;
            RecognisedMarkers _copy = new RecognisedMarkers(IsLowRes);

            _copy.MarkersSeenID.AddRange(MarkersSeenID.ToArray());

            for (i = 0; i < ModelViewMatrix.Count; i++) {
                f = new List<double>();
                f.AddRange(ModelViewMatrix[i]);
                _copy.ModelViewMatrix.Add(f.ToArray());
            }

            _copy.LastSeenDistances.AddRange(LastSeenDistances.ToArray());

            f = new List<double>();
            f.AddRange(ProjMatrix);
            _copy.ProjMatrix = f.ToArray();

            for (i = 0; i < ConfirmedMarkers.Count; i++) {
                _copy.ConfirmedMarkers.Add(ConfirmedMarkers[i].Copy(true));
            }

            for (i = 0; i < SuspectedMarkers.Count; i++) {
                _copy.SuspectedMarkers.Add(SuspectedMarkers[i].Copy(true));
            }

            for (i = 0; i < DoorMarkers.Count; i++) {
                _copy.DoorMarkers.Add(DoorMarkers[i].Copy(true));
            }

            for (i = 0; i < WallMarkers.Count; i++) {
                _copy.WallMarkers.Add(WallMarkers[i].Copy(true));
            }

            for (i = 0; i < BulkheadMarkers.Count; i++) {
                _copy.BulkheadMarkers.Add(BulkheadMarkers[i].Copy(true));
            }

            for (i = 0; i < ObstructMarkers.Count; i++) {
                _copy.ObstructMarkers.Add(ObstructMarkers[i].Copy(true));
            }

            return _copy;
        }

        public RecognisedMarkers QuickCopy() {
            RecognisedMarkers _copy = new RecognisedMarkers(IsLowRes);

            _copy.MarkersSeenID.AddRange(MarkersSeenID.ToArray());

            for (int i = 0; i < ModelViewMatrix.Count; i++) {
                List<double> f = new List<double>();
                f.AddRange(ModelViewMatrix[i]);
                _copy.ModelViewMatrix.Add(f.ToArray());
            }

            return _copy;
        }

        public bool GetMarkerVisible(int myMarkerID)
        {
            int i;
            for (i = 0; i < MarkersSeenID.Count; i++) {
                if (MarkersSeenID[i] == myMarkerID) return true;
            }
            return false;
        }

        public int GetMarkerIndex(int myMarkerID)
        {
            int i;
            for (i = 0; i < MarkersSeenID.Count; i++) {
                if (MarkersSeenID[i] == myMarkerID) return i;
            }
            return -1;
        }

        public void SetMarkerVisible(int myMarkerID, double[] myMatrix)
        {
            if (!MarkersSeenID.Contains(myMarkerID)) {
                MarkersSeenID.Add(myMarkerID);
                ModelViewMatrix.Add(myMatrix);
            }
        }

        public void SetMarkerInvisible(int myMarkerID)
        {
            int i = MarkersSeenID.IndexOf(myMarkerID);
            if (i > -1) {
                MarkersSeenID.RemoveAt(i);
                ModelViewMatrix.RemoveAt(i);
            }
        }

        public List<int> GetConfirmedMarkersSeenID()
        {
            int i;
            List<int> myMarkerList = new List<int>();

            for (i = 0; i < ConfirmedMarkers.Count; i++) {
                if (GetMarkerVisible(ConfirmedMarkers[i].MarkerID)) myMarkerList.Add(ConfirmedMarkers[i].MarkerID);
            }

            return myMarkerList;
        }

        public List<int> GetSuspectedMarkersSeenID()
        {
            int i;
            List<int> myMarkerList = new List<int>();

            for (i = 0; i < SuspectedMarkers.Count; i++) {
                if (GetMarkerVisible(SuspectedMarkers[i].MarkerID) && !myMarkerList.Contains(SuspectedMarkers[i].MarkerID)) myMarkerList.Add(SuspectedMarkers[i].MarkerID);
            }

            return myMarkerList;
        }

        public List<int> GetDoorMarkersSeenID()
        {
            int i;
            List<int> myMarkerList = new List<int>();

            for (i = 0; i < DoorMarkers.Count; i++) {
                if (GetMarkerVisible(DoorMarkers[i].MarkerID)) myMarkerList.Add(DoorMarkers[i].MarkerID);
            }

            return myMarkerList;
        }

        public List<int> GetObstructMarkersSeenID()
        {
            int i;
            List<int> myMarkerList = new List<int>();

            for (i = 0; i < ObstructMarkers.Count; i++) {
                if (GetMarkerVisible(ObstructMarkers[i].MarkerID)) myMarkerList.Add(ObstructMarkers[i].MarkerID);
            }

            return myMarkerList;
        }

        public List<int> GetWallMarkersSeenID()
        {
            int i;
            List<int> myMarkerList = new List<int>();

            for (i = 0; i < WallMarkers.Count; i++) {
                if (GetMarkerVisible(WallMarkers[i].MarkerID)) myMarkerList.Add(WallMarkers[i].MarkerID);
            }

            return myMarkerList;
        }
        
    }
}
