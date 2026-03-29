using UnityEngine;
using UnityEditor;

namespace SwissArmySpline
{
    public partial class SAS_ProfileCreator : EditorWindow
    {
        GUIStyle gridNumbers = new GUIStyle();

        static readonly Color colorBackground = new Color32(35, 35, 35, 255);
        static readonly Color colorGridCenter = new Color32(255, 255, 255, 100);
        static readonly Color colorGridEven = new Color32(255, 255, 255, 64);
        static readonly Color colorGridOdd = new Color32(255, 255, 255, 20);
        static readonly Color colorGridBetween = new Color32(255, 255, 255, 8);



        void DrawGrid()
        {
            bool round = true;
            if (e.type == EventType.MouseDrag)
            {
                // Drag View
                if (e.button == 2)
                {
                    offset += e.delta;
                    round = false;
                }

                // Zoom View
                if (e.button == 1)
                {
                    Zoom(Mathf.Abs(e.delta.x) > Mathf.Abs(e.delta.y) ? e.delta.x : -e.delta.y);
                    round = false;
                }
            }
            if (e.isScrollWheel)
            {
                Zoom(-e.delta.y * 3);
                round = false;
            }

            if (e.keyCode == KeyCode.C) offset = windowSize * 0.5f;
            if (e.keyCode == KeyCode.F)
            {
                if (e.shift) Frame(FrameMode.All);
                else
                {
                    if (selection.Count == 0) Frame(FrameMode.Profile);
                    else Frame(FrameMode.Selection);
                }
            }


            if (round && e.type != EventType.Layout && e.type != EventType.Repaint)
            {
                offset = new Vector2(Mathf.Round(offset.x), Mathf.Round(offset.y));
            }


            // Background Color
            GUI.color = colorBackground;
            GUI.DrawTexture(new Rect(0, 0, window.position.width, window.position.height), Texture2D.whiteTexture);
            GUI.color = Color.white;


            if (mode == Mode.UV)
            {
                if (tex != null)
                {
                    Vector2 min = -offset;
                    Vector2 max = new Vector2(1, -1) * zoom - offset;
                    Vector2 size = windowSize / (max - min);
                    Vector2 pos = (min / windowSize) * size;

                    GUI.color = new Color(0.3f, 0.3f, 0.3f, 1);
                    GUI.DrawTextureWithTexCoords(new Rect(new Vector2(0, windowSize.y), new Vector2(windowSize.x, -windowSize.y)), tex, new Rect(pos, size));

                    GUI.color = Color.white;


                    pos = WorldToGUIPoint(new Vector2(0, 1));
                    size = WorldToGUIPoint(new Vector2(1, 0)) - pos;

                    GUI.DrawTexture(new Rect(pos, size), tex);
                }
                else
                {
                    Vector2 pos = WorldToGUIPoint(new Vector2(0, 1));
                    Vector2 size = WorldToGUIPoint(new Vector2(1, 0)) - pos;

                    GUI.color = new Color(1, 1, 1, 0.1f);
                    GUI.DrawTexture(new Rect(pos, size), Texture2D.whiteTexture);
                }
            }


            Handles.color = colorGridCenter;
            Handles.DrawLine(new Vector2(0, offset.y), new Vector2(windowSize.x, offset.y));
            Handles.DrawLine(new Vector2(offset.x, 0), new Vector2(offset.x, windowSize.y));

            gridNumbers.fontSize = 14;
            gridNumbers.normal.textColor = Color.white;
            gridNumbers.contentOffset = new Vector2(2, 0);
            if (windowSize.y - offset.y - 16 > 0) EditorGUI.LabelField(new Rect(windowSize.x - 38, offset.y - 19, 50, 50), "0.0", gridNumbers);
            EditorGUI.LabelField(new Rect(offset.x, windowSize.y - 19, 50, 50), "0.0", gridNumbers);


            float border = 0.015625f * 1.5f;
            if (increment * zoom > windowSize.y * border) increment *= 0.5f;
            if (increment * zoom < windowSize.y * border) increment *= 2.0f;
            float inc = increment * zoom;


            Handles.color = colorGridOdd;
            gridNumbers.normal.textColor = new Color32(255, 255, 255, 150);
            gridNumbers.fontSize = 10;
            gridNumbers.contentOffset = new Vector2(2, 0);

            int count = 1;
            float current = inc;
            while (current < windowSize.x - offset.x)
            {

                int step = (count % 4);
                if (step == 0) Handles.color = colorGridEven;
                else if (step == 2) Handles.color = colorGridOdd;
                else Handles.color = colorGridBetween;

                Handles.DrawLine(new Vector2(current + offset.x, 0), new Vector2(current + offset.x, windowSize.y));
                if (step == 0) EditorGUI.LabelField(new Rect(current + offset.x, windowSize.y - 16, 50, 50), (GUIToWorldPoint(new Vector2(current + offset.x, 0))).x.ToString("0.####"), gridNumbers);

                current += inc;
                count++;
            }

            count = 1;
            current = -inc;
            while (current > -offset.x)
            {
                int step = (count % 4);
                if (step == 0) Handles.color = colorGridEven;
                else if (step == 2) Handles.color = colorGridOdd;
                else Handles.color = colorGridBetween;

                Handles.DrawLine(new Vector2(current + offset.x, 0), new Vector2(current + offset.x, windowSize.y));
                if (step == 0) EditorGUI.LabelField(new Rect(current + offset.x, windowSize.y - 16, 50, 50), (GUIToWorldPoint(new Vector2(current + offset.x, 0))).x.ToString("0.####"), gridNumbers);

                current -= inc;
                count++;
            }

            count = 1;
            current = -inc;
            while (current > -offset.y)
            {
                int step = (count % 4);
                if (step == 0) Handles.color = colorGridEven;
                else if (step == 2) Handles.color = colorGridOdd;
                else Handles.color = colorGridBetween;

                Handles.DrawLine(new Vector2(0, current + offset.y), new Vector2(windowSize.x, current + offset.y));
                if (step == 0 && current < windowSize.y - offset.y - 16) EditorGUI.LabelField(new Rect(windowSize.x - 38, current + offset.y - 14, 50, 50), (GUIToWorldPoint(new Vector2(0, current + offset.y))).y.ToString("0.####"), gridNumbers);

                current -= inc;
                count++;
            }


            gridNumbers.contentOffset = new Vector2(-3, 0);
            count = 1;
            current = inc;
            while (current < windowSize.y - offset.y)
            {
                int step = (count % 4);
                if (step == 0) Handles.color = colorGridEven;
                else if (step == 2) Handles.color = colorGridOdd;
                else Handles.color = colorGridBetween;

                Handles.DrawLine(new Vector2(0, current + offset.y), new Vector2(windowSize.x, current + offset.y));
                if (step == 0 && current < windowSize.y - offset.y - 16) EditorGUI.LabelField(new Rect(windowSize.x - 38, current + offset.y - 14, 50, 50), (GUIToWorldPoint(new Vector2(0, current + offset.y))).y.ToString("0.####"), gridNumbers);

                current += inc;
                count++;
            }
        } //===========================================================================================================================================================
    }
}
