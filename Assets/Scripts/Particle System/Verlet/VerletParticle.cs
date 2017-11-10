using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerletParticle
{
    public Vector3 position;
    public Vector3 priorPosition;
    public float timeToLive;

    public VerletParticle (SimpleParticle simpleParticle)
    {
        position = simpleParticle.position;
        priorPosition = simpleParticle.position - (simpleParticle.velocity * Time.fixedDeltaTime);
        timeToLive = simpleParticle.timeToLive;
    }

    public void ParticleUpdate (Vector3 verletAcceleration)
    {
        Vector3 currentPosition = position;
        Vector3 implicitVelocity = (currentPosition - priorPosition);
        position = currentPosition + implicitVelocity + verletAcceleration;
        priorPosition = currentPosition;
    }
}