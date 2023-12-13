using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coin : MonoBehaviour
{
    private const int POINT = 1;

    private bool isBeenAcquired;

    public bool notGet;

    void Start()
    {
        isBeenAcquired = false;
        notGet = false;
    }

    void Update()
    {
        transform.Rotate(Vector3.up * 50 * Time.deltaTime, Space.World);

        if (isBeenAcquired) Destroy(gameObject);
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Player")
        {
            if (this.notGet) return;
            this.isBeenAcquired = true;
            collision.gameObject.GetComponentInParent<PlayerSystem>().AddPoint(POINT);
        }
    }

}
