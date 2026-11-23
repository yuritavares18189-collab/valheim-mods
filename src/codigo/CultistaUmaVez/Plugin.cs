using BepInEx;
using System.Collections.Generic;
using System.IO;

namespace CultistaUmaVez
{
    [BepInPlugin("yuri.CultistaUmaVez", "Cultista Uma Vez", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        bool feito;
        int falta = 30;
        float proxima;
        bool avisouCheia;
        string marca;

        void Awake()
        {
            marca = Path.Combine(Paths.ConfigPath, "yuri.cultista-30.txt");
            if (File.Exists(marca))
            {
                string texto = File.ReadAllText(marca).Trim();
                if (texto == "ok")
                    feito = true;
                else
                {
                    int n;
                    if (int.TryParse(texto, out n) && n > 0 && n <= 30)
                        falta = n;
                }
            }
            Logger.LogInfo("Cultista uma vez. Falta " + falta);
        }

        void Update()
        {
            if (feito)
                return;
            if (UnityEngine.Time.time < proxima)
                return;
            Player jogador = Player.m_localPlayer;
            if (jogador == null || jogador.GetPlayerName() != "Bjorn Jandel")
                return;
            if (ObjectDB.instance == null || ObjectDB.instance.GetItemPrefab("TrophyCultist") == null)
                return;
            Inventory bolsa = jogador.GetInventory();
            if (bolsa == null)
                return;
            proxima = UnityEngine.Time.time + 2f;
            int antes = Contar(bolsa);
            int pedido = falta;
            for (int i = 0; i < pedido; i++)
            {
                if (bolsa.AddItem("TrophyCultist", 1, 1, 0, 0L, "", false, false) == null)
                    break;
            }
            int ganhou = Contar(bolsa) - antes;
            if (ganhou < 1)
            {
                if (!avisouCheia)
                {
                    avisouCheia = true;
                    jogador.Message(MessageHud.MessageType.Center, "Bolsa cheia. O Trofeu de Cultista nao entrou.", 0, null, true);
                }
                Logger.LogInfo("Bolsa sem espaco para o Trofeu de Cultista");
                return;
            }
            falta = falta - ganhou;
            if (falta < 1)
            {
                File.WriteAllText(marca, "ok");
                feito = true;
                jogador.Message(MessageHud.MessageType.Center, "Entraram " + ganhou + " Trofeu de Cultista.", 0, null, true);
                Logger.LogInfo("Entraram " + ganhou);
                return;
            }
            File.WriteAllText(marca, falta.ToString());
            Logger.LogInfo("Entraram " + ganhou + ". Falta " + falta);
        }

        static int Contar(Inventory bolsa)
        {
            int n = 0;
            List<ItemDrop.ItemData> lista = bolsa.GetAllItems();
            for (int i = 0; i < lista.Count; i++)
            {
                if (lista[i] != null && lista[i].m_shared != null && lista[i].m_shared.m_name == "TrophyCultist")
                    n = n + lista[i].m_stack;
            }
            return n;
        }
    }
}
