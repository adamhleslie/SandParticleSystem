using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerletParticleSystem : MonoBehaviour
{
    const int kMaxParticles = 65000;

    [SerializeField] private Spawner[] spawners;
    [SerializeField] private int maxParticles;
    [SerializeField] private int timeToLive;

    [Header("Acceleration")]
    [SerializeField] private Vector3 acceleration;
    [SerializeField] private Vector3 deltaAcceleration;

    private List<VerletParticle> particles;
    private int remainingParticles = 0;
    private bool paused = false;
    private Mesh mesh;

    private void Start ()
    {
        if (spawners.Length == 0)
        {
            Debug.LogError("VerletParticleSystem.Start: No spawners found", this);
        }

        if (maxParticles < 0 || maxParticles > kMaxParticles)
        {
            Debug.LogError("VerletParticleSystem.Start: maxParticles set to invalid value, " + maxParticles + ", reset to " + kMaxParticles, this);
            maxParticles = kMaxParticles;
        }

        remainingParticles = maxParticles;
        particles = new List<VerletParticle>(maxParticles);

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

    private void GenerateParticles ()
    {
        for (int i = 0; i < spawners.Length && remainingParticles > 0; i++)
        {
            SimpleParticle[] simpleParticles = spawners[i].GenerateInitialParticles();
            if (simpleParticles != null)
            {
                for (int j = 0; j < remainingParticles; j++)
                {
                    particles.Add(new VerletParticle(simpleParticles[j]));
                }
                remainingParticles -= simpleParticles.Length;

            }
        }

        for (int i = 0; i < initialParticles; i++)
        {
            SimpleParticle simpleParticle = simpleParticles[i];

            // Add Verlet Specific Behavior
            VerletParticle particle = new VerletParticle();


            particle);
        }

        // Set up for rendering particles
        Vector3[] vertices = new Vector3[initialParticles];
        int[] indices = new int[initialParticles];
        for (int i = 0; i < initialParticles; i++)
        {
            vertices[i] = simpleParticles[i].position;
            indices[i] = i;
        }

        mesh = new Mesh();
        gameObject.GetOrAddComponent<MeshFilter>().mesh = mesh;
        mesh.vertices = vertices;
        mesh.SetIndices(indices, MeshTopology.Points, 0);
    }

    private void UpdateParticles (float t)
    {
        Vector3 verletAcceleration = acceleration * t * t;
        Vector3[] vertices = mesh.vertices;
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            VerletParticle particle = particles[i];
            particle.timeToLive -= t;

            if (particle.timeToLive <= 0)
            {
                particles.RemoveAt(i);
            }
            else
            {
                particle.ParticleUpdate(verletAcceleration);
                vertices[i] = particle.position;
            }
        }

        mesh.vertices = vertices;
        acceleration += deltaAcceleration * t;
    }
}
