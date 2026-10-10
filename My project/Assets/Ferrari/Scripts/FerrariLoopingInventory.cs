using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FerrariLab
{
    public sealed class FerrariLoopingInventory : MonoBehaviour, IScrollHandler
    {
        public FerrariAssemblyController assembly;
        public Font font;
        public int StartIndex { get; private set; }
        readonly List<FerrariInventoryItem> rows=new List<FerrariInventoryItem>();
        int lastRevision=-1;
        public IReadOnlyList<FerrariInventoryItem> Rows => rows;
        void Start()
        {
            // Recycle a small fixed pool. Repeated rows never clone the actual vehicle parts.
            for(int i=0;i<9;i++)
            {
                var go=new GameObject("Inventory row "+i,typeof(RectTransform),typeof(Image));
                var rt=(RectTransform)go.transform;rt.SetParent(transform,false);rt.anchorMin=new Vector2(0,1);rt.anchorMax=new Vector2(1,1);rt.pivot=new Vector2(.5f,1);rt.anchoredPosition=new Vector2(0,-6-i*68);rt.sizeDelta=new Vector2(-12,62);
                go.GetComponent<Image>().color=new Color(.075f,.15f,.19f);
                var labelGo=new GameObject("Name",typeof(RectTransform),typeof(Text));var tr=(RectTransform)labelGo.transform;tr.SetParent(rt,false);tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=new Vector2(90,0);tr.offsetMax=new Vector2(-7,0);
                var label=labelGo.GetComponent<Text>();label.font=font;label.fontSize=18;label.color=new Color(.92f,.97f,1);label.alignment=TextAnchor.MiddleLeft;label.raycastTarget=false;
                var picture=new GameObject("Part thumbnail",typeof(RectTransform),typeof(RawImage));var pr=(RectTransform)picture.transform;pr.SetParent(rt,false);pr.anchorMin=pr.anchorMax=new Vector2(0,.5f);pr.pivot=new Vector2(0,.5f);pr.anchoredPosition=new Vector2(5,0);pr.sizeDelta=new Vector2(77,54);
                var thumbnail=picture.GetComponent<RawImage>();thumbnail.raycastTarget=false;
                var row=go.AddComponent<FerrariInventoryItem>();row.inventory=this;row.label=label;row.thumbnail=thumbnail;rows.Add(row);
            }
            RefreshRows();
        }
        void Update(){if(lastRevision!=assembly.Revision)RefreshRows();}
        public void ScrollBy(int steps)
        {
            if(assembly.IsBusy||assembly.Available.Count==0)return;
            int count=assembly.Available.Count;StartIndex=((StartIndex+steps)%count+count)%count;RefreshRows();
        }
        public void OnScroll(PointerEventData e){if(e.scrollDelta.y!=0)ScrollBy(e.scrollDelta.y<0?1:-1);}
        public void RefreshRows()
        {
            lastRevision=assembly.Revision;
            int count=assembly.Available.Count;if(count>0)StartIndex%=count;else StartIndex=0;
            for(int i=0;i<rows.Count;i++)
            {
                var p=count==0?null:assembly.Available[(StartIndex+i)%count];rows[i].part=p;
                rows[i].label.text=p?p.displayName:(i==0?(assembly.IsComplete?"All parts placed!":"Placing final part..."):"");
                rows[i].thumbnail.texture=p?FerrariPartPresentation.Thumbnail(p.partId):null;rows[i].thumbnail.enabled=p;
                rows[i].label.rectTransform.offsetMin=new Vector2(p?90:7,0);
                rows[i].GetComponent<Image>().color=p?(p==assembly.PreviewPart?new Color(.08f,.32f,.39f):new Color(.075f,.15f,.19f)):new Color(.025f,.044f,.060f);
            }
        }
    }
}
