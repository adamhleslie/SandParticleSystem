using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerletParticle
{
    public float timeToLive;
    public Vector3 position;
    public Vector3 priorPosition;

    public void ParticleUpdate (Vector3 verletAcceleration)
    {
        Vector3 currentPosition = position;
        Vector3 implicitVelocity = (currentPosition - priorPosition);
        position = currentPosition + implicitVelocity + verletAcceleration;
        priorPosition = currentPosition;
        //Debug.Log("p: " + currentPosition + ", v: " + implicitVelocity + ", a: " + verletAcceleration);
    }
}