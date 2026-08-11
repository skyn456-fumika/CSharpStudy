using UnityEngine;

public class Coin : MonoBehaviour
{
    public float rotationSpeed = 180f;

    private void Update()
    {
        transform.Rotate(
            0f,
            rotationSpeed * Time.deltaTime,
            0f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        // 현재 Scene 안에서 GameManager 컴포넌트를 가진 객체를 찾아서 가져온다는 뜻
        GameManager gameManager =
            FindFirstObjectByType<GameManager>();

        if (gameManager != null)
        {
            gameManager.AddCoin();
        }

        Destroy(gameObject);
    }
}