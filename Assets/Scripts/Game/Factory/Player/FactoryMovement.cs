using UnityEngine;

public class FactoryMovement
{
    public static Vector3 Movement(float speed = 5f)
    {
        bool isUp, isDown, isLeft, isRight, isHorizontal, isVertical;
        float movementMultiplier, rightMultiplier, topMultiplier;
        isUp = isDown = isLeft = isRight = isHorizontal = isVertical = false;
        movementMultiplier = 1f;
        topMultiplier = 0f;
        rightMultiplier = 0f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) { isUp = true; }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) { isDown = true; }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) { isLeft = true; }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) { isRight = true; }
        if (isUp || isDown) { isVertical = true; }
        if (isLeft || isRight) { isHorizontal = true; }
        if (isVertical || isHorizontal) { movementMultiplier = 0.707f; }
        if (isUp) { topMultiplier += 1; }
        if (isDown) { topMultiplier -= 1; }
        if (isLeft) { rightMultiplier -= 1; }
        if (isRight) { rightMultiplier += 1; }
        return new Vector3(rightMultiplier, topMultiplier, 0) * movementMultiplier * speed;
    }
}
