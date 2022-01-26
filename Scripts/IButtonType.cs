namespace PlatformerPlayerController.Scripts
{
    public interface IButtonType
    {
        ButtonTypes ButtonType { get; set; }
        string LoadScenePath { get; set; }

        void OnPressed();
    }

    public enum ButtonTypes
    {
        ChangeSceneButton,
        QuitGameButton
    }
}