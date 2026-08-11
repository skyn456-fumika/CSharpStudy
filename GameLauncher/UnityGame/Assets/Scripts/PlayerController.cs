using UnityEngine;
using UnityEngine.InputSystem;

// MonoBehaviour 상속해야 이 스크립트를 Unity의 GameObject에 Component로 붙일 수 있다
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 3f;                // 이동속도
    public float jumpForce = 6f;                // 점프력
    public float rotationSpeed = 10f;           // 회전 속도
    public float fallY = -5f;                   // 낙하 기준

    public Transform cameraTransform;           // 카메라 기준 이동
    private Rigidbody _rigidbody;               // 오브젝트 이동 관련 컴포넌트
    private PlayerInputActions _inputActions;   // Input Action Map (W/A/S/D, Jump)
    private GameManager _gameManager;           // 게임 매니저 객체

    private Vector3 _moveDirection;

    private bool _isGrounded;                   // 땅에 닿고 있는지(점프 상태인지 아닌지)
    private bool _jumpRequested;                // 점프 요청

    // Unity가 이 Component를 초기화할 때 실행
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _inputActions = new PlayerInputActions();

        _gameManager = FindFirstObjectByType<GameManager>();
    }

    // Player Action Map의 입력 감지를 켜고 부분
    private void OnEnable()
    {
        _inputActions.Player.Enable();
    }

    // Player Action Map의 입력 감지를 끄는 부분
    private void OnDisable()
    {
        _inputActions.Player.Disable();
    }


    // 게임이 실행되는 동안 매 프레임마다 호출되는 메서드
    // 키보드 입력처럼 매 프레임 처리할 것
    private void Update()
    {
        if (_gameManager != null
            && !_gameManager.IsGameClear
            && !_gameManager.IsGameOver
            && transform.position.y < fallY)
        {
            _gameManager.GameOver();
        }

        // 게임 시작 또는 게임 클리어, 오버 시 입력 초기화 후 종료
        if (_gameManager != null
            && (!_gameManager.IsGameStarted
                || _gameManager.IsGameClear
                || _gameManager.IsGameOver))
        {
            _moveDirection = Vector3.zero;
            _jumpRequested = false;
            return;
        }

        // Input Action Map 설정(PlayerInputActions - Move - 2D Actions - W/A/S/D)
        Vector2 input = _inputActions.Player.Move.ReadValue<Vector2>();
        
        /*
            W → 카메라가 보는 앞쪽
            S → 카메라 기준 뒤쪽
            A → 카메라 기준 왼쪽
            D → 카메라 기준 오른쪽
        */
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        _moveDirection =
            (forward * input.y + right * input.x).normalized;

        // Input Action Map 설정(PlayerInputActions - Jump - Binding - Space)
        if (_inputActions.Player.Jump.WasPressedThisFrame()
            && _isGrounded)
        {
            _jumpRequested = true;
        }
    }

    // Rigidbody 같은 물리 처리
    private void FixedUpdate()
    {
        // 현재 velocity 가져오기
        Vector3 velocity = _rigidbody.linearVelocity;

        // 게임 시작 또는 게임 클리어, 오버 시 X/Z 정지 후 종료
        if (_gameManager != null
            && (!_gameManager.IsGameStarted
                || _gameManager.IsGameClear
                || _gameManager.IsGameOver))
        {
            velocity.x = 0f;
            velocity.z = 0f;

            _rigidbody.linearVelocity = velocity;
            return;
        }

        velocity.x = _moveDirection.x * moveSpeed;
        velocity.z = _moveDirection.z * moveSpeed;

        if (_jumpRequested)
        {
            velocity.y = jumpForce;

            _jumpRequested = false;
            _isGrounded = false;
        }

        _rigidbody.linearVelocity = velocity;

        if (_moveDirection != Vector3.zero)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(_moveDirection);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime);
        }
    } 

    // Tag는 GameObject에게 붙이는 분류 이름
    // 특정 오브젝트(Collision)과 충돌할 때
    private void OnCollisionEnter(Collision collision)
    {
        // Ground 태그를 가진 Plane에 착지(충돌) 시
        if (collision.gameObject.CompareTag("Ground"))
        {
            _isGrounded = true;
        }
    }

    // 특정 오브젝트(Collision)과의 충돌 상태에서 벗어날 때
    private void OnCollisionExit(Collision collision)
    {
        // Ground 태그를 가진 Plane에서 떨어질 시(충돌 상태에서 벗어날 때)
        if (collision.gameObject.CompareTag("Ground"))
        {
            _isGrounded = false;
        }
    }
}