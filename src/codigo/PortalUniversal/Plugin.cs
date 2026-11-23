using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Splatform;
using UnityEngine;

namespace PortalUniversal
{
    [BepInPlugin("yuri.PortalUniversal", "Portal Universal", "1.0.4")]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> Ligado;
        public static bool Escolhendo;
        public static ZDOID Origem;
        public static float Saida = 2f;
        public static float Liberar;
        public static float IgnorarDuplo;
        static readonly Dictionary<ZDOID, Minimap.PinData> marcas = new Dictionary<ZDOID, Minimap.PinData>();
        static float proxima;
        static bool avisou;
        static MethodInfo centro;
        static MethodInfo pontoTela;
        static MethodInfo pinoPerto;
        static PropertyInfo raioProp;

        void Awake()
        {
            Ligado = Config.Bind("Geral", "Ligado", true, "Entrar no portal abre o mapa para escolher o destino. Não muda o nome. Só no seu jogo.");
            centro = typeof(Minimap).GetMethod("CenterMap", BindingFlags.Instance | BindingFlags.NonPublic);
            pontoTela = typeof(Minimap).GetMethod("ScreenToWorldPoint", BindingFlags.Instance | BindingFlags.NonPublic);
            pinoPerto = typeof(Minimap).GetMethod("GetClosestPin", BindingFlags.Instance | BindingFlags.NonPublic);
            raioProp = typeof(Minimap).GetProperty("PinInteractRadius", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            new Harmony("yuri.PortalUniversal").PatchAll();
            Logger.LogInfo("Portal Universal 1.0.4");
        }

        void Update()
        {
            if (Ligado == null || !Ligado.Value)
            {
                Apagar();
                Escolhendo = false;
                return;
            }
            if (ZDOMan.instance == null || Minimap.instance == null)
                return;
            if (Time.time < proxima)
                return;
            proxima = Time.time + 1f;
            Sincronizar();
        }

        public static void Abrir(TeleportWorld portal, Player jogador)
        {
            Saida = portal != null ? portal.m_exitDistance : 2f;
            if (Saida < 0.5f)
                Saida = 2f;
            Origem = PortalPerto(jogador.transform.position);
            Sincronizar();
            int outros = 0;
            foreach (ZDOID id in marcas.Keys)
            {
                if (!id.Equals(Origem))
                    outros++;
            }
            if (outros == 0)
            {
                Avisar(jogador, "Não tem outro portal.");
                return;
            }
            Escolhendo = true;
            Minimap.instance.SetMapMode(Minimap.MapMode.Large);
            if (centro != null)
                centro.Invoke(Minimap.instance, new object[] { jogador.transform.position });
            Avisar(jogador, "Selecione o portal no mapa.");
        }

        public static bool Bloqueado(TeleportWorld portal, Player jogador)
        {
            if (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoPortals))
            {
                Avisar(jogador, "$msg_blocked");
                return true;
            }
            if (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoBossPortals))
            {
                bool chefe = false;
                if (RandEventSystem.instance != null)
                {
                    string evento = RandEventSystem.instance.GetBossEvent();
                    chefe = evento != null && evento.Length > 0;
                }
                if (!chefe)
                {
                    float forca = 0f;
                    chefe = ZoneSystem.instance.GetGlobalKey(GlobalKeys.activeBosses, out forca) && forca > 0f;
                }
                if (chefe)
                {
                    Avisar(jogador, "$msg_blockedbyboss");
                    return true;
                }
            }
            bool liberaItens = portal != null && portal.m_allowAllItems;
            if (!jogador.IsTeleportable(liberaItens))
            {
                Avisar(jogador, "$msg_noteleport");
                return true;
            }
            return false;
        }

        public static void Escolher()
        {
            Player jogador = Player.m_localPlayer;
            Minimap mapa = Minimap.instance;
            if (jogador == null || mapa == null || pontoTela == null || pinoPerto == null || ZDOMan.instance == null)
                return;
            object ponto = ZInput.pointerPosition;
            Vector3 mundo = (Vector3)pontoTela.Invoke(mapa, new object[] { ponto });
            float raio = 30f;
            if (raioProp != null)
                raio = (float)raioProp.GetValue(mapa, null);
            if (raio < 8f)
                raio = 8f;
            ZDOID id = MarcaPerto(mundo, raio);
            if (id.Equals(ZDOID.None))
            {
                Avisar(jogador, "Clique na marca do portal.");
                return;
            }
            List<ZDO> lista = ZDOMan.instance.GetPortalList();
            ZDO destino = null;
            if (lista != null)
            {
                for (int i = 0; i < lista.Count; i++)
                {
                    if (lista[i] != null && lista[i].m_uid.Equals(id))
                    {
                        destino = lista[i];
                        break;
                    }
                }
            }
            if (destino == null)
            {
                Avisar(jogador, "Esse portal sumiu.");
                Sincronizar();
                return;
            }
            if (id.Equals(Origem) || Vector3.Distance(destino.GetPosition(), jogador.transform.position) < 4f)
            {
                Avisar(jogador, "Você já está neste portal.");
                return;
            }
            Quaternion giro = destino.GetRotation();
            Vector3 pos = destino.GetPosition() + giro * Vector3.forward * Saida + Vector3.up;
            if (!jogador.TeleportTo(pos, giro, true))
                return;
            if (Game.instance != null)
                Game.instance.IncrementPlayerStat(PlayerStatType.PortalsUsed, 1f, false);
            Escolhendo = false;
            IgnorarDuplo = Time.time + 0.5f;
            Liberar = Time.time + 2f;
            mapa.SetMapMode(Minimap.MapMode.Small);
        }

        static void Sincronizar()
        {
            try
            {
                if (Minimap.instance == null || ZDOMan.instance == null)
                    return;
                List<ZDO> lista = ZDOMan.instance.GetPortalList();
                Dictionary<ZDOID, ZDO> vivos = new Dictionary<ZDOID, ZDO>();
                if (lista != null)
                {
                    for (int i = 0; i < lista.Count; i++)
                    {
                        ZDO zdo = lista[i];
                        if (zdo == null || zdo.m_uid.Equals(ZDOID.None))
                            continue;
                        if (!vivos.ContainsKey(zdo.m_uid))
                            vivos.Add(zdo.m_uid, zdo);
                    }
                }
                List<ZDOID> sairam = new List<ZDOID>();
                foreach (KeyValuePair<ZDOID, Minimap.PinData> par in marcas)
                {
                    if (!vivos.ContainsKey(par.Key))
                        sairam.Add(par.Key);
                }
                for (int i = 0; i < sairam.Count; i++)
                {
                    Minimap.PinData velho;
                    if (marcas.TryGetValue(sairam[i], out velho) && velho != null)
                        Minimap.instance.RemovePin(velho);
                    marcas.Remove(sairam[i]);
                }
                foreach (KeyValuePair<ZDOID, ZDO> par in vivos)
                {
                    string nome = Nome(par.Value);
                    Vector3 pos = par.Value.GetPosition();
                    Minimap.PinData pin;
                    if (marcas.TryGetValue(par.Key, out pin) && pin != null)
                    {
                        pin.m_pos = pos;
                        pin.m_name = nome;
                        pin.m_doubleSize = true;
                        continue;
                    }
                    pin = Minimap.instance.AddPin(pos, Minimap.PinType.Icon3, nome, false, false, 0L, PlatformUserID.None);
                    if (pin == null)
                        continue;
                    pin.m_doubleSize = true;
                    marcas[par.Key] = pin;
                }
                FieldInfo atualiza = typeof(Minimap).GetField("m_pinUpdateRequired", BindingFlags.Instance | BindingFlags.NonPublic);
                if (atualiza != null)
                    atualiza.SetValue(Minimap.instance, true);
            }
            catch (Exception ex)
            {
                if (!avisou)
                {
                    avisou = true;
                    Debug.LogWarning("Portal Universal: " + ex.Message);
                }
            }
        }

        static void Apagar()
        {
            if (marcas.Count == 0 || Minimap.instance == null)
            {
                marcas.Clear();
                return;
            }
            foreach (KeyValuePair<ZDOID, Minimap.PinData> par in marcas)
            {
                if (par.Value != null)
                    Minimap.instance.RemovePin(par.Value);
            }
            marcas.Clear();
        }

        static ZDOID MarcaPerto(Vector3 mundo, float raio)
        {
            ZDOID melhor = ZDOID.None;
            float perto = raio;
            foreach (KeyValuePair<ZDOID, Minimap.PinData> par in marcas)
            {
                if (par.Value == null)
                    continue;
                float dx = par.Value.m_pos.x - mundo.x;
                float dz = par.Value.m_pos.z - mundo.z;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                if (dist < perto)
                {
                    perto = dist;
                    melhor = par.Key;
                }
            }
            return melhor;
        }

        static ZDOID PortalPerto(Vector3 pos)
        {
            ZDOID melhor = ZDOID.None;
            float perto = 8f;
            if (ZDOMan.instance == null)
                return melhor;
            List<ZDO> lista = ZDOMan.instance.GetPortalList();
            if (lista == null)
                return melhor;
            for (int i = 0; i < lista.Count; i++)
            {
                ZDO zdo = lista[i];
                if (zdo == null)
                    continue;
                float distancia = Vector3.Distance(zdo.GetPosition(), pos);
                if (distancia < perto)
                {
                    perto = distancia;
                    melhor = zdo.m_uid;
                }
            }
            return melhor;
        }

        static string Nome(ZDO zdo)
        {
            string tag = zdo.GetString(ZDOVars.s_tag, "");
            if (tag == null)
                return "Portal";
            tag = tag.Trim();
            if (tag.Length == 0)
                return "Portal";
            return tag;
        }

        static void Avisar(Player jogador, string texto)
        {
            if (jogador != null)
                jogador.Message(MessageHud.MessageType.Center, texto, 0, null, false);
        }
    }

    [HarmonyPatch(typeof(TeleportWorld), "Teleport")]
    public static class AoEntrar
    {
        static bool Prefix(TeleportWorld __instance, Player player)
        {
            if (Plugin.Ligado == null || !Plugin.Ligado.Value)
                return true;
            if (player == null || Player.m_localPlayer == null || player != Player.m_localPlayer)
                return true;
            if (Game.m_noMap)
                return true;
            if (Time.time < Plugin.Liberar || player.IsTeleporting())
                return false;
            if (Plugin.Bloqueado(__instance, player))
                return false;
            if (Minimap.instance == null || ZDOMan.instance == null)
                return true;
            Plugin.Abrir(__instance, player);
            return false;
        }
    }

    [HarmonyPatch(typeof(Minimap), "OnMapLeftClick")]
    public static class AoClicar
    {
        static bool Prefix()
        {
            if (!Plugin.Escolhendo)
                return true;
            Plugin.Escolher();
            return false;
        }
    }

    [HarmonyPatch(typeof(Minimap), "OnMapDblClick")]
    public static class AoDuplo
    {
        static bool Prefix()
        {
            if (Plugin.Escolhendo || Time.time < Plugin.IgnorarDuplo)
                return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(Minimap), "SetMapMode")]
    public static class AoFechar
    {
        static void Postfix(Minimap.MapMode mode)
        {
            if (mode != Minimap.MapMode.Large)
                Plugin.Escolhendo = false;
        }
    }
}
