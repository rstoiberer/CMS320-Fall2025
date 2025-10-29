using UnityEngine;
public class move : MonoBehaviour
{
Rigidbody2D pad;
Vector2 initial;
public float displacement;

// Start is called once before the first execution of Update after the MonoBehaviour is created
void Start()
{
    pad = GetComponent<Rigidbody2D>();
    initial = pad.transform.localPosition;
}
// Update is called once per frame
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