using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RotaDoBau
{
    [BepInPlugin("yuri.ChestRoute", "Chest Route", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instancia;
        public static ConfigFile Arquivo;
        public static ConfigEntry<bool> Ligado;
        public static ConfigEntry<float> Distancia;
        public static ConfigEntry<bool> SoEmpilhavel;
        public static ConfigEntry<bool> Aviso;
        public static ConfigEntry<bool> Agrupar;
        public static ConfigEntry<bool> MesmoTipo;
        public static ConfigEntry<string> Grupos;
        public static ConfigEntry<string> Prefixos;
        public static ConfigEntry<string> Sufixos;
        public static ConfigEntry<string> Pular;

        private const string GruposPadrao =
            "valuables=coins,ruby,amber,amberpearl;" +
            "ore=copperore,tinore,ironore,silverore,ironscrap,blackmetalscrap,flametalore;" +
            "stone=stone,flint,obsidian;" +
            "bars=copper,tin,bronze,iron,silver,blackmetal,flametal;" +
            "wood=wood,finewood,corewood,roundlog,yggdrasilwood;" +
            "mushrooms=mushroom,mushroomblue,mushroomyellow;" +
            "fruit=blueberries,cloudberry,cloudberries,raspberry,raspberries,honey;" +
            "crops=carrot,turnip,onion;" +
            "food=cookedmeat,necktailgrilled,fishcooked,serpentmeatcooked,cookedloxmeat,carrotsoup,sausages,queensjam,serpentstew,turnipstew,bloodpudding,onionsoup";

        private void Awake()
        {
            Instancia = this;
            Arquivo = Config;
            Ligado = Config.Bind("General", "Enabled", true, "Turns the mod on. Only your own game needs this mod.");
            Distancia = Config.Bind("General", "Distance", 15f, "Distance, in meters, used to look for chests.");
            SoEmpilhavel = Config.Bind("General", "StacksOnly", true, "Weapons, tools and armor do not leave the inventory through the Store button.");
            Aviso = Config.Bind("General", "Notice", true, "Shows a notice when an item moves to another chest.");
            Agrupar = Config.Bind("General", "Group", true, "Uses groups, prefixes and suffixes. Ore goes to an ore chest, seeds go to a seed chest.");
            MesmoTipo = Config.Bind("General", "SameType", false, "If no group is found, a chest with the same game type is accepted, such as material or food.");
            Grupos = Config.Bind("Groups", "List", GruposPadrao, "Groups separated by semicolons. Example: fish=fishraw,fishcooked");
            Prefixos = Config.Bind("Groups", "Prefixes", "trophy,arrow,mead", "Items that start the same stay together. trophy groups the trophies.");
            Sufixos = Config.Bind("Groups", "Suffixes", "seeds,pelt,hide,cone", "Items that end the same stay together. seeds groups the seeds.");
            Pular = Config.Bind("Store", "Skip", "", "Items the Store button does not touch. Ctrl+click the Group button adds that chest to this list.");
            new Harmony("yuri.ChestRoute").PatchAll();
        }
    }

    public class GrupoInfo
    {
        public string Nome;
        public List<string> Itens;
    }

    [HarmonyPatch(typeof(InventoryGui), "UpdateContainer")]
    public static class BotaoPatch
    {
        private static void Postfix(InventoryGui __instance)
        {
            CliquePatch.GarantirBotao(__instance);
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "OnSelectedItem")]
    public static class CliquePatch
    {
        private static readonly FieldInfo CampoAberto =
            AccessTools.Field(typeof(InventoryGui), "m_currentContainer");

        private static Button botao;
        private static Button botaoGrupo;
        private static Button botaoVoltar;
        private static bool armado;
        private static bool abertoJaTinha;
        private static InventoryGui guiAtual;
        private static List<Envio> ultimoEnvio = new List<Envio>();
        private static int sessaoBau = int.MinValue;
        private static Dictionary<string, int> sessaoItens;
        private static readonly HashSet<int> TampaNossa = new HashSet<int>();

        private class Envio
        {
            public Container Bau;
            public string Nome;
            public int Quantidade;
            public int Qualidade;
            public int Nivel;
            public int Variante;
        }

        public static void GarantirBotao(InventoryGui gui)
        {
            guiAtual = gui;
            if (gui == null || gui.m_stackAllButton == null)
                return;

            if (botao == null)
                botao = CriarBotao(gui, "RotaDoBauGuardar", "Store", 1, new Color(0.35f, 0.62f, 0.38f, 1f), GuardarTudo);
            if (botaoGrupo == null)
                botaoGrupo = CriarBotao(gui, "RotaDoBauGrupo", "Group", 2, new Color(0.75f, 0.62f, 0.25f, 1f), ClicarGrupo);
            if (botaoVoltar == null)
                botaoVoltar = CriarBotao(gui, "RotaDoBauVoltar", "Return", 3, new Color(0.72f, 0.32f, 0.24f, 1f), Voltar);
            MarcarSessao();
        }

        private static Button CriarBotao(InventoryGui gui, string nome, string titulo, int linha, Color cor, UnityAction acao)
        {
            Button novo = (Button)Object.Instantiate(gui.m_stackAllButton, gui.m_stackAllButton.transform.parent);
            novo.name = nome;
            novo.onClick = new Button.ButtonClickedEvent();
            novo.onClick.AddListener(acao);

            RectTransform origem = gui.m_stackAllButton.GetComponent<RectTransform>();
            RectTransform copia = novo.GetComponent<RectTransform>();
            copia.anchorMin = origem.anchorMin;
            copia.anchorMax = origem.anchorMax;
            copia.pivot = origem.pivot;
            copia.sizeDelta = new Vector2(104f, 28f);
            copia.anchoredPosition = origem.anchoredPosition + new Vector2(180f, -34f * (linha - 1));
            novo.transform.SetAsLastSibling();

            Image imagem = novo.GetComponent<Image>();
            if (imagem != null)
                imagem.color = cor;

            TMP_Text texto = novo.GetComponentInChildren<TMP_Text>();
            if (texto != null)
                texto.text = titulo;
            return novo;
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

        private static Container BauAberto(InventoryGui gui)
        {
            if (gui == null || CampoAberto == null)
                return null;
            return (Container)CampoAberto.GetValue(gui);
        }

        private static void Prefix(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item, InventoryGrid.Modifier mod)
        {
            armado = false;
            guiAtual = __instance;
            if (!Plugin.Ligado.Value || item == null || item.m_shared == null)
                return;
            if (mod != InventoryGrid.Modifier.Move || grid != __instance.m_playerGrid || item.m_equipped)
                return;
            if (!PodeMexer(item))
                return;

            if (Tecla(KeyCode.L))
                AvisarSempre("Item: " + Chave(item));

            Container aberto = BauAberto(__instance);
            if (aberto == null || aberto.GetInventory() == null)
                return;

            armado = true;
            abertoJaTinha = aberto.GetInventory().ContainsItemByName(item.m_shared.m_name);
        }

        private static void Postfix(InventoryGui __instance, ItemDrop.ItemData item)
        {
            if (!armado || item == null || item.m_shared == null)
                return;
            armado = false;

            Player player = Player.m_localPlayer;
            if (player == null)
                return;

            Container aberto = BauAberto(__instance);
            Inventory inventario = player.GetInventory();
            bool aindaNoPlayer = inventario != null && inventario.ContainsItem(item);
            bool noAberto = aberto != null && aberto.GetInventory() != null && aberto.GetInventory().ContainsItem(item);

            if (abertoJaTinha && !aindaNoPlayer)
                return;

            Inventory origem;
            if (!abertoJaTinha && noAberto)
                origem = aberto.GetInventory();
            else if (aindaNoPlayer)
                origem = inventario;
            else
                return;

            Container destino = Escolher(player, aberto, item, aindaNoPlayer, true);
            if (!Mover(origem, destino, item))
                return;
            if (Plugin.Aviso.Value)
            {
                string aviso = aindaNoPlayer ? "Chest full, sent to another" : "Sent to another chest";
                player.Message(MessageHud.MessageType.Center, aviso, 0, null, true);
            }
        }

        private static void ClicarGrupo()
        {
            Container aberto = BauAberto(guiAtual);
            if (aberto == null || aberto.GetInventory() == null)
            {
                AvisarSempre("Open a chest first");
                return;
            }

            List<string> chaves = ChavesDoInventario(aberto.GetInventory());
            if (chaves.Count == 0)
            {
                AvisarSempre("Empty chest");
                return;
            }

            if (Tecla(KeyCode.LeftControl) || Tecla(KeyCode.RightControl))
            {
                MudarPular(chaves, true);
                AvisarSempre("These items stay out of Store");
                return;
            }

            if (Tecla(KeyCode.LeftShift) || Tecla(KeyCode.RightShift))
            {
                MudarPular(chaves, false);
                AvisarSempre("These items can be stored again");
                return;
            }

            string nome = JuntarGrupo(chaves);
            AvisarSempre("Group " + nome);
        }

        private static void GuardarTudo()
        {
            if (!Plugin.Ligado.Value)
                return;
            Player player = Player.m_localPlayer;
            if (player == null || player.GetInventory() == null)
                return;

            Container aberto = BauAberto(guiAtual);
            Inventory inventario = player.GetInventory();
            int movidos = 0;
            MarcarSessao();

            for (int volta = 0; volta < 4; volta++)
            {
                bool andou = false;
                List<ItemDrop.ItemData> itens = new List<ItemDrop.ItemData>(inventario.GetAllItems());
                for (int i = 0; i < itens.Count; i++)
                {
                    ItemDrop.ItemData item = itens[i];
                    if (!PodeMexer(item) || !inventario.ContainsItem(item))
                        continue;
                    if (ListaContem(Plugin.Pular.Value, Chave(item)))
                        continue;
                    Container destino = Escolher(player, aberto, item, true, false);
                    if (Mover(inventario, destino, item))
                    {
                        andou = true;
                        movidos++;
                    }
                }
                if (!andou)
                    break;
            }

            if (movidos > 0 && Plugin.Aviso.Value)
                player.Message(MessageHud.MessageType.Center, "Items stored", 0, null, true);
        }

        private static void Voltar()
        {
            Player player = Player.m_localPlayer;
            if (player == null || player.GetInventory() == null)
            {
                AvisarSempre("Nothing to bring back");
                return;
            }

            MarcarSessao();
            int voltou = PuxarDiferenca(player);
            if (voltou == 0)
            {
                for (int i = ultimoEnvio.Count - 1; i >= 0; i--)
                {
                    if (Puxar(player, ultimoEnvio[i]))
                        voltou++;
                }
            }

            ultimoEnvio = new List<Envio>();
            sessaoItens = Contar(player.GetInventory());
            AvisarSempre(voltou > 0 ? "Items brought back" : "Nothing to bring back");
        }

        private static void MarcarSessao()
        {
            Player player = Player.m_localPlayer;
            if (player == null || player.GetInventory() == null)
                return;
            Container aberto = BauAberto(guiAtual);
            int id = aberto != null ? aberto.GetInstanceID() : 0;
            if (sessaoItens != null && id == sessaoBau)
                return;
            sessaoBau = id;
            sessaoItens = Contar(player.GetInventory());
            ultimoEnvio = new List<Envio>();
        }

        private static Dictionary<string, int> Contar(Inventory inv)
        {
            Dictionary<string, int> mapa = new Dictionary<string, int>();
            if (inv == null)
                return mapa;
            List<ItemDrop.ItemData> itens = inv.GetAllItems();
            for (int i = 0; i < itens.Count; i++)
            {
                ItemDrop.ItemData item = itens[i];
                if (item == null || item.m_shared == null || item.m_equipped)
                    continue;
                string chave = ChaveStack(item);
                int tem = mapa.ContainsKey(chave) ? mapa[chave] : 0;
                mapa[chave] = tem + item.m_stack;
            }
            return mapa;
        }

        private static string ChaveStack(ItemDrop.ItemData item)
        {
            return item.m_shared.m_name + "|" + item.m_quality.ToString() + "|" + item.m_variant.ToString() + "|" + item.m_worldLevel.ToString();
        }

        private static int PuxarDiferenca(Player player)
        {
            if (sessaoItens == null)
                return 0;
            Dictionary<string, int> agora = Contar(player.GetInventory());
            int voltou = 0;
            List<string> chaves = new List<string>(sessaoItens.Keys);
            for (int i = 0; i < chaves.Count; i++)
            {
                string chave = chaves[i];
                int tinha = sessaoItens[chave];
                int tem = agora.ContainsKey(chave) ? agora[chave] : 0;
                int falta = tinha - tem;
                if (falta <= 0)
                    continue;
                if (PuxarChave(player, chave, falta))
                    voltou++;
            }
            return voltou;
        }

        private static bool PuxarChave(Player player, string chave, int quantidade)
        {
            string[] partes = chave.Split('|');
            if (partes.Length < 4)
                return false;
            string nome = partes[0];
            int qualidade = int.Parse(partes[1]);
            int variante = int.Parse(partes[2]);
            int nivel = int.Parse(partes[3]);
            int falta = quantidade;
            string marca = nome + "|" + qualidade.ToString() + "|" + variante.ToString() + "|" + nivel.ToString();
            List<Container> baus = BausPerto(player);
            for (int b = 0; b < baus.Count && falta > 0; b++)
            {
                int tinha = Quantidade(player.GetInventory(), marca);
                Envio envio = new Envio();
                envio.Bau = baus[b];
                envio.Nome = nome;
                envio.Quantidade = falta;
                envio.Qualidade = qualidade;
                envio.Variante = variante;
                envio.Nivel = nivel;
                Puxar(player, envio);
                int tem = Quantidade(player.GetInventory(), marca);
                int ganhou = tem - tinha;
                if (ganhou > 0)
                    falta -= ganhou;
            }
            return falta < quantidade;
        }

        private static int Quantidade(Inventory inv, string marca)
        {
            Dictionary<string, int> mapa = Contar(inv);
            return mapa.ContainsKey(marca) ? mapa[marca] : 0;
        }

        private static List<Container> BausPerto(Player player)
        {
            List<Container> lista = new List<Container>();
            Container aberto = BauAberto(guiAtual);
            if (aberto != null)
                lista.Add(aberto);
            float alcance = Plugin.Distancia.Value;
            if (alcance < 30f)
                alcance = 30f;
            List<Piece> pecas = new List<Piece>();
            Piece.GetAllPiecesInRadius(player.transform.position, alcance, pecas);
            for (int i = 0; i < pecas.Count; i++)
            {
                Piece peca = pecas[i];
                if (peca == null)
                    continue;
                Container bau = peca.GetComponent<Container>();
                if (bau == null)
                    bau = peca.GetComponentInParent<Container>();
                if (bau == null || lista.Contains(bau))
                    continue;
                if (NomeInseguro(bau.gameObject.name))
                    continue;
                lista.Add(bau);
            }
            return lista;
        }

        private static bool Puxar(Player player, Envio envio)
        {
            if (envio == null || envio.Bau == null || envio.Quantidade <= 0)
                return false;
            Inventory origem = envio.Bau.GetInventory();
            Inventory destino = player.GetInventory();
            if (origem == null || destino == null)
                return false;

            ZNetView rede = envio.Bau.GetComponent<ZNetView>();
            if (rede != null && rede.IsValid() && !rede.IsOwner())
                rede.ClaimOwnership();

            int falta = envio.Quantidade;
            List<ItemDrop.ItemData> itens = new List<ItemDrop.ItemData>(origem.GetAllItems());
            for (int i = 0; i < itens.Count && falta > 0; i++)
            {
                ItemDrop.ItemData item = itens[i];
                if (item == null || item.m_shared == null || item.m_shared.m_name != envio.Nome)
                    continue;
                if (item.m_quality != envio.Qualidade || item.m_variant != envio.Variante || item.m_worldLevel != envio.Nivel)
                    continue;

                int pegar = item.m_stack < falta ? item.m_stack : falta;
                ItemDrop.ItemData copia = item.Clone();
                copia.m_stack = pegar;
                copia.m_equipped = false;
                if (!destino.AddItem(copia))
                    return falta < envio.Quantidade;
                origem.RemoveItem(item, pegar);
                falta -= pegar;
            }
            return falta < envio.Quantidade;
        }

        private static Container Escolher(Player player, Container aberto, ItemDrop.ItemData item, bool aceitaQualquer, bool pularAberto)
        {
            Container destino = Achar(player, aberto, item, 0, pularAberto);
            if (destino == null && Plugin.Agrupar.Value)
                destino = Achar(player, aberto, item, 1, pularAberto);
            if (destino == null && Plugin.MesmoTipo.Value)
                destino = Achar(player, aberto, item, 2, pularAberto);
            if (destino == null && aceitaQualquer)
                destino = Achar(player, aberto, item, 3, pularAberto);
            return destino;
        }

        private static Container Achar(Player player, Container aberto, ItemDrop.ItemData item, int modo, bool pularAberto)
        {
            List<Piece> pecas = new List<Piece>();
            Piece.GetAllPiecesInRadius(player.transform.position, Plugin.Distancia.Value, pecas);

            Container melhor = null;
            float menor = float.MaxValue;
            string nome = item.m_shared.m_name;
            string chave = Chave(item);

            for (int i = 0; i < pecas.Count; i++)
            {
                Piece peca = pecas[i];
                if (peca == null)
                    continue;

                Container bau = peca.GetComponent<Container>();
                if (bau == null)
                    bau = peca.GetComponentInParent<Container>();
                if (bau == null)
                    continue;
                if (pularAberto && bau == aberto)
                    continue;
                if (bau != aberto && bau.IsInUse() && !TampaNossa.Contains(bau.GetInstanceID()))
                    continue;
                if (NomeInseguro(bau.gameObject.name))
                    continue;

                Inventory inv = bau.GetInventory();
                if (inv == null || !Cabe(inv, item))
                    continue;
                if (modo == 0 && !inv.ContainsItemByName(nome))
                    continue;
                if (modo == 1 && !CombinaGrupo(inv, chave))
                    continue;
                if (modo == 2 && !TemMesmoTipo(inv, item.m_shared.m_itemType))
                    continue;

                float dist = Vector3.Distance(player.transform.position, bau.transform.position);
                if (dist < menor)
                {
                    menor = dist;
                    melhor = bau;
                }
            }

            return melhor;
        }

        private static bool Mover(Inventory origem, Container destino, ItemDrop.ItemData item)
        {
            if (origem == null || destino == null || destino.GetInventory() == null || destino.GetInventory() == origem)
                return false;

            ZNetView rede = destino.GetComponent<ZNetView>();
            if (rede != null && rede.IsValid() && !rede.IsOwner())
                rede.ClaimOwnership();

            int antes = item.m_stack;
            string nome = item.m_shared.m_name;
            int qualidade = item.m_quality;
            int nivel = item.m_worldLevel;
            int variante = item.m_variant;
            destino.GetInventory().MoveItemToThis(origem, item);
            bool ainda = origem.ContainsItem(item);
            int quantidade = ainda ? antes - item.m_stack : antes;
            bool mudou = quantidade > 0;
            if (!mudou)
                return false;

            Envio envio = new Envio();
            envio.Bau = destino;
            envio.Nome = nome;
            envio.Quantidade = quantidade;
            envio.Qualidade = qualidade;
            envio.Nivel = nivel;
            envio.Variante = variante;
            ultimoEnvio.Add(envio);

            TocarBau(destino);
            return true;
        }

        private static readonly Dictionary<int, float> UltimoSom = new Dictionary<int, float>();

        private static void TocarBau(Container bau)
        {
            if (bau == null)
                return;
            int id = bau.GetInstanceID();
            float agora = Time.time;
            if (UltimoSom.ContainsKey(id) && agora - UltimoSom[id] < 0.8f)
                return;
            UltimoSom[id] = agora;

            bool jaAberto = bau == BauAberto(guiAtual);
            bool abertoLigado = bau.m_open != null && bau.m_open.activeSelf;
            bool fechadoLigado = bau.m_closed != null && bau.m_closed.activeSelf;
            if (!jaAberto)
            {
                bau.SetInUse(true);
                TampaNossa.Add(id);
                AbrirTampa(bau, true);
            }

            Vector3 pos = bau.transform.position + Vector3.up * 0.6f;
            TocarEfeito(bau.m_openEffects, pos, true);
            if (!TemEfeito(bau.m_openEffects))
                TocarZsfx(bau);
            if (Plugin.Instancia != null)
                Plugin.Instancia.StartCoroutine(FecharDepois(bau, jaAberto, abertoLigado, fechadoLigado));
        }

        private static void AbrirTampa(Container bau, bool abrir)
        {
            if (bau.m_open != null)
                bau.m_open.SetActive(abrir);
            if (bau.m_closed != null)
                bau.m_closed.SetActive(!abrir);
        }

        private static IEnumerator FecharDepois(Container bau, bool jaAberto, bool abertoLigado, bool fechadoLigado)
        {
            yield return new WaitForSeconds(0.85f);
            if (bau == null)
                yield break;
            TocarEfeito(bau.m_closeEffects, bau.transform.position + Vector3.up * 0.6f, false);
            TampaNossa.Remove(bau.GetInstanceID());
            if (jaAberto || bau == BauAberto(guiAtual))
                yield break;
            bau.SetInUse(false);
            if (bau.m_open != null)
                bau.m_open.SetActive(abertoLigado);
            if (bau.m_closed != null)
                bau.m_closed.SetActive(fechadoLigado);
        }

        private static bool TemEfeito(EffectList lista)
        {
            return lista != null && lista.m_effectPrefabs != null && lista.m_effectPrefabs.Length > 0;
        }

        private static void TocarEfeito(EffectList lista, Vector3 posBau, bool abrir)
        {
            Quaternion rot = Quaternion.identity;
            Player player = Player.m_localPlayer;
            Vector3 perto = posBau;
            if (player != null)
                perto = player.transform.position + Vector3.up * 1.2f;

            if (TemEfeito(lista))
            {
                lista.Create(posBau, rot, null, 1f, -1, default(ZDOID));
                if (player != null && Vector3.Distance(player.transform.position, posBau) > 4f)
                    lista.Create(perto, rot, null, 1f, -1, default(ZDOID));
                return;
            }

            InventoryGui gui = InventoryGui.instance;
            if (gui == null)
                return;
            EffectList reserva = abrir ? gui.m_openInventoryEffects : gui.m_closeInventoryEffects;
            if (TemEfeito(reserva))
                reserva.Create(perto, rot, null, 1f, -1, default(ZDOID));
        }

        private static void TocarZsfx(Container bau)
        {
            ZSFX[] sons = bau.GetComponentsInChildren<ZSFX>(true);
            if (sons == null)
                return;
            for (int i = 0; i < sons.Length && i < 2; i++)
            {
                if (sons[i] != null)
                    sons[i].Play();
            }
        }

        private static bool PodeMexer(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null || item.m_equipped)
                return false;
            if (Plugin.SoEmpilhavel.Value && item.m_shared.m_maxStackSize <= 1)
                return false;
            return true;
        }

        private static bool Cabe(Inventory inv, ItemDrop.ItemData item)
        {
            if (inv.FindFreeStackSpace(item.m_shared.m_name, item.m_worldLevel) > 0)
                return true;
            return inv.GetEmptySlots() > 0 && inv.CanAddItem(item, 1);
        }

        private static bool CombinaGrupo(Inventory inv, string chave)
        {
            if (chave.Length == 0)
                return false;
            List<ItemDrop.ItemData> itens = inv.GetAllItems();
            for (int i = 0; i < itens.Count; i++)
            {
                string outra = Chave(itens[i]);
                if (outra.Length == 0 || outra == chave)
                    continue;
                if (MesmoGrupo(chave, outra) || MesmoAfixo(chave, outra))
                    return true;
            }
            return false;
        }

        private static bool TemMesmoTipo(Inventory inv, ItemDrop.ItemData.ItemType tipo)
        {
            if (tipo == ItemDrop.ItemData.ItemType.None || tipo == ItemDrop.ItemData.ItemType.Misc)
                return false;
            List<ItemDrop.ItemData> itens = inv.GetAllItems();
            for (int i = 0; i < itens.Count; i++)
            {
                ItemDrop.ItemData outro = itens[i];
                if (outro != null && outro.m_shared != null && outro.m_shared.m_itemType == tipo)
                    return true;
            }
            return false;
        }

        private static List<string> ChavesDoInventario(Inventory inv)
        {
            List<string> chaves = new List<string>();
            List<ItemDrop.ItemData> itens = inv.GetAllItems();
            for (int i = 0; i < itens.Count; i++)
            {
                string chave = Chave(itens[i]);
                if (chave.Length > 0 && !chaves.Contains(chave))
                    chaves.Add(chave);
            }
            return chaves;
        }

        private static string JuntarGrupo(List<string> chaves)
        {
            List<GrupoInfo> grupos = LerGrupos();
            List<GrupoInfo> acertos = new List<GrupoInfo>();
            for (int i = 0; i < grupos.Count; i++)
            {
                if (Intersecta(grupos[i].Itens, chaves))
                    acertos.Add(grupos[i]);
            }

            GrupoInfo baseGrupo;
            if (acertos.Count == 0)
            {
                baseGrupo = new GrupoInfo();
                baseGrupo.Nome = "group" + (grupos.Count + 1).ToString();
                baseGrupo.Itens = new List<string>();
                grupos.Add(baseGrupo);
            }
            else
            {
                baseGrupo = acertos[0];
                for (int i = 1; i < acertos.Count; i++)
                {
                    if (acertos[i].Itens.Count > baseGrupo.Itens.Count)
                        baseGrupo = acertos[i];
                }
            }

            for (int i = 0; i < chaves.Count; i++)
            {
                if (!baseGrupo.Itens.Contains(chaves[i]))
                    baseGrupo.Itens.Add(chaves[i]);
            }

            for (int a = 0; a < acertos.Count; a++)
            {
                if (acertos[a] == baseGrupo)
                    continue;
                for (int k = 0; k < acertos[a].Itens.Count; k++)
                {
                    string item = acertos[a].Itens[k];
                    if (!baseGrupo.Itens.Contains(item))
                        baseGrupo.Itens.Add(item);
                }
                acertos[a].Itens.Clear();
            }

            for (int i = grupos.Count - 1; i >= 0; i--)
            {
                if (grupos[i].Itens.Count == 0)
                    grupos.RemoveAt(i);
            }

            SalvarGrupos(grupos);
            return baseGrupo.Nome;
        }

        private static bool MesmoGrupo(string a, string b)
        {
            string grupoA = GrupoDaChave(a);
            string grupoB = GrupoDaChave(b);
            return grupoA.Length > 0 && grupoA == grupoB;
        }

        private static bool MesmoAfixo(string a, string b)
        {
            string prefixoA = Afixo(Plugin.Prefixos.Value, a, true);
            string prefixoB = Afixo(Plugin.Prefixos.Value, b, true);
            if (prefixoA.Length > 0 && prefixoA == prefixoB)
                return true;
            string sufixoA = Afixo(Plugin.Sufixos.Value, a, false);
            string sufixoB = Afixo(Plugin.Sufixos.Value, b, false);
            return sufixoA.Length > 0 && sufixoA == sufixoB;
        }

        private static string GrupoDaChave(string chave)
        {
            List<GrupoInfo> grupos = LerGrupos();
            for (int i = 0; i < grupos.Count; i++)
            {
                if (grupos[i].Itens.Contains(chave))
                    return grupos[i].Nome;
            }
            return "";
        }

        private static string Afixo(string lista, string chave, bool prefixo)
        {
            List<string> partes = Separar(lista);
            string achado = "";
            for (int i = 0; i < partes.Count; i++)
            {
                string parte = partes[i];
                bool bate = prefixo ? chave.StartsWith(parte) : chave.EndsWith(parte);
                if (bate && parte.Length > achado.Length)
                    achado = parte;
            }
            return achado;
        }

        private static List<GrupoInfo> LerGrupos()
        {
            List<GrupoInfo> grupos = new List<GrupoInfo>();
            if (Plugin.Grupos == null || string.IsNullOrEmpty(Plugin.Grupos.Value))
                return grupos;

            string[] partes = Plugin.Grupos.Value.Split(';');
            for (int i = 0; i < partes.Length; i++)
            {
                string parte = partes[i].Trim();
                int igual = parte.IndexOf('=');
                if (igual <= 0)
                    continue;
                GrupoInfo grupo = new GrupoInfo();
                grupo.Nome = parte.Substring(0, igual).Trim().ToLowerInvariant();
                grupo.Itens = Separar(parte.Substring(igual + 1));
                if (grupo.Nome.Length > 0 && grupo.Itens.Count > 0)
                    grupos.Add(grupo);
            }
            return grupos;
        }

        private static void SalvarGrupos(List<GrupoInfo> grupos)
        {
            string texto = "";
            for (int i = 0; i < grupos.Count; i++)
            {
                if (grupos[i].Itens.Count == 0)
                    continue;
                if (texto.Length > 0)
                    texto += ";";
                texto += grupos[i].Nome + "=" + Juntar(grupos[i].Itens);
            }
            Plugin.Grupos.Value = texto;
            Plugin.Arquivo.Save();
        }

        private static void MudarPular(List<string> chaves, bool adicionar)
        {
            List<string> atual = Separar(Plugin.Pular.Value);
            for (int i = 0; i < chaves.Count; i++)
            {
                if (adicionar)
                {
                    if (!atual.Contains(chaves[i]))
                        atual.Add(chaves[i]);
                }
                else
                {
                    atual.Remove(chaves[i]);
                }
            }
            Plugin.Pular.Value = Juntar(atual);
            Plugin.Arquivo.Save();
        }

        private static bool ListaContem(string lista, string chave)
        {
            return Separar(lista).Contains(chave);
        }

        private static List<string> Separar(string texto)
        {
            List<string> lista = new List<string>();
            if (string.IsNullOrEmpty(texto))
                return lista;
            string[] partes = texto.Split(',');
            for (int i = 0; i < partes.Length; i++)
            {
                string parte = partes[i].Trim().ToLowerInvariant();
                if (parte.Length > 0 && !lista.Contains(parte))
                    lista.Add(parte);
            }
            return lista;
        }

        private static string Juntar(List<string> lista)
        {
            string texto = "";
            for (int i = 0; i < lista.Count; i++)
            {
                if (texto.Length > 0)
                    texto += ",";
                texto += lista[i];
            }
            return texto;
        }

        private static bool Intersecta(List<string> a, List<string> b)
        {
            for (int i = 0; i < a.Count; i++)
            {
                if (b.Contains(a[i]))
                    return true;
            }
            return false;
        }

        private static string Chave(ItemDrop.ItemData item)
        {
            if (item == null)
                return "";
            string nome = "";
            if (item.m_dropPrefab != null)
                nome = item.m_dropPrefab.name;
            if (string.IsNullOrEmpty(nome) && item.m_shared != null)
                nome = item.m_shared.m_name;
            if (string.IsNullOrEmpty(nome))
                return "";
            nome = nome.ToLowerInvariant();
            if (nome.StartsWith("$item_"))
                nome = nome.Substring(6);
            return nome;
        }

        private static void AvisarSempre(string texto)
        {
            Player player = Player.m_localPlayer;
            if (player != null)
                player.Message(MessageHud.MessageType.Center, texto, 0, null, true);
        }

        private static bool NomeInseguro(string nome)
        {
            if (string.IsNullOrEmpty(nome))
                return true;
            string n = nome.ToLowerInvariant();
            return n.Contains("ship") || n.Contains("karve") || n.Contains("cart")
                || n.Contains("wagon") || n.Contains("tombstone") || n.Contains("obliterator");
        }
    }
}
