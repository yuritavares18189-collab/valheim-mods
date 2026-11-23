using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProgressionTaskBar
{
    [BepInPlugin("yuri.ProgressionTaskBar", "Progression Task Bar", "1.0.6")]
    public class Plugin : BaseUnityPlugin
    {
        public static bool Aberta;
        private ConfigEntry<bool> ligado;
        private ConfigEntry<KeyboardShortcut> tecla;
        private bool aberto;
        private bool escolhendo;
        private Rect janela;
        private Vector2 rolagem;
        private bool posicionado;
        private Texture2D branco;
        private GUIStyle texto;
        private GUIStyle titulo;
        private GUIStyle miudo;
        private bool avisou;

        private static readonly Etapa[] etapas = new Etapa[]
        {
            new Etapa("defeated_eikthyr", "Meadows", "Eikthyr", "Deer in the meadows.", "TrophyDeer", "deer trophy", 2, "Antler pickaxe. The Black Forest truly opens."),
            new Etapa("defeated_gdking", "Black Forest", "The Elder", "Greydwarfs, brutes and shamans. Break the nests.", "AncientSeed", "ancient seed", 3, "Swamp key. The sunken crypts open."),
            new Etapa("defeated_bonemass", "Swamp", "Bonemass", "Draugr, skeletons, leeches and blobs in the crypts.", "WitheredBone", "withered bone", 10, "Wishbone. Mountain silver can be found."),
            new Etapa("defeated_dragon", "Mountain", "Moder", "Wolves, drakes and golems. The eggs sit in drake nests and do not go through portals.", "DragonEgg", "dragon egg", 3, "Dragon tear and the artisan table."),
            new Etapa("defeated_goblinking", "Plains", "Yagluth", "Fulings, berserkers and shamans in the villages.", "GoblinTotem", "fuling totem", 5, "Torn spirit and the wisp fountain."),
            new Etapa("defeated_queen", "Mistlands", "The Queen", "Seekers and seeker soldiers in the infested mines.", "TrophySeekerBrute", "seeker soldier trophy", 3, "Majestic carapace. The Ashlands come next."),
            new Etapa("defeated_fader", "Ashlands", "Fader", "The creatures and the ash fortresses, where the bells are.", "Bell", "bell", 3, "The way to the Deep North."),
            new Etapa("defeated_frozenking_p3", "Deep North", "Kall Fimbulbringer", "Open Morkhalla with the intricate key, break the hateful ice and win the jotun raid. Do this three times. Kall has three phases.", "HatefulBlood", "hateful blood", 3, "Clears the game.")
        };

        private void Awake()
        {
            ligado = Config.Bind("General", "Enabled", true, "Turns the window on in this game. While it is closed, it does not take the mouse. Only your own game needs this mod.");
            tecla = Config.Bind("General", "Key", new KeyboardShortcut(KeyCode.F6), "Click this option and press a key. The Choose key button inside the window does the same.");
            janela = new Rect(80f, 60f, 680f, 680f);
            branco = new Texture2D(1, 1);
            branco.SetPixel(0, 0, Color.white);
            branco.Apply();
            branco.hideFlags = HideFlags.HideAndDontSave;
            LigarMouse();
            Logger.LogInfo("Progress window ready. Key: " + tecla.Value);
        }

        private void Update()
        {
            Aberta = ligado.Value && aberto;
            if (!ligado.Value)
                return;
            if (aberto)
                AplicarRoda();
            if (escolhendo)
                return;
            if (aberto && Input.GetKeyDown(KeyCode.Escape))
            {
                aberto = false;
                Aberta = false;
                return;
            }
            if (!tecla.Value.IsDown())
                return;
            aberto = !aberto;
            Aberta = aberto;
        }

        private void OnGUI()
        {
            if (!ligado.Value)
                return;
            if (!aberto)
                return;
            Matrix4x4 matriz = GUI.matrix;
            Color antiga = GUI.color;
            GUI.matrix = Matrix4x4.identity;
            try
            {
                if (escolhendo)
                    CapturarTecla();
                GarantirEstilo();
                if (!posicionado)
                {
                    janela.x = (Screen.width - janela.width) * 0.5f;
                    janela.y = (Screen.height - janela.height) * 0.5f;
                    posicionado = true;
                }
                janela = GUI.Window(48189, janela, Desenhar, "Progression Task Bar");
            }
            catch (Exception ex)
            {
                if (!avisou)
                {
                    avisou = true;
                    Logger.LogError("Window failed: " + ex.Message);
                }
            }
            finally
            {
                GUI.color = antiga;
                GUI.matrix = matriz;
            }
        }

        private void AplicarRoda()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
                return;
            float roda = mouse.scroll.ReadValue().y;
            if (roda > -0.01f && roda < 0.01f)
                return;
            if (roda > 10f || roda < -10f)
                roda = roda / 120f;
            Vector2 ponteiro = mouse.position.ReadValue();
            ponteiro.y = Screen.height - ponteiro.y;
            if (!janela.Contains(ponteiro))
                return;
            rolagem.y -= roda * 56f;
            if (rolagem.y < 0f)
                rolagem.y = 0f;
        }

        private void LigarMouse()
        {
            Harmony harmonia = new Harmony("yuri.ProgressionTaskBar");
            BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            harmonia.Patch(AccessTools.Method(typeof(GameCamera), "UpdateMouseCapture"), postfix: new HarmonyMethod(typeof(Plugin).GetMethod("SoltaCursor", flags)));
            harmonia.Patch(AccessTools.Method(typeof(Player), "TakeInput"), postfix: new HarmonyMethod(typeof(Plugin).GetMethod("SemJogo", flags)));
            harmonia.Patch(AccessTools.Method(typeof(ZInput), "GetMouseDelta"), prefix: new HarmonyMethod(typeof(Plugin).GetMethod("BloqueiaDelta", flags)));
            harmonia.Patch(AccessTools.Method(typeof(ZInput), "GetMouseScrollWheel"), prefix: new HarmonyMethod(typeof(Plugin).GetMethod("BloqueiaRoda", flags)));
            harmonia.Patch(AccessTools.Method(typeof(ZInput), "GetMouseButton"), prefix: new HarmonyMethod(typeof(Plugin).GetMethod("BloqueiaBool", flags)));
            harmonia.Patch(AccessTools.Method(typeof(ZInput), "GetMouseButtonDown"), prefix: new HarmonyMethod(typeof(Plugin).GetMethod("BloqueiaBool", flags)));
            harmonia.Patch(AccessTools.Method(typeof(ZInput), "GetMouseButtonUp"), prefix: new HarmonyMethod(typeof(Plugin).GetMethod("BloqueiaBool", flags)));
        }

        private static void SoltaCursor()
        {
            if (!Aberta)
                return;
            ZCursor.LockState = CursorLockMode.None;
            ZCursor.Show();
        }

        private static void SemJogo(ref bool __result)
        {
            if (Aberta)
                __result = false;
        }

        private static bool BloqueiaBool(ref bool __result)
        {
            if (!Aberta)
                return true;
            __result = false;
            return false;
        }

        private static bool BloqueiaRoda(ref float __result)
        {
            if (!Aberta)
                return true;
            __result = 0f;
            return false;
        }

        private static bool BloqueiaDelta(ref Vector2 __result)
        {
            if (!Aberta)
                return true;
            __result = Vector2.zero;
            return false;
        }

        private void Desenhar(int id)
        {
            int feitos;
            int atual;
            ContarProgresso(out feitos, out atual);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Valheim story. " + feitos + " of " + etapas.Length + " bosses.", titulo);
            if (GUILayout.Button(escolhendo ? "Press the key" : "Choose key", GUILayout.Width(140f)))
                escolhendo = true;
            if (GUILayout.Button("Close", GUILayout.Width(90f)))
                aberto = false;
            GUILayout.EndHorizontal();

            Barra(feitos / (float)etapas.Length, new Color(0.9f, 0.62f, 0.18f));
            if (escolhendo)
                GUILayout.Label("Press the new key now. Esc cancels. Shift, Ctrl or Alt can go with it.", titulo);
            else
                GUILayout.Label(NomeTecla() + " opens and closes. Esc also closes. Outside the window the game works as usual. Choose key saves the next key you press.", miudo);
            GUILayout.Space(6f);

            rolagem = GUILayout.BeginScrollView(rolagem);
            for (int i = 0; i < etapas.Length; i++)
            {
                bool morto = ChefeMorto(etapas[i].chave);
                int tem = Contar(etapas[i].itemId);
                float fator = Fator(morto, tem, etapas[i].precisa);
                string estado = morto ? "Done" : (i == atual ? "Now" : "Later");
                Color cor = morto ? new Color(0.35f, 0.72f, 0.4f) : (i == atual ? new Color(0.9f, 0.72f, 0.28f) : new Color(0.45f, 0.42f, 0.38f));

                GUILayout.Label((i + 1) + ". " + etapas[i].bioma + "  -  " + etapas[i].chefe + "   [" + estado + "]", titulo);
                Barra(fator, cor);
                GUILayout.Label("Kill: " + etapas[i].matar, texto);
                GUILayout.Label("Collect: " + tem + " / " + etapas[i].precisa + " " + etapas[i].itemNome + ". Offer them at the altar and kill the boss.", texto);
                GUILayout.Label("Opens: " + etapas[i].abre, texto);
                GUILayout.Space(8f);
            }
            GUILayout.EndScrollView();
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 28f));
        }

        private static void ContarProgresso(out int feitos, out int atual)
        {
            feitos = 0;
            atual = -1;
            for (int i = 0; i < etapas.Length; i++)
            {
                if (ChefeMorto(etapas[i].chave))
                    feitos++;
                else if (atual < 0)
                    atual = i;
            }
        }

        private static float Fator(bool morto, int tem, int precisa)
        {
            if (morto)
                return 1f;
            if (precisa <= 0)
                return 0f;
            float itens = tem / (float)precisa;
            if (itens < 0f)
                itens = 0f;
            if (itens > 1f)
                itens = 1f;
            return itens * 0.7f;
        }

        private void Barra(float fator, Color cor)
        {
            if (fator < 0f)
                fator = 0f;
            if (fator > 1f)
                fator = 1f;
            Rect r = GUILayoutUtility.GetRect(18f, 16f);
            Color antiga = GUI.color;
            GUI.color = new Color(0.12f, 0.1f, 0.08f, 0.95f);
            GUI.DrawTexture(r, branco);
            GUI.color = cor;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width * fator, r.height), branco);
            GUI.color = antiga;
        }

        private void CapturarTecla()
        {
            Event e = Event.current;
            if (e == null || e.type != EventType.KeyDown || e.keyCode == KeyCode.None)
                return;
            if (e.keyCode == KeyCode.Escape)
            {
                escolhendo = false;
                e.Use();
                return;
            }
            if (EhModificador(e.keyCode))
                return;
            if (e.shift)
                tecla.Value = new KeyboardShortcut(e.keyCode, new KeyCode[] { KeyCode.LeftShift });
            else if (e.control)
                tecla.Value = new KeyboardShortcut(e.keyCode, new KeyCode[] { KeyCode.LeftControl });
            else if (e.alt)
                tecla.Value = new KeyboardShortcut(e.keyCode, new KeyCode[] { KeyCode.LeftAlt });
            else
                tecla.Value = new KeyboardShortcut(e.keyCode);
            escolhendo = false;
            e.Use();
            Logger.LogInfo("New key: " + tecla.Value);
        }

        private static bool EhModificador(KeyCode codigo)
        {
            return codigo == KeyCode.LeftShift || codigo == KeyCode.RightShift
                || codigo == KeyCode.LeftControl || codigo == KeyCode.RightControl
                || codigo == KeyCode.LeftAlt || codigo == KeyCode.RightAlt;
        }

        private string NomeTecla()
        {
            if (tecla.Value.MainKey == KeyCode.None)
                return "no key";
            return tecla.Value.ToString();
        }

        private void GarantirEstilo()
        {
            if (texto != null)
                return;
            texto = new GUIStyle(GUI.skin.label);
            Pintar(texto, 14, new Color(0.93f, 0.88f, 0.78f));
            titulo = new GUIStyle(GUI.skin.label);
            Pintar(titulo, 16, new Color(0.96f, 0.84f, 0.55f));
            miudo = new GUIStyle(GUI.skin.label);
            Pintar(miudo, 12, new Color(0.75f, 0.7f, 0.62f));
        }

        private static void Pintar(GUIStyle estilo, int tamanho, Color cor)
        {
            estilo.wordWrap = true;
            estilo.fontSize = tamanho;
            estilo.normal.textColor = cor;
        }

        private static bool ChefeMorto(string chave)
        {
            if (ZoneSystem.instance == null || chave == null)
                return false;
            return ZoneSystem.instance.GetGlobalKey(chave);
        }

        private static int Contar(string item)
        {
            Player jogador = Player.m_localPlayer;
            if (jogador == null || item == null)
                return 0;
            Inventory inv = jogador.GetInventory();
            if (inv == null)
                return 0;
            return inv.CountItems(item);
        }

        private class Etapa
        {
            public string chave;
            public string bioma;
            public string chefe;
            public string matar;
            public string itemId;
            public string itemNome;
            public int precisa;
            public string abre;

            public Etapa(string chave, string bioma, string chefe, string matar, string itemId, string itemNome, int precisa, string abre)
            {
                this.chave = chave;
                this.bioma = bioma;
                this.chefe = chefe;
                this.matar = matar;
                this.itemId = itemId;
                this.itemNome = itemNome;
                this.precisa = precisa;
                this.abre = abre;
            }
        }
    }
}
