using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class MiragePuppetController : MonoBehaviour
    {
        private Character _target;
        private MirageBehavior _behavior;
        private float _speed;
        private float _followDistance;
        private float _vanishDistance;
        private float _destroyAt;
        private Vector3 _walkDirection;

        public void Initialize(Character target, MirageBehavior behavior, float lifetime, float speed, float followDistance, float vanishDistance)
        {
            _target = target; _behavior = behavior; _speed = Mathf.Clamp(speed, .2f, 8f); _followDistance = Mathf.Clamp(followDistance, 2f, 15f); _vanishDistance = Mathf.Clamp(vanishDistance, 1f, 10f);
            _destroyAt = Time.time + Mathf.Clamp(lifetime, 1f, 120f); _walkDirection = transform.right;
        }

        private void Update()
        {
            if (Time.time >= _destroyAt) { Destroy(gameObject); return; }
            if (_target == null) return;
            Vector3 targetPos = _target.Center; Vector3 delta = targetPos - transform.position; delta.y = 0f; float distance = delta.magnitude;
            if (delta.sqrMagnitude > .01f && (_behavior == MirageBehavior.StandAndStare || _behavior == MirageBehavior.FollowAtDistance || _behavior == MirageBehavior.ApproachSlowly || _behavior == MirageBehavior.ChargeTarget || _behavior == MirageBehavior.VanishWhenClose))
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(delta.normalized, Vector3.up), Time.deltaTime * 4f);
            if (_behavior == MirageBehavior.FollowAtDistance && distance > _followDistance) Move(delta.normalized, _speed);
            else if (_behavior == MirageBehavior.ApproachSlowly && distance > 1.5f) Move(delta.normalized, _speed * .45f);
            else if (_behavior == MirageBehavior.ChargeTarget && distance > 1f) Move(delta.normalized, _speed * 1.7f);
            else if (_behavior == MirageBehavior.WalkAcross) Move(_walkDirection, _speed);
            else if (_behavior == MirageBehavior.RunAway && distance < _followDistance * 2f) Move(-delta.normalized, _speed * 1.5f);
            else if (_behavior == MirageBehavior.VanishWhenClose && distance <= _vanishDistance) Destroy(gameObject);
        }

        private void Move(Vector3 direction, float speed) { transform.position += direction * speed * Time.deltaTime; }
    }
}
