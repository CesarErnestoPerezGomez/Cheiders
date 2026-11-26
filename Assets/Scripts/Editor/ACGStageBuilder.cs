using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ACG.Editor
{
    // Authoring only. The saved scenes do not depend on this script at runtime.
    public static class ACGStageBuilder
    {
        const string MatRoot = "Assets/Materials/ShaderPlaceholders/";
        const string PrefabRoot = "Assets/Prefabs/";
        static readonly string[] StageNames = { "TronArena", "GamingRoom", "TouchGrass" };
        static readonly Dictionary<string, Material> Mats = new Dictionary<string, Material>();
        static readonly Color Cyan = new Color(.02f, .7f, 1f);
        static readonly Color Orange = new Color(1f, .22f, .025f);
        static Material dark, metal, cyan, orange, white, wood, grass, leaf, stone, wall, fabric, pink;
        static Mesh ring;
        static GameObject neutralCharacter;

        [MenuItem("ACG/Prepare three stages (new scenes only)")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (string name in StageNames)
                if (File.Exists("Assets/Scenes/" + name + ".unity"))
                    throw new InvalidOperationException(name + " already exists. Open and edit the saved scene; this builder never overwrites it.");
            Setup();
            neutralCharacter = PrepareCharacter("Neutral", Color.white);
            PrepareCharacter("Cyan", new Color(.25f,.85f,1));
            PrepareCharacter("Orange", new Color(1,.48f,.16f));
            BuildArena();
            BuildRoom();
            BuildGarden();
            AssetDatabase.SaveAssets();
            ValidateAndCapture();
            Debug.Log("ACG: Three stages saved and checked. No runtime movement, Timeline or final shaders were added.");
        }

        static void Setup()
        {
            foreach (string dir in new[] { "Assets/Scenes", MatRoot, "Assets/Models/FuturisticCity", "Assets/Models/Motorcycles", "Assets/Models/GamingRoom", "Assets/Models/Nature", "Assets/Prefabs/Tron", "Assets/Prefabs/GamingRoom", "Assets/Prefabs/Nature", "Assets/Prefabs/Characters", "Assets/Cinematics", "Docs/Previews" })
                Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
            dark = Material("Tron/Obsidian", new Color(.025f, .04f, .065f), .65f, .65f);
            metal = Material("Tron/BrushedMetal", new Color(.13f, .19f, .24f), .65f, .55f);
            cyan = Material("Tron/Neon_Cyan", Cyan * .3f, .1f, .55f, new Color(.02f,.65f,1)*7);
            orange = Material("Tron/Neon_Orange", Orange * .3f, .1f, .55f, new Color(1,.13f,.008f)*7);
            white = Material("Common/WarmWhite", new Color(.84f, .85f, .8f), 0, .4f);
            wood = Material("Nature/Wood_Slot", new Color(.30f, .14f, .058f), 0, .25f);
            grass = Material("Nature/Grass_Slot", new Color(.24f, .39f, .11f), 0, .15f);
            leaf = Material("Nature/Leaves", new Color(.35f, .53f, .12f), 0, .1f);
            stone = Material("Nature/Stone", new Color(.37f, .43f, .40f), 0, .25f);
            wall = Material("GamingRoom/Wall_Teal", new Color(.10f, .22f, .25f), 0, .3f);
            fabric = Material("GamingRoom/Fabric", new Color(.06f, .09f, .14f), 0, .1f);
            pink = Material("GamingRoom/RGB_Magenta", new Color(.30f, .015f, .16f), 0, .4f, new Color(1f, .015f, .3f) * 2f);
            ring = MakeRing();
        }

        static Material Material(string name, Color color, float metallic = 0, float smoothness = .3f, Color emission = default)
        {
            if (Mats.TryGetValue(name, out Material cached)) return cached;
            string path = MatRoot + name + ".mat";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = Path.GetFileName(name), enableInstancing = true };
                m.SetColor("_BaseColor", color); m.SetFloat("_Metallic", metallic); m.SetFloat("_Smoothness", smoothness);
                if (emission.maxColorComponent > 0)
                {
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                    m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emission);
                }
                AssetDatabase.CreateAsset(m, path);
            }
            Mats[name] = m;
            return m;
        }

        static Transform Group(string name, Transform parent = null, Vector3 position = default)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = position; return t;
        }

        static GameObject Shape(string name, PrimitiveType kind, Transform parent, Vector3 pos, Vector3 scale, Material mat, Vector3 rotation = default)
        {
            var go = GameObject.CreatePrimitive(kind); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = pos; go.transform.localScale = scale; go.transform.localEulerAngles = rotation;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, Vector3 rotation = default)
            => Shape(name, PrimitiveType.Cube, parent, pos, scale, mat, rotation);

        static GameObject MeshObject(string name, Transform parent, Vector3 pos, Vector3 scale, Mesh mesh, Material mat, Vector3 rotation = default)
        {
            var go = Group(name, parent, pos).gameObject; go.transform.localScale = scale; go.transform.localEulerAngles = rotation;
            go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = mat; return go;
        }

        static GameObject SavePrefab(Transform root, string relative)
        {
            string path = PrefabRoot + relative + ".prefab";
            if (File.Exists(path)) throw new InvalidOperationException("Refusing to replace prefab " + path);
            var asset = PrefabUtility.SaveAsPrefabAsset(root.gameObject, path);
            Object.DestroyImmediate(root.gameObject); return asset;
        }

        static GameObject Instance(GameObject prefab, Transform parent, Vector3 pos, Vector3 rotation = default, float scale = 1)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent, false); go.transform.localPosition = pos; go.transform.localEulerAngles = rotation; go.transform.localScale = Vector3.one * scale; return go;
        }

        static void Begin(Color sky, Color ambient, bool fog = false)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null; RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = ambient;
            RenderSettings.fog = fog; RenderSettings.fogColor = sky; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .0055f;
        }

        static Light Light(string name, Transform parent, Vector3 pos, Color color, float intensity, float range, LightType type = LightType.Point, Vector3 rotation = default)
        {
            var l = Group(name, parent, pos).gameObject.AddComponent<Light>(); l.type = type; l.color = color; l.intensity = intensity; l.range = range; l.transform.localEulerAngles = rotation;
            l.shadows = type == LightType.Directional ? LightShadows.Soft : LightShadows.None;
            if (type == LightType.Directional) RenderSettings.sun = l;
            return l;
        }

        static Camera Camera(Vector3 pos, Vector3 target, Color background, float fov)
        {
            var cam = Group("Preview Camera").gameObject.AddComponent<Camera>(); cam.tag = "MainCamera";
            cam.transform.position = pos; cam.transform.LookAt(target); cam.fieldOfView = fov; cam.nearClipPlane = .08f; cam.farClipPlane = 500;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = background; cam.allowHDR = true;
            cam.gameObject.AddComponent<AudioListener>(); cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            cam.GetUniversalAdditionalCameraData().antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cam.GetUniversalAdditionalCameraData().antialiasingQuality = AntialiasingQuality.High;
            return cam;
        }

        static void Grade(string name, float bloom)
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            string path = MatRoot + name + "_PreviewVolume.asset";
            AssetDatabase.CreateAsset(profile, path);
            var b = profile.Add<Bloom>(true); b.intensity.Override(bloom); b.threshold.Override(name=="TronArena"?.7f:1f); b.scatter.Override(.65f);
            var t = profile.Add<Tonemapping>(true); t.mode.Override(TonemappingMode.ACES);
            var v = profile.Add<Vignette>(true); v.intensity.Override(.16f); v.smoothness.Override(.5f);
            foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
            var volume = Group("Lighting - Preview grade").gameObject.AddComponent<Volume>(); volume.isGlobal = true; volume.sharedProfile = profile;
            EditorUtility.SetDirty(profile);
        }

        static void Label(string text, Transform parent, Vector3 pos, float size, Color color, Vector3 rotation = default)
        {
            var t = Group(text, parent, pos); t.localEulerAngles = rotation;
            var mesh = t.gameObject.AddComponent<TextMesh>(); mesh.text = text; mesh.fontSize = 72; mesh.characterSize = size; mesh.anchor = TextAnchor.MiddleCenter; mesh.color = color;
            mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.GetComponent<MeshRenderer>().sharedMaterial = mesh.font.material;
        }

        static void Save(string name)
        {
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/Scenes/" + name + ".unity")) throw new IOException("Could not save " + name);
        }

        static Mesh MakeRing()
        {
            const string path = "Assets/Models/Motorcycles/NeonRing.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (existing) return existing;
            const int n = 64, k = 8; var vertices = new Vector3[n*k]; var normals = new Vector3[n*k]; var uv = new Vector2[n*k]; var triangles = new List<int>();
            for (int i=0;i<n;i++) for (int j=0;j<k;j++)
            {
                float a = 2*Mathf.PI*i/n, b = 2*Mathf.PI*j/k; int idx=i*k+j;
                Vector3 radial = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                normals[idx] = radial*Mathf.Cos(b)+Vector3.forward*Mathf.Sin(b); vertices[idx] = radial+normals[idx]*.045f; uv[idx]=new Vector2((float)i/n,(float)j/k);
                int next=((i+1)%n)*k; int jj=(j+1)%k;
                triangles.AddRange(new[]{idx,next+j,next+jj,idx,next+jj,i*k+jj});
            }
            var mesh = new Mesh { name="Original neon torus", vertices=vertices, normals=normals, uv=uv, triangles=triangles.ToArray() }; mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh,path); return mesh;
        }

        static GameObject Bike(string name, Material accent)
        {
            var root=Group(name);
            // Forward +Z. Root on ground; all pieces and attachment anchors are editable.
            foreach(float z in new[]{-1.18f,1.18f})
            {
                Shape("Wheel",PrimitiveType.Cylinder,root,new Vector3(0,.65f,z),new Vector3(1.3f,.22f,1.3f),dark,new Vector3(0,0,90));
                foreach(float x in new[]{-.235f,.235f})
                {
                    MeshObject("Wheel emissive ring",root,new Vector3(x,.65f,z),Vector3.one*.57f,ring,accent,new Vector3(0,90,0));
                    Shape("Hub",PrimitiveType.Cylinder,root,new Vector3(x,.65f,z),new Vector3(.4f,.025f,.4f),metal,new Vector3(0,0,90));
                }
            }
            Box("Chassis",root,new Vector3(0,.62f,0),new Vector3(.64f,.32f,2.35f),metal);
            Shape("Sculpted front fairing",PrimitiveType.Sphere,root,new Vector3(0,1.05f,.94f),new Vector3(.94f,.83f,1.44f),dark);
            Shape("Sculpted rear fairing",PrimitiveType.Sphere,root,new Vector3(0,.93f,-1.04f),new Vector3(.88f,.64f,1.30f),dark);
            Box("Saddle",root,new Vector3(0,1.0f,-.15f),new Vector3(.57f,.16f,.91f),fabric);
            Box("Battery spine",root,new Vector3(0,.87f,.24f),new Vector3(.5f,.35f,.6f),dark,new Vector3(-12,0,0));
            foreach(float x in new[]{-.37f,.37f})
            {
                Box("Body light rail",root,new Vector3(x,.76f,0),new Vector3(.045f,.07f,1.9f),accent);
                Box("Footrest",root,new Vector3(x,.4f,-.25f),new Vector3(.20f,.10f,.55f),dark);
                Box("Front light",root,new Vector3(x*.55f,1.12f,1.58f),new Vector3(.055f,.31f,.035f),accent,new Vector3(-23,0,0));
            }
            Box("Handlebar",root,new Vector3(0,1.28f,.55f),new Vector3(.95f,.09f,.09f),metal);
            Group("RiderAnchor",root,new Vector3(0,1.12f,-.1f)); Group("EnergyWallAnchor",root,new Vector3(0,.1f,-1.9f));
            return SavePrefab(root,"Tron/"+name);
        }

        static GameObject Building(string name, float height, Material accent, bool steps)
        {
            var root=Group(name); float width=8;
            Box("Podium",root,new Vector3(0,1,0),new Vector3(11,2,11),dark);
            Box("Tower",root,new Vector3(0,height/2,0),new Vector3(width,height,width),dark);
            if(steps) Box("Crown",root,new Vector3(0,height+2.5f,0),new Vector3(5,5,5),metal);
            for(int side=0;side<4;side++)
            {
                var facade=Group("Facade "+side,root); facade.localEulerAngles=new Vector3(0,90*side,0);
                Box("Vertical light seam",facade,new Vector3(-3.7f,height/2,-4.04f),new Vector3(.09f,height,.05f),accent);
                for(int floor=1;floor<height/3;floor++)
                    Box("Horizontal light band "+floor,facade,new Vector3(.6f,floor*3,-4.04f),new Vector3(5.8f,.13f,.05f),accent);
            }
            Box("Roof light",root,new Vector3(0,height+.03f,0),new Vector3(8,.06f,8),accent);
            return SavePrefab(root,"Tron/"+name);
        }

        static GameObject EnergyWall(string name, Material glow, Color color)
        {
            var root=Group(name);
            var surface=Material("Tron/"+name+"_Surface_Slot",new Color(color.r*.12f,color.g*.12f,color.b*.12f),.15f,.6f,color*.55f);
            Box("Energy surface - replace material",root,new Vector3(0,1.15f,-5),new Vector3(.055f,2.3f,10),surface);
            foreach(float y in new[]{.03f,2.3f}) Box("Edge glow",root,new Vector3(0,y,-5),new Vector3(.09f,.045f,10),glow);
            for(int i=1;i<5;i++) Box("Energy filament",root,new Vector3(0,i*.45f,-5),new Vector3(.065f,.012f,10),glow);
            return SavePrefab(root,"Tron/"+name);
        }

        static void BuildArena()
        {
            var sky=new Color(.008f,.018f,.035f); Begin(sky,new Color(.17f,.23f,.32f),true);
            var arena=Group("01 - Arena - clear race area 90 x 120 m");
            var glassFloor = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials for Shader/Transparent Floor.mat");
            Box("Glass racing surface",arena,new Vector3(0,-.3f,0),new Vector3(110,.6f,140),glassFloor).GetComponent<Renderer>().sortingOrder = -2;
            Box("Track foundation",arena,new Vector3(0,-4.3f,0),new Vector3(110,.6f,140),Material("Tron/ArenaFloor_Slot",new Color(.025f,.04f,.065f),.15f,.55f));
            var overpass=Group("Glass overpass",arena);
            overpass.gameObject.AddComponent<GlassOverpass>();
            var deckRenderer = overpass.GetComponent<MeshRenderer>();
            deckRenderer.sharedMaterial = glassFloor;
            deckRenderer.sortingOrder = -1;
            var grid=Group("Geometric floor light grid",arena,new Vector3(0,-2.5f,0));
            var gridMat=Material("Tron/Grid_DimCyan",new Color(.01f,.10f,.13f),0,.4f,Cyan*.6f);
            for(int x=-50;x<=50;x+=10) Box("Longitudinal grid "+x,grid,new Vector3(x,.009f,0),new Vector3(.035f,.016f,136),gridMat);
            for(int z=-70;z<=70;z+=10) Box("Transverse grid "+z,grid,new Vector3(0,.01f,z),new Vector3(106,.018f,.035f),gridMat);
            foreach(float x in new[]{-52f,52f})
            {
                Box("Boundary platform",arena,new Vector3(x,.45f,0),new Vector3(3,.9f,140),metal);
                Box("Boundary light",arena,new Vector3(x- Mathf.Sign(x)*1.4f,.93f,0),new Vector3(.12f,.12f,140),x<0?orange:cyan);
            }
            var city=Group("02 - Modular city");
            var towerA=Building("Tower_Spire",32,cyan,true); var towerB=Building("Tower_Block",22,orange,false); var towerC=Building("Tower_Signal",44,cyan,true);
            var buildings=new[]{towerA,towerB,towerC};
            for(int side=-1;side<=1;side+=2)
                for(int i=0;i<12;i++) Instance(buildings[i%3],city,new Vector3(side*(65+(i%2)*11),0,-62+i*13),new Vector3(0,(i%2)*90,0),.8f+(i%4)*.15f);
            for(int i=0;i<11;i++) Instance(buildings[(i+1)%3],city,new Vector3(-70+i*14,0,90+(i%2)*13),Vector3.zero,.8f+(i%3)*.23f);
            var arch=Group("03 - Finish portal");
            foreach(float x in new[]{-36f,36f})
            {
                Box("Portal pillar",arch,new Vector3(x,12,60),new Vector3(2,24,3),metal);
                Box("Portal light",arch,new Vector3(x,12,58.4f),new Vector3(.2f,23,.12f),cyan);
            }
            Box("Portal beam",arch,new Vector3(0,24,60),new Vector3(74,1.5f,3),dark);
            Box("Portal beam light",arch,new Vector3(0,23.2f,58.4f),new Vector3(72,.12f,.12f),cyan);
            Label("N E O N   C I R C U I T",arch,new Vector3(0,25.8f,58),.65f,Cyan);
            var bikes=Group("04 - Motorcycles - static staging");
            var blue=Bike("Lightbike_Cyan",cyan); var amber=Bike("Lightbike_Orange",orange);
            Instance(blue,bikes,new Vector3(-4,0,-4),new Vector3(0,158,0));
            Instance(amber,bikes,new Vector3(5,0,3),new Vector3(0,202,0));
            var trails=Group("05 - Energy walls - static placeholders");
            Instance(EnergyWall("EnergyWall_Cyan",cyan,Cyan),trails,new Vector3(-4.7f,0,-2.25f),new Vector3(0,158,0));
            Instance(EnergyWall("EnergyWall_Orange",orange,Orange),trails,new Vector3(5.7f,0,4.75f),new Vector3(0,202,0));
            var tech=Group("06 - Trackside technology");
            for(int i=0;i<10;i++) foreach(int side in new[]{-1,1})
            {
                var beacon=Group("Track beacon",tech,new Vector3(side*49,0,-55+i*12));
                Box("Base",beacon,new Vector3(0,.5f,0),new Vector3(1,1,1),dark);
                Box("Signal",beacon,new Vector3(0,1.2f,0),new Vector3(.14f,1.3f,.14f),side<0?orange:cyan);
            }
            var anchors=Group("07 - Future staging anchors (empty)"); Group("Race_Start",anchors,new Vector3(0,0,-40)); Group("Impact_Point",anchors,new Vector3(8,0,18)); Group("GameOver_Composition",anchors,new Vector3(0,4,0));
            var lights=Group("08 - Lighting");
            Light("Moon key",lights,Vector3.zero,new Color(.48f,.7f,1),1.4f,0,LightType.Directional,new Vector3(38,-35,0));
            Light("Bike orange fill",lights,new Vector3(4,4,-1),Orange,12,18); Light("Bike cyan fill",lights,new Vector3(-4,4,-5),Cyan,12,18);
            Camera(new Vector3(12,5,-17),new Vector3(0,3,10),sky,59); Grade("TronArena",1.5f);
            Save("TronArena");
        }

        static GameObject Furnishing(string source, string name, Transform parent, Vector3 pos, float targetHeight, float yaw=0)
        {
            var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LowPolyLivingRoomPack/Prefabs/"+source+".prefab");
            if(!model) throw new FileNotFoundException(source);
            var go=Instance(model,null,Vector3.zero,new Vector3(0,yaw,0)); go.name=name;
            var mat=Material("GamingRoom/Furniture_Palette",Color.white,0,.35f);
            if(!mat.GetTexture("_BaseMap"))
            {
                var original=AssetDatabase.LoadAssetAtPath<Material>("Assets/LowPolyLivingRoomPack/Materials/M_LowPolyLivingRoom.mat");
                mat.SetTexture("_BaseMap",original.GetTexture("_BaseMap")); EditorUtility.SetDirty(mat);
            }
            foreach(var r in go.GetComponentsInChildren<Renderer>()) r.sharedMaterials=Enumerable.Repeat(mat,r.sharedMaterials.Length).ToArray();
            Fit(go,targetHeight,true); go.transform.SetParent(parent,false); go.transform.localPosition+=pos; return go;
        }

        static Bounds Bounds(GameObject go)
        {
            var rs=go.GetComponentsInChildren<Renderer>(); var b=rs[0].bounds; foreach(var r in rs.Skip(1)) b.Encapsulate(r.bounds); return b;
        }

        static void Fit(GameObject go,float size,bool height)
        {
            var b=Bounds(go); float dimension=height?b.size.y:Mathf.Max(b.size.x,b.size.z); go.transform.localScale*=size/Mathf.Max(.001f,dimension);
            b=Bounds(go); go.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);
        }

        static GameObject Visor()
        {
            var root=Group("VR_Headset");
            Shape("Headset shell",PrimitiveType.Cube,root,Vector3.zero,new Vector3(.38f,.19f,.16f),white);
            Shape("Front black glass",PrimitiveType.Cube,root,new Vector3(0,0,.09f),new Vector3(.34f,.14f,.035f),dark);
            Box("Status LED",root,new Vector3(.12f,.03f,.111f),new Vector3(.055f,.015f,.012f),cyan);
            foreach(float x in new[]{-.095f,.095f}) Shape("Lens",PrimitiveType.Cylinder,root,new Vector3(x,0,-.09f),new Vector3(.09f,.012f,.09f),metal,new Vector3(90,0,0));
            MeshObject("Head strap",root,new Vector3(0,.015f,-.13f),new Vector3(.19f,.19f,.17f),ring,fabric,new Vector3(90,0,0));
            Group("HeadAttachment",root,new Vector3(0,0,-.17f)); Group("HandGrip_L",root,new Vector3(-.21f,0,0)); Group("HandGrip_R",root,new Vector3(.21f,0,0));
            return SavePrefab(root,"GamingRoom/VR_Headset");
        }

        static void BuildRoom()
        {
            var sky=new Color(.12f,.19f,.24f); Begin(sky,new Color(.48f,.52f,.56f));
            var shell=Group("01 - Room shell - removable camera walls");
            var floor=Material("GamingRoom/Floor_Oak",new Color(.38f,.26f,.16f),0,.35f);
            Box("Floor",shell,new Vector3(0,-.12f,0),new Vector3(9,.24f,8),floor);
            for(int i=-8;i<=8;i++) Box("Floor seam",shell,new Vector3(i*.5f,.005f,0),new Vector3(.012f,.008f,8),wood);
            Box("Back wall",shell,new Vector3(0,2,4),new Vector3(9,4,.18f),wall);
            Box("Left wall",shell,new Vector3(-4.5f,2,0),new Vector3(.18f,4,8),wall);
            // Camera-side walls and ceiling remain absent intentionally for accessible staging.
            Box("Right wall rear",shell,new Vector3(4.5f,2,2.65f),new Vector3(.18f,4,2.7f),wall);
            Box("Right wall front",shell,new Vector3(4.5f,2,-2.65f),new Vector3(.18f,4,2.7f),wall);
            Box("Door lintel",shell,new Vector3(4.5f,3.5f,0),new Vector3(.18f,1,2.6f),wall);
            foreach(float z in new[]{-1.32f,1.32f}) Box("Door jamb",shell,new Vector3(4.43f,1.5f,z),new Vector3(.28f,3,.12f),white);
            var door=Group("02 - Exit door - hinge pivot",null,new Vector3(4.45f,0,1.2f)); door.localEulerAngles=new Vector3(0,-28,0);
            Box("Door leaf",door,new Vector3(0,1.45f,-1.2f),new Vector3(.12f,2.9f,2.4f),floor);
            Shape("Door handle",PrimitiveType.Sphere,door,new Vector3(-.12f,1.25f,-2.1f),Vector3.one*.13f,metal);
            var furniture=Group("03 - Furniture");
            var desk=Group("Gamer desk",furniture,new Vector3(-.4f,0,2.7f));
            Box("Desktop",desk,new Vector3(0,1.03f,0),new Vector3(3.8f,.12f,1.2f),dark);
            foreach(float x in new[]{-1.6f,1.6f}) Box("Leg",desk,new Vector3(x,.48f,0),new Vector3(.12f,.96f,.9f),metal);
            Box("Desk RGB strip",desk,new Vector3(0,1.02f,-.61f),new Vector3(3.6f,.035f,.025f),cyan);
            var monitor=Group("Monitor module");
            Box("Frame",monitor,new Vector3(0,.55f,0),new Vector3(1.36f,.81f,.12f),dark);
            Box("Display - shader slot",monitor,new Vector3(0,.55f,-.068f),new Vector3(1.23f,.68f,.015f),Material("GamingRoom/Monitor_Display_Slot",new Color(.02f,.09f,.13f),0,.4f,new Color(.025f,.22f,.35f)));
            Box("Stand",monitor,new Vector3(0,.14f,.04f),new Vector3(.12f,.28f,.12f),metal);
            Box("Foot",monitor,new Vector3(0,.025f,-.05f),new Vector3(.5f,.05f,.3f),dark);
            var mp=SavePrefab(monitor,"GamingRoom/Monitor");
            Instance(mp,desk,new Vector3(-.75f,1.09f,.2f),new Vector3(0,-8,0)); Instance(mp,desk,new Vector3(.75f,1.09f,.2f),new Vector3(0,8,0));
            Label("NEON CIRCUIT",desk,new Vector3(-.77f,1.7f,.107f),.016f,Cyan,new Vector3(0,0,0));
            Label("VR  /  CONNECTED",desk,new Vector3(.77f,1.65f,.105f),.013f,new Color(.6f,.9f,1));
            Box("Keyboard base",desk,new Vector3(-.15f,1.13f,-.35f),new Vector3(1.12f,.055f,.34f),metal);
            for(int row=0;row<4;row++) for(int col=0;col<13;col++) Box("Key",desk,new Vector3(-.65f+col*.082f,1.164f,-.47f+row*.075f),new Vector3(.06f,.015f,.05f),row%2==0?cyan:pink);
            Shape("Mouse",PrimitiveType.Sphere,desk,new Vector3(.7f,1.15f,-.35f),new Vector3(.13f,.07f,.2f),white);
            var pc=Group("PC tower",furniture,new Vector3(2.3f,0,2.8f));
            Box("Case",pc,new Vector3(0,.63f,0),new Vector3(.65f,1.26f,.72f),dark);
            Box("Side inset",pc,new Vector3(-.331f,.65f,0),new Vector3(.016f,1.03f,.54f),metal);
            for(int i=0;i<3;i++) MeshObject("RGB intake fan",pc,new Vector3(0,.28f+i*.35f,-.37f),Vector3.one*.14f,ring,i%2==0?cyan:pink);
            var chair=Group("Gaming chair");
            Shape("Base",PrimitiveType.Cylinder,chair,new Vector3(0,.12f,0),new Vector3(.8f,.06f,.8f),metal);
            Shape("Stem",PrimitiveType.Cylinder,chair,new Vector3(0,.36f,0),new Vector3(.12f,.24f,.12f),metal);
            Box("Seat",chair,new Vector3(0,.65f,0),new Vector3(.75f,.18f,.75f),fabric);
            Box("Back",chair,new Vector3(0,1.2f,-.3f),new Vector3(.74f,1.04f,.18f),fabric,new Vector3(-8,0,0));
            foreach(float x in new[]{-.32f,.32f}) Box("Back accent",chair,new Vector3(x,1.22f,-.17f),new Vector3(.05f,.83f,.035f),cyan,new Vector3(-8,0,0));
            foreach(float x in new[]{-.45f,.45f}) Box("Armrest",chair,new Vector3(x,.94f,0),new Vector3(.12f,.10f,.6f),dark);
            Instance(SavePrefab(chair,"GamingRoom/GamingChair"),furniture,new Vector3(-.2f,0,1.15f),new Vector3(0,-15,0));
            var props=Group("04 - Reused room props");
            Furnishing("Bookshelf_Tall","Bookshelf",props,new Vector3(-3.75f,0,2.5f),2.7f,180);
            Furnishing("PottedPlant_Tall_1","Indoor plant",props,new Vector3(-3.65f,0,-2.7f),1.65f);
            Furnishing("Sofa_2Seat","Reading sofa",props,new Vector3(-3.3f,0,-.4f),1.0f,90);
            Furnishing("Mug","Mug",desk,new Vector3(-1.6f,1.1f,-.35f),.18f);
            Furnishing("Gamepad_Classic","Gamepad",desk,new Vector3(1.45f,1.1f,-.35f),.08f);
            var rug=Material("GamingRoom/Rug",new Color(.13f,.21f,.25f),0,.1f);
            Box("VR play area rug",furniture,new Vector3(.3f,.018f,-.9f),new Vector3(3.4f,.035f,3.5f),rug);
            var rgb=Group("05 - Decorative lighting");
            Box("Back wall RGB",rgb,new Vector3(0,3.65f,3.88f),new Vector3(8.6f,.05f,.05f),pink);
            Box("Left wall RGB",rgb,new Vector3(-4.38f,3.65f,0),new Vector3(.05f,.05f,7.6f),cyan);
            for(int i=0;i<6;i++) Box("Acoustic panel",rgb,new Vector3(-1.7f+i*.65f,2.7f,3.85f),new Vector3(.45f,.7f,.10f),i%2==0?dark:metal);
            var headset=Instance(Visor(),furniture,new Vector3(1.25f,1.22f,2.3f),new Vector3(0,180,0)); headset.name="VR_Headset - independent prop";
            var anchors=Group("06 - Future action anchors (empty)"); Group("Zomboony_Standing",anchors,new Vector3(.35f,0,-.7f)); Group("Exit_Approach",anchors,new Vector3(3.3f,0,0)); Group("VR_Removal_Closeup",anchors,new Vector3(.35f,1.5f,-.7f));
            var lights=Group("07 - Lighting");
            Light("Warm window key",lights,Vector3.zero,new Color(1,.83f,.65f),1.4f,0,LightType.Directional,new Vector3(38,-35,0));
            Light("Ceiling softbox",lights,new Vector3(0,3.8f,0),new Color(1,.85f,.68f),5,9);
            Light("Desk cyan wash",lights,new Vector3(-.6f,2.7f,2.8f),Cyan,3,4); Light("Magenta corner wash",lights,new Vector3(2.6f,2.8f,3),new Color(1,.06f,.3f),3,4);
            PlaceCharacter(new Vector3(.35f,0,-.7f),new Vector3(0,180,0));
            Camera(new Vector3(1.2f,2.9f,-6.5f),new Vector3(-.3f,1.45f,1.2f),sky,55); Grade("GamingRoom",.22f); Save("GamingRoom");
        }

        static void Branch(Transform parent, string name, Vector3 start, Vector3 end, float width)
        {
            var delta=end-start; var go=Shape(name,PrimitiveType.Cylinder,parent,(start+end)*.5f,new Vector3(width,delta.magnitude*.5f,width),wood);
            go.transform.localRotation=Quaternion.FromToRotation(Vector3.up,delta.normalized);
        }

        static GameObject MainTree()
        {
            var root=Group("HeroTree"); var trunk=Group("Wood - replace materials here",root); var crown=Group("Leaves - separate shader slot",root);
            Branch(trunk,"Main trunk - clear touch surface",Vector3.zero,new Vector3(0,4.5f,0),1.05f);
            for(int i=0;i<6;i++)
            {
                float a=i*Mathf.PI/3; var p=new Vector3(Mathf.Cos(a)*1.8f,4.3f+(i%2)*.8f,Mathf.Sin(a)*1.8f);
                Branch(trunk,"Branch "+i,new Vector3(0,2.8f,0),p,.35f);
                Shape("Canopy "+i,PrimitiveType.Sphere,crown,p+Vector3.up*.7f,new Vector3(3.2f,2.5f,3.1f),leaf);
                Branch(trunk,"Root "+i,new Vector3(0,.2f,0),new Vector3(Mathf.Cos(a)*1.25f,.05f,Mathf.Sin(a)*1.25f),.3f);
            }
            Shape("Top canopy",PrimitiveType.Sphere,crown,new Vector3(0,6,0),new Vector3(3.8f,2.7f,3.8f),leaf);
            Group("HandContact",root,new Vector3(0,1.25f,-.54f)); Group("ActorStand",root,new Vector3(0,0,-1.15f));
            return SavePrefab(root,"Nature/HeroTree");
        }

        static GameObject NaturePrefab(string source, string output, string materialSource)
        {
            var original=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Nicrom/Shaders/Wind/Prefabs/"+source+".prefab");
            if(!original) throw new FileNotFoundException(source);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(original);
            PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction); go.name=output;
            var palette=Material("Nature/"+output+"_Palette",Color.white,0,.2f);
            var src=AssetDatabase.LoadAssetAtPath<Material>("Assets/Nicrom/Shaders/Wind/Assets/Materials/"+materialSource+".mat");
            palette.SetTexture("_BaseMap",src.GetTexture("_MainTex")); palette.SetFloat("_Cull",0); EditorUtility.SetDirty(palette);
            foreach(var r in go.GetComponentsInChildren<Renderer>()) r.sharedMaterials=Enumerable.Repeat(palette,r.sharedMaterials.Length).ToArray();
            foreach(var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            return SavePrefab(go.transform,"Nature/"+output);
        }

        static void BuildGarden()
        {
            var sky=new Color(.57f,.76f,.85f); Begin(sky,new Color(.65f,.7f,.57f));
            var ground=Group("01 - Grass ground"); Box("Lawn - grass shader slot",ground,new Vector3(0,-.3f,0),new Vector3(4096,.6f,4096),grass);
            var path=Group("02 - Clear walking path"); var sand=Material("Nature/Path",new Color(.62f,.48f,.29f),0,.15f);
            // Wide continuous path from entry to the reachable front face of the trunk.
            Box("Walkway",path,new Vector3(0,.017f,-5.5f),new Vector3(2.6f,.035f,14),sand);
            Shape("Tree clearing",PrimitiveType.Cylinder,path,new Vector3(0,.02f,3),new Vector3(4.2f,.025f,4.2f),sand);
            var hero=Group("03 - Hero tree and touch anchors"); Instance(MainTree(),hero,new Vector3(0,0,3));
            var background=Group("04 - Secondary trees");
            var treeA=NaturePrefab("LPW_Tree_A1_6.5m_01","SecondaryTree","LPW_Tree_A1_01");
            foreach(var p in new[]{new Vector3(-8,0,6),new Vector3(7,0,9),new Vector3(-11,0,-2),new Vector3(11,0,2),new Vector3(-4,0,12),new Vector3(12,0,12)}) Instance(treeA,background,p,new Vector3(0,p.x*17,0),.7f+Mathf.Abs(p.x)*.025f);
            var grasses=Group("05 - Grass tufts - replace palette material");
            var tuft=NaturePrefab("LPW_Grass_A1_40cm_01","GrassTuft","LPW_Grass_A1_01");
            var rocks=Group("06 - Rocks and shrubs");
            var rock=NaturePrefab("LPW_Rock_Boulder_A1_01","Rock","LPW_TextureAtlas_01");
            var random=new System.Random(1207);
            for(int i=0;i<175;i++)
            {
                float x=(float)random.NextDouble()*29-14.5f, z=(float)random.NextDouble()*27-13.5f;
                if(Mathf.Abs(x)<1.8f && z<5 || new Vector2(x,z-3).magnitude<2.8f) continue;
                Instance(tuft,grasses,new Vector3(x,0,z),new Vector3(0,random.Next(360),0),.7f+(float)random.NextDouble()*.8f);
            }
            for(int i=0;i<14;i++)
            {
                float x=(i%2==0?-1:1)*(4+(float)random.NextDouble()*9), z=-10+(float)random.NextDouble()*23;
                Instance(rock,rocks,new Vector3(x,0,z),new Vector3(0,random.Next(360),0),.22f+(float)random.NextDouble()*.30f);
            }
            var bush=Group("Rounded shrub");
            Shape("Foliage",PrimitiveType.Sphere,bush,new Vector3(0,.55f,0),new Vector3(1.5f,1.1f,1.2f),leaf);
            var shrub=SavePrefab(bush,"Nature/Shrub");
            for(int i=0;i<9;i++) Instance(shrub,rocks,new Vector3(i%2==0?-6:6,0,-8+i*2.3f),new Vector3(0,i*31,0),.7f+(i%3)*.2f);
            var boundary=Group("07 - Garden boundary");
            for(int i=-7;i<=7;i++)
            {
                Box("Fence post",boundary,new Vector3(i*2, .6f,14),new Vector3(.15f,1.2f,.15f),wood);
            }
            foreach(float y in new[]{.4f,.95f}) Box("Fence rail",boundary,new Vector3(0,y,14),new Vector3(28,.10f,.10f),wood);
            var stones=Group("Path edging",boundary);
            for(int i=0;i<14;i++) foreach(int side in new[]{-1,1}) Box("Path stone",stones,new Vector3(side*1.47f,.09f,-11.8f+i*.95f),new Vector3(.22f,.18f,.65f),stone,new Vector3(0,i%3*4,0));
            var anchors=Group("08 - Future action anchors (empty)"); Group("Entry",anchors,new Vector3(0,0,-11)); Group("Walk_Mid",anchors,new Vector3(0,0,-4)); Group("TouchTree_Stand",anchors,new Vector3(0,0,1.85f));
            var lighting=Group("09 - Natural lighting"); Light("Afternoon sun",lighting,Vector3.zero,new Color(1,.86f,.64f),2.1f,0,LightType.Directional,new Vector3(47,-35,0));
            PlaceCharacter(new Vector3(0,0,-7.4f),Vector3.zero);
            Camera(new Vector3(15,11,-21),new Vector3(0,2.1f,2),sky,48); Grade("TouchGrass",.08f); Save("TouchGrass");
        }

        static GameObject PrepareCharacter(string suffix, Color color)
        {
            string path=PrefabRoot+"Characters/Zomboony_"+suffix+"_Staging.prefab";
            var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(existing) return existing;
            var root=Group("Zomboony_"+suffix+"_Staging");
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Zombunny/Zombunny.fbx"));
            model.transform.SetParent(root,false); Fit(model,1.65f,true);
            foreach(var a in model.GetComponentsInChildren<Animator>()) a.enabled=false;
            var mat=Material("Zomboony_"+suffix+"_Preview",color,0,.25f);
            mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Zombunny/ZombunnyTextures/ZomBunnyDiffuse.png")); EditorUtility.SetDirty(mat);
            foreach(var r in model.GetComponentsInChildren<Renderer>()) r.sharedMaterials=Enumerable.Repeat(mat,r.sharedMaterials.Length).ToArray();
            return SavePrefab(root,"Characters/Zomboony_"+suffix+"_Staging");
        }

        static void PlaceCharacter(Vector3 position, Vector3 rotation)
        {
            var actor=Instance(neutralCharacter,null,position,rotation); actor.name="Zomboony - static scale and staging reference";
        }

        [MenuItem("ACG/Validate and capture prepared scenes")]
        public static void ValidateAndCapture()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup=EditorSceneManager.GetSceneManagerSetup();
            var report=new StringBuilder("ACG stage validation\n"+DateTime.UtcNow.ToString("u")+"\n");
            Directory.CreateDirectory("Docs/Previews");
            try
            {
                foreach(string name in StageNames)
                {
                    var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity",OpenSceneMode.Single);
                    var roots=scene.GetRootGameObjects(); int count=0,renderers=0;
                    foreach(var root in roots) foreach(var t in root.GetComponentsInChildren<Transform>(true))
                    {
                        count++;
                        if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0) throw new Exception("Missing script: "+t.name);
                        var renderer=t.GetComponent<Renderer>();
                        if(renderer)
                        {
                            renderers++;
                            foreach(var mat in renderer.sharedMaterials)
                            {
                                if(!mat || !mat.shader || !mat.shader.isSupported || mat.shader.name=="Hidden/InternalErrorShader") throw new Exception("Missing/unsupported material/shader: "+t.name);
                                if(mat.HasProperty("_EmissionColor") && mat.GetColor("_EmissionColor").maxColorComponent>0 && !mat.IsKeywordEnabled("_EMISSION")) throw new Exception("Emission disabled: "+mat.name);
                            }
                        }
                        var filter=t.GetComponent<MeshFilter>(); if(filter && !filter.sharedMesh) throw new Exception("Missing mesh: "+t.name);
                    }
                    var cameras=roots.SelectMany(r=>r.GetComponentsInChildren<Camera>()).ToArray();
                    if(cameras.Length!=1) throw new Exception(name+" must have one preview camera");
                    var volume=roots.SelectMany(r=>r.GetComponentsInChildren<Volume>()).Single();
                    if(!volume.sharedProfile || !volume.isGlobal || volume.weight<=0 || !cameras[0].GetUniversalAdditionalCameraData().renderPostProcessing) throw new Exception("Invalid preview volume: "+name);
                    report.AppendLine(name+": "+count+" objects, "+renderers+" renderers; meshes, materials, scripts and camera PASS.");
                    Capture(cameras[0],"Docs/Previews/"+name+".png");
                }
                report.AppendLine("Scenes reopened from disk successfully. No animation systems or final shaders created.");
                File.WriteAllText("Docs/StageValidation.txt",report.ToString());
            }
            finally
            {
                // Batch execution can start with no restorable scene. Interactive use preserves the user's workspace.
                if (!Application.isBatchMode && setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        static void Capture(Camera cam,string path)
        {
            var rt=new RenderTexture(1600,900,24,RenderTextureFormat.ARGBHalf); rt.Create();
            var previous=RenderTexture.active; var target=cam.targetTexture;
            var tex=new Texture2D(1600,900,TextureFormat.RGBAFloat,false,true);
            var png=new Texture2D(1600,900,TextureFormat.RGB24,false);
            try
            {
                cam.targetTexture=rt;
                var request = new RenderPipeline.StandardRequest { destination = rt };
                if (!RenderPipeline.SupportsRenderRequest(cam, request)) throw new InvalidOperationException("Active pipeline cannot render the preview request.");
                RenderPipeline.SubmitRenderRequest(cam, request);
                RenderPipeline.SubmitRenderRequest(cam, request);
                RenderTexture.active=rt; tex.ReadPixels(new Rect(0,0,1600,900),0,0);
                // Floating-point render targets store linear light; PNG viewers expect sRGB values.
                var pixels=tex.GetPixels();
                if (QualitySettings.activeColorSpace == ColorSpace.Linear)
                    for(int i=0;i<pixels.Length;i++) pixels[i]=pixels[i].gamma;
                png.SetPixels(pixels); png.Apply(); File.WriteAllBytes(path,png.EncodeToPNG());
            }
            finally { cam.targetTexture=target; RenderTexture.active=previous; rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(tex); Object.DestroyImmediate(png); }
        }
    }
}
