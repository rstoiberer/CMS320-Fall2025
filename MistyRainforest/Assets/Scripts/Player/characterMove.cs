using UnityEngine;
public class move : MonoBehaviour
{
Rigidbody2D pad;
Vector2 initial;
public float displacement;

void Start()
{
    pad = GetComponent<Rigidbody2D>();
    initial = pad.transform.localPosition;
}
void Update()
{
    if ((Input.GetKey(KeyCode.RightArrow))){
        if (initial.x<=9.75)
            initial.x=initial.x+displacement;
    }
    else if((Input.GetKey(KeyCode.LeftArrow))){
        if (initial.x>-9.75)
            initial.x=initial.x-displacement;
    }
    pad.MovePosition(initial);
}
}