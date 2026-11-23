using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace DropTriplo
{
    [BepInPlugin("yuri.TripleDrop", "Triple Drop", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Inst;

        ConfigEntry<bool> Ligado;
        ConfigEntry<int> Vezes;

        List<Marca> mortes = new List<Marca>();
        List<Marca> minas = new List<Marca>();
        List<Marca> cortes = new List<Marca>();
        List<Marca> fornos = new List<Marca>();
        List<Marca> minhasMortes = new List<Marca>();
        List<Alvo> alvos = new List<Alvo>();
        List<ItemDrop> largados = new List<ItemDrop>();
        float proximaLimpeza;
        int entregando;
        float ultimoAvisoPeso;
        public static int Pesca;
        public static int IgnorarPadrao;

        class Marca
        {
            public Vector3 pos;
            public float quando;
        }

        class Alvo
        {
            public Character bicho;
            public Vector3 pos;
            public float quando;
        }

        void Awake()
        {
            Inst = this;
            Ligado = Config.Bind("General", "Enabled", true, "Triples what you pick up. Your grave and the inventory from your own death stay at the normal amount. Extra copies stop at your carry weight. Only your own game needs this mod.");
            Vezes = Config.Bind("General", "Times", 3, "How many times the item comes in. 3 means the normal drop plus two extra copies.");
            Harmony harmonia = new Harmony("yuri.TripleDrop");
            harmonia.PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo("Triple Drop 1.0.7");
        }

        void Update()
        {
            VigiarAlvos();
            if (Time.time < proximaLimpeza)
                return;
            proximaLimpeza = Time.time + 2f;
            Limpar(mortes, 45f);
            Limpar(minas, 180f);
            Limpar(cortes, 180f);
            Limpar(fornos, 180f);
            Limpar(minhasMortes, 7200f);
            for (int i = largados.Count - 1; i >= 0; i--)
            {
                if (largados[i] == null)
                    largados.RemoveAt(i);
            }
        }

        static void Limpar(List<Marca> lista, float vida)
        {
            float agora = Time.time;
            for (int i = lista.Count - 1; i >= 0; i--)
            {
                if (agora - lista[i].quando > vida)
                    lista.RemoveAt(i);
            }
        }

        public int Multiplicador()
        {
            int n = Vezes.Value;
            if (n < 1)
                n = 1;
            if (n > 5)
                n = 5;
            return n;
        }

        public bool Ativo()
        {
            return Ligado.Value && Multiplicador() > 1;
        }

        public void Avisar(string texto)
        {
            Logger.LogInfo(texto);
        }

        public void MarcarGolpe(Character bicho)
        {
            if (bicho == null || bicho.IsPlayer())
                return;
            for (int i = 0; i < alvos.Count; i++)
            {
                if (alvos[i].bicho == bicho)
                {
                    alvos[i].quando = Time.time;
                    return;
                }
            }
            if (alvos.Count > 24)
                alvos.RemoveAt(0);
            Alvo novo = new Alvo();
            novo.bicho = bicho;
            novo.pos = bicho.transform.position;
            novo.quando = Time.time;
            alvos.Add(novo);
        }

        void VigiarAlvos()
        {
            for (int i = alvos.Count - 1; i >= 0; i--)
            {
                Alvo alvo = alvos[i];
                if (alvo.bicho == null)
                {
                    Guardar(mortes, alvo.pos, 16);
                    alvos.RemoveAt(i);
                    continue;
                }
                alvo.pos = alvo.bicho.transform.position;
                if (alvo.bicho.IsDead() || alvo.bicho.GetHealth() <= 0f)
                {
                    Guardar(mortes, alvo.pos, 16);
                    alvos.RemoveAt(i);
                    continue;
                }
                if (Time.time - alvo.quando > 180f)
                    alvos.RemoveAt(i);
            }
        }

        public void MarcarMorte(Character bicho)
        {
            if (bicho == null)
                return;
            bool meu = false;
            for (int i = alvos.Count - 1; i >= 0; i--)
            {
                if (alvos[i].bicho == bicho)
                {
                    meu = true;
                    alvos.RemoveAt(i);
                }
            }
            if (!meu)
                return;
            Guardar(mortes, bicho.transform.position, 16);
        }

        public void MarcarMina(Vector3 pos)
        {
            Guardar(minas, pos, 24);
        }

        public void MarcarCorte(Vector3 pos)
        {
            Guardar(cortes, pos, 24);
        }

        public void MarcarForno(Vector3 pos)
        {
            Guardar(fornos, pos, 16);
        }

        public void MarcarLargado(ItemDrop drop)
        {
            if (drop == null)
                return;
            if (!largados.Contains(drop))
                largados.Add(drop);
        }

        static void Guardar(List<Marca> lista, Vector3 pos, int max)
        {
            if (lista.Count >= max)
                lista.RemoveAt(0);
            Marca marca = new Marca();
            marca.pos = pos;
            marca.quando = Time.time;
            lista.Add(marca);
        }

        public bool VeioDeMonstro(Vector3 pos)
        {
            return Perto(mortes, pos, 18f, 45f);
        }

        public bool VeioDeMina(Vector3 pos, string prefab)
        {
            if (!EhMinerio(prefab))
                return false;
            return Perto(minas, pos, 14f, 180f);
        }

        public bool VeioDeCorte(Vector3 pos, string prefab)
        {
            if (!EhMadeira(prefab))
                return false;
            return Perto(cortes, pos, 20f, 180f);
        }

        public bool VeioDeForno(Vector3 pos, string prefab)
        {
            if (!EhMetal(prefab))
                return false;
            if (Perto(fornos, pos, 8f, 180f))
                return true;
            Smelter[] maquinas = UnityEngine.Object.FindObjectsOfType<Smelter>();
            if (maquinas == null)
                return false;
            for (int i = 0; i < maquinas.Length; i++)
            {
                if (maquinas[i] == null)
                    continue;
                if ((maquinas[i].transform.position - pos).sqrMagnitude <= 64f)
                    return true;
            }
            return false;
        }

        public bool FoiLargado(ItemDrop drop)
        {
            return drop != null && largados.Contains(drop);
        }

        public bool EhBolsaLocal(Inventory bolsa)
        {
            Player jogador = Player.m_localPlayer;
            return jogador != null && bolsa != null && jogador.GetInventory() == bolsa;
        }

        public bool BauDeLoot(Inventory bolsa)
        {
            if (bolsa == null)
                return false;
            Container[] todos = UnityEngine.Object.FindObjectsOfType<Container>();
            if (todos == null)
                return false;
            for (int i = 0; i < todos.Length; i++)
            {
                Container bau = todos[i];
                if (bau == null || bau.GetInventory() != bolsa)
                    continue;
                if (bau.m_wagon != null)
                    return false;
                if (EhLapide(bau))
                    return false;
                Piece peca = bau.GetComponent<Piece>();
                if (peca == null)
                    peca = bau.GetComponentInParent<Piece>();
                if (peca != null && peca.IsPlacedByPlayer())
                    return false;
                return true;
            }
            return false;
        }

        public void MarcarMinhaMorte(Vector3 pos)
        {
            Guardar(minhasMortes, pos, 12);
        }

        public bool VeioDaMinhaMorte(Vector3 pos)
        {
            return Perto(minhasMortes, pos, 18f, 7200f);
        }

        static bool EhLapide(Component coisa)
        {
            if (coisa == null)
                return false;
            if (coisa.GetComponent<TombStone>() != null)
                return true;
            if (coisa.GetComponentInParent<TombStone>() != null)
                return true;
            Transform atual = coisa.transform;
            while (atual != null)
            {
                string nome = atual.name;
                if (nome != null && nome.ToLower().IndexOf("tomb") >= 0)
                    return true;
                atual = atual.parent;
            }
            return false;
        }

        public void Entregar(ItemDrop.ItemData modelo, int quantidade)
        {
            if (modelo == null)
                return;
            GameObject origem = modelo.m_dropPrefab;
            if (origem == null && modelo.m_shared != null && ObjectDB.instance != null)
                origem = ObjectDB.instance.GetItemPrefab(modelo.m_shared.m_name);
            EntregarPrefab(origem, quantidade);
        }

        public void EntregarPrefab(GameObject origem, int quantidade)
        {
            if (!Ativo() || origem == null || quantidade < 1 || entregando > 0)
                return;
            int extra = quantidade * (Multiplicador() - 1);
            if (extra < 1)
                return;
            Player jogador = Player.m_localPlayer;
            if (jogador == null)
                return;
            Inventory bolsa = jogador.GetInventory();
            if (bolsa == null)
                return;
            entregando = entregando + 1;
            int coube = 0;
            bool travouPeso = false;
            float peso = PesoDeUm(origem);
            float limite = jogador.GetMaxCarryWeight();
            for (int i = 0; i < extra; i++)
            {
                if (peso > 0.001f && bolsa.GetTotalWeight() + peso > limite + 0.05f)
                {
                    travouPeso = true;
                    break;
                }
                if (!bolsa.AddItem(origem, 1))
                    break;
                coube++;
            }
            entregando = entregando - 1;
            if (travouPeso && Time.time - ultimoAvisoPeso > 3f)
            {
                ultimoAvisoPeso = Time.time;
                string texto = "Carry weight is full.";
                if (coube == 0)
                    texto = "Carry weight is full. The extra did not go in.";
                jogador.Message(MessageHud.MessageType.Center, texto, 0, null, true);
            }
            else if (coube == 0)
                jogador.Message(MessageHud.MessageType.Center, "Bag is full. The extra did not fit.", 0, null, true);
            else
                Avisar("Extra " + origem.name + " x" + coube);
        }

        static float PesoDeUm(GameObject origem)
        {
            if (origem == null)
                return 0f;
            ItemDrop drop = origem.GetComponent<ItemDrop>();
            if (drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null)
                return 0f;
            return drop.m_itemData.GetWeight(1);
        }

        public static int Contar(string nome)
        {
            Player jogador = Player.m_localPlayer;
            if (jogador == null || jogador.GetInventory() == null || nome == null || nome == "")
                return 0;
            int n = 0;
            List<ItemDrop.ItemData> lista = jogador.GetInventory().GetAllItems();
            for (int i = 0; i < lista.Count; i++)
            {
                if (lista[i] != null && lista[i].m_shared != null && lista[i].m_shared.m_name == nome)
                    n = n + lista[i].m_stack;
            }
            return n;
        }

        static bool Perto(List<Marca> lista, Vector3 pos, float raio, float vida)
        {
            float agora = Time.time;
            float raio2 = raio * raio;
            for (int i = 0; i < lista.Count; i++)
            {
                if (agora - lista[i].quando > vida)
                    continue;
                if ((lista[i].pos - pos).sqrMagnitude <= raio2)
                    return true;
            }
            return false;
        }

        static double Idade(ItemDrop drop)
        {
            try
            {
                if (drop == null)
                    return 0.0;
                System.Reflection.FieldInfo campo = typeof(ItemDrop).GetField("m_nview", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                object visao = campo == null ? null : campo.GetValue(drop);
                if (visao == null)
                    return 0.0;
                System.Reflection.MethodInfo valido = visao.GetType().GetMethod("IsValid");
                if (valido != null && !(bool)valido.Invoke(visao, null))
                    return 0.0;
                System.Reflection.MethodInfo pegar = visao.GetType().GetMethod("GetZDO");
                if (pegar != null && pegar.Invoke(visao, null) == null)
                    return 0.0;
                System.Reflection.MethodInfo metodo = typeof(ItemDrop).GetMethod("GetTimeSinceSpawned", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (metodo == null)
                    return 0.0;
                object valor = metodo.Invoke(drop, null);
                if (valor is double)
                    return (double)valor;
                if (valor is float)
                    return (float)valor;
            }
            catch
            {
            }
            return 0.0;
        }

        static bool EhMinerio(string nome)
        {
            if (string.IsNullOrEmpty(nome))
                return false;
            string[] lista = new string[] {
                "Stone", "Flint", "Coal", "CopperOre", "TinOre", "IronScrap", "SilverOre",
                "Obsidian", "Chitin", "BlackMarble", "CopperScrap", "FlametalOre", "Sulfur",
                "ProustitePowder", "Grausten", "BlackwoodAsh", "Ashstone"
            };
            for (int i = 0; i < lista.Length; i++)
            {
                if (nome == lista[i])
                    return true;
            }
            return false;
        }

        static bool EhMadeira(string nome)
        {
            if (string.IsNullOrEmpty(nome))
                return false;
            string[] lista = new string[] {
                "Wood", "FineWood", "RoundLog", "ElderBark", "YggdrasilWood", "Blackwood",
                "Resin", "BeechSeeds", "BirchSeeds", "Acorn", "FirCone"
            };
            for (int i = 0; i < lista.Length; i++)
            {
                if (nome == lista[i])
                    return true;
            }
            return false;
        }

        static bool EhMetal(string nome)
        {
            if (string.IsNullOrEmpty(nome))
                return false;
            string[] lista = new string[] {
                "Coal", "Tar", "Copper", "Tin", "Bronze", "Iron", "Silver",
                "BlackMetal", "Flametal", "Eitr", "LinenThread"
            };
            for (int i = 0; i < lista.Length; i++)
            {
                if (nome == lista[i])
                    return true;
            }
            return false;
        }

        [HarmonyPatch(typeof(Character), "Damage")]
        static class AoGolpe
        {
            static void Postfix(Character __instance, HitData hit)
            {
                if (Inst == null || !Inst.Ativo() || hit == null || __instance == null)
                    return;
                Character atacante = hit.GetAttacker();
                if (atacante == null || atacante != Player.m_localPlayer)
                    return;
                Inst.MarcarGolpe(__instance);
            }
        }

        [HarmonyPatch(typeof(Character), "OnDeath")]
        static class AoMorrer
        {
            static void Postfix(Character __instance)
            {
                if (Inst == null || !Inst.Ativo())
                    return;
                Inst.MarcarMorte(__instance);
            }
        }

        [HarmonyPatch(typeof(Player), "OnDeath")]
        static class AoMorrerEu
        {
            static void Prefix(Player __instance)
            {
                if (Inst == null || !Inst.Ativo() || __instance != Player.m_localPlayer)
                    return;
                Inst.MarcarMinhaMorte(__instance.transform.position);
            }
        }

        [HarmonyPatch(typeof(MineRock), "Damage")]
        static class AoMinerar
        {
            static void Postfix(MineRock __instance, HitData hit)
            {
                MarcarPedra(__instance, hit);
            }
        }

        [HarmonyPatch(typeof(MineRock5), "Damage")]
        static class AoMinerar5
        {
            static void Postfix(MineRock5 __instance, HitData hit)
            {
                MarcarPedra(__instance, hit);
            }
        }

        static void MarcarPedra(Component pedra, HitData hit)
        {
            if (Inst == null || !Inst.Ativo() || pedra == null || hit == null)
                return;
            Character atacante = hit.GetAttacker();
            if (atacante == null || atacante != Player.m_localPlayer)
                return;
            Inst.MarcarMina(pedra.transform.position);
        }

        [HarmonyPatch(typeof(TreeBase), "Damage")]
        static class AoCortar
        {
            static void Postfix(TreeBase __instance, HitData hit)
            {
                MarcarCorteSeMeu(__instance, hit);
            }
        }

        [HarmonyPatch(typeof(TreeLog), "Damage")]
        static class AoCortarTronco
        {
            static void Postfix(TreeLog __instance, HitData hit)
            {
                MarcarCorteSeMeu(__instance, hit);
            }
        }

        [HarmonyPatch(typeof(Destructible), "Damage")]
        static class AoQuebrar
        {
            static void Postfix(Destructible __instance, HitData hit)
            {
                MarcarCorteSeMeu(__instance, hit);
            }
        }

        static void MarcarCorteSeMeu(Component alvo, HitData hit)
        {
            if (Inst == null || !Inst.Ativo() || alvo == null || hit == null)
                return;
            Character atacante = hit.GetAttacker();
            if (atacante == null || atacante != Player.m_localPlayer)
                return;
            Inst.MarcarCorte(alvo.transform.position);
        }

        [HarmonyPatch(typeof(Smelter), "OnEmpty")]
        static class AoEsvaziarForno
        {
            static void Postfix(Smelter __instance, Humanoid user)
            {
                if (Inst == null || !Inst.Ativo() || __instance == null)
                    return;
                if (user == null || user != Player.m_localPlayer)
                    return;
                Inst.MarcarForno(__instance.transform.position);
            }
        }

        [HarmonyPatch(typeof(ItemDrop), "OnPlayerDrop")]
        static class AoLargar
        {
            static void Postfix(ItemDrop __instance)
            {
                if (Inst == null)
                    return;
                Inst.MarcarLargado(__instance);
            }
        }

        [HarmonyPatch(typeof(Humanoid), "Pickup")]
        static class AoPegar
        {
            static int stackAntes;

            static void Prefix(GameObject go)
            {
                stackAntes = 0;
                if (go == null)
                    return;
                ItemDrop drop = go.GetComponent<ItemDrop>();
                if (drop != null && drop.m_itemData != null)
                    stackAntes = drop.m_itemData.m_stack;
            }

            static void Postfix(Humanoid __instance, GameObject go, bool __result)
            {
                if (!__result || Inst == null || !Inst.Ativo())
                    return;
                if (__instance != Player.m_localPlayer || go == null)
                    return;
                ItemDrop drop = go.GetComponent<ItemDrop>();
                if (drop == null || drop.m_itemData == null)
                    return;
                if (Inst.FoiLargado(drop))
                    return;
                if (Inst.VeioDaMinhaMorte(go.transform.position))
                    return;
                int sobrou = drop.m_itemData.m_stack;
                int pegou = stackAntes - sobrou;
                if (pegou < 1)
                    pegou = stackAntes;
                if (pegou < 1)
                    pegou = 1;
                Inst.Entregar(drop.m_itemData, pegou);
            }
        }

        [HarmonyPatch(typeof(Humanoid), "GiveDefaultItems")]
        static class AoKitInicial
        {
            static void Prefix()
            {
                IgnorarPadrao = IgnorarPadrao + 1;
            }

            static void Postfix()
            {
                IgnorarPadrao = IgnorarPadrao - 1;
                if (IgnorarPadrao < 0)
                    IgnorarPadrao = 0;
            }
        }

        [HarmonyPatch(typeof(Humanoid), "PickupPrefab")]
        static class AoPrefab
        {
            static void Postfix(Humanoid __instance, GameObject prefab, int stackSize, ItemDrop.ItemData __result)
            {
                if (__result == null || Inst == null || !Inst.Ativo() || IgnorarPadrao > 0)
                    return;
                if (__instance != Player.m_localPlayer || prefab == null)
                    return;
                int n = stackSize;
                if (n < 1)
                    n = 1;
                Inst.EntregarPrefab(prefab, n);
            }
        }

        [HarmonyPatch(typeof(FishingFloat), "Catch")]
        static class AoPescar
        {
            static void Prefix()
            {
                Pesca = Pesca + 1;
            }

            static void Postfix()
            {
                Pesca = Pesca - 1;
                if (Pesca < 0)
                    Pesca = 0;
            }
        }

        [HarmonyPatch(typeof(Inventory), "AddItem", new System.Type[] { typeof(GameObject), typeof(int) })]
        static class AoExtraDoPeixe
        {
            static void Postfix(Inventory __instance, GameObject prefab, int amount, bool __result)
            {
                if (!__result || Inst == null || !Inst.Ativo() || Pesca <= 0 || Inst.entregando > 0)
                    return;
                if (!Inst.EhBolsaLocal(__instance))
                    return;
                Inst.EntregarPrefab(prefab, amount);
            }
        }

        [HarmonyPatch(typeof(StoreGui), "BuySelectedItem")]
        static class AoComprar
        {
            static int antes;
            static string nome;
            static ItemDrop prefab;
            static int pilha;

            static void Prefix(Trader.TradeItem ___m_selectedItem)
            {
                antes = -1;
                nome = "";
                prefab = null;
                pilha = 0;
                if (___m_selectedItem == null || ___m_selectedItem.m_prefab == null || ___m_selectedItem.m_prefab.m_itemData == null)
                    return;
                if (___m_selectedItem.m_prefab.m_itemData.m_shared == null)
                    return;
                nome = ___m_selectedItem.m_prefab.m_itemData.m_shared.m_name;
                prefab = ___m_selectedItem.m_prefab;
                pilha = ___m_selectedItem.m_stack;
                antes = Contar(nome);
            }

            static void Postfix()
            {
                if (antes < 0 || Inst == null || prefab == null)
                    return;
                int ganhou = Contar(nome) - antes;
                if (ganhou < 1)
                    ganhou = pilha;
                if (ganhou < 1 || Contar(nome) <= antes)
                    return;
                Inst.Entregar(prefab.m_itemData, ganhou);
            }
        }

        [HarmonyPatch(typeof(Inventory), "MoveItemToThis", new System.Type[] { typeof(Inventory), typeof(ItemDrop.ItemData) })]
        static class AoTirarUm
        {
            static ItemDrop.ItemData pendente;
            static int stackPendente;
            static Inventory origemPendente;
            static bool loot;

            static void Prefix(Inventory __instance, Inventory fromInventory, ItemDrop.ItemData item)
            {
                loot = false;
                if (Inst == null || !Inst.Ativo() || item == null)
                    return;
                if (!Inst.EhBolsaLocal(__instance))
                    return;
                if (!Inst.BauDeLoot(fromInventory))
                    return;
                pendente = item;
                stackPendente = item.m_stack;
                origemPendente = fromInventory;
                loot = true;
            }

            static void Postfix()
            {
                if (!loot || Inst == null)
                    return;
                loot = false;
                int movido = stackPendente;
                if (origemPendente != null && pendente != null)
                {
                    List<ItemDrop.ItemData> lista = origemPendente.GetAllItems();
                    for (int i = 0; i < lista.Count; i++)
                    {
                        if (lista[i] == pendente)
                            movido = stackPendente - pendente.m_stack;
                    }
                }
                Inst.Entregar(pendente, movido);
                pendente = null;
                origemPendente = null;
            }
        }

        [HarmonyPatch(typeof(Inventory), "MoveItemToThis", new System.Type[] { typeof(Inventory), typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int) })]
        static class AoTirarParte
        {
            static void Prefix(Inventory __instance, Inventory fromInventory, ref bool __state)
            {
                __state = false;
                if (Inst == null || !Inst.Ativo())
                    return;
                if (!Inst.EhBolsaLocal(__instance))
                    return;
                if (!Inst.BauDeLoot(fromInventory))
                    return;
                __state = true;
            }

            static void Postfix(ItemDrop.ItemData item, int amount, bool __result, bool __state)
            {
                if (!__state || !__result || Inst == null)
                    return;
                Inst.Entregar(item, amount);
            }
        }

        [HarmonyPatch(typeof(Inventory), "MoveAll")]
        static class AoTirarTudo
        {
            class Pilha
            {
                public ItemDrop.ItemData item;
                public int stack;
            }

            static List<Pilha> antes;
            static Inventory origem;

            static void Prefix(Inventory __instance, Inventory fromInventory)
            {
                antes = null;
                if (Inst == null || !Inst.Ativo())
                    return;
                if (!Inst.EhBolsaLocal(__instance))
                    return;
                if (!Inst.BauDeLoot(fromInventory))
                    return;
                origem = fromInventory;
                antes = new List<Pilha>();
                List<ItemDrop.ItemData> lista = fromInventory.GetAllItems();
                for (int i = 0; i < lista.Count; i++)
                {
                    if (lista[i] == null)
                        continue;
                    Pilha pilha = new Pilha();
                    pilha.item = lista[i];
                    pilha.stack = lista[i].m_stack;
                    antes.Add(pilha);
                }
            }

            static void Postfix()
            {
                if (antes == null || Inst == null)
                    return;
                List<Pilha> copia = antes;
                Inventory deOnde = origem;
                antes = null;
                origem = null;
                for (int i = 0; i < copia.Count; i++)
                {
                    int movido = copia[i].stack;
                    if (deOnde != null)
                    {
                        List<ItemDrop.ItemData> lista = deOnde.GetAllItems();
                        for (int j = 0; j < lista.Count; j++)
                        {
                            if (lista[j] == copia[i].item)
                                movido = copia[i].stack - copia[i].item.m_stack;
                        }
                    }
                    Inst.Entregar(copia[i].item, movido);
                }
            }
        }

        [HarmonyPatch(typeof(Pickable), "Interact")]
        static class AoColher
        {
            static bool estava;

            static void Prefix(bool ___m_picked, bool ___m_pickedLocal)
            {
                estava = ___m_picked || ___m_pickedLocal;
            }

            static void Postfix(Pickable __instance, Humanoid character, bool ___m_picked, bool ___m_pickedLocal)
            {
                if (estava || Inst == null || !Inst.Ativo() || __instance == null)
                    return;
                if (character != Player.m_localPlayer)
                    return;
                if (!___m_pickedLocal && !___m_picked)
                    return;
                if (__instance.m_itemPrefab == null)
                    return;
                ItemDrop drop = __instance.m_itemPrefab.GetComponent<ItemDrop>();
                if (drop == null || drop.m_itemData == null)
                    return;
                int quantidade = __instance.m_amount;
                if (quantidade < 1)
                    quantidade = 1;
                Inst.Entregar(drop.m_itemData, quantidade);
            }
        }
    }
}
