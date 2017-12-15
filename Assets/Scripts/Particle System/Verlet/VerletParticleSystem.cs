using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerletParticleSystem : MonoBehaviour
{
    const int kMaxParticles = 65000;
    const int kParticleMass = 1;
    const float kCollisionError = 0.00001f;
    const float kRaycastOffset = 0;

    [Header("Terrain Physics")]
    [SerializeField] private Terrain terrain;
    [SerializeField] private TerrainCollider terrainCollider;
    [SerializeField] private float terrainRestitution = .3f;
    [SerializeField] private float absorptionVelocity = 0;
    [SerializeField] private bool useInterpolatedNormal = true;
    [SerializeField] private bool printTerrainCoordinates;
    [SerializeField] private float absorptionHeightModifier = .1f;

    [Header("Spawn Settings")]
    [SerializeField] private Spawner[] spawners;
    [SerializeField] private int maxParticles;
    [SerializeField] private float particleMass;

    [Header("Forces")]
    [SerializeField] private Vector3 baseAcceleration;
    [SerializeField] private Vector2 xRangeForce;
    [SerializeField] private Vector2 yRangeForce;
    [SerializeField] private Vector2 zRangeForce;

    [Header("Verlet Particles")]
    private Vector3[] position;
    private Vector3[] priorPosition;
    private Vector3[] force;
    private float[] timeToLive;      // Particle removed if timeToLive <= 0

    private bool[] inUse;   // TODO convert to free list implementation
    private List<int> indices;
    private bool indicesModified = false;
    private bool terrainModified = false;

    private int remainingParticles = 0;
    private bool paused = false;
    private Mesh mesh;

    void Start ()
    {
        if (spawners.Length == 0)
        {
            Debug.LogWarning("VerletParticleSystem.Start: No spawners found", this);
        }

        if (maxParticles <= 0 || maxParticles > kMaxParticles)
        {
            Debug.Log("VerletParticleSystem.Start: maxParticles set to invalid value, " + maxParticles + ", reset to " + kMaxParticles, this);
            maxParticles = kMaxParticles;
        }

        if (particleMass <= 0)
        {
            Debug.Log("VerletParticleSystem.Start: particleMass invalid, " + particleMass + ", reset to " + kParticleMass, this);
            particleMass = kParticleMass;
        }

        if (terrain != null && terrainCollider != null)
        {
            absorptionHeightModifier /= terrain.terrainData.heightmapScale.y;
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
            int newParticles = Mathf.Min(simpleParticles.Length, remainingParticles);
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
        while (inUse[i])
        {
            i++;
        }

        position[i] = simpleParticle.position;
        priorPosition[i] = simpleParticle.position - (simpleParticle.velocity * Time.fixedDeltaTime);
        force[i] = new Vector3(UnityEngine.Random.Range(xRangeForce.x, xRangeForce.y), UnityEngine.Random.Range(yRangeForce.x, yRangeForce.y), UnityEngine.Random.Range(zRangeForce.x, zRangeForce.y));
        timeToLive[i] = simpleParticle.timeToLive;
        inUse[i] = true;
        indices.Add(i);

        if (!indicesModified)
        {
            indicesModified = true;
        }

        return i;
    }

    // i = the index in indices
    // j = the index of the actual particle
    public void RemoveParticle (int i, int j)
    {
        indices.RemoveAt(i);
        inUse[j] = false;

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
        force = new Vector3[maxParticles];
        timeToLive = new float[maxParticles];
        inUse = new bool[maxParticles];
        indices = new List<int>(maxParticles);

        remainingParticles = maxParticles;

        for (int i = 0; i < spawners.Length && remainingParticles > 0; i++)
        {
            AddSimpleParticles(spawners[i].GenerateInitialParticles());
        }

        // Set up for rendering particles
        mesh = new Mesh();
        gameObject.GetOrAddComponent<MeshFilter>().mesh = mesh;
        mesh.vertices = position;
        mesh.SetIndices(indices.ToArray(), MeshTopology.Points, 0);
    }

    private void UpdateParticles (float t)
    {
        float tSquared = t * t;

        for (int i = 0; i < spawners.Length; i++)
        {
            AddSimpleParticles(spawners[i].GenerateParticles());
        }

        for (int i = (indices.Count - 1); i >= 0; i--)
        {
            int j = indices[i];
            if (!inUse[j])
            {
                Debug.LogError("Not In Use: j = " + j + ", i = " + i + ", inUse[j] = " + inUse[j]);
            }

            if (timeToLive[j] <= t)
            {
                RemoveParticle(i, j);
            }
            else
            {
                timeToLive[j] -= t;
                ParticleUpdate(j, tSquared);
            }
        }

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

        // Update terrain
        if (terrain != null && terrainModified)
        {
            terrain.ApplyDelayedHeightmapModification();
        }
    }

    private void ParticleUpdate (int particle, float tSquared)
    {
        // Accumulate forces for particle
        Vector3 verletAcceleration = ((force[particle] / particleMass) + baseAcceleration) * tSquared;

        // Calculate next step
        Vector3 currentPosition = position[particle];
        Vector3 implicitVelocity = currentPosition - priorPosition[particle];
        Vector3 nextVelocity = implicitVelocity + verletAcceleration;
        Vector3 nextPosition = currentPosition + nextVelocity;

        priorPosition[particle] = currentPosition;
        position[particle] = nextPosition;

        // Check for collision
        if (terrain != null && terrainCollider != null)
        {
            if (printTerrainCoordinates)
            {
                PrintTerrainCoordinates(currentPosition);
            }

            if (ParticleCollisionUpdate(particle, nextVelocity, 0, currentPosition, nextPosition, 10))
            {
                //Debug.Log("-- Fin with " + particle);
            }
        }
    }

    private bool ParticleCollisionUpdate (int particle, Vector3 nextVelocity, float distanceTraveled, Vector3 currentPosition, Vector3 nextPosition, int cont)
    {
        // If particle height below mesh height, find collision point
        Vector3 nextWorldPosition = nextPosition + transform.position;
        if (IsBelowTerrain(nextWorldPosition))
        {
            // Find collision point
            RaycastHit hitInfo;
            Vector3 rayOrigin = currentPosition + transform.position + (distanceTraveled * nextVelocity);
            if (!terrainCollider.Raycast(new Ray(rayOrigin, nextVelocity), out hitInfo, (nextVelocity.magnitude - distanceTraveled)))
            {
                //Debug.LogError("VerletParticleSystem.ParticleUpdate: Below height, but no collision with particle " + particle); // + " - prior diff = " + priorHeightDiff + " - next diff = " + nextHeightDiff);
                //AbsorbParticle(particle, rayOrigin);
                timeToLive[particle] = 0;

                return false;
            }

            // Collision Response
            if (nextVelocity.magnitude > absorptionVelocity) // TODO Convert to using sqrmagnitude
            {
                Vector3 normal;
                if (useInterpolatedNormal)
                {
                    Vector2 normalizedCollisionPoint = GetNormalizedPositionOnTerrain(hitInfo.point);
                    normal = terrain.terrainData.GetInterpolatedNormal(normalizedCollisionPoint.x, normalizedCollisionPoint.y);
                }
                else
                {
                    normal = hitInfo.normal;
                }

                // Calculate new trajectory using normal reflection
                Vector3 nextVelocityNormal = Vector3.Dot(normal, nextVelocity) * normal;
                Vector3 newNextVelocity = nextVelocity - ((1 + terrainRestitution) * nextVelocityNormal);
                float newMagnitude = newNextVelocity.magnitude; // TODO if moving block to in raycast if, use cached version

                float newDistanceTraveled = (hitInfo.distance / nextVelocity.magnitude);
                float remainingDistance = (1 - newDistanceTraveled) * newMagnitude;

                // Calculate new position, and modify priorPosition as needed
                Vector3 newCurrentPosition = (hitInfo.point - transform.position) - (newDistanceTraveled * newNextVelocity);
                Vector3 newNextPosition = newCurrentPosition + newNextVelocity;
                priorPosition[particle] = newCurrentPosition;
                position[particle] = newNextPosition;

                if (cont > 0)
                    ParticleCollisionUpdate(particle, newNextVelocity, newDistanceTraveled, newCurrentPosition, newNextPosition, cont - 1);

                //RaycastHit tempHitInfo;
                //Vector3 newNextWorldPosition = newNextPosition + transform.position;
                //if (IsBelowTerrain(newNextWorldPosition))
                //{
                //    if (terrainCollider.Raycast(new Ray(hitInfo.point + (kRaycastOffset * newNextVelocity), newNextVelocity), out tempHitInfo, remainingDistance))
                //    {
                //        float hitDistance = tempHitInfo.distance + (kRaycastOffset * newMagnitude);
                //        Debug.Log(" + collision " + particle + ", " + cont + " | hitPoint = " + hitInfo.point.ToFullString() + " tempHitPoint = " + tempHitInfo.point.ToFullString() + " equal = " + (hitInfo.point == tempHitInfo.point) + " | dist = " + tempHitInfo.distance + " | true dist = " + hitDistance + " | mag of vec = " + remainingDistance + " | perc = " + (tempHitInfo.distance / remainingDistance));
                //        if (cont > 0)
                //            Debug.Log(" - collision " + particle + ", " + cont + " | - " + ParticleCollisionUpdate(particle, newNextVelocity, newDistanceTraveled, newCurrentPosition, newNextPosition, cont - 1));
                //    }
                //    else
                //    {
                //        Debug.Log(" x collision : " + particle + ", " + cont + " | below terrain, raycast failed ");
                //    }
                //}
            }
            else
            {
                AbsorbParticle(particle, hitInfo.point);
                Debug.Log("Absorbing from low velocity: " + nextVelocity.magnitude);
            }

            return true;
        }

        return false;
    }

    private void AbsorbParticle (int particle, Vector3 collisionPoint)
    {
        // Calculate new heights for each index based on barycentric coords
        // Apply heights to terrain

        // Place collisionPoint on terrain
        collisionPoint = new Vector3(collisionPoint.x, terrain.SampleHeight(collisionPoint), collisionPoint.z);

        // Calculate Barycentric Coordinates
        Vector2 terrainIndex = GetIndexPositionOnTerrain(collisionPoint);
        int boundTerrainIndexX = (int)terrainIndex.x;
        int boundTerrainIndexY = (int)terrainIndex.y;

        // Get heights 
        // TODO Fix this to modify the heights available
        float[,] heights;
        try
        {
            heights = terrain.terrainData.GetHeights(boundTerrainIndexX, boundTerrainIndexY, 2, 2);
        }
        catch (ArgumentException e)
        {
            // Out of bounds from the heightmap
            timeToLive[particle] = 0;
            return;
        }

        // Check if u < v
        bool topTriangle = (terrainIndex.x - boundTerrainIndexX) < (terrainIndex.y - boundTerrainIndexY);

        // Get indices in the array of heights
        Vector2 aIndices, bIndices, cIndices;
        GetTerrainIndices(topTriangle, out aIndices, out bIndices, out cIndices);

        // Get points of colliding triangle
        Vector3 a, b, c;
        GetTrianglePoint(heights, boundTerrainIndexX, boundTerrainIndexY, aIndices, out a);
        GetTrianglePoint(heights, boundTerrainIndexX, boundTerrainIndexY, bIndices, out b);
        GetTrianglePoint(heights, boundTerrainIndexX, boundTerrainIndexY, cIndices, out c);

        // TODO Fix the barrycentric misses
        Vector3 barycentricCoords = GetBarycentricCoords(collisionPoint, a, b, c);
        if (barycentricCoords.x < 0 || barycentricCoords.y < 0 || barycentricCoords.z < 0)
        {
            Debug.LogError("VerletParticleSystem.ParticleUpdate: Invalid collision with particle " + particle + " at " + collisionPoint + " | Barrycentric Coordinates: " + barycentricCoords);
        }
        else
        {
            float apppliedMass = particleMass * absorptionHeightModifier;
            UpdateTriangleHeight(ref heights, aIndices, barycentricCoords.x, apppliedMass);
            UpdateTriangleHeight(ref heights, bIndices, barycentricCoords.y, apppliedMass);
            UpdateTriangleHeight(ref heights, cIndices, barycentricCoords.z, apppliedMass);

            //Debug.Log(barycentricCoords);
            //Debug.Log(hitInfo.point - ((a * barycentricCoords.x) + (b * barycentricCoords.y) + (c * barycentricCoords.z)));
            //Debug.Log("Colliding at " + hitInfo.point + " between - " + currentWorldPosition + ", " + nextWorldPosition + " | n = " + normal + ", h = " + collisionHeight);
            //Debug.Log("On Triangle " + a + " - " + b + " - " + c);

            terrain.terrainData.SetHeightsDelayLOD(boundTerrainIndexX, boundTerrainIndexY, heights);

            if (!terrainModified)
                terrainModified = true;
        }

        timeToLive[particle] = 0;
    }

    private void UpdateTriangleHeight(ref float[,] heights, Vector2 indices, float modifier, float appliedMass)
    {
        heights[(int)indices.x, (int)indices.y] += appliedMass * modifier;
    }

    private bool IsBelowTerrain (Vector3 worldPosition)
    {
        Vector2 nextTerrainPosition = GetNormalizedPositionOnTerrain(worldPosition);
        float height = terrain.terrainData.GetInterpolatedHeight(nextTerrainPosition.x, nextTerrainPosition.y); // Could use terrain.SampleHeight() instead
        float heightDiff = worldPosition.y - height;

        return heightDiff <= kCollisionError;
    }

    // Returns (u, v, w)
    private Vector3 GetBarycentricCoords (Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 v0 = b - a;
        Vector3 v1 = c - a;
        Vector3 v2 = p - a;

        float d00 = Vector3.Dot(v0, v0);
        float d01 = Vector3.Dot(v0, v1);
        float d11 = Vector3.Dot(v1, v1);
        float d20 = Vector3.Dot(v2, v0);
        float d21 = Vector3.Dot(v2, v1);
        float denom = 1.0f / (d00 * d11 - d01 * d01);

        float v = (d11 * d20 - d01 * d21) * denom;
        float w = (d00 * d21 - d01 * d20) * denom;

        return new Vector3(1.0f - v - w, v, w);
    }

    // Terrain functions
    private void GetTrianglePoint (float[,] heights, int boundTerrainIndexX, int boundTerrainIndexY, Vector2 indices, out Vector3 p)
    {
        p = new Vector3((boundTerrainIndexX + indices.x) * terrain.terrainData.heightmapScale.x, heights[(int)indices.x, (int)indices.y] * terrain.terrainData.heightmapScale.y, (boundTerrainIndexY + indices.y) * terrain.terrainData.heightmapScale.z);
    }

    private void GetTerrainIndices (bool topTriangle, out Vector2 aIndices, out Vector2 bIndices, out Vector2 cIndices)
    {
        if (!topTriangle)
        {
            aIndices = new Vector2(1, 1); // z11
            bIndices = new Vector2(0, 0); // z00
            cIndices = new Vector2(1, 0); // z10
        }
        else
        {
            aIndices = new Vector2(1, 1); // z11
            bIndices = new Vector2(0, 1); // z01
            cIndices = new Vector2(0, 0); // z00
        }
    }

    private float GetHeight (Vector2 xy)
    {
        return terrain.terrainData.GetHeight((int) xy.x, (int) xy.y);
    }

    private Vector2 GetNormalizedPositionOnTerrain (Vector3 worldPosition)
    {
        Vector3 offsetFromTerrain = worldPosition - terrain.GetPosition();
        return new Vector2(Mathf.InverseLerp(0, terrain.terrainData.size.x, offsetFromTerrain.x), Mathf.InverseLerp(0, terrain.terrainData.size.z, offsetFromTerrain.z));
    }

    private Vector2 GetIndexPositionOnTerrain (Vector3 worldPosition)
    {
        Vector3 offsetFromTerrain = worldPosition - terrain.GetPosition();
        return new Vector2(offsetFromTerrain.x / terrain.terrainData.heightmapScale.x, offsetFromTerrain.z / terrain.terrainData.heightmapScale.z);
    }

    private void PrintTerrainCoordinates (Vector3 worldPosition)
    {
        Vector3 offsetFromTerrain = worldPosition - terrain.GetPosition();
        Vector2 normalizedPosition = new Vector2(Mathf.InverseLerp(0, terrain.terrainData.size.x, offsetFromTerrain.x), Mathf.InverseLerp(0, terrain.terrainData.size.z, offsetFromTerrain.z));
        float height = terrain.terrainData.GetInterpolatedHeight(normalizedPosition.x, normalizedPosition.y);
        Vector3 normal = terrain.terrainData.GetInterpolatedNormal(normalizedPosition.x, normalizedPosition.y);

        Debug.Log("At World Postion: " + position + ", TerrainOffset: " + offsetFromTerrain + ", NormalizedPosition: " + normalizedPosition);
        Debug.Log("InterpolatedHeight: " + height + ", HeightDiff: " + (worldPosition.y - height) + ", InterpolatedNormal: " + normal);
    }
}
