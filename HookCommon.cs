using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HookCommon : MonoBehaviour
{
    //general variables
    public Animator anim;
    public GameObject player;
    public GameObject HookL;
    public GameObject HookR;

    //aiming variables
    public float aimradius;
    public float aimdz;

    //extension variables
    public float extendspd;
    public float extenddist;

    public static HookCommon instance { get; private set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

}
