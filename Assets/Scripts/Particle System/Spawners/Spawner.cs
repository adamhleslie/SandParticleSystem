using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct SimpleParticle
{
    public Vector3 position;
    public Vector3 velocity;
    public float timeToLive;

    public SimpleParticle (Vector3 pos, Vector3 vel, float ttl)
    {
        position = pos;
        velocity = vel;
        timeToLive = ttl;
    }
}

public abstract class Spawner : MonoBehaviour
{
    [SerializeField] protected int initialParticles;
    [SerializeField] protected int spawnedParticles;

    public abstract SimpleParticle GenerateParticle();

    public SimpleParticle[] GenerateParticles ()
    {
        return GenerateParticles(spawnedParticles);
    }

    public SimpleParticle[] GenerateParticles (int numParticles)
    {
        SimpleParticle[] particles = new SimpleParticle[numParticles];
        for (int i = 0; i < numParticles; i++)
        {
            particles[i] = GenerateParticle();
        }

        return particles;
    }

    public SimpleParticle[] GenerateInitialParticles()
    {
        return GenerateParticles(initialParticles);
    }
}
