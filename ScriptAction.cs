using System.ComponentModel;

namespace OlAform
{
    public enum ActionType
    {
        MouseMove,
        MouseClick,
        KeyPress,
        OCR,
        FindImage
    }

    public class ScriptAction
    {
        [DisplayName("Action Name")]
        public string Name { get; set; } = "Action";

        [DisplayName("Type")]
        public ActionType ActionType { get; set; }

        [DisplayName("X")]
        public int X { get; set; }

        [DisplayName("Y")]
        public int Y { get; set; }

        [DisplayName("Width")]
        public int Width { get; set; } = 100;

        [DisplayName("Height")]
        public int Height { get; set; } = 30;

        [DisplayName("Key")]
        public string? Key { get; set; }

        [DisplayName("Additional")]
        public string? Additional { get; set; }

        public override string ToString() => Name;
    }
}
