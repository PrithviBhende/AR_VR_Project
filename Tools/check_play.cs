System.IO.File.WriteAllText("C:/Users/prith/Documents/MYSPACE/PROJECTS/AR VR/Tools/play_state.txt", 
    "isPlaying=" + UnityEditor.EditorApplication.isPlaying + 
    ", EventSystem.current=" + (UnityEngine.EventSystems.EventSystem.current != null ? UnityEngine.EventSystems.EventSystem.current.name : "null"));
