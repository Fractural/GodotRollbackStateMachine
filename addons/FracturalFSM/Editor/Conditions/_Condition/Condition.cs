
using System;
using Godot;

namespace Fractural.StateMachine
{
    [Tool]
    public class Condition : Resource
    {
        [Signal] public delegate void NameChanged(string oldName, string newName);
        [Signal] public delegate void DisplayStringChanged(string newString);   // TODO: Delete if unecesary

        // Name of condition, unique to Transition
        private string name;
        [Export]
        public string Name
        {
            get => name;
            set
            {
                if (name != value)
                {
                    var old = name;
                    name = value;
                    EmitSignal(nameof(NameChanged), old, value);
                    EmitSignal(nameof(DisplayStringChanged), DisplayString());
                }
            }
        }

        public Condition(string name = "")
        {
            this.name = name;
        }

        public virtual string DisplayString()
        {
            return name;
        }
    }
}