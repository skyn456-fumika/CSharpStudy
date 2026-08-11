using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public int coinCount;                               // 코인 개수

    public GameObject startText;                        // 게임 시작 텍스트 오브젝트
    public GameObject startButton;                      // 게임 시작 버튼 오브젝트
    public TMP_Text coinText;                           // 코인 개수 표기 텍스트
    public GameObject clearText;                        // 게임 클리어 텍스트 오브젝트
    public GameObject gameOverText;                     // 게임 오버 텍스트 오브젝트
    public GameObject restartButton;                    // 리스타트 버튼 오브젝트

    public int totalCoinCount;                          // 모든 코인 개수

    public bool IsGameStarted { get; private set; }     // 게임 시작 여부
    public bool IsGameClear { get; private set; }       // 게임 클리어 여부
    public bool IsGameOver { get; private set; }        // 게임 오버 여부

    // 게임 시작 시 
    private void Start()
    {
        // Coin 오브젝트 개수를 확인하여 총 코인 개수로 설정 
        totalCoinCount = FindObjectsByType<Coin>(
            FindObjectsSortMode.None).Length;

        // 좌측 상단 coinText에 획득한 코인 개수 표시
        UpdateCoinText();

        // 시작 시에는 커서 풀어두기
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // 코인 획득
    public void AddCoin()
    {
        coinCount++;

        UpdateCoinText();

        if (coinCount >= totalCoinCount)
        {
            GameClear();
        }
    }

    // 코인 획득 수 갱신
    private void UpdateCoinText()
    {
        if (coinText != null)
        {
            coinText.text =
                $"Coin: {coinCount} / {totalCoinCount}";
        }
    }

    // 게임 클리어
    private void GameClear()
    {
        IsGameClear = true;

        Debug.Log("Game Clear!");

        if (clearText != null)
        {
            // 숨겨둔 clearText 표시
            clearText.SetActive(true);
        }

        if (restartButton != null)
        {
            // 숨겨진 리스타트 버튼 표시
            restartButton.SetActive(true);
        }

        // 커서 잠금 해제
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // 게임 재시작
    public void RestartGame()
    {
        Debug.Log("Restart 클릭됨");

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex);
    }

    // 게임 오버
    public void GameOver()
    {
        // 게임 클리어 또는 게임 오버 시 리턴
        // Hazard가 또 감지되거나 이벤트가 중복 실행되는 걸 막아줌
        if (IsGameClear || IsGameOver)
            return;

        // 게임 오버 선언
        IsGameOver = true;

        Debug.Log("Game Over!");

        if (gameOverText != null)
        {
            // 게임 오버 텍스트 오브젝트 표시
            gameOverText.SetActive(true);
        }

        if (restartButton != null)
        {
            // 재시작 버튼 오브젝트 표시
            restartButton.SetActive(true);
        }

        // 커서 잠금 해제
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // 게임 시작
    public void StartGame()
    {
        // 게임 시작 선언
        IsGameStarted = true;

        if (startText != null)
        {
            // 게임 시작 텍스트 숨기기
            startText.SetActive(false);
        }

        if (startButton != null)
        {
            // 게임 시작 버튼 숨기기
            startButton.SetActive(false);
        }

        // 커서 잠금
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}