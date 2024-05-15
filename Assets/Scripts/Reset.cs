using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Reset : MonoBehaviour
{
    public GameObject player;

    public InputActionReference press = null;

    private void Awake()
    {
        press.action.started += ResetPosition;
    }

    private void OnDestroy()
    {
        press.action.started -= ResetPosition;
    }

    public void ResetPosition(InputAction.CallbackContext ctx)
    {
        GameObject closest = ClosestObject(player.transform.position);
        player.transform.position = new Vector3(closest.transform.position.x,
            closest.transform.position.y + 2, closest.transform.position.x);
    }
    
    static GameObject ClosestObject(Vector3 position)
    {
        float range = 30;
        var list = new List<GameObject>();
        Collider[] found = Physics.OverlapSphere(position, range);

        foreach (var collider in found)
        {
            list.Add(collider.gameObject);
        }

        return ClosestObject(position, list);
    }

    static GameObject ClosestObject(Vector3 origin, IEnumerable<GameObject> gameObjects)
    {
        GameObject closest = null;
        float closestSqrDist = 0f;

        foreach (var gameObject in gameObjects)
        {
            float sqrDist = (gameObject.transform.position - origin).sqrMagnitude;
            bool isValid = gameObject.name.Contains("Road");

            if ((!closest || sqrDist < closestSqrDist) && isValid)
            {
                closest = gameObject;
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }
}
