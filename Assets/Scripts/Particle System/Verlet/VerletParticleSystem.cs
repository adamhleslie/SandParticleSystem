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
    private Mesh mesh;

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

    private void GenerateParticles ()
    {
        SimpleParticle[] simpleParticles = spawner.GenerateParticles(initialParticles);
        for (int i = 0; i < initialParticles; i++)
        {
            SimpleParticle simpleParticle = simpleParticles[i];
            GameObject go = new GameObject("Particle " + i);
            go.transform.SetPositionAndRotation(simpleParticle.position, Quaternion.identity);

            // Add Verlet Specific Behavior
            VerletParticle particle = go.GetOrAddComponent<VerletParticle>();
            particle.priorPosition = simpleParticle.position - (simpleParticle.velocity * Time.fixedDeltaTime);
            particle.timeToLive = timeToLive;

            particles.Add(particle);
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
                Destroy(particle.gameObject);
            }
            else
            {
                particle.ParticleUpdate(verletAcceleration);
                vertices[i] = particle.transform.position;
            }
        }

        mesh.vertices = vertices;
        acceleration += deltaAcceleration * t;
    }
}
