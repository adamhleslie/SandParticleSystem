﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerletParticleSystem : MonoBehaviour
{
    const int kMaxParticles = 65000;
    const int kParticleMass = 1;
    const float kCollisionError = 0;

    [SerializeField] private Terrain terrain;
    [SerializeField] private TerrainCollider terrainCollider;
    [SerializeField] private bool printTerrainCoordinates;

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
    }

    private void ParticleUpdate (int particle, float tSquared)
    {
        // Accumulate forces for particle
        Vector3 verletAcceleration = ((force[particle] / particleMass) + baseAcceleration) * tSquared;

        // Calculate next step
        Vector3 currentPosition = position[particle];
        Vector3 implicitVelocity = currentPosition - priorPosition[particle];
        Vector3 nextPosition = currentPosition + implicitVelocity + verletAcceleration;

        // Check for collision
        if (terrain != null && terrainCollider != null)
        {
            if (printTerrainCoordinates)
            {
                PrintTerrainCoordinates(position[particle]);
            }

            // Calculate height at next position
            Vector3 nextWorldPosition = nextPosition + transform.position;
            Vector2 nextTerrainPosition = GetNormalizedPositionOnTerrain(nextWorldPosition);
            float height = terrain.terrainData.GetInterpolatedHeight(nextTerrainPosition.x, nextTerrainPosition.y);
            float heightDiff = nextWorldPosition.y - height;

            // If particle height below mesh height, find collision point
            if (heightDiff <= kCollisionError)
            {
                RaycastHit hitInfo;
                Vector3 currentWorldPosition = currentPosition + transform.position;
                Vector3 nextVelocity = nextPosition - currentPosition;
                terrainCollider.Raycast(new Ray(currentWorldPosition, nextVelocity), out hitInfo, nextVelocity.magnitude);

                Vector2 normalizedCollisionPoint = GetNormalizedPositionOnTerrain(hitInfo.point);
                Vector3 interpolatedNormal = terrain.terrainData.GetInterpolatedNormal(normalizedCollisionPoint.x, normalizedCollisionPoint.y);
                float interpolatedHeight = terrain.terrainData.GetInterpolatedHeight(normalizedCollisionPoint.x, normalizedCollisionPoint.y);

                //Debug.Log(interpolatedNormal.x + " VS " + hitInfo.normal.x);

                // Calculate Barycentric Coordinates
                Vector2 nextTerrainIndex = GetIndexPositionOnTerrain(nextWorldPosition);
                Vector2 nextTerrainIndexBound = new Vector2((int)nextTerrainIndex.x, (int)nextTerrainIndex.y);
                Vector2 uv = nextTerrainIndex - nextTerrainIndexBound;

                // Points of colliding triangle
                Vector3 a, b, c;
                FillPoints(nextTerrainIndexBound, uv[0] > uv[1], out a, out b, out c);

                //Debug.Log("Colliding at " + hitInfo.point + " between - " + currentWorldPosition + ", " + nextWorldPosition + " | n = " + normal + ", h = " + collisionHeight);
                //Debug.Log("On Triangle " + a + " - " + b + " - " + c);

                timeToLive[particle] = 0;
            }
        }

        position[particle] = nextPosition;
        priorPosition[particle] = currentPosition;
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

    private void FillPoints (Vector2 nextTerrainIndexBound, bool bottomTriangle, out Vector3 a, out Vector3 b, out Vector3 c)
    {
        float[,] heights = terrain.terrainData.GetHeights((int)nextTerrainIndexBound.x, (int)nextTerrainIndexBound.y, 2, 2);

        Vector2 aIndex, bIndex, cIndex;
        if (!bottomTriangle)
        {
            aIndex = new Vector2(1, 1); // z11
            bIndex = new Vector2(0, 0); // z00
            cIndex = new Vector2(1, 0); // z10
        }
        else
        {
            aIndex = new Vector2(1, 1); // z11
            bIndex = new Vector2(0, 1); // z01
            cIndex = new Vector2(0, 0); // z00
        }

        a = new Vector3((nextTerrainIndexBound[0] + aIndex[0]) * terrain.terrainData.heightmapScale[0], heights[(int)aIndex[0], (int)aIndex[1]], (nextTerrainIndexBound[1] + aIndex[1]) * terrain.terrainData.heightmapScale[2]);
        b = new Vector3((nextTerrainIndexBound[0] + bIndex[0]) * terrain.terrainData.heightmapScale[0], heights[(int)bIndex[0], (int)bIndex[1]], (nextTerrainIndexBound[1] + bIndex[1]) * terrain.terrainData.heightmapScale[2]);
        c = new Vector3((nextTerrainIndexBound[0] + cIndex[0]) * terrain.terrainData.heightmapScale[0], heights[(int)cIndex[0], (int)cIndex[1]], (nextTerrainIndexBound[1] + cIndex[1]) * terrain.terrainData.heightmapScale[2]);
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
