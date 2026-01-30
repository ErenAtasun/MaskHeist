using UnityEngine;

namespace MaskHeist.Interaction
{
    public interface IInteractable
    {
        string InteractionPrompt { get; }
        bool CanInteract(GameObject interactor);
        void OnInteract(GameObject interactor);
    }
}
