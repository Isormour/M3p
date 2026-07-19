using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    public float smooth = 3f;

    Transform standardPos;
    Transform frontPos;
    Transform jumpPos;

    bool bQuickSwitch = false;

    void Start()
    {
        standardPos = GameObject.Find("CamPos")?.transform;

        if (GameObject.Find("FrontPos"))
            frontPos = GameObject.Find("FrontPos").transform;

        if (GameObject.Find("JumpPos"))
            jumpPos = GameObject.Find("JumpPos").transform;

        if (standardPos != null)
        {
            transform.position = standardPos.position;
            transform.forward = standardPos.forward;
        }
    }

    void FixedUpdate()
    {
        if (Keyboard.current == null)
            return;

        // Fire1 (Left Ctrl)
        if (Keyboard.current.leftCtrlKey.isPressed)
        {
            SetCameraPositionFrontView();
        }
        // Fire2 (Left Alt)
        else if (Keyboard.current.leftAltKey.isPressed)
        {
            SetCameraPositionJumpView();
        }
        else
        {
            SetCameraPositionNormalView();
        }
    }

    void SetCameraPositionNormalView()
    {
        if (!bQuickSwitch)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                standardPos.position,
                Time.fixedDeltaTime * smooth
            );
            transform.forward = Vector3.Lerp(
                transform.forward,
                standardPos.forward,
                Time.fixedDeltaTime * smooth
            );
        }
        else
        {
            transform.position = standardPos.position;
            transform.forward = standardPos.forward;
            bQuickSwitch = false;
        }
    }

    void SetCameraPositionFrontView()
    {
        if (frontPos == null) return;

        bQuickSwitch = true;
        transform.position = frontPos.position;
        transform.forward = frontPos.forward;
    }

    void SetCameraPositionJumpView()
    {
        if (jumpPos == null) return;

        bQuickSwitch = false;
        transform.position = Vector3.Lerp(
            transform.position,
            jumpPos.position,
            Time.fixedDeltaTime * smooth
        );
        transform.forward = Vector3.Lerp(
            transform.forward,
            jumpPos.forward,
            Time.fixedDeltaTime * smooth
        );
    }
}
