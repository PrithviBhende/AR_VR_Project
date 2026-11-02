var c = UnityEngine.Object.FindAnyObjectByType<FerrariLab.FerrariInspectionController>();
if (c != null && c.Zeno != null)
{
    var type = c.Zeno.GetType();
    var suspendedField = type.GetField("suspended", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    var hasFocusField = type.GetField("hasFocus", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    var remoteStartedField = type.GetField("remoteStarted", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    Debug.Log("ZENO INTERNAL: suspended=" + suspendedField.GetValue(c.Zeno) + ", hasFocus=" + hasFocusField.GetValue(c.Zeno) + ", remoteStarted=" + remoteStartedField.GetValue(c.Zeno));
}
