using UnityEngine;
using UnityEditor;

namespace SwissArmySpline
{
    public partial class SAS_ProfileCreator : EditorWindow
    {
        static Rect pivotRect = new Rect();
        static Rect pivotRectT = new Rect();
        static Rect pivotRectR = new Rect();
        static Rect pivotRectS = new Rect();



        void SetupPivotRects()
        {
            Vector2 pivotGUIPos = WorldToGUIPoint(p.profiles[p.selectedProfile].pivot);
            pivotRect.position = pivotGUIPos - vertexSize3.position;
            pivotRectT.position = pivotGUIPos - vertexSize2.position + new Vector2(-15, 0);
            pivotRectR.position = pivotGUIPos - vertexSize2.position + new Vector2(0, -15);
            pivotRectS.position = pivotGUIPos - vertexSize2.position + new Vector2(15, 0);
            pivotRect.size = vertexSize3.size;
            pivotRectT.size = vertexSize2.size;
            pivotRectR.size = vertexSize2.size;
            pivotRectS.size = vertexSize2.size;
        } //===========================================================================================================================================================



        void DrawPivot()
        {
            Vector2 pos = WorldToGUIPoint(p.profiles[p.selectedProfile].pivot);

            if (selection.Count > 0)
            {
                Color yellow = Color.yellow * 0.66f;
                Color white1 = Color.white * 0.75f;
                Color white2 = Color.white * 0.50f;

                Handles.color = new Color(1, 1, 1, 0.02f);
                Handles.DrawSolidArc(pos, Vector3.forward, new Vector3(-1, 0, 0), 180, 40);

                bool hover = indexDrag == -3 || indexHover == -3;
                GUI.color = Color.white;
                GUI.Label(new Rect(pos + new Vector2(-15, 0) + new Vector2(-10, -23), new Vector2(20, 20)), "T", vertexLabels);
                GUI.color = hover ? white1 : white2;
                GUI.DrawTexture(new Rect(pos + new Vector2(-15, 0) - vertexSize2.position, vertexSize2.size), Texture2D.whiteTexture);
                GUI.color = hover ? yellow : colorBackground;
                GUI.DrawTexture(new Rect(pos + new Vector2(-15, 0) - vertexSize1.position, vertexSize1.size), Texture2D.whiteTexture);


                if (mode == Mode.Shape)
                {
                    hover = indexDrag == -4 || indexHover == -4;
                    GUI.color = Color.white;
                    GUI.Label(new Rect(pos + new Vector2(0, -15) + new Vector2(-10, -23), new Vector2(20, 20)), "R", vertexLabels);
                    GUI.color = hover ? white1 : white2;
                    GUI.DrawTexture(new Rect(pos + new Vector2(0, -15) - vertexSize2.position, vertexSize2.size), Texture2D.whiteTexture);
                    GUI.color = hover ? yellow : colorBackground;
                    GUI.DrawTexture(new Rect(pos + new Vector2(0, -15) - vertexSize1.position, vertexSize1.size), Texture2D.whiteTexture);
                }

                hover = indexDrag == -5 || indexHover == -5;
                GUI.color = Color.white;
                GUI.Label(new Rect(pos + new Vector2(15, 0) + new Vector2(-10, -23), new Vector2(20, 20)), "S", vertexLabels);
                GUI.color = hover ? white1 : white2;
                GUI.DrawTexture(new Rect(pos + new Vector2(15, 0) - vertexSize2.position, vertexSize2.size), Texture2D.whiteTexture);
                GUI.color = hover ? yellow : colorBackground;
                GUI.DrawTexture(new Rect(pos + new Vector2(15, 0) - vertexSize1.position, vertexSize1.size), Texture2D.whiteTexture);
            }

            Handles.color = new Color(1, 1, 1, 1);
            Handles.DrawLine(pos + new Vector2(-11, 0), pos + new Vector2(10, 0));
            Handles.DrawLine(pos + new Vector2(0, -10), pos + new Vector2(0, 11));

            GUI.color = Color.white;
            GUI.DrawTexture(pivotRect, Texture2D.whiteTexture);
            GUI.color = colorBackground;
            GUI.DrawTexture(new Rect(pos - vertexSize2.position, vertexSize2.size), Texture2D.whiteTexture);

            Handles.color = new Color(1, 1, 1, 0.02f);
            if (indexHover == -2 || indexDrag == -2)
            {
                GUI.color = Color.yellow;
                GUI.DrawTexture(new Rect(pos - vertexSize1.position, vertexSize1.size), Texture2D.whiteTexture);
            }
            else if (indexHover == -3 || indexDrag == -3) Handles.color = new Color(1, 1, 0, 0.05f);
        } //===========================================================================================================================================================
    }
}
