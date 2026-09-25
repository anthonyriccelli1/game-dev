using System;
using System.Collections.Generic;
using System.IO;
using RestaurantCity;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class PrototypeBuilder {
    const string ScenePath = "Assets/Scenes/RestaurantCity.unity";
    static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
    static readonly List<Light> lamps = new List<Light>();
    static Material asphalt, stone, cream, coral, teal, dark, glass, wood, leaf, gold;
    static Material textMaterial;
    static Font textFont;

    [MenuItem("Restaurant City/Open Prototype")]
    public static void Open() { EditorSceneManager.OpenScene(ScenePath); }

    [MenuItem("Restaurant City/Rebuild Prototype Scene")]
    public static void Generate() {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        materials.Clear(); lamps.Clear(); Directory.CreateDirectory("Assets/Generated");
        ArtPackDressing.GenerateOverrides();
        asphalt = Mat("Asphalt", "344650"); stone = Mat("Sidewalk", "C5C6B9"); cream = Mat("Cream", "F4E8CD");
        coral = Mat("Coral", "E87760"); teal = Mat("Teal", "39877F"); dark = Mat("Ink", "203743");
        glass = Mat("Windows", "547C89"); wood = Mat("Warm wood", "A66C4C"); leaf = Mat("Foliage", "699777"); gold = Mat("Gold", "EEBD68");
        var game = new GameObject("Restaurant City / Systems").AddComponent<CityGame>();
        textFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Generated/WorldText.mat");
        if (!textMaterial) { textMaterial = new Material(Shader.Find("RestaurantCity/WorldText")); AssetDatabase.CreateAsset(textMaterial, "Assets/Generated/WorldText.mat"); }
        textMaterial.mainTexture = textFont.material.mainTexture;
        var fontBinding = game.gameObject.AddComponent<WorldTextFont>(); fontBinding.Font = textFont; fontBinding.Material = textMaterial;
        game.gameObject.AddComponent<CityHud>().Game = game;
        var world = new GameObject("Market Row / World").transform;
        bool city = CityMap.Available;
        if (city) Cube("Ground", new Vector3(0, -.6f, 0), new Vector3(200, .6f, 200), stone, world);
        else {
        Cube("Ground", new Vector3(0, -.35f, 8), new Vector3(60, .6f, 62), stone, world);
        Cube("Street", new Vector3(0, -.025f, 0), new Vector3(48, .05f, 10), asphalt, world);
        for (int i = -5; i <= 5; i++) Cube("Lane marking", new Vector3(i * 4, .01f, 0), new Vector3(1.8f, .02f, .10f), cream, world, false);
        for (int i = 0; i < 7; i++) {
            Cube("Crosswalk", new Vector3(-7, .02f, -3.6f + i * 1.2f), new Vector3(2.6f, .025f, .55f), cream, world, false);
            Cube("Crosswalk", new Vector3(8, .02f, -3.6f + i * 1.2f), new Vector3(2.6f, .025f, .55f), cream, world, false);
        }
        for (int side = -1; side <= 1; side += 2) {
            Cube("Curb", new Vector3(0, .09f, side * 5.2f), new Vector3(48, .18f, .28f), cream, world);
            for (int i = -11; i < 12; i++) Cube("Paving seam", new Vector3(i * 2, .015f, side * 8), new Vector3(.025f, .02f, 5.4f), Mat("Seam", "AAAFA4"), world, false);
        }
        }
        if (!city) { Building("Milo's Supply", -13, 17, 12, 10, teal, world); Building("Apartments", -1, 19, 10, 14, coral, world); }
        RivalRestaurantShell(world);
        if (!city) Building("Corner cafe", -20, -15, 7, 9, Mat("Sage", "8DAB91"), world);
        Building("Future Restaurant", -10, -15, 10, 11, Mat("Brick", "BC896D"), world);
        if (!city) { Building("Records", 2, -16, 10, 14, teal, world); Building("Bodega", 16, -15, 13, 10, Mat("Mustard", "CEAE70"), world); }
        if (city) CityMap.Build(world);
        else {
            Cube("North district boundary", new Vector3(0, 2, 32), new Vector3(58, 4, 1), dark, world);
            Cube("West boundary", new Vector3(-25, 3, 6), new Vector3(1, 6, 54), teal, world);
            Cube("East boundary", new Vector3(25, 3, 6), new Vector3(1, 6, 54), teal, world);
            Cube("South boundary", new Vector3(0, 3, -24), new Vector3(58, 6, 1), dark, world);
        }

        var supply = Cube("Supplier counter", new Vector3(-12, .65f, 9), new Vector3(4, 1.3f, 1.5f), teal, world);
        supply.AddComponent<Interactable>().Kind = InteractionKind.Supplier;
        Awning(new Vector3(-12, 3.3f, 9), 5, teal, world);
        Sign("MILO'S SUPPLY", new Vector3(-12, 2.6f, 9.1f), 4.8f, teal, world, .18f);
        Label("FRESH PACKS  /  3 FOR $6", new Vector3(-12, 1, 8.23f), .11f, cream.color, world);
        Person("Milo", new Vector3(-12, 0, 10.1f), gold, world);
        for (int i = 0; i < 3; i++) {
            Cube("Produce crate", new Vector3(-13 + i, 1.42f, 9), new Vector3(.75f, .3f, .8f), wood, world, false);
            for (int j = 0; j < 3; j++) Sphere("Produce", new Vector3(-13.23f + i + j * .22f, 1.65f, 8.8f), Vector3.one * .22f, i == 1 ? leaf : coral, world);
        }

        game.SetupMarker = Cube("Your first stand / interact to set up", new Vector3(0, .45f, 8), new Vector3(5.7f, .9f, 1.4f), coral, world);
        game.SetupMarker.AddComponent<Interactable>().Kind = InteractionKind.Stand;
        Label("YOUR FIRST STAND", new Vector3(0, 1.6f, 7.3f), .24f, dark.color, game.SetupMarker.transform);
        Label("SET UP  /  $10", new Vector3(0, .55f, 7.28f), .16f, cream.color, game.SetupMarker.transform);
        game.Stand = new GameObject("Your food stand"); game.Stand.transform.parent = world;
        var stand = game.Stand.transform;
        var counter = Cube("Service counter", new Vector3(2.2f, .7f, 8), new Vector3(1.9f, 1.4f, 1.6f), coral, stand);
        counter.AddComponent<Interactable>().Kind = InteractionKind.Serve;
        Cube("Service surface", new Vector3(2.2f, 1.43f, 8), new Vector3(2.0f, .08f, 1.7f), cream, stand, false);
        var prep = Cube("Prep station", new Vector3(-2.2f, .7f, 8), new Vector3(1.9f, 1.4f, 1.6f), coral, stand);
        prep.AddComponent<Interactable>().Kind = InteractionKind.Prep;
        Cube("Cutting board", new Vector3(-2.2f, 1.43f, 8), new Vector3(1.5f, .10f, 1.2f), wood, stand, false);
        var grill = Cube("Grill station", new Vector3(0, .7f, 8), new Vector3(1.9f, 1.4f, 1.6f), dark, stand);
        grill.AddComponent<Interactable>().Kind = InteractionKind.Grill;
        for (int i = 0; i < 9; i++) Cube("Grill bar", new Vector3(-.8f + i * .2f, 1.43f, 8), new Vector3(.055f, .07f, 1.35f), stone, stand, false);
        Label("01  PREP", new Vector3(-2.2f, .92f, 7.17f), .15f, cream.color, stand);
        Label("02  GRILL", new Vector3(0, .92f, 7.17f), .15f, cream.color, stand);
        Label("03  SERVE", new Vector3(2.2f, .92f, 7.17f), .15f, cream.color, stand);
        Awning(new Vector3(0, 3.5f, 8), 7, coral, stand);
        Sign("LITTLE FLAME", new Vector3(0, 3.03f, 7.22f), 6.6f, coral, stand, .27f);
        for (int i = -1; i <= 1; i += 2) Cube("Canopy post", new Vector3(i * 3.35f, 1.65f, 8.6f), new Vector3(.12f, 3.3f, .12f), wood, stand);
        var bin = Cube("Discard bin", new Vector3(4.1f, .55f, 8), new Vector3(.8f, 1.1f, .8f), dark, world);
        bin.AddComponent<Interactable>().Kind = InteractionKind.Bin;
        Label("BIN", new Vector3(4.1f, .9f, 7.59f), .12f, cream.color, world);
        game.GrillFood = Burger(new Vector3(0, 1.6f, 8), stand);
        game.Customer = Person("Waiting customer", new Vector3(2.2f, 0, 5.9f), Mat("Customer jacket", "DCB955"), world);
        game.Customer.AddComponent<Interactable>().Kind = InteractionKind.Serve;
        Label("ONE BURGER, PLEASE", new Vector3(2.2f, 2.35f, 5.9f), .10f, dark.color, game.Customer.transform);

        var future = Cube("Future restaurant sign", new Vector3(-6.2f, 1.25f, -8.7f), new Vector3(2.9f, 2.2f, .15f), dark, world);
        future.AddComponent<Interactable>().Kind = InteractionKind.FutureRestaurant;
        var futureText = Label("LITTLE FLAME\n\nRESTAURANT LEASE\nBUY FOR $150", new Vector3(-6.2f, 1.35f, -8.58f), .15f, cream.color, world);
        futureText.transform.rotation = Quaternion.Euler(0, 180, 0);
        Sign("RIVAL ALLEY", new Vector3(11.6f, 3.8f, 14), 5, dark, world, .21f);
        Label("NIGHTS ONLY  /  ENTER AT YOUR OWN RISK", new Vector3(11.6f, 3.2f, 13.98f), .10f, coral.color, world);
        Cube("Alley left post", new Vector3(8.9f, 1.9f, 14), new Vector3(.15f, 3.8f, .15f), dark, world);
        Cube("Alley right post", new Vector3(14.3f, 1.9f, 14), new Vector3(.15f, 3.8f, .15f), dark, world);
        var stash = Cube("Midnight recipe stash", new Vector3(11.6f, .7f, 26), new Vector3(1.3f, 1.4f, 1), gold, world);
        stash.AddComponent<Interactable>().Kind = InteractionKind.Recipe;
        Label("MIDNIGHT RECIPE", new Vector3(11.6f, 1.85f, 25.5f), .14f, cream.color, world);
        game.RecipeGlow = Sphere("Recipe beacon", new Vector3(11.6f, 2.4f, 26), Vector3.one * .45f, gold, world);
        var guardObject = new GameObject("Alley rival"); guardObject.transform.SetParent(world); guardObject.transform.position = new Vector3(11.6f, 0, 21);
        game.Guard = guardObject.AddComponent<StreetGuard>(); game.Guard.Game = game;
        var guardBody = Person("Rival body", guardObject.transform.position, Mat("Rival coat", "483055"), guardObject.transform);
        game.Guard.Body = guardBody.transform; game.Guard.Coat = guardBody.transform.Find("Torso").GetComponent<Renderer>();

        for (int i = -2; i <= 2; i++) { StreetLamp(new Vector3(i * 10, 0, -6), world); if (i != 0) StreetLamp(new Vector3(i * 10, 0, 6), world); }
        StreetLamp(new Vector3(13.5f, 0, 25), world);
        ArtPackDressing.Dress(world);
        Tree(new Vector3(-20, 0, 7), world); Tree(new Vector3(20, 0, 7), world);
        Tree(new Vector3(6, 0, -8), world); Tree(new Vector3(-3, 0, -8), world);
        for (int i = 0; i < 2; i++) {
            var table = Cylinder("Outdoor table", new Vector3(-5.5f - i * 2, .85f, 11.5f), new Vector3(1.2f, .07f, 1.2f), cream, world);
            Cylinder("Table leg", new Vector3(table.transform.position.x, .4f, 11.5f), new Vector3(.15f, .4f, .15f), dark, world);
            Cylinder("Stool", new Vector3(table.transform.position.x, .35f, 10.6f), new Vector3(.6f, .35f, .6f), coral, world);
        }
        var sun = new GameObject("Afternoon sun").AddComponent<Light>(); sun.type = LightType.Directional;
        sun.transform.rotation = Quaternion.Euler(48, -35, 0); sun.intensity = 1.25f; sun.shadows = LightShadows.Soft;
        game.Sun = sun; game.Lamps = lamps.ToArray();
        RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.66f, .74f, .78f);
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = city ? .0045f : .011f;
        var player = new GameObject("Player / First person"); player.transform.position = game.SpawnPoint;
        var controller = player.AddComponent<CharacterController>(); controller.height = 1.8f; controller.radius = .3f; controller.center = new Vector3(0, .9f, 0); controller.stepOffset = .3f;
        game.Player = player.AddComponent<FirstPersonPlayer>(); game.Player.Game = game;
        var camera = new GameObject("Player camera").AddComponent<Camera>(); camera.tag = "MainCamera"; camera.transform.parent = player.transform; camera.transform.localPosition = new Vector3(0, 1.65f, 0);
        camera.nearClipPlane = .05f; camera.farClipPlane = city ? 520 : 180; camera.fieldOfView = 72; camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.61f, .80f, .83f); camera.gameObject.AddComponent<AudioListener>(); game.Player.View = camera;
        var tool = new GameObject("Spatula").transform; tool.SetParent(camera.transform); tool.localPosition = new Vector3(.38f, -.4f, .75f); tool.localRotation = Quaternion.Euler(-25, -10, -16); tool.localScale = Vector3.one * .6f;
        var handle = Cube("Spatula handle", Vector3.zero, new Vector3(.05f, .45f, .05f), wood, tool, false); handle.transform.localPosition = Vector3.zero;
        var blade = Cube("Spatula blade", Vector3.zero, new Vector3(.20f, .23f, .035f), stone, tool, false); blade.transform.localPosition = new Vector3(0, .3f, 0);
        game.Player.Spatula = tool;
        game.HandFood = Burger(Vector3.zero, camera.transform); game.HandFood.transform.localPosition = new Vector3(-.38f, -.31f, .63f); game.HandFood.transform.localScale = Vector3.one * .65f;
        foreach (var collider in game.HandFood.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
        game.Stand.SetActive(false); game.Customer.SetActive(false); game.GrillFood.SetActive(false); game.HandFood.SetActive(false); game.RecipeGlow.SetActive(false);
        PlayerSettings.companyName = "AntDev"; PlayerSettings.productName = "Restaurant City";
        PlayerSettings.defaultScreenWidth = 1440; PlayerSettings.defaultScreenHeight = 900; PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.runInBackground = true; PlayerSettings.resizableWindow = true;
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.SaveAssets();
        if (SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.LookAt(new Vector3(0, 1.5f, 6), Quaternion.Euler(25, 0, 0), 22);
        Validate();
        Debug.Log("RESTAURANT_CITY_SCENE_READY");
    }
    public static void Validate() {
        var game = UnityEngine.Object.FindFirstObjectByType<CityGame>();
        if (!game || !game.Player || !game.Player.View || !game.Guard || !game.Stand || !game.SetupMarker || !game.Customer || !game.Sun) throw new Exception("Scene is missing required references");
        var kinds = new HashSet<InteractionKind>();
        foreach (var item in UnityEngine.Object.FindObjectsByType<Interactable>(FindObjectsInactive.Include, FindObjectsSortMode.None)) kinds.Add(item.Kind);
        // Milo's supply crates are created at runtime by PhysicalStand.
        foreach (InteractionKind kind in Enum.GetValues(typeof(InteractionKind))) if (kind != InteractionKind.SupplyProtein && kind != InteractionKind.SupplyProduce && kind != InteractionKind.StandSign && !kinds.Contains(kind)) throw new Exception("Missing interaction: " + kind);
        foreach (var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
            if (!renderer.sharedMaterial || !renderer.sharedMaterial.shader) throw new Exception("Missing material: " + renderer.name);
        }
        Debug.Log("RESTAURANT_CITY_VALIDATION_PASSED");
    }
    [MenuItem("Restaurant City/Build Windows Player")]
    public static void Build() {
        Open(); Validate(); Directory.CreateDirectory("Builds/Windows");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { ScenePath }, locationPathName = "Builds/Windows/RestaurantCity.exe",
            target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
        });
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Windows build failed: " + report.summary.result);
        Debug.Log("RESTAURANT_CITY_BUILD_PASSED " + report.summary.totalSize);
    }
    public static void GenerateAndBuild() { Generate(); Build(); }

    static Material Mat(string name, string hex) {
        if (materials.TryGetValue(name, out var result)) return result;
        string path = "Assets/Generated/" + name + ".mat";
        result = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!result) { result = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(result, path); }
        ColorUtility.TryParseHtmlString("#" + hex, out Color color); result.color = color; result.SetFloat("_Smoothness", .15f); materials[name] = result; return result;
    }
    static GameObject Shape(string name, PrimitiveType primitive, Vector3 p, Vector3 scale, Material mat, Transform parent, bool collision = true) {
        var obj = GameObject.CreatePrimitive(primitive); obj.name = name; obj.transform.SetParent(parent); obj.transform.position = p; obj.transform.localScale = scale; obj.transform.localRotation = Quaternion.identity;
        obj.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collision) UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
        return obj;
    }
    static GameObject Cube(string n, Vector3 p, Vector3 s, Material m, Transform t, bool collision = true) => Shape(n, PrimitiveType.Cube, p, s, m, t, collision);
    static GameObject Sphere(string n, Vector3 p, Vector3 s, Material m, Transform t) => Shape(n, PrimitiveType.Sphere, p, s, m, t, false);
    static GameObject Cylinder(string n, Vector3 p, Vector3 s, Material m, Transform t) => Shape(n, PrimitiveType.Cylinder, p, s, m, t);
    static GameObject Label(string text, Vector3 p, float size, Color color, Transform parent) {
        var obj = new GameObject(text); obj.transform.parent = parent; obj.transform.position = p;
        var label = obj.AddComponent<TextMesh>(); label.font = textFont; label.text = text; label.characterSize = size * .25f; label.fontSize = 64; label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center; label.color = color;
        obj.GetComponent<MeshRenderer>().sharedMaterial = textMaterial;
        return obj;
    }
    static void Sign(string text, Vector3 p, float width, Material mat, Transform parent, float size) {
        Cube(text + " signboard", p, new Vector3(width, .66f, .12f), mat, parent);
        Label(text, p + new Vector3(0, 0, -.071f), size, cream.color, parent);
    }
    static void Awning(Vector3 p, float width, Material mat, Transform parent) {
        int count = Mathf.CeilToInt(width / .45f);
        for (int i = 0; i < count; i++) Cube("Striped awning", p + new Vector3(-width / 2 + (i + .5f) * width / count, 0, 0), new Vector3(width / count, .18f, 2.8f), i % 2 == 0 ? mat : cream, parent);
    }
    // Walk-in shell of the rival four-star restaurant (x 14..24, z 16.5..23.5). Interior is furnished at runtime.
    static void RivalRestaurantShell(Transform parent) {
        var root = new GameObject("Rival restaurant / The Gilded Orbit").transform; root.parent = parent;
        var obsidian = Mat("Obsidian", "1C1B2B"); var gold = Mat("Rival gold", "D9A441"); var plum = Mat("Plum velvet", "4A2548");
        var marble = Mat("Marble floor", "E9E1D3"); var glassDark = Mat("Smoked glass", "3E5B73");
        float x0 = 14, x1 = 24.3f, zf = 16.5f, zb = 29.4f, h = 4.6f; float cx0 = (x0 + x1) / 2, cz = (zf + zb) / 2, depth = zb - zf, width = x1 - x0;
        Cube("Marble floor", new Vector3(cx0, .04f, cz), new Vector3(width, .08f, depth), marble, root);
        Cube("Back wall", new Vector3(cx0, h / 2, zb - .1f), new Vector3(width, h, .2f), plum, root);
        Cube("Left wall", new Vector3(x0 + .1f, h / 2, cz), new Vector3(.2f, h, depth), plum, root);
        Cube("Right wall", new Vector3(x1 - .1f, h / 2, cz), new Vector3(.2f, h, depth), plum, root);
        if (CityMap.Available) {
            // Upper floors come from the city pack (CityMap.GildedTower); here only the ceiling slab and a gold band.
            Cube("Ceiling", new Vector3(cx0, h + .15f, cz), new Vector3(width, .3f, depth), obsidian, root);
            Cube("Gold band", new Vector3(cx0, h + .02f, zf - .02f), new Vector3(width + .2f, .16f, .2f), gold, root, false);
        } else {
            Cube("Upper floors", new Vector3(cx0, h + 3.8f, cz), new Vector3(width + .2f, 7.6f, depth + .2f), obsidian, root);
            Cube("Roof cornice", new Vector3(cx0, h + 7.7f, cz), new Vector3(width + .6f, .4f, depth + .6f), gold, root);
            for (float wx = 15.2f; wx < 23.5f; wx += 2.1f) for (float y = 6.4f; y < 11.8f; y += 2.6f) {
                Cube("Gold window frame", new Vector3(wx, y, zf - .02f), new Vector3(1.4f, 1.9f, .16f), gold, root, false);
                Cube("Lit window", new Vector3(wx, y, zf - .08f), new Vector3(1.15f, 1.62f, .08f), Mat("Warm window", "F2C27A"), root, false);
            }
        }
        // Glass frontage with a centered doorway.
        foreach (var seg in new[] { new Vector2(x0, 18.1f), new Vector2(19.9f, x1) }) {
            float cx = (seg.x + seg.y) / 2, w = seg.y - seg.x;
            Cube("Front base", new Vector3(cx, .3f, zf), new Vector3(w, .6f, .3f), obsidian, root);
            Cube("Front glass", new Vector3(cx, 2.25f, zf), new Vector3(w, 3.3f, .08f), glassDark, root);
            for (float px = seg.x; px <= seg.y + .01f; px += w / 2) Cube("Gold mullion", new Vector3(px, 2.25f, zf - .05f), new Vector3(.1f, 3.3f, .14f), gold, root, false);
        }
        Cube("Front header", new Vector3(cx0, 4.2f, zf), new Vector3(width, .8f, .3f), obsidian, root);
        Cube("Door frame left", new Vector3(18.05f, 2, zf - .05f), new Vector3(.12f, 4, .2f), gold, root);
        Cube("Door frame right", new Vector3(19.95f, 2, zf - .05f), new Vector3(.12f, 4, .2f), gold, root);
        Cube("Red carpet", new Vector3(19, .03f, zf - 1.2f), new Vector3(1.6f, .04f, 2.4f), Mat("Carpet", "A3243B"), root, false);
        for (int s = -1; s <= 1; s += 2) {
            Cube("Rope post", new Vector3(19 + s * 1.1f, .5f, zf - 1.9f), new Vector3(.12f, 1, .12f), gold, root);
            Cube("Velvet rope", new Vector3(19 + s * 1.1f, .85f, zf - 1.2f), new Vector3(.05f, .05f, 1.4f), plum, root, false);
        }
        Cube("Marquee", new Vector3(19, 5.1f, zf - .45f), new Vector3(9.2f, 1.1f, .6f), obsidian, root);
        Cube("Marquee trim", new Vector3(19, 4.52f, zf - .76f), new Vector3(9.4f, .08f, .1f), gold, root, false);
        var name = Label("THE GILDED ORBIT", new Vector3(19, 5.22f, zf - .78f), .3f, new Color(.98f, .78f, .35f), root);
        var stars = Label("FOUR STARS  *  *  *  *   RESERVATIONS ONLY", new Vector3(19, 4.78f, zf - .78f), .11f, new Color(1, .93f, .8f), root);
    }
    static void Building(string name, float x, float z, float width, float height, Material mat, Transform parent) {
        var root = new GameObject(name).transform; root.parent = parent;
        Cube("Building", new Vector3(x, height / 2, z), new Vector3(width, height, 7), mat, root);
        Cube("Roof cornice", new Vector3(x, height, z), new Vector3(width + .4f, .35f, 7.4f), cream, root);
        float front = z > 0 ? z - 3.53f : z + 3.53f;
        Cube("Shopfront", new Vector3(x, 1.8f, front), new Vector3(width - 1, 3.3f, .15f), dark, root);
        Cube("Door", new Vector3(x, 1.5f, front + (z > 0 ? -.09f : .09f)), new Vector3(1.5f, 2.9f, .08f), glass, root);
        for (float y = 5; y < height - 1; y += 3) for (float wx = -width / 2 + 1.4f; wx < width / 2 - .5f; wx += 2.4f) {
            Cube("Window trim", new Vector3(x + wx, y, front), new Vector3(1.5f, 1.9f, .18f), cream, root);
            Cube("Window", new Vector3(x + wx, y, front + (z > 0 ? -.1f : .1f)), new Vector3(1.22f, 1.64f, .08f), glass, root);
        }
        Cube("Roof equipment", new Vector3(x - width / 4, height + .7f, z), new Vector3(2, 1.2f, 1.8f), dark, root);
    }
    static GameObject Person(string name, Vector3 p, Material shirt, Transform parent) {
        var root = new GameObject(name); root.transform.parent = parent; root.transform.position = p;
        var torso = Shape("Torso", PrimitiveType.Capsule, p + Vector3.up * 1.05f, new Vector3(.62f, .5f, .48f), shirt, root.transform, false);
        var bodyCollider = root.AddComponent<CapsuleCollider>(); bodyCollider.center = new Vector3(0, 1, 0); bodyCollider.height = 2; bodyCollider.radius = .34f;
        Sphere("Head", p + Vector3.up * 1.77f, Vector3.one * .46f, Mat("Skin", "D7AC87"), root.transform);
        Cube("Cap", p + new Vector3(0, 1.99f, -.04f), new Vector3(.52f, .12f, .6f), dark, root.transform, false);
        for (int i = -1; i <= 1; i += 2) {
            Cube("Leg", p + new Vector3(i * .16f, .35f, 0), new Vector3(.2f, .7f, .24f), dark, root.transform, false);
            Cube("Shoe", p + new Vector3(i * .16f, .10f, -.09f), new Vector3(.25f, .16f, .4f), dark, root.transform, false);
            Sphere("Eye", p + new Vector3(i * .09f, 1.81f, -.212f), Vector3.one * .045f, dark, root.transform);
        }
        return root;
    }
    static GameObject Burger(Vector3 p, Transform parent) {
        var root = new GameObject("Burger"); root.transform.parent = parent; root.transform.position = p;
        Cylinder("Bun bottom", p, new Vector3(.48f, .045f, .48f), gold, root.transform);
        Cylinder("Patty", p + Vector3.up * .085f, new Vector3(.5f, .035f, .5f), wood, root.transform);
        Cube("Lettuce", p + Vector3.up * .145f, new Vector3(.48f, .03f, .48f), leaf, root.transform, false);
        Sphere("Bun top", p + Vector3.up * .22f, new Vector3(.49f, .22f, .49f), gold, root.transform);
        foreach (var collider in root.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
        return root;
    }
    static void Tree(Vector3 p, Transform parent) {
        if (CityMap.Available) { CityMap.Prefab("Environments/SM_Env_Tree_0" + (Mathf.Abs((int)(p.x + p.z)) % 3 + 1), p, p.x * 17, parent); return; }
        Cube("Planter", p + Vector3.up * .3f, new Vector3(1.5f, .6f, 1.5f), cream, parent);
        Cylinder("Tree trunk", p + Vector3.up * 1.7f, new Vector3(.25f, 1.7f, .25f), wood, parent);
        Sphere("Tree crown", p + Vector3.up * 3.5f, new Vector3(2.7f, 2.5f, 2.4f), leaf, parent);
        Sphere("Tree crown", p + new Vector3(.7f, 4, .2f), new Vector3(1.7f, 1.7f, 1.7f), leaf, parent);
    }
    static void StreetLamp(Vector3 p, Transform parent) {
        if (CityMap.Available) {
            bool alley = p.z > 10;
            CityMap.Prefab(alley ? "Props/SM_Prop_LightPole_Base_02" : "Props/SM_Prop_LightPole_Base_01", p, p.z < 0 ? 0 : 180, parent);
            var l = new GameObject("Warm street light").AddComponent<Light>(); l.transform.parent = parent;
            l.transform.position = p + Vector3.up * (alley ? 4.5f : 5.8f) + (alley ? Vector3.zero : new Vector3(0, 0, p.z < 0 ? 2.1f : -2.1f));
            l.type = LightType.Point; l.range = 12; l.color = new Color(1, .69f, .38f); l.intensity = .2f; lamps.Add(l);
            return;
        }
        Cylinder("Street lamp post", p + Vector3.up * 2, new Vector3(.09f, 2, .09f), dark, parent);
        Cube("Lantern", p + Vector3.up * 4, new Vector3(.36f, .5f, .36f), gold, parent, false);
        var light = new GameObject("Warm street light").AddComponent<Light>(); light.transform.parent = parent; light.transform.position = p + Vector3.up * 3.8f;
        light.type = LightType.Point; light.range = 9; light.color = new Color(1, .69f, .38f); light.intensity = .2f; lamps.Add(light);
    }
}
