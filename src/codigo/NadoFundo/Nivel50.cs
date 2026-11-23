using System;
using System.Collections;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;

namespace Nivel50
{
    [BepInPlugin("yuri.Nivel50", "Nivel 50", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private ConfigEntry<bool> feito;
        private bool aplicado;

        private void Awake()
        {
            feito = Config.Bind("Geral", "Aplicado", false, "Marca que as habilidades deste personagem ja foram para 70.");
        }

        private void Update()
        {
            if (aplicado || feito.Value)
                return;

            Type playerType = Type.GetType("Player, assembly_valheim");
            if (playerType == null)
                return;

            FieldInfo localField = playerType.GetField("m_localPlayer", BindingFlags.Public | BindingFlags.Static);
            if (localField == null)
                return;

            object player = localField.GetValue(null);
            if (player == null)
                return;

            MethodInfo getSkills = playerType.GetMethod("GetSkills", BindingFlags.Public | BindingFlags.Instance);
            if (getSkills == null)
                return;

            object skills = getSkills.Invoke(player, null);
            if (skills == null)
                return;

            MethodInfo getList = skills.GetType().GetMethod("GetSkillList", BindingFlags.Public | BindingFlags.Instance);
            if (getList == null)
                return;

            IList lista = getList.Invoke(skills, null) as IList;
            if (lista == null || lista.Count == 0)
                return;

            int mudou = 0;
            int vistos = 0;
            for (int i = 0; i < lista.Count; i++)
            {
                object skill = lista[i];
                if (skill == null)
                    continue;
                float atual;
                if (!LerNivel(skill, out atual))
                    continue;
                vistos++;
                if (atual < 70f)
                {
                    EscreverNivel(skill, 70f);
                    FieldInfo acc = skill.GetType().GetField("m_accumulator", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (acc != null)
                        acc.SetValue(skill, 0f);
                    mudou++;
                }
            }

            if (vistos == 0)
                return;

            Salvar(player);
            feito.Value = true;
            aplicado = true;
            Logger.LogInfo("Habilidades abaixo de 70 foram para 70. Alteradas: " + mudou);
        }

        private static bool LerNivel(object skill, out float atual)
        {
            atual = 0f;
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            FieldInfo campo = skill.GetType().GetField("m_level", flags);
            if (campo != null)
            {
                atual = (float)campo.GetValue(skill);
                return true;
            }
            PropertyInfo prop = skill.GetType().GetProperty("m_level", flags);
            if (prop != null && prop.CanRead)
            {
                atual = (float)prop.GetValue(skill, null);
                return true;
            }
            return false;
        }

        private static void EscreverNivel(object skill, float valor)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            FieldInfo campo = skill.GetType().GetField("m_level", flags);
            if (campo != null)
            {
                campo.SetValue(skill, valor);
                return;
            }
            PropertyInfo prop = skill.GetType().GetProperty("m_level", flags);
            if (prop != null && prop.CanWrite)
                prop.SetValue(skill, valor, null);
        }

        private void Salvar(object player)
        {
            Type gameType = Type.GetType("Game, assembly_valheim");
            if (gameType == null)
                return;
            object game = null;
            PropertyInfo instProp = gameType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
            if (instProp != null)
                game = instProp.GetValue(null, null);
            if (game == null)
            {
                FieldInfo instField = gameType.GetField("instance", BindingFlags.Public | BindingFlags.Static);
                if (instField != null)
                    game = instField.GetValue(null);
            }
            if (game == null)
                return;

            string[] nomes = new string[] { "SavePlayerProfile", "SaveCharacters", "m_playerProfile" };
            for (int i = 0; i < nomes.Length; i++)
            {
                MethodInfo metodo = gameType.GetMethod(nomes[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (metodo != null && metodo.GetParameters().Length == 0)
                {
                    try
                    {
                        metodo.Invoke(game, null);
                        Logger.LogInfo("Personagem salvo.");
                    }
                    catch (Exception)
                    {
                    }
                    return;
                }
            }

            FieldInfo perfilField = gameType.GetField("m_playerProfile", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (perfilField == null)
                return;
            object perfil = perfilField.GetValue(game);
            if (perfil == null)
                return;
            MethodInfo save = perfil.GetType().GetMethod("Save", BindingFlags.Public | BindingFlags.Instance);
            if (save != null && save.GetParameters().Length == 0)
            {
                try
                {
                    save.Invoke(perfil, null);
                    Logger.LogInfo("Personagem salvo.");
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
