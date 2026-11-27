#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ACG
{
    public partial class CinematicPreview
    {
        const float CircuitDuration = 61f, SprintEntry = .85f;
        const float FirstHit = 16f, SecondHit = 31f, DoubleHit = 47f, DerezzTime = 1.65f;
        const float SectorTime = 2.55f;
        static readonly Vector3[] Circuit =
        {
            new Vector3(26,0,4), new Vector3(23,0,38), new Vector3(-1,0,47), new Vector3(-24,0,38),
            new Vector3(-29,0,8), new Vector3(-28,0,-25), new Vector3(-7,0,-43), new Vector3(25,0,-34)
        };
        // Grid order is orange 0-4, then cyan 5-9. Delay is measured along the racing line.
        static readonly float[] Lane = { -4f, -13f, -17f, 3.5f, 10.5f, -1f, 2f, 6.5f, 13.5f, 17f };
        static readonly float[] Delay = { 0, .65f, 1.38f, 2.05f, 2.85f, -.25f, .45f, 1.1f, 1.8f, 2.55f };
        static readonly float[] Impact = { float.PositiveInfinity, DoubleHit, DoubleHit, FirstHit, SecondHit };
        RaceScoreboard scoreboard;

        static Vector3 RacingLine(float time)
        {
            float sector = Mathf.Repeat(time / SectorTime, Circuit.Length);
            int i = Mathf.FloorToInt(sector);
            float t = sector - i;
            Vector3 a = Circuit[(i + Circuit.Length - 1) % Circuit.Length], b = Circuit[i];
            Vector3 c = Circuit[(i + 1) % Circuit.Length], d = Circuit[(i + 2) % Circuit.Length];
            return .5f * (2*b + (c-a)*t + (2*a-5*b+4*c-d)*t*t + (3*b-a-3*c+d)*t*t*t);
        }

        static Vector3 CircuitPosition(int i, float time)
        {
            float lane = Lane[i], delay = Delay[i];
            if (i == 3 || i == 4)
            {
                // Stop at the near edge of the neighboring cyan ribbon, allowing for the front wheel.
                float cut = Mathf.InverseLerp(Impact[i] - 2.4f, Impact[i], time);
                lane = Mathf.Lerp(lane, Lane[i + 4] - 1.48f, cut * cut);
            }
            else if (i == 1 || i == 2)
            {
                float catchUp = Mathf.InverseLerp(DoubleHit - 5, DoubleHit, time);
                delay = Mathf.SmoothStep(delay, 1.05f, catchUp);
                float pinch = Mathf.InverseLerp(DoubleHit - 2.6f, DoubleHit, time);
                lane = Mathf.Lerp(lane, i == 1 ? -14.28f : -15.72f, pinch * pinch);
            }
            else if (i == 0)
            {
                // Two overtakes: draw up alongside the cyan leader, then tuck back inside.
                float pass = Maneuver(time, 4, 12) + Maneuver(time, 34, 42);
                delay -= .8f * pass;

            }
            float station = time - delay;
            Vector3 right = Vector3.Cross(Vector3.up, RacingLine(station + .02f) - RacingLine(station - .02f)).normalized;
            return RacingLine(station) + right * lane;
        }

        IEnumerator RunCircuit(List<Transform> riders, List<TrailRenderer> trails)
        {
            var dissolves = new Material[Impact.Length];
            var engine = gameObject.AddComponent<AudioSource>();
            engine.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SoundEffects/SonidoMoto.mp3");
            engine.loop = true;
            engine.volume = .18f;
            engine.Play();
            yield return Animate(CircuitDuration, progress =>
            {
                float time = progress * CircuitDuration;
                scoreboard.Seconds = time;
                for (int i = 0; i < riders.Count; i++)
                {
                    var bike = riders[i];
                    if (!bike.gameObject.activeSelf) continue;
                    float hit = i < Impact.Length ? Impact[i] : float.PositiveInfinity;
                    float t = Mathf.Min(time, hit);
                    Vector3 p = CircuitPosition(i, t);
                    Vector3 before = CircuitPosition(i, t - .04f);
                    Vector3 after = CircuitPosition(i, t + .04f);
                    Vector3 heading = p - before;
                    float roll = Mathf.Clamp(-4 * Vector3.SignedAngle(heading, after - p, Vector3.up), -24, 24);
                    bike.SetPositionAndRotation(p, Quaternion.LookRotation(heading) * Quaternion.Euler(0, 0, roll));
                    if (time < hit) continue;
                    if (!dissolves[i])
                    {
                        trails[i].emitting = false;
                        dissolves[i] = Material("ACG/CinematicSurface", new Color(1f, .32f, .04f), 2);
                        ApplyDissolve(bike, dissolves[i]);
                        ScatterFragments(bike);
                        if (i != 2) Sound("MotorcycleCrash", .66f);
                        scoreboard.LoseOrange();
                    }
                    float decay = Mathf.Clamp01((time - hit) / DerezzTime);
                    dissolves[i].SetFloat("_Progress", Mathf.SmoothStep(0, 1, decay));
                    if (decay >= 1) bike.gameObject.SetActive(false);
                }
                FilmCircuitShot(time, riders);
                engine.pitch = 1f + .1f * Mathf.Sin(time * .72f) + .12f * Maneuver(time, 50, 61);
            });
            engine.Stop();
            Destroy(engine);
        }

        void FilmCircuitShot(float t, List<Transform> bikes)
        {
            var hero = bikes[0];
            if (t >= FirstHit - 3.6f && t < FirstHit + 2.1f) { ImpactShot(bikes[3], FirstHit, t, -1, false); return; }
            if (t >= SecondHit - 3.8f && t < SecondHit + 2.1f) { ImpactShot(bikes[4], SecondHit, t, 1, false); return; }
            if (t >= DoubleHit - 4f && t < DoubleHit + 2.2f) { ImpactShot(bikes[1], DoubleHit, t, 1, true); return; }
            if (t < 4.8f) TrackingShot(hero, new Vector3(-6.5f, 3.8f, -5), hero.position + hero.forward * 3, 67);
            else if (t < 9) TrackingShot(bikes[5], new Vector3(7, 2.3f, 5), (bikes[5].position + hero.position) * .5f, 56);
            else if (t < FirstHit) TrackingShot(bikes[3], new Vector3(4, 19, -9), bikes[3].position + bikes[3].forward * 5, 63);
            else if (t < 23) TrackingShot(hero, new Vector3(-5, 2.2f, 5), hero.position, 62);
            else if (t < SecondHit) TrackingShot(bikes[4], new Vector3(-5, 4.3f, -8), bikes[8].position, 68);
            else if (t < 38) TrackingShot(hero, new Vector3(-5.5f, 1.4f, -3), (hero.position + bikes[5].position) * .5f, 61);
            else if (t < DoubleHit) TrackingShot(bikes[2], new Vector3(-3, 14, -6), (bikes[2].position + bikes[1].position) * .5f, 55);
            else if (t < 55f) TrackingShot(hero, new Vector3(-2.8f, 1.5f, -5), hero.position + hero.forward * 4, 70);
            else
            {
                float sweep = Mathf.InverseLerp(55f, CircuitDuration, t);
                Vector3 center = Vector3.Lerp(hero.position, bikes[5].position, .4f);
                Shot(center + Vector3.Lerp(new Vector3(10, 14, -10), new Vector3(-6, 4, 9), sweep), center + Vector3.up);
                shotCamera.fieldOfView = Mathf.Lerp(65, 58, sweep);
            }
        }

        void ImpactShot(Transform bike, float hit, float time, float side, bool pair)
        {
            float reveal = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(hit - 2f, hit + .35f, time));
            float recoil = Mathf.Max(0, time - hit);
            float shake = .23f * Mathf.Sin(recoil * 33) * Mathf.Exp(-6 * recoil);
            var mount = Vector3.Lerp(new Vector3(side * 5, pair ? 6 : 3, -8),
                new Vector3(side * (pair ? 5.6f : 4.5f), pair ? 4.6f : 2.4f, 3.4f), reveal);
            // Swing around the outside, maintaining distance even as the camera passes the axle.
            mount.x += side * 2 * Mathf.Sin(reveal * Mathf.PI);
            mount.y += shake;
            TrackingShot(bike, mount, bike.position + bike.forward * (1 - reveal) * 2, Mathf.Lerp(65, 46, reveal));
        }

        void TrackingShot(Transform bike, Vector3 mount, Vector3 focus, float lens)
        {
            var level = Quaternion.LookRotation(Vector3.ProjectOnPlane(bike.forward, Vector3.up));
            shotCamera.fieldOfView = lens;
            Shot(bike.position + level * mount, focus + Vector3.up * 1.05f);
        }

        static void ApplyDissolve(Transform bike, Material dissolve)
        {
            foreach (var renderer in bike.GetComponentsInChildren<Renderer>())
            {
                if (renderer is TrailRenderer) continue;
                var slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++) slots[i] = dissolve;
                renderer.sharedMaterials = slots;
            }
        }

        static void ScatterFragments(Transform bike)
        {
            var obj = new GameObject("Orange impact fragments");
            obj.transform.position = bike.position + Vector3.up;
            var burst = obj.AddComponent<ParticleSystem>();
            burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = burst.main;
            main.loop = false;
            main.duration = 1.3f;
            main.gravityModifier = .85f;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = burst.emission;
            emission.enabled = false;
            var renderer = burst.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = bike.Find("Chassis").GetComponent<MeshFilter>().sharedMesh;
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ShaderPlaceholders/Tron/Neon_Orange.mat");
            burst.Play();
            for (int i = 0; i < 42; i++)
            {
                float angle = i * 2.4f;
                Vector3 spray = new Vector3(Mathf.Cos(angle), .5f + (i % 5) * .23f, Mathf.Sin(angle));
                burst.Emit(new ParticleSystem.EmitParams {
                    position = bike.forward * Mathf.Sin(i * 3.7f) * 1.3f,
                    velocity = spray * (1.8f + i % 4) + bike.forward * 1.8f,
                    startLifetime = .45f + (i % 7) * .085f, startSize = .06f + (i % 4) * .06f,
                    rotation3D = new Vector3(i * 51, i * 29, i * 13)
                }, 1);
            }
        }
    }
}
#endif
