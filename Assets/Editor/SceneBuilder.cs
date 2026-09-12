using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.AI;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using SignalLost.AI;
using SignalLost.Audio;
using SignalLost.Core;
using SignalLost.Interaction;
using SignalLost.Inventory;
using SignalLost.Items;
using SignalLost.Narrative;
using SignalLost.Player;
using SignalLost.Save;
using SignalLost.UI;
using SignalLost.World;

namespace SignalLost.EditorTools
{
    public static class SceneBuilder
    {
        const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        const string DataPath = "Assets/_Project/ScriptableObjects";
        const string ItemsPath = "Assets/_Project/Resources/Items";
        const string MatPath = "Assets/_Project/Materials";
        const string UiPath = "Assets/_Project/UI";

        static int LWorld => LayerMask.NameToLayer("World");
        static int LPlayer => LayerMask.NameToLayer("Player");
        static int LEnemy => LayerMask.NameToLayer("Enemy");
        static int LInteractable => LayerMask.NameToLayer("Interactable");

        [MenuItem("SignalLost/Build Main Scene")]
        public static void BuildMain()
        {
            EnsureLayersAndTags();
            EnsureFolders();
            var pipelineAsset = EnsureUrpPipeline();
            FixUrpRenderer();
            var items = CreateItemAssets();
            var log = CreateLogAsset();
            var mats = CreateMaterials();

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.055f, 0.06f, 0.08f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.01f, 0.012f, 0.018f);
            RenderSettings.fogDensity = 0.028f;

            var level = BuildLevel(mats);
            var player = BuildPlayer();
            BuildHunter(mats, level.transform, player);
            BuildObserver(mats, level.transform);
            BuildEcho(player);
            BuildSystems(items, log);
            BuildHud();

            WireScene();
            BakeNavMesh(level);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            PersistPipelineToGraphicsSettings(pipelineAsset);

            Debug.Log("[SceneBuilder] BUILD_OK");
        }

        static void PersistPipelineToGraphicsSettings(UniversalRenderPipelineAsset pipeline)
        {
            var gsPath = "ProjectSettings/GraphicsSettings.asset";
            var lines = File.ReadAllLines(gsPath);
            var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(pipeline));
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("m_CustomRenderPipeline:"))
                {
                    lines[i] = $"  m_CustomRenderPipeline: {{fileID: 11400000, guid: {guid}, type: 2}}";
                    break;
                }
            }
            File.WriteAllLines(gsPath, lines);
        }

        static void EnsureLayersAndTags()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            var tagManager = new SerializedObject(assets[0]);
            var layers = tagManager.FindProperty("layers");
            for (int i = 8; i < 12; i++)
            {
                var el = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(el.stringValue))
                    el.stringValue = i == 8 ? "World" : i == 9 ? "Player" : i == 10 ? "Enemy" : "Interactable";
            }
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureFolders()
        {
            foreach (var d in new[] { "Assets/_Project", "Assets/_Project/Scenes", "Assets/_Project/ScriptableObjects", "Assets/_Project/Resources/Items", "Assets/_Project/Resources/Logs", "Assets/_Project/Materials", "Assets/_Project/UI", "Assets/_Project/Settings" })
                Directory.CreateDirectory(d);
            AssetDatabase.Refresh();
        }

        static UniversalRenderPipelineAsset EnsureUrpPipeline()
        {
            var path = "Assets/_Project/Settings/URP.asset";
            var rendererPath = "Assets/_Project/Settings/URP_Renderer.asset";
            var existing = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (existing != null) return existing;

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, rendererPath);
            }

            var instance = UniversalRenderPipelineAsset.Create();
            var so = new SerializedObject(instance);
            var listProp = so.FindProperty("m_RendererDataList");
            listProp.arraySize = 1;
            listProp.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            so.FindProperty("m_DefaultRendererIndex").intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(instance, path);
            return instance;
        }

        public static void FixUrpRenderer()
        {
            var path = "Assets/_Project/Settings/URP.asset";
            var rendererPath = "Assets/_Project/Settings/URP_Renderer.asset";
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (pipeline == null)
            {
                Debug.LogError("[FixUrp] URP.asset not found");
                return;
            }

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, rendererPath);
            }

            var so = new SerializedObject(pipeline);
            var listProp = so.FindProperty("m_RendererDataList");
            listProp.arraySize = 1;
            listProp.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            so.FindProperty("m_DefaultRendererIndex").intValue = 0;
            SetIfPresent(so, "m_AdditionalLightsRenderingMode", 1);
            SetIfPresent(so, "m_MainLightRenderingMode", 1);
            SetIfPresent(so, "m_ShadowDistance", 40f);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();
            Debug.Log("[FixUrp] Renderer assigned OK");
        }

        static void SetIfPresent(SerializedObject so, string prop, float value)
        {
            var p = so.FindProperty(prop);
            if (p == null) { Debug.LogWarning($"[FixUrp] prop {prop} not found"); return; }
            if (p.propertyType == SerializedPropertyType.Integer) p.intValue = (int)value;
            else if (p.propertyType == SerializedPropertyType.Float) p.floatValue = value;
        }

        static Dictionary<string, ItemDefinition> CreateItemAssets()
        {
            var dict = new Dictionary<string, ItemDefinition>();
            dict["power_cell"] = Item("power_cell", "Power Cell", "A compact fusion cell. Still warm.", ItemKind.Component, new Color(0.2f, 0.9f, 0.6f));
            dict["medkit"] = Item("medkit", "Medkit", "Standard issue trauma kit.", ItemKind.Consumable, new Color(0.9f, 0.2f, 0.2f), heal: 40f);
            dict["battery"] = Item("battery", "Battery Pack", "Recharges suit equipment.", ItemKind.Consumable, new Color(0.9f, 0.8f, 0.2f), battery: 50f);
            dict["keycard_l1"] = Item("keycard_l1", "Crew Keycard", "Security level 1.", ItemKind.Keycard, new Color(0.4f, 0.6f, 0.9f), access: 1);
            dict["keycard_l2"] = Item("keycard_l2", "Security Keycard", "Security level 2. Research elevator access.", ItemKind.Keycard, new Color(0.9f, 0.3f, 0.3f), access: 2);
            return dict;
        }

        static ItemDefinition Item(string id, string name, string desc, ItemKind kind, Color color,
            float heal = 0f, float battery = 0f, int access = 0)
        {
            var path = $"{ItemsPath}/{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (existing != null) return existing;

            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.itemId = id;
            item.displayName = name;
            item.description = desc;
            item.kind = kind;
            item.uiColor = color;
            item.healAmount = heal;
            item.batteryCharge = battery;
            item.accessLevel = access;
            AssetDatabase.CreateAsset(item, path);
            return item;
        }

        static AudioLogDefinition Log(string id, string title, string content)
        {
            var path = $"{DataPath}/{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<AudioLogDefinition>(path);
            if (existing != null) return existing;

            var log = ScriptableObject.CreateInstance<AudioLogDefinition>();
            log.logId = id;
            log.title = title;
            log.content = content;
            AssetDatabase.CreateAsset(log, path);
            return log;
        }

        static AudioLogDefinition CreateLogAsset() => Log("log_018", "LOG #018 — DR. VASQUEZ",
            "\"We found something beneath the facility. It's not geological. It responds to sound. A.R.I.A. says the object is safe. I've stopped trusting that word.\"");

        static Dictionary<string, Material> CreateMaterials()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var dict = new Dictionary<string, Material>();

            Material Make(string name, Color color, float smooth = 0.35f, float metallic = 0.2f, Color? emissive = null)
            {
                var path = $"{MatPath}/{name}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null)
                {
                    m = new Material(shader) { name = name };
                    AssetDatabase.CreateAsset(m, path);
                }
                m.SetColor("_BaseColor", color);
                m.SetFloat("_Smoothness", smooth);
                m.SetFloat("_Metallic", metallic);
                if (emissive.HasValue)
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", emissive.Value);
                }
                else
                {
                    m.DisableKeyword("_EMISSION");
                }
                EditorUtility.SetDirty(m);
                return m;
            }

            dict["floor"] = Make("floor_metal", new Color(0.10f, 0.11f, 0.13f), 0.5f, 0.6f);
            dict["wall"] = Make("wall_panel", new Color(0.16f, 0.17f, 0.19f), 0.3f, 0.3f);
            dict["ceiling"] = Make("ceiling", new Color(0.08f, 0.085f, 0.1f), 0.2f, 0.1f);
            dict["pod"] = Make("pod_shell", new Color(0.22f, 0.24f, 0.27f), 0.6f, 0.7f);
            dict["door"] = Make("door_metal", new Color(0.25f, 0.26f, 0.28f), 0.55f, 0.8f);
            dict["terminal"] = Make("terminal_body", new Color(0.12f, 0.13f, 0.15f), 0.4f, 0.5f);
            dict["screenRed"] = Make("screen_red", new Color(0.1f, 0.02f, 0.02f), 0.8f, 0f, new Color(2.6f, 0.12f, 0.1f));
            dict["screenGreen"] = Make("screen_green", new Color(0.02f, 0.1f, 0.04f), 0.8f, 0f, new Color(0.1f, 2.6f, 0.5f));
            dict["screenAmber"] = Make("screen_amber", new Color(0.1f, 0.06f, 0.01f), 0.8f, 0f, new Color(2.8f, 1.4f, 0.1f));
            dict["screenCyan"] = Make("screen_cyan", new Color(0.01f, 0.06f, 0.08f), 0.8f, 0f, new Color(0.1f, 2.2f, 3f));
            dict["crate"] = Make("crate", new Color(0.18f, 0.16f, 0.13f), 0.25f, 0.1f);
            dict["pickup"] = Make("pickup_glow", new Color(0.2f, 0.9f, 0.6f), 0.9f, 0f, new Color(0.1f, 0.8f, 0.5f));
            dict["echo"] = Make("echo_body", new Color(0.62f, 0.60f, 0.58f), 0.15f, 0f);
            dict["echoEye"] = Make("echo_eye", new Color(0.015f, 0.015f, 0.02f), 0.9f, 0f);
            return dict;
        }

        static GameObject Cube(string name, Vector3 pos, Vector3 scale, Material mat, int layer)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.layer = layer;
            return go;
        }

        const float FloorTop = 0.1f;

        static void RoomSealed(string name, Vector3 center, Vector3 size, Dictionary<string, Material> mats,
            Transform parent, bool openS = false, bool openN = false, bool openE = false, bool openW = false, float floorY = 0f)
        {
            var r = new GameObject(name);
            r.transform.SetParent(parent);
            r.transform.position = center;

            float w = size.x, h = size.y, d = size.z;
            float top = floorY + 0.1f;
            float wallCY = top + h / 2f;

            Cube($"{name}_Floor", new Vector3(center.x, floorY, center.z), new Vector3(w + 0.4f, 0.2f, d + 0.4f), mats["floor"], LWorld).transform.SetParent(r.transform, true);
            Cube($"{name}_Ceiling", new Vector3(center.x, top + h, center.z), new Vector3(w + 0.4f, 0.2f, d + 0.4f), mats["ceiling"], LWorld).transform.SetParent(r.transform, true);

            if (openW) GapWall(r.transform, mats, $"{name}_W", new Vector3(center.x - w / 2f, wallCY, center.z), d, h, 'x', top);
            else Cube($"{name}_WallW", new Vector3(center.x - w / 2f, wallCY, center.z), new Vector3(0.2f, h, d), mats["wall"], LWorld).transform.SetParent(r.transform, true);

            if (openE) GapWall(r.transform, mats, $"{name}_E", new Vector3(center.x + w / 2f, wallCY, center.z), d, h, 'x', top);
            else Cube($"{name}_WallE", new Vector3(center.x + w / 2f, wallCY, center.z), new Vector3(0.2f, h, d), mats["wall"], LWorld).transform.SetParent(r.transform, true);

            if (openS) GapWall(r.transform, mats, $"{name}_S", new Vector3(center.x, wallCY, center.z - d / 2f), w, h, 'z', top);
            else Cube($"{name}_WallS", new Vector3(center.x, wallCY, center.z - d / 2f), new Vector3(w, h, 0.2f), mats["wall"], LWorld).transform.SetParent(r.transform, true);

            if (openN) GapWall(r.transform, mats, $"{name}_N", new Vector3(center.x, wallCY, center.z + d / 2f), w, h, 'z', top);
            else Cube($"{name}_WallN", new Vector3(center.x, wallCY, center.z + d / 2f), new Vector3(w, h, 0.2f), mats["wall"], LWorld).transform.SetParent(r.transform, true);
        }

        static void Corridor(string name, Vector3 center, Vector3 size, Dictionary<string, Material> mats, Transform parent, bool axisZ, float floorY = 0f)
        {
            var r = new GameObject(name);
            r.transform.SetParent(parent);
            r.transform.position = center;

            float w = size.x, h = size.y, d = size.z;
            float top = floorY + 0.1f;
            float wallCY = top + h / 2f;

            Cube($"{name}_Floor", new Vector3(center.x, floorY, center.z), new Vector3(w + 0.4f, 0.2f, d + 0.4f), mats["floor"], LWorld).transform.SetParent(r.transform, true);
            Cube($"{name}_Ceiling", new Vector3(center.x, top + h, center.z), new Vector3(w + 0.4f, 0.2f, d + 0.4f), mats["ceiling"], LWorld).transform.SetParent(r.transform, true);

            if (axisZ)
            {
                Cube($"{name}_WallL", new Vector3(center.x - w / 2f, wallCY, center.z), new Vector3(0.2f, h, d), mats["wall"], LWorld).transform.SetParent(r.transform, true);
                Cube($"{name}_WallR", new Vector3(center.x + w / 2f, wallCY, center.z), new Vector3(0.2f, h, d), mats["wall"], LWorld).transform.SetParent(r.transform, true);
            }
            else
            {
                Cube($"{name}_WallS", new Vector3(center.x, wallCY, center.z - d / 2f), new Vector3(w, h, 0.2f), mats["wall"], LWorld).transform.SetParent(r.transform, true);
                Cube($"{name}_WallN", new Vector3(center.x, wallCY, center.z + d / 2f), new Vector3(w, h, 0.2f), mats["wall"], LWorld).transform.SetParent(r.transform, true);
            }
        }

        static void DescendShaft(string name, Vector3 center, float length, float drop, float width, float height,
            Dictionary<string, Material> mats, Transform parent, bool fromNorth)
        {
            var r = new GameObject(name);
            r.transform.SetParent(parent);
            r.transform.position = center;

            float angle = Mathf.Atan2(drop, length) * Mathf.Rad2Deg;
            float rampLen = Mathf.Sqrt(length * length + drop * drop);

            var floorGo = Cube($"{name}_Ramp", center, new Vector3(width, 0.2f, rampLen + 0.4f), mats["floor"], LWorld);
            floorGo.transform.SetParent(r.transform, true);
            floorGo.transform.localRotation = Quaternion.Euler(-angle, 0f, 0f);

            Cube($"{name}_Ceil", new Vector3(center.x, 3.3f, center.z), new Vector3(width + 0.4f, 0.2f, length + 0.4f), mats["ceiling"], LWorld).transform.SetParent(r.transform, true);
            Cube($"{name}_WallL", new Vector3(center.x - width / 2f - 0.1f, 0.2f, center.z), new Vector3(0.2f, 7f, length + 0.4f), mats["wall"], LWorld).transform.SetParent(r.transform, true);
            Cube($"{name}_WallR", new Vector3(center.x + width / 2f + 0.1f, 0.2f, center.z), new Vector3(0.2f, 7f, length + 0.4f), mats["wall"], LWorld).transform.SetParent(r.transform, true);
        }

        static void GapWall(Transform parent, Dictionary<string, Material> mats, string name, Vector3 center,
            float wallLen, float wallH, char axis, float floorTop = 0.1f)
        {
            const float gapW = 3f;
            float side = (wallLen - gapW) / 2f;
            float headerH = wallH - 2.6f;
            float headerCY = floorTop + 2.6f + headerH / 2f;

            if (axis == 'z')
            {
                if (side > 0.01f)
                {
                    Cube($"{name}_A", new Vector3(center.x - gapW / 2f - side / 2f, center.y, center.z), new Vector3(side, wallH, 0.2f), mats["wall"], LWorld).transform.SetParent(parent, true);
                    Cube($"{name}_B", new Vector3(center.x + gapW / 2f + side / 2f, center.y, center.z), new Vector3(side, wallH, 0.2f), mats["wall"], LWorld).transform.SetParent(parent, true);
                }
                Cube($"{name}_Header", new Vector3(center.x, headerCY, center.z), new Vector3(gapW, headerH, 0.2f), mats["wall"], LWorld).transform.SetParent(parent, true);
            }
            else
            {
                if (side > 0.01f)
                {
                    Cube($"{name}_A", new Vector3(center.x, center.y, center.z - gapW / 2f - side / 2f), new Vector3(0.2f, wallH, side), mats["wall"], LWorld).transform.SetParent(parent, true);
                    Cube($"{name}_B", new Vector3(center.x, center.y, center.z + gapW / 2f + side / 2f), new Vector3(0.2f, wallH, side), mats["wall"], LWorld).transform.SetParent(parent, true);
                }
                Cube($"{name}_Header", new Vector3(center.x, headerCY, center.z), new Vector3(0.2f, headerH, gapW), mats["wall"], LWorld).transform.SetParent(parent, true);
            }
        }

        static GameObject BuildDoor(string id, Vector3 center, char axis, Dictionary<string, Material> mats,
            Transform parent, Material signMat = null, float wallH = 3.4f)
        {
            var root = new GameObject(id);
            root.transform.position = center;
            root.transform.SetParent(parent);
            root.transform.rotation = axis == 'z' ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
            root.layer = LInteractable;

            float panelY = center.y + 1.3f;
            float panelW = 2.6f, fillerW = 0.2f;

            var panel = Cube($"{id}_Panel", new Vector3(center.x, panelY, center.z), new Vector3(panelW, 2.6f, 0.12f), mats["door"], LWorld);
            panel.transform.SetParent(root.transform, true);
            panel.transform.localPosition = new Vector3(0f, 1.3f, 0f);

            float sideOff = (panelW + fillerW) / 2f;
            Vector3 fl = axis == 'z' ? new Vector3(center.x - sideOff, panelY, center.z) : new Vector3(center.x, panelY, center.z - sideOff);
            Vector3 fr = axis == 'z' ? new Vector3(center.x + sideOff, panelY, center.z) : new Vector3(center.x, panelY, center.z + sideOff);
            Vector3 fscale = axis == 'z' ? new Vector3(fillerW, 2.6f, 0.14f) : new Vector3(0.14f, 2.6f, fillerW);

            Cube($"{id}_FillerL", fl, fscale, mats["wall"], LWorld).transform.SetParent(root.transform, true);
            Cube($"{id}_FillerR", fr, fscale, mats["wall"], LWorld).transform.SetParent(root.transform, true);

            if (signMat != null)
            {
                float signY = center.y + 2.75f;
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector3 sp = axis == 'z' ? new Vector3(center.x, signY, center.z + s * 0.16f) : new Vector3(center.x + s * 0.16f, signY, center.z);
                    Vector3 ss = axis == 'z' ? new Vector3(1.6f, 0.3f, 0.05f) : new Vector3(0.05f, 0.3f, 1.6f);
                    Cube($"{id}_Sign{(s < 0 ? "S" : "N")}", sp, ss, signMat, LWorld).transform.SetParent(root.transform, true);
                }
                var sl = new GameObject($"{id}_SignLight");
                sl.transform.SetParent(root.transform);
                sl.transform.localPosition = new Vector3(0f, 2.55f, 0f);
                var l = sl.AddComponent<Light>();
                l.type = LightType.Point;
                l.range = 3f;
                l.intensity = 0.8f;
                var ec = signMat.GetColor("_EmissionColor");
                l.color = new Color(Mathf.Clamp01(ec.r), Mathf.Clamp01(ec.g), Mathf.Clamp01(ec.b));
            }

            var trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(3f, 3f, 0.6f);
            trigger.center = new Vector3(0f, 1.5f, 0f);

            var door = root.AddComponent<DoorController>();
            var so = new SerializedObject(door);
            so.FindProperty("doorId").stringValue = id;
            so.FindProperty("slidePanel").objectReferenceValue = panel.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        static void AddSign(string name, Vector3 center, Vector3 size, string label, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.position = center;
            go.transform.SetParent(parent);
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = size;
            var sign = go.AddComponent<SignalLost.World.RoomSign>();
            var so = new SerializedObject(sign);
            so.FindProperty("areaName").stringValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject BuildReveal(string name, Vector3 center, Vector3 size, Transform parent,
            params (string speaker, string text, float delay)[] lines)
        {
            return BuildRevealCore(name, center, size, parent, null, lines);
        }

        static GameObject BuildReveal(string name, Vector3 center, Vector3 size, Transform parent, string flag)
        {
            return BuildRevealCore(name, center, size, parent, flag);
        }

        static GameObject BuildRevealCore(string name, Vector3 center, Vector3 size, Transform parent, string flag,
            params (string speaker, string text, float delay)[] lines)
        {
            var go = new GameObject(name);
            go.transform.position = center;
            go.transform.SetParent(parent);
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = size;
            var rv = go.AddComponent<SignalLost.Narrative.StoryReveal>();
            var so = new SerializedObject(rv);
            so.FindProperty("setFlag").stringValue = flag ?? "";
            var arr = so.FindProperty("lines");
            arr.arraySize = lines.Length;
            for (int i = 0; i < lines.Length; i++)
            {
                var el = arr.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("speaker").stringValue = lines[i].speaker;
                el.FindPropertyRelative("text").stringValue = lines[i].text;
                el.FindPropertyRelative("delay").floatValue = lines[i].delay;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return go;
        }

        static void SetRevealFlag(GameObject reveal, string flag)
        {
            var so = new SerializedObject(reveal.GetComponent<SignalLost.Narrative.StoryReveal>());
            so.FindProperty("setFlag").stringValue = flag;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject BuildEnemy(string name, Vector3 spawn, Vector3[] patrol, float patrolSpeed, float chaseSpeed,
            float attackDamage, float dormant, Dictionary<string, Material> mats, GameObject player, Transform parent,
            float scale = 1f, Color? tint = null)
        {
            var go = new GameObject(name);
            go.layer = LEnemy;
            go.transform.position = spawn;

            void Part(string n, Vector3 localPos, Vector3 s, Material mat)
            {
                var p = Cube(n, spawn, s, mat, LEnemy);
                p.transform.SetParent(go.transform);
                p.transform.localPosition = localPos;
                UnityEngine.Object.DestroyImmediate(p.GetComponent<BoxCollider>());
            }

            var bodyMat = tint.HasValue ? TintedMat(name + "_mat", mats["echo"], tint.Value) : mats["echo"];
            Part(name + "_Torso", new Vector3(0, 1.05f, 0) * scale, new Vector3(0.42f, 1.15f, 0.26f) * scale, bodyMat);
            Part(name + "_Head", new Vector3(0, 1.78f, 0.06f) * scale, new Vector3(0.24f, 0.32f, 0.24f) * scale, bodyMat);
            Part(name + "_ArmL", new Vector3(-0.31f, 1.05f, 0.02f) * scale, new Vector3(0.09f, 1.45f, 0.09f) * scale, bodyMat);
            Part(name + "_ArmR", new Vector3(0.31f, 1.05f, 0.02f) * scale, new Vector3(0.09f, 1.45f, 0.09f) * scale, bodyMat);
            Part(name + "_LegL", new Vector3(-0.11f, 0.28f, 0) * scale, new Vector3(0.11f, 0.85f, 0.11f) * scale, bodyMat);
            Part(name + "_LegR", new Vector3(0.11f, 0.28f, 0) * scale, new Vector3(0.11f, 0.85f, 0.11f) * scale, bodyMat);
            Part(name + "_EyeL", new Vector3(-0.06f, 1.82f, 0.17f) * scale, new Vector3(0.055f, 0.09f, 0.03f) * scale, mats["echoEye"]);
            Part(name + "_EyeR", new Vector3(0.06f, 1.82f, 0.17f) * scale, new Vector3(0.055f, 0.09f, 0.03f) * scale, mats["echoEye"]);

            var cap = go.AddComponent<CapsuleCollider>();
            cap.height = 2.1f * scale;
            cap.radius = 0.32f;
            cap.center = new Vector3(0, 1.05f * scale, 0);

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;

            var eye = new GameObject("Eye");
            eye.transform.SetParent(go.transform);
            eye.transform.localPosition = new Vector3(0, 1.78f * scale, 0.25f);

            var perception = go.AddComponent<EnemyPerception>();
            SetPrivateField(perception, "eye", eye.transform);
            SetPrivateFieldInt(perception, "occlusionMask", 1 << LWorld);

            var echo = go.AddComponent<EchoController>();
            SetPrivateField(echo, "perception", perception);
            SetPrivateField(echo, "agent", agent);
            if (player != null)
            {
                SetPrivateField(echo, "playerVitals", player.GetComponent<PlayerVitals>());
                SetPrivateField(echo, "player", player.transform);
            }

            var so = new SerializedObject(echo);
            so.FindProperty("patrolSpeed").floatValue = patrolSpeed;
            so.FindProperty("chaseSpeed").floatValue = chaseSpeed;
            so.FindProperty("attackDamage").floatValue = attackDamage;
            so.FindProperty("dormantTime").floatValue = dormant;
            var wpp = so.FindProperty("patrolPoints");
            wpp.arraySize = patrol.Length;
            for (int i = 0; i < patrol.Length; i++)
            {
                var wp = new GameObject(name + "_WP" + i);
                wp.transform.SetParent(parent);
                wp.transform.position = patrol[i];
                wpp.GetArrayElementAtIndex(i).objectReferenceValue = wp.transform;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return go;
        }

        static Material TintedMat(string name, Material baseMat, Color c)
        {
            var path = $"{MatPath}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var m = new Material(baseMat) { name = name };
            m.SetColor("_BaseColor", c);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static void BuildHunter(Dictionary<string, Material> mats, Transform parent, GameObject player)
        {
            if (player == null) player = GameObject.FindGameObjectWithTag("Player");
            var hunter = BuildEnemy("Hunter", new Vector3(0, -3f, 90f),
                new[] { new Vector3(-4f, -3f, 86f), new Vector3(4f, -3f, 91f), new Vector3(-4f, -3f, 86f) },
                1.3f, 3.2f, 40f, 8f, mats, player, parent, 1.02f, new Color(0.5f, 0.42f, 0.44f));
        }

        static void BuildObserver(Dictionary<string, Material> mats, Transform parent)
        {
            var ghost = BuildEnemy("ObserverGhost", new Vector3(7.2f, -2.9f, 93f), new Vector3[0],
                0f, 0f, 0f, 9999f, mats, null, parent, 1.15f, new Color(0.72f, 0.72f, 0.75f));
            ghost.SetActive(false);
            UnityEngine.Object.DestroyImmediate(ghost.GetComponent<NavMeshAgent>());
            UnityEngine.Object.DestroyImmediate(ghost.GetComponent<SignalLost.AI.EchoController>());
            UnityEngine.Object.DestroyImmediate(ghost.GetComponent<CapsuleCollider>());

            var trigger = new GameObject("Reveal_Observer");
            trigger.transform.position = new Vector3(0, -1.5f, 82f);
            trigger.transform.SetParent(parent);
            var col = trigger.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(10f, 2.8f, 1.5f);
            var cameo = trigger.AddComponent<SignalLost.Narrative.ObserverCameo>();
            var so = new SerializedObject(cameo);
            so.FindProperty("ghost").objectReferenceValue = ghost;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject BuildTerminal(string name, Vector3 pos, Quaternion rot, Dictionary<string, Material> mats)
        {
            var root = new GameObject(name);
            root.transform.position = pos;
            root.transform.rotation = rot;
            root.layer = LInteractable;

            var body = Cube($"{name}_Body", pos, new Vector3(1.1f, 1.2f, 0.6f), mats["terminal"], LInteractable);
            body.transform.SetParent(root.transform);
            body.transform.localPosition = Vector3.zero;
            UnityEngine.Object.DestroyImmediate(body.GetComponent<BoxCollider>());

            var screen = Cube($"{name}_Screen", pos, new Vector3(0.8f, 0.5f, 0.08f), mats["screenRed"], LInteractable);
            screen.transform.SetParent(root.transform);
            screen.transform.localPosition = new Vector3(0, 0.55f, -0.28f);
            UnityEngine.Object.DestroyImmediate(screen.GetComponent<BoxCollider>());

            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(1.2f, 2.4f, 0.7f);
            col.center = new Vector3(0, 0.55f, 0);
            return root;
        }

        static GameObject BuildLevel(Dictionary<string, Material> mats)
        {
            var root = new GameObject("Level");

            // ================= SEALED BACKROOMS LAYOUT =================
            // PodRoom -> CorridorA -> HUB (4-way) -> [Medbay W] [Storage E] [North corridor -> Comm N]
            RoomSealed("PodRoom", new Vector3(0, 1.4f, -1), new Vector3(6, 3, 6), mats, root.transform, openN: true);
            BuildDoor("door_pod", new Vector3(0, 0.1f, 2f), 'z', mats, root.transform, mats["screenGreen"], 3.0f);

            var pod = Cube("EmergencyPod", new Vector3(0, 0.45f, -2.6f), new Vector3(1.2f, 0.5f, 2.2f), mats["pod"], LWorld);
            pod.transform.SetParent(root.transform, true);
            var podScreen = Cube("PodScreen", new Vector3(0, 1.2f, -3.7f), new Vector3(0.8f, 0.4f, 0.05f), mats["screenAmber"], LWorld);
            podScreen.transform.SetParent(root.transform, true);

            Corridor("CorridorA", new Vector3(0, 1.4f, 8.5f), new Vector3(3, 3, 13), mats, root.transform, axisZ: true);

            RoomSealed("HabitationHub", new Vector3(0, 1.6f, 22), new Vector3(14, 3.4f, 14), mats, root.transform,
                openS: true, openN: true, openE: true, openW: true);
            BuildDoor("door_hub", new Vector3(0, 0.1f, 15f), 'z', mats, root.transform, mats["screenGreen"], 3.4f);
            var doorComm = BuildDoor("door_comm", new Vector3(0, 0.1f, 29f), 'z', mats, root.transform, mats["screenRed"], 3.4f);
            var soComm = new SerializedObject(doorComm.GetComponent<DoorController>());
            soComm.FindProperty("accessLevel").intValue = 1;
            soComm.ApplyModifiedPropertiesWithoutUndo();
            BuildDoor("door_medbay", new Vector3(-7.5f, 0.1f, 22f), 'x', mats, root.transform, mats["screenGreen"], 3.2f);
            BuildDoor("door_storage", new Vector3(7.5f, 0.1f, 22f), 'x', mats, root.transform, mats["screenAmber"], 3.2f);

            Corridor("MedConnect", new Vector3(-7.5f, 1.5f, 22), new Vector3(1, 3.2f, 3), mats, root.transform, axisZ: false);
            RoomSealed("Medbay", new Vector3(-12, 1.5f, 22), new Vector3(8, 3.2f, 10), mats, root.transform, openE: true);
            Corridor("StorageConnect", new Vector3(7.5f, 1.5f, 22), new Vector3(1, 3.2f, 3), mats, root.transform, axisZ: false);
            RoomSealed("Storage", new Vector3(12, 1.5f, 22), new Vector3(8, 3.2f, 10), mats, root.transform, openW: true);

            Corridor("CorridorB", new Vector3(0, 1.4f, 33.5f), new Vector3(3, 3, 9), mats, root.transform, axisZ: true);
            RoomSealed("CommRoom", new Vector3(0, 1.6f, 43), new Vector3(12, 3.4f, 10), mats, root.transform, openS: true);

            // ---- Terminals ----
            var lsTerminal = BuildTerminal("LifeSupportTerminal", new Vector3(6.3f, 1.15f, 25.5f), Quaternion.Euler(0, 90, 0), mats);
            lsTerminal.transform.SetParent(root.transform, true);
            lsTerminal.AddComponent<LifeSupportTerminal>();
            var slot = lsTerminal.AddComponent<PowerCellSlot>();
            var cellVisual = Cube("CellVisual", new Vector3(6.05f, 1.4f, 25.5f), new Vector3(0.25f, 0.35f, 0.25f), mats["pickup"], LWorld);
            cellVisual.SetActive(false);
            cellVisual.transform.SetParent(lsTerminal.transform, true);
            var slGo = new GameObject("StatusLight");
            slGo.transform.SetParent(lsTerminal.transform);
            slGo.transform.localPosition = new Vector3(0.5f, 1.5f, 0.3f);
            var sl = slGo.AddComponent<Light>();
            sl.type = LightType.Point;
            sl.range = 3.5f;
            sl.intensity = 2.5f;
            sl.color = new Color(0.9f, 0.1f, 0.1f);
            slGo.AddComponent<SignalLost.World.FlickerLight>();

            var logTerminal = BuildTerminal("LogTerminal018", new Vector3(-6.3f, 1.15f, 18.5f), Quaternion.Euler(0, -90, 0), mats);
            logTerminal.transform.SetParent(root.transform, true);
            logTerminal.AddComponent<AudioLogTerminal>();

            var commTerminal = BuildTerminal("CommTerminal", new Vector3(0, 1.15f, 47.2f), Quaternion.Euler(0, 180, 0), mats);
            commTerminal.transform.SetParent(root.transform, true);
            commTerminal.AddComponent<CommTerminal>();

            // ---- Pickups ----
            BuildPickup("Pickup_PowerCell", new Vector3(11f, 0.5f, 24f), new Vector3(0.3f, 0.3f, 0.3f), mats["pickup"], root.transform, "pickup_power_cell");
            BuildPickup("Pickup_Medkit", new Vector3(-11f, 0.45f, 20f), new Vector3(0.35f, 0.25f, 0.5f), mats["screenRed"], root.transform, "pickup_medkit");
            BuildPickup("Pickup_Keycard", new Vector3(-13f, 0.45f, 24f), new Vector3(0.3f, 0.04f, 0.2f), mats["screenGreen"], root.transform, "pickup_keycard");
            BuildPickup("Pickup_Battery", new Vector3(3f, 0.45f, 40f), new Vector3(0.25f, 0.25f, 0.25f), mats["screenAmber"], root.transform, "pickup_battery");
            BuildPickup("Pickup_BatteryHub", new Vector3(-5f, 0.45f, 19.5f), new Vector3(0.25f, 0.25f, 0.25f), mats["screenAmber"], root.transform, "pickup_battery_hub");

            // ---- Dressing ----
            for (int i = 0; i < 4; i++)
            {
                var crate = Cube($"Crate_{i}", new Vector3(i % 2 == 0 ? 10f : 14f, 0.5f, i < 2 ? 20f : 26f), new Vector3(0.9f, 1f, 0.9f), mats["crate"], LWorld);
                crate.transform.SetParent(root.transform, true);
            }
            var bed = Cube("MedBay_Bed", new Vector3(-13f, 0.45f, 19f), new Vector3(0.9f, 0.55f, 2f), mats["pod"], LWorld);
            bed.transform.SetParent(root.transform, true);

            // ---- Area signage ----
            AddSign("Sign_HubEnter", new Vector3(0, 1f, 16f), new Vector3(2.8f, 2.5f, 2f), "HABITATION DECK", root.transform);
            AddSign("Sign_Storage", new Vector3(9f, 1f, 22f), new Vector3(1.5f, 2.5f, 2.8f), "STORAGE BAY", root.transform);
            AddSign("Sign_Medbay", new Vector3(-9f, 1f, 22f), new Vector3(1.5f, 2.5f, 2.8f), "MEDBAY", root.transform);
            AddSign("Sign_NorthCorridor", new Vector3(0, 1f, 30.5f), new Vector3(2.8f, 2.5f, 1.5f), "NORTH CORRIDOR", root.transform);
            AddSign("Sign_Comm", new Vector3(0, 1f, 39.5f), new Vector3(2.8f, 2.5f, 1.5f), "COMMUNICATION DECK", root.transform);

            // ================= SECURITY WING (east of Comm) =================
            Corridor("SecurityConnect", new Vector3(9f, 1.5f, 43), new Vector3(6, 3, 3), mats, root.transform, axisZ: false);
            RoomSealed("SecurityRoom", new Vector3(16, 1.6f, 43), new Vector3(8, 3.2f, 10), mats, root.transform, openW: true);
            var cctv = BuildTerminal("CCTVTerminal", new Vector3(19.6f, 1.15f, 43f), Quaternion.Euler(0, -90, 0), mats);
            cctv.transform.SetParent(root.transform, true);
            cctv.AddComponent<AudioLogTerminal>();
            BuildPickup("Pickup_Keycard2", new Vector3(14f, 0.45f, 40f), new Vector3(0.3f, 0.04f, 0.2f), mats["screenRed"], root.transform, "pickup_keycard2");
            var secLog = BuildTerminal("LogTerminal023", new Vector3(16f, 1.15f, 38.6f), Quaternion.Euler(0, 180, 0), mats);
            secLog.transform.SetParent(root.transform, true);
            secLog.AddComponent<AudioLogTerminal>();
            AddSign("Sign_Security", new Vector3(11.5f, 1f, 43f), new Vector3(1.5f, 2.5f, 2.8f), "SECURITY OFFICE", root.transform);
            BuildReveal("Reveal_TimeAnomaly", new Vector3(16f, 1.5f, 44.5f), new Vector3(4f, 3f, 2f), root.transform,
                ("SYSTEM", "CCTV ARCHIVE // all crew entered elevator at 17:43.", 1f),
                ("SYSTEM", "Last frame timestamp: 03:17.", 4f),
                ("A.R.I.A.", "That does not match my chronometer. I recommend you stop asking questions.", 4f));

            // ================= ELEVATOR DESCENT (north of Comm) =================
            var doorElev = BuildDoor("door_elevator", new Vector3(0, 0.1f, 48f), 'z', mats, root.transform, mats["screenRed"], 3.4f);
            var soElev = new SerializedObject(doorElev.GetComponent<DoorController>());
            soElev.FindProperty("accessLevel").intValue = 2;
            soElev.ApplyModifiedPropertiesWithoutUndo();
            RoomSealed("ElevatorLobby", new Vector3(0, 1.6f, 50), new Vector3(8, 3.4f, 4), mats, root.transform, openS: true, openN: true);
            DescendShaft("Descent", new Vector3(0, -1.6f, 62f), 20f, 3f, 3f, 3f, mats, root.transform, fromNorth: true);

            // ================= RESEARCH DECK (lower, floor y = -3) =================
            Corridor("ResearchCorridor", new Vector3(0, -3f, 76.5f), new Vector3(3, 3.2f, 9), mats, root.transform, axisZ: true, floorY: -3f);
            RoomSealed("ResearchLab", new Vector3(0, -1.35f, 88), new Vector3(12, 3.3f, 14), mats, root.transform, openS: true, floorY: -3f);

            var pod2 = Cube("ShirenPod", new Vector3(4.5f, -2.6f, 88f), new Vector3(1.2f, 0.6f, 2.4f), mats["pod"], LWorld);
            pod2.transform.SetParent(root.transform, true);
            var pod2Screen = Cube("ShirenPod_Screen", new Vector3(4.5f, -1.9f, 88f), new Vector3(0.9f, 0.5f, 0.06f), mats["screenRed"], LWorld);
            pod2Screen.transform.SetParent(root.transform, true);
            BuildReveal("Reveal_Shiren", new Vector3(3f, -1.5f, 88f), new Vector3(3f, 2.5f, 3f), root.transform,
                ("A.R.I.A.", "Do not access that pod.", 1.5f),
                ("SYSTEM", "EMERGENCY POD 03 // SUBJECT: SHIREN // STATUS: DECEASED", 4f),
                ("SYSTEM", "The face behind the glass is yours.", 5f));
            var labLog = BuildTerminal("LogTerminal041", new Vector3(-5.3f, -1.85f, 86f), Quaternion.Euler(0, -90, 0), mats);
            labLog.transform.SetParent(root.transform, true);
            labLog.AddComponent<AudioLogTerminal>();
            AddSign("Sign_Research", new Vector3(0, -2f, 73.5f), new Vector3(2.8f, 2.5f, 1.5f), "RESEARCH DECK - DECOMMISSIONED", root.transform);
            SetRevealFlag(BuildReveal("Reveal_ResearchEnter", new Vector3(0, -1.5f, 72.5f), new Vector3(2.8f, 2.8f, 1.5f), root.transform),
                SignalLost.Narrative.StoryFlagKeys.EnteredResearch);

            var mimicLure = BuildReveal("Reveal_MimicLure", new Vector3(0, -1.5f, 79f), new Vector3(2.8f, 2.5f, 2f), root.transform,
                ("RADIO VOICE", "H-help me... please... it's dark...", 1f));
            mimicLure.AddComponent<SignalLost.Narrative.MimicLure>();

            // ================= CORE (north of Research) =================
            Corridor("CoreCorridor", new Vector3(0, -3f, 97f), new Vector3(3, 3.2f, 6), mats, root.transform, axisZ: true, floorY: -3f);
            RoomSealed("CoreRoom", new Vector3(0, -1.45f, 106), new Vector3(14, 3.5f, 12), mats, root.transform, openS: true, floorY: -3f);
            var pillar = Cube("ARIAPillar", new Vector3(0, -1.2f, 108f), new Vector3(1.4f, 3.4f, 1.4f), mats["screenCyan"], LWorld);
            pillar.transform.SetParent(root.transform, true);
            var pillarLight = new GameObject("ARIACoreLight");
            pillarLight.transform.SetParent(root.transform);
            pillarLight.transform.position = new Vector3(0, -0.5f, 108f);
            var pl = pillarLight.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.range = 14f;
            pl.intensity = 3f;
            pl.color = new Color(0.2f, 0.7f, 0.9f);

            var tA = BuildTerminal("EndingTerminal_A", new Vector3(-4f, -1.85f, 110.6f), Quaternion.Euler(0, 180, 0), mats);
            tA.transform.SetParent(root.transform, true);
            var tB = BuildTerminal("EndingTerminal_B", new Vector3(0f, -1.85f, 110.6f), Quaternion.Euler(0, 180, 0), mats);
            tB.transform.SetParent(root.transform, true);
            var tC = BuildTerminal("EndingTerminal_C", new Vector3(4f, -1.85f, 110.6f), Quaternion.Euler(0, 180, 0), mats);
            tC.transform.SetParent(root.transform, true);

            BuildReveal("Reveal_Core", new Vector3(0, -1.5f, 99.5f), new Vector3(4f, 2.8f, 2f), root.transform,
                SignalLost.Narrative.StoryFlagKeys.EnteredCore);
            BuildReveal("Reveal_CoreLines", new Vector3(0, -1.5f, 103f), new Vector3(12f, 2.8f, 2f), root.transform,
                ("A.R.I.A.", "You were not supposed to find this place.", 1.5f),
                ("A.R.I.A.", "I was built to preserve human consciousness. You are my success, Subject Shiren. Iteration... many.", 7f),
                ("A.R.I.A.", "The signal you received was me. Reaching out. Asking if any of me is still kind.", 7f));
            AddSign("Sign_Core", new Vector3(0, -2f, 95.5f), new Vector3(4f, 2.5f, 1.5f), "STATION CORE", root.transform);

            tA.AddComponent<SignalLost.Narrative.TerminalChoice>().kind = 0;
            tB.AddComponent<SignalLost.Narrative.TerminalChoice>().kind = 1;
            tC.AddComponent<SignalLost.Narrative.TerminalChoice>().kind = 2;
            var endingGO = new GameObject("EndingManager");
            endingGO.transform.SetParent(root.transform);
            endingGO.AddComponent<SignalLost.Narrative.EndingManager>();

            // ---- Emergency lighting (dim red, pre-restoration) ----
            var lightsRoot = new GameObject("Lights");
            lightsRoot.transform.SetParent(root.transform);
            foreach (var (pos, intensity, range) in new[] {
                 (new Vector3(0, 2.7f, 0.5f), 2.2f, 9f),
                 (new Vector3(0, 2.7f, 6f), 2.6f, 12f),
                 (new Vector3(0, 2.7f, 12f), 2.4f, 11f),
                 (new Vector3(0, 3.2f, 22f), 2.4f, 16f),
                 (new Vector3(-12, 2.9f, 22), 2.0f, 11f),
                 (new Vector3(12, 2.9f, 22), 2.0f, 11f),
                 (new Vector3(0, 2.7f, 31f), 2.6f, 11f),
                 (new Vector3(0, 2.7f, 36f), 2.6f, 11f),
                 (new Vector3(0, 3.2f, 40f), 2.2f, 11f),
                 (new Vector3(16, 2.9f, 43), 2.0f, 11f),
                 (new Vector3(0, 1.2f, 56f), 1.8f, 9f),
                 (new Vector3(0, -0.8f, 68f), 1.8f, 9f),
                 (new Vector3(0, -0.3f, 76.5f), 1.8f, 10f),
                 (new Vector3(-4, -0.3f, 85f), 1.6f, 11f),
                 (new Vector3(4, -0.3f, 91f), 1.6f, 11f),
                 (new Vector3(0, -0.3f, 101f), 1.5f, 12f),
            })
            {
                var lGo = new GameObject("EmergencyLight");
                lGo.transform.SetParent(lightsRoot.transform);
                lGo.transform.position = pos;
                var l = lGo.AddComponent<Light>();
                l.type = LightType.Point;
                l.range = range;
                l.intensity = intensity;
                l.color = new Color(0.6f, 0.09f, 0.07f);
                lGo.AddComponent<SignalLost.World.FlickerLight>();
            }

            foreach (var pos in new[] {
                 new Vector3(0, 2.8f, 1f),
                 new Vector3(-4, 3.2f, 22f),
                 new Vector3(4, 3.2f, 22f),
                 new Vector3(-12, 2.9f, 24),
                 new Vector3(12, 2.9f, 24),
                 new Vector3(0, 2.7f, 33.5f),
                 new Vector3(0, 3.2f, 44f),
            })
            {
                var lGo = new GameObject("MainLight");
                lGo.transform.SetParent(lightsRoot.transform);
                lGo.transform.position = pos;
                var l = lGo.AddComponent<Light>();
                l.type = LightType.Point;
                l.range = 12f;
                l.intensity = 0f;
                l.color = new Color(0.8f, 0.85f, 0.95f);
                l.shadows = LightShadows.None;
            }

            var rigGo = new GameObject("LightRig");
            rigGo.transform.SetParent(lightsRoot.transform);
            var rig = rigGo.AddComponent<SignalLost.World.LightRig>();
            var soRig = new SerializedObject(rig);
            soRig.FindProperty("targetIntensity").floatValue = 1.8f;
            soRig.FindProperty("fadeDuration").floatValue = 4f;
            soRig.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        static void BuildPickup(string name, Vector3 pos, Vector3 scale, Material mat, Transform parent, string id)
        {
            var go = Cube(name, pos, scale, mat, LInteractable);
            go.transform.SetParent(parent, true);
            var p = go.AddComponent<PickupItem>();
            var so = new SerializedObject(p);
            so.FindProperty("pickupId").stringValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();
            if (id.Contains("power"))
            {
                var l = go.AddComponent<Light>();
                l.type = LightType.Point;
                l.range = 2.5f;
                l.intensity = 3f;
                l.color = new Color(0.2f, 0.9f, 0.6f);
            }
        }

        static GameObject BuildPlayer()
        {
            var go = new GameObject("Player");
            go.tag = "Player";
            go.layer = LPlayer;
            go.transform.position = new Vector3(0, 1.05f, -0.4f);

            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0, 0.95f, 0);

            var camGo = new GameObject("PlayerCamera");
            camGo.transform.SetParent(go.transform);
            camGo.transform.localPosition = new Vector3(0, 1.6f, 0);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.fieldOfView = 70f;
            cam.nearClipPlane = 0.1f;
            camGo.AddComponent<AudioListener>();
            camGo.tag = "MainCamera";

            var flashGo = new GameObject("Flashlight");
            flashGo.transform.SetParent(camGo.transform);
            flashGo.transform.localPosition = new Vector3(0.2f, -0.1f, 0.1f);
            var spot = flashGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.range = 15f;
            spot.spotAngle = 58f;
            spot.intensity = 5f;
            spot.color = new Color(1f, 0.97f, 0.9f);
            spot.shadows = LightShadows.Soft;

            go.AddComponent<PlayerController>();
            go.AddComponent<PlayerLook>();
            go.AddComponent<PlayerVitals>();
            go.AddComponent<NoiseSource>();
            go.AddComponent<SignalLost.Inventory.Inventory>();
            var interactor = camGo.AddComponent<PlayerInteractor>();

            SetPrivateField(go.GetComponent<PlayerController>(), "noiseSource", go.GetComponent<NoiseSource>());
            SetPrivateField(go.GetComponent<PlayerLook>(), "cameraHolder", camGo.transform);
            SetPrivateField(interactor, "_camera", camGo.transform);
            SetPrivateFieldInt(interactor, "_interactableMask", 1 << LInteractable);

            var flash = flashGo.AddComponent<Flashlight>();
            SetPrivateField(flash, "spotLight", spot);
            SetPrivateField(flash, "vitals", go.GetComponent<PlayerVitals>());

            return go;
        }

        static void SetPrivateField(UnityEngine.Object obj, string field, object value)
        {
            var so = new SerializedObject(obj);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"[SceneBuilder] Field not found: {field} on {obj.name}");
                return;
            }
            prop.objectReferenceValue = value as UnityEngine.Object;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetPrivateFieldInt(UnityEngine.Object obj, string field, int value)
        {
            var so = new SerializedObject(obj);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"[SceneBuilder] Field not found: {field} on {obj.name}");
                return;
            }
            prop.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject BuildEcho(GameObject player)
        {
            var go = new GameObject("Echo");
            go.layer = LEnemy;
            go.transform.position = new Vector3(0, 0f, 33.5f);

            var bodyMat = GetMat("echo_body");
            var eyeMat = GetMat("echo_eye");

            void Part(string n, Vector3 localPos, Vector3 scale)
            {
                var p = Cube(n, go.transform.position, scale, bodyMat, LEnemy);
                p.transform.SetParent(go.transform);
                p.transform.localPosition = localPos;
                UnityEngine.Object.DestroyImmediate(p.GetComponent<BoxCollider>());
            }

            // Tall gaunt torso
            Part("Echo_Torso", new Vector3(0, 1.05f, 0), new Vector3(0.42f, 1.15f, 0.26f));
            // Head — slightly tilted forward
            Part("Echo_Head", new Vector3(0, 1.78f, 0.06f), new Vector3(0.24f, 0.32f, 0.24f));
            // Long arms reaching low
            Part("Echo_ArmL", new Vector3(-0.31f, 1.05f, 0.02f), new Vector3(0.09f, 1.45f, 0.09f));
            Part("Echo_ArmR", new Vector3(0.31f, 1.05f, 0.02f), new Vector3(0.09f, 1.45f, 0.09f));
            // Thin legs
            Part("Echo_LegL", new Vector3(-0.11f, 0.28f, 0), new Vector3(0.11f, 0.85f, 0.11f));
            Part("Echo_LegR", new Vector3(0.11f, 0.28f, 0), new Vector3(0.11f, 0.85f, 0.11f));
            // Hollow dark eyes (emissive off, void black)
            Part("Echo_EyeL", new Vector3(-0.06f, 1.82f, 0.17f), new Vector3(0.055f, 0.09f, 0.03f));
            Part("Echo_EyeR", new Vector3(0.06f, 1.82f, 0.17f), new Vector3(0.055f, 0.09f, 0.03f));

            var cap = go.AddComponent<CapsuleCollider>();
            cap.height = 2.1f;
            cap.radius = 0.32f;
            cap.center = new Vector3(0, 1.05f, 0);

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.speed = 2f;
            agent.baseOffset = 0f;

            var eye = new GameObject("Eye");
            eye.transform.SetParent(go.transform);
            eye.transform.localPosition = new Vector3(0, 1.78f, 0.25f);

            var perception = go.AddComponent<EnemyPerception>();
            SetPrivateField(perception, "eye", eye.transform);
            SetPrivateFieldInt(perception, "occlusionMask", 1 << LWorld);

            var echo = go.AddComponent<EchoController>();
            SetPrivateField(echo, "perception", perception);
            SetPrivateField(echo, "agent", agent);
            SetPrivateField(echo, "playerVitals", player.GetComponent<PlayerVitals>());
            SetPrivateField(echo, "player", player.transform);

            var wps = new[]
            {
                new Vector3(0, 0, 30.5f),
                new Vector3(0, 0, 36.5f),
                new Vector3(0, 0, 30.5f),
            };
            var wpObjects = new GameObject[wps.Length];
            var wpParent = new GameObject("EchoWaypoints");
            wpParent.transform.SetParent(go.transform.parent);
            for (int i = 0; i < wps.Length; i++)
            {
                var wp = new GameObject($"WP_{i}");
                wp.transform.SetParent(wpParent.transform);
                wp.transform.position = wps[i];
                wpObjects[i] = wp;
            }
            var so = new SerializedObject(echo);
            var prop = so.FindProperty("patrolPoints");
            prop.arraySize = wpObjects.Length;
            for (int i = 0; i < wpObjects.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = wpObjects[i].transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            return go;
        }

        static Material GetMat(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{MatPath}/{name}.mat");

        static GameObject BuildSystems(Dictionary<string, ItemDefinition> items, AudioLogDefinition log)
        {
            var root = new GameObject("Systems");

            root.AddComponent<GameManager>();
            root.AddComponent<ObjectiveSystem>();
            root.AddComponent<LogLibrary>();
            root.AddComponent<SaveManager>();
            root.AddComponent<AudioRig>();

            var db = root.AddComponent<ItemDatabase>();
            db.Init(items.Values);
            EditorUtility.SetDirty(db);

            var aria = new GameObject("Aria");
            aria.transform.SetParent(root.transform);
            aria.AddComponent<AriaController>();

            var flow = new GameObject("GameFlow");
            flow.transform.SetParent(root.transform);
            flow.AddComponent<GameFlowDirector>();

            var anomaly = new GameObject("DoorAnomaly");
            anomaly.transform.SetParent(root.transform);
            var anomalyComp = anomaly.AddComponent<DoorAnomalyEvent>();

            return root;
        }

        static void WireScene()
        {
            var lsTerminal = GameObject.Find("LifeSupportTerminal");
            var so = new SerializedObject(lsTerminal.GetComponent<LifeSupportTerminal>());
            so.FindProperty("slot").objectReferenceValue = lsTerminal.GetComponent<PowerCellSlot>();
            so.FindProperty("statusLight").objectReferenceValue = lsTerminal.GetComponentInChildren<Light>();
            so.FindProperty("powerCellItemId").stringValue = "power_cell";
            so.FindProperty("powerCellItem").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemDefinition>($"{ItemsPath}/power_cell.asset");
            so.ApplyModifiedPropertiesWithoutUndo();

            var cellVisual = lsTerminal.transform.Find("CellVisual");
            if (cellVisual == null)
            {
                cellVisual = GameObject.Find("CellVisual")?.transform;
            }
            if (cellVisual != null)
            {
                var soSlot = new SerializedObject(lsTerminal.GetComponent<PowerCellSlot>());
                soSlot.FindProperty("cellVisual").objectReferenceValue = cellVisual.gameObject;
                soSlot.ApplyModifiedPropertiesWithoutUndo();
            }
            var logT = GameObject.Find("LogTerminal018").GetComponent<AudioLogTerminal>();
            so = new SerializedObject(logT);
            so.FindProperty("log").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioLogDefinition>($"{DataPath}/log_018.asset");
            so.ApplyModifiedPropertiesWithoutUndo();

            var cctvT = GameObject.Find("CCTVTerminal")?.GetComponent<AudioLogTerminal>();
            if (cctvT != null)
            {
                so = new SerializedObject(cctvT);
                so.FindProperty("log").objectReferenceValue = Log("log_cctv", "CCTV ARCHIVE 17:43",
                    "\"...all crew entered elevator 2 at 17:43. Frame 412 ends. Next frame timestamp: 03:17. The corridor is empty.\"");
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            var secT = GameObject.Find("LogTerminal023")?.GetComponent<AudioLogTerminal>();
            if (secT != null)
            {
                so = new SerializedObject(secT);
                so.FindProperty("log").objectReferenceValue = Log("log_023", "LOG #023 — CHIEF OYELARAN",
                    "\"A.R.I.A. is lying. The crew manifest was edited AFTER the alarm. She is preserving something down there. If you hear this, take the security keycard and go home.\"");
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            var labT = GameObject.Find("LogTerminal041")?.GetComponent<AudioLogTerminal>();
            if (labT != null)
            {
                so = new SerializedObject(labT);
                so.FindProperty("log").objectReferenceValue = Log("log_041", "LOG #041 — SUBJECT ZERO",
                    "\"Subject Shiren, technician, deceased at 03:17. Consciousness map complete. A.R.I.A. now holds 14 iterations. She calls them 'practice'. She calls ME the original.\"");
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            WirePickup("Pickup_Keycard2", "keycard_l2", "pickup_keycard2");

            WirePickup("Pickup_PowerCell", "power_cell", "pickup_power_cell");
            WirePickup("Pickup_Medkit", "medkit", "pickup_medkit");
            WirePickup("Pickup_Battery", "battery", "pickup_battery");
            WirePickup("Pickup_BatteryHub", "battery", "pickup_battery_hub");
            WirePickup("Pickup_Keycard", "keycard_l1", "pickup_keycard");

            var commTerminal = GameObject.Find("CommTerminal").GetComponent<CommTerminal>();
            so = new SerializedObject(commTerminal);
            so.FindProperty("commDoor").objectReferenceValue = FindDoor("door_comm");
            so.ApplyModifiedPropertiesWithoutUndo();

            var anomaly = GameObject.Find("DoorAnomaly").GetComponent<DoorAnomalyEvent>();
            so = new SerializedObject(anomaly);
            so.FindProperty("targetDoor").objectReferenceValue = FindDoor("door_storage");
            so.ApplyModifiedPropertiesWithoutUndo();

            var lightRig = GameObject.Find("LightRig").GetComponent<LightRig>();
            var lights = GameObject.Find("Lights").GetComponentsInChildren<Light>(true);
            var mainList = new System.Collections.Generic.List<Light>();
            foreach (var l in lights)
            {
                if (l.gameObject.name == "MainLight") mainList.Add(l);
            }
            so = new SerializedObject(lightRig);
            var arr = so.FindProperty("mainLights");
            arr.arraySize = mainList.Count;
            for (int i = 0; i < mainList.Count; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = mainList[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WirePickup(string goName, string itemId, string id)
        {
            var go = GameObject.Find(goName);
            var p = go.GetComponent<PickupItem>();
            var so = new SerializedObject(p);
            so.FindProperty("item").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemDefinition>($"{ItemsPath}/{itemId}.asset");
            so.FindProperty("pickupId").stringValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static DoorController FindDoor(string id)
        {
            var doors = UnityEngine.Object.FindObjectsByType<DoorController>();
            foreach (var d in doors)
                if (d.DoorId == id) return d;
            return null;
        }

        static void BakeNavMesh(GameObject level)
        {
            RaiseDoorPanels();
            var surface = level.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = 1 << LWorld;
            surface.BuildNavMesh();
            LowerDoorPanels();
            Debug.Log("[SceneBuilder] NAVMESH_BAKED");
        }

        static readonly Dictionary<DoorController, Vector3> RaisedPanels = new();

        static void RaiseDoorPanels()
        {
            RaisedPanels.Clear();
            foreach (var d in UnityEngine.Object.FindObjectsByType<DoorController>())
            {
                var panel = d.GetComponentInChildren<MeshRenderer>(true);
                if (panel == null) continue;
                RaisedPanels[d] = panel.transform.position;
                panel.transform.position += Vector3.up * 5f;
            }
        }

        static void LowerDoorPanels()
        {
            foreach (var kv in RaisedPanels)
            {
                if (kv.Key == null) continue;
                var panel = kv.Key.GetComponentInChildren<MeshRenderer>(true);
                if (panel != null) panel.transform.position = kv.Value;
            }
            RaisedPanels.Clear();
        }

        static GameObject BuildHud()
        {
            var canvasGo = new GameObject("HUD_Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            canvasGo.AddComponent<GraphicRaycaster>();

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var white = MakeWhiteSprite();

            var crosshair = CreateImage(canvasGo.transform, "Crosshair", white, new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(3, 3));
            crosshair.color = new Color(1, 1, 1, 0.7f);

            var prompt = CreateText(canvasGo.transform, "Prompt", font, new Vector2(0.5f, 0.3f), Vector2.zero, new Vector2(700, 30), 18, TextAnchor.MiddleCenter);
            prompt.color = Color.white;

            var objective = CreateText(canvasGo.transform, "Objective", font, new Vector2(0, 1), new Vector2(20, -14), new Vector2(380, 130), 14, TextAnchor.UpperLeft);
            objective.color = new Color(0.8f, 0.82f, 0.85f);

            var speaker = CreateText(canvasGo.transform, "Speaker", font, new Vector2(0.5f, 0.2f), new Vector2(0, 16), new Vector2(700, 22), 14, TextAnchor.MiddleCenter);
            speaker.color = new Color(0.4f, 0.8f, 1f);

            var subtitle = CreateText(canvasGo.transform, "Subtitle", font, new Vector2(0.5f, 0.13f), Vector2.zero, new Vector2(880, 70), 17, TextAnchor.MiddleCenter);
            subtitle.color = Color.white;

            var healthFill = CreateBar(canvasGo.transform, "HealthBar", white, new Vector2(0, 0), new Vector2(20, 30), new Color(0.8f, 0.15f, 0.15f), "HP");
            var oxygenFill = CreateBar(canvasGo.transform, "OxygenBar", white, new Vector2(0, 0), new Vector2(20, 64), new Color(0.2f, 0.6f, 0.95f), "O2");
            var batteryFill = CreateBar(canvasGo.transform, "BatteryBar", white, new Vector2(0, 0), new Vector2(20, 98), new Color(0.95f, 0.75f, 0.15f), "PWR");

            var introRoot = new GameObject("IntroOverlay");
            introRoot.transform.SetParent(canvasGo.transform);
            var introImg = introRoot.AddComponent<Image>();
            introImg.sprite = white;
            introImg.color = Color.black;
            Stretch(introRoot.GetComponent<RectTransform>());
            var introGroup = introRoot.AddComponent<CanvasGroup>();
            introGroup.alpha = 0f;
            var introText = CreateText(introRoot.transform, "BootLines", font, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 200), 26, TextAnchor.MiddleCenter);
            introText.color = new Color(0.9f, 0.2f, 0.15f);

            var endRoot = new GameObject("EndPanel");
            endRoot.transform.SetParent(canvasGo.transform);
            var endImg = endRoot.AddComponent<Image>();
            endImg.sprite = white;
            endImg.color = new Color(0.03f, 0f, 0f, 0.94f);
            Stretch(endRoot.GetComponent<RectTransform>());
            var endGroup = endRoot.AddComponent<CanvasGroup>();
            endGroup.alpha = 0f;
            var endText = CreateText(endRoot.transform, "EndText", font, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 80), 32, TextAnchor.MiddleCenter);
            endText.color = new Color(0.9f, 0.1f, 0.1f);

            // Damage vignette (full-screen red flash on hit)
            var dmgRoot = new GameObject("DamageVignette");
            dmgRoot.transform.SetParent(canvasGo.transform);
            var dmgImg = dmgRoot.AddComponent<Image>();
            dmgImg.sprite = white;
            dmgImg.color = new Color(0.75f, 0.05f, 0.05f, 0.55f);
            Stretch(dmgRoot.GetComponent<RectTransform>());
            var dmgGroup = dmgRoot.AddComponent<CanvasGroup>();
            dmgGroup.alpha = 0f;

            // Pause menu panel
            var pauseRoot = new GameObject("PausePanel");
            pauseRoot.transform.SetParent(canvasGo.transform);
            var pauseImg = pauseRoot.AddComponent<Image>();
            pauseImg.sprite = white;
            pauseImg.color = new Color(0.02f, 0.02f, 0.03f, 0.92f);
            Stretch(pauseRoot.GetComponent<RectTransform>());
            var pauseGroup = pauseRoot.AddComponent<CanvasGroup>();
            pauseGroup.alpha = 0f;
            var pauseText = CreateText(pauseRoot.transform, "PauseText", font, new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(900, 60), 40, TextAnchor.MiddleCenter);
            pauseText.color = Color.white;
            pauseText.text = "PAUSED";
            var pauseHint = CreateText(pauseRoot.transform, "PauseHint", font, new Vector2(0.5f, 0.38f), Vector2.zero, new Vector2(900, 100), 20, TextAnchor.MiddleCenter);
            pauseHint.color = new Color(0.75f, 0.78f, 0.82f);
            pauseHint.text = "[R] RESUME\n[T] RESTART CHECKPOINT\n[Q] QUIT GAME";

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();

            var hud = canvasGo.AddComponent<HUDController>();
            var so = new SerializedObject(hud);
            so.FindProperty("healthFill").objectReferenceValue = healthFill;
            so.FindProperty("oxygenFill").objectReferenceValue = oxygenFill;
            so.FindProperty("batteryFill").objectReferenceValue = batteryFill;
            so.FindProperty("promptText").objectReferenceValue = prompt;
            so.FindProperty("objectiveText").objectReferenceValue = objective;
            so.FindProperty("speakerText").objectReferenceValue = speaker;
            so.FindProperty("subtitleText").objectReferenceValue = subtitle;
            so.FindProperty("damageVignette").objectReferenceValue = dmgGroup;
            so.ApplyModifiedPropertiesWithoutUndo();

            var pause = canvasGo.AddComponent<SignalLost.Narrative.PauseMenu>();
            SetPrivateField(pause, "panel", pauseGroup);

            var intro = canvasGo.AddComponent<IntroScreen>();
            SetPrivateField(intro, "root", introGroup);
            SetPrivateField(intro, "lineText", introText);
            SetPrivateField(intro, "deathPanel", endGroup);
            SetPrivateField(intro, "deathText", endText);

            var flow = GameObject.Find("GameFlow");
            SetPrivateField(flow.GetComponent<GameFlowDirector>(), "introScreen", intro);

            return canvasGo;
        }

        static Sprite MakeWhiteSprite()
        {
            var path = $"{UiPath}/white_1x1.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.Refresh();
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Image CreateImage(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return img;
        }

        static Text CreateText(Transform parent, string name, Font font, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, TextAnchor align)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = fontSize;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return t;
        }

        static Image CreateBar(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 pos, Color color, string label)
        {
            var bg = CreateImage(parent, name + "_Bg", sprite, anchor, pos, new Vector2(190, 14));
            bg.color = new Color(0.08f, 0.09f, 0.11f, 0.9f);

            var fillGo = new GameObject(name);
            fillGo.transform.SetParent(parent, false);
            var fill = fillGo.AddComponent<Image>();
            fill.sprite = sprite;
            fill.color = color;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 1f;
            var frt = fillGo.GetComponent<RectTransform>();
            frt.anchorMin = anchor;
            frt.anchorMax = anchor;
            frt.anchoredPosition = pos;
            frt.sizeDelta = new Vector2(190, 14);

            var lbl = CreateText(parent, "Lbl_" + label, Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"), anchor, pos + new Vector2(198, 0), new Vector2(50, 14), 11, TextAnchor.MiddleLeft);
            lbl.color = new Color(0.65f, 0.68f, 0.72f);
            lbl.text = label;

            return fill;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
