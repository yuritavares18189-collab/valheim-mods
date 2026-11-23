namespace UnityEngine
{
    public class Object
    {
    }

    public class Component : Object
    {
    }

    public class Behaviour : Component
    {
    }

    public class MonoBehaviour : Behaviour
    {
    }

    public struct Color
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public Color(float r, float g, float b)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = 1f;
        }

        public Color(float r, float g, float b, float a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public static Color white
        {
            get { return new Color(1f, 1f, 1f, 1f); }
        }
    }

    public struct Rect
    {
        public float x;
        public float y;
        public float width;
        public float height;

        public Rect(float x, float y, float width, float height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }
    }

    public struct Vector2
    {
        public float x;
        public float y;
    }

    public class Screen
    {
        public static int width { get; set; }
        public static int height { get; set; }
    }

    public class Texture
    {
    }
}
