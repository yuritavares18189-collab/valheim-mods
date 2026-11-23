using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace FreePotions
{
    [BepInPlugin("yuri.FreePotions", "Free Potions", "1.0.2")]
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
            Ligado = Config.Bind("General", "Enabled", true, "Known mead bases and barley wine bases craft without using ingredients. Only your own game needs this mod.");
            CampoReceita = typeof(InventoryGui).GetField("m_craftRecipe", BindingFlags.Instance | BindingFlags.NonPublic);
            CampoUpgrade = typeof(InventoryGui).GetField("m_craftUpgradeItem", BindingFlags.Instance | BindingFlags.NonPublic);
            Harmony harmonia = new Harmony("yuri.FreePotions");
            harmonia.PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo("Free Potions 1.0.2");
        }

        public static bool Ativo()
        {
            return Inst != null && Inst.Ligado.Value;
        }

        public void Avisar(string nome)
        {
            Logger.LogInfo("Potion base without ingredients: " + nome);
        }

        public static bool BaseConhecida(Player jogador, Recipe receita)
        {
            if (jogador == null || receita == null || receita.m_item == null)
                return false;
            if (receita.m_item.m_itemData == null || receita.m_item.m_itemData.m_shared == null)
                return false;
            string token = receita.m_item.m_itemData.m_shared.m_name;
            string prefab = receita.m_item.gameObject != null ? receita.m_item.gameObject.name : receita.m_item.name;
            if (!EhBase(prefab) && !EhBase(token) && !NoCaldeirao(receita))
                return false;
            if (jogador.IsRecipeKnown(token))
                return true;
            return MateriaisConhecidos(jogador, receita);
        }

        static bool EhBase(string nome)
        {
            if (string.IsNullOrEmpty(nome))
                return false;
            return nome.IndexOf("meadbase", System.StringComparison.OrdinalIgnoreCase) >= 0
                || nome.IndexOf("barleywine", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static bool NoCaldeirao(Recipe receita)
        {
            if (receita.m_craftingStation == null)
                return false;
            string peca = "";
            if (receita.m_craftingStation.gameObject != null)
                peca = receita.m_craftingStation.gameObject.name;
            if (peca != null && peca.IndexOf("meadcauldron", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            string nome = receita.m_craftingStation.m_name;
            return nome != null && nome.IndexOf("meadcauldron", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static bool MateriaisConhecidos(Player jogador, Recipe receita)
        {
            if (receita.m_resources == null || receita.m_resources.Length == 0)
                return true;
            bool algum = false;
            for (int i = 0; i < receita.m_resources.Length; i++)
            {
                Piece.Requirement req = receita.m_resources[i];
                if (req == null || req.m_resItem == null || req.m_resItem.m_itemData == null || req.m_resItem.m_itemData.m_shared == null)
                    continue;
                algum = true;
                bool sabe = jogador.IsKnownMaterial(req.m_resItem.m_itemData.m_shared.m_name);
                if (receita.m_requireOnlyOneIngredient)
                {
                    if (sabe)
                        return true;
                }
                else if (!sabe)
                    return false;
            }
            return algum && !receita.m_requireOnlyOneIngredient;
        }
    }

    [HarmonyPatch(typeof(Player), "HaveRequirementItems")]
    public static class ItensDaBase
    {
        static bool Prefix(Player __instance, Recipe piece, bool discover, int qualityLevel, ref bool __result)
        {
            if (!Plugin.Ativo() || qualityLevel > 1)
                return true;
            if (__instance != Player.m_localPlayer)
                return true;
            if (!Plugin.BaseConhecida(__instance, piece))
                return true;
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "DoCrafting")]
    public static class CraftDaBase
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
            if (!Plugin.BaseConhecida(player, receita))
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
    public static class NaoGastaBase
    {
        static bool Prefix()
        {
            return !Plugin.pularCusto;
        }
    }
}
