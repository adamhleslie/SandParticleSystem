using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerletParticle : MonoBehaviour
{
    public float timeToLive;
    public Vector3 priorPosition;

    public void ParticleUpdate (Vector3 verletAcceleration)
    {
        Vector3 currentPosition = transform.position;
        Vector3 implicitVelocity = (currentPosition - priorPosition);
        transform.position = currentPosition + implicitVelocity + verletAcceleration;
        priorPosition = currentPosition;
        //Debug.Log("p: " + currentPosition + ", v: " + implicitVelocity + ", a: " + verletAcceleration);
    }
}