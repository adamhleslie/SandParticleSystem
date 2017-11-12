using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerletParticleSystem : MonoBehaviour
{
    const int kMaxParticles = 65000;
    const int kNotInUse = -1;

    [SerializeField] private Spawner[] spawners;
    [SerializeField] private int maxParticles;

    [Header("Acceleration")]
    [SerializeField] private Vector3 acceleration;
    [SerializeField] private Vector3 deltaAcceleration;

    [Header("Verlet Particles")]
    public Vector3[] position;
    public Vector3[] priorPosition;
    public float[] timeToLive;      // Particle removed if timeToLive <= 0

    public int[] indexInIndices;    // Stores the index of the particle in indices, or kNotInUse if unused
    public List<int> indices;
    private bool indicesModified;

    private int remainingParticles = 0;
    private bool paused = false;
    private Mesh mesh;

    void Start ()
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

        GenerateInitialParticles();
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

    // Adds particles provided, so that remainingParticles does not become negative
    public void AddSimpleParticles (SimpleParticle[] simpleParticles)
    {
        if (simpleParticles != null)
        {
            int newParticles = Math.Min(simpleParticles.Length, remainingParticles);
            for (int i = 0, j = 0; i < newParticles; i++)
            {
                j = AddSimpleParticle(simpleParticles[i], j) + 1;
            }
            remainingParticles -= newParticles;
        }
    }

    // Returns the index the particle was inserted into
    private int AddSimpleParticle (SimpleParticle simpleParticle, int i = 0)
    {
        while (indexInIndices[i] != kNotInUse)
        {
            i++;
        }

        position[i] = simpleParticle.position;
        priorPosition[i] = simpleParticle.position - (simpleParticle.velocity * Time.fixedDeltaTime);
        timeToLive[i] = simpleParticle.timeToLive;
        indexInIndices[i] = indices.Count;
        indices.Add(i);

        if (!indicesModified)
        {
            indicesModified = true;
        }

        return i;
    }

    // Remove all particles whose timeToLive <= 0
    //public void RemoveParticles ()
    //{
    //    // Iterate through the particles we know are alive (backwards for removal)
    //    for (int i = (indices.Count - 1); i >= 0; i--)
    //    {
    //        int j = indices[i];
    //        Debug.Assert(indexInIndices[j] == i);
    //        if (timeToLive[j] <= 0)
    //        {
    //            RemoveParticle(i, j);
    //        }
    //    }
    //}

    // i = the index in indices
    // j = the index of the actual particle
    public void RemoveParticle (int i, int j)
    {
        indices.RemoveAt(i);
        indexInIndices[j] = kNotInUse;

        remainingParticles++;
        if (!indicesModified)
        {
            indicesModified = true;
        }
    }

    private void GenerateInitialParticles ()
    {
        // Initialize all arrays and lists to max size
        position = new Vector3[maxParticles];
        priorPosition = new Vector3[maxParticles];
        timeToLive = new float[maxParticles];
        indexInIndices = new int[maxParticles];
        indices = new List<int>(maxParticles);

        // Set default values
        for (int i = 0; i < maxParticles; i++)
        {
            indexInIndices[i] = kNotInUse;
        }

        remainingParticles = maxParticles;

        for (int i = 0; i < spawners.Length && remainingParticles > 0; i++)
        {
            SimpleParticle[] simpleParticles = spawners[i].GenerateInitialParticles();
            AddSimpleParticles(simpleParticles);
        }

        // Set up for rendering particles
        mesh = new Mesh();
        gameObject.GetOrAddComponent<MeshFilter>().mesh = mesh;
        mesh.vertices = position;
        mesh.SetIndices(indices.ToArray(), MeshTopology.Points, 0);
    }

    private void UpdateParticles (float t)
    {
        Vector3 verletAcceleration = acceleration * t * t;

        for (int i = (indices.Count - 1); i >= 0; i--)
        {
            int j = indices[i];
            Debug.Assert(indexInIndices[j] == i);

            if (timeToLive[j] <= t)
            {
                RemoveParticle(i, j);
            }
            else
            {
                timeToLive[j] -= t;

                ParticleUpdate(j, verletAcceleration);
            }
        }
        acceleration += deltaAcceleration * t;

        // Update mesh
        if (indicesModified)
        {
            mesh.Clear();
            mesh.vertices = position;
            mesh.SetIndices(indices.ToArray(), MeshTopology.Points, 0);
            indicesModified = false;
        }
        else
        {
            mesh.vertices = position;
        }
    }

    private void ParticleUpdate (int particle, Vector3 verletAcceleration)
    {
        Vector3 currentPosition = position[particle];
        Vector3 implicitVelocity = (currentPosition - priorPosition[particle]);
        position[particle] = currentPosition + implicitVelocity + verletAcceleration;
        priorPosition[particle] = currentPosition;
    }
}
