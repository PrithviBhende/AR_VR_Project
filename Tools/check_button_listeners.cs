var buttons = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>();
string s = "Total buttons: " + buttons.Length + "\n";
foreach (var b in buttons)
{
    // Check if onClick has any runtime listeners via reflection or by checking if invoking does anything
    var field = typeof(UnityEngine.Events.UnityEventBase).GetField("m_Calls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    var calls = field != null ? field.GetValue(b.onClick) : null;
    s += "Btn: '" + b.name + "' parent='" + b.transform.parent.name + "', m_Calls=" + (calls != null ? calls.ToString() : "null") + "\n";
}
System.IO.File.WriteAllText("C:/Users/prith/Documents/MYSPACE/PROJECTS/AR VR/Tools/listeners_log.txt", s);
