using DoomArcade.Scripts.DoomClone.Items;
using UnityEngine;

namespace DoomArcade.Scripts.DoomClone
{
    public class ItemSpawner : MonoBehaviour 
    {
        [SerializeField] public GameObject itemPrefab;
    
        void Start() {
            SpawnItem();
        }
    
        public void Restart(int diff) {
            Item[] oldItems = GetComponentsInChildren<Item>();
            foreach (var item in oldItems) DestroyImmediate(item.gameObject);
        
            SpawnItem();
        }
    
        void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position, 1.0f);
        }
    
    
        void SpawnItem() {
            if (itemPrefab) {
                Instantiate(itemPrefab, transform.position, transform.rotation, null);
            }
        }
    }
}