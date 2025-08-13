
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public class AssetEncryptor
{
    // 중요: 이 키를 다른 값으로 변경하세요. 이 키가 암호화 및 복호화에 사용됩니다.
    private const string EncryptionKey = "YourSecretKey";

    // 메뉴 아이템 경로 정의
    private const string MenuItemEncrypt = "Assets/Encrypt Audio File";

    /// <summary>
    /// 선택된 오디오 파일을 암호화하는 메뉴 커맨드
    /// </summary>
    [MenuItem(MenuItemEncrypt)]
    private static void EncryptSelectedAudioFile()
    {
        // 현재 선택된 에셋 가져오기
        Object selectedObject = Selection.activeObject;
        if (selectedObject == null || !(selectedObject is AudioClip))
        {
            EditorUtility.DisplayDialog("오류", "암호화할 오디오 파일(.wav, .ogg)을 선택해주세요.", "확인");
            return;
        }

        // 에셋 경로 가져오기
        string assetPath = AssetDatabase.GetAssetPath(selectedObject);
        string fullPath = Path.GetFullPath(assetPath);

        // 원본 파일 바이트 읽기
        byte[] fileBytes = File.ReadAllBytes(fullPath);

        // XOR 암호화 수행
        byte[] encryptedBytes = ProcessData(fileBytes);

        // 새 파일 경로 생성 (확장자를 .bytes로 변경)
        string newPath = AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(assetPath, ".bytes"));

        // 암호화된 파일 저장
        File.WriteAllBytes(newPath, encryptedBytes);

        // 에셋 데이터베이스 새로고침
        AssetDatabase.Refresh();

        Debug.Log($"암호화 완료: '{{assetPath}}' -> '{{newPath}}'");
        EditorUtility.DisplayDialog("성공", $"파일이 성공적으로 암호화되었습니다.\n원본: {{assetPath}}\n결과: {{newPath}}", "확인");
    }

    /// <summary>
    /// 메뉴 아이템 활성화 여부 검증
    /// </summary>
    [MenuItem(MenuItemEncrypt, true)]
    private static bool ValidateEncryptAudioFile()
    {
        // 선택된 객체가 AudioClip일 때만 메뉴 활성화
        return Selection.activeObject is AudioClip;
    }

    /// <summary>
    /// XOR 연산을 통해 데이터를 암호화하거나 복호화합니다.
    /// </summary>
    /// <param name="data">처리할 데이터</param>
    /// <returns>처리된 데이터</returns>
    private static byte[] ProcessData(byte[] data)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(EncryptionKey);
        byte[] result = new byte[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            result[i] = (byte)(data[i] ^ keyBytes[i % keyBytes.Length]);
        }

        return result;
    }
}

/// <summary>
/// 런타임에서 암호화된 오디오 파일을 로드하는 헬퍼 클래스
/// </summary>
public static class RuntimeAudioLoader
{
    // 중요: AssetEncryptor.cs의 EncryptionKey와 동일한 키를 사용해야 합니다.
    private const string EncryptionKey = "YourSecretKey";

    /// <summary>
    /// 암호화된 TextAsset을 AudioClip으로 변환합니다.
    /// 참고: 이 방법은 .wav 파일에 가장 안정적으로 동작합니다. .ogg는 추가적인 처리가 필요할 수 있습니다.
    /// </summary>
    /// <param name="encryptedAudioAsset">.bytes로 끝나는 암호화된 오디오 에셋</param>
    /// <returns>재생 가능한 AudioClip</returns>
    public static AudioClip LoadEncryptedAudio(TextAsset encryptedAudioAsset)
    {
        if (encryptedAudioAsset == null)
        {
            Debug.LogError("암호화된 오디오 에셋이 null입니다.");
            return null;
        }

        // 데이터 복호화
        byte[] decryptedBytes = ProcessData(encryptedAudioAsset.bytes);

        // 복호화된 .wav 바이트를 AudioClip으로 로드
        // 이 예제는 WavUtility를 사용하며, .wav 파일에 최적화되어 있습니다.
        AudioClip audioClip = WavUtility.ToAudioClip(decryptedBytes);

        return audioClip;
    }

    /// <summary>
    /// XOR 연산을 통해 데이터를 복호화합니다.
    /// </summary>
    private static byte[] ProcessData(byte[] data)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(EncryptionKey);
        byte[] result = new byte[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            result[i] = (byte)(data[i] ^ keyBytes[i % keyBytes.Length]);
        }

        return result;
    }
}

/// <summary>
/// .wav 파일 바이트 배열을 AudioClip으로 변환하는 유틸리티 클래스
/// 출처: https://github.com/deadlyfingers/UnityWav
/// </summary>
public static class WavUtility
{
    public static AudioClip ToAudioClip(byte[] fileBytes)
    {
        // WAV 헤더 파싱
        int headerSize = 44; // 기본 WAV 헤더 크기
        int channels = fileBytes[22];
        int sampleRate = System.BitConverter.ToInt32(fileBytes, 24);
        int bitDepth = System.BitConverter.ToInt16(fileBytes, 34);

        // 데이터 시작 위치 찾기
        int dataChunkPos = 12; // "data" 문자열 위치
        while (!(fileBytes[dataChunkPos] == 'd' && fileBytes[dataChunkPos + 1] == 'a' && fileBytes[dataChunkPos + 2] == 't' && fileBytes[dataChunkPos + 3] == 'a'))
        {
            dataChunkPos += 4;
            int chunkSize = System.BitConverter.ToInt32(fileBytes, dataChunkPos);
            dataChunkPos += 4 + chunkSize;
        }
        int dataSize = System.BitConverter.ToInt32(fileBytes, dataChunkPos + 4);
        int dataStart = dataChunkPos + 8;

        // 오디오 데이터 추출
        float[] data = new float[dataSize / (bitDepth / 8)];
        
        for (int i = 0; i < data.Length; i++)
        {
            int sampleIndex = dataStart + i * (bitDepth / 8);
            if (bitDepth == 16)
            {
                short sample = System.BitConverter.ToInt16(fileBytes, sampleIndex);
                data[i] = sample / 32768f;
            }
            else if (bitDepth == 8)
            {
                data[i] = (fileBytes[sampleIndex] - 128) / 128f;
            }
        }

        AudioClip audioClip = AudioClip.Create("DecryptedWav", data.Length / channels, channels, sampleRate, false);
        audioClip.SetData(data, 0);
        return audioClip;
    }
}
