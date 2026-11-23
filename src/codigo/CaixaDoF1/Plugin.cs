using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace CaixaDoF1
{
    [BepInPlugin("yuri.CaixaDoF1", "Caixa do F1", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<int> Largura;
        public static ConfigEntry<int> Altura;

        private void Awake()
        {
            Largura = Config.Bind("Geral", "Largura", 900, new ConfigDescription("Largura da janela do F1, no eixo X.", new AcceptableValueRange<int>(480, 1600)));
            Altura = Config.Bind("Geral", "Altura", 900, new ConfigDescription("Altura da janela do F1, no eixo Y.", new AcceptableValueRange<int>(360, 1400)));

            System.Type tipo = AccessTools.TypeByName("ConfigurationManager.ConfigurationManager");
            if (tipo == null)
                return;
            Harmony harmonia = new Harmony("yuri.CaixaDoF1");
            harmonia.Patch(AccessTools.Method(tipo, "CalculateWindowRect"), postfix: new HarmonyMethod(typeof(Ajuste), "DepoisDoCalculo"));
            harmonia.Patch(AccessTools.Method(tipo, "OnGUI"), postfix: new HarmonyMethod(typeof(Ajuste), "DepoisDoDesenho"));
            harmonia.Patch(AccessTools.Method(tipo, "SettingsWindow"), prefix: new HarmonyMethod(typeof(Ajuste), "AntesDaJanela"));
        }
    }

    public static class Ajuste
    {
        public static void DepoisDoCalculo(object __instance)
        {
            Aplicar(__instance, true);
        }

        public static void DepoisDoDesenho(object __instance)
        {
            if (!Aberto(__instance))
                return;
            Aplicar(__instance, false);
        }

        public static void AntesDaJanela()
        {
            if (Plugin.Largura == null || Plugin.Altura == null)
                return;

            GUILayout.Label("Tamanho desta janela");
            int largura = Barra("Largura", Plugin.Largura.Value, 480, 1600);
            int altura = Barra("Altura", Plugin.Altura.Value, 360, 1400);
            if (largura != Plugin.Largura.Value)
                Plugin.Largura.Value = largura;
            if (altura != Plugin.Altura.Value)
                Plugin.Altura.Value = altura;
        }

        private static int Barra(string nome, int atual, int minimo, int maximo)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(nome, GUILayout.Width(70f));
            int valor = Mathf.RoundToInt(GUILayout.HorizontalSlider(atual, minimo, maximo));
            GUILayout.Label(valor.ToString(), GUILayout.Width(48f));
            GUILayout.EndHorizontal();
            return valor;
        }

        private static bool Aberto(object alvo)
        {
            PropertyInfo prop = alvo.GetType().GetProperty("DisplayingWindow");
            if (prop == null)
                return false;
            return (bool)prop.GetValue(alvo, null);
        }

        private static void Aplicar(object alvo, bool recentrar)
        {
            if (Plugin.Largura == null || Plugin.Altura == null)
                return;
            PropertyInfo prop = alvo.GetType().GetProperty("SettingWindowRect");
            if (prop == null)
                return;

            float largura = Plugin.Largura.Value;
            float altura = Plugin.Altura.Value;
            float tetoL = Screen.width - 40f;
            float tetoA = Screen.height - 40f;
            if (tetoL < 480f)
                tetoL = 480f;
            if (tetoA < 360f)
                tetoA = 360f;
            if (largura < 480f)
                largura = 480f;
            if (altura < 360f)
                altura = 360f;
            if (largura > tetoL)
                largura = tetoL;
            if (altura > tetoA)
                altura = tetoA;

            Rect caixa = (Rect)prop.GetValue(alvo, null);
            if (!recentrar && Mathf.Abs(caixa.width - largura) < 0.5f && Mathf.Abs(caixa.height - altura) < 0.5f)
                return;

            if (recentrar)
            {
                caixa.x = (Screen.width - largura) / 2f;
                caixa.y = (Screen.height - altura) / 2f;
            }
            caixa.width = largura;
            caixa.height = altura;
            if (caixa.x < 0f)
                caixa.x = 0f;
            if (caixa.y < 0f)
                caixa.y = 0f;
            if (caixa.xMax > Screen.width)
                caixa.x = Screen.width - caixa.width;
            if (caixa.yMax > Screen.height)
                caixa.y = Screen.height - caixa.height;
            prop.SetValue(alvo, caixa, null);

            int esquerda = Mathf.RoundToInt(largura / 2.5f);
            int direita = Mathf.RoundToInt(largura) - esquerda - 115;
            if (direita < 80)
                direita = 80;
            Definir(alvo, "LeftColumnWidth", esquerda);
            Definir(alvo, "RightColumnWidth", direita);
        }

        private static void Definir(object alvo, string nome, int valor)
        {
            PropertyInfo prop = alvo.GetType().GetProperty(nome);
            if (prop != null)
                prop.SetValue(alvo, valor, null);
        }
    }
}
