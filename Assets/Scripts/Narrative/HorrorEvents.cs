using SignalLost.AI;
using SignalLost.Core;
using UnityEngine;

namespace SignalLost.Narrative
{
    // Mimic lure: fake voice from a fake source. Steps up noise so Echo investigates the lie.
    public class MimicLure : MonoBehaviour
    {
        [SerializeField] private float cooldown = 45f;
        private float _last;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (Time.time - _last < cooldown) return;
            _last = Time.time;
            NoiseSystem.EmitNoise(transform.position, 40f, gameObject);
        }
    }

    // The Observer: never attacks. It watches from far, then it is gone.
    public class ObserverCameo : MonoBehaviour
    {
        [SerializeField] private GameObject ghost;
        [SerializeField] private float visibleTime = 2.2f;
        [SerializeField] private float cooldown = 90f;
        private bool _shown;
        private float _last;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (_shown || Time.time - _last < cooldown) return;
            _shown = true;
            _last = Time.time;
            StartCoroutine(Show());
        }

        private System.Collections.IEnumerator Show()
        {
            if (ghost == null) yield break;
            ghost.SetActive(true);
            EventBus.Publish(new SubtitleEvent("A.R.I.A.", "...that is not one of mine.", 3f));
            yield return new WaitForSeconds(visibleTime);
            ghost.SetActive(false);
            yield return new WaitForSeconds(1.5f);
            _shown = false;
        }
    }
}
