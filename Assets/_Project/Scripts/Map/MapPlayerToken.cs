using System;
using System.Collections;
using UnityEngine;

namespace M3P
{
    public sealed class MapPlayerToken : MonoBehaviour
    {
        [SerializeField] float _moveSpeed = 6f;
        [SerializeField] float _hoverHeight = 0.65f;
        [SerializeField] Animator _animator;
        [Tooltip("Bool on the child Animator (HeroAnim uses Walk for locomotion).")]
        [SerializeField] string _moveBoolParameter = "Walk";
        [SerializeField] bool _faceMoveDirection = true;
        [SerializeField] float _turnSpeed = 720f;

        Coroutine _moveRoutine;
        int _moveBoolHash;

        public bool IsMoving { get; private set; }

        void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);

            _moveBoolHash = string.IsNullOrEmpty(_moveBoolParameter)
                ? 0
                : Animator.StringToHash(_moveBoolParameter);

            SetMoveAnimation(false);
        }

        public void SnapTo(Vector3 worldPosition)
        {
            if (_moveRoutine != null)
            {
                StopCoroutine(_moveRoutine);
                _moveRoutine = null;
            }

            IsMoving = false;
            SetMoveAnimation(false);
            transform.position = worldPosition + Vector3.up * _hoverHeight;
        }

        public void MoveTo(Vector3 worldPosition, Action onArrived)
        {
            if (_moveRoutine != null)
                StopCoroutine(_moveRoutine);

            _moveRoutine = StartCoroutine(MoveRoutine(worldPosition + Vector3.up * _hoverHeight, onArrived));
        }

        IEnumerator MoveRoutine(Vector3 target, Action onArrived)
        {
            IsMoving = true;
            SetMoveAnimation(true);

            while ((transform.position - target).sqrMagnitude > 0.0004f)
            {
                Vector3 next = Vector3.MoveTowards(
                    transform.position,
                    target,
                    _moveSpeed * Time.deltaTime);

                if (_faceMoveDirection)
                    FaceDirection(next - transform.position);

                transform.position = next;
                yield return null;
            }

            transform.position = target;
            IsMoving = false;
            SetMoveAnimation(false);
            _moveRoutine = null;
            onArrived?.Invoke();
        }

        void FaceDirection(Vector3 delta)
        {
            delta.y = 0f;
            if (delta.sqrMagnitude < 0.0001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                _turnSpeed * Time.deltaTime);
        }

        void SetMoveAnimation(bool moving)
        {
            if (_animator == null || _moveBoolHash == 0)
                return;

            _animator.SetBool(_moveBoolHash, moving);
        }
    }
}
