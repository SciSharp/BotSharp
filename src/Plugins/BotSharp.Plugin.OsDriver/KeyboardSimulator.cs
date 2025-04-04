using SharpHook.Native;
using SharpHook;

namespace BotSharp.Plugin.OsDriver;

public class KeyboardSimulator
{
    public static void SimulateTextInput(string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            var simulator = new EventSimulator();
            simulator.SimulateTextEntry(text);
        }
    }

    public static void SimulateKeyCombination(string keyCombination)
    {
        if (string.IsNullOrWhiteSpace(keyCombination))
            throw new ArgumentException("Key combination cannot be null or empty.");

        // Split the key combination into individual keys
        var keys = keyCombination.Split(new[] { '+', ' ' }, StringSplitOptions.RemoveEmptyEntries);

        IEventSimulator simulator = new EventSimulator();

        // Stack to keep track of keys pressed
        Stack<KeyCode> keysDown = new Stack<KeyCode>();
        ModifierMask currentMask = ModifierMask.None;

        // Map key names to KeyCode enums
        var keyMap = new Dictionary<string, KeyCode>(StringComparer.OrdinalIgnoreCase)
        {
        { "Shift", KeyCode.VcLeftShift },
        { "Ctrl", KeyCode.VcLeftControl },
        { "Control", KeyCode.VcLeftControl },
        { "Alt", KeyCode.VcLeftAlt },
        { "LWin", KeyCode.VcLeftMeta },
        { "RWin", KeyCode.VcRightMeta },
        { "Enter", KeyCode.VcEnter },
        { "Return", KeyCode.VcEnter },
        { "Super", KeyCode.VcLeftMeta },
        { "Windows", KeyCode.VcLeftMeta },
            // Add other keys as needed
    };

        // Map modifier keys to ModifierMask enums
        var modifierMap = new Dictionary<KeyCode, ModifierMask>
        {
        { KeyCode.VcLeftShift, ModifierMask.Shift },
        { KeyCode.VcLeftControl, ModifierMask.Ctrl },
        { KeyCode.VcLeftAlt, ModifierMask.Alt },
        { KeyCode.VcLeftMeta, ModifierMask.Meta },
        { KeyCode.VcRightMeta, ModifierMask.Meta }
    };

        // Press down all keys
        foreach (var key in keys)
        {
            KeyCode keyCode;

            // Check if the key is in the keyMap
            if (keyMap.TryGetValue(key, out keyCode))
            {
                simulator.SimulateKeyPress(keyCode);

                // Update the modifier mask if it's a modifier key
                if (modifierMap.TryGetValue(keyCode, out ModifierMask modifier))
                {
                    currentMask |= modifier;
                }

                keysDown.Push(keyCode);
            }
            else if (key.Length == 1)
            {
                // Handle single character keys
                char c = key.ToUpper()[0];
                KeyCode charKeyCode = (KeyCode)Enum.Parse(typeof(KeyCode), $"Vc{c}", true);

                simulator.SimulateKeyPress(charKeyCode);

                keysDown.Push(charKeyCode);
            }
            else
            {
                throw new ArgumentException($"Unknown key: {key}");
            }
        }

        // Release keys in reverse order
        while (keysDown.Count > 0)
        {
            KeyCode keyCode = keysDown.Pop();

            // Update the modifier mask if it's a modifier key
            if (modifierMap.ContainsKey(keyCode))
            {
                currentMask &= ~modifierMap[keyCode];
            }

            simulator.SimulateKeyRelease(keyCode);
        }
    }
}
