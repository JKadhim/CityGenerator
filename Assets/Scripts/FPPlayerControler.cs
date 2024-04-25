using UnityEngine;

public class Player_Controller : MonoBehaviour
{
    PlayerControls input_control;
    Vector2 move;
    readonly float movement_force = 50000;
    // Start is called before the first frame update
    void Start()
    {
        input_control = new PlayerControls();
        input_control.Ground.Enable();
        move = Vector2.zero;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        playermove();
    }
    void playermove()
    {
        move = input_control.Ground.Move.ReadValue<Vector2>();
    }
}