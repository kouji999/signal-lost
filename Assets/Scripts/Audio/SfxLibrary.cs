using UnityEngine;

namespace SignalLost.Audio
{
    public static class SfxLibrary
    {
        const int SR = 44100;
        static AudioClip s_drone, s_tension, s_heartbeat, s_whisper, s_pickup, s_door,
                       s_beep, s_static, s_stinger, s_hurt, s_stepA, s_stepB, s_clank,
                       s_scan, s_powerup, s_craft;

        public static AudioClip Drone => s_drone ??= BuildDrone();
        public static AudioClip Tension => s_tension ??= BuildTension();
        public static AudioClip Heartbeat => s_heartbeat ??= BuildHeartbeat();
        public static AudioClip Whisper => s_whisper ??= BuildWhisper();
        public static AudioClip Pickup => s_pickup ??= BuildPickup();
        public static AudioClip Door => s_door ??= BuildDoor();
        public static AudioClip Beep => s_beep ??= BuildBeep();
        public static AudioClip Static => s_static ??= BuildStatic();
        public static AudioClip Stinger => s_stinger ??= BuildStinger();
        public static AudioClip Hurt => s_hurt ??= BuildHurt();
        public static AudioClip StepA => s_stepA ??= BuildStep(1.0f);
        public static AudioClip StepB => s_stepB ??= BuildStep(0.82f);
        public static AudioClip Clank => s_clank ??= BuildClank();
        public static AudioClip Scan => s_scan ??= BuildScan();
        public static AudioClip PowerUp => s_powerup ??= BuildPowerUp();
        public static AudioClip Craft => s_craft ??= BuildCraft();

        static float Lerp(float a, float b, float t) => a + (b - a) * t;

        static AudioClip Make(string name, float[] data)
        {
            LoopSmooth(data);
            var clip = AudioClip.Create(name, data.Length, 1, SR, false);
            clip.SetData(data, 0);
            return clip;
        }

        static void LoopSmooth(float[] d)
        {
            int xf = Mathf.Min(SR / 25, d.Length / 2);
            if (xf < 2) return;
            for (int i = 0; i < xf; i++)
            {
                float w = (float)i / xf;
                d[i] = d[i] * w + d[d.Length - xf + i] * (1f - w);
            }
        }

        static float N() => Random.value * 2f - 1f;

        static AudioClip BuildDrone()
        {
            int len = SR * 6;
            var d = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                float lfo = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 0.07f * t);
                float breathe = 0.8f + 0.2f * Mathf.Sin(2f * Mathf.PI * 0.031f * t);
                d[i] = lfo * breathe * (0.30f * Mathf.Sin(2f * Mathf.PI * 48f * t)
                       + 0.20f * Mathf.Sin(2f * Mathf.PI * 72.5f * t)
                       + 0.08f * Mathf.Sin(2f * Mathf.PI * 96f * t));
            }
            return Make("drone", d);
        }

        static AudioClip BuildTension()
        {
            int len = SR * 5;
            var d = new float[len];
            float lp = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                float wob = 1f + 0.004f * Mathf.Sin(2f * Mathf.PI * 0.5f * t);
                float pad = 0.22f * Mathf.Sin(2f * Mathf.PI * 110f * wob * t)
                          + 0.20f * Mathf.Sin(2f * Mathf.PI * 155.6f * t)
                          + 0.10f * Mathf.Sin(2f * Mathf.PI * 233f * t)
                          + 0.05f * Mathf.Sin(2f * Mathf.PI * 622f * t * (1f + 0.02f * Mathf.Sin(2f * Mathf.PI * 0.23f * t)));
                lp = lp * 0.985f + pad * 0.015f;
                d[i] = pad * 0.7f + lp * 1.4f;
            }
            return Make("tension", d);
        }

        static AudioClip BuildHeartbeat()
        {
            int len = SR * 2;
            var d = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                float Thump(float at, float amp)
                {
                    float dt = t - at;
                    if (dt < 0f || dt > 0.32f) return 0f;
                    return amp * Mathf.Sin(2f * Mathf.PI * (58f - 24f * dt) * dt) * Mathf.Exp(-dt * 14f);
                }
                d[i] = Thump(0.1f, 0.8f) + Thump(0.42f, 0.5f);
            }
            return Make("heartbeat", d);
        }

        static AudioClip BuildWhisper()
        {
            int len = SR * 4;
            var d = new float[len];
            float lp = 0f, hp = 0f, prev = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                float n = N();
                lp = lp * 0.7f + n * 0.3f;
                hp = lp - prev; prev = lp;
                float env = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 1.7f * t + Mathf.Sin(2f * Mathf.PI * 0.31f * t) * 3f);
                float sibil = 0.55f + 0.45f * Mathf.Sin(2f * Mathf.PI * 0.9f * t + Mathf.Sin(2f * Mathf.PI * 0.23f * t) * 4f);
                d[i] = hp * 0.45f * env * sibil;
            }
            return Make("whisper", d);
        }

        static AudioClip BuildPickup()
        {
            int len = SR * 6 / 100;
            var d = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                float f = Lerp(620f, 940f, t / (len / (float)SR));
                d[i] = 0.5f * Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 26f);
            }
            return Make("pickup", d);
        }

        static AudioClip BuildDoor()
        {
            int len = SR * 9 / 10;
            var d = new float[len];
            float lp = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                float p = t / (len / (float)SR);
                lp = lp * 0.92f + N() * 0.08f;
                float servo = lp * 6f * Mathf.Min(1f, p * 6f) * (1f - p) * 0.8f;
                servo += 0.25f * Mathf.Sin(2f * Mathf.PI * Lerp(180f, 90f, p) * t) * (1f - p);
                d[i] = servo;
            }
            return Make("door", d);
        }

        static AudioClip BuildBeep()
        {
            int len = SR * 7 / 100;
            var d = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                float env = t < 0.02f ? t / 0.02f : (1f - (t - 0.02f) / 0.05f);
                d[i] = 0.35f * Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 1180f * t)) * Mathf.Max(0f, env);
            }
            return Make("beep", d);
        }

        static AudioClip BuildStatic()
        {
            int len = SR * 5 / 10;
            var d = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                float crackle = Random.value < 0.06f ? 1f : 0.25f;
                d[i] = N() * 0.28f * crackle * Mathf.Exp(-t * 6f);
            }
            return Make("static", d);
        }

        static AudioClip BuildStinger()
        {
            int len = SR * 14 / 10;
            var d = new float[len];
            float lp = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                float p = t / (len / (float)SR);
                lp = lp * 0.6f + N() * 0.4f;
                float swell = 0.5f * Mathf.Sin(2f * Mathf.PI * Lerp(140f, 660f, p * p) * t) * Mathf.Min(1f, p * 3f);
                float hit = lp * 2.6f * Mathf.Exp(-t * 3.4f);
                float sub = 0.6f * Mathf.Sin(2f * Mathf.PI * 38f * t) * Mathf.Exp(-t * 2.2f);
                d[i] = (swell * 0.5f + hit * (p > 0.45f ? 1f : 0f) + sub) * Mathf.Clamp01(1.2f - p * 0.9f);
            }
            return Make("stinger", d);
        }

        static AudioClip BuildHurt()
        {
            int len = SR * 3 / 10;
            var d = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                float body = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * (95f - 55f * t) * t)) * 0.4f;
                d[i] = (body + N() * 0.12f) * Mathf.Exp(-t * 7f);
            }
            return Make("hurt", d);
        }

        static AudioClip BuildStep(float pitch)
        {
            int len = SR * 9 / 1000;
            var d = new float[len];
            float lp = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                lp = lp * 0.55f + N() * 0.45f;
                d[i] = lp * 1.6f * Mathf.Exp(-t * 60f * pitch) + 0.2f * Mathf.Sin(2f * Mathf.PI * 70f * pitch * t) * Mathf.Exp(-t * 40f);
            }
            return Make("step", d);
        }

        static AudioClip BuildClank()
        {
            int len = SR * 6 / 10;
            var d = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                float metal = 0.30f * Mathf.Sin(2f * Mathf.PI * 317f * t) * Mathf.Exp(-t * 5f)
                            + 0.22f * Mathf.Sin(2f * Mathf.PI * 543f * t) * Mathf.Exp(-t * 6f)
                            + 0.18f * Mathf.Sin(2f * Mathf.PI * 901f * t) * Mathf.Exp(-t * 8f);
                d[i] = metal + N() * 0.3f * Mathf.Exp(-t * 40f);
            }
            return Make("clank", d);
        }

        static AudioClip BuildScan()
        {
            int len = SR * 18 / 1000;
            var d = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                float blip = 0f;
                if (t < 0.06f) blip = Mathf.Sin(2f * Mathf.PI * 1420f * t) * Mathf.Exp(-t * 34f);
                else if (t < 0.14f) blip = Mathf.Sin(2f * Mathf.PI * 1890f * (t - 0.06f)) * Mathf.Exp(-(t - 0.06f) * 34f);
                d[i] = blip * 0.5f;
            }
            return Make("scan", d);
        }

        static AudioClip BuildPowerUp()
        {
            int len = SR * 16 / 10;
            var d = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                float p = t / (len / (float)SR);
                float rumble = Mathf.Sin(2f * Mathf.PI * (36f + 70f * p) * t) * 0.4f;
                float click = p < 0.05f ? N() * 0.5f * Mathf.Exp(-p * 60f) : 0f;
                float hum = Mathf.Sin(2f * Mathf.PI * (50f + 120f * Mathf.Min(1f, p * 2f)) * t) * Mathf.Clamp01((p - 0.5f) * 2f) * 0.25f;
                d[i] = (rumble + click + hum) * (1f - p * 0.6f);
            }
            return Make("powerup", d);
        }

        static AudioClip BuildCraft()
        {
            int len = SR * 12 / 100;
            var d = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                float tick = Mathf.Abs(Mathf.Sin(2f * Mathf.PI * 440f * t));
                float chirp = Mathf.Sin(2f * Mathf.PI * (660f + 500f * t / 0.12f) * t) * Mathf.Exp(-t * 24f);
                d[i] = tick * 0.15f * Mathf.Exp(-t * 30f) + chirp * 0.4f;
            }
            return Make("craft", d);
        }
    }
}
