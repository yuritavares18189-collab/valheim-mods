using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace MonstrosDoRio
{
    [BepInPlugin("yuri.MonstrosDoRio", "Monstros do Rio", "1.0.14")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Inst;
        public static Recipe Receita;

        const string NomeVara = "VaraMonstrosDoRio";
        const float Pista = 568f;

        ConfigEntry<bool> Ligado;

        enum Fase
        {
            Nada = 0,
            Forca = 1,
            Espera = 2,
            Fisgou = 3,
            Barra = 4,
            Aviso = 5
        }

        Fase fase;
        bool esperandoSoltar;
        float forca;
        float espera;
        float janela;
        float aviso;
        float habilidade;
        float dificuldade;
        int jeito;
        float barraY;
        float barraH;
        float velBarra;
        float peixeY;
        float peixeAlvo;
        float peixeVel;
        float peixeAcel;
        float peixeTempo;
        float tickAcum;
        float progresso;
        string nomePremio = "";
        string itemA;
        string itemB;
        string itemC;
        int qtdA;
        float chanceB;
        float chanceC;
        string textoAviso = "";
        Vector3 boia;
        Texture2D pixel;
        GUIStyle estilo;
        GUIStyle estiloGrande;
        bool listaPronta;
        bool receitaPronta;
        bool falouOnde;
        bool viuVara;
        bool entrouNaLista;
        GameObject boiaVisivel;
        GameObject peixePulo;
        LineRenderer linha;
        Transform pontaVara;
        Transform ossoVara;
        Quaternion ossoOriginal;
        bool ossoGuardado;
        List<string> peixes = new List<string>();
        List<string> monstros = new List<string>();

        void Awake()
        {
            Inst = this;
            Ligado = Config.Bind("Geral", "Ligado", true, "Liga a vara Monstros do Rio neste jogo. So no seu jogo.");
            Harmony harmonia = new Harmony("yuri.MonstrosDoRio");
            harmonia.PatchAll(typeof(Plugin).Assembly);
            if (ObjectDB.instance != null)
                Registrar(ObjectDB.instance);
            Logger.LogInfo("Monstros do Rio 1.0.14");
        }

        void Update()
        {
            if (Inst != this)
                return;
            if (!Ligado.Value)
            {
                if (fase != Fase.Nada)
                    Encerrar("", false);
                return;
            }

            if (!receitaPronta && ObjectDB.instance != null)
                Registrar(ObjectDB.instance);
            if (!falouOnde && receitaPronta && Player.m_localPlayer != null)
            {
                falouOnde = true;
                Avisar("Vara de pescar no topo da Producao. Clique esquerdo joga. Segura sobe a barra, solta desce.", true);
            }

            Player jogador = Player.m_localPlayer;
            if (jogador == null || jogador.IsDead())
            {
                if (fase != Fase.Nada)
                    Encerrar("", false);
                return;
            }

            if (!ComVara(jogador))
            {
                viuVara = false;
                if (fase != Fase.Nada)
                    Encerrar("Guardou a vara.", true);
                return;
            }
            if (!viuVara)
            {
                viuVara = true;
                Logger.LogInfo("Vara equipada");
            }

            if (Console.IsVisible() || InventoryGui.IsVisible())
            {
                if (fase == Fase.Forca)
                    fase = Fase.Nada;
                if (fase == Fase.Espera || fase == Fase.Fisgou || fase == Fase.Barra)
                    Encerrar("A linha folgou.", true);
                return;
            }

            bool segura = Segurando();
            bool desceu = Apertou();
            bool soltou = Soltou();
            float dt = Time.deltaTime;

            if (fase == Fase.Nada)
            {
                if (esperandoSoltar)
                {
                    if (!segura)
                        esperandoSoltar = false;
                    return;
                }
                if (desceu)
                {
                    forca = 0.7f;
                    Soltar(jogador);
                }
                return;
            }

            if (ZInput.GetKeyDown(KeyCode.Escape))
            {
                Encerrar("Voce recolheu a linha.", true);
                esperandoSoltar = true;
                return;
            }

            if (fase == Fase.Espera)
            {
                if (esperandoSoltar && !segura)
                    esperandoSoltar = false;
                espera -= dt;
                if (espera <= 0f)
                {
                    fase = Fase.Fisgou;
                    janela = 1.35f + habilidade * 0.7f;
                    Avisar("Mordeu! Segura o botao esquerdo.", false);
                    PularPeixe();
                }
                AtualizarCena(jogador, 0.16f);
                return;
            }

            if (fase == Fase.Fisgou)
            {
                if (esperandoSoltar)
                {
                    if (!segura)
                        esperandoSoltar = false;
                }
                else if (segura || desceu)
                    ComecarBarra();
                janela -= dt;
                if (fase == Fase.Barra)
                {
                    AtualizarBarra(jogador, dt, Segurando());
                    if (fase == Fase.Barra)
                        AtualizarCena(jogador, Segurando() ? 0.9f : 0.25f);
                    return;
                }
                if (janela <= 0f)
                {
                    Encerrar("Demorou. O bicho soltou.", true);
                    esperandoSoltar = true;
                    return;
                }
                AtualizarCena(jogador, 0.7f + Mathf.Abs(Mathf.Sin(Time.time * 16f)) * 0.3f);
                return;
            }

            if (fase == Fase.Barra)
            {
                AtualizarBarra(jogador, dt, segura);
                if (fase == Fase.Barra)
                {
                    bool dentro = PeixeNaBarra();
                    AtualizarCena(jogador, dentro ? 0.32f : 0.92f);
                }
                return;
            }

            if (fase == Fase.Aviso)
            {
                aviso -= dt;
                if (aviso <= 0f || desceu)
                {
                    fase = Fase.Nada;
                    esperandoSoltar = segura;
                }
            }
        }

        void Soltar(Player jogador)
        {
            fase = Fase.Nada;
            if (!TemAgua(jogador, forca, out boia))
            {
                Avisar("Nao tem agua ai. Olha pra agua e solta o ataque.", true);
                esperandoSoltar = true;
                return;
            }
            if (!jogador.HaveStamina(5f))
            {
                Avisar("Sem folego pra arremessar.", true);
                esperandoSoltar = true;
                return;
            }

            MontarLista();
            if (!Sortear(jogador))
            {
                Avisar("Nada mordeu.", true);
                esperandoSoltar = true;
                return;
            }

            jogador.UseStamina(5f);
            habilidade = jogador.GetSkillFactor(Skills.SkillType.Fishing);
            if (habilidade < 0f)
                habilidade = 0f;
            if (habilidade > 1f)
                habilidade = 1f;
            float esperaMin = 0.7f - habilidade * 0.25f;
            float esperaMax = 2.6f - habilidade * 0.7f;
            if (esperaMin < 0.35f)
                esperaMin = 0.35f;
            if (esperaMax < esperaMin + 0.3f)
                esperaMax = esperaMin + 0.3f;
            espera = Random.Range(esperaMin, esperaMax);
            fase = Fase.Espera;
            esperandoSoltar = true;
            CriarCena(jogador);
            Logger.LogInfo("Arremessou " + nomePremio);
        }

        bool Sortear(Player jogador)
        {
            itemA = null;
            itemB = null;
            itemC = null;
            qtdA = 1;
            chanceB = 0f;
            chanceC = 0f;
            Heightmap.Biome bioma = Heightmap.FindBiome(boia);
            bool mar = bioma == Heightmap.Biome.Ocean;
            float cSerp = (mar ? 0.16f : 0.05f) + forca * 0.08f;
            float cLev = mar ? 0.06f + forca * 0.04f : 0.01f;
            float cMon = 0.22f + forca * 0.06f;
            float giro = Random.value;

            if (giro < cSerp && Existe("SerpentMeat"))
            {
                nomePremio = "Serpente marinha";
                itemA = "SerpentMeat";
                qtdA = 1;
                if (Existe("SerpentScale"))
                {
                    itemB = "SerpentScale";
                    chanceB = 0.55f;
                }
                if (Existe("TrophySerpent"))
                {
                    itemC = "TrophySerpent";
                    chanceC = 0.1f;
                }
                dificuldade = 110f;
                jeito = 1;
                return true;
            }

            if (giro < cSerp + cLev && Existe("Chitin"))
            {
                itemA = "Chitin";
                nomePremio = NomeDe(itemA);
                dificuldade = 95f;
                jeito = 3;
                return true;
            }

            if (giro < cSerp + cLev + cMon && monstros.Count > 0)
            {
                itemA = monstros[Random.Range(0, monstros.Count)];
                nomePremio = NomeDe(itemA);
                if (itemA == "Bloodbag" || itemA == "Leech")
                {
                    dificuldade = 85f;
                    jeito = 1;
                }
                else
                {
                    dificuldade = 65f;
                    jeito = 0;
                }
                return true;
            }

            if (peixes.Count == 0)
            {
                if (!Existe("SerpentMeat"))
                    return false;
                nomePremio = "Serpente marinha";
                itemA = "SerpentMeat";
                dificuldade = 110f;
                jeito = 1;
                return true;
            }
            itemA = peixes[Random.Range(0, peixes.Count)];
            nomePremio = NomeDe(itemA);
            dificuldade = Random.Range(35f, 90f);
            jeito = Random.Range(0, 5);
            return true;
        }

        void ComecarBarra()
        {
            float nivel = habilidade * 10f;
            if (nivel < 0f)
                nivel = 0f;
            if (nivel > 10f)
                nivel = 10f;
            barraH = 168f + nivel * 14f;
            if (barraH > 320f)
                barraH = 320f;
            barraY = Pista - barraH;
            velBarra = 0f;
            peixeY = 508f;
            peixeVel = 0f;
            peixeAcel = 0f;
            peixeTempo = 0f;
            peixeAlvo = (100f - dificuldade) / 100f * 548f;
            progresso = 0.3f;
            tickAcum = 0f;
            fase = Fase.Barra;
        }

        void AtualizarBarra(Player jogador, float dt, bool segura)
        {
            tickAcum += dt;
            if (tickAcum > 0.05f)
                tickAcum = 0.05f;
            while (tickAcum >= 1f / 60f)
            {
                tickAcum -= 1f / 60f;
                PassoPesca(jogador, segura);
                if (fase != Fase.Barra)
                    return;
            }
        }

        void PassoPesca(Player jogador, bool segura)
        {
            MoverPeixe();
            bool dentro = PeixeNaBarra();
            float grav = segura ? -0.25f : 0.25f;
            float teto = Pista - barraH;
            if (segura && (barraY <= 0f || barraY >= teto))
                velBarra = 0f;
            if (dentro)
                grav *= 0.6f;
            velBarra += grav;
            barraY += velBarra;
            if (barraY > teto)
            {
                barraY = teto;
                velBarra = -velBarra * (2f / 3f);
            }
            else if (barraY < 0f)
            {
                barraY = 0f;
                velBarra = -velBarra * (2f / 3f);
            }
            if (dentro)
                progresso += 0.002f;
            else
                progresso -= 0.003f;
            if (progresso > 1f)
                progresso = 1f;
            if (progresso >= 1f)
            {
                Entregar(jogador);
                return;
            }
            if (progresso <= 0f)
            {
                jogador.RaiseSkill(Skills.SkillType.Fishing, 0.15f);
                Encerrar(nomePremio + " escapou.", true);
                esperandoSoltar = true;
            }
        }

        void MoverPeixe()
        {
            float mult = (jeito == 2) ? 20f : 1f;
            if (Random.value < dificuldade * mult / 4000f && (jeito != 2 || peixeAlvo < 0f))
            {
                float abaixo = 548f - peixeY;
                float num3 = Mathf.Min(99f, dificuldade + Random.Range(10, 45)) / 100f;
                int menor = (int)Mathf.Min(-peixeY, abaixo);
                int maior = (int)abaixo;
                if (menor < maior)
                    peixeAlvo = peixeY + Random.Range(menor, maior) * num3;
                else
                    peixeAlvo = peixeY;
            }
            if (jeito == 4)
                peixeTempo = Mathf.Max(peixeTempo - 0.01f, -1.5f);
            else if (jeito == 3)
                peixeTempo = Mathf.Min(peixeTempo + 0.01f, 1.5f);
            if (Mathf.Abs(peixeY - peixeAlvo) > 3f && peixeAlvo >= 0f)
            {
                float denom = Random.Range(10, 30) + (100f - Mathf.Min(100f, dificuldade));
                peixeAcel = (peixeAlvo - peixeY) / denom;
                peixeVel += (peixeAcel - peixeVel) / 5f;
            }
            else if (jeito != 2 && Random.value < dificuldade / 2000f)
            {
                float salto = (Random.value < 0.5f) ? Random.Range(-100, -50) : Random.Range(50, 101);
                peixeAlvo = peixeY + salto;
            }
            else
                peixeAlvo = -1f;
            if (jeito == 1 && Random.value < dificuldade / 1000f)
            {
                int dif = (int)dificuldade;
                int salto = (Random.value < 0.5f) ? Proximo(-100 - dif * 2, -50) : Proximo(50, 101 + dif * 2);
                peixeAlvo = peixeY + salto;
            }
            if (peixeAlvo < -1f)
                peixeAlvo = -1f;
            if (peixeAlvo > 548f)
                peixeAlvo = 548f;
            peixeY += peixeVel + peixeTempo;
            if (peixeY > 532f)
                peixeY = 532f;
            else if (peixeY < 0f)
                peixeY = 0f;
        }

        static int Proximo(int menor, int maior)
        {
            if (menor >= maior)
                return maior;
            return Random.Range(menor, maior);
        }

        bool PeixeNaBarra()
        {
            bool noMeio = peixeY + 12f <= barraY - 32f + barraH && peixeY - 16f >= barraY - 32f;
            bool noFundo = peixeY >= 548f - barraH && barraY >= Pista - barraH - 4f;
            return noMeio || noFundo;
        }

        void Entregar(Player jogador)
        {
            string ganhos = "";
            bool pegou = Colocar(jogador, itemA, qtdA, ref ganhos);
            if (pegou && itemB != null && Random.value < chanceB)
                Colocar(jogador, itemB, 1, ref ganhos);
            if (pegou && itemC != null && Random.value < chanceC)
                Colocar(jogador, itemC, 1, ref ganhos);
            if (!pegou)
            {
                Encerrar("Bolsa cheia. O bicho escapou com o premio.", true);
                esperandoSoltar = true;
                return;
            }
            jogador.RaiseSkill(Skills.SkillType.Fishing, 1.15f);
            textoAviso = "Voce pescou: " + ganhos + ".";
            Avisar(textoAviso, true);
            fase = Fase.Aviso;
            aviso = 2.4f;
            esperandoSoltar = true;
        }

        bool Colocar(Player jogador, string prefab, int qtd, ref string ganhos)
        {
            if (string.IsNullOrEmpty(prefab) || qtd <= 0)
                return false;
            GameObject go = ObjectDB.instance.GetItemPrefab(prefab);
            if (go == null)
                return false;
            Inventory bolsa = jogador.GetInventory();
            if (bolsa == null || !bolsa.CanAddItem(go, qtd))
                return false;
            if (!bolsa.AddItem(go, qtd))
                return false;
            string bonito = NomeDe(prefab);
            if (qtd > 1)
                bonito = bonito + " x" + qtd;
            if (ganhos.Length == 0)
                ganhos = bonito;
            else
                ganhos = ganhos + " e " + bonito;
            return true;
        }

        void Encerrar(string texto, bool mostrar)
        {
            fase = Fase.Nada;
            LimparCena();
            if (mostrar && !string.IsNullOrEmpty(texto))
                Avisar(texto, true);
        }

        void Avisar(string texto, bool guardar)
        {
            if (string.IsNullOrEmpty(texto))
                return;
            Logger.LogInfo(texto);
            if (MessageHud.instance == null)
                return;
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, texto, 0, null, true, guardar);
            if (guardar)
                MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, texto, 0, null, true, true);
        }

        void CriarCena(Player jogador)
        {
            LimparCena();
            boiaVisivel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            boiaVisivel.name = "BoiaMonstros";
            boiaVisivel.transform.position = boia;
            boiaVisivel.transform.localScale = new Vector3(0.22f, 0.16f, 0.22f);
            Collider colisor = boiaVisivel.GetComponent<Collider>();
            if (colisor != null)
                UnityEngine.Object.Destroy(colisor);
            Renderer bola = boiaVisivel.GetComponent<Renderer>();
            if (bola != null)
                bola.material.color = new Color(0.82f, 0.12f, 0.1f);
            GameObject goLinha = new GameObject("LinhaMonstros");
            linha = goLinha.AddComponent<LineRenderer>();
            linha.positionCount = 8;
            linha.startWidth = 0.012f;
            linha.endWidth = 0.008f;
            Shader tinta = Shader.Find("Sprites/Default");
            if (tinta == null)
                tinta = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (tinta != null)
                linha.material = new Material(tinta);
            linha.startColor = new Color(0.9f, 0.9f, 0.82f);
            linha.endColor = linha.startColor;
            AcharVara(jogador);
        }

        void PularPeixe()
        {
            if (peixePulo != null)
                UnityEngine.Object.Destroy(peixePulo);
            peixePulo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            peixePulo.name = "PuloMonstros";
            peixePulo.transform.localScale = new Vector3(0.16f, 0.34f, 0.16f);
            peixePulo.transform.rotation = Quaternion.Euler(70f, 0f, 0f);
            Collider colisor = peixePulo.GetComponent<Collider>();
            if (colisor != null)
                UnityEngine.Object.Destroy(colisor);
            Renderer corpo = peixePulo.GetComponent<Renderer>();
            if (corpo != null)
                corpo.material.color = new Color(0.95f, 0.45f, 0.12f);
        }

        void AcharVara(Player jogador)
        {
            pontaVara = null;
            ossoVara = null;
            ossoGuardado = false;
            if (jogador == null)
                return;
            Transform[] todos = jogador.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < todos.Length; i++)
            {
                if (todos[i].name == "_RodTop")
                {
                    pontaVara = todos[i];
                    break;
                }
            }
            if (pontaVara == null)
                return;
            Transform sobe = pontaVara.parent;
            while (sobe != null && sobe != jogador.transform)
            {
                string n = sobe.name.ToLowerInvariant();
                if (n.Contains("rod") || n.Contains("fishing") || n.Contains("vara"))
                {
                    ossoVara = sobe;
                    break;
                }
                sobe = sobe.parent;
            }
            if (ossoVara == null)
                ossoVara = pontaVara.parent;
            if (ossoVara != null && ossoVara != jogador.transform)
            {
                ossoOriginal = ossoVara.localRotation;
                ossoGuardado = true;
            }
        }

        void AtualizarCena(Player jogador, float tencao)
        {
            if (tencao < 0f)
                tencao = 0f;
            if (tencao > 1f)
                tencao = 1f;
            if (boiaVisivel == null)
                CriarCena(jogador);
            if (pontaVara == null)
                AcharVara(jogador);
            float sobe = Mathf.Sin(Time.time * 3f) * 0.05f;
            Vector3 ponto = boia;
            ponto.y += sobe - tencao * 0.62f;
            if (boiaVisivel != null)
                boiaVisivel.transform.position = ponto;
            if (peixePulo != null)
            {
                float altura = Mathf.Sin(Time.time * 9f);
                if (altura < 0f)
                    altura = 0f;
                Vector3 salto = boia;
                salto.y += 0.25f + altura * (0.35f + tencao * 1.1f);
                peixePulo.transform.position = salto;
                peixePulo.transform.rotation = Quaternion.Euler(70f - tencao * 40f, Time.time * 40f, 0f);
            }
            Vector3 origem = jogador.transform.position + Vector3.up * 1.4f;
            if (pontaVara != null)
                origem = pontaVara.position;
            if (linha != null)
            {
                float folga = (1f - tencao) * 0.85f;
                for (int i = 0; i < 8; i++)
                {
                    float t = i / 7f;
                    Vector3 p = Vector3.Lerp(origem, ponto, t);
                    p.y -= Mathf.Sin(t * 3.14159f) * folga;
                    linha.SetPosition(i, p);
                }
            }
            if (ossoGuardado && ossoVara != null)
            {
                float curva = tencao * 38f;
                float tremer = 0f;
                if (tencao > 0.5f)
                    tremer = Mathf.Sin(Time.time * 22f) * 6f * tencao;
                ossoVara.localRotation = ossoOriginal * Quaternion.Euler(curva + tremer, 0f, 0f);
            }
        }

        void LimparCena()
        {
            if (ossoGuardado && ossoVara != null)
                ossoVara.localRotation = ossoOriginal;
            ossoGuardado = false;
            ossoVara = null;
            pontaVara = null;
            if (boiaVisivel != null)
                UnityEngine.Object.Destroy(boiaVisivel);
            boiaVisivel = null;
            if (peixePulo != null)
                UnityEngine.Object.Destroy(peixePulo);
            peixePulo = null;
            if (linha != null)
                UnityEngine.Object.Destroy(linha.gameObject);
            linha = null;
        }

        bool TemAgua(Player jogador, float potencia, out Vector3 ponto)
        {
            ponto = jogador.transform.position;
            Vector3 dir = jogador.GetLookDir();
            if (GameCamera.instance != null)
                dir = GameCamera.instance.transform.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f)
                dir = jogador.transform.forward;
            dir.Normalize();
            if (jogador.InWater())
            {
                Vector3 pe = jogador.transform.position;
                Vector3 superficiePe;
                if (Superficie(pe, out superficiePe))
                {
                    ponto = superficiePe;
                    return true;
                }
            }
            float dist = 2f + potencia * 18f;
            float melhor = -1f;
            Vector3 achou = ponto;
            float passo = dist;
            while (passo >= 1.2f)
            {
                Vector3 teste = jogador.transform.position + dir * passo;
                Vector3 superficie;
                if (Superficie(teste, out superficie))
                {
                    float dy = jogador.transform.position.y - superficie.y;
                    if (dy < 18f && dy > -6f && passo > melhor)
                    {
                        melhor = passo;
                        achou = superficie;
                    }
                }
                passo -= 1.2f;
            }
            if (melhor < 0f)
                return false;
            ponto = achou;
            return true;
        }

        bool Superficie(Vector3 teste, out Vector3 superficie)
        {
            superficie = teste;
            WaterVolume volume = null;
            float agua = Floating.GetWaterLevel(teste, ref volume);
            if (volume != null && volume.GetLiquidType() != LiquidType.Water)
                return false;
            float chao = teste.y;
            if (ZoneSystem.instance != null)
                chao = ZoneSystem.instance.GetGroundHeight(teste);
            if (agua < chao - 0.05f)
                return false;
            superficie = new Vector3(teste.x, agua, teste.z);
            return true;
        }

        void MontarLista()
        {
            if (listaPronta || ObjectDB.instance == null)
                return;
            peixes.Clear();
            monstros.Clear();
            foreach (GameObject go in ObjectDB.instance.m_items)
            {
                if (go == null)
                    continue;
                ItemDrop drop = go.GetComponent<ItemDrop>();
                if (drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null)
                    continue;
                if (drop.m_itemData.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Fish)
                    continue;
                string n = go.name.ToLowerInvariant();
                if (n.Contains("bait") || n.Contains("rod") || n.Contains("cooked") || n.Contains("grilled"))
                    continue;
                peixes.Add(go.name);
            }
            if (peixes.Count == 0)
            {
                string[] reserva = new string[] {
                    "FishRaw", "Fish1", "Fish2", "Fish3", "Fish4_cave", "Fish5", "Fish6",
                    "Fish7", "Fish8", "Fish9", "Fish10", "Fish11", "Fish12"
                };
                for (int i = 0; i < reserva.Length; i++)
                {
                    if (Existe(reserva[i]))
                        peixes.Add(reserva[i]);
                }
            }
            string[] bruto = new string[] { "NeckTail", "Bloodbag", "Leech" };
            for (int i = 0; i < bruto.Length; i++)
            {
                if (Existe(bruto[i]))
                    monstros.Add(bruto[i]);
            }
            listaPronta = true;
            Logger.LogInfo("Peixes " + peixes.Count + ", monstros " + monstros.Count);
        }

        bool Existe(string nome)
        {
            return ObjectDB.instance != null && ObjectDB.instance.GetItemPrefab(nome) != null;
        }

        string NomeDe(string prefab)
        {
            if (ObjectDB.instance == null)
                return prefab;
            GameObject go = ObjectDB.instance.GetItemPrefab(prefab);
            if (go == null)
                return prefab;
            ItemDrop drop = go.GetComponent<ItemDrop>();
            if (drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null)
                return prefab;
            string nome = drop.m_itemData.m_shared.m_name;
            if (Localization.instance != null)
                nome = Localization.instance.Localize(nome);
            return nome;
        }

        public static bool ComVara(Player jogador)
        {
            if (jogador == null)
                return false;
            ItemDrop.ItemData item = jogador.RightItem;
            if (item == null || item.m_shared == null)
                return false;
            if (item.m_shared.m_name == "River Monsters Rod" || item.m_shared.m_name == "Vara Monstros do Rio")
                return true;
            if (item.m_shared.m_name == "$item_fishingrod")
                return true;
            if (item.m_dropPrefab == null)
                return false;
            string nome = item.m_dropPrefab.name;
            return nome == NomeVara || nome == "FishingRod";
        }

        static bool Segurando()
        {
            if (Input.GetMouseButton(0))
                return true;
            if (ZInput.GetMouseButton(0))
                return true;
            return ZInput.GetButton("Attack");
        }

        static bool Apertou()
        {
            return ZInput.GetButtonDown("Attack") || ZInput.GetMouseButtonDown(0);
        }

        static bool Soltou()
        {
            return ZInput.GetButtonUp("Attack") || ZInput.GetMouseButtonUp(0);
        }

        public static void Registrar(ObjectDB db)
        {
            if (Inst == null || db == null || db.m_recipes == null)
                return;
            Recipe achada = null;
            for (int i = 0; i < db.m_recipes.Count; i++)
            {
                Recipe r = db.m_recipes[i];
                if (r == null || r.m_item == null || r.m_item.gameObject == null)
                    continue;
                if (r.m_item.gameObject.name == "FishingRod")
                {
                    achada = r;
                    break;
                }
            }
            if (achada == null)
            {
                GameObject origem = db.GetItemPrefab("FishingRod");
                if (origem == null)
                    return;
                ItemDrop drop = origem.GetComponent<ItemDrop>();
                if (drop == null)
                    return;
                achada = ScriptableObject.CreateInstance<Recipe>();
                achada.name = "Recipe_VaraMonstrosDoRio";
                achada.m_item = drop;
                achada.m_amount = 1;
                achada.m_craftingStation = AcharBancada(db);
                achada.m_minStationLevel = 1;
                List<Piece.Requirement> custos = new List<Piece.Requirement>();
                Piece.Requirement madeira = Custo(db, "Wood", 8);
                if (madeira != null)
                    custos.Add(madeira);
                Piece.Requirement resina = Custo(db, "Resin", 4);
                if (resina != null)
                    custos.Add(resina);
                achada.m_resources = custos.ToArray();
                db.m_recipes.Add(achada);
                System.Reflection.MethodInfo registrar = typeof(ObjectDB).GetMethod("UpdateRegisters", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                if (registrar != null)
                    registrar.Invoke(db, null);
            }
            achada.m_enabled = true;
            Receita = achada;
            Inst.receitaPronta = true;
            string estacao = "sem bancada";
            if (achada.m_craftingStation != null && achada.m_craftingStation.gameObject != null)
                estacao = achada.m_craftingStation.gameObject.name;
            Inst.Logger.LogInfo("Vara pronta na estacao " + estacao);
        }

        static CraftingStation AcharBancada(ObjectDB db)
        {
            for (int i = 0; i < db.m_recipes.Count; i++)
            {
                Recipe r = db.m_recipes[i];
                if (r == null || r.m_craftingStation == null)
                    continue;
                string n = r.m_craftingStation.gameObject.name.ToLowerInvariant();
                if (n.Contains("workbench"))
                    return r.m_craftingStation;
            }
            return null;
        }

        static Piece.Requirement Custo(ObjectDB db, string prefab, int qtd)
        {
            GameObject go = db.GetItemPrefab(prefab);
            if (go == null)
                return null;
            ItemDrop drop = go.GetComponent<ItemDrop>();
            if (drop == null)
                return null;
            Piece.Requirement req = new Piece.Requirement();
            req.m_resItem = drop;
            req.m_amount = qtd;
            return req;
        }

        void OnGUI()
        {
            if (fase == Fase.Nada || !Ligado.Value)
                return;
            if (pixel == null)
            {
                pixel = new Texture2D(1, 1);
                pixel.SetPixel(0, 0, Color.white);
                pixel.Apply();
            }
            if (estilo == null)
            {
                estilo = new GUIStyle(GUI.skin.label);
                estilo.alignment = TextAnchor.MiddleCenter;
                estilo.fontSize = 18;
                estilo.normal.textColor = Color.white;
                estilo.wordWrap = true;
                estiloGrande = new GUIStyle(estilo);
                estiloGrande.fontSize = 64;
                estiloGrande.fontStyle = FontStyle.Bold;
                estiloGrande.normal.textColor = new Color(1f, 0.86f, 0.2f);
            }

            float escala = Screen.height / 1080f;
            if (escala < 0.7f)
                escala = 0.7f;

            if (fase == Fase.Forca)
            {
                float w = 280f * escala;
                float h = 28f * escala;
                float x = (Screen.width - w) * 0.5f;
                float y = Screen.height - 160f * escala;
                Caixa(new Rect(x, y, w, h), new Color(0.05f, 0.08f, 0.1f, 0.85f));
                Caixa(new Rect(x + 3f, y + 3f, (w - 6f) * forca, h - 6f), new Color(0.35f, 0.75f, 0.28f, 0.95f));
                GUI.Label(new Rect(x, y - 28f * escala, w, 24f * escala), "Forca do arremesso", estilo);
                return;
            }

            if (fase == Fase.Espera)
            {
                GUI.Label(new Rect(0f, Screen.height * 0.72f, Screen.width, 40f), "A boia esta na agua. Espera morder.", estilo);
                return;
            }

            if (fase == Fase.Fisgou)
            {
                GUI.Label(new Rect(0f, Screen.height * 0.38f, Screen.width, 80f), "!", estiloGrande);
                GUI.Label(new Rect(0f, Screen.height * 0.38f + 70f, Screen.width, 36f), "Segura o esquerdo", estilo);
                return;
            }

            if (fase == Fase.Aviso)
            {
                GUI.Label(new Rect(Screen.width * 0.2f, Screen.height * 0.4f, Screen.width * 0.6f, 80f), textoAviso, estilo);
                return;
            }

            if (fase != Fase.Barra)
                return;

            float desenhoH = 520f * escala;
            float desenhoW = 46f * escala;
            float xBar = Screen.width - desenhoW - 110f * escala;
            float yBar = (Screen.height - desenhoH) * 0.5f;
            float progW = 18f * escala;
            Caixa(new Rect(xBar - 16f * escala, yBar - 36f * escala, desenhoW + progW + 40f * escala, desenhoH + 78f * escala), new Color(0.08f, 0.12f, 0.1f, 0.55f));
            Caixa(new Rect(xBar - progW - 8f * escala, yBar, progW, desenhoH), new Color(0.04f, 0.05f, 0.06f, 0.9f));
            Color corProg = Color.Lerp(new Color(0.75f, 0.22f, 0.16f), new Color(0.32f, 0.78f, 0.28f), progresso);
            float cheio = desenhoH * progresso;
            Caixa(new Rect(xBar - progW - 8f * escala, yBar + desenhoH - cheio, progW, cheio), corProg);
            Caixa(new Rect(xBar, yBar, desenhoW, desenhoH), new Color(0.05f, 0.16f, 0.22f, 0.94f));

            float zonaTop = barraY - 16f;
            float zonaH = barraH - 28f;
            if (zonaTop < 0f)
            {
                zonaH += zonaTop;
                zonaTop = 0f;
            }
            if (zonaTop + zonaH > Pista)
                zonaH = Pista - zonaTop;
            if (zonaH < 8f)
                zonaH = 8f;
            float bh = (zonaH / Pista) * desenhoH;
            float by = yBar + (zonaTop / Pista) * desenhoH;
            Caixa(new Rect(xBar + 3f, by, desenhoW - 6f, bh), new Color(0.45f, 0.78f, 0.28f, 0.92f));
            Caixa(new Rect(xBar + 3f, by, desenhoW - 6f, 4f * escala), new Color(0.75f, 0.95f, 0.55f, 0.95f));

            float fy = yBar + (peixeY / Pista) * desenhoH;
            float lado = 16f * escala;
            Color corPeixe = (nomePremio == "Serpente marinha") ? new Color(0.15f, 0.72f, 0.32f) : new Color(0.95f, 0.55f, 0.16f);
            Caixa(new Rect(xBar + (desenhoW - lado) * 0.5f, fy - lado * 0.5f, lado, lado), corPeixe);

            GUI.Label(new Rect(xBar - 80f * escala, yBar - 32f * escala, desenhoW + 120f * escala, 28f * escala), nomePremio, estilo);
            GUI.Label(new Rect(xBar - 180f * escala, yBar + desenhoH + 8f * escala, desenhoW + 280f * escala, 48f * escala), "Segura o esquerdo para subir. Solta para descer.", estilo);
        }

        void Caixa(Rect r, Color c)
        {
            Color antes = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, pixel);
            GUI.color = antes;
        }

        static bool EhBancada(string nome)
        {
            return nome == "Hammer" || nome == "Torch" || nome == "Bow" || nome == "Club" || nome == "ArrowWood" || nome == "ArrowFire" || nome == "ArrowFlint";
        }

        public static void ColocarNaLista(List<Recipe> lista)
        {
            if (Inst == null || !Inst.Ligado.Value || lista == null)
                return;
            CraftingStation bancada = null;
            int ja = -1;
            for (int i = 0; i < lista.Count; i++)
            {
                Recipe r = lista[i];
                if (r == null || r.m_item == null || r.m_item.gameObject == null)
                    continue;
                string n = r.m_item.gameObject.name;
                if (n == "FishingRod")
                    ja = i;
                if (bancada == null && EhBancada(n) && r.m_craftingStation != null)
                    bancada = r.m_craftingStation;
            }
            if (bancada == null)
                return;
            if (ja > 0)
            {
                Recipe existente = lista[ja];
                lista.RemoveAt(ja);
                lista.Insert(0, existente);
                AvisarLista();
                return;
            }
            if (ja == 0)
            {
                AvisarLista();
                return;
            }
            Recipe vara = ReceitaDaBancada(bancada);
            if (vara == null)
                return;
            lista.Insert(0, vara);
            AvisarLista();
        }

        static void AvisarLista()
        {
            if (Inst == null || Inst.entrouNaLista)
                return;
            Inst.entrouNaLista = true;
            Inst.Logger.LogInfo("Vara de pescar entrou no topo da producao");
        }

        [HarmonyPatch(typeof(Player), "GetAvailableRecipes")]
        static class NoTopo
        {
            static void Postfix(Player __instance, ref List<Recipe> available)
            {
                if (Inst == null || !Inst.Ligado.Value || available == null)
                    return;
                if (__instance != Player.m_localPlayer)
                    return;
                ColocarNaLista(available);
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "UpdateRecipeList")]
        static class NaAba
        {
            static void Prefix(List<Recipe> recipes)
            {
                ColocarNaLista(recipes);
            }
        }

        static Recipe ReceitaDaBancada(CraftingStation bancada)
        {
            if (Receita != null)
            {
                Receita.m_craftingStation = bancada;
                Receita.m_enabled = true;
                Receita.m_minStationLevel = 1;
                return Receita;
            }
            if (ObjectDB.instance == null)
                return null;
            Registrar(ObjectDB.instance);
            if (Receita == null)
                return null;
            Receita.m_craftingStation = bancada;
            Receita.m_enabled = true;
            Receita.m_minStationLevel = 1;
            return Receita;
        }

        [HarmonyPatch(typeof(ObjectDB), "Awake")]
        static class AoAcordar
        {
            static void Postfix(ObjectDB __instance)
            {
                Registrar(__instance);
            }
        }

        [HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
        static class AoCopiar
        {
            static void Postfix(ObjectDB __instance)
            {
                Registrar(__instance);
            }
        }

        [HarmonyPatch(typeof(Attack), "Start")]
        static class SemAtaqueDaVara
        {
            static bool Prefix(Humanoid ___m_character)
            {
                if (Inst == null || !Inst.Ligado.Value)
                    return true;
                if (___m_character != Player.m_localPlayer)
                    return true;
                if (!ComVara(Player.m_localPlayer))
                    return true;
                return false;
            }
        }

        [HarmonyPatch(typeof(Attack), "HaveAmmo")]
        static class SemAvisoDeIsca
        {
            static bool Prefix(Humanoid ___m_character, ref bool __result)
            {
                if (Inst == null || !Inst.Ligado.Value)
                    return true;
                if (___m_character != Player.m_localPlayer)
                    return true;
                if (!ComVara(Player.m_localPlayer))
                    return true;
                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(Humanoid), "StartAttack")]
        static class SemAtaqueVanilla
        {
            static bool Prefix(Humanoid __instance, ref bool __result)
            {
                if (Inst == null || !Inst.Ligado.Value)
                    return true;
                Player jogador = __instance as Player;
                if (jogador == null || Player.m_localPlayer != jogador)
                    return true;
                if (!ComVara(jogador))
                    return true;
                __result = false;
                return false;
            }
        }

    }
}
