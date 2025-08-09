
using System;
using UnityEngine;

/// <summary>
/// 리듬 게임의 개별 노트를 정의하는 데이터 클래스입니다.
/// 이 클래스의 인스턴스 하나가 노트 하나에 해당합니다.
/// [Serializable] 속성은 이 클래스가 Unity 인스펙터에 노출되고, 파일로 저장(직렬화)될 수 있음을 의미합니다.
/// </summary>
[Serializable]
public class NoteData
{
    /// <summary>
    /// 노트의 종류를 정의합니다.
    /// </summary>
    public enum NoteType
    {
        /// <summary>
        /// 한 번만 누르는 일반적인 노트입니다.
        /// </summary>
        Normal,
        /// <summary>
        /// 일정 시간 동안 누르고 있어야 하는 롱노트입니다.
        /// </summary>
        Long
    }

    /// <summary>
    /// 이 노트의 종류 (일반 노트 또는 롱노트)
    /// </summary>
    [Tooltip("노트의 종류를 선택합니다 (일반/롱노트).")]
    public NoteType type = NoteType.Normal;

    /// <summary>
    /// 이 노트가 나타날 레인(세로줄)의 번호입니다.
    /// 0부터 시작합니다 (예: 4키 게임의 경우 0, 1, 2, 3).
    /// </summary>
    [Tooltip("노트가 위치할 레인의 인덱스 (0부터 시작).")]
    public int laneIndex = 0;

    /// <summary>
    /// 곡의 시작 지점으로부터 이 노트의 판정이 시작되어야 하는 정확한 시간입니다.
    /// 밀리초(ms) 단위로 저장하여 정밀도를 높입니다.
    /// </summary>
    [Tooltip("곡 시작부터 노트 판정까지의 시간 (단위: ms).")]
    public long timestamp = 0;

    /// <summary>
    /// 이 노트가 롱노트일 경우, 누르고 있어야 하는 총 시간입니다.
    /// 일반 노트의 경우 이 값은 0입니다.
    /// 밀리초(ms) 단위로 저장됩니다.
    /// </summary>
    [Tooltip("롱노트의 지속 시간 (단위: ms). 일반 노트는 0.")]
    public long duration = 0;
}
