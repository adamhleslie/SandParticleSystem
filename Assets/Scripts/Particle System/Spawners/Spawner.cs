using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct SimpleParticle
{
    public Vector3 position;
    public Vector3 velocity;

    public SimpleParticle (Vector3 pos, Vector3 vel)
    {
        position = pos;
        velocity = vel;
    }
}

public abstract class Spawner : MonoBehaviour
{
    public abstract SimpleParticle GenerateParticle();

    public SimpleParticle[] GenerateParticles (int numParticles)
    {
        SimpleParticle[] particles = new SimpleParticle[numParticles];
        for (int i = 0; i < numParticles; i++)
        {
            particles[i] = GenerateParticle();
        }

        return particles;
    }
}
