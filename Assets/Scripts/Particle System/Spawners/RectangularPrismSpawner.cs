using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RectangularPrismSpawner : Spawner
{
    [Header("Initial Position")]
    public Vector2 xRange;
    public Vector2 yRange;
    public Vector2 zRange;

    [Header("Initial Velocity")]
    public Vector2 xVelRange;
    public Vector2 yVelRange;
    public Vector2 zVelRange;

    public int initialParticles;
    public float timeToLive;

    public override SimpleParticle GenerateParticle ()
    {
        Vector3 position = new Vector3(Random.Range(xRange.x, xRange.y), Random.Range(yRange.x, yRange.y), Random.Range(zRange.x, zRange.y));
        Vector3 velocity = new Vector3(Random.Range(xVelRange.x, xVelRange.y), Random.Range(yVelRange.x, yVelRange.y), Random.Range(zVelRange.x, zVelRange.y));

        return new SimpleParticle(position, velocity, timeToLive);
    }

    public override SimpleParticle[] GenerateInitialParticles()
    {
        return GenerateParticles(initialParticles);
    }
}
