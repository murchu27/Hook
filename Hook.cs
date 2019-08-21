using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hook : MonoBehaviour
{
    public HookCommon vars;

    //general variables
    public bool left;
    public Hook otherHook;
    private string hand;

    private Transform thisTransform;
    private Transform playerTransform;
    private Vector3 centPos;
    private Vector3 centPosInWorld;
    private Quaternion startRot;
    private Vector3 prevPos;
    private Quaternion prevRot;

    private bool extended;

    private Rigidbody2D rb2d;   

    private Vector3 drawPos;

    //aiming variables
    public GameObject reticle;

    private float aimxpoint;
    private float aimypoint;
    private float aimAng;

    private bool aiming;
    private bool blocked;

    //extension variables
    private bool attach;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(centPosInWorld, vars.aimradius);
    }

    void Start()
    {
        hand = (left ? "L" : "R");
        playerTransform = vars.player.GetComponent<Transform>();

        thisTransform = transform;
        rb2d = GetComponent<Rigidbody2D>();
        centPos = thisTransform.localPosition;
        startRot = thisTransform.localRotation;
        prevPos = Vector3.zero;
        prevRot = Quaternion.identity;

        extended = false;
        attach = false;
        Physics2D.IgnoreCollision(vars.player.GetComponent<Collider2D>(), this.GetComponent<Collider2D>());
        //Debug.Log(LayerMask.NameToLayer("Hook System"));
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Hook System"), LayerMask.NameToLayer("Hook System"));
    }

    void Update()
    {
        centPosInWorld = playerTransform.TransformPoint(centPos);
        if (!extended)
        {
            Aim();
            Extend();
        }
        if (Input.GetButtonDown(hand + "Retract")) Retract();

        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log(centPosInWorld);
        }
    }

    void Aim()
    {
        AimHook(getRawAimInput());
        AimReticle();
        vars.anim.SetBool("IsAiming", isAiming() || otherHook.isAiming() );
    }

    private Vector3 getRawAimInput()
    {
        Vector3 input = Vector3.zero;

        float x = Input.GetAxis(hand + "AimX");
        float y = Input.GetAxis(hand + "AimY");
        float mag = Vector2.SqrMagnitude(new Vector2(x, y));

        if (mag >= vars.aimdz) //if player is aiming
        {
            aiming = true;

            //calculate and store angle and position for arm due to input
            aimAng = Mathf.Atan2(y, x);
            aimxpoint = vars.aimradius * Mathf.Cos(aimAng);
            aimypoint = vars.aimradius * Mathf.Sin(aimAng);

            input.x = aimxpoint;
            input.y = aimypoint;
        }
        else //if not aiming
        {
            aiming = false;
            aimAng = (left ? Mathf.PI : 0f);
        }

        return input;
    }

    void AimHook(Vector3 input)
    {
        if (!blocked || !aiming)
        {
            prevPos = thisTransform.localPosition;
            prevRot = thisTransform.localRotation;

            //when adjusting local position, add input onto world position, then convert to local
            thisTransform.localPosition = playerTransform.InverseTransformPoint(centPosInWorld + input);

            Quaternion rot = Quaternion.AngleAxis(Mathf.Rad2Deg * aimAng, Vector3.forward);
            if (aiming) thisTransform.rotation = rot; //use world rotation so that rotation is based on input
            else thisTransform.localRotation = rot; //use local rotation so that rotation is based on player orientation
        }
        else
        {
            thisTransform.localPosition = prevPos;
            thisTransform.localRotation = prevRot;
        }
    }

    void AimReticle()
    {
        if (aiming)
        {
            Vector3 aimingAt = new Vector3(Mathf.Cos(aimAng), Mathf.Sin(aimAng), 0) * vars.extenddist;
            //reticle.transform.localPosition = centPos + aimingAt;
            reticle.transform.localPosition = playerTransform.InverseTransformPoint(centPosInWorld + aimingAt);
            reticle.transform.rotation = Quaternion.AngleAxis(Mathf.Rad2Deg * aimAng, Vector3.forward);
        }
        else
        {
            reticle.transform.localPosition = thisTransform.localPosition;
            reticle.transform.localRotation = thisTransform.localRotation;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log(collision.collider.tag);
        
        //if extended, check if it should attach
        if (extended)
        {
            attach = true;
        }
        //if retracted, stop it from aiming
        else
        {
            blocked = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log(other.name);
        //if extended, check if it should attach
        if (extended && other.tag == "attachable")
        {
            attach = true;
            rb2d.velocity = Vector2.zero;
        }
        //if retracted, stop it from aiming
        else
        {
            blocked = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        //if extended, retract
        if (extended)
        {

        }
        //if retracted, resume aiming
        else
        {
            blocked = false;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        //if extended, retract
        if (extended)
        {
            Debug.Log("detach");
            attach = false;
        }
        //if retracted, resume aiming
        else
        {
            blocked = false;
        }
    }

    void Extend()
    {
        if (Input.GetAxis(hand + "Extend") > 0f)
        {
            extended = true;
            Vector3 extendPos = new Vector3(Mathf.Cos(aimAng), Mathf.Sin(aimAng), 0) * vars.extenddist;
            StartCoroutine("Extending", playerTransform.InverseTransformPoint(centPosInWorld + extendPos));
        }
        else
        {
            //something
        }
    }

    IEnumerator Extending(Vector3 goTo)
    {
        extended = true;

        // Vector to store start marker for the journey.
        Vector3 from = transform.localPosition;

        // Time when the movement started.
        float startTime;

        // Total distance between the markers.
        float journeyLength;

        // Keep a note of the time the movement started.
        startTime = Time.time;

        // Calculate the journey length.
        journeyLength = Vector3.Distance(from, goTo);

        while (!attach)
        {
            // Distance moved = time * speed.
            float distCovered = (Time.time - startTime) * vars.extendspd;

            // Fraction of journey completed = current distance divided by total distance.
            float fracJourney = distCovered / journeyLength;
            if (fracJourney > 1) { break; }

            // Set our position as a fraction of the distance between the markers.
            thisTransform.localPosition = Vector3.Lerp(from, goTo, fracJourney);
            //Debug.Log(reticle.transform.localPosition.ToString() + goTo.ToString() + thisTransform.localPosition.ToString());
            yield return null;
        }

        if (!attach)
            Retract();
    }

    void Retract()
    {
        thisTransform.localPosition = centPos;
        Debug.Log("retracting");
        extended = false;
        attach = false;
    }

    public bool isAiming()
    {
        return aiming;
    }
}
