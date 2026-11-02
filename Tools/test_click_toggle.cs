var aiBtn = UnityEngine.GameObject.Find("ZENO AI: OFF") ?? UnityEngine.GameObject.Find("ZENO AI: ON");
var c = UnityEngine.Object.FindAnyObjectByType<FerrariLab.FerrariInspectionController>();
string before = "Before click: IsEnabled=" + c.Zeno.IsEnabled + ", Status=" + c.Zeno.Status;

if (aiBtn != null)
{
    var btn = aiBtn.GetComponent<UnityEngine.UI.Button>();
    before += ", Button found: name=" + btn.name + ", interactable=" + btn.interactable + ", onClick count=" + btn.onClick.GetPersistentEventCount();
    btn.onClick.Invoke();
}
else
{
    before += ", Button NOT found!";
}

string after = "After click: IsEnabled=" + c.Zeno.IsEnabled + ", Status=" + c.Zeno.Status;
System.IO.File.WriteAllText("C:/Users/prith/Documents/MYSPACE/PROJECTS/AR VR/Tools/click_log.txt", before + "\n" + after);
