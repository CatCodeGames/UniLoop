using UnityEditor;
using UnityEngine;

namespace CatCode.PlayerLoops.EditorWindows
{
    /// <summary>
    /// Shows how many UniLoop tasks run in every runner and PlayerLoop point.
    /// Rows are runner types in execution order, columns are the six insertion points.
    /// </summary>
    public sealed class UniLoopWindow : EditorWindow
    {
        private enum ValueMode
        {
            Active,
            Peak
        }

        private const double RepaintInterval = 0.1;
        private const float NameWidth = 110;
        private const float CellWidth = 64;

        private static readonly PlayerLoopTiming[] Timings =
        {
            PlayerLoopTiming.FixedUpdate,
            PlayerLoopTiming.Update,
            PlayerLoopTiming.LateUpdate
        };

        private static readonly PlayerLoopPhase[] Phases =
        {
            PlayerLoopPhase.Early,
            PlayerLoopPhase.Late
        };

        private static readonly UniLoopRunnerType[] RunnerTypes =
        {
            UniLoopRunnerType.SlimLoop,
            UniLoopRunnerType.Loop,
            UniLoopRunnerType.TokenLoop,
            UniLoopRunnerType.CountedLoop,
            UniLoopRunnerType.While,
            UniLoopRunnerType.TokenWhile
        };

        private static readonly string[] RunnerNames =
        {
            "Slim", "Loop", "Loop + Token", "Counted", "While", "While + Token"
        };

        // Column headers carry the matching UniLoop property in the tooltip.
        private static readonly GUIContent[] PhaseHeaders =
        {
            new("Early", "UniLoop.FixedUpdate"), new("Late", "UniLoop.PostFixedUpdate"),
            new("Early", "UniLoop.Update"), new("Late", "UniLoop.PostUpdate"),
            new("Early", "UniLoop.LateUpdate"), new("Late", "UniLoop.PostLateUpdate")
        };

        private static readonly string[] ModeNames = { "Active", "Peak" };

        private const int PointCount = 6;

        // Last sampled metrics and peaks, indexed [runner type, point].
        private readonly RunnerMetrics[,] _metrics = new RunnerMetrics[RunnerTypes.Length, PointCount];
        private readonly int[,] _peaks = new int[RunnerTypes.Length, PointCount];

        [SerializeField] private ValueMode _mode;
        [SerializeField] private bool _hideEmptyRows = true;
        private double _nextRepaintTime;

        private GUIStyle _cellStyle;
        private GUIStyle _emptyCellStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _rowNameStyle;


        [MenuItem("Window/UniLoop/Runner Monitor")]
        public static void Open() => GetWindow<UniLoopWindow>("UniLoop");

        private void OnEnable()
            => EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        private void OnDisable()
            => EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
                ResetPeaks();

            Repaint();
        }

        // Metrics are sampled on every editor tick, so peaks are rarely missed;
        // the window itself repaints at a lower rate.
        private void Update()
        {
            if (!EditorApplication.isPlaying)
                return;

            Sample();

            var now = EditorApplication.timeSinceStartup;
            if (now < _nextRepaintTime)
                return;

            _nextRepaintTime = now + RepaintInterval;
            Repaint();
        }

        private void Sample()
        {
            for (int t = 0; t < Timings.Length; t++)
            {
                for (int p = 0; p < Phases.Length; p++)
                {
                    var point = t * Phases.Length + p;
                    for (int r = 0; r < RunnerTypes.Length; r++)
                    {
                        var metrics = UniLoop.GetMetrics(Timings[t], Phases[p], RunnerTypes[r]);
                        _metrics[r, point] = metrics;

                        if (metrics.TotalCount > _peaks[r, point])
                            _peaks[r, point] = metrics.TotalCount;
                    }
                }
            }
        }

        private void ResetPeaks()
            => System.Array.Clear(_peaks, 0, _peaks.Length);


        private void OnGUI()
        {
            EnsureStyles();
            DrawToolbar();

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Runner data is available in Play Mode.", MessageType.Info);
                return;
            }

            GUILayout.Space(6);
            DrawHeader();
            DrawRows();
            DrawTotalsRow();

            GUILayout.Space(6);
            EditorGUILayout.LabelField(
                _mode == ValueMode.Active
                    ? "Running tasks; +N are scheduled this frame and start on the next run."
                    : "Highest task count seen since entering Play Mode.",
                EditorStyles.miniLabel);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            _mode = (ValueMode)GUILayout.Toolbar((int)_mode, ModeNames, EditorStyles.toolbarButton, GUILayout.Width(110));
            _hideEmptyRows = GUILayout.Toggle(_hideEmptyRows, "Hide empty", EditorStyles.toolbarButton, GUILayout.Width(80));

            GUILayout.FlexibleSpace();

            if (EditorApplication.isPlaying)
            {
                GetTotals(out var active, out var pending);
                GUILayout.Label($"Active: {active}   Pending: {pending}", EditorStyles.miniLabel);
            }

            if (GUILayout.Button("Reset peak", EditorStyles.toolbarButton, GUILayout.Width(75)))
                ResetPeaks();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(NameWidth);
            for (int t = 0; t < Timings.Length; t++)
                GUILayout.Label(Timings[t].ToString(), _headerStyle, GUILayout.Width(CellWidth * Phases.Length));
            GUILayout.Label("Total", _headerStyle, GUILayout.Width(CellWidth));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(NameWidth);
            for (int point = 0; point < PointCount; point++)
                GUILayout.Label(PhaseHeaders[point], EditorStyles.centeredGreyMiniLabel, GUILayout.Width(CellWidth));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRows()
        {
            var drawnRows = 0;
            for (int r = 0; r < RunnerTypes.Length; r++)
            {
                var rowTotal = 0;
                for (int point = 0; point < PointCount; point++)
                    rowTotal += GetValue(r, point);

                if (_hideEmptyRows && rowTotal == 0)
                    continue;

                var rect = EditorGUILayout.BeginHorizontal();
                if (drawnRows % 2 == 1 && Event.current.type == EventType.Repaint)
                    EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.08f));

                GUILayout.Label(RunnerNames[r], _rowNameStyle, GUILayout.Width(NameWidth));
                for (int point = 0; point < PointCount; point++)
                    DrawCell(r, point);
                DrawNumber(rowTotal, _headerStyle);

                EditorGUILayout.EndHorizontal();
                drawnRows++;
            }

            if (drawnRows == 0)
                EditorGUILayout.LabelField("No tasks.", EditorStyles.centeredGreyMiniLabel);
        }

        private void DrawTotalsRow()
        {
            var rect = EditorGUILayout.BeginHorizontal();
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), new Color(0.5f, 0.5f, 0.5f, 0.5f));

            GUILayout.Label("Total", _rowNameStyle, GUILayout.Width(NameWidth));

            var grandTotal = 0;
            for (int point = 0; point < PointCount; point++)
            {
                var columnTotal = 0;
                for (int r = 0; r < RunnerTypes.Length; r++)
                    columnTotal += GetValue(r, point);

                grandTotal += columnTotal;
                DrawNumber(columnTotal, _headerStyle);
            }

            DrawNumber(grandTotal, _headerStyle);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCell(int runner, int point)
        {
            if (_mode == ValueMode.Peak)
            {
                DrawNumber(_peaks[runner, point], _cellStyle);
                return;
            }

            var metrics = _metrics[runner, point];
            if (metrics.TotalCount == 0)
            {
                GUILayout.Label("0", _emptyCellStyle, GUILayout.Width(CellWidth));
                return;
            }

            var text = metrics.PendingAddCount > 0
                ? $"{metrics.Count} +{metrics.PendingAddCount}"
                : metrics.Count.ToString();
            GUILayout.Label(text, _cellStyle, GUILayout.Width(CellWidth));
        }

        private void DrawNumber(int value, GUIStyle style)
            => GUILayout.Label(value.ToString(), value == 0 ? _emptyCellStyle : style, GUILayout.Width(CellWidth));


        private int GetValue(int runner, int point)
            => _mode == ValueMode.Peak ? _peaks[runner, point] : _metrics[runner, point].TotalCount;

        private void GetTotals(out int active, out int pending)
        {
            active = 0;
            pending = 0;
            for (int r = 0; r < RunnerTypes.Length; r++)
            {
                for (int point = 0; point < PointCount; point++)
                {
                    active += _metrics[r, point].Count;
                    pending += _metrics[r, point].PendingAddCount;
                }
            }
        }

        private void EnsureStyles()
        {
            if (_cellStyle != null)
                return;

            _cellStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };

            _emptyCellStyle = new GUIStyle(_cellStyle);
            _emptyCellStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);

            _headerStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };

            _rowNameStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft };
        }
    }
}
