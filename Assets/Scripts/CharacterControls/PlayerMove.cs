using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public CharacterController controller;

    public float speed = 12f;

    private void Update()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        controller.Move(move * Time.deltaTime * speed);
    }
}
