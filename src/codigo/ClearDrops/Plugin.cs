using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace LimpaDrop
{
    [BepInPlugin("yuri.ClearDrops", "Clear Drops", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        ConfigEntry<bool> Ligado;
        ConfigEntry<KeyboardShortcut> Tecla;
        ConfigEntry<float> Raio;

        void Awake()
        {
            Ligado = Config.Bind("General", "Enabled", true, "Clears ground items near you. Only your own game needs this mod.");
            Tecla = Config.Bind("General", "Key", new KeyboardShortcut(KeyCode.F9), "Key that clears nearby drops.");
            Raio = Config.Bind("General", "Radius", 12f, "Distance in meters. From 3 to 40.");
            Logger.LogInfo("Clear Drops 1.0.1");
        }

        void Update()
        {
            if (!Ligado.Value)
                return;
            if (!Tecla.Value.IsDown())
                return;
            Player jogador = Player.m_localPlayer;
            if (jogador == null || jogador.IsDead())
                return;
            if (!PodeAgir(jogador))
                return;
            Limpar(jogador);
        }

        void Limpar(Player jogador)
        {
            float raio = Raio.Value;
            if (raio < 3f)
                raio = 3f;
            if (raio > 40f)
                raio = 40f;
            float raio2 = raio * raio;
            Vector3 pos = jogador.transform.position;
            ItemDrop[] todos = Object.FindObjectsOfType<ItemDrop>();
            int n = 0;
            if (todos != null)
            {
                for (int i = 0; i < todos.Length; i++)
                {
                    ItemDrop drop = todos[i];
                    if (drop == null || drop.IsPiece())
                        continue;
                    if (EhProtegido(drop))
                        continue;
                    if ((drop.transform.position - pos).sqrMagnitude > raio2)
                        continue;
                    if (Apagar(drop.gameObject))
                        n++;
                }
            }
            if (n == 0)
                jogador.Message(MessageHud.MessageType.Center, "No drops nearby.", 0, null, true);
            else
                jogador.Message(MessageHud.MessageType.Center, "Nearby drops cleared. " + n + ".", 0, null, true);
            Logger.LogInfo("Drops cleared " + n);
        }

        static bool PodeAgir(Player jogador)
        {
            System.Reflection.MethodInfo metodo = typeof(Player).GetMethod("TakeInput", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (metodo == null)
                return true;
            object valor = metodo.Invoke(jogador, null);
            return valor is bool && (bool)valor;
        }

        static bool EhProtegido(Component coisa)
        {
            if (coisa == null)
                return true;
            if (coisa.GetComponentInParent<TombStone>() != null)
                return true;
            if (coisa.GetComponentInParent<ItemStand>() != null)
                return true;
            if (coisa.GetComponentInParent<ArmorStand>() != null)
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

        static bool Apagar(GameObject go)
        {
            if (go == null)
                return false;
            ZNetView visao = go.GetComponent<ZNetView>();
            if (visao != null && visao.IsValid())
            {
                if (!visao.IsOwner())
                    visao.ClaimOwnership();
                visao.Destroy();
                return true;
            }
            Object.Destroy(go);
            return true;
        }
    }
}
