using CustomRegister;
using Godot;

namespace Game.Level
{
    [Register]
    public class HealthSystem : Resource
    {
        [Signal]
        public delegate void Changed(HealthSystem healthSystem);
        [Signal]
        public delegate void Died();
        
        [Export(PropertyHint.Range,"0")]
        public int Max { get; private set; } 
        
        public float Percent => (float)Value / Max;
        public bool IsDied { get; private set; }
        
        private int _value;
        public int Value
        {
            get => _value;
            private set
            {
                if (value > Max) _value = Max;
                else if (value < 0) _value = 0;
                else
                {
                    _value = value;
                    EmitSignal(nameof(Changed), this);
                    if (_value == 0)
                    {
                        IsDied = true;
                        EmitSignal(nameof(Died));
                    }
                }
            }
        }
        
        public HealthSystem Init()
        {
            IsDied = false;
            Value = Max;
            return this;
        }

        public void Damage(int damageAmount)
        {
            Value -= damageAmount;
        }

        public void Heal(int healAmount)
        {
            Value += healAmount;
        }
    }
}
