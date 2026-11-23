using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace NadoFundo
{
    [BepInPlugin("yuri.QuickSwim", "Quick Swim", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> Ligado;
        public static ConfigEntry<bool> Mergulho;
        public static ConfigEntry<float> VelocidadeMin;
        public static ConfigEntry<float> VelocidadeMax;
        public static ConfigEntry<float> GastoMin;
        public static ConfigEntry<float> GastoMax;
        public static ConfigEntry<float> Regen;
        public static ConfigEntry<float> Exp;
        public static ConfigEntry<float> Dano;
        public static ConfigEntry<float> Nevoa;
        public static ConfigEntry<float> Brilho;

        public static float FatorNado(Player jogador)
        {
            float nivel = jogador.GetSkillLevel(Skills.SkillType.Swim);
            if (nivel < 0f)
                nivel = 0f;
            if (nivel > 100f)
                nivel = 100f;
            return nivel / 100f;
        }

        private void Awake()
        {
            Ligado = Config.Bind("General", "Enabled", true, "Turns the mod on in this game. Only your own game needs this mod.");
            Mergulho = Config.Bind("Dive", "Enabled", true, "Hold Ctrl in the water and look with the mouse to dive or rise.");
            VelocidadeMin = Config.Bind("Swim", "SpeedMin", 2f, "Speed with the Swim skill at zero.");
            VelocidadeMax = Config.Bind("Swim", "SpeedMax", 7f, "Speed with the Swim skill at 100. It rises with the level.");
            GastoMin = Config.Bind("Stamina", "DrainAtStart", 14f, "Stamina spent swimming with little skill.");
            GastoMax = Config.Bind("Stamina", "DrainAtMax", 4f, "Stamina spent swimming with the skill at maximum.");
            Regen = Config.Bind("Stamina", "RegenWhileStill", 8f, "Stamina per second while still in the water, at maximum Swim.");
            Exp = Config.Bind("Swim", "Exp", 1f, "Multiplies Swim skill experience. 1 is the game default.");
            Dano = Config.Bind("Dive", "DamageWithoutStamina", 10f, "Damage per second underwater with no stamina.");
            Nevoa = Config.Bind("Water", "Fog", 0.08f, "Fog underwater. 0 turns it off.");
            Brilho = Config.Bind("Water", "Brightness", 0.45f, "How clear the water is. 0 is dark, 1 is bright.");
            new Harmony("yuri.QuickSwim").PatchAll();
        }
    }

    [HarmonyPatch(typeof(Player), "RaiseSkill")]
    public static class ExpPatch
    {
        private static void Prefix(Player __instance, Skills.SkillType skill, ref float value)
        {
            if (!Plugin.Ligado.Value || __instance != Player.m_localPlayer)
                return;
            if (skill != Skills.SkillType.Swim)
                return;
            value *= Plugin.Exp.Value;
        }
    }

    [HarmonyPatch(typeof(Character), "UpdateSwimming")]
    public static class VelocidadePatch
    {
        private static void Prefix(Character __instance)
        {
            Player jogador = __instance as Player;
            if (jogador == null || jogador != Player.m_localPlayer || !Plugin.Ligado.Value)
                return;
            float fator = Plugin.FatorNado(jogador);
            jogador.m_swimSpeed = Mathf.Lerp(Plugin.VelocidadeMin.Value, Plugin.VelocidadeMax.Value, fator);
            jogador.m_swimStaminaDrainMinSkill = Plugin.GastoMin.Value;
            jogador.m_swimStaminaDrainMaxSkill = Plugin.GastoMax.Value;
        }

        private static void Postfix(Character __instance, Rigidbody ___m_body)
        {
            Player jogador = __instance as Player;
            if (jogador == null || jogador != Player.m_localPlayer || !Plugin.Ligado.Value)
                return;
            if (!Plugin.Mergulho.Value || MergulhoPatch.Profundidade < 0.05f || ___m_body == null)
                return;
            Vector3 vel = ___m_body.linearVelocity;
            vel.y = 0f;
            ___m_body.linearVelocity = vel;
        }
    }

    [HarmonyPatch(typeof(Player), "LateUpdate")]
    public static class MergulhoPatch
    {
        public static float Profundidade;
        private static float superficie;
        private static bool nevoaAtiva;
        private static bool fogLigado;
        private static int modo;
        private static float densidade;
        private static Color cor;

        private static void Postfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer)
                return;
            if (!Plugin.Ligado.Value || !__instance.IsSwimming())
            {
                Profundidade = 0f;
                RestaurarNevoa();
                return;
            }

            float fator = Plugin.FatorNado(__instance);
            float gasto = Mathf.Lerp(Plugin.GastoMin.Value, Plugin.GastoMax.Value, fator);
            bool ctrl = Tecla(KeyCode.LeftControl) || Tecla(KeyCode.RightControl);
            bool pulo = Tecla(KeyCode.Space);
            float antes = Profundidade;

            if (Plugin.Mergulho.Value && ctrl)
            {
                Vector3 olhar = __instance.GetLookDir();
                Profundidade -= olhar.y * __instance.m_swimSpeed * Time.deltaTime;
            }

            if (pulo)
                Profundidade = Mathf.MoveTowards(Profundidade, 0f, __instance.m_swimSpeed * 3f * Time.deltaTime);

            if (Profundidade < 0f)
                Profundidade = 0f;
            if (Profundidade > 60f)
                Profundidade = 60f;

            if (antes <= 0.05f)
                superficie = __instance.transform.position.y;

            if (Profundidade > 0.05f)
            {
                Vector3 pos = __instance.transform.position;
                float linha = superficie - Profundidade;
                float chao = Chao(pos, superficie);
                if (linha < chao)
                {
                    linha = chao;
                    Profundidade = superficie - linha;
                    if (Profundidade < 0f)
                        Profundidade = 0f;
                }
                pos.y = linha;
                __instance.transform.position = pos;
            }

            bool embaixo = Profundidade > 0.6f;
            if (embaixo)
            {
                __instance.UseStamina(gasto * Time.deltaTime);
                if (__instance.GetStamina() <= 0.5f && Plugin.Dano.Value > 0f)
                {
                    float vida = __instance.GetHealth() - Plugin.Dano.Value * Time.deltaTime;
                    if (vida < 0f)
                        vida = 0f;
                    __instance.SetHealth(vida);
                }
                AplicarNevoa();
            }
            else
            {
                RestaurarNevoa();
                if (__instance.GetVelocity().magnitude < 0.35f && Plugin.Regen.Value > 0f)
                    __instance.AddStamina(Plugin.Regen.Value * fator * Time.deltaTime);
            }
        }

        private static float Chao(Vector3 pos, float agua)
        {
            RaycastHit hit;
            Vector3 origem = new Vector3(pos.x, agua - 0.3f, pos.z);
            if (Physics.Raycast(origem, Vector3.down, out hit, 120f))
                return hit.point.y + 0.7f;
            return agua - 60f;
        }

        private static bool Tecla(KeyCode tecla)
        {
            System.Type tipo = System.Type.GetType("UnityEngine.Input, UnityEngine.InputLegacyModule");
            if (tipo == null)
                return false;
            MethodInfo metodo = tipo.GetMethod("GetKey", new System.Type[] { typeof(KeyCode) });
            if (metodo == null)
                return false;
            return (bool)metodo.Invoke(null, new object[] { tecla });
        }

        private static void AplicarNevoa()
        {
            if (Plugin.Nevoa.Value <= 0f)
                return;
            if (!nevoaAtiva)
            {
                fogLigado = LerBool("fog");
                modo = LerInt("fogMode");
                densidade = LerFloat("fogDensity");
                cor = LerCor("fogColor");
                nevoaAtiva = true;
            }
            float luz = Plugin.Brilho.Value;
            if (luz < 0.05f)
                luz = 0.05f;
            Escrever("fog", true);
            Escrever("fogMode", 2);
            Escrever("fogDensity", Plugin.Nevoa.Value);
            Escrever("fogColor", new Color(0.04f * luz, 0.16f * luz, 0.28f * luz, 1f));
        }

        private static void RestaurarNevoa()
        {
            if (!nevoaAtiva)
                return;
            Escrever("fog", fogLigado);
            Escrever("fogMode", modo);
            Escrever("fogDensity", densidade);
            Escrever("fogColor", cor);
            nevoaAtiva = false;
        }

        private static System.Type TipoCena()
        {
            return System.Type.GetType("UnityEngine.RenderSettings, UnityEngine.CoreModule");
        }

        private static void Escrever(string nome, object valor)
        {
            System.Type tipo = TipoCena();
            if (tipo == null)
                return;
            PropertyInfo propriedade = tipo.GetProperty(nome);
            if (propriedade == null)
                return;
            if (nome == "fogMode")
                valor = System.Enum.ToObject(propriedade.PropertyType, valor);
            propriedade.SetValue(null, valor, null);
        }

        private static bool LerBool(string nome)
        {
            object valor = Ler(nome);
            return valor is bool && (bool)valor;
        }

        private static int LerInt(string nome)
        {
            object valor = Ler(nome);
            if (valor == null)
                return 2;
            return (int)System.Convert.ToInt32(valor);
        }

        private static float LerFloat(string nome)
        {
            object valor = Ler(nome);
            if (valor == null)
                return 0f;
            return (float)System.Convert.ToSingle(valor);
        }

        private static Color LerCor(string nome)
        {
            object valor = Ler(nome);
            if (valor is Color)
                return (Color)valor;
            return Color.gray;
        }

        private static object Ler(string nome)
        {
            System.Type tipo = TipoCena();
            if (tipo == null)
                return null;
            PropertyInfo propriedade = tipo.GetProperty(nome);
            if (propriedade == null)
                return null;
            return propriedade.GetValue(null, null);
        }
    }
}
