using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace FundidorCheio
{
    [BepInPlugin("yuri.FullSmelter", "Full Smelter", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Inst;

        ConfigEntry<bool> Ligado;
        ConfigEntry<int> Vagas;
        float proximo;
        FieldInfo campoVisao;
        MethodInfo metodoCombustivel;

        void Awake()
        {
            Inst = this;
            Ligado = Config.Bind("General", "Enabled", true, "The smelter and the blast furnace use no coal and hold up to 100 ore. Only your own game needs this mod.");
            Vagas = Config.Bind("General", "Slots", 100, "How much ore fits. From 10 to 100.");
            campoVisao = typeof(Smelter).GetField("m_nview", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            metodoCombustivel = typeof(Smelter).GetMethod("GetFuel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Harmony harmonia = new Harmony("yuri.FullSmelter");
            harmonia.PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo("Full Smelter 1.0.2");
        }

        public int Capacidade()
        {
            int n = Vagas.Value;
            if (n < 10)
                n = 10;
            if (n > 100)
                n = 100;
            return n;
        }

        void Update()
        {
            if (!Ligado.Value || Player.m_localPlayer == null)
                return;
            if (Time.time < proximo)
                return;
            proximo = Time.time + 1f;
            Smelter[] lista = UnityEngine.Object.FindObjectsOfType<Smelter>();
            if (lista == null)
                return;
            for (int i = 0; i < lista.Length; i++)
                Manter(lista[i]);
        }

        public void AbrirVaga(Smelter forno)
        {
            if (forno == null || !EhMetal(forno))
                return;
            int vagas = Capacidade();
            if (forno.m_maxOre < vagas)
                forno.m_maxOre = vagas;
        }

        void Manter(Smelter forno)
        {
            AbrirVaga(forno);
            if (forno == null || !EhMetal(forno) || campoVisao == null || metodoCombustivel == null)
                return;
            ZNetView visao = campoVisao.GetValue(forno) as ZNetView;
            if (visao == null || !visao.IsValid())
                return;
            object lido = metodoCombustivel.Invoke(forno, null);
            float combustivel = 0f;
            if (lido is float)
                combustivel = (float)lido;
            int falta = 40 - (int)combustivel;
            if (falta < 1)
                return;
            if (falta > 8)
                falta = 8;
            for (int i = 0; i < falta; i++)
                visao.InvokeRPC("RPC_AddFuel");
        }

        static bool EhMetal(Smelter forno)
        {
            if (forno == null || forno.m_conversion == null)
                return false;
            for (int i = 0; i < forno.m_conversion.Count; i++)
            {
                Smelter.ItemConversion receita = forno.m_conversion[i];
                if (receita == null || receita.m_to == null || receita.m_to.gameObject == null)
                    continue;
                if (EhBarra(receita.m_to.gameObject.name))
                    return true;
            }
            return false;
        }

        static bool EhBarra(string nome)
        {
            return nome == "Copper" || nome == "Tin" || nome == "Bronze" || nome == "Iron" || nome == "Silver" || nome == "BlackMetal" || nome == "Flametal";
        }

        [HarmonyPatch(typeof(Smelter), "OnAddOre")]
        static class AoPorMinerio
        {
            static void Prefix(Smelter __instance)
            {
                if (Inst == null || !Inst.Ligado.Value)
                    return;
                Inst.AbrirVaga(__instance);
            }
        }
    }
}
