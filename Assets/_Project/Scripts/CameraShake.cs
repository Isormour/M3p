using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [SerializeField] float _distancePerForce = 0.045f;
    [SerializeField] float _spring = 70f;
    [SerializeField] float _damping = 8f;
    [SerializeField] float _maxDistance = 0.4f;

    Vector3 _restLocalPosition;
    Vector3 _offset;
    Vector3 _velocity;

    void Awake()
    {
        _restLocalPosition = transform.localPosition;
    }

    public void Shake(Vector3 sourcePosition, float Force)
    {
        if (Force <= 0f)
            return;

        Vector3 away = transform.position - sourcePosition;
        away.z = 0f;
        if (away.sqrMagnitude < 0.0001f)
            away = Vector3.up;
        else
            away.Normalize();

        _offset += away * (Force * _distancePerForce);
        _offset.z = 0f;
        if (_offset.sqrMagnitude > _maxDistance * _maxDistance)
            _offset = _offset.normalized * _maxDistance;
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f)
            return;

        _velocity += (-_offset * _spring - _velocity * _damping) * dt;
        _offset += _velocity * dt;
        _offset.z = 0f;
        _velocity.z = 0f;

        if (_offset.sqrMagnitude < 0.0000001f && _velocity.sqrMagnitude < 0.0000001f)
        {
            _offset = Vector3.zero;
            _velocity = Vector3.zero;
            transform.localPosition = _restLocalPosition;
            return;
        }

        Vector3 localOffset = transform.parent != null
            ? transform.parent.InverseTransformDirection(_offset)
            : _offset;
        transform.localPosition = _restLocalPosition + localOffset;
    }
}
