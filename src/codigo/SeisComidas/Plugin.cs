using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeisComidas
{
    [BepInPlugin("yuri.SeisComidas", "Seis Comidas", "1.0.1")]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> Ligado;
        public static ConfigEntry<int> Comidas;

        private void Awake()
        {
            Ligado = Config.Bind("Geral", "Ligado", true, "Ate seis comidas diferentes ao mesmo tempo. A mesma comida continua uma vez. E de cliente. So no seu jogo.");
            Comidas = Config.Bind("Geral", "Comidas", 6, new ConfigDescription("Quantas comidas diferentes ao mesmo tempo.", new AcceptableValueRange<int>(3, 8)));
            new Harmony("yuri.SeisComidas").PatchAll();
            Logger.LogInfo("Seis Comidas 1.0.1");
        }

        public static int Limite()
        {
            if (Ligado == null || !Ligado.Value)
                return 3;
            int n = Comidas.Value;
            if (n < 3)
                return 3;
            if (n > 8)
                return 8;
            return n;
        }
    }

    public static class TrocaTres
    {
        public static IEnumerable<CodeInstruction> Troca(IEnumerable<CodeInstruction> instructions)
        {
            var metodo = AccessTools.Method(typeof(Plugin), "Limite");
            foreach (CodeInstruction ins in instructions)
            {
                if (ins.opcode != OpCodes.Ldc_I4_3)
                {
                    yield return ins;
                    continue;
                }
                CodeInstruction novo = new CodeInstruction(OpCodes.Call, metodo);
                novo.labels.AddRange(ins.labels);
                novo.blocks.AddRange(ins.blocks);
                yield return novo;
            }
        }
    }

    [HarmonyPatch(typeof(Player), "CanEat")]
    public static class PodeComer
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return TrocaTres.Troca(instructions);
        }
    }

    [HarmonyPatch(typeof(Player), "EatFood")]
    public static class Come
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return TrocaTres.Troca(instructions);
        }
    }

    [HarmonyPatch(typeof(Hud), "UpdateFood")]
    public static class Mostra
    {
        private static int hudId;
        private static bool parou;

        private static void Prefix(Hud __instance)
        {
            if (!Plugin.Ligado.Value)
                return;
            int id = __instance.GetInstanceID();
            if (id != hudId)
            {
                hudId = id;
                parou = false;
            }
            if (parou)
                return;
            int quer = Plugin.Limite();
            Image[] icones = __instance.m_foodIcons;
            Image[] barras = __instance.m_foodBars;
            TMP_Text[] tempos = __instance.m_foodTime;
            if (icones == null || barras == null || tempos == null)
                return;
            if (icones.Length >= quer)
                return;
            if (icones.Length < 2 || barras.Length < 2 || tempos.Length < 2)
                return;
            if (icones.Length != barras.Length || icones.Length != tempos.Length)
                return;
            Crescer(__instance, quer, icones, barras, tempos);
        }

        private static void Crescer(Hud hud, int quer, Image[] icones, Image[] barras, TMP_Text[] tempos)
        {
            List<Image> novosIcones = new List<Image>(icones);
            List<Image> novasBarras = new List<Image>(barras);
            List<TMP_Text> novosTempos = new List<TMP_Text>(tempos);
            int n = icones.Length;
            Transform raizA = Raiz(icones[n - 2].transform, barras[n - 2].transform, tempos[n - 2].transform, icones[n - 1].transform);
            Transform raizB = Raiz(icones[n - 1].transform, barras[n - 1].transform, tempos[n - 1].transform, icones[n - 2].transform);
            if (raizA != null && raizB != null && raizA != raizB)
                CrescerJunto(quer, novosIcones, novasBarras, novosTempos, icones[n - 1], barras[n - 1], tempos[n - 1], raizA, raizB);
            else
                CrescerSolto(quer, novosIcones, novasBarras, novosTempos, icones, barras, tempos);
            hud.m_foodIcons = novosIcones.ToArray();
            hud.m_foodBars = novasBarras.ToArray();
            hud.m_foodTime = novosTempos.ToArray();
        }

        private static void CrescerJunto(int quer, List<Image> novosIcones, List<Image> novasBarras, List<TMP_Text> novosTempos, Image iconeBase, Image barraBase, TMP_Text tempoBase, Transform raizA, Transform raizB)
        {
            string caminhoIcone = Caminho(raizB, iconeBase.transform);
            string caminhoBarra = Caminho(raizB, barraBase.transform);
            string caminhoTempo = Caminho(raizB, tempoBase.transform);
            Vector3 passo = raizB.localPosition - raizA.localPosition;
            if (passo.sqrMagnitude < 0.01f)
                passo = new Vector3(0f, -36f, 0f);
            int tenho = novosIcones.Count;
            for (int i = tenho; i < quer; i++)
            {
                GameObject clone = Object.Instantiate(raizB.gameObject, raizB.parent);
                clone.name = "seiscomida_" + i;
                clone.transform.localPosition = raizB.localPosition + passo * (i - (tenho - 1));
                clone.transform.localRotation = raizB.localRotation;
                clone.transform.localScale = raizB.localScale;
                Image icone = Imagem(clone.transform, caminhoIcone);
                Image barra = Imagem(clone.transform, caminhoBarra);
                TMP_Text tempo = Texto(clone.transform, caminhoTempo);
                if (icone == null || barra == null || tempo == null)
                {
                    Object.Destroy(clone);
                    parou = true;
                    return;
                }
                novosIcones.Add(icone);
                novasBarras.Add(barra);
                novosTempos.Add(tempo);
            }
        }

        private static void CrescerSolto(int quer, List<Image> novosIcones, List<Image> novasBarras, List<TMP_Text> novosTempos, Image[] icones, Image[] barras, TMP_Text[] tempos)
        {
            int ultimo = icones.Length - 1;
            Vector3 passoI = icones[ultimo].rectTransform.localPosition - icones[ultimo - 1].rectTransform.localPosition;
            Vector3 passoB = barras[ultimo].rectTransform.localPosition - barras[ultimo - 1].rectTransform.localPosition;
            Vector3 passoT = tempos[ultimo].rectTransform.localPosition - tempos[ultimo - 1].rectTransform.localPosition;
            if (passoI.sqrMagnitude < 0.01f)
                passoI = new Vector3(0f, -36f, 0f);
            if (passoB.sqrMagnitude < 0.01f)
                passoB = passoI;
            if (passoT.sqrMagnitude < 0.01f)
                passoT = passoI;
            int tenho = novosIcones.Count;
            for (int i = tenho; i < quer; i++)
            {
                int k = i - ultimo;
                Image icone = Object.Instantiate(icones[ultimo], icones[ultimo].transform.parent);
                Image barra = Object.Instantiate(barras[ultimo], barras[ultimo].transform.parent);
                TMP_Text tempo = Object.Instantiate(tempos[ultimo], tempos[ultimo].transform.parent);
                icone.name = "seisicone_" + i;
                barra.name = "seisbarra_" + i;
                tempo.name = "seistempo_" + i;
                icone.rectTransform.localPosition = icones[ultimo].rectTransform.localPosition + passoI * k;
                barra.rectTransform.localPosition = barras[ultimo].rectTransform.localPosition + passoB * k;
                tempo.rectTransform.localPosition = tempos[ultimo].rectTransform.localPosition + passoT * k;
                novosIcones.Add(icone);
                novasBarras.Add(barra);
                novosTempos.Add(tempo);
            }
        }

        private static Transform Raiz(Transform icone, Transform barra, Transform tempo, Transform outro)
        {
            Transform t = icone;
            while (t != null)
            {
                if (Debaixo(barra, t) && Debaixo(tempo, t) && !Debaixo(outro, t))
                    return t;
                t = t.parent;
            }
            return null;
        }

        private static bool Debaixo(Transform filho, Transform pai)
        {
            while (filho != null)
            {
                if (filho == pai)
                    return true;
                filho = filho.parent;
            }
            return false;
        }

        private static string Caminho(Transform raiz, Transform alvo)
        {
            if (alvo == raiz)
                return "";
            string s = alvo.name;
            Transform t = alvo.parent;
            while (t != null && t != raiz)
            {
                s = t.name + "/" + s;
                t = t.parent;
            }
            return s;
        }

        private static Transform Achar(Transform raiz, string caminho)
        {
            if (caminho == null || caminho.Length == 0)
                return raiz;
            return raiz.Find(caminho);
        }

        private static Image Imagem(Transform raiz, string caminho)
        {
            Transform t = Achar(raiz, caminho);
            if (t == null)
                return null;
            return t.GetComponent<Image>();
        }

        private static TMP_Text Texto(Transform raiz, string caminho)
        {
            Transform t = Achar(raiz, caminho);
            if (t == null)
                return null;
            return t.GetComponent<TMP_Text>();
        }
    }
}
