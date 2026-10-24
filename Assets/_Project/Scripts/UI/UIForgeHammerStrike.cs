using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// Drops CenterForge/Hammer onto the workpiece, bursts sparks, plays the craft sting, then lifts it back.
    /// </summary>
    public sealed class UIForgeHammerStrike : MonoBehaviour
    {
        const float RaisedY = 55f;
        const float StrikeY = -15f;
        const float DropDuration = 0.14f;
        const float HoldDuration = 0.05f;
        const float ReturnDuration = 0.4f;
        const int SparkCount = 14;

        [SerializeField] RectTransform _hammer;

        readonly List<Spark> _sparks = new List<Spark>();
        Coroutine _routine;
        Vector2 _restPosition;
        bool _restCaptured;
        Sprite _sparkSprite;
        Texture2D _sparkTexture;

        void Awake()
        {
            ResolveHammer();
            CaptureRest();
        }

        void OnEnable()
        {
            if (_routine == null)
                CaptureRest();
        }

        void OnDisable()
        {
            StopStrike(resetHammer: true);
        }

        void OnDestroy()
        {
            if (_sparkSprite != null)
                Destroy(_sparkSprite);
            if (_sparkTexture != null)
                Destroy(_sparkTexture);
        }

        /// <summary>Plays one press. The craft sting fires when the head reaches the workpiece.</summary>
        public void Play()
        {
            ResolveHammer();
            if (_hammer == null)
            {
                Sfx.Play(SfxId.TileCraftSuccess);
                return;
            }

            if (!_restCaptured)
                CaptureRest();

            StopStrike(resetHammer: true);
            _routine = StartCoroutine(StrikeRoutine());
        }

        void ResolveHammer()
        {
            if (_hammer != null)
                return;

            Transform child = transform.Find("Hammer");
            if (child is RectTransform rect)
                _hammer = rect;
        }

        void CaptureRest()
        {
            if (_hammer == null)
                return;

            _restPosition = _hammer.anchoredPosition;
            _restPosition.y = RaisedY;
            _hammer.anchoredPosition = _restPosition;
            _restCaptured = true;
        }

        void StopStrike(bool resetHammer)
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            ClearSparks();
            if (resetHammer && _hammer != null && _restCaptured)
                _hammer.anchoredPosition = _restPosition;
        }

        IEnumerator StrikeRoutine()
        {
            Vector2 raised = _restPosition;
            Vector2 struck = _restPosition;
            struck.y = StrikeY;

            yield return Move(raised, struck, DropDuration, easeIn: true);

            SpawnSparks(struck);
            Sfx.Play(SfxId.TileCraftSuccess);

            if (HoldDuration > 0f)
                yield return new WaitForSeconds(HoldDuration);

            float elapsed = 0f;
            while (elapsed < ReturnDuration || AnySparkAlive())
            {
                float dt = Time.deltaTime;
                elapsed += dt;
                if (elapsed < ReturnDuration)
                {
                    float u = Mathf.Clamp01(elapsed / ReturnDuration);
                    float e = 1f - (1f - u) * (1f - u);
                    _hammer.anchoredPosition = Vector2.Lerp(struck, raised, e);
                }
                else
                {
                    _hammer.anchoredPosition = raised;
                }

                StepSparks(dt);
                yield return null;
            }

            _hammer.anchoredPosition = raised;
            ClearSparks();
            _routine = null;
        }

        IEnumerator Move(Vector2 from, Vector2 to, float duration, bool easeIn)
        {
            if (duration <= 0f)
            {
                _hammer.anchoredPosition = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / duration);
                float e = easeIn ? u * u * u : 1f - (1f - u) * (1f - u);
                _hammer.anchoredPosition = Vector2.Lerp(from, to, e);
                yield return null;
            }

            _hammer.anchoredPosition = to;
        }

        void SpawnSparks(Vector2 struck)
        {
            ClearSparks();
            EnsureSparkSprite();
            Vector2 origin = struck + new Vector2(6f, -72f);

            for (int i = 0; i < SparkCount; i++)
            {
                var go = new GameObject("Spark", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                go.transform.SetAsLastSibling();

                var rect = (RectTransform)go.transform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = origin + Random.insideUnitCircle * 8f;
                rect.sizeDelta = new Vector2(Random.Range(5f, 9f), Random.Range(16f, 34f));

                var image = go.AddComponent<Image>();
                image.sprite = _sparkSprite;
                image.raycastTarget = false;
                Color color = Color.Lerp(new Color(1f, 0.55f, 0.12f, 1f), new Color(1f, 0.96f, 0.78f, 1f), Random.value);
                image.color = color;

                float angle = Random.Range(-20f, 200f) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float life = Random.Range(0.28f, 0.5f);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);

                _sparks.Add(new Spark
                {
                    Rect = rect,
                    Image = image,
                    Velocity = direction * Random.Range(240f, 560f),
                    Life = life,
                    Age = 0f,
                    Color = color
                });
            }
        }

        void StepSparks(float dt)
        {
            for (int i = _sparks.Count - 1; i >= 0; i--)
            {
                Spark spark = _sparks[i];
                spark.Age += dt;
                if (spark.Rect == null || spark.Age >= spark.Life)
                {
                    if (spark.Rect != null)
                        Destroy(spark.Rect.gameObject);
                    _sparks.RemoveAt(i);
                    continue;
                }

                spark.Velocity.y -= 820f * dt;
                spark.Rect.anchoredPosition += spark.Velocity * dt;
                spark.Rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(spark.Velocity.y, spark.Velocity.x) * Mathf.Rad2Deg - 90f);
                float fade = 1f - spark.Age / spark.Life;
                Color color = spark.Color;
                color.a = fade;
                spark.Image.color = color;
                _sparks[i] = spark;
            }
        }

        bool AnySparkAlive()
        {
            return _sparks.Count > 0;
        }

        void ClearSparks()
        {
            for (int i = 0; i < _sparks.Count; i++)
            {
                if (_sparks[i].Rect != null)
                    Destroy(_sparks[i].Rect.gameObject);
            }

            _sparks.Clear();
        }

        void EnsureSparkSprite()
        {
            if (_sparkSprite != null)
                return;

            const int width = 8;
            const int height = 32;
            _sparkTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            _sparkTexture.wrapMode = TextureWrapMode.Clamp;
            _sparkTexture.filterMode = FilterMode.Bilinear;
            _sparkTexture.hideFlags = HideFlags.HideAndDontSave;

            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                float along = Mathf.Sin((y + 0.5f) / height * Mathf.PI);
                for (int x = 0; x < width; x++)
                {
                    float across = 1f - Mathf.Abs((x + 0.5f) / width * 2f - 1f);
                    float alpha = along * across * across;
                    Color color = Color.Lerp(new Color(1f, 0.45f, 0.08f, alpha), new Color(1f, 0.98f, 0.85f, alpha), (y + 0.5f) / height);
                    pixels[y * width + x] = color;
                }
            }

            _sparkTexture.SetPixels(pixels);
            _sparkTexture.Apply(false, true);
            _sparkSprite = Sprite.Create(_sparkTexture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
            _sparkSprite.hideFlags = HideFlags.HideAndDontSave;
        }

        struct Spark
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Velocity;
            public float Life;
            public float Age;
            public Color Color;
        }
    }
}
