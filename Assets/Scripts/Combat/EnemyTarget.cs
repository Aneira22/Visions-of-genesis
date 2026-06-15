using UnityEngine;
using UnityEngine.EventSystems;

namespace VisionsOfGenesis.Combat
{
    public class EnemyTarget : MonoBehaviour, IPointerClickHandler
    {
        public UnitComponent unit;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (unit == null || unit.isDead) return;
            if (BattleManager.Instance != null)
                BattleManager.Instance.SetTarget(unit);
        }
    }
}
