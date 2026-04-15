using CatCode.PlayerLoops;
using UnityEditor;
using UnityEngine;

public class UniLoopWindow : EditorWindow
{
    private Vector2 _scroll;

    [MenuItem("Window/UniLoop Tracker")]
    public static void Open() => GetWindow<UniLoopWindow>("Loop Runner Debugger");

    private void Update() => Repaint();

    private void OnGUI()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Loop Runner Debugger доступен только в Play Mode.", MessageType.Info);
            return;
        }

        var snapshots = UniLoop.GetSnapshot();
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        DrawGlobalHeader();
        if (snapshots != null && snapshots.Length > 0)
            for (int i = 0; i < snapshots.Length; i++)
                DrawRunnerBlock(snapshots[i]);        
        else
            GUILayout.Label("No snapshot data available.");
        EditorGUILayout.EndScrollView();
    }

    private void DrawGlobalHeader()
    {
        GUILayout.Space(6);
        EditorGUILayout.LabelField("Loop Runner Snapshot", EditorStyles.boldLabel);
        GUILayout.Space(4);
    }

    private void DrawRunnerBlock(TimingRunnerSnapshot runner)
    {
        // Position header
        GUILayout.Space(6);
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"Timing: {runner.Position.Timing}", GUILayout.Width(140));
        EditorGUILayout.LabelField($"Phase: {runner.Position.Phase}", GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();

        // Columns header for processors
        EditorGUILayout.BeginHorizontal();
        DrawCol("Type", 180);
        DrawCol("Total", 60);
        DrawCol("Active", 60);
        DrawCol("Pending", 60);
        EditorGUILayout.EndHorizontal();

        // Processor rows
        var processors = runner.RunnerSnapshots;
        for (int p = 0; p < processors.Length; p++)
        {
            var ps = processors[p];
            EditorGUILayout.BeginHorizontal();
            DrawCol(ps.Type.ToString(), 180);
            DrawCol(ps.Metrics.TotalCount.ToString(), 60);
            DrawCol(ps.Metrics.Count.ToString(), 60);
            DrawCol(ps.Metrics.PendingAddCount.ToString(), 60);
            EditorGUILayout.EndHorizontal();
        }

        GUILayout.Space(4);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
    }

    private void DrawCol(string text, float width)
    {
        GUILayout.Label(text, GUILayout.Width(width));
    }
}
