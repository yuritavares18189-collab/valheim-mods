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
    [BepInPlugin("yuri.Fartura", "Fartura", "1.0.1")]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> Ligado;
        public static ConfigEntry<int> Comidas;

        private void Awake()
        {
            Ligado = Config.Bind("Geral", "Ligado", true, "Ate seis comidas diferentes ao mesmo tempo. A mesma comida continua uma vez. So no seu jogo.");
            Comidas = Config.Bind("Geral", "Comidas", 6, new ConfigDescription("Quantas comidas diferentes ao mesmo tempo.", new AcceptableValueRange<int>(3, 8)));
            new Harmony("yuri.Fartura").PatchAll();
            Logger.LogInfo("Fartura 1.0.1");
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
        private static bool guardou;
        private static bool temRaiz;
        private static Vector2[] baseIcone;
        private static Vector2[] baseBarra;
        private static Vector2[] baseTempo;
        private static Vector2[] baseRaiz;
        private static Vector2 passoI;
        private static Vector2 passoB;
        private static Vector2 passoT;
        private static Vector2 passoRaiz;

        private static void Prefix(Hud __instance)
        {
            if (!Plugin.Ligado.Value)
                return;
            int id = __instance.GetInstanceID();
            if (id != hudId)
            {
                hudId = id;
                parou = false;
                guardou = false;
            }
            int quer = Plugin.Limite();
            Image[] icones = __instance.m_foodIcons;
            Image[] barras = __instance.m_foodBars;
            TMP_Text[] tempos = __instance.m_foodTime;
            if (icones == null || barras == null || tempos == null)
                return;
            if (icones.Length < 2 || barras.Length < 2 || tempos.Length < 2)
                return;
            if (icones.Length != barras.Length || icones.Length != tempos.Length)
                return;
            Guardar(icones, barras, tempos);
            if (!parou && icones.Length < quer)
                Crescer(__instance, quer, icones, barras, tempos);
            Posicionar(__instance.m_foodIcons, __instance.m_foodBars, __instance.m_foodTime);
        }

        private static void Guardar(Image[] icones, Image[] barras, TMP_Text[] tempos)
        {
            if (guardou)
                return;
            int n = icones.Length;
            baseIcone = new Vector2[n];
            baseBarra = new Vector2[n];
            baseTempo = new Vector2[n];
            baseRaiz = new Vector2[n];
            temRaiz = true;
            for (int i = 0; i < n; i++)
            {
                baseIcone[i] = icones[i].rectTransform.anchoredPosition;
                baseBarra[i] = barras[i].rectTransform.anchoredPosition;
                baseTempo[i] = tempos[i].rectTransform.anchoredPosition;
                Transform outro = icones[i == 0 ? 1 : 0].transform;
                Transform raiz = Raiz(icones[i].transform, barras[i].transform, tempos[i].transform, outro);
                RectTransform rt = raiz == null ? null : raiz.GetComponent<RectTransform>();
                if (rt == null)
                {
                    temRaiz = false;
                    continue;
                }
                baseRaiz[i] = rt.anchoredPosition;
            }
            float altoIcone = icones[0].rectTransform.rect.height;
            float altoBarra = barras[0].rectTransform.rect.height;
            float altoTempo = tempos[0].rectTransform.rect.height;
            passoI = Passo(baseIcone[0], baseIcone[1], altoIcone);
            passoB = Passo(baseBarra[0], baseBarra[1], altoBarra);
            passoT = Passo(baseTempo[0], baseTempo[1], altoTempo);
            if (temRaiz)
            {
                float alto = altoIcone;
                if (alto < 16f)
                    alto = 64f;
                passoRaiz = Passo(baseRaiz[0], baseRaiz[1], alto);
            }
            guardou = true;
        }

        private static Vector2 Passo(Vector2 a, Vector2 b, float alto)
        {
            Vector2 d = b - a;
            if (alto < 16f)
                alto = 64f;
            float minimo = alto + 8f;
            if (d.magnitude >= minimo * 0.85f)
                return d;
            if (d.y > 0.5f)
                return new Vector2(0f, minimo);
            if (d.y < -0.5f)
                return new Vector2(0f, -minimo);
            if (d.x > 0.5f)
                return new Vector2(minimo, 0f);
            if (d.x < -0.5f)
                return new Vector2(-minimo, 0f);
            return new Vector2(0f, -minimo);
        }

        private static Vector2 Onde(int i, Vector2[] bases, Vector2 passo)
        {
            if (i < bases.Length)
                return bases[i];
            int ult = bases.Length - 1;
            return bases[ult] + passo * (i - ult);
        }

        private static void Soltar(Transform t)
        {
            if (t == null)
                return;
            LayoutGroup grupo = t.GetComponent<LayoutGroup>();
            if (grupo != null)
                grupo.enabled = false;
            ContentSizeFitter fitter = t.GetComponent<ContentSizeFitter>();
            if (fitter != null)
                fitter.enabled = false;
        }

        private static void Posicionar(Image[] icones, Image[] barras, TMP_Text[] tempos)
        {
            if (!guardou || icones == null || barras == null || tempos == null)
                return;
            if (icones.Length == 0 || icones.Length != barras.Length || icones.Length != tempos.Length)
                return;
            Soltar(icones[0].transform.parent);
            Soltar(barras[0].transform.parent);
            Soltar(tempos[0].transform.parent);
            for (int i = 0; i < icones.Length; i++)
            {
                Transform outro = icones[i == 0 ? Mathf.Min(1, icones.Length - 1) : 0].transform;
                Transform raiz = Raiz(icones[i].transform, barras[i].transform, tempos[i].transform, outro);
                RectTransform rt = raiz == null ? null : raiz.GetComponent<RectTransform>();
                if (temRaiz && rt != null)
                {
                    rt.anchoredPosition = Onde(i, baseRaiz, passoRaiz);
                    continue;
                }
                icones[i].rectTransform.anchoredPosition = Onde(i, baseIcone, passoI);
                barras[i].rectTransform.anchoredPosition = Onde(i, baseBarra, passoB);
                tempos[i].rectTransform.anchoredPosition = Onde(i, baseTempo, passoT);
            }
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
