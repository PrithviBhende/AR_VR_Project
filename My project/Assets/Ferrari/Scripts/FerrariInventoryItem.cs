using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FerrariLab
{
    public sealed class FerrariInventoryItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IScrollHandler
    {
        public FerrariPart part;
        public FerrariLoopingInventory inventory;
        public Text label;
        public RawImage thumbnail;
        bool ownsDrag;
        public void OnPointerClick(PointerEventData e){if(e.button==PointerEventData.InputButton.Left&&part)inventory.assembly.Preview(part);}
        public void OnBeginDrag(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)ownsDrag=inventory.assembly.BeginDrag(part,e.position);}
        public void OnDrag(PointerEventData e){if(ownsDrag)inventory.assembly.DragPointer(e.position);}
        public void OnEndDrag(PointerEventData e){if(ownsDrag)inventory.assembly.Release(e.position);ownsDrag=false;}
        public void OnScroll(PointerEventData e){inventory.OnScroll(e);}
        void OnDisable(){if(ownsDrag&&inventory&&inventory.assembly)inventory.assembly.CancelDrag();ownsDrag=false;}
    }
}
