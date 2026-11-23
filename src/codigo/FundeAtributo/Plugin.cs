using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace FundeAtributo
{
    [BepInPlugin("yuri.HerancaDeAtributos", "Herança de Atributos", "1.0.17")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Inst;
        public static bool Aberto;
        static int quadroSemPausa;
        public static List<string> EfeitosLigados = new List<string>();

        const string Chave = "yuri.funde";

        ConfigEntry<bool> Ligado;
        ConfigEntry<string> Tecla;
        ConfigEntry<int> Largura;
        ConfigEntry<int> Altura;
        public static System.Reflection.FieldInfo CampoMelhoria;
        public static Dictionary<string, string> DadosMelhoria;
        public static string NomeMelhoria;
        public static string PrefabMelhoria;
        public static int DentroMelhoria;
        ItemDrop.ItemData origem;
        ItemDrop.ItemData destino;
        Vector2 rolagem;

        void Awake()
        {
            Inst = this;
            Ligado = Config.Bind("Geral", "Ligado", true, "Escudo com escudo: o que fica guarda o aparar, a adrenalina de aparo e a defesa mais alta. Roupa passa o efeito. So no seu jogo.");
            Tecla = Config.Bind("Geral", "Tecla", "F7", "Tecla que abre a janela.");
            Largura = Config.Bind("Geral", "Eixo X", LerNumero("Largura", 552), new ConfigDescription("Largura da janela do F7.", new AcceptableValueRange<int>(360, 1600)));
            Altura = Config.Bind("Geral", "Eixo Y", LerNumero("Altura", 700), new ConfigDescription("Altura da janela do F7.", new AcceptableValueRange<int>(360, 1400)));
            CampoMelhoria = typeof(InventoryGui).GetField("m_craftUpgradeItem", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Harmony harmonia = new Harmony("yuri.HerancaDeAtributos");
            harmonia.PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo("Herança de Atributos 1.0.17");
        }

        int LerNumero(string chave, int padrao)
        {
            try
            {
                string caminho = Config.ConfigFilePath;
                if (!System.IO.File.Exists(caminho))
                    return padrao;
                string[] linhas = System.IO.File.ReadAllLines(caminho);
                string prefixo = chave + " = ";
                for (int i = 0; i < linhas.Length; i++)
                {
                    string linha = linhas[i].Trim();
                    if (!linha.StartsWith(prefixo))
                        continue;
                    int n;
                    if (int.TryParse(linha.Substring(prefixo.Length).Trim(), out n))
                        return n;
                }
            }
            catch
            {
            }
            return padrao;
        }

        void Update()
        {
            if (!Ligado.Value || Player.m_localPlayer == null)
            {
                if (Aberto)
                    Aberto = false;
                return;
            }
            if (Input.GetKeyDown(LerTecla()))
                Aberto = !Aberto;
            if (Aberto && Input.GetKeyDown(KeyCode.Escape))
            {
                Aberto = false;
                quadroSemPausa = Time.frameCount;
            }
            PassarConjuntos(Player.m_localPlayer);
            ManterEfeitos(Player.m_localPlayer);
        }

        static string conjuntosAssinatura;

        static void PassarConjuntos(Player jogador)
        {
            if (jogador == null || jogador.GetInventory() == null)
                return;
            List<ItemDrop.ItemData> todos = jogador.GetInventory().GetAllItems();
            if (todos == null || todos.Count == 0)
                return;
            string agora = "";
            int destino = 0;
            for (int i = 0; i < todos.Count; i++)
            {
                ItemDrop.ItemData item = todos[i];
                if (item == null || !item.m_equipped || Familia(item) != 3)
                    continue;
                agora = agora + "|" + item.m_shared.m_name + ":" + item.m_quality.ToString();
                destino++;
            }
            if (destino == 0 || agora == conjuntosAssinatura)
                return;
            Bonus extra = new Bonus();
            int fontes = 0;
            for (int i = 0; i < todos.Count; i++)
            {
                ItemDrop.ItemData item = todos[i];
                if (!EhConjuntoPedido(item))
                    continue;
                extra = Juntar(extra, LerDoItem(item));
                fontes++;
            }
            if (fontes == 0)
                return;
            bool mudou = false;
            for (int i = 0; i < todos.Count; i++)
            {
                ItemDrop.ItemData item = todos[i];
                if (item == null || !item.m_equipped || Familia(item) != 3)
                    continue;
                Bonus ja = LerGravado(item);
                Bonus junto = Juntar(ja, extra);
                if (junto.equipar != ja.equipar || junto.resistencias != ja.resistencias || Math.Abs(junto.vigorCasa - ja.vigorCasa) > 0.001f)
                    mudou = true;
                Gravar(item, junto);
            }
            conjuntosAssinatura = agora;
            if (mudou)
                Avisar(jogador, "Os conjuntos foram para a armadura que você está usando.");
        }

        static bool EhConjuntoPedido(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null || item.m_shared.m_name == null)
                return false;
            string nome = item.m_shared.m_name;
            if (nome == "$item_chest_berserker_undead")
                return true;
            if (nome == "$item_helmet_berserker")
                return true;
            if (nome == "$item_helmet_celebration")
                return true;
            if (nome == "$item_helmet_fishinghat")
                return true;
            return false;
        }

        KeyCode LerTecla()
        {
            try
            {
                return (KeyCode)Enum.Parse(typeof(KeyCode), Tecla.Value);
            }
            catch
            {
                return KeyCode.F7;
            }
        }

        static Texture2D fundo;
        static GUIStyle titulo;
        static GUIStyle texto;

        static Texture2D Fundo()
        {
            if (fundo != null)
                return fundo;
            fundo = new Texture2D(1, 1);
            fundo.SetPixel(0, 0, new Color(0.06f, 0.06f, 0.06f, 0.96f));
            fundo.Apply();
            return fundo;
        }

        static void GarantirEstilo()
        {
            if (texto != null)
                return;
            texto = new GUIStyle(GUI.skin.label);
            texto.fontSize = 16;
            texto.wordWrap = true;
            texto.richText = false;
            texto.normal.textColor = Color.white;
            texto.padding = new RectOffset(0, 0, 2, 8);
            titulo = new GUIStyle(texto);
            titulo.fontSize = 22;
            titulo.padding = new RectOffset(0, 0, 0, 10);
        }

        static void Linha(string frase, GUIStyle estilo, float largura)
        {
            float altura = estilo.CalcHeight(new GUIContent(frase), largura);
            if (altura < estilo.fontSize + 8f)
                altura = estilo.fontSize + 8f;
            GUILayout.Label(frase, estilo, GUILayout.Width(largura), GUILayout.Height(altura));
        }

        void OnGUI()
        {
            if (!Aberto || !Ligado.Value || Player.m_localPlayer == null)
                return;
            GarantirEstilo();
            float caixaL = Largura.Value;
            float caixaA = Altura.Value;
            if (caixaL < 360f)
                caixaL = 360f;
            if (caixaA < 360f)
                caixaA = 360f;
            float largura = caixaL - 32f;
            float lista = caixaA - 380f;
            if (lista < 70f)
                lista = 70f;
            Rect caixa = new Rect(20f, 20f, caixaL, caixaA);
            GUI.DrawTexture(caixa, Fundo());
            GUILayout.BeginArea(new Rect(caixa.x + 16f, caixa.y + 14f, largura, caixa.height - 28f));
            Linha("Herança de Atributos", titulo, largura);
            Linha("Elmo, peitoral, calça, capa e luva passam o efeito entre si.", texto, largura);
            Linha("Escudo: primeiro o que some, depois o que fica. O que fica guarda o aparar, a adrenalina de aparo e a defesa mais alta.", texto, largura);
            Linha("Com a janela aberta o personagem não anda, não olha e não bate.", texto, largura);
            List<ItemDrop.ItemData> itens = Itens(Player.m_localPlayer);
            rolagem = GUILayout.BeginScrollView(rolagem, GUILayout.Height(lista));
            for (int i = 0; i < itens.Count; i++)
            {
                ItemDrop.ItemData item = itens[i];
                string marca = "";
                if (item == origem)
                    marca = "  [doa]";
                else if (item == destino)
                    marca = "  [recebe]";
                if (GUILayout.Button(Nome(item) + marca))
                    Escolher(item);
            }
            GUILayout.EndScrollView();
            Linha(Resumo(), texto, largura);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Fundir", GUILayout.Height(34f)))
                Fundir(Player.m_localPlayer);
            if (GUILayout.Button("Limpar", GUILayout.Height(34f)))
            {
                origem = null;
                destino = null;
            }
            if (GUILayout.Button("Fechar", GUILayout.Height(34f)))
                Aberto = false;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void Escolher(ItemDrop.ItemData item)
        {
            if (origem == null || origem == item)
            {
                origem = item;
                if (destino == item)
                    destino = null;
                return;
            }
            destino = item;
        }

        string Resumo()
        {
            if (origem == null)
                return "Nenhuma peça doando.";
            if (destino == null)
                return "Doa: " + Nome(origem) + ". Escolhe quem recebe.";
            return "Doa: " + Nome(origem) + "  ->  Recebe: " + Nome(destino) + "\n" + TextoPassa(origem);
        }

        void Fundir(Player jogador)
        {
            if (origem == null || destino == null || origem == destino)
            {
                Avisar(jogador, "Escolhe duas peças diferentes.");
                return;
            }
            if (!jogador.GetInventory().ContainsItem(origem) || !jogador.GetInventory().ContainsItem(destino))
            {
                Avisar(jogador, "A peça não está mais na bolsa.");
                origem = null;
                destino = null;
                return;
            }
            if (Familia(origem) == 0 || Familia(origem) != Familia(destino))
            {
                Avisar(jogador, "Só arma com arma, roupa com roupa, e escudo com escudo.");
                return;
            }
            Bonus extra = LerDoItem(origem);
            if (!extra.Tem())
            {
                Avisar(jogador, "Essa peça não tem atributo pra passar.");
                return;
            }
            Bonus ja = LerGravado(destino);
            if (Familia(destino) == 2)
                ja = Juntar(ja, LerDoItem(destino));
            Bonus junto = Juntar(ja, extra);
            if (origem.m_equipped)
                jogador.UnequipItem(origem, false);
            if (!jogador.GetInventory().RemoveOneItem(origem))
            {
                Avisar(jogador, "Não deu pra tirar a peça que doa.");
                return;
            }
            Gravar(destino, junto);
            if (Familia(destino) == 2)
                Avisar(jogador, "O escudo que ficou guarda o aparar, a adrenalina de aparo e a defesa mais alta.");
            else
                Avisar(jogador, "Atributo passado pra " + Nome(destino) + ".");
            origem = null;
            destino = null;
            ManterEfeitos(jogador);
        }

        static void Avisar(Player jogador, string texto)
        {
            jogador.Message(MessageHud.MessageType.Center, texto, 0, null, true);
        }

        static int Familia(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null)
                return 0;
            ItemDrop.ItemData.ItemType tipo = item.m_shared.m_itemType;
            if (tipo == ItemDrop.ItemData.ItemType.OneHandedWeapon || tipo == ItemDrop.ItemData.ItemType.TwoHandedWeapon || tipo == ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft || tipo == ItemDrop.ItemData.ItemType.Bow || tipo == ItemDrop.ItemData.ItemType.Torch)
                return 1;
            if (tipo == ItemDrop.ItemData.ItemType.Shield)
                return 2;
            if (tipo == ItemDrop.ItemData.ItemType.Helmet || tipo == ItemDrop.ItemData.ItemType.Chest || tipo == ItemDrop.ItemData.ItemType.Legs || tipo == ItemDrop.ItemData.ItemType.Hands || tipo == ItemDrop.ItemData.ItemType.Shoulder)
                return 3;
            return 0;
        }

        static List<ItemDrop.ItemData> Itens(Player jogador)
        {
            List<ItemDrop.ItemData> todos = jogador.GetInventory().GetAllItems();
            List<ItemDrop.ItemData> lista = new List<ItemDrop.ItemData>();
            for (int i = 0; i < todos.Count; i++)
            {
                if (Familia(todos[i]) != 0)
                    lista.Add(todos[i]);
            }
            return lista;
        }

        static string Nome(ItemDrop.ItemData item)
        {
            string bruto = item.m_shared.m_name;
            string bonito = bruto;
            if (Localization.instance != null)
                bonito = Localization.instance.Localize(bruto);
            return bonito + "  q" + item.m_quality.ToString();
        }

        static Bonus LerDoItem(ItemDrop.ItemData item)
        {
            Bonus bonus = LerGravado(item);
            HitData.DamageTypes dano = item.GetDamage();
            bonus.fogo = Maior(bonus.fogo, dano.m_fire);
            bonus.gelo = Maior(bonus.gelo, dano.m_frost);
            bonus.raio = Maior(bonus.raio, dano.m_lightning);
            bonus.veneno = Maior(bonus.veneno, dano.m_poison);
            bonus.espirito = Maior(bonus.espirito, dano.m_spirit);
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
            if (item.m_shared.m_homeItemsStaminaModifier < 0f)
                bonus.vigorCasa = MaisUtil(bonus.vigorCasa, item.m_shared.m_homeItemsStaminaModifier);
            bonus.ataque = Acrescentar(bonus.ataque, NomeEfeito(item.m_shared.m_attackStatusEffect));
            bonus.bloqueio = Maior(bonus.bloqueio, item.GetBaseBlockPower());
            bonus.defesa = Maior(bonus.defesa, item.GetDeflectionForce());
            bonus.aparar = Maior(bonus.aparar, item.m_shared.m_timedBlockBonus);
            bonus.adrenalina = Maior(bonus.adrenalina, item.m_shared.m_perfectBlockAdrenaline);
            bonus.parada = Acrescentar(bonus.parada, NomeEfeito(item.m_shared.m_perfectBlockStatusEffect));
            return bonus;
        }

        static float MaisUtil(float a, float b)
        {
            if (a >= 0f)
                return b < 0f ? b : a;
            if (b >= 0f)
                return a;
            if (a < b)
                return a;
            return b;
        }

        static float Maior(float a, float b)
        {
            if (a > b)
                return a;
            return b;
        }

        static Bonus Juntar(Bonus ja, Bonus extra)
        {
            Bonus junto = new Bonus();
            junto.fogo = Maior(ja.fogo, extra.fogo);
            junto.gelo = Maior(ja.gelo, extra.gelo);
            junto.raio = Maior(ja.raio, extra.raio);
            junto.veneno = Maior(ja.veneno, extra.veneno);
            junto.espirito = Maior(ja.espirito, extra.espirito);
            junto.resistencias = MesclarRes(ja.resistencias, extra.resistencias);
            junto.equipar = JuntarNomes(ja.equipar, extra.equipar);
            junto.ataque = JuntarNomes(ja.ataque, extra.ataque);
            junto.bloqueio = Maior(ja.bloqueio, extra.bloqueio);
            junto.defesa = Maior(ja.defesa, extra.defesa);
            junto.aparar = Maior(ja.aparar, extra.aparar);
            junto.adrenalina = Maior(ja.adrenalina, extra.adrenalina);
            junto.parada = JuntarNomes(ja.parada, extra.parada);
            junto.vigorCasa = MaisUtil(ja.vigorCasa, extra.vigorCasa);
            return junto;
        }

        static string MesclarRes(string a, string b)
        {
            string junto = a;
            string[] partes = (b == null ? "" : b).Split(',');
            for (int i = 0; i < partes.Length; i++)
            {
                string[] kv = partes[i].Split('=');
                if (kv.Length != 2 || kv[0] == "")
                    continue;
                try
                {
                    HitData.DamageType tipo = (HitData.DamageType)Enum.Parse(typeof(HitData.DamageType), kv[0]);
                    HitData.DamageModifier mod = (HitData.DamageModifier)Enum.Parse(typeof(HitData.DamageModifier), kv[1]);
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
            if (Forca(mod) < 3)
                return atual;
            string nome = tipo.ToString();
            string melhor = mod.ToString();
            int forcaNova = Forca(mod);
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
                    try
                    {
                        HitData.DamageModifier velho = (HitData.DamageModifier)Enum.Parse(typeof(HitData.DamageModifier), kv[1]);
                        if (forcaNova > Forca(velho))
                            usar = nome + "=" + melhor;
                    }
                    catch
                    {
                        usar = nome + "=" + melhor;
                    }
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
            if (mod == HitData.DamageModifier.Ignore)
                return 2;
            if (mod == HitData.DamageModifier.Normal)
                return 1;
            if (mod == HitData.DamageModifier.SlightlyWeak)
                return 0;
            if (mod == HitData.DamageModifier.Weak)
                return -1;
            return -2;
        }

        static void Gravar(ItemDrop.ItemData item, Bonus bonus)
        {
            if (item.m_customData == null)
                item.m_customData = new Dictionary<string, string>();
            item.m_customData[Chave] = Num(bonus.fogo) + "|" + Num(bonus.gelo) + "|" + Num(bonus.raio) + "|" + Num(bonus.veneno) + "|" + Num(bonus.espirito) + "|" + Limpo(bonus.resistencias) + "|" + Limpo(bonus.equipar) + "|" + Limpo(bonus.ataque) + "|" + Num(bonus.bloqueio) + "|" + Num(bonus.defesa) + "|" + Num(bonus.aparar) + "|" + Limpo(bonus.parada) + "|" + Num(bonus.adrenalina) + "|" + Num(bonus.vigorCasa);
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

        public static Bonus LerGravado(ItemDrop.ItemData item)
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
            if (partes.Length >= 13)
                bonus.adrenalina = Numero(partes[12]);
            if (partes.Length >= 14)
                bonus.vigorCasa = Numero(partes[13]);
            return bonus;
        }

        static float Numero(string texto)
        {
            float valor;
            if (float.TryParse(texto, NumberStyles.Float, CultureInfo.InvariantCulture, out valor))
                return valor;
            return 0f;
        }

        static string TextoPassa(ItemDrop.ItemData item)
        {
            Bonus bonus = LerDoItem(item);
            if (!bonus.Tem())
                return "Nada pra passar.";
            return "Passa: " + bonus.Texto();
        }

        static void ManterEfeitos(Player jogador)
        {
            List<string> quer = new List<string>();
            List<ItemDrop.ItemData> equipados = jogador.GetInventory().GetEquippedItems();
            for (int i = 0; i < equipados.Count; i++)
            {
                Bonus bonus = LerGravado(equipados[i]);
                AdicionarNomes(quer, bonus.equipar);
            }
            SEMan mana = jogador.GetSEMan();
            if (mana == null)
                return;
            for (int i = EfeitosLigados.Count - 1; i >= 0; i--)
            {
                if (quer.Contains(EfeitosLigados[i]))
                    continue;
                if (EfeitoNativo(jogador, EfeitosLigados[i]))
                    continue;
                StatusEffect velho = AcharEfeito(EfeitosLigados[i]);
                if (velho != null)
                    mana.RemoveStatusEffect(velho, true);
                EfeitosLigados.RemoveAt(i);
            }
            for (int i = 0; i < quer.Count; i++)
            {
                StatusEffect efeito = AcharEfeito(quer[i]);
                if (efeito == null)
                    continue;
                if (!mana.HaveStatusEffect(efeito))
                    mana.AddStatusEffect(efeito, false, 1, 0f, 0);
                if (!EfeitosLigados.Contains(quer[i]))
                    EfeitosLigados.Add(quer[i]);
            }
        }

        static bool EfeitoNativo(Player jogador, string nome)
        {
            if (jogador == null || jogador.GetInventory() == null || nome == null || nome == "")
                return false;
            List<ItemDrop.ItemData> equipados = jogador.GetInventory().GetEquippedItems();
            for (int i = 0; i < equipados.Count; i++)
            {
                ItemDrop.ItemData item = equipados[i];
                if (item == null || item.m_shared == null)
                    continue;
                if (EfeitoEh(item.m_shared.m_equipStatusEffect, nome))
                    return true;
                if (EfeitoEh(item.m_shared.m_setStatusEffect, nome))
                    return true;
            }
            return false;
        }

        static bool EfeitoEh(StatusEffect efeito, string nome)
        {
            if (efeito == null || nome == null || nome == "")
                return false;
            if (efeito.m_name == nome)
                return true;
            if (efeito.name == nome)
                return true;
            return false;
        }

        static string NomeRes(string res)
        {
            string texto = "";
            string[] partes = res.Split(',');
            for (int i = 0; i < partes.Length; i++)
            {
                string[] kv = partes[i].Split('=');
                if (kv.Length != 2 || kv[0] == "")
                    continue;
                string nome = kv[0];
                if (nome == "Pierce")
                    nome = "perfuração";
                else if (nome == "Slash")
                    nome = "corte";
                else if (nome == "Blunt")
                    nome = "contusão";
                else if (nome == "Fire")
                    nome = "fogo";
                else if (nome == "Frost")
                    nome = "gelo";
                else if (nome == "Lightning")
                    nome = "raio";
                else if (nome == "Poison")
                    nome = "veneno";
                else if (nome == "Spirit")
                    nome = "espírito";
                if (texto != "")
                    texto = texto + ", ";
                texto = texto + nome;
            }
            if (texto == "")
                return "resistência";
            return "resistência a " + texto;
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

        static string JuntarNomes(string a, string b)
        {
            return Acrescentar(a, b);
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
                if (nome == "" || ContemNome(saida, nome))
                    continue;
                if (saida != "")
                    saida = saida + ",";
                saida = saida + nome;
            }
            return saida;
        }

        static bool ContemNome(string lista, string nome)
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

        static void AdicionarNomes(List<string> quer, string nomes)
        {
            if (nomes == null || nomes == "")
                return;
            string[] partes = nomes.Split(',');
            for (int i = 0; i < partes.Length; i++)
            {
                if (partes[i] != "" && !quer.Contains(partes[i]))
                    quer.Add(partes[i]);
            }
        }

        static string DescreverEfeitos(string nomes)
        {
            bool furto = false;
            bool outro = false;
            string[] partes = nomes.Split(',');
            for (int i = 0; i < partes.Length; i++)
            {
                if (partes[i] == "")
                    continue;
                if (EhFurtividade(partes[i]))
                    furto = true;
                else
                    outro = true;
            }
            string texto = "";
            if (furto)
                texto = "furtividade";
            if (outro)
            {
                if (texto != "")
                    texto = texto + ", ";
                texto = texto + "efeito herdado";
            }
            return texto;
        }

        static bool EhFurtividade(string nome)
        {
            string baixo = nome.ToLower();
            if (baixo.IndexOf("sneak") >= 0 || baixo.IndexOf("troll") >= 0 || baixo.IndexOf("stealth") >= 0 || baixo.IndexOf("furt") >= 0)
                return true;
            StatusEffect efeito = AcharEfeito(nome);
            SE_Stats stats = efeito as SE_Stats;
            if (stats == null)
                return false;
            if (stats.m_stealthModifier > 0.01f)
                return true;
            if (stats.m_skillLevel == Skills.SkillType.Sneak || stats.m_skillLevel2 == Skills.SkillType.Sneak)
                return true;
            return false;
        }

        public static StatusEffect AcharEfeito(string nome)
        {
            if (nome == null || nome == "" || ObjectDB.instance == null || ObjectDB.instance.m_StatusEffects == null)
                return null;
            List<StatusEffect> lista = ObjectDB.instance.m_StatusEffects;
            for (int i = 0; i < lista.Count; i++)
            {
                StatusEffect efeito = lista[i];
                if (efeito == null)
                    continue;
                if (efeito.m_name == nome)
                    return efeito;
                if (efeito.name == nome)
                    return efeito;
            }
            return null;
        }

        public static void AplicarDano(ref HitData.DamageTypes dano, Bonus bonus)
        {
            if (bonus.fogo > dano.m_fire)
                dano.m_fire = bonus.fogo;
            if (bonus.gelo > dano.m_frost)
                dano.m_frost = bonus.gelo;
            if (bonus.raio > dano.m_lightning)
                dano.m_lightning = bonus.raio;
            if (bonus.veneno > dano.m_poison)
                dano.m_poison = bonus.veneno;
            if (bonus.espirito > dano.m_spirit)
                dano.m_spirit = bonus.espirito;
        }

        public static void AplicarRes(ref HitData.DamageModifiers mods, string res)
        {
            if (res == null || res == "")
                return;
            string[] partes = res.Split(',');
            for (int i = 0; i < partes.Length; i++)
            {
                string[] kv = partes[i].Split('=');
                if (kv.Length != 2)
                    continue;
                try
                {
                    HitData.DamageType tipo = (HitData.DamageType)Enum.Parse(typeof(HitData.DamageType), kv[0]);
                    HitData.DamageModifier mod = (HitData.DamageModifier)Enum.Parse(typeof(HitData.DamageModifier), kv[1]);
                    if (Forca(mod) > Forca(LerMod(mods, tipo)))
                        EscreverMod(ref mods, tipo, mod);
                }
                catch
                {
                }
            }
        }

        static HitData.DamageModifier LerMod(HitData.DamageModifiers mods, HitData.DamageType tipo)
        {
            if (tipo == HitData.DamageType.Blunt)
                return mods.m_blunt;
            if (tipo == HitData.DamageType.Slash)
                return mods.m_slash;
            if (tipo == HitData.DamageType.Pierce)
                return mods.m_pierce;
            if (tipo == HitData.DamageType.Chop)
                return mods.m_chop;
            if (tipo == HitData.DamageType.Pickaxe)
                return mods.m_pickaxe;
            if (tipo == HitData.DamageType.Fire)
                return mods.m_fire;
            if (tipo == HitData.DamageType.Frost)
                return mods.m_frost;
            if (tipo == HitData.DamageType.Lightning)
                return mods.m_lightning;
            if (tipo == HitData.DamageType.Poison)
                return mods.m_poison;
            if (tipo == HitData.DamageType.Spirit)
                return mods.m_spirit;
            return HitData.DamageModifier.Normal;
        }

        static void EscreverMod(ref HitData.DamageModifiers mods, HitData.DamageType tipo, HitData.DamageModifier mod)
        {
            if (tipo == HitData.DamageType.Blunt)
                mods.m_blunt = mod;
            else if (tipo == HitData.DamageType.Slash)
                mods.m_slash = mod;
            else if (tipo == HitData.DamageType.Pierce)
                mods.m_pierce = mod;
            else if (tipo == HitData.DamageType.Chop)
                mods.m_chop = mod;
            else if (tipo == HitData.DamageType.Pickaxe)
                mods.m_pickaxe = mod;
            else if (tipo == HitData.DamageType.Fire)
                mods.m_fire = mod;
            else if (tipo == HitData.DamageType.Frost)
                mods.m_frost = mod;
            else if (tipo == HitData.DamageType.Lightning)
                mods.m_lightning = mod;
            else if (tipo == HitData.DamageType.Poison)
                mods.m_poison = mod;
            else if (tipo == HitData.DamageType.Spirit)
                mods.m_spirit = mod;
        }

        public class Bonus
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
            public float adrenalina;
            public string parada;
            public float vigorCasa;

            public bool Tem()
            {
                if (fogo > 0.1f || gelo > 0.1f || raio > 0.1f || veneno > 0.1f || espirito > 0.1f)
                    return true;
                if (bloqueio > 0.1f || defesa > 0.1f || aparar > 0.01f || adrenalina > 0.1f)
                    return true;
                if (resistencias != null && resistencias != "")
                    return true;
                if (equipar != null && equipar != "")
                    return true;
                if (ataque != null && ataque != "")
                    return true;
                if (parada != null && parada != "")
                    return true;
                if (vigorCasa < -0.001f)
                    return true;
                return false;
            }

            public string Texto()
            {
                string texto = "";
                if (espirito > 0.1f)
                    texto = Somar(texto, "espírito " + ((int)espirito).ToString());
                if (fogo > 0.1f)
                    texto = Somar(texto, "fogo " + ((int)fogo).ToString());
                if (gelo > 0.1f)
                    texto = Somar(texto, "gelo " + ((int)gelo).ToString());
                if (raio > 0.1f)
                    texto = Somar(texto, "raio " + ((int)raio).ToString());
                if (veneno > 0.1f)
                    texto = Somar(texto, "veneno " + ((int)veneno).ToString());
                if (bloqueio > 0.1f)
                    texto = Somar(texto, "defesa " + ((int)bloqueio).ToString());
                if (aparar > 1.05f)
                    texto = Somar(texto, "aparar");
                if (adrenalina > 0.1f)
                    texto = Somar(texto, "adrenalina " + ((int)adrenalina).ToString());
                if (vigorCasa < -0.001f)
                    texto = Somar(texto, "vigor em casa " + ((int)(vigorCasa * 100f)).ToString() + "%");
                if (resistencias != null && resistencias != "")
                    texto = Somar(texto, NomeRes(resistencias));
                if (equipar != null && equipar != "")
                    texto = Somar(texto, DescreverEfeitos(equipar));
                if (ataque != null && ataque != "")
                    texto = Somar(texto, "efeito de ataque");
                return texto;
            }

            static string Somar(string texto, string pedaco)
            {
                if (texto == "")
                    return pedaco;
                return texto + ", " + pedaco;
            }
        }

        [HarmonyPatch(typeof(ItemDrop.ItemData), "GetDamage", new System.Type[] { typeof(int), typeof(float) })]
        static class AoDano
        {
            static void Postfix(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result)
            {
                if (Inst == null || !Inst.Ligado.Value)
                    return;
                Bonus bonus = LerGravado(__instance);
                AplicarDano(ref __result, bonus);
            }
        }

        [HarmonyPatch(typeof(ItemDrop.ItemData), "GetBaseBlockPower", new System.Type[] { typeof(int) })]
        static class AoBloqueio
        {
            static void Postfix(ItemDrop.ItemData __instance, ref float __result)
            {
                if (Inst == null || !Inst.Ligado.Value)
                    return;
                Bonus bonus = LerGravado(__instance);
                if (bonus.bloqueio > __result)
                    __result = bonus.bloqueio;
            }
        }

        [HarmonyPatch(typeof(ItemDrop.ItemData), "GetDeflectionForce", new System.Type[] { typeof(int) })]
        static class AoDefesa
        {
            static void Postfix(ItemDrop.ItemData __instance, ref float __result)
            {
                if (Inst == null || !Inst.Ligado.Value)
                    return;
                Bonus bonus = LerGravado(__instance);
                if (bonus.defesa > __result)
                    __result = bonus.defesa;
            }
        }

        [HarmonyPatch(typeof(Humanoid), "BlockAttack")]
        static class AoAparar
        {
            static void Prefix(Humanoid __instance, ItemDrop.ItemData ___m_leftItem, ItemDrop.ItemData ___m_rightItem)
            {
                TrocaParada.Entrar(__instance, ___m_leftItem, ___m_rightItem);
            }

            static void Postfix()
            {
                TrocaParada.Sair();
            }
        }

        static class TrocaParada
        {
            static int profundidade;
            static ItemDrop.ItemData itemA;
            static ItemDrop.ItemData itemB;
            static float paradaA;
            static float paradaB;
            static float adrenalinaA;
            static float adrenalinaB;
            static StatusEffect efeitoA;
            static StatusEffect efeitoB;
            static bool mexeuA;
            static bool mexeuB;
            static int extrasA;
            static int extrasB;

            public static void Entrar(Humanoid quem, ItemDrop.ItemData esquerda, ItemDrop.ItemData direita)
            {
                if (profundidade == 0)
                    Preparar(quem, esquerda, direita);
                profundidade = profundidade + 1;
            }

            public static void Sair()
            {
                profundidade = profundidade - 1;
                if (profundidade > 0)
                    return;
                profundidade = 0;
                Restaurar(itemA, paradaA, adrenalinaA, efeitoA, mexeuA, extrasA);
                Restaurar(itemB, paradaB, adrenalinaB, efeitoB, mexeuB, extrasB);
                itemA = null;
                itemB = null;
                mexeuA = false;
                mexeuB = false;
                extrasA = 0;
                extrasB = 0;
            }

            static void Restaurar(ItemDrop.ItemData item, float parada, float adrenalina, StatusEffect efeito, bool mexeu, int extras)
            {
                if (item != null && item.m_shared != null && item.m_shared.m_damageModifiers != null)
                {
                    while (extras > 0 && item.m_shared.m_damageModifiers.Count > 0)
                    {
                        item.m_shared.m_damageModifiers.RemoveAt(item.m_shared.m_damageModifiers.Count - 1);
                        extras = extras - 1;
                    }
                }
                if (!mexeu || item == null || item.m_shared == null)
                    return;
                item.m_shared.m_timedBlockBonus = parada;
                item.m_shared.m_perfectBlockAdrenaline = adrenalina;
                item.m_shared.m_perfectBlockStatusEffect = efeito;
            }

            static void Preparar(Humanoid quem, ItemDrop.ItemData esquerda, ItemDrop.ItemData direita)
            {
                mexeuA = false;
                mexeuB = false;
                itemA = null;
                itemB = null;
                if (Inst == null || !Inst.Ligado.Value || quem != Player.m_localPlayer)
                    return;
                mexeuA = PrepararItem(esquerda, out itemA, out paradaA, out adrenalinaA, out efeitoA, out extrasA);
                mexeuB = PrepararItem(direita, out itemB, out paradaB, out adrenalinaB, out efeitoB, out extrasB);
            }

            static bool PrepararItem(ItemDrop.ItemData item, out ItemDrop.ItemData guardado, out float parada, out float adrenalina, out StatusEffect efeito, out int extras)
            {
                guardado = null;
                parada = 0f;
                adrenalina = 0f;
                efeito = null;
                extras = 0;
                if (item == null || item.m_shared == null)
                    return false;
                Bonus bonus = LerGravado(item);
                float originalParada = item.m_shared.m_timedBlockBonus;
                float originalAdrenalina = item.m_shared.m_perfectBlockAdrenaline;
                StatusEffect originalEfeito = item.m_shared.m_perfectBlockStatusEffect;
                bool mexeu = false;
                if (bonus.aparar > originalParada)
                {
                    item.m_shared.m_timedBlockBonus = bonus.aparar;
                    mexeu = true;
                }
                if (bonus.adrenalina > originalAdrenalina)
                {
                    item.m_shared.m_perfectBlockAdrenaline = bonus.adrenalina;
                    mexeu = true;
                }
                if (originalEfeito == null && bonus.parada != null && bonus.parada != "")
                {
                    string nome = bonus.parada.Split(',')[0];
                    StatusEffect novo = AcharEfeito(nome);
                    if (novo != null)
                    {
                        item.m_shared.m_perfectBlockStatusEffect = novo;
                        mexeu = true;
                    }
                }
                extras = InjetarRes(item, bonus.resistencias);
                if (extras > 0)
                    mexeu = true;
                if (!mexeu)
                    return false;
                guardado = item;
                parada = originalParada;
                adrenalina = originalAdrenalina;
                efeito = originalEfeito;
                return true;
            }

            static int InjetarRes(ItemDrop.ItemData item, string res)
            {
                if (item == null || item.m_shared == null || res == null || res == "")
                    return 0;
                if (item.m_shared.m_damageModifiers == null)
                    item.m_shared.m_damageModifiers = new List<HitData.DamageModPair>();
                int n = 0;
                string[] partes = res.Split(',');
                for (int i = 0; i < partes.Length; i++)
                {
                    string[] kv = partes[i].Split('=');
                    if (kv.Length != 2)
                        continue;
                    try
                    {
                        HitData.DamageType tipo = (HitData.DamageType)Enum.Parse(typeof(HitData.DamageType), kv[0]);
                        HitData.DamageModifier mod = (HitData.DamageModifier)Enum.Parse(typeof(HitData.DamageModifier), kv[1]);
                        if (Forca(mod) < 3)
                            continue;
                        if (Forca(mod) <= Forca(ModNaLista(item, tipo)))
                            continue;
                        HitData.DamageModPair par = new HitData.DamageModPair();
                        par.m_type = tipo;
                        par.m_modifier = mod;
                        item.m_shared.m_damageModifiers.Add(par);
                        n = n + 1;
                    }
                    catch
                    {
                    }
                }
                return n;
            }

            static HitData.DamageModifier ModNaLista(ItemDrop.ItemData item, HitData.DamageType tipo)
            {
                List<HitData.DamageModPair> lista = item.m_shared.m_damageModifiers;
                HitData.DamageModifier melhor = HitData.DamageModifier.Normal;
                for (int i = 0; i < lista.Count; i++)
                {
                    if (lista[i].m_type == tipo && Forca(lista[i].m_modifier) > Forca(melhor))
                        melhor = lista[i].m_modifier;
                }
                return melhor;
            }
        }

        [HarmonyPatch(typeof(Player), "GetEquipmentHomeItemModifier")]
        static class VigorDaCasa
        {
            static void Postfix(Player __instance, ref float __result)
            {
                if (Inst == null || !Inst.Ligado.Value || __instance == null || __instance.GetInventory() == null)
                    return;
                List<ItemDrop.ItemData> equipados = __instance.GetInventory().GetEquippedItems();
                float nativo = 0f;
                float gravado = 0f;
                for (int i = 0; i < equipados.Count; i++)
                {
                    ItemDrop.ItemData item = equipados[i];
                    if (item == null || item.m_shared == null)
                        continue;
                    nativo += item.m_shared.m_homeItemsStaminaModifier;
                    gravado = MaisUtil(gravado, LerGravado(item).vigorCasa);
                }
                if (gravado < -0.001f && gravado < nativo - 0.001f)
                    __result += gravado;
            }
        }

        [HarmonyPatch(typeof(ItemDrop.ItemData), "AddBlockTooltip")]
        static class MostraAparo
        {
            static ItemDrop.ItemData item;
            static bool mexeuAparo;
            static bool mexeuAdrenalina;
            static float aparoAntes;
            static float adrenalinaAntes;

            static void Prefix(ItemDrop.ItemData __instance)
            {
                item = null;
                mexeuAparo = false;
                mexeuAdrenalina = false;
                if (Inst == null || !Inst.Ligado.Value || __instance == null || __instance.m_shared == null)
                    return;
                Bonus bonus = LerGravado(__instance);
                if (bonus.aparar > __instance.m_shared.m_timedBlockBonus)
                {
                    aparoAntes = __instance.m_shared.m_timedBlockBonus;
                    __instance.m_shared.m_timedBlockBonus = bonus.aparar;
                    mexeuAparo = true;
                    item = __instance;
                }
                if (bonus.adrenalina > __instance.m_shared.m_perfectBlockAdrenaline)
                {
                    adrenalinaAntes = __instance.m_shared.m_perfectBlockAdrenaline;
                    __instance.m_shared.m_perfectBlockAdrenaline = bonus.adrenalina;
                    mexeuAdrenalina = true;
                    item = __instance;
                }
            }

            static void Postfix()
            {
                if (item != null && item.m_shared != null)
                {
                    if (mexeuAparo)
                        item.m_shared.m_timedBlockBonus = aparoAntes;
                    if (mexeuAdrenalina)
                        item.m_shared.m_perfectBlockAdrenaline = adrenalinaAntes;
                }
                item = null;
                mexeuAparo = false;
                mexeuAdrenalina = false;
            }
        }

        [HarmonyPatch(typeof(ItemDrop.ItemData), "GetTooltip", new System.Type[] { typeof(int) })]
        static class AoDica
        {
            static void Postfix(ItemDrop.ItemData __instance, ref string __result)
            {
                if (Inst == null || !Inst.Ligado.Value)
                    return;
                Bonus bonus = LerGravado(__instance);
                if (!bonus.Tem())
                    return;
                __result = __result + "\nFundido: " + bonus.Texto();
            }
        }

        [HarmonyPatch(typeof(Character), "GetDamageModifiers")]
        static class AoResistencia
        {
            static void Postfix(Character __instance, ref HitData.DamageModifiers __result)
            {
                if (Inst == null || !Inst.Ligado.Value || __instance != Player.m_localPlayer)
                    return;
                List<ItemDrop.ItemData> equipados = Player.m_localPlayer.GetInventory().GetEquippedItems();
                for (int i = 0; i < equipados.Count; i++)
                {
                    if (Familia(equipados[i]) == 2)
                        continue;
                    Bonus bonus = LerGravado(equipados[i]);
                    AplicarRes(ref __result, bonus.resistencias);
                }
            }
        }

        [HarmonyPatch(typeof(Humanoid), "UpdateEquipmentStatusEffects")]
        static class AoEquipar
        {
            static void Postfix(Humanoid __instance)
            {
                if (Inst == null || __instance != Player.m_localPlayer)
                    return;
                ManterEfeitos(Player.m_localPlayer);
            }
        }

        [HarmonyPatch(typeof(Attack), "DoMeleeAttack")]
        static class GolpePerto
        {
            static void Prefix(Humanoid ___m_character)
            {
                TrocaEfeito.Entrar(___m_character);
            }

            static void Postfix()
            {
                TrocaEfeito.Sair();
            }
        }

        [HarmonyPatch(typeof(Attack), "FireProjectileBurst")]
        static class GolpeLonge
        {
            static void Prefix(Humanoid ___m_character)
            {
                TrocaEfeito.Entrar(___m_character);
            }

            static void Postfix()
            {
                TrocaEfeito.Sair();
            }
        }

        static class TrocaEfeito
        {
            static int profundidade;
            static ItemDrop.ItemData item;
            static StatusEffect anterior;

            public static void Entrar(Humanoid quem)
            {
                if (profundidade == 0)
                    Preparar(quem);
                profundidade = profundidade + 1;
            }

            public static void Sair()
            {
                profundidade = profundidade - 1;
                if (profundidade > 0)
                    return;
                profundidade = 0;
                if (item != null && item.m_shared != null)
                    item.m_shared.m_attackStatusEffect = anterior;
                item = null;
                anterior = null;
            }

            static void Preparar(Humanoid quem)
            {
                item = null;
                anterior = null;
                if (Inst == null || !Inst.Ligado.Value || quem != Player.m_localPlayer)
                    return;
                ItemDrop.ItemData arma = quem.GetCurrentWeapon();
                if (arma == null || arma.m_shared == null || arma.m_shared.m_attackStatusEffect != null)
                    return;
                Bonus bonus = LerGravado(arma);
                StatusEffect efeito = AcharEfeito(bonus.ataque);
                if (efeito == null)
                    return;
                item = arma;
                anterior = arma.m_shared.m_attackStatusEffect;
                arma.m_shared.m_attackStatusEffect = efeito;
            }
        }

        [HarmonyPatch(typeof(GameCamera), "UpdateMouseCapture")]
        static class SoltaCursor
        {
            static void Postfix()
            {
                if (!Aberto)
                    return;
                ZCursor.LockState = CursorLockMode.None;
                ZCursor.Show();
            }
        }

        [HarmonyPatch(typeof(Player), "TakeInput")]
        static class SemJogo
        {
            static void Postfix(ref bool __result)
            {
                if (Aberto)
                    __result = false;
            }
        }

        [HarmonyPatch(typeof(PlayerController), "TakeInput")]
        static class SemComando
        {
            static void Postfix(ref bool __result)
            {
                if (Aberto)
                    __result = false;
            }
        }

        [HarmonyPatch(typeof(Menu), "Update")]
        static class SemPausa
        {
            static bool Prefix()
            {
                if (quadroSemPausa == Time.frameCount)
                    return false;
                if (!Aberto || !ZInput.GetKeyDown(KeyCode.Escape, true))
                    return true;
                Aberto = false;
                quadroSemPausa = Time.frameCount;
                return false;
            }
        }

        [HarmonyPatch(typeof(ZInput), "GetMouseDelta")]
        static class SemOlhar
        {
            static bool Prefix(ref Vector2 __result)
            {
                if (!Aberto)
                    return true;
                __result = Vector2.zero;
                return false;
            }
        }

        [HarmonyPatch(typeof(ZInput), "GetMouseButton")]
        static class SemClique
        {
            static bool Prefix(ref bool __result)
            {
                if (!Aberto)
                    return true;
                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(ZInput), "GetMouseButtonUp")]
        static class SemSolta
        {
            static bool Prefix(ref bool __result)
            {
                if (!Aberto)
                    return true;
                __result = false;
                return false;
            }
        }

        static void GuardarPeca(ItemDrop.ItemData item)
        {
            if (DadosMelhoria != null || item == null || item.m_customData == null)
                return;
            string texto;
            if (!item.m_customData.TryGetValue(Chave, out texto) || texto == null || texto == "")
                return;
            DadosMelhoria = new Dictionary<string, string>(item.m_customData);
            if (item.m_shared != null)
                NomeMelhoria = item.m_shared.m_name;
            if (item.m_dropPrefab != null)
                PrefabMelhoria = item.m_dropPrefab.name;
        }

        static void ColarPeca(ItemDrop.ItemData item)
        {
            if (DadosMelhoria == null || item == null)
                return;
            bool mesmo = false;
            if (NomeMelhoria != null && item.m_shared != null && item.m_shared.m_name == NomeMelhoria)
                mesmo = true;
            if (!mesmo && PrefabMelhoria != null && item.m_dropPrefab != null && item.m_dropPrefab.name == PrefabMelhoria)
                mesmo = true;
            if (!mesmo)
                return;
            if (item.m_customData == null)
                item.m_customData = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> par in DadosMelhoria)
            {
                string ja;
                if (!item.m_customData.TryGetValue(par.Key, out ja) || ja == null || ja == "")
                    item.m_customData[par.Key] = par.Value;
            }
        }

        static void LimparPeca()
        {
            DadosMelhoria = null;
            NomeMelhoria = null;
            PrefabMelhoria = null;
        }

        [HarmonyPatch(typeof(InventoryGui), "DoCrafting")]
        static class GuardaMelhoria
        {
            static void Prefix(InventoryGui __instance)
            {
                DentroMelhoria++;
                LimparPeca();
                if (CampoMelhoria == null)
                    return;
                GuardarPeca(CampoMelhoria.GetValue(__instance) as ItemDrop.ItemData);
            }

            static void Postfix()
            {
                DentroMelhoria--;
                if (DentroMelhoria < 0)
                    DentroMelhoria = 0;
                LimparPeca();
            }
        }

        [HarmonyPatch(typeof(Inventory), "RemoveItem", new System.Type[] { typeof(ItemDrop.ItemData) })]
        static class GuardaAoTirar
        {
            static void Prefix(ItemDrop.ItemData item)
            {
                if (DentroMelhoria <= 0)
                    return;
                GuardarPeca(item);
            }
        }

        [HarmonyPatch(typeof(Inventory), "AddItem", new System.Type[] { typeof(string), typeof(int), typeof(int), typeof(int), typeof(long), typeof(string), typeof(Vector2i), typeof(bool), typeof(bool), typeof(bool) })]
        static class CopiaNaMelhoria
        {
            static void Postfix(ItemDrop.ItemData __result)
            {
                if (DentroMelhoria <= 0)
                    return;
                ColarPeca(__result);
            }
        }

        [HarmonyPatch(typeof(Inventory), "AddItem", new System.Type[] { typeof(string), typeof(int), typeof(int), typeof(int), typeof(long), typeof(string), typeof(bool), typeof(bool) })]
        static class CopiaNaMelhoriaCurta
        {
            static void Postfix(ItemDrop.ItemData __result)
            {
                if (DentroMelhoria <= 0)
                    return;
                ColarPeca(__result);
            }
        }
    }
}
