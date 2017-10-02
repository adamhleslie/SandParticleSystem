using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicParticle : MonoBehaviour
{
    public float timeToLive;
    public Vector3 velocity;

    public void ParticleUpdate (float t)
    {
        transform.Translate(velocity * t); 
    }
}
