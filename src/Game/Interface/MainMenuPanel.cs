namespace Game.Interface
{
    public class MainMenuPanel : Panel
    {
        public new MainMenuPanel Init()
        {
            base.Init();
            FocusControl.GrabFocus();
            return this;
        }
    }
}
