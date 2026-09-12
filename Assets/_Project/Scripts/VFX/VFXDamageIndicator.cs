using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace M3P
{
    [DisallowMultipleComponent]
    public class VFXDamage : MonoBehaviour
    {
        [FormerlySerializedAs("damageValue")]
        [SerializeField] TextMeshPro _damageValue;

        [Header("Lifetime")]
        [SerializeField] float _lifetime = 0.85f;
        [SerializeField] float _fadeOutDuration = 0.45f;

        [Header("Pop")]
        [SerializeField] float _upDistance = 0.55f;
        [FormerlySerializedAs("_backDistance")]
        [SerializeField] float _frontDistance = 0.45f;
        [SerializeField] float _randomSpread = 0.28f;
        [SerializeField] bool _faceCamera = true;

        Color _baseColor;
        Coroutine _playRoutine;

        void Awake()
        {
            if (_damageValue == null)
                _damageValue = GetComponent<TextMeshPro>();

            if (_damageValue != null)
            {
                _baseColor = _damageValue.color;
                _damageValue.renderer.sortingOrder = 20;
            }
        }

        public void Present(int amount)
        {
            if (_damageValue != null)
            {
                _damageValue.text = amount.ToString();
                _baseColor = _damageValue.color;
            }

            SetFade(1f);
            transform.position += GetTowardCamera() * _frontDistance;

            if (_playRoutine != null)
                StopCoroutine(_playRoutine);

            _playRoutine = StartCoroutine(PlayRoutine());
        }

        void LateUpdate()
        {
            if (!_faceCamera)
                return;

            Camera cameraRef = Camera.main;
            if (cameraRef != null)
                transform.rotation = cameraRef.transform.rotation;
        }

        IEnumerator PlayRoutine()
        {
            Vector3 start = transform.position;
            Vector3 offset = BuildPopOffset();
            float moveDuration = Mathf.Max(0.01f, _lifetime);
            float fadeDuration = Mathf.Max(0.01f, _fadeOutDuration);
            float elapsed = 0f;

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveDuration);
                transform.position = start + offset * EaseOutQuad(t);

                float fadeStart = Mathf.Max(0f, moveDuration - fadeDuration);
                float alpha = elapsed <= fadeStart
                    ? 1f
                    : 1f - Mathf.Clamp01((elapsed - fadeStart) / fadeDuration);
                SetFade(alpha);
                yield return null;
            }

            SetFade(0f);
            _playRoutine = null;
            Destroy(gameObject);
        }

        static float EaseOutQuad(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }

        Vector3 BuildPopOffset()
        {
            Vector3 towardCamera = GetTowardCamera();
            Vector3 right = GetCameraRight();

            return towardCamera * _frontDistance
                + Vector3.up * _upDistance
                + right * Random.Range(-_randomSpread, _randomSpread)
                + Vector3.up * Random.Range(-_randomSpread * 0.35f, _randomSpread)
                + towardCamera * Random.Range(0f, _randomSpread);
        }

        static Vector3 GetTowardCamera()
        {
            Camera cameraRef = Camera.main;
            return cameraRef != null ? -cameraRef.transform.forward : Vector3.back;
        }

        static Vector3 GetCameraRight()
        {
            Camera cameraRef = Camera.main;
            return cameraRef != null ? cameraRef.transform.right : Vector3.right;
        }

        void SetFade(float alpha)
        {
            if (_damageValue == null)
                return;

            Color color = _baseColor;
            color.a = _baseColor.a * alpha;
            _damageValue.color = color;
        }
    }
}
