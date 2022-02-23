using System.Threading.Tasks;
using Godot;
using Manager;

namespace UI
{
    public class Gui : CanvasLayer
    {
        public async Task FadeControlAlpha(
            CanvasItem control, float from, float to, float duration)
        {
            float count = 0f;
            while (count < duration)
            {
                if (!IsInstanceValid(this)) return;
                float alpha = Mathf.Lerp(from, to, count / duration);
                control.Modulate = new Color(
                    control.Modulate.r, control.Modulate.g, control.Modulate.b, alpha
                );
                count += GetProcessDeltaTime();
                await TreeTimer.S.Wait(GetProcessDeltaTime());
            }
            control.Modulate = new Color(
                control.Modulate.r, control.Modulate.g, control.Modulate.b, to
            );
        }
    }
}
