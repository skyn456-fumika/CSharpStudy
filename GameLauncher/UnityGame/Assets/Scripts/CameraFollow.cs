using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    public Transform target;                        // 타겟(Player)
    private GameManager _gameManager;               // 게임 매니저 객체

    public float distance = 8f;                     // 길이
    public float targetHeight = 1f;                 // 높이
    public float mouseSensitivity = 120f;           // 마우스 감도

    public float minPitch = -20f;                   // 최소 y축 각도
    public float maxPitch = 60f;                    // 최대 y축 각도

    private float _yaw;
    private float _pitch = 20f;

    // Update / FixedUpdate 실행 후 실행
    // 카메라가 플레이어보다 먼저 움직이면 미묘하게 흔들리거나 어긋나는 느낌이 날 수 있어서, 추적 카메라는 LateUpdate()를 자주 사용
    private void LateUpdate()
    {
        if (target == null)
            return;

        // 마우스 회전만 막고 카메라 추적은 계속 유지
        if (Mouse.current != null
            && Cursor.lockState == CursorLockMode.Locked
            && _gameManager != null
            && _gameManager.IsGameStarted
            && !_gameManager.IsGameClear
            && !_gameManager.IsGameOver)
        {
            float mouseX = Mouse.current.delta.x.ReadValue();
            float mouseY = Mouse.current.delta.y.ReadValue();

            // 마우스 좌우 이동량을 누적해서 카메라의 Y축 회전값을 만듬
            // 플레이어 주변 좌우 회전
            _yaw += mouseX * mouseSensitivity * Time.deltaTime;
            // 위 / 아래 시점 변경
            _pitch -= mouseY * mouseSensitivity * Time.deltaTime;

            /* 
             * 카메라가 뒤집히거나 너무 아래로 내려가는 것 방지
             * Mathf.Clamp()는 값을 범위 안에 묶어둠
            */
            _pitch = Mathf.Clamp(
                _pitch,
                minPitch,
                maxPitch);
        }

        // Y축 회전을 만듬
        Quaternion rotation = Quaternion.Euler(
            _pitch,
            _yaw,
            0f);

        // 카메라가 바라볼 중심점을 먼저 만듬
        Vector3 targetPosition =
            target.position + Vector3.up * targetHeight;

        // 카메라는 그 중심을 기준으로 일정한 거리만큼 회전
        Vector3 cameraOffset =
            rotation * new Vector3(0f, 0f, -distance);

        transform.position =
            targetPosition + cameraOffset;

        // 카메라가 계속 Player 쪽을 바라보게 만듬
        transform.LookAt(targetPosition);
    }

    // 게임 시작 동작
    private void Start()
    {
        _gameManager = FindFirstObjectByType<GameManager>();
    }

    private void Update()
    {
        // ESC 누르면
        if (Keyboard.current != null
            && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // 마우스 잠금 해제
            Cursor.lockState = CursorLockMode.None;
            // 커서 표시
            Cursor.visible = true;
        }

        // 게임 클리어 또는 게임 오버 시 리턴
        if (_gameManager != null
            && (_gameManager.IsGameClear || _gameManager.IsGameOver))
        {
            return;
        }

        // 마우스 왼쪽 클릭
        if (_gameManager != null
            && _gameManager.IsGameStarted
            && !_gameManager.IsGameClear
            && !_gameManager.IsGameOver
            && Mouse.current != null
            && Mouse.current.leftButton.wasPressedThisFrame)
        {
            LockCursor();
        }
    }

    // 마우스 잠금
    private void LockCursor()
    {
        // 마우스 중앙 잠금
        Cursor.lockState = CursorLockMode.Locked;
        // 커서 숨김
        Cursor.visible = false;
    }
}