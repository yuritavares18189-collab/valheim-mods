using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace CozinhaLivre
{
    [BepInPlugin("yuri.CozinhaLivre", "Free Cooking", "1.0.1")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Inst;
        public static bool pularCusto;
        public static FieldInfo CampoReceita;
        public static FieldInfo CampoUpgrade;

        ConfigEntry<bool> Ligado;

        void Awake()
        {
            Inst = this;
            Ligado = Config.Bind("General", "Enabled", true, "Known food recipes craft without using ingredients. Only your own game needs this mod.");
            CampoReceita = typeof(InventoryGui).GetField("m_craftRecipe", BindingFlags.Instance | BindingFlags.NonPublic);
            CampoUpgrade = typeof(InventoryGui).GetField("m_craftUpgradeItem", BindingFlags.Instance | BindingFlags.NonPublic);
            Harmony harmonia = new Harmony("yuri.CozinhaLivre");
            harmonia.PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo("Free Cooking 1.0.1");
        }

        public static bool Ativo()
        {
            return Inst != null && Inst.Ligado.Value;
        }

        public void Avisar(string nome)
        {
            Logger.LogInfo("Food without ingredients: " + nome);
        }

        public static bool ComidaConhecida(Player jogador, Recipe receita)
        {
            if (jogador == null || receita == null || receita.m_item == null)
                return false;
            if (receita.m_item.m_itemData == null || receita.m_item.m_itemData.m_shared == null)
                return false;
            ItemDrop.ItemData.SharedData dados = receita.m_item.m_itemData.m_shared;
            if (dados.m_food <= 0f && dados.m_foodStamina <= 0f && dados.m_foodEitr <= 0f)
                return false;
            return jogador.IsRecipeKnown(dados.m_name);
        }
    }

    [HarmonyPatch(typeof(Player), "HaveRequirementItems")]
    public static class ItensDaComida
    {
        static bool Prefix(Player __instance, Recipe piece, bool discover, int qualityLevel, ref bool __result)
        {
            if (!Plugin.Ativo() || discover || qualityLevel > 1)
                return true;
            if (__instance != Player.m_localPlayer)
                return true;
            if (!Plugin.ComidaConhecida(__instance, piece))
                return true;
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "DoCrafting")]
    public static class CraftDaComida
    {
        static void Prefix(InventoryGui __instance, Player player)
        {
            Plugin.pularCusto = false;
            if (!Plugin.Ativo() || player != Player.m_localPlayer)
                return;
            if (Plugin.CampoUpgrade == null || Plugin.CampoReceita == null)
                return;
            if (Plugin.CampoUpgrade.GetValue(__instance) != null)
                return;
            Recipe receita = Plugin.CampoReceita.GetValue(__instance) as Recipe;
            if (!Plugin.ComidaConhecida(player, receita))
                return;
            Plugin.pularCusto = true;
            Plugin.Inst.Avisar(receita.m_item.m_itemData.m_shared.m_name);
        }

        static void Postfix()
        {
            Plugin.pularCusto = false;
        }
    }

    [HarmonyPatch(typeof(Player), "ConsumeResources")]
    public static class NaoGastaComida
    {
        static bool Prefix()
        {
            return !Plugin.pularCusto;
        }
    }
}
