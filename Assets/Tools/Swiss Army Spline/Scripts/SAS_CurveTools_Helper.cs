using UnityEngine;

namespace SwissArmySpline
{
    public static class SAS_CurveTools_Helper
    {
        [System.Serializable]
        public class Point
        {
            public Vector3 position;
            public Quaternion rotation;
            public float tangentIn;
            public float tangentOut;
        } //================================================================================================================================================

        public static Vector3 GetPosition(Point a, Point b, float t)
        {
            Vector3 p0 = a.position;
            Vector3 p1 = GetOutTangent(a);
            Vector3 p2 = GetInTangent(b);
            Vector3 p3 = b.position;

            float oneMinusT = 1f - t;
            return oneMinusT * oneMinusT * oneMinusT * p0 + 3f * oneMinusT * oneMinusT * t * p1 + 3f * oneMinusT * t * t * p2 + t * t * t * p3;
        } //================================================================================================================================================

        public static Point Split(Point point1, Point point2, float t, Quaternion newRot)
        {
            Vector3 a = point1.position;
            Vector3 b = GetOutTangent(point1);
            Vector3 c = GetInTangent(point2);
            Vector3 d = point2.position;

            Vector3 ab     = Vector3.Lerp(a,    b,    t); // previous point new tangent pos OUT
            Vector3 bc     = Vector3.Lerp(b,    c,    t);
            Vector3 cd     = Vector3.Lerp(c,    d,    t); // next point new tangent pos IN
            Vector3 abbc   = Vector3.Lerp(ab,   bc,   t); // new point tangent pos IN
            Vector3 bccd   = Vector3.Lerp(bc,   cd,   t); // new point tangent pos OUT
            Vector3 newPos = Vector3.Lerp(abbc, bccd, t); // new point pos

            point1.tangentOut = Vector3.Distance(a, ab);
            point2.tangentIn  = Vector3.Distance(d, cd);
            return new Point { position = newPos, rotation = newRot, tangentIn = Vector3.Distance(newPos, abbc), tangentOut = Vector3.Distance(newPos, bccd) };
        } //================================================================================================================================================

        public static Quaternion GetRotationLerp(Point a, Point b, float t)
        {
            Quaternion rA = a.rotation;
            Quaternion rB = b.rotation;

            float third = 1 / 3;

            float t1 = (t / 3);
            float t2 = t1 + third;
            float t3 = t2 + third;

            Quaternion r1 = Quaternion.Lerp(rA, rB, third    );
            Quaternion r2 = Quaternion.Lerp(rA, rB, third * 2);

            if (t < third) return Quaternion.Lerp(rA, r1, t2);
            if (t >= third && t < third * 2) return Quaternion.Lerp(r1, r2, t2);
            else return Quaternion.Lerp(r2, rB, t2);
        } //================================================================================================================================================

        public static Vector3 GetDerivative(Point a, Point b, float t)
        {
            Vector3 p0 = a.position;
            Vector3 p1 = GetOutTangent(a);
            Vector3 p2 = GetInTangent(b);
            Vector3 p3 = b.position;

            float oneMinusT = 1f - t;
            return 3f * oneMinusT * oneMinusT * (p1 - p0) + 6f * oneMinusT * t * (p2 - p1) + 3f * t * t * (p3 - p2);
        } //================================================================================================================================================

        public static Vector3 GetOutTangent(Point p) => p.position + p.rotation * Vector3.forward * p.tangentOut;

        public static Vector3 GetInTangent(Point p) => p.position - p.rotation * Vector3.forward * p.tangentIn;

        public static void DrawCurve(Point a, Point b, Color color, Transform t = null)
        {
            Vector3 p1 = a.position;
            Vector3 t1 = GetOutTangent(a);
            Vector3 p2 = b.position;
            Vector3 t2 = GetInTangent(b);

            if (t != null)
            {
                p1 = t.TransformPoint(p1);
                t1 = t.TransformPoint(t1);
                p2 = t.TransformPoint(p2);
                t2 = t.TransformPoint(t2);
            }

            //Gizmos.color = Color.blue;
            //Gizmos.DrawLine(p1, t1);
            //Gizmos.DrawLine(p2, t2);

            //Gizmos.DrawSphere(t1, 0.05f);
            //Gizmos.DrawSphere(t2, 0.05f);

            //Gizmos.color = Color.green;
            //Gizmos.DrawRay(a.position, a.rotation * Vector3.up * 0.5f);

            //Gizmos.color = Color.white;
            //Gizmos.DrawSphere(p1, 0.1f);
            //Gizmos.DrawSphere(p2, 0.1f);

#if UNITY_EDITOR
            UnityEditor.Handles.DrawBezier(p1, p2, t1, t2, color, null, 3.0f);
#endif
        } //================================================================================================================================================
    }
}