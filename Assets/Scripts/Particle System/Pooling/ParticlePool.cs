using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//public class ParticlePool
//{
//    private BasicParticle[] particles;
//    private int maxParticles;
//    private int usedParticles;

//    public ParticlePool(int maxParticles_)
//    {
//        usedParticles = 0;
//        maxParticles = maxParticles_;

//        particles = new BasicParticle[maxParticles];
//        for (int i = 0; i < maxParticles; i++)
//        {
//            particles[i] = new BasicParticle();
//        }
//    }

//    public void FreeParticles()
//    {
//        // Free particles at end of usedParticles
//        while (usedParticles > 0 && particles[usedParticles - 1].IsFree())
//        {
//            usedParticles--;
//        }

//        for (int i = usedParticles - 2; i >= 0; i++)
//        {
//            if (particles[i].IsFree())
//            {
//                BasicParticle temp = particles[usedParticles - 1];
//                particles[usedParticles - 1] = particles[i];
//                particles[i] = particles[usedParticles - 1];
//                usedParticles--;
//            }
//        }
//    }

//    public BasicParticle GetParticle()
//    {
//        BasicParticle particle;

//        // { 0 to usedParticles - 1 used }
//        if (usedParticles == maxParticles)
//        {
//            // { maxParticles used } : All particles used, ignore request/skip
//            particle = null;
//        }
//        else
//        {
//            // { usedParticles free }
//            Debug.Assert(particles[usedParticles].IsFree());
//            particle = particles[usedParticles];
//            // { usedParticles used }
//            usedParticles++;
//            // { usedParticles - 1 used }
//        }
//        // { 0 to usedParticles - 1 used }

//        return particle;
//    }
//}


//public abstract class ObjectPool<T>
//{
//    public abstract T GetObject();
//}

//public abstract class PooledObject
//{
//    public abstract bool IsFree();
//}

//public class IteratingPool<T> where T : PooledObject
//{


//    public override T GetObject()
//    {
//        for (int i = 0; i < )
//    }
//}