using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BepInEx;
using UnityEngine;

namespace AplicaHeranca
{
    [BepInPlugin("yuri.AplicaHeranca", "Aplica Heranca", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        const string Chave = "yuri.funde";
        bool feito;
        float proxima;
        string marca;

        void Awake()
        {
            marca = Path.Combine(Paths.ConfigPath, "yuri.heranca-roupa.txt");
            if (File.Exists(marca) && File.ReadAllText(marca).Trim() == "ok")
                feito = true;
            Logger.LogInfo("Aplica heranca pronta");
        }

        void Update()
        {
            if (UnityEngine.Time.time < proxima)
                return;
            proxima = UnityEngine.Time.time + 2f;
            Player jogador = Player.m_localPlayer;
            if (jogador == null || jogador.GetPlayerName() != "Bjorn Jandel")
                return;
            if (ObjectDB.instance == null)
                return;
            Inventory bolsa = jogador.GetInventory();
            if (bolsa == null)
                return;
            if (feito)
            {
                RefrescarEspada(bolsa);
                return;
            }
            ItemDrop.ItemData raiz = Prefab("ArmorRootChest");
            ItemDrop.ItemData lox = Prefab("CapeLox");
            ItemDrop.ItemData loxPeito = Prefab("ArmorLoxChest");
            ItemDrop.ItemData loxCalca = Prefab("ArmorLoxLegs");
            ItemDrop.ItemData loxElmo = Prefab("HelmetLox");
            ItemDrop.ItemData troll = Prefab("ArmorTrollLeatherChest");
            ItemDrop.ItemData fenris = Prefab("ArmorFenringChest");
            ItemDrop.ItemData prata = Prefab("SwordSilver");
            ItemDrop.ItemData broquel = Prefab("ShieldBlackmetal");
            if (raiz == null || lox == null || troll == null || fenris == null || prata == null || broquel == null)
                return;
            if (loxPeito == null && loxCalca == null && loxElmo == null)
            {
                Logger.LogInfo("Conjunto do Lox nao encontrado");
                return;
            }
            Bonus roupa = new Bonus();
            SomarRoupa(roupa, raiz);
            SomarRoupa(roupa, lox);
            SomarRoupa(roupa, loxPeito);
            SomarRoupa(roupa, loxCalca);
            SomarRoupa(roupa, loxElmo);
            SomarRoupa(roupa, troll);
            SomarRoupa(roupa, fenris);
            string efeitos = roupa.equipar == null ? "" : roupa.equipar.ToLower();
            bool temLox = efeitos.IndexOf("lox") >= 0;
            if (!temLox)
                Logger.LogInfo("Bencao do Lox nao entrou. Efeitos: " + roupa.equipar);
            float espirito = prata.GetDamage().m_spirit;
            float aparar = broquel.m_shared.m_timedBlockBonus;
            float bloqueio = broquel.GetBaseBlockPower();
            float defesa = broquel.GetDeflectionForce();
            string parada = NomeEfeito(broquel.m_shared.m_perfectBlockStatusEffect);
            List<ItemDrop.ItemData> itens = bolsa.GetAllItems();
            int roupas = 0;
            int espadas = 0;
            int escudos = 0;
            for (int i = 0; i < itens.Count; i++)
            {
                ItemDrop.ItemData item = itens[i];
                if (item == null || item.m_shared == null)
                    continue;
                string nome = item.m_shared.m_name;
                GameObject origem = item.m_dropPrefab;
                string prefab = origem == null ? "" : origem.name;
                if (temLox && EhRoupa(item, prefab))
                {
                    Bonus junto = Juntar(Ler(item), roupa);
                    Gravar(item, junto);
                    roupas++;
                }
                else if (prefab == "SwordIron" || nome == "$item_sword_iron" || prefab == "SwordBlackmetal" || nome == "$item_sword_blackmetal")
                {
                    Bonus espada = Ler(item);
                    if (espirito > espada.espirito)
                        espada.espirito = espirito;
                    ItemDrop.ItemData naBolsa = Achar(itens, "SwordSilver", "$item_sword_silver");
                    if (naBolsa != null && naBolsa.GetDamage().m_spirit > espada.espirito)
                        espada.espirito = naBolsa.GetDamage().m_spirit;
                    Gravar(item, espada);
                    espadas++;
                }
                else if (prefab == "ShieldBlackmetalTower" || nome == "$item_shield_blackmetal_tower")
                {
                    Bonus escudo = Ler(item);
                    if (aparar > escudo.aparar)
                        escudo.aparar = aparar;
                    if (bloqueio > escudo.bloqueio)
                        escudo.bloqueio = bloqueio;
                    float torre = item.GetBaseBlockPower();
                    if (torre > escudo.bloqueio)
                        escudo.bloqueio = torre;
                    if (defesa > escudo.defesa)
                        escudo.defesa = defesa;
                    escudo.parada = Acrescentar(escudo.parada, parada);
                    ItemDrop.ItemData menor = Achar(itens, "ShieldBlackmetal", "$item_shield_blackmetal");
                    if (menor != null && menor.m_shared != null && menor.m_shared.m_timedBlockBonus > escudo.aparar)
                        escudo.aparar = menor.m_shared.m_timedBlockBonus;
                    Gravar(item, escudo);
                    escudos++;
                }
            }
            if (roupas < 1 || espadas < 1 || escudos < 1)
            {
                Logger.LogInfo("Ainda falta peca. Roupa " + roupas + " espada " + espadas + " escudo " + escudos);
                return;
            }
            File.WriteAllText(marca, "ok");
            feito = true;
            RefrescarEspada(bolsa);
            jogador.Message(MessageHud.MessageType.Center, "Efeitos colocados na roupa, na espada e no escudo.", 0, null, true);
            Logger.LogInfo("Heranca aplicada");
        }

        static void RefrescarEspada(Inventory bolsa)
        {
            ItemDrop.ItemData prata = Prefab("SwordSilver");
            if (prata == null)
                return;
            float espirito = prata.GetDamage().m_spirit;
            List<ItemDrop.ItemData> itens = bolsa.GetAllItems();
            ItemDrop.ItemData naBolsa = Achar(itens, "SwordSilver", "$item_sword_silver");
            if (naBolsa != null && naBolsa.GetDamage().m_spirit > espirito)
                espirito = naBolsa.GetDamage().m_spirit;
            if (espirito <= 0.1f)
                return;
            for (int i = 0; i < itens.Count; i++)
            {
                ItemDrop.ItemData item = itens[i];
                if (item == null || item.m_shared == null)
                    continue;
                string nome = item.m_shared.m_name;
                string prefab = item.m_dropPrefab == null ? "" : item.m_dropPrefab.name;
                if (prefab != "SwordBlackmetal" && nome != "$item_sword_blackmetal" && prefab != "SwordIron" && nome != "$item_sword_iron")
                    continue;
                Bonus espada = Ler(item);
                if (espada.espirito >= espirito)
                    continue;
                espada.espirito = espirito;
                Gravar(item, espada);
            }
        }

        static bool EhRoupa(ItemDrop.ItemData item, string prefab)
        {
            if (prefab == "HelmetPadded" || prefab == "ArmorPaddedCuirass" || prefab == "ArmorPaddedGreaves")
                return true;
            if (item.m_shared.m_name == "$item_helmet_padded" || item.m_shared.m_name == "$item_chest_padded" || item.m_shared.m_name == "$item_legs_padded")
                return true;
            if (!item.m_equipped)
                return false;
            ItemDrop.ItemData.ItemType tipo = item.m_shared.m_itemType;
            if (tipo == ItemDrop.ItemData.ItemType.Helmet || tipo == ItemDrop.ItemData.ItemType.Chest || tipo == ItemDrop.ItemData.ItemType.Legs || tipo == ItemDrop.ItemData.ItemType.Hands || tipo == ItemDrop.ItemData.ItemType.Shoulder)
                return true;
            return false;
        }

        static ItemDrop.ItemData Achar(List<ItemDrop.ItemData> itens, string prefab, string nome)
        {
            for (int i = 0; i < itens.Count; i++)
            {
                ItemDrop.ItemData item = itens[i];
                if (item == null || item.m_shared == null)
                    continue;
                if (item.m_dropPrefab != null && item.m_dropPrefab.name == prefab)
                    return item;
                if (item.m_shared.m_name == nome)
                    return item;
            }
            return null;
        }

        static ItemDrop.ItemData Prefab(string nome)
        {
            GameObject go = ObjectDB.instance.GetItemPrefab(nome);
            if (go == null)
                return null;
            ItemDrop drop = go.GetComponent<ItemDrop>();
            if (drop == null)
                return null;
            return drop.m_itemData;
        }

        static void SomarRoupa(Bonus bonus, ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null)
                return;
            if (item.m_shared.m_damageModifiers != null)
            {
                for (int i = 0; i < item.m_shared.m_damageModifiers.Count; i++)
                {
                    HitData.DamageModPair par = item.m_shared.m_damageModifiers[i];
                    if (Forca(par.m_modifier) >= 3)
                        bonus.resistencias = PorRes(bonus.resistencias, par.m_type, par.m_modifier);
                }
            }
            bonus.equipar = Acrescentar(bonus.equipar, NomeEfeito(item.m_shared.m_equipStatusEffect));
            bonus.equipar = Acrescentar(bonus.equipar, NomeEfeito(item.m_shared.m_setStatusEffect));
        }

        class Bonus
        {
            public float fogo;
            public float gelo;
            public float raio;
            public float veneno;
            public float espirito;
            public string resistencias;
            public string equipar;
            public string ataque;
            public float bloqueio;
            public float defesa;
            public float aparar;
            public string parada;
        }

        static Bonus Ler(ItemDrop.ItemData item)
        {
            Bonus bonus = new Bonus();
            if (item == null || item.m_customData == null)
                return bonus;
            string texto;
            if (!item.m_customData.TryGetValue(Chave, out texto) || texto == null)
                return bonus;
            string[] partes = texto.Split('|');
            if (partes.Length < 8)
                return bonus;
            bonus.fogo = Numero(partes[0]);
            bonus.gelo = Numero(partes[1]);
            bonus.raio = Numero(partes[2]);
            bonus.veneno = Numero(partes[3]);
            bonus.espirito = Numero(partes[4]);
            bonus.resistencias = partes[5];
            bonus.equipar = partes[6];
            bonus.ataque = partes[7];
            if (partes.Length >= 12)
            {
                bonus.bloqueio = Numero(partes[8]);
                bonus.defesa = Numero(partes[9]);
                bonus.aparar = Numero(partes[10]);
                bonus.parada = partes[11];
            }
            return bonus;
        }

        static Bonus Juntar(Bonus ja, Bonus extra)
        {
            Bonus junto = new Bonus();
            junto.fogo = Maior(ja.fogo, extra.fogo);
            junto.gelo = Maior(ja.gelo, extra.gelo);
            junto.raio = Maior(ja.raio, extra.raio);
            junto.veneno = Maior(ja.veneno, extra.veneno);
            junto.espirito = Maior(ja.espirito, extra.espirito);
            junto.resistencias = JuntarRes(ja.resistencias, extra.resistencias);
            junto.equipar = Acrescentar(ja.equipar, extra.equipar);
            junto.ataque = Acrescentar(ja.ataque, extra.ataque);
            junto.bloqueio = Maior(ja.bloqueio, extra.bloqueio);
            junto.defesa = Maior(ja.defesa, extra.defesa);
            junto.aparar = Maior(ja.aparar, extra.aparar);
            junto.parada = Acrescentar(ja.parada, extra.parada);
            return junto;
        }

        static void Gravar(ItemDrop.ItemData item, Bonus bonus)
        {
            Dictionary<string, string> copia = new Dictionary<string, string>();
            if (item.m_customData != null)
            {
                foreach (KeyValuePair<string, string> par in item.m_customData)
                    copia[par.Key] = par.Value;
            }
            copia[Chave] = Num(bonus.fogo) + "|" + Num(bonus.gelo) + "|" + Num(bonus.raio) + "|" + Num(bonus.veneno) + "|" + Num(bonus.espirito) + "|" + Limpo(bonus.resistencias) + "|" + Limpo(bonus.equipar) + "|" + Limpo(bonus.ataque) + "|" + Num(bonus.bloqueio) + "|" + Num(bonus.defesa) + "|" + Num(bonus.aparar) + "|" + Limpo(bonus.parada);
            item.m_customData = copia;
        }

        static string NomeEfeito(StatusEffect efeito)
        {
            if (efeito == null)
                return "";
            string chave = efeito.m_name == null ? "" : efeito.m_name;
            string arquivo = efeito.name == null ? "" : efeito.name;
            if (chave != "" && arquivo != "" && chave != arquivo)
                return chave + "," + arquivo;
            if (chave != "")
                return chave;
            return arquivo;
        }

        static string Acrescentar(string lista, string nomes)
        {
            string saida = lista == null ? "" : lista;
            if (nomes == null || nomes == "")
                return saida;
            string[] partes = nomes.Split(',');
            for (int i = 0; i < partes.Length; i++)
            {
                string nome = partes[i];
                if (nome == "" || Contem(saida, nome))
                    continue;
                if (saida != "")
                    saida = saida + ",";
                saida = saida + nome;
            }
            return saida;
        }

        static bool Contem(string lista, string nome)
        {
            if (lista == null || lista == "")
                return false;
            string[] partes = lista.Split(',');
            for (int i = 0; i < partes.Length; i++)
            {
                if (partes[i] == nome)
                    return true;
            }
            return false;
        }

        static string JuntarRes(string a, string b)
        {
            string junto = a == null ? "" : a;
            string[] partes = (b == null ? "" : b).Split(',');
            for (int i = 0; i < partes.Length; i++)
            {
                string[] kv = partes[i].Split('=');
                if (kv.Length != 2 || kv[0] == "")
                    continue;
                try
                {
                    HitData.DamageType tipo = (HitData.DamageType)System.Enum.Parse(typeof(HitData.DamageType), kv[0]);
                    HitData.DamageModifier mod = (HitData.DamageModifier)System.Enum.Parse(typeof(HitData.DamageModifier), kv[1]);
                    junto = PorRes(junto, tipo, mod);
                }
                catch
                {
                }
            }
            return junto;
        }

        static string PorRes(string atual, HitData.DamageType tipo, HitData.DamageModifier mod)
        {
            string nome = tipo.ToString();
            string melhor = mod.ToString();
            string[] partes = (atual == null ? "" : atual).Split(',');
            string saida = "";
            bool achou = false;
            for (int i = 0; i < partes.Length; i++)
            {
                string[] kv = partes[i].Split('=');
                if (kv.Length != 2 || kv[0] == "")
                    continue;
                string usar = partes[i];
                if (kv[0] == nome)
                {
                    achou = true;
                    usar = nome + "=" + melhor;
                }
                if (saida != "")
                    saida = saida + ",";
                saida = saida + usar;
            }
            if (!achou)
            {
                if (saida != "")
                    saida = saida + ",";
                saida = saida + nome + "=" + melhor;
            }
            return saida;
        }

        static int Forca(HitData.DamageModifier mod)
        {
            if (mod == HitData.DamageModifier.Immune)
                return 6;
            if (mod == HitData.DamageModifier.VeryResistant)
                return 5;
            if (mod == HitData.DamageModifier.Resistant)
                return 4;
            if (mod == HitData.DamageModifier.SlightlyResistant)
                return 3;
            return 0;
        }

        static float Maior(float a, float b)
        {
            if (a > b)
                return a;
            return b;
        }

        static float Numero(string texto)
        {
            float valor;
            if (float.TryParse(texto, NumberStyles.Float, CultureInfo.InvariantCulture, out valor))
                return valor;
            return 0f;
        }

        static string Num(float valor)
        {
            return valor.ToString(CultureInfo.InvariantCulture);
        }

        static string Limpo(string texto)
        {
            if (texto == null)
                return "";
            return texto;
        }
    }
}
