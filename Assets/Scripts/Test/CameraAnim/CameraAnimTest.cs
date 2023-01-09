using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraAnimTest : MonoBehaviour
{
    public static CameraAnimTest Instance;

    Animator animator;
    void Start()
    {
        animator = GetComponent<Animator>();
        Instance = this;
    }

    public void Play()
    {
        animator.SetTrigger("Anim1");
    }
}
