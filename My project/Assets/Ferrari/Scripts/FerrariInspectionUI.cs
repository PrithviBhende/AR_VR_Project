using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace FerrariLab
{
    public sealed class FerrariInspectionUI : MonoBehaviour
    {
        public FerrariInspectionController controller;
        Font font;
        Text nameText, description, status, mode, autoText, modeToggleText, listTitle, hint, targetText;
        RectTransform inspectionList, assemblyList, targetMarker;
        RawImage detailThumbnail;
        Text placementHint;
        readonly System.Collections.Generic.List<GameObject> inspectionControls=new System.Collections.Generic.List<GameObject>();
        readonly System.Collections.Generic.List<GameObject> assemblyControls=new System.Collections.Generic.List<GameObject>();
        readonly Color ink=new Color(.90f,.94f,.98f), cyan=new Color(.18f,.8f,.95f);
        RectTransform canvas;
        void Start()
        {
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var go=new GameObject("Ferrari Inspection UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            go.transform.SetParent(transform,false); canvas=(RectTransform)go.transform;
            go.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1440,900);scaler.matchWidthOrHeight=.5f;
            if(!EventSystem.current) { var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));events.transform.SetParent(transform);events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions(); }
            var top=Panel("Header",canvas,new Vector2(0,1),new Vector2(1,1),new Vector2(0,-78),Vector2.zero,new Color(.025f,.047f,.067f));
            Label(top,"FERRARI  /  EXPLORATION LAB",28,new Vector2(24,-12),new Vector2(900,36),ink);
            Label(top,"EXPLORED IN PARTS  •  BUILT BY YOU",16,new Vector2(25,-49),new Vector2(850,24),cyan);
            var toggle=Button(top,"SELF ASSEMBLY",new Vector2(-280,-18),new Vector2(260,44),controller.Assembly.ToggleMode,new Color(.045f,.43f,.57f));
            ((RectTransform)toggle.transform).anchorMin=((RectTransform)toggle.transform).anchorMax=new Vector2(1,1);modeToggleText=toggle.GetComponentInChildren<Text>();
            var right=Panel("Information",canvas,new Vector2(.735f,.145f),new Vector2(.99f,.90f),Vector2.zero,Vector2.zero,new Color(.04f,.065f,.087f));
            Label(right,"SELECTED ASSEMBLY",15,new Vector2(20,-18),new Vector2(320,24),cyan);
            nameText=Label(right,"Choose a part",26,new Vector2(20,-50),new Vector2(320,68),ink);
            description=Label(right,"Click a car part or choose it below. Cyan marks your selection. Explode the car to reveal and move its parts.",18,new Vector2(20,-126),new Vector2(315,215),ink);
            detailThumbnail=Thumbnail(right,null,new Vector2(18,-126),new Vector2(96,68));
            placementHint=Label(right,"",17,new Vector2(20,-241),new Vector2(315,104),cyan);placementHint.gameObject.name="Placement clue";
            listTitle=Label(right,"ALL 16 ASSEMBLIES  /  SCROLL",15,new Vector2(20,-350),new Vector2(320,24),cyan);
            var viewport=Panel("Part list",right,new Vector2(0,0),new Vector2(1,1),new Vector2(14,16),new Vector2(-14,-385),new Color(.025f,.044f,.060f));
            viewport.gameObject.AddComponent<RectMask2D>();
            inspectionList=viewport;
            var content=new GameObject("Items",typeof(RectTransform)).GetComponent<RectTransform>();content.SetParent(viewport,false);content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.pivot=new Vector2(.5f,1);content.sizeDelta=new Vector2(0,controller.parts.Length*68+8);
            for(int i=0;i<controller.parts.Length;i++)
            {
                var part=controller.parts[i];
                var button=Button(content,part.displayName,new Vector2(6,-6-i*68),new Vector2(322,62),()=>controller.Select(part),new Color(.075f,.115f,.145f));
                var label=button.GetComponentInChildren<Text>();label.fontSize=18;label.alignment=TextAnchor.MiddleLeft;label.rectTransform.anchoredPosition=new Vector2(90,0);label.rectTransform.sizeDelta=new Vector2(224,62);
                Thumbnail((RectTransform)button.transform,FerrariPartPresentation.Thumbnail(part.partId),new Vector2(5,-4),new Vector2(77,54));
            }
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=28;
            assemblyList=Panel("Looping assembly inventory",right,new Vector2(0,0),new Vector2(1,1),new Vector2(14,16),new Vector2(-14,-385),new Color(.025f,.044f,.060f));
            assemblyList.gameObject.AddComponent<RectMask2D>();var inventory=assemblyList.gameObject.AddComponent<FerrariLoopingInventory>();inventory.assembly=controller.Assembly;inventory.font=font;assemblyList.gameObject.SetActive(false);
            var bottom=Panel("Controls",canvas,new Vector2(0,0),new Vector2(1,0),Vector2.zero,new Vector2(0,124),new Color(.025f,.047f,.067f));
            inspectionControls.Add(Button(bottom,"EXPLODE  [E]",new Vector2(22,-14),new Vector2(175,44),()=>controller.SetExploded(true),new Color(.79f,.23f,.10f)).gameObject);
            inspectionControls.Add(Button(bottom,"ASSEMBLE  [A]",new Vector2(207,-14),new Vector2(175,44),()=>controller.SetExploded(false),new Color(.045f,.43f,.57f)).gameObject);
            inspectionControls.Add(Button(bottom,"RESET LAYOUT",new Vector2(392,-14),new Vector2(175,44),controller.ResetLayout,new Color(.12f,.20f,.26f)).gameObject);
            assemblyControls.Add(Button(bottom,"RESTART GAME",new Vector2(22,-14),new Vector2(175,44),controller.Assembly.Restart,new Color(.79f,.23f,.10f)).gameObject);
            assemblyControls.Add(Button(bottom,"CANCEL  [ESC]",new Vector2(207,-14),new Vector2(175,44),controller.Assembly.CancelDrag,new Color(.12f,.20f,.26f)).gameObject);
            assemblyControls.Add(Button(bottom,"INSPECTION",new Vector2(392,-14),new Vector2(175,44),controller.Assembly.Exit,new Color(.045f,.43f,.57f)).gameObject);
            Button(bottom,"TURN <",new Vector2(587,-14),new Vector2(105,44),()=>controller.Turn(30),new Color(.12f,.20f,.26f));
            Button(bottom,"TURN >",new Vector2(702,-14),new Vector2(105,44),()=>controller.Turn(-30),new Color(.12f,.20f,.26f));
            autoText=Button(bottom,"AUTO: OFF",new Vector2(817,-14),new Vector2(140,44),()=>controller.AutoRotate=!controller.AutoRotate,new Color(.12f,.20f,.26f)).GetComponentInChildren<Text>();
            inspectionControls.Add(autoText.transform.parent.gameObject);
            inspectionControls.Add(Button(bottom,"RESET ALL  [R]",new Vector2(977,-14),new Vector2(188,44),controller.ResetView,new Color(.12f,.20f,.26f)).gameObject);
            mode=Label(bottom,"ASSEMBLED",17,new Vector2(22,-72),new Vector2(170,26),cyan);
            status=Label(bottom,"",17,new Vector2(205,-72),new Vector2(1150,44),ink);
            hint=Label(canvas,"LEFT: select / drag in exploded mode     MIDDLE-DRAG: turn display     WHEEL: zoom",16,new Vector2(24,-87),new Vector2(1010,30),new Color(.64f,.73f,.8f));
            hint.raycastTarget=false;
            targetMarker=new GameObject("Matching snap target",typeof(RectTransform)).GetComponent<RectTransform>();targetMarker.SetParent(canvas,false);targetMarker.anchorMin=targetMarker.anchorMax=targetMarker.pivot=new Vector2(.5f,.5f);
            Panel("Top",targetMarker,new Vector2(0,1),Vector2.one,new Vector2(0,-3),Vector2.zero,cyan).GetComponent<Image>().raycastTarget=false;
            Panel("Bottom",targetMarker,Vector2.zero,new Vector2(1,0),Vector2.zero,new Vector2(0,3),cyan).GetComponent<Image>().raycastTarget=false;
            Panel("Left",targetMarker,Vector2.zero,new Vector2(0,1),Vector2.zero,new Vector2(3,0),cyan).GetComponent<Image>().raycastTarget=false;
            Panel("Right",targetMarker,new Vector2(1,0),Vector2.one,new Vector2(-3,0),Vector2.zero,cyan).GetComponent<Image>().raycastTarget=false;
            targetText=Label(targetMarker,"DROP HERE",17,new Vector2(-100,48),new Vector2(300,42),cyan);targetText.alignment=TextAnchor.MiddleCenter;
            // Clear the entire frame underneath the inset 3D camera.
            var background=new GameObject("UI Background Camera").AddComponent<Camera>();background.transform.SetParent(transform);background.cullingMask=0;background.clearFlags=CameraClearFlags.SolidColor;background.backgroundColor=new Color(.016f,.028f,.043f);background.depth=-10;
            controller.viewCamera.rect=new Rect(.01f,.15f,.715f,.71f);
        }
        void Update()
        {
            if(!status) return;
            var assembly=controller.Assembly;bool active=assembly.IsActive;
            inspectionList.gameObject.SetActive(!active);assemblyList.gameObject.SetActive(active);
            foreach(var go in inspectionControls)go.SetActive(!active);foreach(var go in assemblyControls)go.SetActive(active);
            modeToggleText.text=active?"BACK TO INSPECTION":"SELF ASSEMBLY";
            listTitle.text=active?"UNPLACED PARTS  /  LOOP SCROLL":"ALL 16 ASSEMBLIES  /  SCROLL";
            hint.text=active?"CLICK: picture + placement clue    DRAG: place    WHEEL OVER CAR: zoom    MIDDLE-DRAG: turn":"LEFT: select / drag in exploded mode     MIDDLE-DRAG: turn display     WHEEL: zoom";
            var presentedPart=active?assembly.TargetPart:controller.Selected;
            detailThumbnail.gameObject.SetActive(presentedPart);placementHint.gameObject.SetActive(presentedPart);
            description.rectTransform.anchoredPosition=presentedPart?new Vector2(124,-126):new Vector2(20,-126);
            description.rectTransform.sizeDelta=presentedPart?new Vector2(205,108):new Vector2(315,215);
            if(presentedPart){detailThumbnail.texture=FerrariPartPresentation.Thumbnail(presentedPart.partId);placementHint.text="WHERE IT FITS\n"+FerrariPartPresentation.PlacementHint(presentedPart.partId);}
            targetMarker.gameObject.SetActive(active&&assembly.TargetPart&&!assembly.IsSnapping);
            if(targetMarker.gameObject.activeSelf)
            {
                var world=assembly.TargetPosition(assembly.TargetPart);var screen=controller.viewCamera.WorldToScreenPoint(world);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,screen,null,out var local);targetMarker.anchoredPosition=local;
                float radius=Vector2.Distance(screen,controller.viewCamera.WorldToScreenPoint(world+controller.viewCamera.transform.right*FerrariAssemblyController.SnapRadius));
                targetMarker.sizeDelta=Vector2.one*Mathf.Clamp(radius*2/canvas.GetComponent<Canvas>().scaleFactor,36,130);
                Color color=assembly.CanSnap?new Color(.3f,1,.45f):cyan;
                foreach(var im in targetMarker.GetComponentsInChildren<Image>())im.color=color;
                targetText.color=color;targetText.text=assembly.CanSnap?"RELEASE TO SNAP":"MATCHING TARGET";
                targetText.rectTransform.anchoredPosition=new Vector2((targetMarker.sizeDelta.x-300)/2,48);
            }
            if(active)
            {
                var part=assembly.TargetPart;nameText.text=assembly.IsComplete?"Car complete!":part?part.displayName:"Build the Ferrari";
                description.text=assembly.IsComplete?"All 15 movable assemblies are in their saved positions and orientations. You can rotate your completed car, return to inspection, or restart.":part?FerrariPartPresentation.Purpose(part.partId):"Click a part's picture or name for its purpose and placement clue. Drag it onto the marked target, then release when it turns green.\n\nScroll over the car to zoom. Scroll over this list to cycle the 15 parts.";
                status.text=assembly.Status+(assembly.Access.IsMoving?"  |  Access panels moving":"");mode.text=assembly.PlacedCount+" / "+assembly.TotalCount+" PLACED";return;
            }
            if(controller.Selected) { nameText.text=controller.Selected.displayName;description.text=FerrariPartPresentation.Purpose(controller.Selected.partId); }
            else { nameText.text="Choose a part";description.text="Click a car part or choose it below. Cyan marks your selection. Explode the car to reveal and move its parts."; }
            status.text=controller.Status;mode.text=controller.IsAnimating?"MOVING...":controller.IsExploded?"EXPLODED":"ASSEMBLED";
            autoText.text=controller.AutoRotate?"AUTO: ON":"AUTO: OFF";
        }
        RectTransform Panel(string name,RectTransform parent,Vector2 min,Vector2 max,Vector2 offMin,Vector2 offMax,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));var rt=(RectTransform)go.transform;rt.SetParent(parent,false);rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=offMin;rt.offsetMax=offMax;go.GetComponent<Image>().color=color;return rt;
        }
        RawImage Thumbnail(RectTransform parent,Texture texture,Vector2 position,Vector2 size)
        {
            var go=new GameObject("Part image",typeof(RectTransform),typeof(RawImage));var rt=(RectTransform)go.transform;rt.SetParent(parent,false);rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);rt.anchoredPosition=position;rt.sizeDelta=size;
            var image=go.GetComponent<RawImage>();image.texture=texture;image.raycastTarget=false;return image;
        }
        Text Label(RectTransform parent,string value,int size,Vector2 pos,Vector2 dimensions,Color color)
        {
            var go=new GameObject(value.Length>30?value.Substring(0,30):value,typeof(RectTransform),typeof(Text));var rt=(RectTransform)go.transform;rt.SetParent(parent,false);rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(0,1);rt.anchoredPosition=pos;rt.sizeDelta=dimensions;
            var text=go.GetComponent<Text>();text.font=font;text.text=value;text.fontSize=size;text.color=color;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;return text;
        }
        Button Button(RectTransform parent,string title,Vector2 pos,Vector2 size,UnityEngine.Events.UnityAction action,Color color)
        {
            var rt=Panel(title,parent,new Vector2(0,1),new Vector2(0,1),Vector2.zero,Vector2.zero,color);rt.pivot=new Vector2(0,1);rt.anchoredPosition=pos;rt.sizeDelta=size;
            var im=rt.GetComponent<Image>();im.color=color;var b=rt.gameObject.AddComponent<Button>();b.targetGraphic=im;b.onClick.AddListener(action);
            var text=Label(rt,title,17,Vector2.zero,size,ink);text.alignment=TextAnchor.MiddleCenter;
            var colors=b.colors;colors.highlightedColor=new Color(.7f,.9f,1);colors.pressedColor=new Color(.45f,.7f,.85f);b.colors=colors;return b;
        }
    }
}
