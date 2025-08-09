
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 게임의 전반적인 판정 및 키 설정 등을 관리합니다.
/// ScriptableObject로 만들어 에셋처럼 관리할 수 있습니다.
/// </summary>
[CreateAssetMenu(fileName = "GameSettings", menuName = "RhythmGame/Game Settings", order = 1)]

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 게임의 전반적인 설정을 관리하는 ScriptableObject 입니다.
/// 판정 난이도, 키 설정 등 차트 파일과는 독립적인 '게임의 규칙'을 정의합니다.
/// 이렇게 설정을 분리하면, 하나의 차트(.asset)를 여러 다른 난이도나 키 설정으로 플레이할 수 있게 됩니다.
/// </summary>
[CreateAssetMenu(fileName = "GameSettings", menuName = "RhythmGame/Game Settings", order = 1)]
public class GameSettings : ScriptableObject
{
    [Header("판정 모드별 시간 (단위: ms)")]
    [Tooltip("Normal 난이도의 판정 범위입니다.")]
    public JudgmentConfig NormalMode;

    [Tooltip("Hard 난이도의 판정 범위입니다.")]
    public JudgmentConfig HardMode;

    [Tooltip("Super 난이도의 판정 범위입니다.")]
    public JudgmentConfig SuperMode;

    [Header("키 설정")]
    [Tooltip("차트 에디터와 인게임에서 사용할 키 목록입니다. 에디터에서 레인 수에 맞춰 자동으로 조절됩니다.")]
    public List<KeyCode> keybindings = new List<KeyCode>();

    /// <summary>
    /// 게임의 판정 난이도(모드)를 정의합니다.
    /// </summary>
    public enum JudgmentMode
    {
        Normal,
        Hard,
        Super
    }

    /// <summary>
    /// 하나의 판정 모드에 대한 모든 판정 등급의 시간 범위를 담는 클래스입니다.
    /// </summary>
    [System.Serializable]
    public class JudgmentConfig
    {
        [Tooltip("인스펙터에 표시될 이름입니다.")]
        public string Name;

        [Tooltip("S-Perfect 판정의 시간 범위 (±ms). Normal 모드에서는 사용되지 않습니다.")]
        public float S_Perfect;

        [Tooltip("Perfect 판정의 시간 범위 (±ms).")]
        public float Perfect;

        [Tooltip("Great 판정의 시간 범위 (±ms).")]
        public float Great;

        [Tooltip("Good 판정의 시간 범위 (±ms).")]
        public float Good;

        [Tooltip("Bad 판정의 시간 범위 (±ms). Super 모드에서는 Miss로 처리됩니다.")]
        public float Bad;
    }

    // 이 함수는 ScriptableObject가 생성되거나, 인스펙터에서 값이 수정될 때, 또는 Reset 메뉴를 눌렀을 때 호출됩니다.
    // 사용자가 제공한 기본값으로 설정을 쉽게 초기화하기 위해 사용합니다.
    private void OnValidate()
    {
        NormalMode.Name = "Normal";
        NormalMode.S_Perfect = 0; // 해당 없음
        NormalMode.Perfect = 41.66f;
        NormalMode.Great = 83.33f;
        NormalMode.Good = 120f;
        NormalMode.Bad = 150f;

        HardMode.Name = "Hard";
        HardMode.S_Perfect = 16.67f;
        HardMode.Perfect = 32.25f;
        HardMode.Great = 62.49f;
        HardMode.Good = 100f; // 정보가 없어 임의 지정, 필요시 수정
        HardMode.Bad = 120f;

        SuperMode.Name = "Super";
        SuperMode.S_Perfect = 4.17f;
        SuperMode.Perfect = 12.50f;
        SuperMode.Great = 25.00f;
        SuperMode.Good = 62.49f;
        SuperMode.Bad = 0; // Miss 처리
    }
}

