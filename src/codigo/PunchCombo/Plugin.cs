using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace BomDeMurro
{
    [BepInPlugin("yuri.PunchCombo", "Punch Combo", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> Ligado;
        public static ConfigEntry<float> Janela;
        public static ConfigEntry<float> Dano;
        public static ConfigEntry<float> Folego;

        private static Plugin eu;
        private static bool guardado;
        private static float velBase;
        private static float giroBase;
        private static float danoBase;
        private static float forcaBase;
        private static float tomboBase;
        private static float folegoBase;
        private static float alcanceBase;
        private static float arcoBase;
        private static int cadeiaBase;
        private static int sorteioBase;
        private static string animBase;

        private static int ultimo = -1;
        private static float ultima;
        private static int pendente;
        private static string animUsada;

        private static readonly string[][] AnimGolpe = new string[][]
        {
            new string[] { "unarmed_attack0" },
            new string[] { "unarmed_attack1", "knife_stab0" },
            new string[] { "swing_longsword1", "swing_longsword2", "swing_longsword0" },
            new string[] { "mace_secondary", "swing_sledge0" },
            new string[] { "unarmed_kick" },
            new string[] { "unarmed_kick" }
        };

        private static float giroComeca;
        private static float giroDuracao;
        private static float ociosoDesde = -1f;

        private static readonly string[] NomeGolpe = { "Light", "Straight", "Cross", "Rocket", "Kick", "Spin kick" };
        private static readonly float[] Vel = { 1.5f, 1.2f, 1.05f, 0.82f, 1.1f, 0.9f };
        private static readonly float[] DanoPasso = { 1.1f, 1.3f, 1.45f, 1.85f, 1.5f, 1.75f };
        private static readonly float[] Forca = { 0.6f, 1f, 1.25f, 1.7f, 1.35f, 1.9f };
        private static readonly float[] Tombo = { 0.7f, 1f, 1.35f, 1.65f, 1.3f, 2f };
        private static readonly float[] Custo = { 0.55f, 0.75f, 0.95f, 1.2f, 0.9f, 1.25f };
        private static readonly float[] Alcance = { 1f, 1.05f, 1.1f, 1.12f, 1.2f, 1.3f };
        private static readonly float[] Arco = { 0.8f, 1f, 1.35f, 1.1f, 1.2f, 2.4f };

        private void Awake()
        {
            eu = this;
            Ligado = Config.Bind("General", "Enabled", true, "Turns on the unarmed combo in this game. With a weapon in hand, nothing changes. The character does not walk on its own. Only your own game needs this mod.");
            Janela = Config.Bind("Combo", "Window", 0.22f, "Seconds of slack after a hit ends. After that, the next click returns to the jab. A pause does not store the special.");
            Dano = Config.Bind("Combo", "Damage", 1f, "Multiplies combo damage. 1 is the default.");
            Folego = Config.Bind("Combo", "Stamina", 1f, "Multiplies the stamina cost. Lower spends less.");

            Type[] tipos = new Type[]
            {
                typeof(Humanoid),
                typeof(Rigidbody),
                typeof(ZSyncAnimation),
                typeof(CharacterAnimEvent),
                typeof(VisEquipment),
                typeof(ItemDrop.ItemData),
                typeof(Attack),
                typeof(float),
                typeof(float)
            };
            Harmony harmonia = new Harmony("yuri.PunchCombo");
            harmonia.Patch(
                AccessTools.Method(typeof(Attack), "Start", tipos),
                prefix: new HarmonyMethod(typeof(Plugin).GetMethod("Antes", BindingFlags.Static | BindingFlags.NonPublic)),
                postfix: new HarmonyMethod(typeof(Plugin).GetMethod("Depois", BindingFlags.Static | BindingFlags.NonPublic)));
            harmonia.Patch(
                AccessTools.Method(typeof(Character), "RPC_Damage", new Type[] { typeof(long), typeof(HitData) }),
                prefix: new HarmonyMethod(typeof(Plugin).GetMethod("DanoReal", BindingFlags.Static | BindingFlags.NonPublic)));
        }

        private static void DanoReal(Character __instance, HitData hit)
        {
            if (hit == null || __instance == null || !__instance.IsPlayer())
                return;
            if (hit.m_skill != Skills.SkillType.Unarmed)
                return;
            Character atacante = hit.GetAttacker();
            if (atacante == null || !atacante.IsPlayer() || atacante == __instance)
                return;
            float real = hit.GetTotalDamage();
            if (real < 0.05f)
                return;
            float armor = __instance.GetBodyArmor();
            if (armor <= 0f)
                return;
            float boosted = (float)System.Math.Sqrt(real * armor * 4f);
            if (armor < boosted * 0.5f)
                boosted = real + armor;
            if (boosted <= real)
                return;
            hit.m_damage.Modify(boosted / real);
        }

        private void LateUpdate()
        {
            MarcarPausa();
            VarrerPerna();
        }

        private static void Antes(Attack __instance, Humanoid character, ZSyncAnimation zanim, ItemDrop.ItemData weapon)
        {
            pendente = -1;
            animUsada = null;
            if (Ligado == null || !Ligado.Value)
                return;
            Player jogador = character as Player;
            if (jogador == null || Player.m_localPlayer != jogador)
                return;
            if (__instance.m_attackType != Attack.AttackType.Horizontal && __instance.m_attackType != Attack.AttackType.Vertical)
                return;
            ItemDrop.ItemData arma = weapon;
            if (arma == null)
                arma = jogador.GetCurrentWeapon();
            if (!EhPunho(jogador, arma))
                return;

            Guardar(__instance);
            int golpe = Proximo();
            if (!jogador.HaveStamina(CustoDo(golpe)))
                return;

            string anim = Escolher(zanim, golpe);
            if (anim != null)
            {
                __instance.m_attackAnimation = anim;
                __instance.m_attackChainLevels = 1;
                __instance.m_attackRandomAnimations = 0;
                animUsada = anim;
            }

            Aplicar(__instance, jogador, golpe);
            pendente = golpe;
        }

        private static readonly string[] GatilhosSoco = {
            "unarmed_attack", "unarmed_attack0", "unarmed_attack1", "unarmed_attack2",
            "swing_longsword0", "swing_longsword1", "swing_longsword2",
            "mace_secondary", "swing_sledge0", "knife_stab0"
        };

        private static void Depois(Attack __instance, ZSyncAnimation zanim, bool __result)
        {
            if (pendente < 0)
                return;
            if (!__result)
            {
                Restaurar(__instance);
                pendente = -1;
                return;
            }

            ultimo = pendente;
            ultima = Time.time;
            ociosoDesde = -1f;
            if (pendente == 5 && animUsada == "unarmed_kick")
            {
                giroDuracao = 0.72f;
                giroComeca = Time.time;
                CalarSoco(zanim);
            }
            if (eu != null)
                eu.Logger.LogInfo("Hit " + NomeGolpe[pendente] + " " + (animUsada == null ? "game punch" : animUsada));
            pendente = -1;
        }

        private static void CalarSoco(ZSyncAnimation zanim)
        {
            Player jogador = Player.m_localPlayer;
            if (jogador == null)
                return;
            Animator anim = jogador.GetComponentInChildren<Animator>();
            if (anim == null)
                return;
            int i;
            for (i = 0; i < GatilhosSoco.Length; i++)
            {
                if (zanim != null && zanim.HasParameter(GatilhosSoco[i], AnimatorControllerParameterType.Trigger))
                    anim.ResetTrigger(GatilhosSoco[i]);
            }
            if (zanim != null)
                zanim.SetTrigger("unarmed_kick");
            int estado = Animator.StringToHash("unarmed_kick");
            if (anim.HasState(0, estado))
                anim.CrossFadeInFixedTime(estado, 0.05f, 0, 0f);
        }

        private static void VarrerPerna()
        {
            if (giroDuracao <= 0f)
                return;
            Player jogador = Player.m_localPlayer;
            if (jogador == null)
            {
                giroDuracao = 0f;
                return;
            }
            float t = (Time.time - giroComeca) / giroDuracao;
            if (t >= 1f)
            {
                giroDuracao = 0f;
                return;
            }
            if (t < 0f)
                return;
            Animator anim = jogador.GetComponentInChildren<Animator>();
            if (anim == null)
                return;
            Transform coxa = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            if (coxa == null)
                return;
            float s = t * t * (3f - 2f * t);
            float yaw = Mathf.Lerp(-70f, 200f, s);
            float sobe = Mathf.Sin(t * 3.14159f) * 48f;
            coxa.rotation = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(-sobe, jogador.transform.right) * coxa.rotation;
            Transform joelho = anim.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            if (joelho != null)
            {
                float dobra = Mathf.Lerp(75f, 6f, s);
                joelho.rotation = Quaternion.AngleAxis(-dobra, joelho.right) * joelho.rotation;
            }
        }

        private static string Escolher(ZSyncAnimation zanim, int golpe)
        {
            if (zanim == null || golpe < 0 || golpe >= AnimGolpe.Length)
                return null;
            string[] lista = AnimGolpe[golpe];
            for (int i = 0; i < lista.Length; i++)
            {
                if (zanim.HasParameter(lista[i], AnimatorControllerParameterType.Trigger))
                    return lista[i];
            }
            return null;
        }

        private static bool EhPunho(Player jogador, ItemDrop.ItemData arma)
        {
            if (jogador.m_unarmedWeapon == null || jogador.m_unarmedWeapon.m_itemData == null)
                return false;
            if (arma == null || arma.m_shared == null)
                return false;
            ItemDrop.ItemData punho = jogador.m_unarmedWeapon.m_itemData;
            if (arma == punho)
                return true;
            if (punho.m_shared == null)
                return false;
            if (arma.m_shared == punho.m_shared)
                return true;
            return arma.m_shared.m_name == punho.m_shared.m_name;
        }

        private static int Proximo()
        {
            if (ultimo < 0)
                return 0;
            if (ociosoDesde < 0f)
                return (ultimo + 1) % 6;
            if (Time.time - ociosoDesde <= Folga())
                return (ultimo + 1) % 6;
            return 0;
        }

        private static float Folga()
        {
            float folga = Janela.Value;
            if (folga > 0.45f)
                folga = 0.22f;
            if (folga < 0.08f)
                folga = 0.08f;
            return folga;
        }

        private static void MarcarPausa()
        {
            if (ultimo < 0)
            {
                ociosoDesde = -1f;
                return;
            }
            Player jogador = Player.m_localPlayer;
            if (jogador != null && EmGolpe(jogador))
                ociosoDesde = -1f;
            else if (ociosoDesde < 0f)
                ociosoDesde = Time.time;
        }

        private static bool EmGolpe(Player jogador)
        {
            if (Time.time - ultima < DuracaoDo(ultimo))
                return true;
            Animator anim = jogador.GetComponentInChildren<Animator>();
            if (anim == null)
                return false;
            if (TemTagAtaque(anim.GetCurrentAnimatorStateInfo(0)))
                return true;
            if (anim.IsInTransition(0) && TemTagAtaque(anim.GetNextAnimatorStateInfo(0)))
                return true;
            return false;
        }

        private static bool TemTagAtaque(AnimatorStateInfo info)
        {
            return info.IsTag("attack");
        }

        private static float DuracaoDo(int golpe)
        {
            float vel = 1f;
            if (golpe >= 0 && golpe < Vel.Length)
                vel = Vel[golpe];
            if (vel < 0.2f)
                vel = 0.2f;
            return 0.72f / vel;
        }

        private static float CustoDo(int golpe)
        {
            float extra = Folego.Value;
            if (extra < 0f)
                extra = 0f;
            return folegoBase * Custo[golpe] * extra;
        }

        private static void Guardar(Attack ataque)
        {
            if (guardado)
                return;
            velBase = ataque.m_speedFactor;
            if (velBase < 0.05f)
                velBase = 1f;
            giroBase = ataque.m_speedFactorRotation;
            if (giroBase < 0.05f)
                giroBase = 1f;
            danoBase = ataque.m_damageMultiplier;
            if (danoBase < 1f)
                danoBase = 1f;
            forcaBase = ataque.m_forceMultiplier;
            if (forcaBase < 0.05f)
                forcaBase = 1f;
            tomboBase = ataque.m_staggerMultiplier;
            if (tomboBase < 0.05f)
                tomboBase = 1f;
            folegoBase = ataque.m_attackStamina;
            alcanceBase = ataque.m_attackRange;
            arcoBase = ataque.m_attackAngle;
            cadeiaBase = ataque.m_attackChainLevels;
            sorteioBase = ataque.m_attackRandomAnimations;
            animBase = ataque.m_attackAnimation;
            if (string.IsNullOrEmpty(animBase))
                animBase = "unarmed_attack";
            guardado = true;
        }

        private static void Aplicar(Attack ataque, Player jogador, int golpe)
        {
            float hab = jogador.GetSkillLevel(Skills.SkillType.Unarmed) / 100f;
            if (hab < 0f)
                hab = 0f;
            if (hab > 1f)
                hab = 1f;
            float danoCfg = Dano.Value;
            if (danoCfg < 0f)
                danoCfg = 0f;

            ataque.m_speedFactor = velBase * Vel[golpe] * (1f + hab * 0.1f);
            ataque.m_speedFactorRotation = giroBase;
            ataque.m_damageMultiplier = danoBase * DanoPasso[golpe] * (1f + hab * 0.45f) * danoCfg;
            ataque.m_forceMultiplier = forcaBase * Forca[golpe] * (1f + hab * 0.15f);
            ataque.m_staggerMultiplier = tomboBase * Tombo[golpe];
            ataque.m_attackStamina = CustoDo(golpe);
            if (alcanceBase > 0.2f)
                ataque.m_attackRange = alcanceBase * Alcance[golpe];
            if (arcoBase > 1f)
                ataque.m_attackAngle = arcoBase * Arco[golpe];
        }

        private static void Restaurar(Attack ataque)
        {
            if (!guardado)
                return;
            ataque.m_attackAnimation = animBase;
            ataque.m_speedFactor = velBase;
            ataque.m_speedFactorRotation = giroBase;
            ataque.m_damageMultiplier = danoBase;
            ataque.m_forceMultiplier = forcaBase;
            ataque.m_staggerMultiplier = tomboBase;
            ataque.m_attackStamina = folegoBase;
            ataque.m_attackRange = alcanceBase;
            ataque.m_attackAngle = arcoBase;
            ataque.m_attackChainLevels = cadeiaBase;
            ataque.m_attackRandomAnimations = sorteioBase;
        }

    }
}
