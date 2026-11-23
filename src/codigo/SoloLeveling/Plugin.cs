using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace SoloLeveling
{
    [BepInPlugin("yuri.SoloLeveling", "Solo Leveling", "1.0.1")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Inst;
        public static bool Espalhando;
        public static bool Aplicar;
        public static float Quanto;

        ConfigEntry<bool> Ligado;
        ConfigEntry<int> Vezes;
        bool guardouCap;
        bool capOriginal;

        void Awake()
        {
            Inst = this;
            Ligado = Config.Bind("General", "Enabled", true, "Every action raises every skill at once, with multiplied experience. No level cap. Only your own game needs this mod.");
            Vezes = Config.Bind("General", "Times", 2, "How many times the experience comes in. The default is 2.");
            Harmony harmonia = new Harmony("yuri.SoloLeveling");
            harmonia.PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo("Solo Leveling 1.0.1");
        }

        void Update()
        {
            Player jogador = Player.m_localPlayer;
            if (jogador == null)
                return;
            Skills habilidades = jogador.GetSkills();
            if (habilidades == null)
                return;
            if (!guardouCap)
            {
                capOriginal = habilidades.m_useSkillCap;
                guardouCap = true;
            }
            if (Ligado.Value)
                habilidades.m_useSkillCap = false;
            else
                habilidades.m_useSkillCap = capOriginal;
        }

        public bool Ativo()
        {
            return Ligado.Value;
        }

        public float Multiplicador()
        {
            int n = Vezes.Value;
            if (n < 1)
                n = 1;
            if (n > 5)
                n = 5;
            return n;
        }

        public static bool EhMeu(Skills habilidades)
        {
            Player jogador = Player.m_localPlayer;
            if (jogador == null || habilidades == null)
                return false;
            return jogador.GetSkills() == habilidades;
        }

        [HarmonyPatch(typeof(Skills), "RaiseSkill")]
        static class AoSubir
        {
            static void Prefix(Skills __instance, ref float factor)
            {
                Aplicar = false;
                if (Espalhando || Inst == null || !Inst.Ativo() || !EhMeu(__instance))
                    return;
                __instance.m_useSkillCap = false;
                factor = factor * Inst.Multiplicador();
                Quanto = factor;
                Aplicar = true;
            }

            static void Postfix(Skills __instance, Skills.SkillType skillType)
            {
                if (!Aplicar || Espalhando)
                    return;
                Aplicar = false;
                Espalhando = true;
                try
                {
                    Array tipos = Enum.GetValues(typeof(Skills.SkillType));
                    for (int i = 0; i < tipos.Length; i++)
                    {
                        Skills.SkillType tipo = (Skills.SkillType)tipos.GetValue(i);
                        if (tipo == skillType || tipo == Skills.SkillType.None || tipo == Skills.SkillType.All)
                            continue;
                        Skills.Skill habilidade = Pegar(__instance, tipo);
                        if (habilidade == null || habilidade.m_info == null)
                            continue;
                        habilidade.Raise(Quanto);
                    }
                }
                finally
                {
                    Espalhando = false;
                }
            }
        }

        static MethodInfo pegarSkill;

        static Skills.Skill Pegar(Skills habilidades, Skills.SkillType tipo)
        {
            if (pegarSkill == null)
                pegarSkill = typeof(Skills).GetMethod("GetSkill", BindingFlags.Instance | BindingFlags.NonPublic);
            if (pegarSkill == null)
                return null;
            return (Skills.Skill)pegarSkill.Invoke(habilidades, new object[] { tipo });
        }

        [HarmonyPatch(typeof(Skills.Skill), "Raise")]
        static class SemTeto
        {
            static bool Prefix(Skills.Skill __instance, float factor, ref bool __result)
            {
                if (Inst == null || !Inst.Ativo() || __instance == null || __instance.m_info == null)
                    return true;
                float ganho = __instance.m_info.m_increseStep * factor * Game.m_skillGainRate;
                __instance.m_accumulator = __instance.m_accumulator + ganho;
                float falta = Proxima(__instance.m_level);
                if (__instance.m_accumulator < falta)
                {
                    __result = false;
                    return false;
                }
                __instance.m_level = __instance.m_level + 1f;
                if (__instance.m_level < 0f)
                    __instance.m_level = 0f;
                __instance.m_accumulator = 0f;
                __result = true;
                return false;
            }

            static float Proxima(float nivel)
            {
                float n = Mathf.Floor(nivel + 1f);
                return Mathf.Pow(n, 1.5f) * 0.5f + 0.5f;
            }
        }

        [HarmonyPatch(typeof(Skills), "GetSkillFactor")]
        static class SemTrava
        {
            static bool Prefix(Skills __instance, Skills.SkillType skillType, ref float __result)
            {
                if (Inst == null || !Inst.Ativo() || !EhMeu(__instance))
                    return true;
                if (skillType == Skills.SkillType.None)
                {
                    __result = 0f;
                    return false;
                }
                float nivel = __instance.GetSkillLevel(skillType);
                __result = nivel / 100f;
                if (__result < 0f)
                    __result = 0f;
                return false;
            }
        }
    }
}
