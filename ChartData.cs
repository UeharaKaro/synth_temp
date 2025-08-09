

using UnityEngine;1
using System.Collections.Generic;

/// <summary>
/// 한 곡에 대한 전체 차트 정보를 담는 ScriptableObject 입니다.
/// ScriptableObject는 게임 오브젝트에 붙일 필요 없이, 프로젝트 내에 에셋 파일(.asset)로 독립적으로 존재할 수 있는 데이터 컨테이너입니다.
/// 이를 통해 여러 씬이나 게임 오브젝트에서 차트 데이터를 쉽게 공유하고 재사용할 수 있습니다.
/// </summary>
[CreateAssetMenu(fileName = "NewChart", menuName = "RhythmGame/Chart Data", order = 0)]
public class ChartData : ScriptableObject
{
    [Header("곡 기본 정보")]
    [Tooltip("게임 내에 표시될 곡의 제목입니다.")]
    public string songName;

    [Tooltip("게임 내에 표시될 아티스트의 이름입니다.")]
    public string artistName;

    [Tooltip("차트 에디터 및 게임에서 사용할 FMOD 이벤트의 경로입니다. [EventRef] 속성을 통해 Unity 인스펙터에서 편리하게 선택할 수 있습니다.")]
    [FMODUnity.EventRef]
    public string fmodEventPath;

    [Header("차트 상세 정보")]
    [Tooltip("곡의 기본 BPM(분당 비트 수)입니다. 이 값은 차트의 그리드를 계산하는 기준이 됩니다.")]
    public float initialBpm = 120.0f;

    [Header("핵심 데이터 리스트")]
    [Tooltip("이 차트를 구성하는 모든 노트의 데이터가 담긴 리스트입니다.")]
    public List<NoteData> notes = new List<NoteData>();

    [Tooltip("곡 중간에 BPM이 바뀌는 경우(변속)에 대한 정보 리스트입니다. 비어있을 경우 initialBpm만 사용됩니다.")]
    public List<BpmChangeEvent> bpmChanges = new List<BpmChangeEvent>();
}

/// <summary>
/// 곡 중간의 BPM 변경(변속) 이벤트를 정의하는 구조체입니다.
/// </summary>
[System.Serializable]
public struct BpmChangeEvent
{
    /// <summary>
    /// BPM이 변경될 정확한 시간 (단위: ms)
    /// </summary>
    [Tooltip("BPM이 변경될 시간 (단위: ms).")]
    public long timestamp;

    /// <summary>
    /// 이 시간부터 적용될 새로운 BPM 값
    /// </summary>
    [Tooltip("새롭게 적용될 BPM 값.")]
    public float newBpm;
}

