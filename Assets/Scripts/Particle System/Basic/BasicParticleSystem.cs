using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicParticleSystem : MonoBehaviour
{
    public GameObject particlePrefab;
    public int initialParticles;
    public int timeToLive;
    [Header("Initial Position")]
    public Vector2 xRange;
    public Vector2 yRange;
    public Vector2 zRange;
    [Header("Initial Velocity")]
    public Vector2 xVelRange;
    public Vector2 yVelRange;
    public Vector2 zVelRange;

    private List<BasicParticle> particles;
    private bool paused = false;

    private void Start ()
    {
        particles = new List<BasicParticle>(initialParticles);

        for (int i = 0; i < initialParticles; i++)
        {
            Vector3 position = new Vector3(Random.Range(xRange.x, xRange.y), Random.Range(yRange.x, yRange.y), Random.Range(zRange.x, zRange.y));
            GameObject go = Instantiate(particlePrefab, position, Quaternion.identity);

            Vector3 velocity = new Vector3(Random.Range(xVelRange.x, xVelRange.y), Random.Range(yVelRange.x, yVelRange.y), Random.Range(zVelRange.x, zVelRange.y));
            BasicParticle particle = go.GetOrAddComponent<BasicParticle>();
            particle.timeToLive = timeToLive;
            particle.velocity = velocity;
            particles.Add(particle);
        }
    }

    void Update ()
    {
        if (Input.GetButton("Fire1"))
        {
            paused = !paused;
        }

        if (!paused)
        {
            UpdateParticles(Time.deltaTime);
        }
    }

    //private void GenerateParticles ()
    //{

    //}

    private void UpdateParticles (float t)
    {
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            BasicParticle particle = particles[i];
            particle.timeToLive -= t;

            if (particle.timeToLive <= 0)
            {
                particles.RemoveAt(i);
                Destroy(particle.gameObject);
            }
            else
            {
                particle.ParticleUpdate(t);
            }
        }
    }

    //private void RenderParticles ()
    //{
    //    for (int i = 0; i < particles.Count; i++)
    //    {

    //    }
    //}
}
