using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerletParticleSystem : MonoBehaviour
{
    [SerializeField] private Spawner spawner;
    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private int initialParticles;
    [SerializeField] private int timeToLive;

    [Header("Acceleration")]
    [SerializeField] private Vector3 acceleration;
    [SerializeField] private Vector3 deltaAcceleration;

    private List<VerletParticle> particles;
    private bool paused = false;

    private void Start ()
    {
        particles = new List<VerletParticle>(initialParticles);
        GenerateParticles();
    }

    void FixedUpdate ()
    {
        if (Input.GetButton("Fire1"))
        {
            paused = !paused;
        }

        if (!paused)
        {
            UpdateParticles(Time.fixedDeltaTime);
        }
    }

    private void GenerateParticles()
    {
        SimpleParticle[] simpleParticles = spawner.GenerateParticles(initialParticles);
        for (int i = 0; i < initialParticles; i++)
        {
            SimpleParticle simpleParticle = simpleParticles[i];
            GameObject go = Instantiate(particlePrefab, simpleParticle.position, Quaternion.identity);

            // Add Verlet Specific Behavior
            VerletParticle particle = go.GetOrAddComponent<VerletParticle>();
            particle.priorPosition = simpleParticle.position - (simpleParticle.velocity * Time.fixedDeltaTime);
            particle.timeToLive = timeToLive;

            particles.Add(particle);
        }
    }

    private void UpdateParticles (float t)
    {
        Vector3 verletAcceleration = acceleration * t * t;
        //if (verletAcceleration.magnitude > .5f)
        //    Debug.Log("werid");
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            VerletParticle particle = particles[i];
            particle.timeToLive -= t;

            if (particle.timeToLive <= 0)
            {
                particles.RemoveAt(i);
                Destroy(particle.gameObject);
            }
            else
            {
                particle.ParticleUpdate(verletAcceleration);
            }
        }

        acceleration += deltaAcceleration * t;
    }

    //private void RenderParticles ()
    //{
    //    for (int i = 0; i < particles.Count; i++)
    //    {

    //    }
    //}
}
