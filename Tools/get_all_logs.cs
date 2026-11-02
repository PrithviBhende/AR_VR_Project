var count = UnityEditor.LogEntries.GetCount();
string logs = "Total log entries: " + count + "\n";
for (int i = Mathf.Max(0, count - 15); i < count; i++)
{
    var entry = new UnityEditor.LogEntry();
    UnityEditor.LogEntries.GetEntryInternal(i, entry);
    logs += "[" + entry.mode + "] " + entry.message + "\n";
}
System.IO.File.WriteAllText("C:/Users/prith/Documents/MYSPACE/PROJECTS/AR VR/Tools/all_logs.txt", logs);
