using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

namespace VRCSDK2
{
    public static class AvatarValidation
    {
        public static readonly string[] ComponentTypeWhiteList = new string[] {
            "UnityEngine.Transform",
            "UnityEngine.Animator",
            "VRC.Core.PipelineManager",
#if !VRC_CLIENT
            "VRC.Core.PipelineSaver",
#endif
            "VRCSDK2.VRC_AvatarDescriptor",
            "VRCSDK2.VRC_AvatarVariations",
            "NetworkMetadata",
            "RootMotion.FinalIK.IKExecutionOrder",
            "RootMotion.FinalIK.VRIK",
            "RootMotion.FinalIK.FullBodyBipedIK",
            "RootMotion.FinalIK.LimbIK",
            "RootMotion.FinalIK.AimIK",
            "RootMotion.FinalIK.BipedIK",
            "RootMotion.FinalIK.GrounderIK",
            "RootMotion.FinalIK.GrounderFBBIK",
            "RootMotion.FinalIK.GrounderVRIK",
            "RootMotion.FinalIK.GrounderQuadruped",
            "RootMotion.FinalIK.TwistRelaxer",
            "RootMotion.FinalIK.ShoulderRotator",
            "RootMotion.FinalIK.FBBIKArmBending",
            "RootMotion.FinalIK.FBBIKHeadEffector",
            "RootMotion.FinalIK.FABRIK",
            "RootMotion.FinalIK.FABRIKChain",
            "RootMotion.FinalIK.FABRIKRoot",
            "RootMotion.FinalIK.CCDIK",
            "RootMotion.FinalIK.RotationLimit",
            "RootMotion.FinalIK.RotationLimitHinge",
            "RootMotion.FinalIK.RotationLimitPolygonal",
            "RootMotion.FinalIK.RotationLimitSpline",
            "UnityEngine.SkinnedMeshRenderer",
            "LimbIK", // our limbik based on Unity ik
            "AvatarAnimation",
            "LoadingAvatarTextureAnimation",
            "UnityEngine.MeshFilter",
            "UnityEngine.MeshRenderer",
            "UnityEngine.Animation",
            "UnityEngine.ParticleSystem",
            "UnityEngine.ParticleSystemRenderer",
            "DynamicBone",
            "DynamicBoneCollider",
            "UnityEngine.TrailRenderer",
            "UnityEngine.Cloth",
            "UnityEngine.Light",
            "UnityEngine.Collider",
            "UnityEngine.Rigidbody",
            "UnityEngine.Joint",
            "UnityEngine.Camera",
            "UnityEngine.FlareLayer",
            "UnityEngine.GUILayer",
            "UnityEngine.AudioSource",
            "ONSPAudioSource",
            "AvatarCustomAudioLimiter",
            "UnityEngine.EllipsoidParticleEmitter",
            "UnityEngine.ParticleRenderer",
            "UnityEngine.ParticleAnimator",
            "UnityEngine.MeshParticleEmitter",
            "UnityEngine.LineRenderer",
            "VRC_StationInternal",
            "VRCSDK2.VRC_Station",
            "VRCSDK2.VRC_IKFollower",
            "VRC_IKFollowerInternal"
        };

        public static readonly int MAX_PARTICLES = 20000;
        public static readonly int MAX_PARTICLE_SYSTEMS = 200;
        public static readonly short MAX_PARTICLE_EMISSION = 5000;

        private static IEnumerable<System.Type> FindTypes()
        {
            Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();

            return ComponentTypeWhiteList.Select((name) =>
            {
                foreach (Assembly a in assemblies)
                {
                    System.Type found = a.GetType(name);
                    if (found != null)
                        return found;
                }

                //This is really verbose for some SDK scenes, eg.
                //If they don't have FinalIK installed
#if VRC_CLIENT && UNITY_EDITOR
                Debug.LogError("Could not find type " + name);
#endif
                return null;
            }).Where(t => t != null);
        }

        public static IEnumerable<Component> FindIllegalComponents(string Name, GameObject currentAvatar)
        {
            HashSet<System.Type> typesInUse = new HashSet<System.Type>();
            List<Component> componentsInUse = new List<Component>();
            Queue<GameObject> children = new Queue<GameObject>();
            children.Enqueue(currentAvatar.gameObject);
            while (children.Count > 0)
            {
                GameObject child = children.Dequeue();
                int childCount = child.transform.childCount;
                for (int idx = 0; idx < child.transform.childCount; ++idx)
                    children.Enqueue(child.transform.GetChild(idx).gameObject);
                foreach (Component c in child.transform.GetComponents<Component>())
                {
                    if (c == null)
                        continue;

                    if (typesInUse.Contains(c.GetType()) == false)
                        typesInUse.Add(c.GetType());
                    componentsInUse.Add(c);
                }
            }

            IEnumerable<System.Type> foundTypes = FindTypes();
            return componentsInUse.Where(c => !foundTypes.Any(allowedType => c != null && (c.GetType() == allowedType || c.GetType().IsSubclassOf(allowedType))));
        }

        public static void RemoveIllegalComponents(string Name, GameObject currentAvatar, bool retry = true)
        {
            IEnumerable<Component> componentsToRemove = VRCSDK2.AvatarValidation.FindIllegalComponents(Name, currentAvatar);

            HashSet<string> componentsToRemoveNames = new HashSet<string>();
            foreach (Component c in componentsToRemove)
            {
                if (componentsToRemoveNames.Contains(c.GetType().Name) == false)
                    componentsToRemoveNames.Add(c.GetType().Name);
                Object.DestroyImmediate(c);
            }

            if (retry && componentsToRemoveNames.Count > 0)
            {
                Debug.LogErrorFormat("Avatar {0} had components of the following types removed: {1}", Name, string.Join(", ", componentsToRemoveNames.ToArray()));

                // Call again, to see if there's components that were prevented from being removed
                RemoveIllegalComponents(Name, currentAvatar, false);
            }
        }

        public static List<AudioSource> EnforceAudioSourceLimits(GameObject currentAvatar)
        {
            List<AudioSource> audioSources = new List<AudioSource>();

            Queue<GameObject> children = new Queue<GameObject>();
            children.Enqueue(currentAvatar.gameObject);
            while (children.Count > 0)
            {
                GameObject child = children.Dequeue();
                int childCount = child.transform.childCount;
                for (int idx = 0; idx < child.transform.childCount; ++idx)
                    children.Enqueue(child.transform.GetChild(idx).gameObject);

#if VRC_CLIENT
                if (child.GetComponent<USpeaker>() != null)
                    continue;
#endif

                AudioSource[] sources = child.transform.GetComponents<AudioSource>();
                if (sources != null && sources.Length > 0)
                {
                    AudioSource au = sources[0];
#if VRC_CLIENT
                    au.outputAudioMixerGroup = VRCAudioManager.GetAvatarGroup();
#endif

                    if (au.volume > 0.9f)
                        au.volume = 0.9f;

#if VRC_CLIENT
                    // someone mucked with the sdk forced settings, shame on them!
                    if (au.spatialize == false)
                        au.volume = 0;
#else
                    au.spatialize = true;
#endif
                    au.priority = Mathf.Clamp(au.priority, 200, 255);
                    au.bypassEffects = false;
                    au.bypassListenerEffects = false;
                    au.spatialBlend = 1f;
                    au.spread = 0;

                    au.minDistance = Mathf.Clamp(au.minDistance, 0, 2);
                    au.maxDistance = Mathf.Clamp(au.maxDistance, 0, 30);

                    float range = au.maxDistance - au.minDistance;
                    float min = au.minDistance;
                    float max = au.maxDistance;
                    float mult = 50.0f/range;

                    // setup a custom rolloff curve
                    Keyframe[] keys = new Keyframe[7];
                    keys[0] = new Keyframe(0, 1);
                    keys[1] = new Keyframe(min, 1, 0, -0.4f * mult);
                    keys[2] = new Keyframe(min + 0.022f * range, 0.668f, -0.2f * mult, -0.2f * mult);
                    keys[3] = new Keyframe(min + 0.078f * range, 0.359f, -0.05f * mult, -0.05f * mult);
                    keys[4] = new Keyframe(min + 0.292f * range, 0.102f, -0.01f * mult, -0.01f * mult);
                    keys[5] = new Keyframe(min + 0.625f * range, 0.025f, -0.002f * mult, -0.002f * mult);
                    keys[6] = new Keyframe(max, 0);
                    AnimationCurve curve = new AnimationCurve(keys);

                    au.rolloffMode = AudioRolloffMode.Custom;
                    au.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curve);

                    // if we have an onsp component, also configure that
                    ONSPAudioSource oa = sources[0].GetComponent<ONSPAudioSource>();
                    if (oa)
                    {
                        if (oa.Gain > 10f) oa.Gain = 10f;
#if VRC_CLIENT
                        // someone mucked with the sdk forced settings, shame on them!
                        if (oa.enabled == false || oa.EnableSpatialization == false)
                        {
                            oa.Gain = 0f;
                            au.volume = 0f;
                        }
#else
                        oa.enabled = true;
                        oa.EnableSpatialization = true;
#endif
                        oa.UseInvSqr = true; // This is the ENABLED value for OCULUS ATTENUATION
                        oa.EnableRfl = false;
                        if (oa.Near > 2f) oa.Near = 2f;
                        if (oa.Far > 30f) oa.Far = 30f;
                        oa.VolumetricRadius = 0f;
                    }

                    audioSources.Add(au);

                    if (sources.Length > 1)
                    {
                        Debug.LogError("Disabling extra AudioSources on GameObject("+ child.name +"). Only one is allowed per GameObject.");
                        for (int i=1; i<sources.Length; i++)
                        {
#if VRC_CLIENT
                            sources[i].enabled = false;
                            sources[i].clip = null;
#else
                            Object.DestroyImmediate(sources[i]);
#endif
                        }
                    }
                }
            }

            return audioSources;
        }

        public static Dictionary<ParticleSystem, int> EnforceParticleSystemLimits(GameObject currentAvatar)
        {
            Dictionary<ParticleSystem, int> particleSystems = new Dictionary<ParticleSystem, int>();
            int particleSystemCount = 0;
            
            foreach(var ps in currentAvatar.transform.GetComponentsInChildren<ParticleSystem>(true))
            {
                if(particleSystemCount > MAX_PARTICLE_SYSTEMS)
                {
                    Debug.LogError("Too many particle systems, #" + particleSystemCount + " named " + ps.gameObject.name + " deleted");
                    Object.DestroyImmediate(ps);
                } else {
                    var main = ps.main;
                    var collision = ps.collision;
                    var emission = ps.emission;

                    var realtime_max = MAX_PARTICLES;

                    if(ps.GetComponent<ParticleSystemRenderer>())
                    {
                        var renderer = ps.GetComponent<ParticleSystemRenderer>();
                        if(renderer.renderMode == ParticleSystemRenderMode.Mesh)
                        {
                            Mesh[] meshes = new Mesh[0];
                            int heighestPoly = 0;
                            renderer.GetMeshes(meshes);
                            if(meshes.Length == 0 && renderer.mesh != null)
                            {
                                meshes = new Mesh[] { renderer.mesh };
                            }
                            // Debug.Log(meshes.Length + " meshes possible emmited meshes from " + ps.gameObject.name);
                            foreach(var m in meshes)
                            {
                                if(m.isReadable)
                                {
                                    if(m.triangles.Length/3 > heighestPoly)
                                    {
                                        heighestPoly = m.triangles.Length/3;
                                    }
                                } else {
                                    if(1000 > heighestPoly)
                                    {
                                        heighestPoly = 1000;
                                    }
                                }
                            }
                            if(heighestPoly > 0)
                            {
                                heighestPoly = Mathf.Clamp(heighestPoly / 10, 1, heighestPoly);
                                if(heighestPoly < realtime_max)
                                {
                                    realtime_max = realtime_max / heighestPoly;
                                } else {
                                    realtime_max = 1;
                                }
                                if(heighestPoly > 20000)
                                {
                                    Debug.LogError("Particle system named " + ps.gameObject.name + " breached polygon limits, it has been deleted");
                                    Object.DestroyImmediate(ps);
                                    particleSystemCount--;
                                    continue;
                                }
                            }
                        }
                    }
                    
                    
                    var rate = emission.rateOverTime;

                    if(rate.mode == ParticleSystemCurveMode.Constant)
                    {
                        rate.constant = Mathf.Clamp(rate.constant, 0, MAX_PARTICLE_EMISSION);
                    } else if(rate.mode == ParticleSystemCurveMode.TwoConstants) {
                        rate.constantMax = Mathf.Clamp(rate.constantMax, 0, MAX_PARTICLE_EMISSION);
                    } else {
                        rate.curveMultiplier = Mathf.Clamp(rate.curveMultiplier, 0, MAX_PARTICLE_EMISSION);
                    }

                    emission.rateOverTime = rate;
                    rate = emission.rateOverDistance;

                    if(rate.mode == ParticleSystemCurveMode.Constant)
                    {
                        rate.constant = Mathf.Clamp(rate.constant, 0, MAX_PARTICLE_EMISSION);
                    } else if(rate.mode == ParticleSystemCurveMode.TwoConstants) {
                        rate.constantMax = Mathf.Clamp(rate.constantMax, 0, MAX_PARTICLE_EMISSION);
                    } else {
                        rate.curveMultiplier = Mathf.Clamp(rate.curveMultiplier, 0, MAX_PARTICLE_EMISSION);
                    }

                     emission.rateOverDistance = rate;

                    ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[emission.burstCount];
                    emission.GetBursts(bursts);

                    for(int i=0;i<bursts.Length;i++)
                    {
                        if(bursts[i].maxCount > MAX_PARTICLE_EMISSION)
                        {
                            bursts[i].maxCount = (short)Mathf.Clamp(bursts[i].maxCount, 0, MAX_PARTICLE_EMISSION);
                        }
                    }

                    emission.SetBursts(bursts);

                    var max = realtime_max;

                    if(collision.enabled)
                    {
                        switch(collision.quality)
                        {
                            case ParticleSystemCollisionQuality.High:
                                max = max / 100;
                                break;
                            case ParticleSystemCollisionQuality.Medium:
                                max = max / 50;
                                break;
                            case ParticleSystemCollisionQuality.Low:
                                max = max / 10;
                                break;
                        }
                    }

                    if(main.maxParticles > max)
                    {
                        main.maxParticles = Mathf.Clamp(max, 1, MAX_PARTICLES);
                    }

                    particleSystems.Add(ps, realtime_max);
                }
                particleSystemCount++;
            }

            return particleSystems;
        }
    }
}
