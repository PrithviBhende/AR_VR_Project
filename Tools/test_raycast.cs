var c = UnityEngine.Object.FindAnyObjectByType<FerrariLab.FerrariInspectionController>();
var canvas = UnityEngine.Object.FindAnyObjectByType<UnityEngine.Canvas>();
var raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
var eventSystem = UnityEngine.EventSystems.EventSystem.current;

var buttons = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>();
string log = "Buttons in scene: " + buttons.Length + "\n";

foreach (var b in buttons)
{
    var rt = b.GetComponent<UnityEngine.RectTransform>();
    Vector3[] corners = new Vector3[4];
    rt.GetWorldCorners(corners);
    Vector2 screenPos = UnityEngine.RectTransformUtility.WorldToScreenPoint(null, rt.position);

    var pointerData = new UnityEngine.EventSystems.PointerEventData(eventSystem);
    pointerData.position = screenPos;
    var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
    raycaster.Raycast(pointerData, results);

    string hitNames = "";
    foreach (var r in results)
    {
        hitNames += r.gameObject.name + " (" + (r.gameObject.GetComponent<UnityEngine.UI.Button>() != null ? "HAS_BTN" : "NO_BTN") + "), ";
    }
    log += "Btn '" + b.name + "' at " + screenPos + " -> Hits: " + (results.Count > 0 ? hitNames : "NONE") + "\n";
}

System.IO.File.WriteAllText("C:/Users/prith/Documents/MYSPACE/PROJECTS/AR VR/Tools/raycast_log.txt", log);
