#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace ACG
{
    public partial class CinematicPreview : MonoBehaviour
    {
        readonly List<Material> temporaryMaterials = new List<Material>();
        AudioSource music, effects;
        Camera shotCamera;
        Font arcadeFont;
        bool gameOver;
        float defeatTint;
        bool showingEnding;

        [MenuItem("ACG/Play cinematic preview")]
        static void PlayPreview()
        {
            if (EditorApplication.isPlaying) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SessionState.SetBool("ACG.Cinematic", true);
            EditorApplication.isPlaying = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Begin()
        {
            if (!SessionState.GetBool("ACG.Cinematic", false)) return;
            SessionState.SetBool("ACG.Cinematic", false);
            var root = new GameObject("Cinematic preview director");
            DontDestroyOnLoad(root);
            root.AddComponent<CinematicPreview>();
        }

        IEnumerator Start()
        {
            arcadeFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Cinematics/Fonts/PressStart2P-Regular.ttf");
            music = gameObject.AddComponent<AudioSource>();
            effects = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            yield return PlayFilm();
        }

        IEnumerator PlayFilm()
        {
            yield return Load("TronArena");
            Hide("04 - Motorcycles - static staging");
            Hide("05 - Energy walls - static placeholders");
            var riders = new List<Transform>();
            var grid = new List<Vector3>();
            var trails = new List<TrailRenderer>();
            for (int team = 0; team < 2; team++)
                for (int i = 0; i < 5; i++)
                {
                    string color = team == 0 ? "Orange" : "Cyan";
                    Transform bike = Spawn("Tron/Lightbike_" + color).transform;
                    // Reserve the center for the duel and spread each supporting pack outwards.
                    float lane = (team == 0 ? -1 : 1) * (10 + i * 7);
                    grid.Add(new Vector3(lane, 0, -60 - i * 1.5f));
                    int slot = riders.Count;
                    Vector3 start = CircuitPosition(slot, 0);
                    bike.SetPositionAndRotation(start, Quaternion.LookRotation(CircuitPosition(slot, .02f) - start));
                    var rider = Spawn("Characters/Zomboony_" + color + "_Staging").transform;
                    rider.SetParent(bike, false);
                    rider.localPosition = new Vector3(0, .8f, 0);
                    rider.localScale *= .7f;
                    riders.Add(bike);
                    var emitter = new GameObject("Rear light ribbon").transform;
                    emitter.SetParent(bike, false);
                    emitter.localPosition = new Vector3(0, .95f, -1.8f);
                    var trail = emitter.gameObject.AddComponent<TrailRenderer>();
                    trails.Add(trail);
                    trail.time = 2.6f; trail.startWidth = 1.75f; trail.endWidth = 1.35f;
                    trail.minVertexDistance = .3f;
                    trail.material = Material("ACG/CinematicSurface", team == 0 ? new Color(1,.18f,.02f, .8f) : new Color(.02f,.7f,1,.8f), 1);
                    trail.emitting = false;
                }
            Transform hero = riders[0];
            scoreboard = new RaceScoreboard(arcadeFont);
            Music("EpicBattleSong", .95f);
            Sound("MotorcycleStart", .45f);
            yield return Animate(3, t => {
                float sweep = Mathf.SmoothStep(0, 1, t);
                Shot(hero.TransformPoint(Vector3.Lerp(new Vector3(11, 14, -24), new Vector3(-4, 2, 6), sweep)),
                    Vector3.Lerp(riders[2].position, hero.position, sweep) + Vector3.up);
            });
            Sound("SonidoMoto", .38f);
            foreach (var trail in trails) trail.emitting = true;
            yield return RunCircuit(riders, trails);
            // Match cut to the final straight. Begin a new trail strip in the new shot.
            for (int i = 0; i < riders.Count; i++)
            {
                if (!riders[i].gameObject.activeSelf) continue;
                trails[i].emitting = false;
                Vector3 p = RacePosition(grid[i], i, SprintEntry);
                riders[i].SetPositionAndRotation(p, Quaternion.LookRotation(RacePosition(grid[i], i, SprintEntry + .01f) - p));
                trails[i].Clear();
                trails[i].emitting = true;
            }
            var glass = GameObject.CreatePrimitive(PrimitiveType.Quad);
            glass.name = "Voronoi impact panel";
            glass.transform.position = new Vector3(-10, 2, -6);
            glass.transform.localScale = new Vector3(7, 4, 1);
            var glassMaterial = Material("ACG/CinematicSurface", new Color(.25f,.85f,1,.25f), 0);
            glass.GetComponent<Renderer>().material = glassMaterial;
            const float raceDuration = 6.43f;
            const float glassTime = 3.6f;
            const float overpassIn = 1.25f, overpassOut = 3.18f, extraShotTime = 3f;
            bool hitGlass = false;
            bool heardPass = false;
            shotCamera.fieldOfView = 72;
            float sprintDuration = raceDuration + extraShotTime - SprintEntry;
            yield return Animate(sprintDuration, progress => {
                float screenTime = SprintEntry + progress * sprintDuration;
                scoreboard.Seconds = CircuitDuration + progress * sprintDuration;
                // Stretch only the elevated pass. The glass hit and final duel keep their timing.
                float inset = Mathf.InverseLerp(overpassIn, overpassOut + extraShotTime, screenTime);
                float seconds = screenTime - extraShotTime * inset;
                for (int i = 0; i < riders.Count; i++)
                {
                    Transform bike = riders[i];
                    if (!bike.gameObject.activeSelf) continue;
                    Vector3 position = RacePosition(grid[i], i, seconds);
                    Vector3 heading = RacePosition(grid[i], i, seconds + .01f) - position;
                    Vector3 nextHeading = RacePosition(grid[i], i, seconds + .09f) -
                        RacePosition(grid[i], i, seconds + .08f);
                    float lean = Mathf.Clamp(-2 * Vector3.SignedAngle(heading, nextHeading, Vector3.up), -25, 25);
                    bike.SetPositionAndRotation(position, Quaternion.LookRotation(heading) * Quaternion.Euler(0, 0, lean));
                }
                if (!heardPass && seconds >= 1.95f)
                {
                    heardPass = true;
                    Sound("SonidoMoto", .48f);
                }
                if (!hitGlass && seconds >= glassTime)
                {
                    hitGlass = true;
                    Sound("VidrioRompiendose", .55f);
                }
                if (hitGlass) glassMaterial.SetFloat("_Progress", Mathf.Clamp01((seconds-glassTime)/.55f));
                if (seconds < .75f)
                    Shot(hero.position + new Vector3(-4,1.8f,-6), hero.position + Vector3.forward*6);
                else if (seconds < overpassIn)
                {
                    Vector3 duel = Vector3.Lerp(hero.position, riders[5].position, .45f);
                    Shot(duel + new Vector3(-5, 2, 8), duel + Vector3.up);
                }
                else if (seconds < overpassOut)
                {
                    Vector3 blue = riders[GlassOverpass.RiderIndex].position;
                    // Frame both levels as the blue rider sweeps over the orange pack.
                    Vector3 camera = blue + new Vector3(4, 3.1f, -8.8f);
                    Vector3 target = blue + new Vector3(-1, 0.8f-blue.y*.43f, 1.1f);
                    Shot(camera, target);
                }
                else if (seconds < 4.4f)
                    Shot(new Vector3(-18,3,-2), hero.position + Vector3.up);
                else if (seconds < 5.9f)
                    Shot(hero.position + new Vector3(-6,6,-8), Vector3.Lerp(hero.position, riders[5].position, .45f) + Vector3.up);
                else
                {
                    float rush = Mathf.InverseLerp(5.9f, raceDuration, seconds);
                    shotCamera.fieldOfView = Mathf.Lerp(72, 82, rush);
                    Shot(hero.position + new Vector3(-1.7f, 1.5f, -3), hero.position + Vector3.forward * 9 + Vector3.up);
                }
            });
            Destroy(glass);
            Vector3 eye = hero.position + Vector3.up*1.85f + hero.forward*.55f;
            Vector3 subject = hero.position + Vector3.up * 1.15f;
            Vector3 blast = subject + hero.forward * 2;
            Vector3 wide = subject + new Vector3(-12, 6, -10);
            shotCamera.fieldOfView = 53;
            Shot(wide, blast);
            const string blastPath = "Assets/Mirza Beig/Cinematic Explosions FREE/Prefabs/Explosions/Explosion FREE 1 Variant.prefab";
            var explosion = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(blastPath), blast, Quaternion.identity);
            explosion.transform.localScale *= 1.65f;
            foreach (var particles in explosion.GetComponentsInChildren<ParticleSystem>())
            {
                var main = particles.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                if (!particles.isPlaying) particles.Play(false);
            }
            Sound("MotorcycleCrash", .65f);
            music.volume = .55f;
            yield return Animate(1.65f, t => {
                float recoil = Mathf.Sin(t * Mathf.PI * 6) * Mathf.Exp(-9*t);
                Shot(wide + Vector3.up * (.22f * recoil), blast);
            });
            Destroy(explosion);

            // Cut to the clear side of the trail and hold the whole silhouette in frame.
            Vector3 close = subject + new Vector3(-5.4f, 1.5f, -2.8f);
            shotCamera.fieldOfView = 46;
            Shot(close, subject);
            yield return new WaitForSeconds(.35f);
            Sound("MuerteEnTron");
            var dissolve = Material("ACG/CinematicSurface", new Color(.32f,.74f,1,1), 2);
            ApplyDissolve(hero, dissolve);
            yield return Animate(2.8f, t => {
                dissolve.SetFloat("_Progress", Mathf.SmoothStep(0, 1, t));
                Shot(Vector3.Lerp(close, subject + new Vector3(-4.7f,1.3f,-2), t), subject);
            });
            hero.gameObject.SetActive(false); scoreboard.LoseOrange(); Sound("Aplausos");
            yield return new WaitForSeconds(.7f);
            shotCamera.fieldOfView = 70;
            Shot(eye, eye + hero.forward * 8);
            defeatTint = .46f;
            scoreboard = null;
            music.Stop(); Sound("GameOver"); gameOver = true;
            yield return new WaitForSeconds(3);
            gameOver = false;
            defeatTint = 0;
            yield return Load("GamingRoom");
            var actor = Spawn("Characters/Zomboony_Neutral_Staging").transform;
            actor.position = new Vector3(.35f,0,-.7f);
            actor.rotation = Quaternion.Euler(0,180,0);
            Hide("VR_Headset - independent prop");
            var headset = Spawn("GamingRoom/VR_Headset").transform;
            headset.position = actor.position + new Vector3(0,1.5f,-.2f);
            Shot(actor.position + new Vector3(-.7f, 1.6f, -2.4f), actor.position + Vector3.up * 1.05f);
            Sound("AngryScream");
            Vector3 visorStart = headset.position;
            yield return Animate(2, t => {
                headset.position = Vector3.Lerp(visorStart, new Vector3(1.25f,1.22f,2.3f), t);
                headset.rotation = Quaternion.Euler(t*70,180,t*15);
                actor.localEulerAngles = new Vector3(Mathf.Sin(t*Mathf.PI)*12,180,0);
            });
            Sound("SighSound");
            yield return new WaitForSeconds(2);
            Music("ChillSong", .35f);
            var door = GameObject.Find("02 - Exit door - hinge pivot");
            if (door) door.transform.localEulerAngles = new Vector3(0,-100,0);
            Vector3 roomStart = actor.position;
            yield return Animate(4, t => {
                actor.position = Vector3.Lerp(roomStart,new Vector3(5.5f,0,0),t);
                actor.rotation = Quaternion.Euler(0,90,Mathf.Sin(t*30)*2);
                Shot(new Vector3(-.35f, 1.9f, -3.2f), actor.position + Vector3.up);
            });
            yield return Load("TouchGrass");
            actor = Spawn("Characters/Zomboony_Neutral_Staging").transform;
            Sound("birds-chirping");
            yield return Animate(6, t => {
                actor.position = Vector3.Lerp(new Vector3(0,0,-11),new Vector3(0,0,-2),t);
                actor.rotation = Quaternion.Euler(0,0,Mathf.Sin(t*40)*2);
                Shot(actor.position+new Vector3(4,3,-5),actor.position+Vector3.up);
            });
            Sound("RelaxBreathing");
            yield return Animate(3, t => {
                float reach = Mathf.SmoothStep(0,1,t);
                actor.rotation = Quaternion.Euler(reach*28,0,0);
                actor.position = new Vector3(0,-reach*.06f,-2);
                Shot(new Vector3(2,1,-.5f),new Vector3(0,.4f,-1));
            });
            yield return new WaitForSeconds(4);
            showingEnding = true;
        }

        static Vector3 RacePosition(Vector3 start, int index, float seconds)
        {
            if (index == GlassOverpass.RiderIndex) return GlassOverpass.Center(seconds);
            if (index == 5)
            {
                // The blue bike rounds a left corner and cuts across the orange lane.
                const float turnStart = 99f/21f - .6f/(Mathf.PI*.5f);
                const float turnDuration = .6f;
                const float speed = 21f;
                float angle = Mathf.Clamp01((seconds-turnStart)/turnDuration)*Mathf.PI*.5f;
                float radius = speed*turnDuration/(Mathf.PI*.5f);
                Vector3 corner = Vector3.forward*(speed*Mathf.Min(seconds,turnStart) + radius*Mathf.Sin(angle))
                    + Vector3.left*(radius*(1-Mathf.Cos(angle)) + speed*Mathf.Max(0,seconds-turnStart-turnDuration));
                // Dive alongside the orange rider, then break away to lay the final wall.
                float challenge = Maneuver(seconds, .35f, 3.75f);
                return start + corner + challenge * new Vector3(-15, 0, -12);
            }
            float lateral = index == 0
                ? -3.8f * Maneuver(seconds, 1.15f, 3.25f) + 3 * Maneuver(seconds, 5.15f, 6.55f)
                : .55f*(Mathf.Sin(seconds*2+index)-Mathf.Sin(index));
            float pace = index == 0 ? 15 : 12 + index%3 * .4f;
            // The first orange wingman draws level under the bridge, then drops back.
            float underpass = index == 1 ? 8 * Maneuver(seconds, 1.35f, 4.65f) : 0;
            return start + new Vector3(lateral,0,pace*seconds + underpass);
        }

        // The bike enters and leaves the lane change tangent to its original path.
        static float Maneuver(float seconds, float start, float end)
        {
            float wave = Mathf.Sin(Mathf.PI * Mathf.InverseLerp(start, end, seconds));
            return wave * wave;
        }

        IEnumerator Load(string name)
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/"+name+".unity", new LoadSceneParameters(LoadSceneMode.Single));
            foreach (var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None)) camera.enabled = false;
            foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None)) listener.enabled = false;
            shotCamera = new GameObject("Cinematic camera").AddComponent<Camera>();
            if (name == "TronArena")
            {
                var cameraData = shotCamera.GetUniversalAdditionalCameraData();
                cameraData.renderPostProcessing = true;
                cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            }
            shotCamera.clearFlags = RenderSettings.skybox ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            shotCamera.gameObject.AddComponent<AudioListener>();
            shotCamera.fieldOfView = 55;
            foreach (var transform in FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (transform.name == "Zomboony - static scale and staging reference") transform.gameObject.SetActive(false);
        }
        static GameObject Spawn(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/"+path+".prefab");
            if (!prefab) throw new InvalidOperationException("Missing cinematic prefab: "+path);
            return Instantiate(prefab);
        }
        Material Material(string shader, Color color, float mode)
        {
            var material = new Material(Shader.Find(shader));
            material.SetColor("_Color",color); material.SetFloat("_Mode",mode);
            temporaryMaterials.Add(material); return material;
        }
        static void Hide(string name) { var obj = GameObject.Find(name); if (obj) obj.SetActive(false); }
        void Shot(Vector3 position, Vector3 target) { shotCamera.transform.position=position; shotCamera.transform.LookAt(target); }
        void Sound(string name, float gain = .7f) { var clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SoundEffects/"+name+".mp3"); if(clip) effects.PlayOneShot(clip,gain); }
        void Music(string name,float volume) { music.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SoundEffects/"+name+".mp3"); music.volume=volume; music.Play(); }
        static IEnumerator Animate(float duration,Action<float> update)
        {
            for(float time=0;time<duration;time+=Time.deltaTime) { update(Mathf.Clamp01(time/duration)); yield return null; }
            update(1);
        }
        void OnGUI()
        {
            scoreboard?.Draw();
            if (defeatTint > 0)
            {
                Color original = GUI.color;
                GUI.color = new Color(.43f, .008f, .024f, defeatTint);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = original;
            }
            if (!gameOver && !showingEnding) return;
            float scale = Mathf.Min(Screen.width / 960f, Screen.height / 540f);
            var style=new GUIStyle(GUI.skin.label) { font=arcadeFont, fontSize=Mathf.Max(1,Mathf.RoundToInt(48*scale)), alignment=TextAnchor.MiddleCenter };
            style.normal.textColor=gameOver?new Color(1,.055f,.1f):Color.white;
            if (gameOver)
            {
                GUI.Label(new Rect(0,0,Screen.width,Screen.height),"GAME OVER",style);
                return;
            }

            Color previous = GUI.color;
            GUI.color = new Color(0, .025f, .03f, .58f);
            GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;
            GUI.Label(new Rect(0, Screen.height*.5f-82*scale, Screen.width, 80*scale), "FIN", style);
            var button = new Rect((Screen.width-200*scale)*.5f, Screen.height*.5f+12*scale, 200*scale, 48*scale);
            var buttonStyle = new GUIStyle(GUI.skin.button) { font = arcadeFont, fontSize = Mathf.Max(1, Mathf.RoundToInt(14*scale)) };
            if (GUI.Button(button, "Volver a ver", buttonStyle)) Replay();
        }
        void Replay()
        {
            if (!showingEnding) return;
            showingEnding = false;
            music.Stop();
            effects.Stop();
            ReleaseMaterials();
            StartCoroutine(PlayFilm());
        }

        void ReleaseMaterials()
        {
            foreach(var material in temporaryMaterials) if(material) Destroy(material);
            temporaryMaterials.Clear();
        }
        void OnDestroy() => ReleaseMaterials();
    }
}
#endif


