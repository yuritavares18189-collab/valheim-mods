using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace GaloDeGuerra
{
    [BepInPlugin("yuri.GaloDeGuerra", "War Rooster", "1.0.3")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Inst;

        ConfigEntry<bool> Ligado;
        ConfigEntry<KeyboardShortcut> Tecla;

        Contrato contrato;
        List<string> resolvidos = new List<string>();
        ZRoutedRpc rpcDono;
        bool aberto;
        Rect janela;
        Vector2 scrollJogadores;
        Vector2 scrollItens;
        string selJogador;
        long selUser;
        uint selZid;
        string selNome;
        int selQual;
        int selVar;
        int selX;
        int selY;
        int selQtd;
        long ultimoId;
        string ultimoNome = "";
        float ultimoQuando;
        float proximoPasso;
        bool avisouCheio;

        void Awake()
        {
            Inst = this;
            Ligado = Config.Bind("General", "Enabled", true, "Turns on War Rooster in this game. Both fighters need this mod.");
            Tecla = Config.Bind("General", "Key", new KeyboardShortcut(KeyCode.F8), "Opens the window to challenge someone to a 1v1.");
            Carregar();
            Harmony harmonia = new Harmony("yuri.GaloDeGuerra");
            harmonia.PatchAll(Assembly.GetExecutingAssembly());
            BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            harmonia.Patch(AccessTools.Method(typeof(GameCamera), "UpdateMouseCapture"), postfix: new HarmonyMethod(typeof(Plugin).GetMethod("SoltaCursor", flags)));
            harmonia.Patch(AccessTools.Method(typeof(Player), "TakeInput"), postfix: new HarmonyMethod(typeof(Plugin).GetMethod("SemJogo", flags)));
            harmonia.Patch(AccessTools.Method(typeof(ZInput), "GetMouseDelta"), prefix: new HarmonyMethod(typeof(Plugin).GetMethod("BloqueiaDelta", flags)));
            harmonia.Patch(AccessTools.Method(typeof(ZInput), "GetMouseScrollWheel"), prefix: new HarmonyMethod(typeof(Plugin).GetMethod("BloqueiaRoda", flags)));
            harmonia.Patch(AccessTools.Method(typeof(ZInput), "GetMouseButton"), prefix: new HarmonyMethod(typeof(Plugin).GetMethod("BloqueiaBool", flags)));
            harmonia.Patch(AccessTools.Method(typeof(ZInput), "GetMouseButtonDown"), prefix: new HarmonyMethod(typeof(Plugin).GetMethod("BloqueiaBool", flags)));
            harmonia.Patch(AccessTools.Method(typeof(ZInput), "GetMouseButtonUp"), prefix: new HarmonyMethod(typeof(Plugin).GetMethod("BloqueiaBool", flags)));
            Logger.LogInfo("War Rooster 1.0.3");
        }

        static bool JanelaAberta()
        {
            return Inst != null && Inst.aberto;
        }

        static void SoltaCursor()
        {
            if (!JanelaAberta())
                return;
            ZCursor.LockState = CursorLockMode.None;
            ZCursor.Show();
        }

        static void SemJogo(ref bool __result)
        {
            if (JanelaAberta())
                __result = false;
        }

        static bool BloqueiaBool(ref bool __result)
        {
            if (!JanelaAberta())
                return true;
            __result = false;
            return false;
        }

        static bool BloqueiaRoda(ref float __result)
        {
            if (!JanelaAberta())
                return true;
            __result = 0f;
            return false;
        }

        static bool BloqueiaDelta(ref Vector2 __result)
        {
            if (!JanelaAberta())
                return true;
            __result = Vector2.zero;
            return false;
        }

        void Update()
        {
            RegistrarRpc();
            if (Player.m_localPlayer == null)
                return;
            if (Tecla.Value.IsDown() && !ConsoleAberto())
                aberto = !aberto;
            if (contrato == null)
                return;
            if (Time.time < proximoPasso)
                return;
            if (contrato.estado == 1 && contrato.souDesafiante && Agora() - contrato.criado > 90)
            {
                proximoPasso = Time.time + 1f;
                CancelarEspera("Nobody accepted. Your bet came back.");
                return;
            }
            if (contrato.estado == 4)
            {
                proximoPasso = Time.time + 5f;
                Enviar(contrato.repetir, contrato.id, contrato.oponenteId, contrato.oponenteNome, null);
                return;
            }
            if (contrato.estado == 5 || contrato.estado == 6)
            {
                proximoPasso = Time.time + 1f;
                if (contrato.estado == 5)
                    TentarPagar();
                else
                    TentarDevolver();
            }
            if (contrato != null && contrato.pediuDevolucao && Agora() - contrato.pedidoQuando > 40)
                contrato.pediuDevolucao = false;
        }

        void OnGUI()
        {
            if (!aberto)
                return;
            if (janela.width < 10f)
                janela = new Rect((Screen.width - 560f) / 2f, (Screen.height - 640f) / 2f, 560f, 640f);
            Color fundo = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.42f, 0.1f, 0.1f);
            janela = GUI.Window(4545, janela, Desenhar, "War Rooster");
            GUI.backgroundColor = fundo;
        }

        void Desenhar(int id)
        {
            Player jogador = Player.m_localPlayer;
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                aberto = false;
                Event.current.Use();
                GUI.DragWindow();
                return;
            }
            if (jogador == null)
            {
                GUILayout.Label("Enter the world to challenge someone.");
                if (GUILayout.Button("Close", GUILayout.Height(28)))
                    aberto = false;
                GUI.DragWindow();
                return;
            }
            GUILayout.Label("Both fighters need this mod. The bet leaves the bag and does not stay on the body.");
            if (jogador.IsPVPEnabled())
                GUILayout.Label("Your PvP is on. The other fighter needs it on too.");
            else
                GUILayout.Label("PvP is off. Without it the hit does not land.");
            if (GUILayout.Button("Turn PvP on for both", GUILayout.Height(28)))
                ForcarPvp(true);
            if (contrato != null && contrato.estado == 1)
                DesenharEspera();
            else if (contrato != null && contrato.estado == 2)
                DesenharConvite(jogador);
            else if (contrato != null && contrato.estado == 3)
                DesenharRinha();
            else if (contrato != null && contrato.estado == 4)
                GUILayout.Label(contrato.oponenteNome + " won: " + TextoPremio(contrato.minha, contrato.deles) + ".");
            else if (contrato != null && contrato.estado == 5)
                GUILayout.Label("You won: " + TextoPremio(contrato.minha, contrato.deles) + ". If the bag is full, make a free slot.");
            else if (contrato != null && contrato.estado == 6)
                GUILayout.Label("Giving your bet back.");
            else
                DesenharChamado(jogador);
            GUILayout.Space(8);
            if (GUILayout.Button("Close", GUILayout.Height(28)))
                aberto = false;
            GUI.DragWindow();
        }

        void DesenharEspera()
        {
            GUILayout.Label("Waiting for " + contrato.oponenteNome + " to accept.");
            GUILayout.Label("Your bet: " + Texto(contrato.minha));
            if (GUILayout.Button("Cancel", GUILayout.Height(32)))
                CancelarEspera("Challenge cancelled. Your bet came back.");
        }

        void DesenharConvite(Player jogador)
        {
            GUILayout.Label(contrato.oponenteNome + " challenged you to a 1v1.");
            GUILayout.Label("They bet: " + Texto(contrato.deles));
            GUILayout.Label("Pick what you bet. It can be coins, gems, a weapon, armor or another item.");
            DesenharItens(jogador);
            Color cor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.75f, 0.55f, 0.16f);
            if (GUILayout.Button("Accept and bet", GUILayout.Height(34)))
                Aceitar(jogador);
            GUI.backgroundColor = cor;
            if (GUILayout.Button("Decline", GUILayout.Height(28)))
                Recusar();
        }

        void DesenharRinha()
        {
            GUILayout.Label("Fight against " + contrato.oponenteNome + ".");
            GUILayout.Label("You bet: " + Texto(contrato.minha));
            GUILayout.Label("They bet: " + Texto(contrato.deles));
            GUILayout.Label("Whoever falls to the other loses. A fall, a creature or hunger do not count. Forfeit hands over the bet.");
            if (contrato.pedidoRecebido)
            {
                GUILayout.Label(contrato.oponenteNome + " wants to return both bets.");
                if (GUILayout.Button("Accept and return", GUILayout.Height(32)))
                    ConcordarDevolucao();
            }
            else if (contrato.pediuDevolucao)
                GUILayout.Label("Return request sent.");
            else if (GUILayout.Button("Ask to return", GUILayout.Height(28)))
                PedirDevolucao();
            GUILayout.Space(6);
            if (GUILayout.Button("Forfeit", GUILayout.Height(32)))
                Perder("desistir");
        }

        void DesenharChamado(Player jogador)
        {
            if (!Ligado.Value)
            {
                GUILayout.Label("War Rooster is off in F1.");
                return;
            }
            GUILayout.Label("Challenge someone. Coins are the currency. Gems are amber, ruby and silver. Without those, the item you are holding is used.");
            scrollJogadores = GUILayout.BeginScrollView(scrollJogadores, GUILayout.Height(120));
            List<ZNet.PlayerInfo> gente = Gente();
            if (gente.Count == 0)
                GUILayout.Label("Nobody else in this world.");
            for (int i = 0; i < gente.Count; i++)
            {
                ZNet.PlayerInfo info = gente[i];
                bool marcado = info.m_name == selJogador && info.m_characterID != null && info.m_characterID.UserID == selUser && info.m_characterID.ID == selZid;
                Color cor = GUI.backgroundColor;
                if (marcado)
                    GUI.backgroundColor = new Color(0.75f, 0.55f, 0.16f);
                if (GUILayout.Button(info.m_name, GUILayout.Height(26)))
                {
                    selJogador = info.m_name;
                    selUser = info.m_characterID.UserID;
                    selZid = info.m_characterID.ID;
                }
                GUI.backgroundColor = cor;
            }
            GUILayout.EndScrollView();
            if (!string.IsNullOrEmpty(selJogador))
                GUILayout.Label("Against: " + selJogador);
            DesenharItens(jogador);
            Color ouro = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.75f, 0.55f, 0.16f);
            if (GUILayout.Button("Challenge to a 1v1", GUILayout.Height(34)))
                Chamar(jogador);
            GUI.backgroundColor = ouro;
        }

        void DesenharItens(Player jogador)
        {
            List<ItemDrop.ItemData> itens = Itens(jogador);
            scrollItens = GUILayout.BeginScrollView(scrollItens, GUILayout.Height(200));
            if (itens.Count == 0)
                GUILayout.Label("Bag is empty.");
            for (int i = 0; i < itens.Count; i++)
            {
                ItemDrop.ItemData item = itens[i];
                bool marcado = MesmoItem(item);
                string texto = NomeBonito(item.m_shared.m_name);
                if (item.m_quality > 1)
                    texto = texto + " lv" + item.m_quality;
                if (item.m_stack > 1)
                    texto = texto + " x" + item.m_stack;
                if (item.m_equipped)
                    texto = texto + " (equipped)";
                Color cor = GUI.backgroundColor;
                if (marcado)
                    GUI.backgroundColor = new Color(0.75f, 0.55f, 0.16f);
                if (GUILayout.Button(texto, GUILayout.Height(24)))
                {
                    selNome = NomeInterno(item);
                    selQual = item.m_quality;
                    selVar = item.m_variant;
                    selX = item.m_gridPos.x;
                    selY = item.m_gridPos.y;
                    selQtd = item.m_stack;
                }
                GUI.backgroundColor = cor;
            }
            GUILayout.EndScrollView();
            if (string.IsNullOrEmpty(selNome))
                return;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-10", GUILayout.Width(48)))
                selQtd = selQtd - 10;
            if (GUILayout.Button("-1", GUILayout.Width(40)))
                selQtd = selQtd - 1;
            GUILayout.Label("Amount: " + selQtd, GUILayout.Width(140));
            if (GUILayout.Button("+1", GUILayout.Width(40)))
                selQtd = selQtd + 1;
            if (GUILayout.Button("+10", GUILayout.Width(48)))
                selQtd = selQtd + 10;
            if (GUILayout.Button("All", GUILayout.Width(56)))
            {
                ItemDrop.ItemData item = AcharItem(jogador);
                if (item != null)
                    selQtd = item.m_stack;
            }
            GUILayout.EndHorizontal();
            ItemDrop.ItemData escolhido = AcharItem(jogador);
            if (escolhido != null && selQtd > escolhido.m_stack)
                selQtd = escolhido.m_stack;
            if (selQtd < 1)
                selQtd = 1;
        }

        void Chamar(Player jogador)
        {
            if (contrato != null)
                return;
            if (string.IsNullOrEmpty(selJogador))
            {
                Avisar("Pick who you will challenge.");
                return;
            }
            ZNet.PlayerInfo info;
            if (!AcharJogador(selJogador, selUser, selZid, out info))
            {
                Avisar("That player is no longer in the world.");
                return;
            }
            ItemDrop.ItemData item = AcharItem(jogador);
            if (item == null || selQtd < 1 || selQtd > item.m_stack)
            {
                Avisar("Pick what you will bet.");
                return;
            }
            Aposta aposta = Copiar(item, selQtd);
            if (item.m_equipped)
                jogador.UnequipItem(item, false);
            if (!jogador.GetInventory().RemoveItem(item, selQtd))
            {
                Avisar("Could not split off the bet.");
                return;
            }
            contrato = new Contrato();
            contrato.id = Guid.NewGuid().ToString("N");
            contrato.estado = 1;
            contrato.souDesafiante = true;
            contrato.oponenteNome = info.m_name;
            contrato.minha = aposta;
            contrato.criado = Agora();
            Salvar();
            EnviarAlvo("desafio", contrato.id, info.m_name, info.m_characterID.UserID, info.m_characterID.ID, aposta);
            Avisar("Challenge sent to " + info.m_name + ".");
        }

        void Aceitar(Player jogador)
        {
            if (contrato == null || contrato.estado != 2)
                return;
            ItemDrop.ItemData item = AcharItem(jogador);
            if (item == null || selQtd < 1 || selQtd > item.m_stack)
            {
                Avisar("Pick what you will bet.");
                return;
            }
            Aposta aposta = Copiar(item, selQtd);
            if (item.m_equipped)
                jogador.UnequipItem(item, false);
            if (!jogador.GetInventory().RemoveItem(item, selQtd))
            {
                Avisar("Could not split off the bet.");
                return;
            }
            contrato.minha = aposta;
            contrato.estado = 3;
            Salvar();
            Enviar("aceitar", contrato.id, contrato.oponenteId, contrato.oponenteNome, aposta);
            ForcarPvp(true);
            Avisar("Fight on against " + contrato.oponenteNome + ".");
        }

        void ForcarPvp(bool avisarOutro)
        {
            Player jogador = Player.m_localPlayer;
            if (jogador == null)
                return;
            if (jogador.IsPVPEnabled())
                jogador.SetPVP(false);
            jogador.SetPVP(true);
            if (!avisarOutro)
                return;
            if (contrato != null && !string.IsNullOrEmpty(contrato.oponenteNome))
                Enviar("pvp", string.IsNullOrEmpty(contrato.id) ? "pvp" : contrato.id, contrato.oponenteId, contrato.oponenteNome, null);
            else if (!string.IsNullOrEmpty(selJogador))
                EnviarAlvo("pvp", "pvp", selJogador, selUser, selZid, null);
            else
            {
                Avisar("PvP is on here. Pick the other fighter to turn it on for them too.");
                return;
            }
            Avisar("PvP is on for both.");
        }

        void Recusar()
        {
            if (contrato == null || contrato.estado != 2)
                return;
            Enviar("recusar", contrato.id, contrato.oponenteId, contrato.oponenteNome, null);
            string nome = contrato.oponenteNome;
            contrato = null;
            Salvar();
            Avisar("You declined the challenge from " + nome + ".");
        }

        void PedirDevolucao()
        {
            if (contrato == null || contrato.estado != 3)
                return;
            contrato.pediuDevolucao = true;
            contrato.pedidoQuando = Agora();
            if (contrato.pedidoRecebido)
            {
                ConcordarDevolucao();
                return;
            }
            Salvar();
            Enviar("pedido", contrato.id, contrato.oponenteId, contrato.oponenteNome, null);
            Avisar("Asked to return the bets.");
        }

        void ConcordarDevolucao()
        {
            if (contrato == null || contrato.estado != 3)
                return;
            contrato.aviso = "Bets returned.";
            contrato.pendencia = "devolvido";
            contrato.estado = 6;
            Salvar();
            TentarDevolver();
        }

        void Perder(string verbo)
        {
            if (contrato == null || contrato.estado != 3)
                return;
            contrato.estado = 4;
            contrato.repetir = verbo;
            Salvar();
            Enviar(verbo, contrato.id, contrato.oponenteId, contrato.oponenteNome, null);
            proximoPasso = Time.time + 5f;
            aberto = true;
            Avisar(contrato.oponenteNome + " won: " + TextoPremio(contrato.minha, contrato.deles) + ".", true);
        }

        void CancelarEspera(string aviso)
        {
            if (contrato == null || contrato.estado != 1)
                return;
            string id = contrato.id;
            long para = contrato.oponenteId;
            string nome = contrato.oponenteNome;
            Enviar("expirou", id, para, nome, null);
            contrato.aviso = aviso;
            contrato.pendencia = "";
            contrato.estado = 6;
            Salvar();
            TentarDevolver();
        }

        public void AoDano(Character vitima, HitData hit)
        {
            if (vitima != Player.m_localPlayer || hit == null)
                return;
            if (contrato == null || contrato.estado != 3)
                return;
            List<Player> todos = Player.GetAllPlayers();
            if (todos == null)
                return;
            for (int i = 0; i < todos.Count; i++)
            {
                Player p = todos[i];
                if (p == null || p == Player.m_localPlayer)
                    continue;
                if (p.GetZDOID() != hit.m_attacker)
                    continue;
                ultimoId = p.GetPlayerID();
                ultimoNome = p.GetPlayerName();
                ultimoQuando = Time.time;
                return;
            }
        }

        public void AoMorrer(Player vitima)
        {
            if (vitima != Player.m_localPlayer)
                return;
            if (contrato == null || contrato.estado != 3)
                return;
            bool foiEle = false;
            if (Time.time - ultimoQuando < 15f)
            {
                if (contrato.oponenteId != 0 && ultimoId == contrato.oponenteId)
                    foiEle = true;
                else if (!string.IsNullOrEmpty(ultimoNome) && ultimoNome == contrato.oponenteNome)
                    foiEle = true;
            }
            if (!foiEle)
            {
                Avisar("You fell, but not to your opponent. The fight goes on.");
                return;
            }
            Perder("morri");
        }

        void RegistrarRpc()
        {
            if (ZRoutedRpc.instance == null || ZRoutedRpc.instance == rpcDono)
                return;
            ZRoutedRpc.instance.Register<ZPackage>("GaloDeGuerra", new Action<long, ZPackage>(Receber));
            rpcDono = ZRoutedRpc.instance;
        }

        static void Receber(long remetente, ZPackage pkg)
        {
            if (Inst != null)
                Inst.Tratar(remetente, pkg);
        }

        void Tratar(long remetente, ZPackage pkg)
        {
            try
            {
                if (Player.m_localPlayer == null || pkg == null)
                    return;
                int ver = pkg.ReadInt();
                if (ver != 1)
                    return;
                string verbo = pkg.ReadString();
                string id = pkg.ReadString();
                long deId = pkg.ReadLong();
                string deNome = pkg.ReadString();
                string alvoNome = pkg.ReadString();
                long alvoUser = pkg.ReadLong();
                uint alvoZid = pkg.ReadUInt();
                Aposta aposta = null;
                if (pkg.ReadBool())
                    aposta = LerAposta(pkg);
                long meuId = Player.m_localPlayer.GetPlayerID();
                if (deId == meuId)
                    return;
                if (alvoNome != Player.m_localPlayer.GetPlayerName())
                    return;
                if (verbo == "desafio")
                    ReceberDesafio(id, deId, deNome, alvoUser, alvoZid, aposta);
                else if (verbo == "aceitar")
                    ReceberAceite(id, deId, deNome, aposta);
                else if (verbo == "recusar")
                    ReceberRecusa(id, deNome);
                else if (verbo == "expirou")
                    ReceberExpirou(id, deId, deNome);
                else if (verbo == "morri" || verbo == "desistir")
                    ReceberDerrota(verbo, id, deId, deNome);
                else if (verbo == "pedido")
                    ReceberPedido(id, deId);
                else if (verbo == "devolvido" || verbo == "empate")
                    ReceberDevolucao(id, deId, verbo);
                else if (verbo == "ack")
                    ReceberAck(id, deId);
                else if (verbo == "pvp")
                    ForcarPvp(false);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("War Rooster packet ignored: " + ex.Message);
            }
        }

        void ReceberDesafio(string id, long deId, string deNome, long alvoUser, uint alvoZid, Aposta aposta)
        {
            ZDOID meu = Player.m_localPlayer.GetZDOID();
            if (meu == null || meu.UserID != alvoUser || meu.ID != alvoZid)
                return;
            if (aposta == null || aposta.qtd <= 0)
                return;
            if (!Ligado.Value || (contrato != null && !(contrato.estado == 2 && contrato.id == id)))
            {
                Enviar("recusar", id, deId, deNome, null);
                return;
            }
            if (contrato != null && contrato.id == id)
                return;
            contrato = new Contrato();
            contrato.id = id;
            contrato.estado = 2;
            contrato.souDesafiante = false;
            contrato.oponenteId = deId;
            contrato.oponenteNome = deNome;
            contrato.deles = aposta;
            contrato.criado = Agora();
            Salvar();
            aberto = true;
            Avisar(deNome + " challenged you to a 1v1.");
        }

        void ReceberAceite(string id, long deId, string deNome, Aposta aposta)
        {
            if (contrato == null || contrato.id != id || contrato.estado != 1 || !contrato.souDesafiante || deNome != contrato.oponenteNome)
            {
                Enviar("expirou", id, deId, deNome, null);
                return;
            }
            if (aposta == null || aposta.qtd <= 0)
            {
                Enviar("expirou", id, deId, deNome, null);
                return;
            }
            contrato.oponenteId = deId;
            contrato.deles = aposta;
            contrato.estado = 3;
            Salvar();
            aberto = true;
            ForcarPvp(true);
            Avisar("Fight on against " + deNome + ".");
        }

        void ReceberRecusa(string id, string deNome)
        {
            if (contrato == null || contrato.id != id || contrato.estado != 1)
                return;
            if (deNome != contrato.oponenteNome)
                return;
            CancelarEspera(deNome + " declined. Your bet came back.");
        }

        void ReceberExpirou(string id, long deId, string deNome)
        {
            if (contrato == null || contrato.id != id || contrato.souDesafiante)
                return;
            if (deId != contrato.oponenteId && deNome != contrato.oponenteNome)
                return;
            if (contrato.estado == 2)
            {
                contrato = null;
                Salvar();
                Avisar("The challenge expired.");
                return;
            }
            if (contrato.estado == 3)
            {
                contrato.aviso = "The challenge expired. Your bet came back.";
                contrato.pendencia = "";
                contrato.estado = 6;
                Salvar();
                TentarDevolver();
            }
        }

        void ReceberDerrota(string verbo, string id, long deId, string deNome)
        {
            if (JaResolvido(id))
            {
                Enviar("ack", id, deId, deNome, null);
                return;
            }
            if (contrato == null || contrato.id != id)
                return;
            if (deId != contrato.oponenteId && deNome != contrato.oponenteNome)
                return;
            if (contrato.estado == 4)
            {
                Empatar();
                return;
            }
            if (contrato.estado == 5)
            {
                TentarPagar();
                return;
            }
            if (contrato.estado != 3)
                return;
            aberto = true;
            Avisar("You won: " + TextoPremio(contrato.minha, contrato.deles) + ".", true);
            Pagar();
        }

        void ReceberPedido(string id, long deId)
        {
            if (contrato == null || contrato.id != id || contrato.estado != 3)
                return;
            if (deId != contrato.oponenteId && deId != 0)
                return;
            contrato.pedidoRecebido = true;
            aberto = true;
            if (contrato.pediuDevolucao)
                ConcordarDevolucao();
            else
                Avisar(contrato.oponenteNome + " wants to return the bets.");
        }

        void ReceberDevolucao(string id, long deId, string verbo)
        {
            if (contrato == null || contrato.id != id)
                return;
            if (deId != contrato.oponenteId && deId != 0)
                return;
            if (contrato.estado == 6 || contrato.estado == 5 || contrato.estado == 4)
                return;
            if (contrato.estado != 3 && contrato.estado != 1 && contrato.estado != 2)
                return;
            if (contrato.estado == 2)
            {
                contrato = null;
                Salvar();
                Avisar("The challenge was cancelled.");
                return;
            }
            contrato.aviso = verbo == "empate" ? "Both fell. Your bet came back." : "Bets returned.";
            contrato.pendencia = "";
            contrato.estado = 6;
            Salvar();
            TentarDevolver();
        }

        void ReceberAck(string id, long deId)
        {
            if (contrato == null || contrato.id != id || contrato.estado != 4)
                return;
            if (deId != contrato.oponenteId && deId != 0)
                return;
            Lembrar(id);
            contrato = null;
            Salvar();
        }

        void Pagar()
        {
            if (contrato == null)
                return;
            if (JaResolvido(contrato.id))
            {
                Enviar("ack", contrato.id, contrato.oponenteId, contrato.oponenteNome, null);
                return;
            }
            if (contrato.estado != 5)
            {
                contrato.estado = 5;
                contrato.minhaPaga = false;
                contrato.delesPaga = false;
                Salvar();
            }
            TentarPagar();
        }

        void TentarPagar()
        {
            if (contrato == null || contrato.estado != 5)
                return;
            Player jogador = Player.m_localPlayer;
            if (jogador == null)
                return;
            if (!contrato.minhaPaga)
                contrato.minhaPaga = Entregar(jogador, contrato.minha);
            if (!contrato.delesPaga)
                contrato.delesPaga = Entregar(jogador, contrato.deles);
            Salvar();
            if (!contrato.minhaPaga || !contrato.delesPaga)
            {
                if (!avisouCheio)
                {
                    avisouCheio = true;
                    Avisar("Inventory full. Make a free slot and the bet goes in.");
                }
                return;
            }
            string id = contrato.id;
            long para = contrato.oponenteId;
            string nome = contrato.oponenteNome;
            string premio = TextoPremio(contrato.minha, contrato.deles);
            Lembrar(id);
            contrato = null;
            avisouCheio = false;
            Salvar();
            Enviar("ack", id, para, nome, null);
            Avisar("You won: " + premio + ".", true);
        }

        void Empatar()
        {
            if (contrato == null)
                return;
            contrato.aviso = "Both fell. Your bet came back.";
            contrato.pendencia = "empate";
            contrato.estado = 6;
            Salvar();
            TentarDevolver();
        }

        void TentarDevolver()
        {
            if (contrato == null || contrato.estado != 6)
                return;
            Player jogador = Player.m_localPlayer;
            if (jogador == null)
                return;
            if (contrato.minha != null && !Entregar(jogador, contrato.minha))
            {
                if (!avisouCheio)
                {
                    avisouCheio = true;
                    Avisar("Inventory full. Make a free slot and the bet comes back.");
                }
                return;
            }
            string pendencia = contrato.pendencia;
            string id = contrato.id;
            long para = contrato.oponenteId;
            string nome = contrato.oponenteNome;
            string aviso = contrato.aviso;
            Lembrar(id);
            contrato = null;
            avisouCheio = false;
            Salvar();
            if (!string.IsNullOrEmpty(pendencia))
                Enviar(pendencia, id, para, nome, null);
            if (!string.IsNullOrEmpty(aviso))
                Avisar(aviso);
        }

        bool Entregar(Player jogador, Aposta aposta)
        {
            if (aposta == null || aposta.qtd <= 0 || string.IsNullOrEmpty(aposta.nome))
                return true;
            if (ObjectDB.instance == null)
                return false;
            GameObject prefab = ObjectDB.instance.GetItemPrefab(aposta.nome);
            if (prefab == null)
            {
                Logger.LogWarning("Bet item does not exist: " + aposta.nome);
                return false;
            }
            ItemDrop origem = prefab.GetComponent<ItemDrop>();
            if (origem == null || origem.m_itemData == null)
                return false;
            ItemDrop.ItemData novo = origem.m_itemData.Clone();
            novo.m_stack = aposta.qtd;
            novo.m_quality = aposta.qualidade;
            novo.m_variant = aposta.variante;
            novo.m_durability = aposta.durabilidade;
            novo.m_crafterID = aposta.crafterId;
            novo.m_crafterName = aposta.crafterNome;
            novo.m_worldLevel = aposta.mundo;
            novo.m_equipped = false;
            return jogador.GetInventory().AddItem(novo);
        }

        void Enviar(string verbo, string id, long paraId, string alvoNome, Aposta aposta)
        {
            long user = 0;
            uint zid = 0;
            EnviarPacote(verbo, id, alvoNome, user, zid, aposta);
        }

        void EnviarAlvo(string verbo, string id, string alvoNome, long user, uint zid, Aposta aposta)
        {
            EnviarPacote(verbo, id, alvoNome, user, zid, aposta);
        }

        void EnviarPacote(string verbo, string id, string alvoNome, long user, uint zid, Aposta aposta)
        {
            if (ZRoutedRpc.instance == null || Player.m_localPlayer == null || string.IsNullOrEmpty(id))
                return;
            ZPackage pkg = new ZPackage();
            pkg.Write(1);
            pkg.Write(verbo);
            pkg.Write(id);
            pkg.Write(Player.m_localPlayer.GetPlayerID());
            pkg.Write(Player.m_localPlayer.GetPlayerName());
            pkg.Write(alvoNome == null ? "" : alvoNome);
            pkg.Write(user);
            pkg.Write(zid);
            pkg.Write(aposta != null);
            if (aposta != null)
                EscreverAposta(pkg, aposta);
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "GaloDeGuerra", pkg);
        }

        static void EscreverAposta(ZPackage pkg, Aposta aposta)
        {
            pkg.Write(aposta.nome == null ? "" : aposta.nome);
            pkg.Write(aposta.qtd);
            pkg.Write(aposta.qualidade);
            pkg.Write(aposta.variante);
            pkg.Write(aposta.durabilidade);
            pkg.Write(aposta.crafterId);
            pkg.Write(aposta.crafterNome == null ? "" : aposta.crafterNome);
            pkg.Write(aposta.mundo);
        }

        static Aposta LerAposta(ZPackage pkg)
        {
            Aposta aposta = new Aposta();
            aposta.nome = pkg.ReadString();
            aposta.qtd = pkg.ReadInt();
            aposta.qualidade = pkg.ReadInt();
            aposta.variante = pkg.ReadInt();
            aposta.durabilidade = pkg.ReadSingle();
            aposta.crafterId = pkg.ReadLong();
            aposta.crafterNome = pkg.ReadString();
            aposta.mundo = pkg.ReadInt();
            return aposta;
        }

        List<ZNet.PlayerInfo> Gente()
        {
            List<ZNet.PlayerInfo> lista = new List<ZNet.PlayerInfo>();
            if (ZNet.instance == null || Player.m_localPlayer == null)
                return lista;
            List<ZNet.PlayerInfo> todos = ZNet.instance.GetPlayerList();
            if (todos == null)
                return lista;
            ZDOID meu = Player.m_localPlayer.GetZDOID();
            for (int i = 0; i < todos.Count; i++)
            {
                ZNet.PlayerInfo info = todos[i];
                if (string.IsNullOrEmpty(info.m_name) || info.m_characterID == null)
                    continue;
                if (meu != null && info.m_characterID == meu)
                    continue;
                lista.Add(info);
            }
            return lista;
        }

        bool AcharJogador(string nome, long user, uint zid, out ZNet.PlayerInfo achado)
        {
            achado = default(ZNet.PlayerInfo);
            List<ZNet.PlayerInfo> gente = Gente();
            for (int i = 0; i < gente.Count; i++)
            {
                ZNet.PlayerInfo info = gente[i];
                if (info.m_name == nome && info.m_characterID.UserID == user && info.m_characterID.ID == zid)
                {
                    achado = info;
                    return true;
                }
            }
            return false;
        }

        List<ItemDrop.ItemData> Itens(Player jogador)
        {
            List<ItemDrop.ItemData> lista = new List<ItemDrop.ItemData>();
            if (jogador == null || jogador.GetInventory() == null)
                return lista;
            List<ItemDrop.ItemData> todos = jogador.GetInventory().GetAllItems();
            if (todos == null)
                return lista;
            for (int i = 0; i < todos.Count; i++)
            {
                ItemDrop.ItemData item = todos[i];
                if (item == null || item.m_shared == null || item.m_stack <= 0)
                    continue;
                if (item.m_shared.m_questItem)
                    continue;
                lista.Add(item);
            }
            lista.Sort(delegate (ItemDrop.ItemData a, ItemDrop.ItemData b)
            {
                int c = Peso(NomeInterno(a)).CompareTo(Peso(NomeInterno(b)));
                if (c != 0)
                    return c;
                return NomeBonito(a.m_shared.m_name).CompareTo(NomeBonito(b.m_shared.m_name));
            });
            return lista;
        }

        static int Peso(string nome)
        {
            if (nome == "Coins")
                return 0;
            if (nome == "Ruby" || nome == "Amber" || nome == "AmberPearl" || nome == "Silver" || nome == "SilverNecklace")
                return 1;
            return 2;
        }

        ItemDrop.ItemData AcharItem(Player jogador)
        {
            List<ItemDrop.ItemData> itens = Itens(jogador);
            for (int i = 0; i < itens.Count; i++)
            {
                if (MesmoItem(itens[i]))
                    return itens[i];
            }
            return null;
        }

        bool MesmoItem(ItemDrop.ItemData item)
        {
            return item != null && NomeInterno(item) == selNome && item.m_quality == selQual && item.m_variant == selVar && item.m_gridPos.x == selX && item.m_gridPos.y == selY;
        }

        static Aposta Copiar(ItemDrop.ItemData item, int qtd)
        {
            Aposta aposta = new Aposta();
            aposta.nome = NomeInterno(item);
            aposta.qtd = qtd;
            aposta.qualidade = item.m_quality;
            aposta.variante = item.m_variant;
            aposta.durabilidade = item.m_durability;
            aposta.crafterId = item.m_crafterID;
            aposta.crafterNome = item.m_crafterName == null ? "" : item.m_crafterName;
            aposta.mundo = item.m_worldLevel;
            return aposta;
        }

        static string NomeInterno(ItemDrop.ItemData item)
        {
            if (item.m_dropPrefab != null && !string.IsNullOrEmpty(item.m_dropPrefab.name))
                return item.m_dropPrefab.name;
            return item.m_shared.m_name;
        }

        static string NomeBonito(string nome)
        {
            if (string.IsNullOrEmpty(nome))
                return "";
            if (Localization.instance == null)
                return nome;
            string direto = Localization.instance.Localize(nome);
            if (direto != nome)
                return direto;
            if (!nome.StartsWith("$"))
            {
                string token = "$item_" + nome.ToLowerInvariant();
                string s = Localization.instance.Localize(token);
                if (s != token)
                    return s;
            }
            return nome;
        }

        static string Texto(Aposta aposta)
        {
            if (aposta == null || aposta.qtd <= 0)
                return "nothing";
            string s = NomeBonito(aposta.nome);
            if (aposta.qualidade > 1)
                s = s + " lv" + aposta.qualidade;
            if (aposta.qtd > 1)
                s = s + " x" + aposta.qtd;
            return s;
        }

        static string TextoPremio(Aposta minha, Aposta deles)
        {
            string a = Texto(minha);
            string b = Texto(deles);
            if (a == "nothing")
                return b;
            if (b == "nothing")
                return a;
            return a + " and " + b;
        }

        void Avisar(string texto)
        {
            Avisar(texto, false);
        }

        void Avisar(string texto, bool guardar)
        {
            Logger.LogInfo(texto);
            if (MessageHud.instance == null)
                return;
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, texto, 0, null, true, guardar);
            if (guardar)
                MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, texto, 0, null, true, true);
        }

        bool JaResolvido(string id)
        {
            return resolvidos.Contains(id);
        }

        void Lembrar(string id)
        {
            if (string.IsNullOrEmpty(id) || resolvidos.Contains(id))
                return;
            resolvidos.Insert(0, id);
            while (resolvidos.Count > 30)
                resolvidos.RemoveAt(resolvidos.Count - 1);
        }

        string Arquivo()
        {
            return Path.Combine(Paths.ConfigPath, "yuri.GaloDeGuerra.contrato");
        }

        void Salvar()
        {
            try
            {
                ZPackage pkg = new ZPackage();
                pkg.Write(1);
                pkg.Write(resolvidos.Count);
                for (int i = 0; i < resolvidos.Count; i++)
                    pkg.Write(resolvidos[i]);
                pkg.Write(contrato != null);
                if (contrato != null)
                {
                    pkg.Write(contrato.id == null ? "" : contrato.id);
                    pkg.Write(contrato.estado);
                    pkg.Write(contrato.souDesafiante);
                    pkg.Write(contrato.oponenteId);
                    pkg.Write(contrato.oponenteNome == null ? "" : contrato.oponenteNome);
                    pkg.Write(contrato.criado);
                    pkg.Write(contrato.pediuDevolucao);
                    pkg.Write(contrato.pedidoRecebido);
                    pkg.Write(contrato.pedidoQuando);
                    pkg.Write(contrato.repetir == null ? "" : contrato.repetir);
                    pkg.Write(contrato.pendencia == null ? "" : contrato.pendencia);
                    pkg.Write(contrato.aviso == null ? "" : contrato.aviso);
                    pkg.Write(contrato.minhaPaga);
                    pkg.Write(contrato.delesPaga);
                    pkg.Write(contrato.minha != null);
                    if (contrato.minha != null)
                        EscreverAposta(pkg, contrato.minha);
                    pkg.Write(contrato.deles != null);
                    if (contrato.deles != null)
                        EscreverAposta(pkg, contrato.deles);
                }
                string path = Arquivo();
                string tmp = path + ".tmp";
                File.WriteAllBytes(tmp, pkg.GetArray());
                if (File.Exists(path))
                    File.Delete(path);
                File.Move(tmp, path);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Could not save the contract: " + ex.Message);
            }
        }

        void Carregar()
        {
            try
            {
                string path = Arquivo();
                if (!File.Exists(path))
                    return;
                ZPackage pkg = new ZPackage(File.ReadAllBytes(path));
                if (pkg.ReadInt() != 1)
                    return;
                int n = pkg.ReadInt();
                resolvidos.Clear();
                for (int i = 0; i < n; i++)
                    resolvidos.Add(pkg.ReadString());
                if (!pkg.ReadBool())
                {
                    contrato = null;
                    return;
                }
                contrato = new Contrato();
                contrato.id = pkg.ReadString();
                contrato.estado = pkg.ReadInt();
                contrato.souDesafiante = pkg.ReadBool();
                contrato.oponenteId = pkg.ReadLong();
                contrato.oponenteNome = pkg.ReadString();
                contrato.criado = pkg.ReadLong();
                contrato.pediuDevolucao = pkg.ReadBool();
                contrato.pedidoRecebido = pkg.ReadBool();
                contrato.pedidoQuando = pkg.ReadLong();
                contrato.repetir = pkg.ReadString();
                contrato.pendencia = pkg.ReadString();
                contrato.aviso = pkg.ReadString();
                contrato.minhaPaga = pkg.ReadBool();
                contrato.delesPaga = pkg.ReadBool();
                if (pkg.ReadBool())
                    contrato.minha = LerAposta(pkg);
                if (pkg.ReadBool())
                    contrato.deles = LerAposta(pkg);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Old contract ignored: " + ex.Message);
                contrato = null;
            }
        }

        static bool ConsoleAberto()
        {
            return Console.instance != null && Console.IsVisible();
        }

        static long Agora()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        public class Aposta
        {
            public string nome;
            public int qtd;
            public int qualidade;
            public int variante;
            public float durabilidade;
            public long crafterId;
            public string crafterNome;
            public int mundo;
        }

        public class Contrato
        {
            public string id;
            public int estado;
            public bool souDesafiante;
            public long oponenteId;
            public string oponenteNome;
            public long criado;
            public bool pediuDevolucao;
            public bool pedidoRecebido;
            public long pedidoQuando;
            public string repetir;
            public string pendencia;
            public string aviso;
            public bool minhaPaga;
            public bool delesPaga;
            public Aposta minha;
            public Aposta deles;
        }

        [HarmonyPatch(typeof(Character), "Damage")]
        public static class DanoPatch
        {
            static void Postfix(Character __instance, HitData hit)
            {
                if (Inst != null)
                    Inst.AoDano(__instance, hit);
            }
        }

        [HarmonyPatch(typeof(Player), "OnDeath")]
        public static class MortePatch
        {
            static void Postfix(Player __instance)
            {
                if (Inst != null)
                    Inst.AoMorrer(__instance);
            }
        }
    }
}
