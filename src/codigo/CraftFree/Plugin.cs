using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace CraftFree
{
    [BepInPlugin("yuri.CraftFree", "CraftFree", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> Ligado;

        private void Awake()
        {
            Ligado = Config.Bind("Geral", "Ligado", true, "Nas mesas de craft, a receita sai sem o material. So no seu jogo.");
            new Harmony("yuri.CraftFree").PatchAll();
        }
    }

    public static class Mesa
    {
        public static bool fabricando;

        public static bool NaMesa(Recipe recipe)
        {
            if (!Plugin.Ligado.Value || recipe == null || recipe.m_craftingStation == null)
                return false;
            Player player = Player.m_localPlayer;
            if (player == null)
                return false;
            CraftingStation atual = player.GetCurrentCraftingStation();
            if (atual == null)
                return false;
            return atual.m_name == recipe.m_craftingStation.m_name;
        }
    }

    [HarmonyPatch(typeof(Player), "HaveRequirementItems")]
    public static class SemMaterial
    {
        private static void Postfix(Recipe piece, ref bool __result)
        {
            if (Mesa.NaMesa(piece))
                __result = true;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "DoCrafting")]
    public static class Craftando
    {
        private static void Prefix()
        {
            Mesa.fabricando = true;
        }

        private static void Postfix()
        {
            Mesa.fabricando = false;
        }
    }

    [HarmonyPatch(typeof(Player), "ConsumeResources")]
    public static class NaoGasta
    {
        private static bool Prefix()
        {
            if (!Mesa.fabricando)
                return true;
            return false;
        }
    }
}
