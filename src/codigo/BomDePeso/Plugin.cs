using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace BomDePeso
{
    [BepInPlugin("yuri.BomDePeso", "Carry Weight", "1.0.1")]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> Ligado;
        public static ConfigEntry<float> ExtraMax;

        private void Awake()
        {
            Ligado = Config.Bind("General", "Enabled", true, "Turns on the extra carry weight in this game. Only your own game needs this mod.");
            ExtraMax = Config.Bind("Weight", "MaxExtra", 300f, "Extra weight at Run skill 100. At zero, the extra is 0.");
            Harmony harmonia = new Harmony("yuri.BomDePeso");
            harmonia.PatchAll();
        }

        public static float Fator(Player jogador)
        {
            float nivel = jogador.GetSkillLevel(Skills.SkillType.Run);
            if (nivel < 0f)
                nivel = 0f;
            if (nivel > 100f)
                nivel = 100f;
            return nivel / 100f;
        }
    }

    [HarmonyPatch(typeof(Player), "GetMaxCarryWeight")]
    public static class PesoPatch
    {
        private static void Postfix(Player __instance, ref float __result)
        {
            if (!Plugin.Ligado.Value)
                return;
            if (Player.m_localPlayer != __instance)
                return;
            float extra = Plugin.ExtraMax.Value;
            if (extra < 0f)
                extra = 0f;
            __result += extra * Plugin.Fator(__instance);
        }
    }
}
