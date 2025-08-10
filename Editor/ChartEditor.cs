using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ChartEditor : EditorWindow
{
    // 편집할 차트 데이터와 게임 설정
    private ChartData currentChart;
    private GameSettings gameSettings;

    // UI 상태 변수
    private int selectedLaneCount = 4;
    private Vector2 scrollPosition;

    [MenuItem("Window/Rhythm Game/Chart Editor")]
    public static void ShowWindow()
    {
        GetWindow<ChartEditor>("Chart Editor");
    }

    private void OnGUI()
    {
        // 전체 UI를 스크롤 뷰 안에 배치
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.LabelField("리듬 게임 차트 에디터", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // --- 1. 데이터 에셋 설정 ---
        EditorGUILayout.LabelField("1. 데이터 에셋 설정", EditorStyles.centeredGreyMiniLabel);
        currentChart = (ChartData)EditorGUILayout.ObjectField("현재 차트 (ChartData)", currentChart, typeof(ChartData), false);
        gameSettings = (GameSettings)EditorGUILayout.ObjectField("게임 설정 (GameSettings)", gameSettings, typeof(GameSettings), false);

        EditorGUILayout.Space(20);

        // currentChart나 gameSettings가 할당되지 않았으면 이후 UI를 그리지 않음
        if (currentChart == null || gameSettings == null)
        {
            EditorGUILayout.HelpBox("먼저 ChartData와 GameSettings 에셋을 할당해주세요.\nProject 창에서 우클릭 -> Create -> RhythmGame에서 생성할 수 있습니다.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        // --- 2. 차트 기본 정보 ---
        EditorGUILayout.LabelField("2. 차트 기본 정보", EditorStyles.centeredGreyMiniLabel);
        currentChart.songName = EditorGUILayout.TextField("곡 제목", currentChart.songName);
        currentChart.artistName = EditorGUILayout.TextField("아티스트", currentChart.artistName);
        currentChart.initialBpm = EditorGUILayout.FloatField("초기 BPM", currentChart.initialBpm);
        currentChart.audioClip = (AudioClip)EditorGUILayout.ObjectField("오디오 클립", currentChart.audioClip, typeof(AudioClip), false);

        EditorGUILayout.Space(20);

        // --- 3. 에디터 설정 ---
        EditorGUILayout.LabelField("3. 에디터 설정", EditorStyles.centeredGreyMiniLabel);

        // 레인 수 선택
        selectedLaneCount = EditorGUILayout.IntSlider("레인 수", selectedLaneCount, 4, 8);

        // 키 설정 UI
        if (gameSettings.keybindings.Count != selectedLaneCount)
        {
            // 리스트의 크기를 레인 수에 맞게 조절
            while (gameSettings.keybindings.Count < selectedLaneCount)
                gameSettings.keybindings.Add(KeyCode.None);
            while (gameSettings.keybindings.Count > selectedLaneCount)
                gameSettings.keybindings.RemoveAt(gameSettings.keybindings.Count - 1);
        }

        EditorGUILayout.LabelField("키 설정:");
        for (int i = 0; i < selectedLaneCount; i++)
        {
            



using UnityEngine;
using UnityEditor;
using FMODUnity;
using FMOD.Studio;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// FMOD와 연동하여 리듬 게임의 차트를 제작하는 에디터 클래스입니다.
/// </summary>
public class ChartEditor : EditorWindow
{
    #region Variables
    // 데이터 에셋
    private ChartData currentChart;
    private GameSettings gameSettings;

    // FMOD
    private EventInstance musicInstance;
    private PLAYBACK_STATE playbackState;
    private int currentPlaybackTimeMs = 0;
    private int totalPlaybackTimeMs = 0;

    // UI & 타임라인
    private int selectedLaneCount = 4;
    private Vector2 scrollPosition;
    private bool isSeeking = false;
    private Rect timelineRect;
    public enum GridSnap { None, Beat_1_4, Beat_1_8, Beat_1_16, Beat_1_32 }
    private GridSnap gridSnapValue = GridSnap.Beat_1_16;
    private float timelineZoom = 1.0f;
    private NoteData.NoteType currentNoteType = NoteData.NoteType.Normal;
    private Dictionary<int, NoteData> activeLongNotes = new Dictionary<int, NoteData>();
    #endregion

    [MenuItem("Window/Rhythm Game/Chart Editor")]
    public static void ShowWindow() { GetWindow<ChartEditor>("Chart Editor"); }

    #region Unity Lifecycle
    private void OnEnable() { EditorApplication.update += EditorUpdate; }
    private void OnDisable() { EditorApplication.update -= EditorUpdate; StopMusic(); }

    /// <summary> 에디터 프레임마다 호출되어 오디오 상태를 업데이트합니다. </summary>
    private void EditorUpdate()
    {
        if (isSeeking) return;
        if (musicInstance.isValid())
        {
            musicInstance.getPlaybackState(out playbackState);
            if (playbackState == PLAYBACK_STATE.PLAYING)
            {
                musicInstance.getTimelinePosition(out currentPlaybackTimeMs);
                Repaint();
            }
        }
    }

    /// <summary> 에디터의 모든 UI를 그리고 이벤트를 처리하는 메인 함수입니다. </summary>
    private void OnGUI()
    {
        HandleInputs();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Width(position.width), GUILayout.Height(position.height));
        
        DrawSetupUI();
        if (currentChart == null || gameSettings == null) { EditorGUILayout.EndScrollView(); return; }
        
        DrawAudioControlsUI();
        EditorGUILayout.Space(20);
        DrawTimelineUI();
        
        if (GUI.changed) { EditorUtility.SetDirty(currentChart); EditorUtility.SetDirty(gameSettings); }
        
        EditorGUILayout.EndScrollView();
    }
    #endregion

    #region Input Handling
    void HandleInputs()
    {
        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode != KeyCode.None) HandleKeyDown(e);
        if (e.type == EventType.KeyUp && e.keyCode != KeyCode.None) HandleKeyUp(e);
        if (timelineRect.Contains(e.mousePosition))
        {
            if (e.type == EventType.MouseDown && e.button == 0) HandleLeftClick(e.mousePosition);
            if (e.type == EventType.MouseDown && e.button == 1) HandleRightClick(e.mousePosition);
            Repaint();
        }
    }

    void HandleKeyDown(Event e)
    {
        int lane = gameSettings.keybindings.IndexOf(e.keyCode);
        if (lane != -1 && lane < selectedLaneCount)
        {
            if (currentNoteType == NoteData.NoteType.Normal) CreateNote(lane, currentPlaybackTimeMs);
            else if (currentNoteType == NoteData.NoteType.Long && !activeLongNotes.ContainsKey(lane)) StartLongNote(lane, currentPlaybackTimeMs);
            e.Use();
        }
    }

    void HandleKeyUp(Event e)
    {
        int lane = gameSettings.keybindings.IndexOf(e.keyCode);
        if (lane != -1 && activeLongNotes.ContainsKey(lane)) { EndLongNote(lane, currentPlaybackTimeMs); e.Use(); }
    }

    void HandleLeftClick(Vector2 mousePos)
    {
        int lane = GetLaneFromMousePosition(mousePos.y);
        long time = GetTimeFromMousePosition(mousePos.x);
        CreateNote(lane, time);
    }

    void HandleRightClick(Vector2 mousePos)
    {
        int lane = GetLaneFromMousePosition(mousePos.y);
        long time = GetTimeFromMousePosition(mousePos.x);
        NoteData noteToDelete = FindNoteAt(lane, time, 10f);
        if (noteToDelete != null) { currentChart.notes.Remove(noteToDelete); SortAndSaveChanges(); }
    }
    #endregion

    #region Note Management
    void CreateNote(int laneIndex, long timeMs)
    {
        if (currentNoteType == NoteData.NoteType.Normal)
        {
            currentChart.notes.Add(new NoteData { laneIndex = laneIndex, timestamp = SnapToGrid(timeMs), type = NoteData.NoteType.Normal, duration = 0 });
            SortAndSaveChanges();
        }
        else if (currentNoteType == NoteData.NoteType.Long)
        {
            Debug.Log("롱노트는 키보드를 누르고 떼서 입력해주세요.");
        }
    }

    void StartLongNote(int laneIndex, long timeMs)
    {
        NoteData newNote = new NoteData { laneIndex = laneIndex, timestamp = SnapToGrid(timeMs), type = NoteData.NoteType.Long, duration = 0 };
        currentChart.notes.Add(newNote);
        activeLongNotes[laneIndex] = newNote;
        SortAndSaveChanges();
    }

    void EndLongNote(int laneIndex, long timeMs)
    {
        NoteData note = activeLongNotes[laneIndex];
        note.duration = SnapToGrid(timeMs) - note.timestamp;
        if (note.duration < 50) { currentChart.notes.Remove(note); } 
        activeLongNotes.Remove(laneIndex);
        SortAndSaveChanges();
    }

    NoteData FindNoteAt(int lane, long time, float pixelThreshold)
    {
        float pixelsPerMs = (timelineRect.width / 1000.0f) * timelineZoom;
        long timeThreshold = (long)(pixelThreshold / pixelsPerMs);
        return currentChart.notes.FirstOrDefault(n => n.laneIndex == lane && Mathf.Abs(n.timestamp - time) < timeThreshold);
    }

    void SortAndSaveChanges()
    {
        currentChart.notes.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));
        EditorUtility.SetDirty(currentChart);
        Repaint();
    }
    #endregion

    #region UI Drawing
    void DrawSetupUI()
    {
        EditorGUILayout.LabelField("1. 데이터 에셋 설정", EditorStyles.centeredGreyMiniLabel);
        currentChart = (ChartData)EditorGUILayout.ObjectField("현재 차트 (ChartData)", currentChart, typeof(ChartData), false);
        gameSettings = (GameSettings)EditorGUILayout.ObjectField("게임 설정 (GameSettings)", gameSettings, typeof(GameSettings), false);
        EditorGUILayout.Space(20);
        if (currentChart == null || gameSettings == null) { EditorGUILayout.HelpBox("먼저 ChartData와 GameSettings 에셋을 할당해주세요.", MessageType.Info); return; }

        EditorGUILayout.LabelField("2. 차트 기본 정보", EditorStyles.centeredGreyMiniLabel);
        SerializedObject chartObject = new SerializedObject(currentChart);
        EditorGUILayout.PropertyField(chartObject.FindProperty("songName"));
        EditorGUILayout.PropertyField(chartObject.FindProperty("artistName"));
        EditorGUILayout.PropertyField(chartObject.FindProperty("initialBpm"));
        EditorGUILayout.PropertyField(chartObject.FindProperty("fmodEventPath"));
        chartObject.ApplyModifiedProperties();
        EditorGUILayout.Space(20);

        EditorGUILayout.LabelField("3. 에디터 설정", EditorStyles.centeredGreyMiniLabel);
        selectedLaneCount = EditorGUILayout.IntSlider("레인 수", selectedLaneCount, 4, 8);
        if (gameSettings.keybindings.Count != selectedLaneCount) { while (gameSettings.keybindings.Count < selectedLaneCount) gameSettings.keybindings.Add(KeyCode.None); while (gameSettings.keybindings.Count > selectedLaneCount) gameSettings.keybindings.RemoveAt(gameSettings.keybindings.Count - 1); }
        EditorGUILayout.LabelField("키 설정:");
        for (int i = 0; i < selectedLaneCount; i++) { gameSettings.keybindings[i] = (KeyCode)EditorGUILayout.EnumPopup($"  레인 {i + 1} 키", gameSettings.keybindings[i]); }
        EditorGUILayout.Space(20);
    }

    void DrawAudioControlsUI()
    {
        EditorGUILayout.LabelField("4. 오디오 제어", EditorStyles.centeredGreyMiniLabel);
        EditorGUI.BeginChangeCheck();
        var newTime = EditorGUILayout.IntSlider("타임라인", currentPlaybackTimeMs, 0, totalPlaybackTimeMs);
        if (EditorGUI.EndChangeCheck()) { isSeeking = true; currentPlaybackTimeMs = newTime; if(musicInstance.isValid()) musicInstance.setTimelinePosition(currentPlaybackTimeMs); }
        if (Event.current.type == EventType.MouseUp && isSeeking) { isSeeking = false; }
        EditorGUILayout.LabelField("시간", $"{currentPlaybackTimeMs / 1000.0f:00.000} / {totalPlaybackTimeMs / 1000.0f:00.000} 초");
        EditorGUILayout.BeginHorizontal();
        if (playbackState != PLAYBACK_STATE.PLAYING) { if (GUILayout.Button("Play")) PlayMusic(); }
        else { if (GUILayout.Button("Pause")) musicInstance.setPaused(true); }
        if (playbackState == PLAYBACK_STATE.PAUSED) { if (GUILayout.Button("Resume")) musicInstance.setPaused(false); }
        if (GUILayout.Button("Stop")) StopMusic();
        EditorGUILayout.EndHorizontal();
    }

    void DrawTimelineUI()
    {
        EditorGUILayout.LabelField("5. 타임라인 & 노트", EditorStyles.centeredGreyMiniLabel);
        currentNoteType = (NoteData.NoteType)EditorGUILayout.EnumPopup("노트 종류", currentNoteType);
        gridSnapValue = (GridSnap)EditorGUILayout.EnumPopup("그리드 분할", gridSnapValue);
        timelineZoom = EditorGUILayout.Slider("타임라인 확대", timelineZoom, 0.1f, 10f);
        timelineRect = GUILayoutUtility.GetRect(100, 10000, 300, 300);
        GUI.Box(timelineRect, "");
        Handles.BeginGUI();
        DrawGridLines();
        DrawNotes();
        DrawPlayhead();
        Handles.EndGUI();
    }

    void DrawNotes()
    {
        if (currentChart == null) return;
        float pixelsPerMs = (timelineRect.width / 1000.0f) * timelineZoom;
        float laneHeight = timelineRect.height / selectedLaneCount;
        foreach (var note in currentChart.notes)
        {
            float noteX = GetXForTime(note.timestamp);
            float noteY = timelineRect.y + note.laneIndex * laneHeight;
            float noteWidth = (note.type == NoteData.NoteType.Long && note.duration > 0) ? note.duration * pixelsPerMs : 10f;
            if (noteX < timelineRect.x - noteWidth || noteX > timelineRect.xMax) continue;
            Rect noteRect = new Rect(noteX, noteY, noteWidth, laneHeight);
            GUI.color = note.type == NoteData.NoteType.Long ? Color.green : Color.blue;
            GUI.DrawTexture(noteRect, EditorGUIUtility.whiteTexture);
            GUI.color = Color.white;
        }
    }

    void DrawGridLines()
    {
        if (currentChart == null || currentChart.initialBpm <= 0) return;
        float pixelsPerMs = (timelineRect.width / 1000.0f) * timelineZoom;
        float beatDurationMs = (60.0f / currentChart.initialBpm) * 1000.0f;
        if (beatDurationMs <= 0) return;
        float stepMs = beatDurationMs / 4.0f; // 1/16 beat
        long startTime = GetTimeFromMousePosition(timelineRect.x);
        long endTime = GetTimeFromMousePosition(timelineRect.xMax);
        int startStep = Mathf.FloorToInt(startTime / stepMs);
        int endStep = Mathf.CeilToInt(endTime / stepMs);
        for (int i = startStep; i < endStep; i++)
        {
            long time = (long)(i * stepMs);
            float x = GetXForTime(time);
            if (i % 16 == 0) Handles.color = Color.white;
            else if (i % 4 == 0) Handles.color = Color.gray;
            else Handles.color = new Color(0.3f, 0.3f, 0.3f);
            Handles.DrawLine(new Vector3(x, timelineRect.y), new Vector3(x, timelineRect.yMax));
        }
    }

    void DrawPlayhead()
    {
        float playheadX = GetXForTime(currentPlaybackTimeMs);
        Handles.color = Color.red;
        Handles.DrawLine(new Vector3(playheadX, timelineRect.y), new Vector3(playheadX, timelineRect.yMax));
    }
    #endregion

    #region Utility Methods
    long GetTimeFromMousePosition(float mouseX)
    {
        float pixelsPerMs = (timelineRect.width / 1000.0f) * timelineZoom;
        float relativeX = mouseX - timelineRect.x;
        return currentPlaybackTimeMs + (long)((relativeX - (timelineRect.width / 2)) / pixelsPerMs);
    }

    float GetXForTime(long timeMs)
    {
        float pixelsPerMs = (timelineRect.width / 1000.0f) * timelineZoom;
        return timelineRect.x + (timelineRect.width / 2) + (timeMs - currentPlaybackTimeMs) * pixelsPerMs;
    }

    int GetLaneFromMousePosition(float mouseY) { return Mathf.Clamp(Mathf.FloorToInt((mouseY - timelineRect.y) / (timelineRect.height / selectedLaneCount)), 0, selectedLaneCount - 1); }

    long SnapToGrid(long timeMs)
    {
        if (gridSnapValue == GridSnap.None || currentChart.initialBpm <= 0) return timeMs;
        float beatDurationMs = (60.0f / currentChart.initialBpm) * 1000.0f;
        float division = 4.0f; 
        if (gridSnapValue == GridSnap.Beat_1_8) division = 8.0f; else if (gridSnapValue == GridSnap.Beat_1_16) division = 16.0f; else if (gridSnapValue == GridSnap.Beat_1_32) division = 32.0f;
        float stepMs = beatDurationMs / (division / 4.0f);
        return (long)(Mathf.Round(timeMs / stepMs) * stepMs);
    }
    #endregion

    #region FMOD Control
    void PlayMusic()
    {
        if (currentChart == null || string.IsNullOrEmpty(currentChart.fmodEventPath)) return;
        StopMusic();
        musicInstance = RuntimeManager.CreateInstance(currentChart.fmodEventPath);
        EventDescription desc;
        musicInstance.getDescription(out desc);
        desc.getLength(out totalPlaybackTimeMs);
        musicInstance.start();
        if(currentPlaybackTimeMs > 0) musicInstance.setTimelinePosition(currentPlaybackTimeMs);
    }

    void StopMusic()
    {
        if (musicInstance.isValid()) { musicInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE); musicInstance.release(); musicInstance.clearHandle(); currentPlaybackTimeMs = 0; playbackState = PLAYBACK_STATE.STOPPED; }
    }
    #endregion
}



