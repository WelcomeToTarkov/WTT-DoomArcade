using UnityEngine;

namespace DoomArcade.Scripts.DoomClone
{
    public class SecretTrigger : MonoBehaviour
    {
        private bool _discovered;

        public void TryDiscoverFromPlayer()
        {
            if (_discovered) return;
            if (Player.current == null) return;

            _discovered = true;
            Player.current.secretsFound++;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_discovered) return;

            var body = Player.current?.body;
            if (body != null && other.GetComponent<DoomStyleController>() == body)
            {
                _discovered = true;
                Player.current.secretsFound++;
            }
        }
    }
}