var c = UnityEngine.Object.FindAnyObjectByType<FerrariLab.FerrariInspectionController>();
if (c != null && c.Zeno != null)
{
    Debug.Log("Zeno Check after 3s: IsEnabled=" + c.Zeno.IsEnabled + ", IsBusy=" + c.Zeno.IsBusy + ", IsListening=" + c.Zeno.IsListening + ", Status=" + c.Zeno.Status);
}
