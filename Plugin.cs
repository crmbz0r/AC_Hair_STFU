using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Runtime.CompilerServices;
using AC.Scene;
using AC.Scene.Explore;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Character;
using CharacterCreation;
using CharacterCreation.UI.View.Hair;
using H;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using ILLGAMES.ADV;
using ILLGAMES.Unity.Component;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace AC_Hair
{
    [BepInPlugin("AC_Hair", "AC_Hair", "0.0.1")]
    public class Plugin : BasePlugin
    {
        public override void Load()
        {
            LogDebugConfig = base.Config.Bind<bool>(
                "Logging",
                "Enable Debug Logging",
                false,
                "Show LogDebug messages in the BepInEx console."
            );

            LogInfoConfig = base.Config.Bind<bool>(
                "Logging",
                "Enable Info Logging",
                false,
                "Show LogInfo messages in the BepInEx console."
            );
            
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            
            Plugin.Log = base.Log;
            Plugin.AlphaConfig = base.Config.Bind<float>(
                "General",
                "Alpha",
                0.8f,
                new ConfigDescription(
                    "",
                    new AcceptableValueRange<float>(0f, 1f),
                    Array.Empty<object>()
                )
            );
            BackHairConfig = base.Config.Bind<bool>("Option", "Rear Hair", false, "");
            FrontHairConfig = base.Config.Bind<bool>("Option", "Front Hair", true, "");
            SideHairConfig = base.Config.Bind<bool>("Option", "Side Hair", true, "");
            OptionHairConfig = base.Config.Bind<bool>("Option", "Extension Hair", true, "");
            Plugin.AlphaConfig.SettingChanged += delegate(object sender, EventArgs args)
            {
                Plugin.Hooks.updateColor();
            };
            Plugin.patchedHooks = Harmony.CreateAndPatchAll(typeof(Plugin.Hooks), null);
            foreach (var name in typeof(Plugin).Assembly.GetManifestResourceNames())
                Plugin.Log.LogInfo($"Resource: {name}");
        }

        public override bool Unload()
        {
            AssetBundle assetBundle = Plugin.bundle;
            if (assetBundle != null)
            {
                assetBundle.Unload(true);
            }
            Harmony harmony = Plugin.patchedHooks;
            if (harmony != null)
            {
                harmony.UnpatchSelf();
            }
            return true;
        }

        internal static new ManualLogSource Log;

        internal static ConfigEntry<bool> LogDebugConfig;

        internal static ConfigEntry<bool> LogInfoConfig;

        internal static ConfigEntry<float> AlphaConfig;

        internal static ConfigEntry<bool> BackHairConfig;

        internal static ConfigEntry<bool> FrontHairConfig;

        internal static ConfigEntry<bool> SideHairConfig;

        internal static ConfigEntry<bool> OptionHairConfig;

        private static Harmony patchedHooks;

        private static AssetBundle bundle;

        internal static class Hooks
        {
            private static void updateColor(Material dst, Material src)
            {
                dst.CopyPropertiesFromMaterial(src);
                dst.SetFloat("_Alpha", Plugin.AlphaConfig.Value);
                dst.renderQueue = 2860;
            }

            private static void updateHair(HumanHair.Hair hair)
            {
                bool flag = Plugin.Hooks.shader == null;
                if (flag)
                {
                    bool flag2 = Plugin.bundle == null;
                    if (flag2)
                    {
                        Plugin.bundle = AssetBundle.LoadFromMemory(Resources.hairoverlay);
                    }
                    Il2CppReferenceArray<UnityEngine.Object> il2CppReferenceArray =
                        Plugin.bundle.LoadAllAssets(Il2CppType.Of<Shader>());
                    Plugin.Hooks.shader = (
                        (il2CppReferenceArray != null)
                            ? il2CppReferenceArray
                                .FirstOrDefault<UnityEngine.Object>()
                                .TryCast<Shader>()
                            : null
                    );
                    UnityEngine.Object.DontDestroyOnLoad(shader);
                }
                bool flag3 = hair.infoHair == null || hair.objHair == null;
                if (!flag3)
                {
                    ManualLogSource log = Plugin.Log;
                    if (Plugin.LogInfoConfig.Value)
                    {
                        Plugin.Log.LogInfo($"ChangeHair {hair.infoHair?.Name}");
                    }
                    bool flag5 =
                        hair.objHair.GetComponentsInChildren<SkinnedMeshRenderer>().Count
                        != hair.renderers.Length;
                    if (!flag5)
                    {
                        IEnumerable<Renderer> enumerable =
                            from v in hair.renderers
                            where v.material.shader.name == "AC/hair"
                            select v;
                        Material material = null;
                        foreach (Renderer renderer in enumerable)
                        {
                            Renderer renderer2 = UnityEngine.Object.Instantiate(renderer);
                            renderer2.shadowCastingMode = ShadowCastingMode.Off;
                            renderer2.gameObject.active = renderer.gameObject.activeInHierarchy;
                            renderer2.transform.SetParent(renderer.transform.parent);
                            bool flag6 = material == null;
                            if (flag6)
                            {
                                material = UnityEngine.Object.Instantiate(renderer.sharedMaterial);
                                material.name = Plugin.Hooks.materialName;
                                material.shader = Plugin.Hooks.shader;
                                Plugin.Hooks.updateColor(material, renderer.sharedMaterial);
                            }
                            renderer2.sharedMaterial = material;
                        }
                    }
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(
                typeof(HumanHair),
                "ChangeHair",
                new Type[] { typeof(ChaFileDefine.HairKind), typeof(int), typeof(bool) }
            )]
            [HarmonyPatch(typeof(HumanHair), "ChangeHairBack", new Type[] { typeof(bool) })]
            [HarmonyPatch(typeof(HumanHair), "ChangeHairFront", new Type[] { typeof(bool) })]
            [HarmonyPatch(typeof(HumanHair), "ChangeHairSide", new Type[] { typeof(bool) })]
            [HarmonyPatch(typeof(HumanHair), "ChangeHairOption", new Type[] { typeof(bool) })]
            public static void Postfix_HumanHair_ChangeHair(ref HumanHair __instance)
            {
                bool flag = __instance._hairs.Count < 4;
                if (!flag)
                {
                    bool value = Plugin.BackHairConfig.Value;
                    if (value)
                    {
                        Plugin.Hooks.updateHair(__instance._hairs[0]);
                    }
                    bool value2 = Plugin.FrontHairConfig.Value;
                    if (value2)
                    {
                        Plugin.Hooks.updateHair(__instance._hairs[1]);
                    }
                    bool value3 = Plugin.SideHairConfig.Value;
                    if (value3)
                    {
                        Plugin.Hooks.updateHair(__instance._hairs[2]);
                    }
                    bool value4 = Plugin.OptionHairConfig.Value;
                    if (value4)
                    {
                        Plugin.Hooks.updateHair(__instance._hairs[3]);
                    }
                }
            }

            public static void updateColor(HumanHair.Hair hair)
            {
                List<Renderer> list;
                if (hair == null)
                {
                    list = null;
                }
                else
                {
                    Il2CppReferenceArray<Renderer> renderers = hair.renderers;
                    if (renderers == null)
                    {
                        list = null;
                    }
                    else
                    {
                        list = renderers
                            .Where(
                                delegate(Renderer v)
                                {
                                    string a;
                                    if (v == null)
                                    {
                                        a = null;
                                    }
                                    else
                                    {
                                        Material material = v.material;
                                        if (material == null)
                                        {
                                            a = null;
                                        }
                                        else
                                        {
                                            Shader shader = material.shader;
                                            a = ((shader != null) ? shader.name : null);
                                        }
                                    }
                                    return a == "AC/hair";
                                }
                            )
                            .ToList<Renderer>();
                    }
                }
                List<Renderer> list2 = list;
                List<SkinnedMeshRenderer> list3;
                if (hair == null)
                {
                    list3 = null;
                }
                else
                {
                    GameObject objHair = hair.objHair;
                    if (objHair == null)
                    {
                        list3 = null;
                    }
                    else
                    {
                        list3 = objHair
                            .GetComponentsInChildren<SkinnedMeshRenderer>()
                            .Where(
                                delegate(SkinnedMeshRenderer v)
                                {
                                    bool result;
                                    if (v == null)
                                    {
                                        result = false;
                                    }
                                    else
                                    {
                                        Material material = v.material;
                                        bool? flag3;
                                        if (material == null)
                                        {
                                            flag3 = null;
                                        }
                                        else
                                        {
                                            string name = material.name;
                                            flag3 = (
                                                (name != null)
                                                    ? new bool?(
                                                        name.StartsWith(Plugin.Hooks.materialName)
                                                    )
                                                    : null
                                            );
                                        }
                                        bool? flag4 = flag3;
                                        result = flag4.GetValueOrDefault();
                                    }
                                    return result;
                                }
                            )
                            .ToList<SkinnedMeshRenderer>();
                    }
                }
                List<SkinnedMeshRenderer> list4 = list3;
                bool flag;
                if (list2 != null)
                {
                    int count = list2.Count;
                    int? num = (list4 != null) ? new int?(list4.Count) : null;
                    flag = (count == num.GetValueOrDefault() & num != null);
                }
                else
                {
                    flag = false;
                }
                bool flag2 = flag;
                if (flag2)
                {
                    for (int i = 0; i < list2.Count; i++)
                    {
                        Plugin.Hooks.updateColor(list4[i].sharedMaterial, list2[0].sharedMaterial);
                    }
                }
            }

            public static void updateColor(HumanHair __instance)
            {
                bool flag = __instance == null || __instance._hairs.Count < 4;
                if (!flag)
                {
                    bool value = Plugin.BackHairConfig.Value;
                    if (value)
                    {
                        Plugin.Hooks.updateColor(__instance._hairs[0]);
                    }
                    bool value2 = Plugin.FrontHairConfig.Value;
                    if (value2)
                    {
                        Plugin.Hooks.updateColor(__instance._hairs[1]);
                    }
                    bool value3 = Plugin.SideHairConfig.Value;
                    if (value3)
                    {
                        Plugin.Hooks.updateColor(__instance._hairs[2]);
                    }
                    bool value4 = Plugin.OptionHairConfig.Value;
                    if (value4)
                    {
                        Plugin.Hooks.updateColor(__instance._hairs[3]);
                    }
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(HairEdit), "UpdateColor")]
            [HarmonyPatch(typeof(HairEdit), "UpdateHairGlossColor")]
            [HarmonyPatch(typeof(HairEdit), "UpdateHairInnerColor")]
            [HarmonyPatch(typeof(HairEdit), "UpdateHairMeshColor")]
            [HarmonyPatch(typeof(HairEdit), "UpdateHairOutlineColor")]
            [HarmonyPatch(typeof(HairEdit), "UpdateHairShadowColor")]
            [HarmonyPatch(typeof(HairEdit), "UpdateHairAllColor")]
            [HarmonyPatch(typeof(HairEdit), "UpdateAllHairGlossColor")]
            [HarmonyPatch(typeof(HairEdit), "UpdateAllHairInnerColor")]
            [HarmonyPatch(typeof(HairEdit), "UpdateAllHairMeshColor")]
            [HarmonyPatch(typeof(HairEdit), "UpdateAllHairOutlineColor")]
            [HarmonyPatch(typeof(HairEdit), "UpdateAllHairShadowColor")]
            public static void Postfix_HairEdit(ref HairEdit __instance)
            {
                Plugin.Hooks.updateColor(__instance._humanHair);
            }

            [HarmonyPostfix]
            [HarmonyPatch(
                typeof(HumanGraphic),
                "SetTexture",
                new Type[] { typeof(Material), typeof(int), typeof(Texture) }
            )]
            [HarmonyPatch(
                typeof(HumanGraphic),
                "SetValue",
                new Type[] { typeof(Material), typeof(int), typeof(float) }
            )]
            public static void Postfix_HumanGraphic_SetValue(Material material)
            {
                bool flag = material.shader.name == "AC/hair";
                if (flag)
                {
                    if (Plugin.LogDebugConfig.Value)
                        Plugin.Log.LogDebug("updateColor on HumanGraphic.SetTexture/SetValue");
                    Plugin.Hooks.updateColor();
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(HScene), "InitializeActors")]
            public static void Postfix_HScene_InitializeActors()
            {
                if (Plugin.LogDebugConfig.Value)
                    Plugin.Log.LogDebug("updateColor on HScene.InitializeActors");
                Plugin.Hooks.updateColor();
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(TextScenario), "CrossFadeStart")]
            [HarmonyPatch(typeof(TextScenario), "ChangeCurrentChara")]
            public static void Postfix_TextScenario_ChangeCurrentChara(TextScenario __instance)
            {
                if (Plugin.LogDebugConfig.Value)
                    Plugin.Log.LogDebug(
                        "updateColor on TextScenario.ChangeCurrentChara/CrossFadeStart"
                    );
                Plugin.Hooks.updateColor();
            }

            public static void updateColor()
            {
                bool flag;
                if (SingletonInitializer<HumanCustom>.Initialized)
                {
                    Human human = SingletonInitializer<HumanCustom>.Instance.Human;
                    flag = (((human != null) ? human.hair : null) != null);
                }
                else
                {
                    flag = false;
                }
                bool flag2 = flag;
                if (flag2)
                {
                    Plugin.Hooks.updateColor(SingletonInitializer<HumanCustom>.Instance.Human.hair);
                }
                else
                {
                    bool flag3 = SingletonInitializer<HScene>.Initialized && HScene.IsActive();
                    if (flag3)
                    {
                        foreach (
                            Human human2 in SingletonInitializer<HScene>.Instance._humanAttackers
                        )
                        {
                            Plugin.Hooks.updateColor(human2.hair);
                        }
                        foreach (
                            Human human3 in SingletonInitializer<HScene>.Instance._humanReceivers
                        )
                        {
                            Plugin.Hooks.updateColor(human3.hair);
                        }
                    }
                    else
                    {
                        bool initialized = SceneSingleton<ExploreScene>.Initialized;
                        if (initialized)
                        {
                            foreach (Actor actor in SceneSingleton<ExploreScene>.Instance._actors)
                            {
                                Plugin.Hooks.updateColor(actor.Params.Chara.hair);
                            }
                        }
                        else
                        {
                            foreach (
                                GameObject gameObject in SceneManager
                                    .GetActiveScene()
                                    .GetRootGameObjects()
                            )
                            {
                                foreach (
                                    HumanComponent humanComponent in gameObject.GetComponentsInChildren<HumanComponent>()
                                )
                                {
                                    Plugin.Hooks.updateColor(humanComponent._human.hair);
                                }
                            }
                        }
                    }
                }
            }

            private static Shader shader;

            private static string materialName = "Overlay Hair Material";
        }

        [GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
        [DebuggerNonUserCode]
        [CompilerGenerated]
        internal class Resources
        {
            private static ResourceManager resourceMan;
            private static CultureInfo resourceCulture;

            internal Resources() { }

            [EditorBrowsable(EditorBrowsableState.Advanced)]
            internal static ResourceManager ResourceManager
            {
                get
                {
                    if (resourceMan == null)
                        resourceMan = new ResourceManager(
                            "AC_Hair_STFU.Resources",
                            typeof(Resources).Assembly
                        );
                    return resourceMan;
                }
            }

            [EditorBrowsable(EditorBrowsableState.Advanced)]
            internal static CultureInfo Culture
            {
                get { return resourceCulture; }
                set { resourceCulture = value; }
            }

            internal static byte[] hairoverlay
            {
                get { return (byte[])ResourceManager.GetObject("hairoverlay", resourceCulture); }
            }
        }
    }
}
