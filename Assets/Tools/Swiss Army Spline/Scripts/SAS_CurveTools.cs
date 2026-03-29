using System.Collections.Generic;
using UnityEngine;

namespace SwissArmySpline
{
    public partial class SAS_CurveTools : MonoBehaviour
    {
        public List<SAS_CurveTools_Helper.Point> points = new List<SAS_CurveTools_Helper.Point>();
        public bool loop;

        public float totalLength;
        public AnimationCurve distanceToSplineTime;
        public int selectedIndex;
        public float handleSize = (0.05f + 0.3f) * 0.5f;
        public bool zTest = false;
        public bool showLabels = true;

        public bool editable = true;

        public Vector4[] polyLine = new Vector4[0];




        public struct Point
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 forward;
            public Vector3 up;
            public Vector3 right;
        };//================================================================================================================================================




        public Point GetPoint(float distance, Vector2 sampleSeparation, bool posIsBetweenSamples = false)
        {
            //distance = Mathf.Clamp(distance, 0, totalLength);

            float t1, t2;
            if (loop)
            {
                t1 = distanceToSplineTime.Evaluate(Mathf.Repeat(distance + sampleSeparation.x, totalLength));
                t2 = distanceToSplineTime.Evaluate(Mathf.Repeat(distance + sampleSeparation.y, totalLength));
            }
            else
            {
                t1 = distanceToSplineTime.Evaluate(Mathf.Clamp(distance + sampleSeparation.x, 0, totalLength));
                t2 = distanceToSplineTime.Evaluate(Mathf.Clamp(distance + sampleSeparation.y, 0, totalLength));
            }

            int index1Curr = Mathf.FloorToInt(t1); index1Curr = index1Curr > points.Count - 1 ? 0 : index1Curr;
            int index1Next = Mathf.CeilToInt (t1); index1Next = index1Next > points.Count - 1 ? 0 : index1Next;
            int index2Curr = Mathf.FloorToInt(t2); index2Curr = index2Curr > points.Count - 1 ? 0 : index2Curr;
            int index2Next = Mathf.CeilToInt (t2); index2Next = index2Next > points.Count - 1 ? 0 : index2Next;

            Vector3 right1 = Quaternion.Slerp(points[index1Curr].rotation, points[index1Next].rotation, Mathf.Repeat(t1, 1)) * Vector3.right;
            Vector3 right2 = Quaternion.Slerp(points[index2Curr].rotation, points[index2Next].rotation, Mathf.Repeat(t2, 1)) * Vector3.right;

            Vector3 p1 = SamplePoint(t1);
            Vector3 p2 = SamplePoint(t2);

            Vector3 p;
            if (posIsBetweenSamples) p = (p1 + p2) * 0.5f;
            else p = SamplePoint(distanceToSplineTime.Evaluate(Mathf.Repeat(distance, totalLength)));
            Vector3 f = (p2 - p1).normalized;
            Vector3 u = Vector3.Cross(f, ((right1 + right2) * 0.5f).normalized).normalized;
            Vector3 r = Vector3.Cross(u, f).normalized;
            Quaternion q = Quaternion.LookRotation(f, u);
            //if (f == Vector3.zero || u == Vector3.zero) Debug.Log("View Vector = zero! Distance: " + distance);
            return new Point { position = p, rotation = q, forward = f, up = u, right = r };
        } //================================================================================================================================================


        public void InsertPoint(float distance)
        {
            float t;
            Vector3 p1, p2;

            if (loop)
            {
                t = distanceToSplineTime.Evaluate(Mathf.Repeat(distance, totalLength));
                p1 = SamplePoint(distanceToSplineTime.Evaluate(Mathf.Repeat(distance - 0.01f, totalLength)));
                p2 = SamplePoint(distanceToSplineTime.Evaluate(Mathf.Repeat(distance + 0.01f, totalLength)));
            }
            else
            {
                t = distanceToSplineTime.Evaluate(Mathf.Clamp(distance, 0, totalLength));
                p1 = SamplePoint(distanceToSplineTime.Evaluate(Mathf.Clamp(distance - 0.01f, 0, totalLength)));
                p2 = SamplePoint(distanceToSplineTime.Evaluate(Mathf.Clamp(distance + 0.01f, 0, totalLength)));
            }

            int indexCurr = Mathf.FloorToInt(t); indexCurr = indexCurr > points.Count - 1 ? 0 : indexCurr;
            int indexNext = Mathf.CeilToInt (t); indexNext = indexNext > points.Count - 1 ? 0 : indexNext;
            SAS_CurveTools_Helper.Point point1 = points[indexCurr];
            SAS_CurveTools_Helper.Point point2 = points[indexNext];

            t = Mathf.Repeat(t, 1);

            Vector3 forward = (p2 - p1).normalized;
            Vector3 right = Quaternion.Slerp(point1.rotation, point2.rotation, Mathf.Repeat(t, 1)) * Vector3.right;
            Vector3 up = Vector3.Cross(forward, right).normalized;

            selectedIndex = indexNext == 0 ? points.Count : indexNext;

            points.Insert(selectedIndex, SAS_CurveTools_Helper.Split(point1, point2, Mathf.Repeat(t, 1), Quaternion.LookRotation(forward, up)));
        } //================================================================================================================================================


        public int GetIndex(float distance)
        {
            float t;
            if (loop) t = distanceToSplineTime.Evaluate(Mathf.Repeat(distance,    totalLength));
            else      t = distanceToSplineTime.Evaluate(Mathf.Clamp (distance, 0, totalLength));

            int index = Mathf.FloorToInt(t); index = index > points.Count - 1 ? 0 : index;
            return index;
        } //================================================================================================================================================


        public SAS_CurveTools_Helper.Point[] GetBezierSegment(float distance)
        {
            float t;
            if (loop) t = distanceToSplineTime.Evaluate(Mathf.Repeat(distance,    totalLength));
            else      t = distanceToSplineTime.Evaluate(Mathf.Clamp (distance, 0, totalLength));

            int indexCurr = Mathf.FloorToInt(t); indexCurr = indexCurr > points.Count - 1 ? 0 : indexCurr;
            int indexNext = Mathf.CeilToInt(t);  indexNext = indexNext > points.Count - 1 ? 0 : indexNext;
            SAS_CurveTools_Helper.Point point1 = points[indexCurr];
            SAS_CurveTools_Helper.Point point2 = points[indexNext];
            return new SAS_CurveTools_Helper.Point[2] { point1, point2 }; 
        } //================================================================================================================================================


        public Vector3 GetPointSimple(float distance)
        {
            float t;
            if (loop) t = distanceToSplineTime.Evaluate(Mathf.Repeat(distance,    totalLength));
            else      t = distanceToSplineTime.Evaluate(Mathf.Clamp (distance, 0, totalLength));
            return SamplePoint(t);
        } //================================================================================================================================================

        public Vector3 GetDerivative(float distance)
        {
            float t;
            if (loop) t = distanceToSplineTime.Evaluate(Mathf.Repeat(distance,    totalLength));
            else      t = distanceToSplineTime.Evaluate(Mathf.Clamp (distance, 0, totalLength));
            return SampleDerivative(t);
        } //================================================================================================================================================



        private Vector3 SamplePoint(float time)
        {
            if (loop)
            {
                int index = Mathf.FloorToInt(Mathf.Repeat(time, points.Count));
                float t = Mathf.Repeat(time, 1);
                if (index >= points.Count - 1) return SAS_CurveTools_Helper.GetPosition(points[points.Count - 1], points[0], t);
                else return SAS_CurveTools_Helper.GetPosition(points[index], points[index + 1], t);
            }
            else
            {
                int index = Mathf.FloorToInt(Mathf.Repeat(time, points.Count - 1));
                float t = Mathf.Repeat(time, 1);
                return SAS_CurveTools_Helper.GetPosition(points[index], points[index + 1], t);
            }
        } //================================================================================================================================================



        private Vector3 SampleDerivative(float time)
        {
            if (loop)
            {
                int index = Mathf.FloorToInt(Mathf.Repeat(time, points.Count));
                float t = Mathf.Repeat(time, 1);
                if (index >= points.Count - 1) return SAS_CurveTools_Helper.GetDerivative(points[points.Count - 1], points[0], t);
                else return SAS_CurveTools_Helper.GetDerivative(points[index], points[index + 1], t);
            }
            else
            {
                int index = Mathf.FloorToInt(Mathf.Repeat(time, points.Count - 1));
                float t = Mathf.Repeat(time, 1);
                return SAS_CurveTools_Helper.GetDerivative(points[index], points[index + 1], t);
            }
        } //================================================================================================================================================





        public void CalculateLength()
        {
            totalLength = 0;
            if (points.Count < 2) return;

            Vector4 pNow = points[0].position;
            Vector3 pOld = points[0].position;
            distanceToSplineTime = new AnimationCurve();

            // Setup polyLine Array
            int arrayLength = (points.Count - 1) * 20;
            if (loop) arrayLength += 20;
            arrayLength += 1;
            polyLine = new Vector4[arrayLength];
            int indexCounter = 0;

            for (int i = 0; i < points.Count - 1; i++) Do(i);
            if (!loop) SampleIndex(points.Count - 1);
            else
            {
                for (int i = points.Count - 1; i < points.Count; i++) Do(i);

                pNow = points[0].position;
                totalLength += Vector3.Distance(pNow, pOld);
                pNow.w = totalLength;
                polyLine[indexCounter] = pNow;
                distanceToSplineTime.AddKey(totalLength, points.Count);
            }

            SetCurveLinear(distanceToSplineTime);
            if (loop) distanceToSplineTime.postWrapMode = distanceToSplineTime.preWrapMode = WrapMode.Loop;



            void Do(int index)
            {
                SampleIndex(index);

                for (int i = 0; i < sampleTimes.Length; i++)
                {
                    SampleInterpolant(index + sampleTimes[i]);
                }

                //SampleInterpolant(index     + (1.0f / 32.0f));
                //SampleInterpolant(index     + (1.0f / 16.0f));
                //SampleInterpolant(index     + (1.0f /  8.0f));
                //SampleInterpolant(index     + (1.0f /  4.0f));
                //SampleInterpolant(index     + (1.0f /  2.0f));
                //SampleInterpolant(index + 1 - (1.0f /  4.0f));
                //SampleInterpolant(index + 1 - (1.0f /  8.0f));
                //SampleInterpolant(index + 1 - (1.0f / 16.0f));
                //SampleInterpolant(index + 1 - (1.0f / 32.0f));
            }

            void SampleIndex(int index)
            {
                pNow = points[index].position;
                totalLength += Vector3.Distance(pNow, pOld);
                pNow.w = totalLength;
                polyLine[indexCounter++] = pNow;
                pOld = pNow;
                distanceToSplineTime.AddKey(totalLength, index);
            }

            void SampleInterpolant(float interpolant)
            {
                pNow = SamplePoint(interpolant);
                totalLength += Vector3.Distance(pNow, pOld);
                pNow.w = totalLength;
                polyLine[indexCounter++] = pNow;
                pOld = pNow;
                distanceToSplineTime.AddKey(totalLength, interpolant);
            }
        } //================================================================================================================================================

        static readonly float[] sampleTimes = new float[]
        {
            0.015625f, // 1 / 64
            0.03125f, // 1 / 32
            0.046875f, // 1 / 32 + 1 / 64
            0.0625f, // 1 / 16
            0.09375f, // 1 / 16 + 1 / 32
            0.125f, // 1 /  8
            0.1875f, // 1 /  8 + 1 / 16
            0.25f, // 1 / 4
            0.375f, // 1 / 4 + 1 / 8 
            0.5f,
            0.625f,
            0.75f,
            0.8125f,
            0.875f,
            0.90625f,
            0.9375f,
            0.953125f,
            0.96875f,
            0.984375f,
        };

        //static readonly float[] sampleTimes = new float[]
        //{
        //    0.03125f,
        //    0.0625f,
        //    0.125f,
        //    0.25f,
        //    0.5f,
        //    0.75f,
        //    0.875f,
        //    0.9375f,
        //    0.96875f,
        //};


        void SetCurveLinear(AnimationCurve curve)
        {
            for (int i = 0; i < curve.keys.Length; i += 10)
            {
                float intangent = 0;
                float outtangent = 0;
                Vector2 point1;
                Vector2 point2;
                Vector2 deltapoint;
                Keyframe key = curve[i];

                if (i > 0)
                {
                    point1.x = curve.keys[i - 1].time;
                    point1.y = curve.keys[i - 1].value;
                    point2.x = curve.keys[i].time;
                    point2.y = curve.keys[i].value;

                    deltapoint = point2 - point1;

                    intangent = deltapoint.y / deltapoint.x;
                }
                if (i < curve.length - 1)
                {
                    point1.x = curve.keys[i].time;
                    point1.y = curve.keys[i].value;
                    point2.x = curve.keys[i + 1].time;
                    point2.y = curve.keys[i + 1].value;

                    deltapoint = point2 - point1;

                    outtangent = deltapoint.y / deltapoint.x;
                }

                key.inTangent = intangent;
                key.outTangent = outtangent;
                curve.MoveKey(i, key);
            }
        } //================================================================================================================================================
    }
}
