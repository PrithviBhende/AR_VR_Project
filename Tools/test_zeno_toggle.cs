var c = UnityEngine.Object.FindAnyObjectByType<FerrariLab.FerrariInspectionController>();
if (c != null && c.Zeno != null)
{
    c.Zeno.Toggle();
    Debug.Log("Triggered Zeno Toggle. IsEnabled=" + c.Zeno.IsEnabled + ", Status=" + c.Zeno.Status);
}
