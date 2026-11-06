using UnityEngine;
using UnityEngine.EventSystems;
namespace Dungeon.UI {

    /// <summary>格子点击：区分左键/右键，回调给 InventoryUI。</summary>
    public class SlotClickHandler : MonoBehaviour, IPointerClickHandler
    {
        public System.Action<PointerEventData> onClick;
        public void OnPointerClick(PointerEventData eventData) => onClick?.Invoke(eventData);
    }
}