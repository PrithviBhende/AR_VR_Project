using UnityEngine;

namespace FerrariLab
{
    // Saved asset contract for the next teammate. Poses are local to the car's common parent.
    public sealed class FerrariPart : MonoBehaviour
    {
        public string partId;
        public string displayName;
        [TextArea] public string description;
        public Vector3 assembledLocalPosition;
        public Quaternion assembledLocalRotation;
        public Vector3 assembledLocalScale;
        public bool assemblyCandidate = true;
        public void RestoreAssembledPose()
        {
            transform.localPosition = assembledLocalPosition;
            transform.localRotation = assembledLocalRotation;
            transform.localScale = assembledLocalScale;
        }
    }
}
