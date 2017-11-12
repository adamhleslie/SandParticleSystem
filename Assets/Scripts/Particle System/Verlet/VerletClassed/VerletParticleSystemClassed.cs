//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class VerletParticleSystem : MonoBehaviour
//{
//    const int kMaxParticles = 65000;

//    [SerializeField] private Spawner[] spawners;
//    [SerializeField] private int maxParticles;
//    [SerializeField] private int timeToLive;

//    [Header("Acceleration")]
//    [SerializeField] private Vector3 acceleration;
//    [SerializeField] private Vector3 deltaAcceleration;

//    private List<VerletParticle> particles;

//    private int remainingParticles = 0;
//    private bool paused = false;
//    private Mesh mesh;

//    void Start ()
//    {
//        if (spawners.Length == 0)
//        {
//            Debug.LogError("VerletParticleSystem.Start: No spawners found", this);
//        }

//        if (maxParticles < 0 || maxParticles > kMaxParticles)
//        {
//            Debug.LogError("VerletParticleSystem.Start: maxParticles set to invalid value, " + maxParticles + ", reset to " + kMaxParticles, this);
//            maxParticles = kMaxParticles;
//        }

//        GenerateInitialParticles();
//    }

//    void FixedUpdate ()
//    {
//        if (Input.GetButton("Fire1"))
//        {
//            paused = !paused;
//        }

//        if (!paused)
//        {
//            UpdateParticles(Time.fixedDeltaTime);
//        }
//    }

//    public void AddSimpleParticles (SimpleParticle[] simpleParticles)
//    {
//        if (simpleParticles != null)
//        {
//            int i;
//            for (i = 0; i < remainingParticles && i < simpleParticles.Length; i++)
//            {
//                particles.Add(new VerletParticle(simpleParticles[i]));
//            }
//            remainingParticles -= i;
//        }
//    }

//    private void GenerateInitialParticles ()
//    {
//        remainingParticles = maxParticles;
//        particles = new List<VerletParticle>(maxParticles);

//        for (int i = 0; i < spawners.Length && remainingParticles > 0; i++)
//        {
//            SimpleParticle[] simpleParticles = spawners[i].GenerateInitialParticles();
//            AddSimpleParticles(simpleParticles);
//        }

//        // Set up for rendering particles
//        Vector3[] vertices = new Vector3[particles.Count];
//        int[] indices = new int[particles.Count];
//        for (int i = 0; i < particles.Count; i++)
//        {
//            vertices[i] = particles[i].position;
//            indices[i] = i;
//        }

//        mesh = new Mesh();
//        gameObject.GetOrAddComponent<MeshFilter>().mesh = mesh;
//        mesh.vertices = vertices;
//        mesh.SetIndices(indices, MeshTopology.Points, 0);
//    }

//    private void UpdateParticles (float t)
//    {
//        Vector3 verletAcceleration = acceleration * t * t;
//        Vector3[] vertices = mesh.vertices;
//        for (int i = particles.Count - 1; i >= 0; i--)
//        {
//            VerletParticle particle = particles[i];
//            particle.timeToLive -= t;

//            if (particle.timeToLive <= 0)
//            {
//                particles.RemoveAt(i);
//            }
//            else
//            {
//                particle.ParticleUpdate(verletAcceleration);
//                vertices[i] = particle.position;
//            }
//        }

//        mesh.vertices = vertices;
//        acceleration += deltaAcceleration * t;
//    }
//}
